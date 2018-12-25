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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace OutOfPhase
{
    public abstract class NewSchoolHierarchicalBindingBase : HierarchicalBindingBase
    {
        protected NewSchoolHierarchicalBindingBase(NewSchoolHierarchicalBindingBase parent, string propertyNameInParent)
            : base(parent, propertyNameInParent)
        {
        }

        public HierarchicalBindingBase Top
        {
            get
            {
                HierarchicalBindingBase top = this;
                while (top.Parent != null)
                {
                    top = top.Parent;
                }
                return top;
            }
        }

        public abstract void LoadFixup();
    }

    public abstract class NewSchoolHierarchicalBindingRoot : NewSchoolHierarchicalBindingBase
    {
        // Members cloned from HierarchicalBindingBase in DataModel.cs - would be solved by multiple inheritence :-P

        protected bool modified;

        public NewSchoolHierarchicalBindingRoot()
            : base(null, null)
        {
        }

        protected override void SetModified()
        {
            modified = true;
            if (OnSetModified != null)
            {
                OnSetModified.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler OnSetModified;
    }

    public class NewSchoolDocument : NewSchoolHierarchicalBindingRoot
    {
        public const string FileHeaderTag = "SyNS";
        public const int CurrentFormatVersionNumber = 1;


        // references to underlying documents stored on disk

        private readonly MyBindingList<NewSchoolSourceRec> sources = new MyBindingList<NewSchoolSourceRec>();
        public const string Sources_PropertyName = "Sources";
        [Bindable(true)]
        public MyBindingList<NewSchoolSourceRec> Sources { get { return sources; } }
        public NewSchoolSourceRec QuerySource(Document document)
        {
            return sources.Find(delegate (NewSchoolSourceRec s) { return s.Document == document; }); // null if not present
        }

        private readonly MyBindingList<NewSchoolExpandedSourceRec> expandedSources = new MyBindingList<NewSchoolExpandedSourceRec>();
        public const string ExpandedSources_PropertyName = "ExpandedSources";
        [Bindable(true)]
        public MyBindingList<NewSchoolExpandedSourceRec> ExpandedSources { get { return expandedSources; } }


        private string sourcePath;
        public const string SourcePath_PropertyName = "SourcePath";
        [Bindable(true)]
        public string SourcePath { get { return sourcePath; } }
        public void SetSourcePath(string newSourcePath)
        {
            Patch(newSourcePath, ref sourcePath, SourcePath_PropertyName);
            RescanSources();
            LoadFixup();
        }


        // structural parts

        private readonly MyBindingList<NewSchoolParticle> pendingParticles = new MyBindingList<NewSchoolParticle>();
        public const string PendingParticles_PropertyName = "PendingParticles";
        [Bindable(true)]
        public MyBindingList<NewSchoolParticle> PendingParticles { get { return pendingParticles; } }

        private readonly MyBindingList<NewSchoolParticle> currentParticles = new MyBindingList<NewSchoolParticle>();
        public const string CurrentParticles_PropertyName = "CurrentParticles";
        [Bindable(true)]
        public MyBindingList<NewSchoolParticle> CurrentParticles { get { return currentParticles; } }

        private readonly MyBindingList<NewSchoolGroupDefinition> groupDefinitions = new MyBindingList<NewSchoolGroupDefinition>();
        public const string GroupDefinitions_PropertyName = "GroupDefinitions";
        [Bindable(true)]
        public MyBindingList<NewSchoolGroupDefinition> GroupDefinitions { get { return groupDefinitions; } }

        private readonly MyBindingList<NewSchoolPresetDefinitionRec> presetDefinitions = new MyBindingList<NewSchoolPresetDefinitionRec>();
        public const string PresetDefinitions_PropertyName = "PresetDefinitions";
        [Bindable(true)]
        public MyBindingList<NewSchoolPresetDefinitionRec> PresetDefinitions { get { return presetDefinitions; } }


        // control parameters

        // Accessed from multiple threads - LargeBCDType is int (4-byte) which is atomic (won't tear)
        private LargeBCDType _BeatsPerMinute = (LargeBCDType)120;
        public const string BeatsPerMinute_PropertyName = "BeatsPerMinute";
        [Bindable(true)]
        public double BeatsPerMinute { get { return (double)_BeatsPerMinute; } set { double old = (double)_BeatsPerMinute; _BeatsPerMinute = (LargeBCDType)value; Patch(value, ref old, BeatsPerMinute_PropertyName); } }
        [Bindable(false)]
        public LargeBCDType BeatsPerMinuteRaw { get { return _BeatsPerMinute; } set { LargeBCDType old = _BeatsPerMinute; _BeatsPerMinute = value; Patch(value, ref old, BeatsPerMinute_PropertyName); } }

        // Accessed from multiple threads - LargeBCDType is int (4-byte) which is atomic (won't tear)
        private LargeBCDType _LoopLength = (LargeBCDType)16;
        public const string LoopLength_PropertyName = "LoopLength";
        [Bindable(true)]
        public double LoopLength { get { return (double)_LoopLength; } set { double old = (double)_LoopLength; _LoopLength = (LargeBCDType)value; Patch(value, ref old, LoopLength_PropertyName); } }
        [Bindable(false)]
        public LargeBCDType LoopLengthRaw { get { return _LoopLength; } set { LargeBCDType old = _LoopLength; _LoopLength = value; Patch(value, ref old, LoopLength_PropertyName); } }


        // playback preferences

        private int _SamplingRate = 44100;
        public const string SamplingRate_PropertyName = "SamplingRate";
        [Bindable(true)]
        public int SamplingRate { get { return _SamplingRate; } set { Patch(Math.Min(Math.Max(value, Constants.MINSAMPLINGRATE), Constants.MAXSAMPLINGRATE), ref _SamplingRate, SamplingRate_PropertyName); } }

        private int _EnvelopeUpdateRate = 441;
        public const string EnvelopeUpdateRate_PropertyName = "EnvelopeUpdateRate";
        [Bindable(true)]
        public int EnvelopeUpdateRate { get { return _EnvelopeUpdateRate; } set { Patch(Math.Min(Math.Max(value, 1), Constants.MAXSAMPLINGRATE), ref _EnvelopeUpdateRate, EnvelopeUpdateRate_PropertyName); } }

        private int _Oversampling = 1;
        public const string Oversampling_PropertyName = "Oversampling";
        [Bindable(true)]
        public int Oversampling { get { return _Oversampling; } set { Patch(Math.Min(Math.Max(value, Constants.MINOVERSAMPLING), Constants.MAXOVERSAMPLING), ref _Oversampling, Oversampling_PropertyName); } }

        private LargeBCDType _ScanningGap = (LargeBCDType).1;
        public const string ScanningGap_PropertyName = "ScanningGap";
        [Bindable(true)]
        public double ScanningGap { get { return (double)_ScanningGap; } set { double old = (double)_ScanningGap; _ScanningGap = (LargeBCDType)value; Patch(value, ref old, ScanningGap_PropertyName); } }
        [Bindable(false)]
        public LargeBCDType ScanningGapRaw { get { return _ScanningGap; } set { LargeBCDType old = _ScanningGap; _ScanningGap = value; Patch(value, ref old, ScanningGap_PropertyName); } }

        private NumBitsType _OutputNumBits = NumBitsType.eSample16bit;
        public const string OutputNumBits_PropertyName = "OutputNumBits";
        public static Enum[] OutputNumBitsAllowedValues { get { return EnumUtility.GetValues(NumBitsType.eSample8bit.GetType()); } }
        [Bindable(true)]
        public NumBitsType OutputNumBits { get { return _OutputNumBits; } set { PatchObject(value, ref _OutputNumBits, OutputNumBits_PropertyName); } }
        [Bindable(true)]
        public string OutputNumBitsAsString { get { return EnumUtility.GetDescription(_OutputNumBits); } set { string old = EnumUtility.GetDescription(_OutputNumBits); _OutputNumBits = (NumBitsType)EnumUtility.GetValue(NumBitsType.eSample8bit.GetType(), value); PatchObject(value, ref old, OutputNumBits_PropertyName); } }

        private LargeBCDType _BufferDuration = (LargeBCDType).25;
        public const string BufferDuration_PropertyName = "BufferDuration";
        [Bindable(true)]
        public double BufferDuration { get { return (double)_BufferDuration; } set { double old = (double)_BufferDuration; _BufferDuration = (LargeBCDType)value; Patch(value, ref old, BufferDuration_PropertyName); } }
        [Bindable(false)]
        public LargeBCDType BufferDurationRaw { get { return _BufferDuration; } set { LargeBCDType old = _BufferDuration; _BufferDuration = value; Patch(value, ref old, BufferDuration_PropertyName); } }

        private bool _Deterministic = true;
        public const string Deterministic_PropertyName = "Deterministic";
        [Bindable(true)]
        public bool Deterministic { get { return _Deterministic; } set { Patch(value, ref _Deterministic, Deterministic_PropertyName); } }

        private int _Seed = 1;
        public const string Seed_PropertyName = "Seed";
        [Bindable(true)]
        public int Seed { get { return _Seed; } set { Patch(value, ref _Seed, Seed_PropertyName); } }

        // Accessed from multiple threads - LargeBCDType is int (4-byte) which is atomic (won't tear)
        private LargeBCDType _OverallVolumeScalingFactor = (LargeBCDType)2;
        public const string OverallVolumeScalingFactor_PropertyName = "OverallVolumeScalingFactor";
        [Bindable(true)]
        public double OverallVolumeScalingFactor { get { return (double)OverallVolumeScalingFactorRaw; } set { OverallVolumeScalingFactorRaw = (LargeBCDType)value; } }
        public const string OverallVolumeScalingFactorRaw_PropertyName = "OverallVolumeScalingFactorRaw";
        [Bindable(false)]
        public LargeBCDType OverallVolumeScalingFactorRaw { get { return _OverallVolumeScalingFactor; } set { Patch(value, ref _OverallVolumeScalingFactor, new string[] { OverallVolumeScalingFactor_PropertyName, OverallVolumeScalingFactorRaw_PropertyName, OverallVolumeScalingDecibels_PropertyName }); } }
        public const string OverallVolumeScalingDecibels_PropertyName = "OverallVolumeScalingDecibels";
        [Bindable(true)]
        public double OverallVolumeScalingDecibels
        {
            get { return Math.Log((double)_OverallVolumeScalingFactor) * -Constants.LN_DB; }
            set
            {
                // TODO: should make a proper truly silenced value for emergencies
                OverallVolumeScalingFactor = value > -48
                    ? Math.Exp(value * -Constants.DB_EXP)
                    : (double)LargeBCDType.FromRawInt32(LargeBCDType.MAXVALRAW);
            }
        }


        private NewSchoolDocument()
        {
        }

        public static NewSchoolDocument CreateWithSourcePath(string sourcePath)
        {
            NewSchoolDocument document = new NewSchoolDocument();
            document.SetSourcePath(sourcePath);
            return document;
        }

        // For the new document format, I've chosen to use a human-readable format (XML)
        // - computers are a lot faster with bigger storage than they were in the 1990s when the binary format beat
        //   text formats by a huge margin on both fronts
        // - want flexibility during development and future versioning to easily apply conversions or recovery

        public static NewSchoolDocument CreateFromSavedFile(string filePath)
        {
            NewSchoolDocument document = new NewSchoolDocument();

            using (Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, Constants.BufferSize))
            {
                // validate file header

                byte[] signature = new byte[FileHeaderTag.Length];
                if (signature.Length != stream.Read(signature, 0, signature.Length))
                {
                    throw new InvalidDataException();
                }
                if (!String.Equals(UTF8Encoding.ASCII.GetString(signature), FileHeaderTag))
                {
                    throw new InvalidDataException();
                }


                // parse data in xml format

                using (XmlReaderStack reader = new XmlReaderStack(stream))
                {
                    document.Load(reader);
                }
            }

            document.RescanSources();
            document.LoadFixup();

            return document;
        }

        public void Save(string path)
        {
            // TODO: write to temp file to prevent data loss in case of failure/bug
            using (Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, Constants.BufferSize))
            {
                byte[] header = Encoding.ASCII.GetBytes(FileHeaderTag);
                Debug.Assert(header.Length == FileHeaderTag.Length);
                stream.Write(header, 0, header.Length);

                using (XmlWriterStack writer = new XmlWriterStack(stream))
                {
                    Save(writer);
                }
            }
        }

        private void Load(XmlReaderStack reader)
        {
            XmlBase.Read(MakeXmlTransfer(new ValueBox<int>()), reader);

            LoadFixup();
        }

        public override void LoadFixup()
        {
            for (int i = 0; i < sources.Count; i++)
            {
                NewSchoolSourceRec source = sources[i];
                source.LoadFixup();
                if (source.Document == null)
                {
                    sources.RemoveAt(i);
                    i--;
                }
            }
            for (int i = 0; i < expandedSources.Count; i++)
            {
                NewSchoolExpandedSourceRec expandedSource = expandedSources[i];
                expandedSource.LoadFixup();
                if (expandedSource.Document == null)
                {
                    expandedSources.RemoveAt(i);
                    i--;
                }
            }
        }

        private void Save(XmlWriterStack writer)
        {
            const int FormatVersion = 1;
            XmlBase.Write(MakeXmlTransfer(new ValueBox<int>(FormatVersion)), writer);
        }

        private XmlGroup MakeXmlTransfer(ValueBox<int> formatVersion)
        {
            return new XmlGroup(
                "r",
                new XmlBase[]
                {
                    new XmlInt(
                        "version",
                        () => XmlUtil.Val(formatVersion.Value, 1, 1),
                        v => formatVersion.Value = v),
                    new XmlString(
                        "sourcePath",
                        () => this.sourcePath,
                        v => this.sourcePath = v),
                    new XmlList<NewSchoolSourceRec>(
                        "sources",
                        NewSchoolSourceRec.XmlName,
                        this.sources,
                        r => new NewSchoolSourceRec(r, this),
                        (w, v) => v.Save(w)),
                    new XmlList<NewSchoolExpandedSourceRec>(
                        "expandedSources",
                        NewSchoolExpandedSourceRec.XmlName,
                        this.expandedSources,
                        r => new NewSchoolExpandedSourceRec(r, this),
                        (w, v) => v.Save(w)),
                    new XmlList<NewSchoolGroupDefinition>(
                        "groups",
                        NewSchoolGroupDefinition.XmlName,
                        this.groupDefinitions,
                        r => new NewSchoolGroupDefinition(r, this),
                        (w, v) => v.Save(w)),
                    new XmlList<NewSchoolPresetDefinitionRec>(
                        "presetSequences",
                        NewSchoolPresetDefinitionRec.XmlName,
                        this.presetDefinitions,
                        r => new NewSchoolPresetDefinitionRec(r, this),
                        (w, v) => v.Save(w)),
                });
        }


        public static Moniker MakeMoniker(NewSchoolSourceRec source, TrackObjectRec track)
        {
            return MakeMoniker(source.Path, track);
        }

        public static Moniker MakeMoniker(string path, TrackObjectRec track)
        {
            return new Moniker(
                path != null ? Path.GetFileNameWithoutExtension(path) : "<UNDEFINED>",
                track?.Name);
        }


        private struct SourceTuple
        {
            public readonly string name;
            public readonly TrackObjectRec track;
            public readonly NewSchoolExpandedSourceRec expandedSource;

            public SourceTuple(
                string name,
                TrackObjectRec track,
                NewSchoolExpandedSourceRec expandedSource)
            {
                this.name = name;
                this.track = track;
                this.expandedSource = expandedSource;
            }

            public SourceTuple WithName(string newName)
            {
                return new SourceTuple(newName, this.track, this.expandedSource);
            }

            public SourceTuple WithTrack(TrackObjectRec newTrack)
            {
                return new SourceTuple(this.name, newTrack, this.expandedSource);
            }

            public SourceTuple WithExpandedSource(NewSchoolExpandedSourceRec expandedSource)
            {
                return new SourceTuple(this.name, this.track, expandedSource);
            }
        }

        private void RescanSources()
        {
            foreach (string filePath in Directory.GetFiles(this.sourcePath))
            {
                if (Program.SniffDocumentType(filePath) == Program.DocumentType.Classic)
                {
                    // TODO: make indexing faster
                    if (sources.FindIndex(s => String.Equals(s.Path, filePath, StringComparison.OrdinalIgnoreCase)) < 0)
                    {
                        // add new

                        NewSchoolSourceRec source = new NewSchoolSourceRec(filePath, this);
                        sources.Add(source);
                        source.LoadFixup();

                        NewSchoolExpandedSourceRec expandedDocumentSource = new NewSchoolExpandedSourceRec(
                            source.Path,
                            null/*trackName*/,
                            this);
                        expandedSources.Add(expandedDocumentSource);
                        expandedDocumentSource.LoadFixup();

                        foreach (TrackObjectRec track in source.Document.TrackList)
                        {
                            NewSchoolExpandedSourceRec expandedTrackSource = new NewSchoolExpandedSourceRec(
                                source.Path,
                                track.Name,
                                this);
                            expandedSources.Add(expandedTrackSource);
                            expandedTrackSource.LoadFixup();
                            Debug.Assert(track == expandedTrackSource.Track);
                            expandedTrackSource.TrackParams.RefreshFromTrack(expandedTrackSource.Track);
                            expandedTrackSource.Moniker = MakeMoniker(source, expandedTrackSource.Track);
                        }
                    }
                    else
                    {
                        // update (merge tracks)

                        int masterIndex = expandedSources.FindIndex(s => String.IsNullOrEmpty(s.TrackName)
                            && String.Equals(s.SourcePath, filePath, StringComparison.OrdinalIgnoreCase));

                        Dictionary<string, NewSchoolExpandedSourceRec> existingTracks = new Dictionary<string, NewSchoolExpandedSourceRec>();
                        for (int i = masterIndex + 1; (i < expandedSources.Count)
                            && (expandedSources[i].Document == expandedSources[masterIndex].Document); i++)
                        {
                            existingTracks.Add(expandedSources[i].TrackName, expandedSources[i]);
                        }

                        expandedSources.RemoveRange(masterIndex + 1, existingTracks.Count);

                        IList<TrackObjectRec> trackList = expandedSources[masterIndex].Document.TrackList;
                        int index = masterIndex + 1;
                        for (int i = 0; i < trackList.Count; i++)
                        {
                            NewSchoolExpandedSourceRec expandedSource;
                            if (!existingTracks.TryGetValue(trackList[i].Name, out expandedSource))
                            {
                                expandedSource = new NewSchoolExpandedSourceRec(
                                    expandedSources[masterIndex].SourcePath,
                                    trackList[i].Name,
                                    this);
                                expandedSource.LoadFixup();
                                Debug.Assert(trackList[i] == expandedSource.Track);
                                expandedSource.Moniker = MakeMoniker(expandedSources[masterIndex].SourcePath, expandedSource.Track);
                            }
                            expandedSource.TrackParams.RefreshFromTrack(expandedSource.Track);
                            expandedSources.Insert(index++, expandedSource);
                        }
                    }
                }
            }
        }


        public delegate IMainWindowServices GetMainWindowServicesForDocument(Document document, string path);
        public void MakeUpToDate(GetMainWindowServicesForDocument getMainWindowServicesForDocument)
        {
            foreach (NewSchoolSourceRec source in sources)
            {
                source.Built = source.Document.EnsureBuilt(
                    false/*force*/,
                    new PcodeExterns(getMainWindowServicesForDocument(source.Document, source.Path)),
                    delegate (object sender, BuildErrorInfo errorInfo) { });
            }
        }


        public NewSchoolParticle InsertNewPendingParticle(Moniker sourceMoniker, string sequenceName)
        {
            NewSchoolParticle particle = new NewSchoolParticle(
                this,
                NewSchoolDocument.PendingParticles_PropertyName,
                sourceMoniker,
                sequenceName);
            pendingParticles.Add(particle);
            return particle;
        }

        // merge, insert, remove as needed
        public void UpdateCurrentParticles(List<NewSchoolParticle> newParticles) // pass in copy - will modify the collection
        {
            newParticles.Sort((l, r) => l.Moniker.CompareTo(r.Moniker));

            int iNew = 0;
            int iCurrent = 0;
            while ((iNew < newParticles.Count) && (iCurrent < currentParticles.Count))
            {
                int c = newParticles[iNew].Moniker.CompareTo(currentParticles[iCurrent].Moniker);
                if (c == 0)
                {
                    currentParticles[iCurrent].Sequence = newParticles[iNew].Sequence;
                    currentParticles[iCurrent].Sequence2 = newParticles[iNew].Sequence2;
                    iCurrent++;
                    iNew++;
                }
                else if (c < 0)
                {
                    currentParticles.Insert(iCurrent, newParticles[iNew]);
                    iCurrent++;
                    iNew++;
                }
                else // c > 0
                {
                    currentParticles.RemoveAt(iCurrent);
                }
            }
            while (iCurrent < currentParticles.Count)
            {
                currentParticles.RemoveAt(iCurrent);
            }
            while (iNew < newParticles.Count)
            {
                currentParticles.Insert(iCurrent, newParticles[iNew]);
                iNew++;
            }
        }
    }

    public class Moniker : IComparable<Moniker>, IEquatable<Moniker>
    {
        private readonly string source;
        private readonly string track;
        private readonly string param;

        public string Source { get { return source; } }
        public string Track { get { return track; } }
        public string Param { get { return param; } }

        // Didn't want to support the empty moniker but there are too many cases of textboxes editing fields
        // where it is useful.
        public static readonly Moniker Empty = new Moniker();

        public Moniker()
        {
            this.source = String.Empty;
        }

        public Moniker(string source)
        {
            if (source == null)
            {
                throw new ArgumentException();
            }
            this.source = source;
        }

        public Moniker(string source, string track)
        {
            if (source == null)
            {
                throw new ArgumentException();
            }
            this.source = source;
            this.track = track;
        }

        public Moniker(string source, string track, string param)
        {
            if (source == null)
            {
                throw new ArgumentException();
            }
            if ((track == null) && (param != null))
            {
                throw new ArgumentException();
            }
            this.source = source;
            this.track = track;
            this.param = param;
        }

        public Moniker WithSource(string source)
        {
            if ((source == null) && (this.track != null))
            {
                throw new InvalidOperationException();
            }
            return new Moniker(source, this.track, this.param);
        }

        public Moniker WithTrack(string track)
        {
            if ((this.track == null) && (this.param != null))
            {
                throw new InvalidOperationException();
            }
            if ((this.track != null) && (this.source == null))
            {
                throw new InvalidOperationException();
            }
            return new Moniker(this.source, track, this.param);
        }

        public Moniker WithParam(string param)
        {
            if ((param != null) && (this.track == null))
            {
                throw new InvalidOperationException();
            }
            return new Moniker(this.source, this.track, param);
        }

        public Moniker AsSourceOnly()
        {
            return new Moniker(source);
        }

        public Moniker AsSourceAndTrackOnly()
        {
            return new Moniker(source, track);
        }

        public string Value
        {
            get
            {
                string dot1 = track != null ? "." : null;
                string dot2 = param != null ? "." : null;
                return String.Concat(source, dot1, track, dot2, param);
            }
        }

        public static bool TryParse(string value, out Moniker moniker)
        {
            moniker = null;
            if (String.IsNullOrEmpty(value))
            {
                moniker = Moniker.Empty;
                return true;
            }
            int dot1 = value.IndexOf('.');
            if (dot1 < 0)
            {
                dot1 = value.Length;
            }
            string source = value.Substring(0, dot1);
            string track = null;
            string param = null;
            if (dot1 < value.Length)
            {
                int dot2 = value.IndexOf('.', dot1 + 1);
                if (dot2 < 0)
                {
                    dot2 = value.Length;
                }
                track = value.Substring(dot1 + 1, dot2 - (dot1 + 1));
                if (dot2 < value.Length)
                {
                    int dot3 = value.IndexOf(',', dot2 + 1);
                    if (dot3 >= 0)
                    {
                        return false;
                    }
                    param = value.Substring(dot2 + 1);
                }
            }
            moniker = new Moniker(source, track, param);
            return true;
        }

        public static Moniker Parse(string value)
        {
            Moniker moniker;
            if (!TryParse(value, out moniker))
            {
                throw new FormatException();
            }
            return moniker;
        }

        public override string ToString()
        {
            return this.Value;
        }

        public int CompareTo(Moniker other)
        {
            int c = String.Compare(this.source, other.source, StringComparison.OrdinalIgnoreCase);
            if (c != 0)
            {
                return c;
            }
            c = String.Compare(this.track, other.track, StringComparison.OrdinalIgnoreCase);
            if (c != 0)
            {
                return c;
            }
            c = String.Compare(this.param, other.param, StringComparison.OrdinalIgnoreCase);
            return c;
        }

        public bool Equals(Moniker other)
        {
            return String.Equals(this.source, other.source, StringComparison.OrdinalIgnoreCase)
                && String.Equals(this.track, other.track, StringComparison.OrdinalIgnoreCase)
                && String.Equals(this.param, other.param, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return ((IEquatable<Moniker>)this).Equals((Moniker)obj);
        }

        public override int GetHashCode()
        {
            // implementation derived from Roslyn compiler implementation for anonymous types:
            // Microsoft.CodeAnalysis.CSharp.Symbols.AnonymousTypeManager.AnonymousTypeGetHashCodeMethodSymbol 
            const int HASH_FACTOR = -1521134295;
            unchecked
            {
                int hashCode = 0L.GetHashCode();
                hashCode = hashCode * HASH_FACTOR + EqualityComparer<string>.Default.GetHashCode(this.source);
                hashCode = hashCode * HASH_FACTOR + EqualityComparer<string>.Default.GetHashCode(this.track);
                hashCode = hashCode * HASH_FACTOR + EqualityComparer<string>.Default.GetHashCode(this.param);
                return hashCode;
            }
        }
    }

    public class NewSchoolSourceRec : NewSchoolHierarchicalBindingBase
    {
        public const string Source_PropertyName = "Source";

        private string path;
        public const string Path_PropertyName = "Path";
        [Bindable(true)]
        public string Path { get { return path; } }

        private Moniker moniker;
        public const string Moniker_PropertyName = "Moniker";
        [Bindable(true)]
        public Moniker Moniker { get { return moniker; } set { Patch(value, ref moniker, Moniker_PropertyName); } }

        private Document document;
        public const string Document_PropertyName = "Document";
        [Bindable(true)]
        public Document Document { get { return document; } }

        private bool? built;
        public const string Built_PropertyName = "Built";
        [Bindable(true)]
        public bool? Built { get { return built; } set { Patch((bool?)value, () => built, x => built = x, (x, y) => (x.HasValue == y.HasValue) && (!x.HasValue || (x.Value == y.Value)), Built_PropertyName); } }
        [Bindable(true)]
        public string BuiltAsString { get { return built.HasValue ? (built.Value ? "Yes" : "FAILED") : "No"; } }


        public NewSchoolSourceRec(string path, NewSchoolHierarchicalBindingBase parent)
            : base(parent, Source_PropertyName)
        {
            this.path = path;
            this.moniker = NewSchoolDocument.MakeMoniker(this, null/*track*/);

            LoadFixup();
        }

        public NewSchoolSourceRec(XmlReaderStack reader, NewSchoolHierarchicalBindingBase parent)
            : base(parent, Source_PropertyName)
        {
            XmlBase.Read(MakeXmlTransfer(), reader);
        }

        public override void LoadFixup()
        {
            if (File.Exists(path))
            {
                this.document = new Document(path);
            }
        }

        public void Save(XmlWriterStack writer)
        {
            XmlBase.Write(MakeXmlTransfer(), writer);
        }

        public const string XmlName = "source";

        private XmlGroup MakeXmlTransfer()
        {
            return new XmlGroup(
                XmlName,
                new XmlBase[]
                {
                    new XmlString("path", () => this.path, v => this.path = v),
                    new XmlString("moniker", () => this.moniker.Value, v => this.moniker = Moniker.Parse(v)),
                });
        }
    }

    public class NewSchoolExpandedSourceRec : NewSchoolHierarchicalBindingBase
    {
        public const string ExpandedSource_PropertyName = "ExpandedSource";

        private string sourcePath;
        public const string SourcePath_PropertyName = "SourcePath";
        [Bindable(true)]
        public string SourcePath { get { return sourcePath; } }

        private Moniker moniker;
        public const string Moniker_PropertyName = "Moniker";
        [Bindable(true)]
        public Moniker Moniker { get { return moniker; } set { Patch(value, ref moniker, Moniker_PropertyName); } }
        [Bindable(true)]
        public string DisplayMoniker { get { return moniker.Track == null ? moniker.Value : String.Concat("     ", moniker.Track); } }

        private string trackName;
        public const string TrackName_PropertyName = "TrackName";
        [Bindable(true)]
        public string TrackName { get { return trackName; } }

        private Document document;
        public const string Document_PropertyName = "Document";
        [Bindable(true)]
        public Document Document { get { return document; } }

        private TrackObjectRec track; // null = this record is for the document as a whole
        public const string Track_PropertyName = "Track";
        [Bindable(true)]
        public TrackObjectRec Track { get { return track; } }

        private NewSchoolTrackParamsRec trackParams;
        public const string TrackParams_PropertyName = "TrackParams";
        [Bindable(true)]
        public NewSchoolTrackParamsRec TrackParams { get { return trackParams; } }


        public NewSchoolExpandedSourceRec(string sourcePath, string trackName, NewSchoolHierarchicalBindingBase parent)
            : base(parent, ExpandedSource_PropertyName)
        {
            this.sourcePath = sourcePath;
            this.moniker = NewSchoolDocument.MakeMoniker(sourcePath, track);
            this.trackName = trackName;
            this.trackParams = !String.IsNullOrEmpty(trackName) ? new NewSchoolTrackParamsRec(this) : null;

            LoadFixup();
        }

        public NewSchoolExpandedSourceRec(XmlReaderStack reader, NewSchoolHierarchicalBindingBase parent)
            : base(parent, ExpandedSource_PropertyName)
        {
            XmlBase.Read(MakeXmlTransfer(), reader);
        }

        public override void LoadFixup()
        {
            NewSchoolDocument document = (NewSchoolDocument)this.Top;
            NewSchoolSourceRec source = document.Sources.Find(delegate (NewSchoolSourceRec s) { return String.Equals(s.Path, this.sourcePath); });
            if (source != null)
            {
                this.document = source.Document;
                if (this.document != null)
                {
                    if (!String.IsNullOrEmpty(this.trackName))
                    {
                        this.track = this.document.TrackList.Find(delegate (TrackObjectRec t) { return String.Equals(t.Name, this.trackName); });
                    }
                    if (this.TrackParams != null)
                    {
                        this.TrackParams.LoadFixup();
                        this.TrackParams.RefreshFromTrack(this.track);
                    }
                }
            }
        }

        public void Save(XmlWriterStack writer)
        {
            XmlBase.Write(MakeXmlTransfer(), writer);
        }

        public const string XmlName = "expandedSource";

        private XmlGroup MakeXmlTransfer()
        {
            return new XmlGroup(
                XmlName,
                new XmlBase[]
                {
                    new XmlString("path", () => this.sourcePath, v => this.sourcePath = v),
                    new XmlString("moniker", () => this.moniker.Value, v => this.moniker = Moniker.Parse(v)),
                    new XmlString("track", () => this.trackName, v => this.trackName = v),
                    new XmlIf(r => r.Test(NewSchoolTrackParamsRec.XmlName), () => this.trackParams != null,
                        new XmlObject(r => this.trackParams = new NewSchoolTrackParamsRec(r, this), w => this.trackParams.Save(w))),
                });
        }
    }

    public class NewSchoolParticle : NewSchoolHierarchicalBindingBase
    {
        private Moniker moniker;
        public const string Moniker_PropertyName = "Moniker";
        [Bindable(true)]
        public Moniker Moniker { get { return moniker; } set { Patch(value, ref moniker, Moniker_PropertyName); } }

        private string sequence;
        public const string Sequence_PropertyName = "Sequence";
        [Bindable(true)]
        public string Sequence { get { return sequence; } set { Patch(value, ref sequence, Sequence_PropertyName); } }

        private string sequence2;
        public const string Sequence2_PropertyName = "Sequence2";
        [Bindable(true)]
        public string Sequence2 { get { return sequence2; } set { Patch(value, ref sequence2, Sequence2_PropertyName); } }

        public NewSchoolParticle(NewSchoolDocument document, string propertyName)
            : base(document, propertyName)
        {
        }

        public NewSchoolParticle(NewSchoolDocument document, string propertyName, Moniker moniker, string sequence, string sequence2 = null)
            : this(document, propertyName)
        {
            this.moniker = moniker;
            this.sequence = sequence;
            this.sequence2 = sequence2;
        }

        public NewSchoolParticle(NewSchoolDocument document, string propertyName, NewSchoolParticle source)
            : this(document, propertyName)
        {
            this.moniker = source.moniker;
            this.sequence = source.sequence;
            this.sequence2 = source.sequence2;
        }

        public NewSchoolParticle(XmlReaderStack reader, NewSchoolHierarchicalBindingBase parent, string propertyName)
            : base(parent, propertyName)
        {
            XmlBase.Read(MakeXmlTransfer(), reader);
        }

        public override void LoadFixup()
        {
        }

        public void Save(XmlWriterStack writer)
        {
            XmlBase.Write(MakeXmlTransfer(), writer);
        }

        public const string XmlName = "particle";

        private XmlGroup MakeXmlTransfer()
        {
            return new XmlGroup(
                XmlName,
                new XmlBase[]
                {
                    new XmlString("target", () => this.moniker.Value, v => this.moniker = Moniker.Parse(v)),
                    new XmlString("command", () => this.sequence, v => this.sequence = v),
                });
        }
    }

    public class OVal<T>
    {
        private T value;
        private bool overridden;

        public OVal()
        {
        }

        public OVal(T value)
        {
            this.value = value;
        }

        public void Set(T value)
        {
            this.value = value;
            this.overridden = true;
        }

        public void SetUnder(T value)
        {
            if (!this.overridden)
            {
                this.value = value;
            }
        }

        public T Value { get { return this.value; } }

        public bool Overridden { get { return this.overridden; } }

        public static implicit operator T(OVal<T> o)
        {
            return o.value;
        }
    }

    public class NewSchoolTrackParamsRec : NewSchoolHierarchicalBindingBase, Synthesizer.ITrackParameterProvider
    {
        // defaults for per-note parameters

        public readonly OVal<double> _DefaultPortamentoDuration = new OVal<double>();
        public const string DefaultPortamentoDuration_PropertyName = "PortamentoDuration";
        [Bindable(true)]
        public double DefaultPortamentoDuration { get { return _DefaultPortamentoDuration; } set { Patch(value, () => _DefaultPortamentoDuration, v => _DefaultPortamentoDuration.Set(v), DefaultPortamentoDuration_PropertyName); } }

        private readonly OVal<double> _DefaultEarlyLateAdjust = new OVal<double>();
        public const string DefaultEarlyLateAdjust_PropertyName = "DefaultEarlyLateAdjust";
        [Bindable(true)]
        public double DefaultEarlyLateAdjust { get { return _DefaultEarlyLateAdjust; } set { Patch(value, () => _DefaultEarlyLateAdjust, v => _DefaultEarlyLateAdjust.Set(v), DefaultEarlyLateAdjust_PropertyName); } }

        private readonly OVal<double> _DefaultReleasePoint1 = new OVal<double>();
        public const string DefaultReleasePoint1_PropertyName = "DefaultReleasePoint1";
        [Bindable(true)]
        public double DefaultReleasePoint1 { get { return _DefaultReleasePoint1; } set { Patch(value, () => _DefaultReleasePoint1, v => _DefaultReleasePoint1.Set(v), DefaultReleasePoint1_PropertyName); } }

        private readonly OVal<NoteFlags> _DefaultReleasePoint1ModeFlag = new OVal<NoteFlags>(NoteFlags.eRelease1FromEnd); // More generally useful to default to from-end
        public const string DefaultReleasePoint1ModeFlag_PropertyName = "DefaultReleasePoint1ModeFlag";
        public const string DefaultReleasePoint1ModeFlag_EnumCategoryName = NoteNoteObjectRec.ReleasePoint1Origin_EnumCategoryName;
        public static Enum[] DefaultReleasePoint1ModeFlagAllowedValues { get { return new Enum[] { NoteFlags.eRelease1FromStart, NoteFlags.eRelease1FromEnd, }; } }
        [Bindable(true)]
        public NoteFlags DefaultReleasePoint1ModeFlag
        {
            get { return _DefaultReleasePoint1ModeFlag & NoteFlags.eRelease1OriginMask; }
            set { Patch(value, () => _DefaultReleasePoint1ModeFlag, v => _DefaultReleasePoint1ModeFlag.Set(v), (a, b) => a == b, DefaultReleasePoint1ModeFlag_PropertyName); }
        }
        [Bindable(true)]
        public string DefaultReleasePoint1ModeFlagAsString
        {
            get { return EnumUtility.GetDescription(DefaultReleasePoint1ModeFlag, DefaultReleasePoint1ModeFlag_EnumCategoryName); }
            set
            {
                string old = EnumUtility.GetDescription(_DefaultReleasePoint1ModeFlag & NoteFlags.eRelease1OriginMask, DefaultReleasePoint1ModeFlag_EnumCategoryName);
                NoteFlags valueEnum = (NoteFlags)EnumUtility.GetValue(NoteFlags.eCommandFlag.GetType(), value, DefaultReleasePoint1ModeFlag_EnumCategoryName);
                this.DefaultReleasePoint1ModeFlag = valueEnum;
            }
        }
        [Bindable(true)]
        public bool DefaultReleasePoint1FromStart { get { return DefaultReleasePoint1ModeFlag == NoteFlags.eRelease1FromStart; } set { DefaultReleasePoint1ModeFlag = value ? NoteFlags.eRelease1FromStart : NoteFlags.eRelease1FromEnd; } }

        private readonly OVal<double> _DefaultReleasePoint2 = new OVal<double>();
        public const string DefaultReleasePoint2_PropertyName = "DefaultReleasePoint2";
        [Bindable(true)]
        public double DefaultReleasePoint2 { get { return _DefaultReleasePoint2; } set { Patch(value, () => _DefaultReleasePoint2, v => _DefaultReleasePoint2.Set(v), DefaultReleasePoint2_PropertyName); } }

        private readonly OVal<NoteFlags> _DefaultReleasePoint2ModeFlag = new OVal<NoteFlags>(NoteFlags.eRelease2FromStart);
        public const string DefaultReleasePoint2ModeFlag_PropertyName = "DefaultReleasePoint2ModeFlag";
        public const string DefaultReleasePoint2ModeFlag_EnumCategoryName = NoteNoteObjectRec.ReleasePoint2Origin_EnumCategoryName;
        public static Enum[] DefaultReleasePoint2ModeFlagAllowedValues { get { return new Enum[] { NoteFlags.eRelease2FromStart, NoteFlags.eRelease2FromEnd, }; } }
        [Bindable(true)]
        public NoteFlags DefaultReleasePoint2ModeFlag
        {
            get { return _DefaultReleasePoint2ModeFlag & NoteFlags.eRelease2OriginMask; }
            set { Patch(value, () => _DefaultReleasePoint2ModeFlag, v => _DefaultReleasePoint2ModeFlag.Set(v), (a, b) => a == b, DefaultReleasePoint2ModeFlag_PropertyName); }
        }
        [Bindable(true)]
        public string DefaultReleasePoint2ModeFlagAsString
        {
            get { return EnumUtility.GetDescription(DefaultReleasePoint2ModeFlag, DefaultReleasePoint2ModeFlag_EnumCategoryName); }
            set
            {
                string old = EnumUtility.GetDescription(_DefaultReleasePoint2ModeFlag & NoteFlags.eRelease2OriginMask, DefaultReleasePoint2ModeFlag_EnumCategoryName);
                NoteFlags valueEnum = (NoteFlags)EnumUtility.GetValue(NoteFlags.eCommandFlag.GetType(), value, DefaultReleasePoint2ModeFlag_EnumCategoryName);
                DefaultReleasePoint2ModeFlag = valueEnum;
            }
        }
        [Bindable(true)]
        public bool DefaultReleasePoint2FromStart { get { return DefaultReleasePoint2ModeFlag == NoteFlags.eRelease2FromStart; } set { DefaultReleasePoint2ModeFlag = value ? NoteFlags.eRelease2FromStart : NoteFlags.eRelease2FromEnd; } }

        private readonly OVal<double> _DefaultOverallLoudness = new OVal<double>(1);
        public const string DefaultOverallLoudness_PropertyName = "DefaultOverallLoudness";
        [Bindable(true)]
        public double DefaultOverallLoudness { get { return _DefaultOverallLoudness; } set { Patch(value, () => _DefaultOverallLoudness, v => _DefaultOverallLoudness.Set(v), DefaultOverallLoudness_PropertyName); } }

        private readonly OVal<double> _DefaultStereoPositioning = new OVal<double>();
        public const string DefaultStereoPositioning_PropertyName = "DefaultStereoPositioning";
        [Bindable(true)]
        public double DefaultStereoPositioning { get { return _DefaultStereoPositioning; } set { Patch(value, () => _DefaultStereoPositioning, v => _DefaultStereoPositioning.Set(v), DefaultStereoPositioning_PropertyName); } }

        private readonly OVal<double> _DefaultAccent1 = new OVal<double>();
        public const string DefaultAccent1_PropertyName = "DefaultAccent1";
        [Bindable(true)]
        public double DefaultAccent1 { get { return _DefaultAccent1; } set { Patch(value, () => _DefaultAccent1, v => _DefaultAccent1.Set(v), DefaultAccent1_PropertyName); } }

        private readonly OVal<double> _DefaultAccent2 = new OVal<double>();
        public const string DefaultAccent2_PropertyName = "DefaultAccent2";
        [Bindable(true)]
        public double DefaultAccent2 { get { return _DefaultAccent2; } set { Patch(value, () => _DefaultAccent2, v => _DefaultAccent2.Set(v), DefaultAccent2_PropertyName); } }

        private readonly OVal<double> _DefaultAccent3 = new OVal<double>();
        public const string DefaultAccent3_PropertyName = "DefaultAccent3";
        [Bindable(true)]
        public double DefaultAccent3 { get { return _DefaultAccent3; } set { Patch(value, () => _DefaultAccent3, v => _DefaultAccent3.Set(v), DefaultAccent3_PropertyName); } }

        private readonly OVal<double> _DefaultAccent4 = new OVal<double>();
        public const string DefaultAccent4_PropertyName = "DefaultAccent4";
        [Bindable(true)]
        public double DefaultAccent4 { get { return _DefaultAccent4; } set { Patch(value, () => _DefaultAccent4, v => _DefaultAccent4.Set(v), DefaultAccent4_PropertyName); } }

        private readonly OVal<double> _DefaultAccent5 = new OVal<double>();
        public const string DefaultAccent5_PropertyName = "DefaultAccent5";
        [Bindable(true)]
        public double DefaultAccent5 { get { return _DefaultAccent5; } set { Patch(value, () => _DefaultAccent5, v => _DefaultAccent5.Set(v), DefaultAccent5_PropertyName); } }

        private readonly OVal<double> _DefaultAccent6 = new OVal<double>();
        public const string DefaultAccent6_PropertyName = "DefaultAccent6";
        [Bindable(true)]
        public double DefaultAccent6 { get { return _DefaultAccent6; } set { Patch(value, () => _DefaultAccent6, v => _DefaultAccent6.Set(v), DefaultAccent6_PropertyName); } }

        private readonly OVal<double> _DefaultAccent7 = new OVal<double>();
        public const string DefaultAccent7_PropertyName = "DefaultAccent7";
        [Bindable(true)]
        public double DefaultAccent7 { get { return _DefaultAccent7; } set { Patch(value, () => _DefaultAccent7, v => _DefaultAccent7.Set(v), DefaultAccent7_PropertyName); } }

        private readonly OVal<double> _DefaultAccent8 = new OVal<double>();
        public const string DefaultAccent8_PropertyName = "DefaultAccent8";
        [Bindable(true)]
        public double DefaultAccent8 { get { return _DefaultAccent8; } set { Patch(value, () => _DefaultAccent8, v => _DefaultAccent8.Set(v), DefaultAccent8_PropertyName); } }

        private readonly OVal<double> _DefaultPitchDisplacementDepthAdjust = new OVal<double>(1);
        public const string DefaultPitchDisplacementDepthAdjust_PropertyName = "DefaultPitchDisplacementDepthAdjust";
        [Bindable(true)]
        public double DefaultPitchDisplacementDepthAdjust { get { return _DefaultPitchDisplacementDepthAdjust; } set { Patch(value, () => _DefaultPitchDisplacementDepthAdjust, v => _DefaultPitchDisplacementDepthAdjust.Set(v), DefaultPitchDisplacementDepthAdjust_PropertyName); } }

        private readonly OVal<double> _DefaultPitchDisplacementRateAdjust = new OVal<double>(1);
        public const string DefaultPitchDisplacementRateAdjust_PropertyName = "DefaultPitchDisplacementRateAdjust";
        [Bindable(true)]
        public double DefaultPitchDisplacementRateAdjust { get { return _DefaultPitchDisplacementRateAdjust; } set { Patch(value, () => _DefaultPitchDisplacementRateAdjust, v => _DefaultPitchDisplacementRateAdjust.Set(v), DefaultPitchDisplacementRateAdjust_PropertyName); } }

        private readonly OVal<double> _DefaultPitchDisplacementStartPoint = new OVal<double>();
        public const string DefaultPitchDisplacementStartPoint_PropertyName = "DefaultPitchDisplacementStartPoint";
        [Bindable(true)]
        public double DefaultPitchDisplacementStartPoint { get { return _DefaultPitchDisplacementStartPoint; } set { Patch(value, () => _DefaultPitchDisplacementStartPoint, v => _DefaultPitchDisplacementStartPoint.Set(v), DefaultPitchDisplacementStartPoint_PropertyName); } }

        private readonly OVal<NoteFlags> _DefaultPitchDisplacementStartPointModeFlag = new OVal<NoteFlags>(NoteFlags.ePitchDisplacementStartFromStart);
        public const string DefaultPitchDisplacementStartPointModeFlag_PropertyName = "DefaultPitchDisplacementStartPointModeFlag";
        public const string DefaultPitchDisplacementStartPointModeFlag_EnumCategoryName = NoteNoteObjectRec.PitchDisplacementOrigin_EnumCategoryName;
        public static Enum[] DefaultPitchDisplacementStartPointModeFlagAllowedValues { get { return new Enum[] { NoteFlags.ePitchDisplacementStartFromStart, NoteFlags.ePitchDisplacementStartFromEnd, }; } }
        [Bindable(true)]
        public NoteFlags DefaultPitchDisplacementStartPointModeFlag
        {
            get { return _DefaultPitchDisplacementStartPointModeFlag & NoteFlags.ePitchDisplacementStartOriginMask; }
            set { Patch(value, () => _DefaultPitchDisplacementStartPointModeFlag, v => _DefaultPitchDisplacementStartPointModeFlag.Set(v), (a, b) => a == b, DefaultPitchDisplacementStartPointModeFlag_PropertyName); }
        }
        [Bindable(true)]
        public string DefaultPitchDisplacementStartPointModeFlagAsString
        {
            get { return EnumUtility.GetDescription(DefaultPitchDisplacementStartPointModeFlag, DefaultPitchDisplacementStartPointModeFlag_EnumCategoryName); }
            set
            {
                string old = EnumUtility.GetDescription(_DefaultPitchDisplacementStartPointModeFlag & NoteFlags.ePitchDisplacementStartOriginMask, DefaultPitchDisplacementStartPointModeFlag_EnumCategoryName);
                NoteFlags valueEnum = (NoteFlags)EnumUtility.GetValue(NoteFlags.eCommandFlag.GetType(), value, DefaultPitchDisplacementStartPointModeFlag_EnumCategoryName);
                DefaultPitchDisplacementStartPointModeFlag = valueEnum;
            }
        }
        [Bindable(true)]
        public bool DefaultPitchDisplacementStartPointFromStart { get { return DefaultPitchDisplacementStartPointModeFlag == NoteFlags.ePitchDisplacementStartFromStart; } set { DefaultPitchDisplacementStartPointModeFlag = value ? NoteFlags.ePitchDisplacementStartFromStart : NoteFlags.ePitchDisplacementStartFromEnd; } }

        private readonly OVal<double> _DefaultHurryUpFactor = new OVal<double>(1);
        public const string DefaultHurryUpFactor_PropertyName = "DefaultHurryUpFactor";
        [Bindable(true)]
        public double DefaultHurryUpFactor { get { return _DefaultHurryUpFactor; } set { Patch(value, () => _DefaultHurryUpFactor, v => _DefaultHurryUpFactor.Set(v), DefaultHurryUpFactor_PropertyName); } }

        private readonly OVal<double> _DefaultDetune = new OVal<double>();
        public const string DefaultDetune_PropertyName = "DefaultDetune";
        [Bindable(true)]
        public double DefaultDetune { get { return _DefaultDetune; } set { Patch(value, () => _DefaultDetune, v => _DefaultDetune.Set(v), DefaultDetune_PropertyName); } }

        private readonly OVal<NoteFlags> _DefaultDetuneModeFlag = new OVal<NoteFlags>(NoteFlags.eDetuningModeHalfSteps);
        public const string DefaultDetuneModeFlag_PropertyName = "DefaultDetuneModeFlag";
        public const string DefaultDetuneModeFlag_EnumCategoryName = NoteNoteObjectRec.DetuningMode_EnumCategoryName;
        public static Enum[] DefaultDetuneModeFlagAllowedValues { get { return new Enum[] { NoteFlags.eDetuningModeHalfSteps, NoteFlags.eDetuningModeHertz, }; } }
        [Bindable(true)]
        public NoteFlags DefaultDetuneModeFlag
        {
            get { return _DefaultDetuneModeFlag & NoteFlags.eDetuningModeMask; }
            set { Patch(value, () => _DefaultDetuneModeFlag, v => _DefaultDetuneModeFlag.Set(v), (a, b) => a == b, DefaultDetuneModeFlag_PropertyName); }
        }
        [Bindable(true)]
        public string DefaultDetuneModeFlagAsString
        {
            get { return EnumUtility.GetDescription(DefaultDetuneModeFlag, DefaultDetuneModeFlag_EnumCategoryName); }
            set
            {
                string old = EnumUtility.GetDescription(_DefaultDetuneModeFlag & NoteFlags.eDetuningModeMask, DefaultDetuneModeFlag_EnumCategoryName);
                NoteFlags valueEnum = (NoteFlags)EnumUtility.GetValue(NoteFlags.eCommandFlag.GetType(), value, DefaultDetuneModeFlag_EnumCategoryName);
                DefaultDetuneModeFlag = valueEnum;
            }
        }
        [Bindable(true)]
        public bool DefaultDetuneHalfSteps { get { return DefaultDetuneModeFlag == NoteFlags.eDetuningModeHalfSteps; } set { DefaultDetuneModeFlag = value ? NoteFlags.eDetuningModeHalfSteps : NoteFlags.eDetuningModeHertz; } }

        private readonly OVal<double> _DefaultDuration = new OVal<double>();
        public const string DefaultDuration_PropertyName = "DefaultDuration";
        [Bindable(true)]
        public double DefaultDuration { get { return _DefaultDuration; } set { Patch(value, () => _DefaultDuration, v => _DefaultDuration.Set(v), DefaultDuration_PropertyName); } }

        private readonly OVal<NoteFlags> _DefaultDurationModeFlag = new OVal<NoteFlags>(NoteFlags.eDurationAdjustAdditive);
        public const string DefaultDurationModeFlag_PropertyName = "DefaultDurationModeFlag";
        public const string DefaultDurationModeFlag_EnumCategoryName = NoteNoteObjectRec.DurationAdjustMode_EnumCategoryName;
        public static Enum[] DefaultDurationModeFlagAllowedValues { get { return new Enum[] { NoteFlags.eDurationAdjustAdditive, NoteFlags.eDurationAdjustMultiplicative, }; } }
        [Bindable(true)]
        public NoteFlags DefaultDurationModeFlag
        {
            get { return _DefaultDurationModeFlag & NoteFlags.eDurationAdjustMask; }
            set { Patch(value, () => _DefaultDurationModeFlag, v => _DefaultDurationModeFlag.Set(v), (a, b) => a == b, DefaultDurationModeFlag_PropertyName); }
        }
        [Bindable(true)]
        public string DefaultDurationModeFlagAsString
        {
            get { return EnumUtility.GetDescription(DefaultDurationModeFlag, DefaultDurationModeFlag_EnumCategoryName); }
            set
            {
                string old = EnumUtility.GetDescription(_DefaultDurationModeFlag & NoteFlags.eDurationAdjustMask, DefaultDurationModeFlag_EnumCategoryName);
                NoteFlags valueEnum = (NoteFlags)EnumUtility.GetValue(NoteFlags.eCommandFlag.GetType(), value, DefaultDurationModeFlag_EnumCategoryName);
                DefaultDurationModeFlag = valueEnum;
            }
        }
        [Bindable(true)]
        public bool DefaultDurationAdditive { get { return DefaultDurationModeFlag == NoteFlags.eDurationAdjustAdditive; } set { DefaultDurationModeFlag = value ? NoteFlags.eDurationAdjustAdditive : NoteFlags.eDurationAdjustMultiplicative; } }

        private readonly OVal<double> _DefaultTrackAccent1 = new OVal<double>();
        public const string DefaultTrackAccent1_PropertyName = "DefaultTrackAccent1";
        [Bindable(true)]
        public double DefaultTrackAccent1 { get { return _DefaultTrackAccent1; } set { Patch(value, () => _DefaultTrackAccent1, v => _DefaultTrackAccent1.Set(v), DefaultTrackAccent1_PropertyName); } }

        private readonly OVal<double> _DefaultTrackAccent2 = new OVal<double>();
        public const string DefaultTrackAccent2_PropertyName = "DefaultTrackAccent2";
        [Bindable(true)]
        public double DefaultTrackAccent2 { get { return _DefaultTrackAccent2; } set { Patch(value, () => _DefaultTrackAccent2, v => _DefaultTrackAccent2.Set(v), DefaultTrackAccent2_PropertyName); } }

        private readonly OVal<double> _DefaultTrackAccent3 = new OVal<double>();
        public const string DefaultTrackAccent3_PropertyName = "DefaultTrackAccent3";
        [Bindable(true)]
        public double DefaultTrackAccent3 { get { return _DefaultTrackAccent3; } set { Patch(value, () => _DefaultTrackAccent3, v => _DefaultTrackAccent3.Set(v), DefaultTrackAccent3_PropertyName); } }

        private readonly OVal<double> _DefaultTrackAccent4 = new OVal<double>();
        public const string DefaultTrackAccent4_PropertyName = "DefaultTrackAccent4";
        [Bindable(true)]
        public double DefaultTrackAccent4 { get { return _DefaultTrackAccent4; } set { Patch(value, () => _DefaultTrackAccent4, v => _DefaultTrackAccent4.Set(v), DefaultTrackAccent4_PropertyName); } }

        private readonly OVal<double> _DefaultTrackAccent5 = new OVal<double>();
        public const string DefaultTrackAccent5_PropertyName = "DefaultTrackAccent5";
        [Bindable(true)]
        public double DefaultTrackAccent5 { get { return _DefaultTrackAccent5; } set { Patch(value, () => _DefaultTrackAccent5, v => _DefaultTrackAccent5.Set(v), DefaultTrackAccent5_PropertyName); } }

        private readonly OVal<double> _DefaultTrackAccent6 = new OVal<double>();
        public const string DefaultTrackAccent6_PropertyName = "DefaultTrackAccent6";
        [Bindable(true)]
        public double DefaultTrackAccent6 { get { return _DefaultTrackAccent6; } set { Patch(value, () => _DefaultTrackAccent6, v => _DefaultTrackAccent6.Set(v), DefaultTrackAccent6_PropertyName); } }

        private readonly OVal<double> _DefaultTrackAccent7 = new OVal<double>();
        public const string DefaultTrackAccent7_PropertyName = "DefaultTrackAccent7";
        [Bindable(true)]
        public double DefaultTrackAccent7 { get { return _DefaultTrackAccent7; } set { Patch(value, () => _DefaultTrackAccent7, v => _DefaultTrackAccent7.Set(v), DefaultTrackAccent7_PropertyName); } }

        private readonly OVal<double> _DefaultTrackAccent8 = new OVal<double>();
        public const string DefaultTrackAccent8_PropertyName = "DefaultTrackAccent8";
        [Bindable(true)]
        public double DefaultTrackAccent8 { get { return _DefaultTrackAccent8; } set { Patch(value, () => _DefaultTrackAccent8, v => _DefaultTrackAccent8.Set(v), DefaultTrackAccent8_PropertyName); } }

        private readonly OVal<int> _DefaultTranspose = new OVal<int>();
        public const string DefaultTranspose_PropertyName = "DefaultTranspose";
        [Bindable(true)]
        public int DefaultTranspose { get { return _DefaultTranspose; } set { Patch(value, () => _DefaultTranspose, v => _DefaultTranspose.Set(v), DefaultTranspose_PropertyName); } }

        private readonly OVal<string> _DefaultFrequencyModel = new OVal<string>();
        public const string DefaultFrequencyModel_PropertyName = "DefaultFrequencyModel";
        [Bindable(true)]
        public string DefaultFrequencyModel { get { return _DefaultFrequencyModel; } set { Patch(value, () => _DefaultFrequencyModel, v => _DefaultFrequencyModel.Set(v), DefaultFrequencyModel_PropertyName); } }

        private readonly OVal<int> _DefaultFrequencyModelTonicOffset = new OVal<int>();
        public const string DefaultFrequencyModelTonicOffset_PropertyName = "DefaultFrequencyModelTonicOffset";
        [Bindable(true)]
        public int DefaultFrequencyModelTonicOffset { get { return _DefaultFrequencyModelTonicOffset; } set { Patch(value, () => _DefaultFrequencyModelTonicOffset, v => _DefaultFrequencyModelTonicOffset.Set(v), DefaultFrequencyModelTonicOffset_PropertyName); } }

        private readonly OVal<bool> _DefaultFrequencyModelRelativeToCurrent = new OVal<bool>();
        public const string DefaultFrequencyModelRelativeToCurrent_PropertyName = "DefaultFrequencyModelRelativeToCurrent";
        [Bindable(true)]
        public bool DefaultFrequencyModelRelativeToCurrent { get { return _DefaultFrequencyModelRelativeToCurrent; } set { Patch(value, () => _DefaultFrequencyModelRelativeToCurrent, v => _DefaultFrequencyModelRelativeToCurrent.Set(v), DefaultFrequencyModelRelativeToCurrent_PropertyName); } }


        private readonly MyBindingList<BindableParam> bindableParamList;
        [Bindable(true)]
        public MyBindingList<BindableParam> BindableParamList { get { return bindableParamList; } }
        [Bindable(false)]
        public static readonly Dictionary<string, int> BindableParamListIndex = new Dictionary<string, int>();

        private readonly Dictionary<string, string> paramComments = new Dictionary<string, string>();
        private readonly Dictionary<string, SliderState> paramMetrics = new Dictionary<string, SliderState>();

        public NewSchoolTrackParamsRec(NewSchoolExpandedSourceRec parent)
            : base(parent, NewSchoolExpandedSourceRec.TrackParams_PropertyName)
        {
            bindableParamList = new MyBindingList<BindableParam>();

            bindableParamList.Add(new BindableParamSlidable<double>("loudness", null, () => _DefaultOverallLoudness, v => _DefaultOverallLoudness.Set(v), new SliderState(0.05, 10, SliderScale.LogWithZero)));
            bindableParamList.Add(new BindableParamSlidable<double>("pan", null, () => _DefaultStereoPositioning, v => _DefaultStereoPositioning.Set(v), new SliderState(-1, 1)));
            bindableParamList.Add(new BindableParam<int>("transpose", null, () => _DefaultTranspose, v => _DefaultTranspose.Set(v)));
            bindableParamList.Add(new BindableParamSlidable<double>("hurryup", null, () => _DefaultHurryUpFactor, v => _DefaultHurryUpFactor.Set(v)));
            bindableParamList.Add(new BindableParamSlidable<double>("release1", null, () => _DefaultReleasePoint1, v => _DefaultReleasePoint1.Set(v)));
            bindableParamList.Add(new BindableParamEnum<bool>("release1origin", null, new(string, bool)[] { ("start", true), ("end", false) }, () => DefaultReleasePoint1FromStart, v => DefaultReleasePoint1FromStart = v));
            bindableParamList.Add(new BindableParamSlidable<double>("release2", null, () => _DefaultReleasePoint2, v => _DefaultReleasePoint2.Set(v)));
            bindableParamList.Add(new BindableParamEnum<bool>("release2origin", null, new(string, bool)[] { ("start", true), ("end", false) }, () => DefaultReleasePoint2FromStart, v => DefaultReleasePoint2FromStart = v));

            bindableParamList.Add(new BindableParamSlidable<double>("accent1", null, () => _DefaultAccent1, v => _DefaultAccent1.Set(v), new SliderState(-7, 7), paramComments, paramMetrics));
            bindableParamList.Add(new BindableParamSlidable<double>("accent2", null, () => _DefaultAccent2, v => _DefaultAccent2.Set(v), new SliderState(-7, 7), paramComments, paramMetrics));
            bindableParamList.Add(new BindableParamSlidable<double>("accent3", null, () => _DefaultAccent3, v => _DefaultAccent3.Set(v), new SliderState(-7, 7), paramComments, paramMetrics));
            bindableParamList.Add(new BindableParamSlidable<double>("accent4", null, () => _DefaultAccent4, v => _DefaultAccent4.Set(v), new SliderState(-7, 7), paramComments, paramMetrics));
            bindableParamList.Add(new BindableParamSlidable<double>("accent5", null, () => _DefaultAccent5, v => _DefaultAccent5.Set(v), new SliderState(-7, 7), paramComments, paramMetrics));
            bindableParamList.Add(new BindableParamSlidable<double>("accent6", null, () => _DefaultAccent6, v => _DefaultAccent6.Set(v), new SliderState(-7, 7), paramComments, paramMetrics));
            bindableParamList.Add(new BindableParamSlidable<double>("accent7", null, () => _DefaultAccent7, v => _DefaultAccent7.Set(v), new SliderState(-7, 7), paramComments, paramMetrics));
            bindableParamList.Add(new BindableParamSlidable<double>("accent8", null, () => _DefaultAccent8, v => _DefaultAccent8.Set(v), new SliderState(-7, 7), paramComments, paramMetrics));

            bindableParamList.Add(new BindableParamSlidable<double>("trackaccent1", null, () => _DefaultTrackAccent1, v => _DefaultTrackAccent1.Set(v), paramComments, paramMetrics));
            bindableParamList.Add(new BindableParamSlidable<double>("trackaccent2", null, () => _DefaultTrackAccent2, v => _DefaultTrackAccent2.Set(v), paramComments, paramMetrics));
            bindableParamList.Add(new BindableParamSlidable<double>("trackaccent3", null, () => _DefaultTrackAccent3, v => _DefaultTrackAccent3.Set(v), paramComments, paramMetrics));
            bindableParamList.Add(new BindableParamSlidable<double>("trackaccent4", null, () => _DefaultTrackAccent4, v => _DefaultTrackAccent4.Set(v), paramComments, paramMetrics));
            bindableParamList.Add(new BindableParamSlidable<double>("trackaccent5", null, () => _DefaultTrackAccent5, v => _DefaultTrackAccent5.Set(v), paramComments, paramMetrics));
            bindableParamList.Add(new BindableParamSlidable<double>("trackaccent6", null, () => _DefaultTrackAccent6, v => _DefaultTrackAccent6.Set(v), paramComments, paramMetrics));
            bindableParamList.Add(new BindableParamSlidable<double>("trackaccent7", null, () => _DefaultTrackAccent7, v => _DefaultTrackAccent7.Set(v), paramComments, paramMetrics));
            bindableParamList.Add(new BindableParamSlidable<double>("trackaccent8", null, () => _DefaultTrackAccent8, v => _DefaultTrackAccent8.Set(v), paramComments, paramMetrics));

            bindableParamList.Add(new BindableParamSlidable<double>("portamento", null, () => _DefaultPortamentoDuration, v => _DefaultPortamentoDuration.Set(v)));
            bindableParamList.Add(new BindableParamSlidable<double>("earlylateadjust", null, () => _DefaultEarlyLateAdjust, v => _DefaultEarlyLateAdjust.Set(v), new SliderState(-1, 1)));
            bindableParamList.Add(new BindableParamSlidable<double>("pitchdisplacementdepth", null, () => _DefaultPitchDisplacementDepthAdjust, v => _DefaultPitchDisplacementDepthAdjust.Set(v)));
            bindableParamList.Add(new BindableParamSlidable<double>("pitchdisplacementrate", null, () => _DefaultPitchDisplacementRateAdjust, v => _DefaultPitchDisplacementRateAdjust.Set(v)));
            bindableParamList.Add(new BindableParamSlidable<double>("pitchdisplacementoffset", null, () => _DefaultPitchDisplacementStartPoint, v => _DefaultPitchDisplacementStartPoint.Set(v)));
            bindableParamList.Add(new BindableParamEnum<bool>("pitchdisplacementoffsetorigin", null, new(string, bool)[] { ("start", true), ("end", false) }, () => DefaultPitchDisplacementStartPointFromStart, v => DefaultPitchDisplacementStartPointFromStart = v));
            bindableParamList.Add(new BindableParamSlidable<double>("detune", null, () => _DefaultDetune, v => _DefaultDetune.Set(v), new SliderState(-1, 1))); // TODO: change slider scale based on "detunemode"
            bindableParamList.Add(new BindableParamEnum<bool>("detunemode", null, new(string, bool)[] { ("hertz", true), ("halfsteps", false) }, () => DefaultDetuneHalfSteps, v => DefaultDetuneHalfSteps = v));
            bindableParamList.Add(new BindableParamSlidable<double>("duration", null, () => _DefaultDuration, v => _DefaultDuration.Set(v), new SliderState(-1, 1)));
            bindableParamList.Add(new BindableParamEnum<bool>("durationmode", null, new(string, bool)[] { ("add", true), ("multiply", false) }, () => DefaultDurationAdditive, v => DefaultDurationAdditive = v));

            bindableParamList.Add(new BindableParam<string>("freqmodel", null, () => _DefaultFrequencyModel, v => _DefaultFrequencyModel.Set(v)));
            bindableParamList.Add(new BindableParam<int>("freqmodeltonicoffset", null, () => _DefaultFrequencyModelTonicOffset, v => _DefaultFrequencyModelTonicOffset.Set(v)));
            bindableParamList.Add(new BindableParamEnum<bool>("freqmodelrelativetocurrent", null, new(string, bool)[] { ("false", false), ("true", true) }, () => _DefaultFrequencyModelRelativeToCurrent, v => _DefaultFrequencyModelRelativeToCurrent.Set(v)));

            for (int i = 1; i <= 8; i++)
            {
                paramComments.Add(String.Concat("accent", i), String.Empty);
                paramComments.Add(String.Concat("trackaccent", i), String.Empty);
                paramComments.Add(String.Concat("sectionaccent", i), String.Empty);
                paramComments.Add(String.Concat("scoreaccent", i), String.Empty);
            }

            if (BindableParamListIndex.Count == 0)
            {
                for (int i = 0; i < BindableParamList.Count; i++)
                {
                    BindableParamListIndex[BindableParamList[i].ParamName] = i;
                }
            }
        }

        public NewSchoolTrackParamsRec(XmlReaderStack reader, NewSchoolExpandedSourceRec parent)
            : this(parent)
        {
            XmlBase.Read(MakeXmlTransfer(), reader);
        }

        public override void LoadFixup()
        {
        }

        public void Save(XmlWriterStack writer)
        {
            XmlBase.Write(MakeXmlTransfer(), writer);
        }

        public const string XmlName = "trackParams";

        private XmlGroup MakeXmlTransfer()
        {
            return new XmlGroup(
                XmlName,
                new XmlBase[]
                {
                    new XmlDouble("portamentoDuration", () => _DefaultPortamentoDuration, v => _DefaultPortamentoDuration.Set(v), () => _DefaultPortamentoDuration.Overridden),
                    new XmlDouble("earlyLateAdjust", () => _DefaultEarlyLateAdjust, v => _DefaultEarlyLateAdjust.Set(v), () => _DefaultEarlyLateAdjust.Overridden),
                    new XmlDouble("releasePoint1", () => _DefaultReleasePoint1, v => _DefaultReleasePoint1.Set(v), () => _DefaultReleasePoint1.Overridden),
                    new XmlBool("releasePoint1FromStart", () => DefaultReleasePoint1FromStart, v => DefaultReleasePoint1FromStart = v, () => _DefaultReleasePoint1ModeFlag.Overridden),
                    new XmlDouble("releasePoint2", () => _DefaultReleasePoint2, v => _DefaultReleasePoint2.Set(v), () => _DefaultReleasePoint2.Overridden),
                    new XmlBool("releasePoint2FromStart", () => DefaultReleasePoint2FromStart, v => DefaultReleasePoint2FromStart = v, () => _DefaultReleasePoint2ModeFlag.Overridden),
                    new XmlDouble("overallLoudness", () => _DefaultOverallLoudness, v => _DefaultOverallLoudness.Set(v), () => _DefaultOverallLoudness.Overridden),
                    new XmlDouble("stereoPositioning", () => _DefaultStereoPositioning, v => _DefaultStereoPositioning.Set(v), () => _DefaultStereoPositioning.Overridden),
                    new XmlDouble("accent1", () => _DefaultAccent1, v => _DefaultAccent1.Set(v), () => _DefaultAccent1.Overridden),
                    new XmlDouble("accent2", () => _DefaultAccent2, v => _DefaultAccent2.Set(v), () => _DefaultAccent2.Overridden),
                    new XmlDouble("accent3", () => _DefaultAccent3, v => _DefaultAccent3.Set(v), () => _DefaultAccent3.Overridden),
                    new XmlDouble("accent4", () => _DefaultAccent4, v => _DefaultAccent4.Set(v), () => _DefaultAccent4.Overridden),
                    new XmlDouble("accent5", () => _DefaultAccent5, v => _DefaultAccent5.Set(v), () => _DefaultAccent5.Overridden),
                    new XmlDouble("accent6", () => _DefaultAccent6, v => _DefaultAccent6.Set(v), () => _DefaultAccent6.Overridden),
                    new XmlDouble("accent7", () => _DefaultAccent7, v => _DefaultAccent7.Set(v), () => _DefaultAccent7.Overridden),
                    new XmlDouble("accent8", () => _DefaultAccent8, v => _DefaultAccent8.Set(v), () => _DefaultAccent8.Overridden),
                    new XmlDouble("pitchDisplacementDepthAdjust", () => _DefaultPitchDisplacementDepthAdjust, v => _DefaultPitchDisplacementDepthAdjust.Set(v), () => _DefaultPitchDisplacementDepthAdjust.Overridden),
                    new XmlDouble("pitchDisplacementRateAdjust", () => _DefaultPitchDisplacementRateAdjust, v => _DefaultPitchDisplacementRateAdjust.Set(v), () => _DefaultPitchDisplacementRateAdjust.Overridden),
                    new XmlDouble("pitchDisplacementStartPoint", () => _DefaultPitchDisplacementStartPoint, v => _DefaultPitchDisplacementStartPoint.Set(v), () => _DefaultPitchDisplacementStartPoint.Overridden),
                    new XmlBool("pitchDisplacementStartPointFromStart", () => DefaultPitchDisplacementStartPointFromStart, v => DefaultPitchDisplacementStartPointFromStart = v, () => _DefaultPitchDisplacementStartPointModeFlag.Overridden),
                    new XmlDouble("hurryUpFactor", () => _DefaultHurryUpFactor, v => _DefaultHurryUpFactor.Set(v), () => _DefaultHurryUpFactor.Overridden),
                    new XmlDouble("detune", () => _DefaultDetune, v => _DefaultDetune.Set(v), () => _DefaultDetune.Overridden),
                    new XmlBool("detuneHalfSteps", () => DefaultDetuneHalfSteps, v => DefaultDetuneHalfSteps = v, () => _DefaultDetuneModeFlag.Overridden),
                    new XmlDouble("duration", () => _DefaultDuration, v => _DefaultDuration.Set(v), () => _DefaultDuration.Overridden),
                    new XmlBool("durationAdditive", () => DefaultDurationAdditive, v => DefaultDurationAdditive = v, () => _DefaultDurationModeFlag.Overridden),
                    new XmlDouble("trackAccent1", () => _DefaultTrackAccent1, v => _DefaultTrackAccent1.Set(v), () => _DefaultTrackAccent1.Overridden),
                    new XmlDouble("trackAccent2", () => _DefaultTrackAccent2, v => _DefaultTrackAccent2.Set(v), () => _DefaultTrackAccent2.Overridden),
                    new XmlDouble("trackAccent3", () => _DefaultTrackAccent3, v => _DefaultTrackAccent3.Set(v), () => _DefaultTrackAccent3.Overridden),
                    new XmlDouble("trackAccent4", () => _DefaultTrackAccent4, v => _DefaultTrackAccent4.Set(v), () => _DefaultTrackAccent4.Overridden),
                    new XmlDouble("trackAccent5", () => _DefaultTrackAccent5, v => _DefaultTrackAccent5.Set(v), () => _DefaultTrackAccent5.Overridden),
                    new XmlDouble("trackAccent6", () => _DefaultTrackAccent6, v => _DefaultTrackAccent6.Set(v), () => _DefaultTrackAccent6.Overridden),
                    new XmlDouble("trackAccent7", () => _DefaultTrackAccent7, v => _DefaultTrackAccent7.Set(v), () => _DefaultTrackAccent7.Overridden),
                    new XmlDouble("trackAccent8", () => _DefaultTrackAccent8, v => _DefaultTrackAccent8.Set(v), () => _DefaultTrackAccent8.Overridden),
                    new XmlInt("transpose", () => _DefaultTranspose, v => _DefaultTranspose.Set(v), () => _DefaultTranspose.Overridden),
                    new XmlString("frequencyModel", () => _DefaultFrequencyModel, v => _DefaultFrequencyModel.Set(v), () => _DefaultFrequencyModel.Overridden),
                    new XmlInt("frequencyModelTonicOffset", () => _DefaultFrequencyModelTonicOffset, v => _DefaultFrequencyModelTonicOffset.Set(v), () => _DefaultFrequencyModelTonicOffset.Overridden),
                    new XmlBool("frequencyModelRelativeToCurrent", () => _DefaultFrequencyModelRelativeToCurrent, v => _DefaultFrequencyModelRelativeToCurrent.Set(v), () => _DefaultFrequencyModelRelativeToCurrent.Overridden),
                });
        }

        public void RefreshFromTrack(TrackObjectRec track)
        {
            this._DefaultOverallLoudness.SetUnder(track.DefaultOverallLoudness);
            this._DefaultStereoPositioning.SetUnder(track.DefaultStereoPositioning);
            this._DefaultHurryUpFactor.SetUnder(track.DefaultHurryUpFactor);
            this._DefaultReleasePoint1.SetUnder(track.DefaultReleasePoint1);
            this._DefaultReleasePoint1ModeFlag.SetUnder(track.DefaultReleasePoint1ModeFlag);
            this._DefaultReleasePoint2.SetUnder(track.DefaultReleasePoint2);
            this._DefaultReleasePoint2ModeFlag.SetUnder(track.DefaultReleasePoint2ModeFlag);
            this._DefaultAccent1.SetUnder(track.DefaultAccent1);
            this._DefaultAccent2.SetUnder(track.DefaultAccent2);
            this._DefaultAccent3.SetUnder(track.DefaultAccent3);
            this._DefaultAccent4.SetUnder(track.DefaultAccent4);
            this._DefaultAccent5.SetUnder(track.DefaultAccent5);
            this._DefaultAccent6.SetUnder(track.DefaultAccent6);
            this._DefaultAccent7.SetUnder(track.DefaultAccent7);
            this._DefaultAccent8.SetUnder(track.DefaultAccent8);
            //this._DefaultTrackAccent1 = ??? // TODO:
            //this._DefaultTrackAccent2 = ??? // TODO:
            //this._DefaultTrackAccent3 = ??? // TODO:
            //this._DefaultTrackAccent4 = ??? // TODO:
            //this._DefaultTrackAccent5 = ??? // TODO:
            //this._DefaultTrackAccent6 = ??? // TODO:
            //this._DefaultTrackAccent7 = ??? // TODO:
            //this._DefaultTrackAccent8 = ??? // TODO:
            this._DefaultPortamentoDuration.SetUnder(0); // track never had a default portamento field
            this._DefaultEarlyLateAdjust.SetUnder(track.DefaultEarlyLateAdjust);
            this._DefaultPitchDisplacementDepthAdjust.SetUnder(track.DefaultPitchDisplacementDepthAdjust);
            this._DefaultPitchDisplacementRateAdjust.SetUnder(track.DefaultPitchDisplacementRateAdjust);
            this._DefaultPitchDisplacementStartPoint.SetUnder(track.DefaultPitchDisplacementStartPoint);
            this._DefaultPitchDisplacementStartPointModeFlag.SetUnder(track.DefaultPitchDisplacementStartPointModeFlag);
            this._DefaultDetune.SetUnder(track.DefaultDetune);
            this._DefaultDetuneModeFlag.SetUnder(track.DefaultDetuneModeFlag);
            this._DefaultDuration.SetUnder(track.DefaultDuration);
            this._DefaultDurationModeFlag.SetUnder(track.DefaultDurationModeFlag);
            this._DefaultTranspose.SetUnder(0); // track never had a default transpose field

            // scan through first bunch of track commands to get overrides of these values.
            // stop scanning when a note or sequence marker is detected
            for (int i = 0; i < track.FrameArray.Count; i++)
            {
                if (!track.FrameArray[i].IsThisACommandFrame)
                {
                    break;
                }
                Debug.Assert(track.FrameArray[i].Count == 1);
                Debug.Assert(track.FrameArray[i][0] is CommandNoteObjectRec);
                CommandNoteObjectRec command = (CommandNoteObjectRec)track.FrameArray[i][0];
                if (command.GetCommandOpcode() == NoteCommands.eCmdSequenceBegin)
                {
                    break;
                }
                switch (command.GetCommandOpcode())
                {
                    default:
                        break;
                    case NoteCommands.eCmdMarker:
                        foreach (string line in command.GetCommandStringArg1().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            int c = line.IndexOfAny(new char[] { ':', '=' });
                            if (c > 0)
                            {
                                string key = line.Substring(0, c).Trim().ToLowerInvariant();
                                if (paramComments.ContainsKey(key))
                                {
                                    paramComments[key] = line.Substring(c + 1).Trim();
                                    Match m = paramRangeRegex.Match(paramComments[key]);
                                    if (m != Match.Empty)
                                    {
                                        string scale = m.Groups[2].Value;
                                        string low = m.Groups[3].Value;
                                        string high = m.Groups[5].Value;
                                        bool linear = String.IsNullOrEmpty(scale) || String.Equals(scale, "lin", StringComparison.OrdinalIgnoreCase);
                                        bool log = String.Equals(scale, "log", StringComparison.OrdinalIgnoreCase);
                                        bool logWithZero = String.Equals(scale, "log0", StringComparison.OrdinalIgnoreCase) || String.Equals(scale, "logwith0", StringComparison.OrdinalIgnoreCase);
                                        if (linear || log || logWithZero)
                                        {
                                            double vlow, vhigh;
                                            if (Double.TryParse(low, out vlow) && Double.TryParse(high, out vhigh))
                                            {
                                                paramMetrics[key] = new SliderState(vlow, vhigh, linear ? SliderScale.Linear : log ? SliderScale.Log : logWithZero ? SliderScale.LogWithZero : throw new ArgumentException());
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case NoteCommands.eCmdSetStereoPosition: /* set position in channel <1l>: -1 = left, 1 = right */
                        this._DefaultStereoPositioning.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdSetSurroundPosition: /* set position in channel <1l>: 1 = front, -1 = rear */
                        break;
                    case NoteCommands.eCmdSetVolume: /* set the volume to the specified level (0..1) in <1l> */
                        this._DefaultOverallLoudness.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdSetReleasePoint1: /* set the default release point to new value <1l> */
                        this._DefaultReleasePoint1.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdReleasePointOrigin1: /* <1i> -1 = from start, 0 = from end of note */
                        this._DefaultReleasePoint1ModeFlag.SetUnder((command._Argument1 < 0) ? NoteFlags.eRelease1FromStart : NoteFlags.eRelease1FromEnd);
                        break;
                    case NoteCommands.eCmdSetReleasePoint2: /* set the default release point to new value <1l> */
                        this._DefaultReleasePoint2.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdReleasePointOrigin2: /* <1i> -1 = from start, 0 = from end of note */
                        this._DefaultReleasePoint2ModeFlag.SetUnder((command._Argument1 < 0) ? NoteFlags.eRelease2FromStart : NoteFlags.eRelease2FromEnd);
                        break;
                    case NoteCommands.eCmdSetAccent1: /* specify the new default accent in <1l> */
                        this._DefaultAccent1.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdSetAccent2: /* specify the new default accent in <1l> */
                        this._DefaultAccent2.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdSetAccent3: /* specify the new default accent in <1l> */
                        this._DefaultAccent3.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdSetAccent4: /* specify the new default accent in <1l> */
                        this._DefaultAccent4.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdSetAccent5: /* specify the new default accent in <1l> */
                        this._DefaultAccent5.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdSetAccent6: /* specify the new default accent in <1l> */
                        this._DefaultAccent6.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdSetAccent7: /* specify the new default accent in <1l> */
                        this._DefaultAccent7.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdSetAccent8: /* specify the new default accent in <1l> */
                        this._DefaultAccent8.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdSetPitchDispDepth: /* set new max pitch disp depth <1l> */
                        this._DefaultPitchDisplacementDepthAdjust.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdSetPitchDispRate: /* set new max pitch disp rate in seconds to <1l> */
                        this._DefaultPitchDisplacementRateAdjust.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdSetPitchDispStart: /* set the start point to <1l> */
                        this._DefaultPitchDisplacementStartPoint.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdPitchDispStartOrigin: /* specify the origin, same as for release point <1i> */
                        this._DefaultPitchDisplacementStartPointModeFlag.SetUnder((command._Argument1 < 0) ? NoteFlags.ePitchDisplacementStartFromStart : NoteFlags.ePitchDisplacementStartFromEnd);
                        break;
                    case NoteCommands.eCmdSetHurryUp: /* set the hurryup factor to <1l> */
                        this._DefaultHurryUpFactor.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdSetDetune: /* set the detune factor to <1l> */
                        this._DefaultDetune.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdDetuneMode: /* <1i>:  -1: Hertz, 0: half-steps */
                        this._DefaultDetuneModeFlag.SetUnder((command._Argument1 < 0) ? NoteFlags.eDetuningModeHertz : NoteFlags.eDetuningModeHalfSteps);
                        break;
                    case NoteCommands.eCmdSetEarlyLateAdjust: /* set the early/late adjust value to <1l> */
                        this._DefaultEarlyLateAdjust.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdSetDurationAdjust: /* set duration adjust value to <1l> */
                        this._DefaultDuration.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdDurationAdjustMode: /* <1i>:  -1: Multiplicative, 0: Additive */
                        this._DefaultDurationModeFlag.SetUnder((command._Argument1 >= 0) ? NoteFlags.eDurationAdjustMultiplicative : NoteFlags.eDurationAdjustAdditive);
                        break;
                    case NoteCommands.eCmdSetTranspose: /* <1i> = new transpose value */
                        this._DefaultTranspose.SetUnder(command._Argument1);
                        break;
                    // TODO: can we incorporate direct commands updating specific pitches?
                    case NoteCommands.eCmdLoadFrequencyModel: // <1s> = model name, <1l> = tonic offset (integer 0..11)
                        {
                            // <1l> arg:
                            //  - tonic offset (absolute magnitude, integer part, 0..11 modulo 12)
                            //  - sign: negative: relative to existing tonic; non-neg: reset relative to standard concert pitch
                            //    (use -12 to specify for tonic C since 0 can't be made negative)
                            LargeBCDType arg = LargeBCDType.FromRawInt32(command._Argument1);
                            int tonicOffset = (int)Math.Abs((double)arg) % 12;
                            bool relativeToCurrent = (double)arg < 0;
                            this._DefaultFrequencyModel.SetUnder(command._StringArgument1);
                            this._DefaultFrequencyModelTonicOffset.SetUnder(tonicOffset);
                            this._DefaultFrequencyModelRelativeToCurrent.SetUnder(relativeToCurrent);
                        }
                        break;
                    case NoteCommands.eCmdSetPortamento: /* set the portamento to the specified level (0..1) in <1l> */
                        this._DefaultPortamentoDuration.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdSetEffectParam1: /* specify the new default effect parameter in <1l> */
                        this._DefaultTrackAccent1.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdSetEffectParam2: /* specify the new default effect parameter in <1l> */
                        this._DefaultTrackAccent2.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdSetEffectParam3: /* specify the new default effect parameter in <1l> */
                        this._DefaultTrackAccent3.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdSetEffectParam4: /* specify the new default effect parameter in <1l> */
                        this._DefaultTrackAccent4.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdSetEffectParam5: /* specify the new default effect parameter in <1l> */
                        this._DefaultTrackAccent5.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdSetEffectParam6: /* specify the new default effect parameter in <1l> */
                        this._DefaultTrackAccent6.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdSetEffectParam7: /* specify the new default effect parameter in <1l> */
                        this._DefaultTrackAccent7.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                    case NoteCommands.eCmdSetEffectParam8: /* specify the new default effect parameter in <1l> */
                        this._DefaultTrackAccent8.SetUnder((double)LargeBCDType.FromRawInt32(command._Argument1));
                        break;
                }
            }
        }

        private readonly static Regex paramRangeRegex =
            new Regex(@"\[\s*((\w*?)\s*:)?\s*(-?[0-9\.]+)\s*(\.\.|,)\s*(-?[0-9\.]+)\s*\]", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        // groups:      0000000000000000000000000000000000000000000000000000000000000000
        //                   111111111111
        //                    222222
        //                                   333333333333
        //                                                  44444444
        //                                                             555555555555
    }

    public abstract class BindableParam : INotifyPropertyChanged
    {
        private readonly string paramName;
        [Bindable(true)]
        public string ParamName { get { return paramName; } }

        private readonly string paramHelp;
        private readonly Dictionary<string, string> helpOverride;

        public event PropertyChangedEventHandler PropertyChanged;

        [Bindable(true)]
        public string ParamHelp
        {
            get
            {
                string otherHelp;
                if ((helpOverride != null) && helpOverride.TryGetValue(paramName, out otherHelp) && !String.IsNullOrEmpty(otherHelp))
                {
                    return otherHelp;
                }
                return paramHelp;
            }
        }

        [Bindable(true)]
        public abstract Type ParamType { get; }
        [Bindable(true)]
        public abstract object ParamValue { get; set; }

        [Bindable(false)]
        public virtual (string, object)[] ParamValueRange { get { throw new NotSupportedException(); } }

        [Bindable(false)]
        public virtual bool Slidable { get { return false; } }
        [Bindable(false)]
        public virtual SliderState Metrics { get { throw new NotSupportedException(); } }

        public BindableParam(string paramName, string help, Dictionary<string, string> helpOverride)
        {
            this.paramName = paramName;
            this.paramHelp = help;
            this.helpOverride = helpOverride;
        }

        public const string ParamValue_PropertyName = "ParamValue";
        protected void OnParamValueChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ParamValue_PropertyName));
        }
    }

    public class BindableParam<T> : BindableParam
    {
        private readonly Func<T> readFunc;
        private readonly Action<T> writeFunc;

        public BindableParam(string paramName, string help, Func<T> readFunc, Action<T> writeFunc)
            : this(paramName, help, readFunc, writeFunc, null/*helpOverride*/)
        {
        }

        public BindableParam(string paramName, string help, Func<T> readFunc, Action<T> writeFunc, Dictionary<string, string> helpOverride)
            : base(paramName, help, helpOverride)
        {
            if (!(this is BindableParamEnum<bool>) && (typeof(T) == typeof(bool)))
            {
                throw new ArgumentException("BindableParam<bool> not supported, use BindableParamEnum<bool> instead");
            }

            this.readFunc = readFunc;
            this.writeFunc = writeFunc;
        }

        public override Type ParamType { get { return typeof(T); } }

        public override object ParamValue
        {
            get { return readFunc(); }
            set { writeFunc((T)value); OnParamValueChanged(); }
        }
    }

    public class BindableParamEnum<T> : BindableParam<T>
    {
        private (string, object)[] valuesRange;

        public BindableParamEnum(string paramName, string help, (string, T)[] valuesRange, Func<T> readFunc, Action<T> writeFunc)
            : base(paramName, help, readFunc, writeFunc, null/*helpOverride*/)
        {
            InitValuesRange(valuesRange);
        }

        public BindableParamEnum(string paramName, string help, (string, T)[] valuesRange, Func<T> readFunc, Action<T> writeFunc, Dictionary<string, string> helpOverride)
            : base(paramName, help, readFunc, writeFunc, helpOverride)
        {
            InitValuesRange(valuesRange);
        }

        private void InitValuesRange((string, T)[] valuesRange)
        {
            this.valuesRange = new(string, object)[valuesRange.Length];
            for (int i = 0; i < this.valuesRange.Length; i++)
            {
                this.valuesRange[i] = (valuesRange[i].Item1, valuesRange[i].Item2);
            }
        }

        public override (string, object)[] ParamValueRange { get { return valuesRange; } }
    }

    public class BindableParamSlidable<T> : BindableParam<T>
    {
        private readonly SliderState metrics;
        private readonly Dictionary<string, SliderState> metricsOverride;

        public BindableParamSlidable(string paramName, string help, Func<T> readFunc, Action<T> writeFunc)
            : this(paramName, help, readFunc, writeFunc, new SliderState(), null/*helpOverride*/, null/*metricOverride*/)
        {
        }

        public BindableParamSlidable(string paramName, string help, Func<T> readFunc, Action<T> writeFunc, Dictionary<string, string> helpOverride)
            : this(paramName, help, readFunc, writeFunc, new SliderState(), helpOverride, null/*metricOverride*/)
        {
        }

        public BindableParamSlidable(string paramName, string help, Func<T> readFunc, Action<T> writeFunc, SliderState metric)
            : this(paramName, help, readFunc, writeFunc, metric, null/*helpOverride*/, null/*metricOverride*/)
        {
        }

        public BindableParamSlidable(string paramName, string help, Func<T> readFunc, Action<T> writeFunc, Dictionary<string, string> helpOverride, Dictionary<string, SliderState> metricOverride)
            : this(paramName, help, readFunc, writeFunc, new SliderState(), helpOverride, metricOverride)
        {
        }

        public BindableParamSlidable(string paramName, string help, Func<T> readFunc, Action<T> writeFunc, SliderState metric, Dictionary<string, string> helpOverride, Dictionary<string, SliderState> metricOverride)
            : base(paramName, help, readFunc, writeFunc, helpOverride)
        {
            this.metrics = metric;
            this.metricsOverride = metricOverride;
        }

        public override bool Slidable { get { return true; } }

        public override SliderState Metrics { get { return (metricsOverride == null) || !metricsOverride.ContainsKey(ParamName) ? metrics : metricsOverride[ParamName]; } }
    }

    public abstract class BindableParamDelegated : BindableParam
    {
        private ParamBoardEntry paramBoardEntry;

        public ParamBoardEntry ParamBoardEntry { get { return paramBoardEntry; } set { paramBoardEntry = value; } }

        public BindableParamDelegated(string paramName, string help, Dictionary<string, string> helpOverride)
            : base(paramName, help, helpOverride)
        {
        }

        public void Poke()
        {
            OnParamValueChanged();
        }
    }

    public class BindableParamDelegated<T> : BindableParamDelegated
    {
        private readonly BindableParam template;

        private readonly Func<T> readFunc;
        private readonly Action<T> writeFunc;

        public BindableParamDelegated(BindableParam template, Func<T> readFunc, Action<T> writeFunc)
            : base(template.ParamName, template.ParamHelp, null)
        {
            this.template = template;
            this.readFunc = readFunc;
            this.writeFunc = writeFunc;
        }

        public override Type ParamType { get { return template.ParamType; } }
        public override object ParamValue { get { return readFunc(); } set { writeFunc((T)value); Poke(); } }

        public override (string, object)[] ParamValueRange { get { return template.ParamValueRange; } }

        public override bool Slidable { get { return template.Slidable; } }
        public override SliderState Metrics { get { return template.Metrics; } }
    }

    public class NewSchoolGroupDefinition : NewSchoolHierarchicalBindingBase
    {
        public const string GroupDefinition_PropertyName = "GroupDefinition";

        private string groupName;
        public const string GroupName_PropertyName = "GroupName";
        [Bindable(true)]
        public string GroupName { get { return groupName; } set { Patch(value, ref groupName, GroupName_PropertyName); } }

        private MyBindingList<Moniker> memberMonikers = new MyBindingList<Moniker>();
        public const string MemberMonikers_PropertyName = "MemberMonikers";
        [Bindable(true)]
        public BindingList<Moniker> MemberMonikers { get { return memberMonikers; } }

        public NewSchoolGroupDefinition(string groupName, NewSchoolHierarchicalBindingBase parent)
            : base(parent, GroupDefinition_PropertyName)
        {
            this.groupName = groupName;

            LoadFixup();
        }

        public NewSchoolGroupDefinition(XmlReaderStack reader, NewSchoolHierarchicalBindingBase parent)
            : base(parent, GroupDefinition_PropertyName)
        {
            XmlBase.Read(MakeXmlTransfer(), reader);
        }

        public override void LoadFixup()
        {
        }

        public void Save(XmlWriterStack writer)
        {
            XmlBase.Write(MakeXmlTransfer(), writer);
        }

        public const string XmlName = "group";
        private const string XmlMonikerTag = "moniker";

        private XmlGroup MakeXmlTransfer()
        {
            return new XmlGroup(
                XmlName,
                new XmlBase[]
                {
                    new XmlString("name", () => this.groupName, v => this.groupName= v),
                    new XmlList<Moniker>(
                        "monikers",
                        XmlMonikerTag,
                        memberMonikers,
                        (r) => Moniker.Parse(r.ReadString(XmlMonikerTag)),
                        (w, v) => w.WriteString(XmlMonikerTag, v.Value)),
                });
        }
    }

    public class NewSchoolPresetDefinitionRec : NewSchoolHierarchicalBindingBase
    {
        public const string PresetSequence_PropertyName = "PresetSequence";

        private string presetName;
        public const string PresetName_PropertyName = "PresetName";
        [Bindable(true)]
        public string PresetName { get { return presetName; } set { Patch(value, ref presetName, PresetName_PropertyName); } }

        private readonly MyBindingList<NewSchoolParticle> targets = new MyBindingList<NewSchoolParticle>();
        public const string Target_PropertyName = "Target";
        [Bindable(true)]
        public MyBindingList<NewSchoolParticle> Targets { get { return targets; } }


        public NewSchoolPresetDefinitionRec(string presetName, NewSchoolHierarchicalBindingBase parent)
            : base(parent, PresetSequence_PropertyName)
        {
            this.presetName = presetName;

            LoadFixup();
        }

        public NewSchoolPresetDefinitionRec(XmlReaderStack reader, NewSchoolHierarchicalBindingBase parent)
            : base(parent, PresetSequence_PropertyName)
        {
            XmlBase.Read(MakeXmlTransfer(), reader);
        }

        public override void LoadFixup()
        {
        }

        public void Save(XmlWriterStack writer)
        {
            XmlBase.Write(MakeXmlTransfer(), writer);
        }

        public const string XmlName = "presetSequence";

        private XmlGroup MakeXmlTransfer()
        {
            return new XmlGroup(
                XmlName,
                new XmlBase[]
                {
                    new XmlString("name", () => this.presetName, v => this.presetName= v),
                    new XmlList<NewSchoolParticle>(
                        "targets",
                        NewSchoolParticle.XmlName,
                        targets,
                        r => new NewSchoolParticle(r, this, Target_PropertyName),
                        (w, v) => v.Save(w)),
                });
        }
    }
}
