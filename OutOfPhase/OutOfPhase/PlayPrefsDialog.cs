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
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class PlayPrefsDialog : Form
    {
        private readonly Registration registration;
        private readonly object identity;
        private readonly MainWindow mainWindow;
        private readonly Document document;
        private readonly Source source;

        public PlayPrefsDialog(Registration registration, object identity, MainWindow mainWindow, Document document)
        {
            this.registration = registration;
            this.identity = identity;
            this.mainWindow = mainWindow;
            this.document = document;

            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            foreach (string item in EnumUtility.GetDescriptions(Source.OutputNumBitsAllowedValues))
            {
                comboBoxBitDepth.Items.Add(item);
            }

            foreach (string item in EnumUtility.GetDescriptions(Source.NumChannelsAllowedValues))
            {
                comboBoxNumChannels.Items.Add(item);
            }

            source = new Source(document);
            sourceBindingSource.Add(source);

            listBoxIncludedTracks.SetUnderlying(source.IncludedTracks, delegate (object obj) { return ((Source.TrackInclusionRec)obj).Name; });
            for (int i = 0; i < source.IncludedTracks.Count; i++)
            {
                if (source.IncludedTracks[i].Included)
                {
                    listBoxIncludedTracks.SelectItem(i, false/*clear other selections*/);
                }
            }
            listBoxIncludedTracks.SelectionChanged += new EventHandler(delegate (object sender, EventArgs e) { PropagateTrackInclusion(); });
            source.OnIncludedTracksAdded += Source_OnIncludedTracksAdded;

            checkBoxDeterministic.CheckedChanged += new EventHandler(delegate (object sender, EventArgs e) { textBoxSeed.Enabled = checkBoxDeterministic.Checked; });

            this.Text = String.Format("{0} - {1}", mainWindow.DisplayName, "Play");

            registration.Register(identity, this);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            registration.Unregister(identity, this);
            base.OnFormClosed(e);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            Validate(); // commit current edit - we will accept noncommit in case where data is invalid
            base.OnDeactivate(e);
        }

        protected override void WndProc(ref Message m)
        {
            dpiChangeHelper.WndProcDelegate(ref m);
            base.WndProc(ref m);
        }

        [Bindable(true)]
        public Source SourceProperty { get { return source; } }

        public class Source : HierarchicalBindingRoot, IPlayPrefsProvider
        {
            private int _SamplingRate;
            public const string SamplingRate_PropertyName = "SamplingRate";
            [Bindable(true)]
            public int SamplingRate { get { return _SamplingRate; } set { Patch(value, ref _SamplingRate, SamplingRate_PropertyName); } }

            private int _EnvelopeUpdateRate;
            public const string EnvelopeUpdateRate_PropertyName = "EnvelopeUpdateRate";
            [Bindable(true)]
            public int EnvelopeUpdateRate { get { return _EnvelopeUpdateRate; } set { Patch(value, ref _EnvelopeUpdateRate, EnvelopeUpdateRate_PropertyName); } }

            private int _Oversampling;
            public const string Oversampling_PropertyName = "Oversampling";
            [Bindable(true)]
            public int Oversampling { get { return _Oversampling; } set { Patch(value, ref _Oversampling, Oversampling_PropertyName); } }

            private LargeBCDType _DefaultBeatsPerMinute;
            public const string DefaultBeatsPerMinute_PropertyName = "DefaultBeatsPerMinute";
            [Bindable(true)]
            public double DefaultBeatsPerMinute { get { return (double)_DefaultBeatsPerMinute; } set { double old = (double)_DefaultBeatsPerMinute; _DefaultBeatsPerMinute = (LargeBCDType)value; Patch(value, ref old, DefaultBeatsPerMinute_PropertyName); } }

            private LargeBCDType _OverallVolumeScalingFactor;
            public const string OverallVolumeScalingFactor_PropertyName = "OverallVolumeScalingFactor";
            [Bindable(true)]
            public double OverallVolumeScalingFactor { get { return (double)_OverallVolumeScalingFactor; } set { double old = (double)_OverallVolumeScalingFactor; _OverallVolumeScalingFactor = (LargeBCDType)value; Patch(value, ref old, OverallVolumeScalingFactor_PropertyName); } }

            private NumBitsType _OutputNumBits;
            public const string OutputNumBits_PropertyName = "OutputNumBits";
            public static Enum[] OutputNumBitsAllowedValues { get { return EnumUtility.GetValues(NumBitsType.eSample8bit.GetType()); } }
            [Bindable(true)]
            public NumBitsType OutputNumBits { get { return _OutputNumBits; } set { PatchObject(value, ref _OutputNumBits, OutputNumBits_PropertyName); } }
            [Bindable(true)]
            public string OutputNumBitsAsString { get { return EnumUtility.GetDescription(_OutputNumBits); } set { string old = EnumUtility.GetDescription(_OutputNumBits); _OutputNumBits = (NumBitsType)EnumUtility.GetValue(NumBitsType.eSample8bit.GetType(), value); PatchObject(value, ref old, OutputNumBits_PropertyName); } }

            private LargeBCDType _ScanningGap;
            public const string ScanningGap_PropertyName = "ScanningGap";
            [Bindable(true)]
            public double ScanningGap { get { return (double)_ScanningGap; } set { double old = (double)_ScanningGap; _ScanningGap = (LargeBCDType)value; Patch(value, ref old, ScanningGap_PropertyName); } }

            private LargeBCDType _BufferDuration;
            public const string BufferDuration_PropertyName = "BufferDuration";
            [Bindable(true)]
            public double BufferDuration { get { return (double)_BufferDuration; } set { double old = (double)_BufferDuration; _BufferDuration = (LargeBCDType)value; Patch(value, ref old, BufferDuration_PropertyName); } }

            private bool _ClipWarning;
            public const string ClipWarning_PropertyName = "ClipWarning";
            [Bindable(true)]
            public bool ClipWarning { get { return _ClipWarning; } set { Patch(value, ref _ClipWarning, ClipWarning_PropertyName); } }

            private NumChannelsType _NumChannels;
            public const string NumChannels_PropertyName = "NumChannels";
            public static Enum[] NumChannelsAllowedValues { get { return EnumUtility.GetValues(NumChannelsType.eSampleMono.GetType()); } }
            [Bindable(true)]
            public NumChannelsType NumChannels { get { return _NumChannels; } set { PatchObject(value, ref _NumChannels, NumChannels_PropertyName); } }
            [Bindable(true)]
            public string NumChannelsAsString { get { return EnumUtility.GetDescription(_NumChannels); } set { string old = EnumUtility.GetDescription(_NumChannels); _NumChannels = (NumChannelsType)EnumUtility.GetValue(NumChannelsType.eSampleMono.GetType(), value); PatchObject(value, ref old, NumChannels_PropertyName); } }

            private bool _ShowSummary;
            public const string ShowSummary_PropertyName = "ShowSummary";
            [Bindable(true)]
            public bool ShowSummary { get { return _ShowSummary; } set { Patch(value, ref _ShowSummary, ShowSummary_PropertyName); } }

            private bool _Deterministic;
            public const string Deterministic_PropertyName = "Deterministic";
            [Bindable(true)]
            public bool Deterministic { get { return _Deterministic; } set { Patch(value, ref _Deterministic, Deterministic_PropertyName); } }

            private int _Seed;
            public const string Seed_PropertyName = "Seed";
            [Bindable(true)]
            public int Seed { get { return _Seed; } set { Patch(value, ref _Seed, Seed_PropertyName); } }

            public class TrackInclusionRec : HierarchicalBindingBase
            {
                public readonly TrackObjectRec Track;

                [Bindable(true)]
                public string Name { get { return Track.Name; } }

                public bool _Included;
                public const string Included_PropertyName = "Included";
                [Bindable(true)]
                public bool Included { get { return _Included; } set { Patch(value, ref _Included, Included_PropertyName); } }

                public TrackInclusionRec(TrackObjectRec Track, Source source)
                    : base(source, Source.IncludedTracks_PropertyName)
                {
                    this.Track = Track;
                    this._Included = Track.IncludeThisTrackInFinalPlayback;
                    this.Track.PropertyChanged += Track_PropertyChanged;
                }

                private void Track_PropertyChanged(object sender, PropertyChangedEventArgs e)
                {
                    Changed(e.PropertyName);
                }

                public new void Changed(string propertyName)
                {
                    base.Changed(propertyName);
                }
            }

            private readonly BindingList<TrackInclusionRec> _IncludedTracks = new BindingList<TrackInclusionRec>();
            public const string IncludedTracks_PropertyName = "IncludedTracks";
            [Bindable(true)]
            public BindingList<TrackInclusionRec> IncludedTracks { get { return _IncludedTracks; } }

            TrackObjectRec[] IPlayPrefsProvider.IncludedTracks
            {
                get
                {
                    List<TrackObjectRec> included = new List<TrackObjectRec>();
                    foreach (TrackInclusionRec inclusion in _IncludedTracks)
                    {
                        if (inclusion.Included)
                        {
                            included.Add(inclusion.Track);
                        }
                    }
                    return included.ToArray();
                }
            }

            private readonly Document _document;

            public event ListChangedEventHandler OnIncludedTracksAdded;

            public Source(Document document)
            {
                _document = document;

                _SamplingRate = document.SamplingRate;
                _EnvelopeUpdateRate = document.EnvelopeUpdateRate;
                _Oversampling = document.Oversampling;
                _DefaultBeatsPerMinute = (LargeBCDType)document.DefaultBeatsPerMinute;
                _OverallVolumeScalingFactor = (LargeBCDType)document.OverallVolumeScalingFactor;
                _OutputNumBits = document.OutputNumBits;
                _ScanningGap = (LargeBCDType)document.ScanningGap;
                _BufferDuration = (LargeBCDType)document.BufferDuration;
                _ClipWarning = document.ClipWarning;
                _NumChannels = document.NumChannels;
                _ShowSummary = document.ShowSummary;
                _Deterministic = document.Deterministic;
                _Seed = document.Seed;
                foreach (TrackObjectRec Track in document.TrackList)
                {
                    IncludedTracks.Add(new TrackInclusionRec(Track, this));
                }

                document.TrackList.ListChanged += TrackList_ListChanged;
            }

            public void Save(Document document)
            {
                document.SamplingRate = _SamplingRate;
                document.EnvelopeUpdateRate = _EnvelopeUpdateRate;
                document.Oversampling = _Oversampling;
                document.DefaultBeatsPerMinute = (double)_DefaultBeatsPerMinute;
                document.OverallVolumeScalingFactor = (double)_OverallVolumeScalingFactor;
                document.OutputNumBits = _OutputNumBits;
                document.ScanningGap = (double)_ScanningGap;
                document.BufferDuration = (double)_BufferDuration;
                document.ClipWarning = _ClipWarning;
                document.NumChannels = _NumChannels;
                document.ShowSummary = _ShowSummary;
                document.Deterministic = _Deterministic;
                document.Seed = _Seed;
                foreach (TrackInclusionRec inclusion in _IncludedTracks)
                {
                    if (inclusion.Track.IncludeThisTrackInFinalPlayback != inclusion.Included)
                    {
                        inclusion.Track.IncludeThisTrackInFinalPlayback = inclusion.Included;
                    }
                }
            }

            private void TrackList_ListChanged(object sender, ListChangedEventArgs e)
            {
                switch (e.ListChangedType)
                {
                    default:
                        Debug.Assert(false);
                        break;
                    case ListChangedType.ItemAdded:
                        _IncludedTracks.Insert(e.NewIndex, new TrackInclusionRec(_document.TrackList[e.NewIndex], this));
                        if (OnIncludedTracksAdded != null)
                        {
                            OnIncludedTracksAdded.Invoke(_IncludedTracks, e);
                        }
                        break;
                    case ListChangedType.ItemDeleted:
                        _IncludedTracks.RemoveAt(e.NewIndex);
                        break;
                    case ListChangedType.ItemChanged:
                        _IncludedTracks[e.NewIndex].Changed(e.PropertyDescriptor.Name);
                        break;
                    case ListChangedType.Reset:
                    case ListChangedType.PropertyDescriptorChanged:
                    case ListChangedType.PropertyDescriptorAdded:
                    case ListChangedType.PropertyDescriptorDeleted:
                    case ListChangedType.ItemMoved:
                        Dictionary<TrackObjectRec, TrackInclusionRec> old = new Dictionary<TrackObjectRec, TrackInclusionRec>();
                        foreach (TrackInclusionRec item in _IncludedTracks)
                        {
                            old.Add(item.Track, item);
                        }
                        _IncludedTracks.Clear();
                        foreach (TrackObjectRec track in _document.TrackList)
                        {
                            if (old.ContainsKey(track))
                            {
                                _IncludedTracks.Add(old[track]);
                            }
                            else
                            {
                                _IncludedTracks.Add(new TrackInclusionRec(track, this));
                            }
                        }
                        break;
                }
            }
        }

        // source --> UI
        private void Source_OnIncludedTracksAdded(object sender, ListChangedEventArgs e)
        {
            if (source.IncludedTracks[e.NewIndex].Track.IncludeThisTrackInFinalPlayback)
            {
                listBoxIncludedTracks.SelectItem(e.NewIndex, false/*clearOtherSelections*/);
            }
        }

        // UI --> source
        private void PropagateTrackInclusion()
        {
            int[] selected = listBoxIncludedTracks.SelectedIndices;
            for (int i = 0; i < source.IncludedTracks.Count; i++)
            {
                source.IncludedTracks[i].Included = false;
            }
            foreach (int i in selected)
            {
                source.IncludedTracks[i].Included = true;
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonDone_Click(object sender, EventArgs e)
        {
            if (!Validate())
            {
                return;
            }

            source.Save(document);

            Close();
        }

        private void buttonPlayToAudio_Click(object sender, EventArgs e)
        {
            if (!Validate())
            {
                return;
            }

            // TODO: reuse code with file playback
            List<TrackObjectRec> included = new List<TrackObjectRec>();
            foreach (Source.TrackInclusionRec inclusion in source.IncludedTracks)
            {
                if (inclusion.Included)
                {
                    included.Add(inclusion.Track);
                }
            }
#if true // prevents "Add New Data Source..." from working
            SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.Do(
                mainWindow.DisplayName,
                OutputDeviceEnumerator.OutputDeviceGetDestination,
                OutputDeviceEnumerator.CreateOutputDeviceDestinationHandler,
                new OutputDeviceArguments(source.BufferDuration),
                SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.SynthesizerMainLoop,
                new SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>(
                    mainWindow,
                    document,
                    included,
                    included[0],
                    0,
                    source.SamplingRate,
                    source.EnvelopeUpdateRate,
                    source.NumChannels,
                    (LargeBCDType)source.DefaultBeatsPerMinute,
                    source.OverallVolumeScalingFactor,
                    (LargeBCDType)source.ScanningGap,
                    source.OutputNumBits,
                    source.ClipWarning,
                    source.Oversampling,
                    source.ShowSummary,
                    source.Deterministic,
                    source.Seed,
                    null/*automationSettings*/),
                SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.SynthesizerCompletion,
                mainWindow,
                source.NumChannels,
                source.OutputNumBits,
                source.SamplingRate,
                source.Oversampling,
                true/*showProgressWindow*/,
                true/*modal*/);
#endif
        }

        private void buttonPlayToDisk_Click(object sender, EventArgs e)
        {
            if (!Validate())
            {
                return;
            }

            // TODO: reuse code with device playback
            List<TrackObjectRec> included = new List<TrackObjectRec>();
            foreach (Source.TrackInclusionRec inclusion in source.IncludedTracks)
            {
                if (inclusion.Included)
                {
                    included.Add(inclusion.Track);
                }
            }
#if true // prevents "Add New Data Source..." from working
            SynthesizerGeneratorParams<OutputSelectableFileDestination, OutputSelectableFileArguments>.Do(
                mainWindow.DisplayName,
                OutputSelectableFileDestinationHandler.OutputSelectableFileGetDestination,
                OutputSelectableFileDestinationHandler.CreateOutputSelectableFileDestinationHandler,
                new OutputSelectableFileArguments(),
                SynthesizerGeneratorParams<OutputSelectableFileDestination, OutputSelectableFileArguments>.SynthesizerMainLoop,
                new SynthesizerGeneratorParams<OutputSelectableFileDestination, OutputSelectableFileArguments>(
                    mainWindow,
                    document,
                    included,
                    included[0],
                    0,
                    source.SamplingRate,
                    source.EnvelopeUpdateRate,
                    source.NumChannels,
                    (LargeBCDType)source.DefaultBeatsPerMinute,
                    source.OverallVolumeScalingFactor,
                    (LargeBCDType)source.ScanningGap,
                    source.OutputNumBits,
                    source.ClipWarning,
                    source.Oversampling,
                    source.ShowSummary,
                    source.Deterministic,
                    source.Seed,
                    null/*automationSettings*/),
                SynthesizerGeneratorParams<OutputSelectableFileDestination, OutputSelectableFileArguments>.SynthesizerCompletion,
                mainWindow,
                source.NumChannels,
                source.OutputNumBits,
                source.SamplingRate,
                source.Oversampling,
                true/*showProgressWindow*/,
                true/*modal*/);
#endif
        }

        private void buttonSelectAllTracks_Click(object sender, EventArgs e)
        {
            listBoxIncludedTracks.SelectAll();
        }

        private void buttonUnselectAllTracks_Click(object sender, EventArgs e)
        {
            listBoxIncludedTracks.SelectNone();
        }
    }
}
