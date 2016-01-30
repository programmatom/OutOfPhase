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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    public static class Constants
    {
        private const int MaxSmallObjectHeapObjectSize = 85000; // http://msdn.microsoft.com/en-us/magazine/cc534993.aspx, http://blogs.msdn.com/b/dotnet/archive/2011/10/04/large-object-heap-improvements-in-net-4-5.aspx
        private const int PageSize = 4096;
        private const int MaxSmallObjectPageDivisibleSize = MaxSmallObjectHeapObjectSize & ~(PageSize - 1);

        public const int BufferSize = MaxSmallObjectPageDivisibleSize;

        public const int MINTABCOUNT = 1;
        public const int MAXTABCOUNT = 255;

        public const int MINOVERSAMPLING = 1;
        public const int MAXOVERSAMPLING = 255;

        public const int MINSAMPLINGRATE = 100;
        public const int MAXSAMPLINGRATE = 300000;

        public const float MINNATURALFREQ = 0.01f;
        public const float MAXNATURALFREQ = 1e6f;

        /* number of notes.  this should be a whole number of octaves */
        public const int NUMNOTES = 32 * 12; /* should make CENTERNOTE a multiple of 12 */
        public const int CENTERNOTE = NUMNOTES / 2; /* middle C -- should be a multiple of 12 */
        public const double MIDDLEC = 261.625565300598635;
        public const double LOG2 = 0.693147180559945309;
        public const double INVLOG2 = 1.44269504088896341;

        // common maximum possible denominator for note duration fractions
        public const int Denominator = 64 * 3 * 5 * 7 * 2; // should be equal to DURATIONUPDATECLOCKRESOLUTION

        public const int MINAUTOSAVEINTERVAL = 5;
        public const int MAXAUTOSAVEINTERVAL = Int32.MaxValue;
    }


    public static class EnumUtility
    {
        public static Enum[] GetValues(Type type, string category)
        {
            List<Enum> values = new List<Enum>();
            foreach (FieldInfo fieldInfo in type.GetFields())
            {
                if (!fieldInfo.IsLiteral)
                {
                    continue;
                }

                if (!HasDescription(fieldInfo))
                {
                    continue;
                }

                if (!IsInCategory(fieldInfo, category))
                {
                    continue;
                }

                values.Add((Enum)Enum.ToObject(type, fieldInfo.GetValue(type)));
            }
            return values.ToArray();
        }

        public static Enum[] GetValues(Type type)
        {
            return GetValues(type, null);
        }


        public static Enum GetValue(Type type, string description, string category)
        {
            foreach (FieldInfo fieldInfo in type.GetFields())
            {
                if (!fieldInfo.IsLiteral)
                {
                    continue;
                }

                if (!IsInCategory(fieldInfo, category))
                {
                    continue;
                }

                object[] descriptionAttributes = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false/*inherit*/);
                foreach (object o in descriptionAttributes)
                {
                    if ((o as DescriptionAttribute).Description == description)
                    {
                        return (Enum)Enum.ToObject(type, fieldInfo.GetValue(type));
                    }
                }
            }
            Debug.Assert(false);
            throw new ArgumentException();
        }

        public static Enum GetValue(Type type, string description)
        {
            return GetValue(type, description, null);
        }


        // use this version to work with [Flags] enums and subsets
        public static string[] GetDescriptions(Enum[] values, string category)
        {
            List<string> descriptions = new List<string>();
            foreach (Enum value in values)
            {
                Type fieldType = value.GetType();
                object valueObject = Enum.ToObject(fieldType, value);
                foreach (FieldInfo fieldInfo in fieldType.GetFields())
                {
                    if (!fieldInfo.IsLiteral)
                    {
                        continue;
                    }

                    if (!IsInCategory(fieldInfo, category))
                    {
                        continue;
                    }

                    object candidateValueObject = Enum.ToObject(fieldType, fieldInfo.GetValue(fieldType));
                    if (valueObject.Equals(candidateValueObject))
                    {
                        object[] descriptionAttributes = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false/*inherit*/);
                        foreach (object o in descriptionAttributes)
                        {
                            descriptions.Add((descriptionAttributes[0] as DescriptionAttribute).Description);
                        }
                        goto ItemDone;
                    }
                }
                Debug.Assert(false);
                throw new ArgumentException();
            ItemDone:
                ;
            }
            return descriptions.ToArray();
        }

        public static string[] GetDescriptions(Enum[] values)
        {
            return GetDescriptions(values, null);
        }

        public static string[] GetDescriptions(Type type, string category)
        {
            return GetDescriptions(GetValues(type, category), category);
        }

        public static string[] GetDescriptions(Type type)
        {
            return GetDescriptions(GetValues(type), null);
        }


        public static string GetDescription(Enum value, string category)
        {
            Type enumType = value.GetType();
            object valueObject = Enum.ToObject(enumType, value);
            foreach (FieldInfo fieldInfo in enumType.GetFields())
            {
                if (!fieldInfo.IsLiteral)
                {
                    continue;
                }

                if (!IsInCategory(fieldInfo, category))
                {
                    continue;
                }

                Type fieldType = fieldInfo.GetType();
                object candidateValueObject = Enum.ToObject(enumType, fieldInfo.GetValue(enumType));
                if (valueObject.Equals(candidateValueObject))
                {
                    object[] descriptionAttributes = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false/*inherit*/);
                    if (descriptionAttributes.Length == 0)
                    {
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
                    return (descriptionAttributes[0] as DescriptionAttribute).Description;
                }
            }
            Debug.Assert(false);
            throw new ArgumentException();
        }

        public static string GetDescription(Enum value)
        {
            return GetDescription(value, null);
        }


        private static bool IsInCategory(FieldInfo fieldInfo, string category)
        {
            if (category == null)
            {
                return true;
            }
            else
            {
                object[] categoryAttributes = fieldInfo.GetCustomAttributes(typeof(CategoryAttribute), false/*inherit*/);
                foreach (object o in categoryAttributes)
                {
                    if (String.Equals(((CategoryAttribute)o).Category, category))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool HasDescription(FieldInfo fieldInfo)
        {
            object[] descriptionAttributes = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false/*inherit*/);
            return (descriptionAttributes != null) && (descriptionAttributes.Length > 0);
        }
    }

    public enum NumBitsType
    {
        [Description("8 bit")]
        eSample8bit,
        [Description("16 bit")]
        eSample16bit,
        [Description("24 bit")]
        eSample24bit,

        Max = eSample24bit,
    }

    public enum NumChannelsType
    {
        [Description("Mono")]
        eSampleMono,
        [Description("Stereo")]
        eSampleStereo,
    }

    public enum LoopBidirectionalType
    {
        [Description("Unidirectional")]
        No,
        [Description("Bidirectional")]
        Yes,
    }

    public enum ChannelType
    {
        eLeftChannel,
        eRightChannel,
        eMonoChannel,
    }

    /* this type specifies errors that can be returned from routines that try */
    /* to get fixed-point arrays in the expression evaluator. */
    public enum SampleErrors
    {
        eEvalSampleNoError,
        eEvalSampleUndefined,
        eEvalSampleWrongChannel,
    }


    //

    public abstract class HierarchicalBindingBase : INotifyPropertyChanged
    {
        private readonly string propertyNameInParent;
        private readonly HierarchicalBindingBase parent;

        private HierarchicalBindingBase()
        {
        }

        protected HierarchicalBindingBase(HierarchicalBindingBase parent, string propertyNameInParent)
        {
            this.parent = parent;
            this.propertyNameInParent = propertyNameInParent;
        }

        public HierarchicalBindingBase Parent { get { return parent; } }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void SetModified()
        {
            if (parent != null)
            {
                parent.SetModified();
            }
        }

        protected virtual void NotifyFromChild(string propertyName, bool modified)
        {
            if (parent != null)
            {
                parent.NotifyFromChild(propertyNameInParent, modified);
            }
        }

        protected bool Patch<T>(T newValue, ref T storage, string propertyName, bool modified) where T : IEquatable<T>
        {
            if (!EqualityComparer<T>.Default.Equals(newValue, storage))
            {
                storage = newValue;
                Changed(propertyName, modified);
                return true;
            }
            return false;
        }

        protected bool Patch<T>(T newValue, ref T storage, string propertyName) where T : IEquatable<T>
        {
            return Patch(newValue, ref storage, propertyName, true/*modified*/);
        }

        protected bool PatchObject<T>(object newValue, ref T storage, string propertyName, bool modified)
        {
            if (!object.Equals(newValue, storage))
            {
                storage = (T)newValue;
                Changed(propertyName, modified);
                return true;
            }
            return false;
        }

        protected bool PatchObject<T>(object newValue, ref T storage, string propertyName)
        {
            return PatchObject(newValue, ref storage, propertyName, true/*modified*/);
        }

        protected bool PatchReference<T>(object newValue, ref T storage, string propertyName, bool modified)
        {
            if (!(newValue == (object)storage))
            {
                storage = (T)newValue;
                Changed(propertyName, modified);
                return true;
            }
            return false;
        }

        protected bool PatchReference<T>(object newValue, ref T storage, string propertyName)
        {
            return PatchReference(newValue, ref storage, propertyName, true/*modified*/);
        }

        protected virtual void Changed(string propertyName, bool modified)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
            if (parent != null)
            {
                parent.NotifyFromChild(propertyNameInParent, modified);
            }
            if (modified)
            {
                SetModified();
            }
        }

        protected void Changed(string propertyName)
        {
            Changed(propertyName, true/*modified*/);
        }
    }

    public class HierarchicalBindingRoot : HierarchicalBindingBase
    {
        protected bool modified;

        public HierarchicalBindingRoot()
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


    //

    public class MyBindingList<T> : BindingList<T> where T : class
    {
        public int FindIndex(Predicate<T> predicate)
        {
            for (int i = 0; i < Count; i++)
            {
                if (predicate(this[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public T Find(Predicate<T> predicate)
        {
            int i = FindIndex(predicate);
            return i >= 0 ? this[i] : null;
        }
    }


    // 

    public class MyTrackList : MyBindingList<TrackObjectRec>
    {
        protected override void RemoveItem(int index)
        {
            DelinkBackgroundInstances(this[index]);
            base.RemoveItem(index);
        }

        private void DelinkBackgroundInstances(TrackObjectRec trackToRemove)
        {
            foreach (TrackObjectRec track in this)
            {
                if (track != trackToRemove)
                {
                    RemoveBackgroundObj(track, trackToRemove);
                }
            }
        }

        private void RemoveBackgroundObj(TrackObjectRec track, TrackObjectRec trackToRemove)
        {
            for (int i = 0; i < track.BackgroundObjects.Count; i++)
            {
                if (track.BackgroundObjects[i] == trackToRemove)
                {
                    track.BackgroundObjects.RemoveAt(i);
                    i--;
                }
            }
        }
    }


    //

    public partial class Document : HierarchicalBindingRoot
    {
        public const int CurrentFormatVersionNumber = 6; // 5 was last Macintosh version


        /* generic control parameters */

        private int _TabSize = Program.Config.TabSize;
        public const string TabSize_PropertyName = "TabSize";
        [Bindable(true)]
        public int TabSize { get { return _TabSize; } set { Patch(value, ref _TabSize, TabSize_PropertyName); } }

        private string _CommentInfo = String.Empty;
        public const string CommentInfo_PropertyName = "CommentInfo";
        [Bindable(true)]
        [Searchable]
        public string CommentInfo { get { return _CommentInfo; } set { Patch(value, ref _CommentInfo, CommentInfo_PropertyName); } }


        /* score data */

        private readonly MyBindingList<SampleObjectRec> _SampleList = new MyBindingList<SampleObjectRec>();
        public const string SampleList_PropertyName = "SampleList";
        [Bindable(true)]
        [Searchable]
        public MyBindingList<SampleObjectRec> SampleList { get { return _SampleList; } }

        private readonly MyBindingList<FunctionObjectRec> _FunctionList = new MyBindingList<FunctionObjectRec>();
        public const string FunctionList_PropertyName = "FunctionList";
        [Bindable(true)]
        [Searchable]
        public MyBindingList<FunctionObjectRec> FunctionList { get { return _FunctionList; } }

        private readonly MyBindingList<AlgoSampObjectRec> _AlgoSampList = new MyBindingList<AlgoSampObjectRec>();
        public const string AlgoSampList_PropertyName = "AlgoSampList";
        [Bindable(true)]
        [Searchable]
        public MyBindingList<AlgoSampObjectRec> AlgoSampList { get { return _AlgoSampList; } }

        private readonly MyBindingList<WaveTableObjectRec> _WaveTableList = new MyBindingList<WaveTableObjectRec>();
        public const string WaveTableList_PropertyName = "WaveTableList";
        [Bindable(true)]
        [Searchable]
        public MyBindingList<WaveTableObjectRec> WaveTableList { get { return _WaveTableList; } }

        private readonly MyBindingList<AlgoWaveTableObjectRec> _AlgoWaveTableList = new MyBindingList<AlgoWaveTableObjectRec>();
        public const string AlgoWaveTableList_PropertyName = "AlgoWaveTableList";
        [Bindable(true)]
        [Searchable]
        public MyBindingList<AlgoWaveTableObjectRec> AlgoWaveTableList { get { return _AlgoWaveTableList; } }

        public readonly MyBindingList<InstrObjectRec> _InstrumentList = new MyBindingList<InstrObjectRec>();
        public const string InstrumentList_PropertyName = "InstrumentList";
        [Bindable(true)]
        [Searchable]
        public MyBindingList<InstrObjectRec> InstrumentList { get { return _InstrumentList; } }

        public readonly MyBindingList<TrackObjectRec> _TrackList = new MyTrackList();
        public const string TrackList_PropertyName = "TrackList";
        [Bindable(true)]
        [Searchable]
        public MyBindingList<TrackObjectRec> TrackList { get { return _TrackList; } }


        /* playback preferences */

        private NumChannelsType _NumChannels = NumChannelsType.eSampleStereo;
        public const string NumChannels_PropertyName = "NumChannels";
        public static Enum[] NumChannelsAllowedValues { get { return EnumUtility.GetValues(NumChannelsType.eSampleMono.GetType()); } }
        [Bindable(true)]
        public NumChannelsType NumChannels { get { return _NumChannels; } set { PatchObject(value, ref _NumChannels, NumChannels_PropertyName); } }
        [Bindable(true)]
        public string NumChannelsAsString { get { return EnumUtility.GetDescription(_NumChannels); } set { string old = EnumUtility.GetDescription(_NumChannels); _NumChannels = (NumChannelsType)EnumUtility.GetValue(NumChannelsType.eSampleMono.GetType(), value); PatchObject(value, ref old, NumChannels_PropertyName); } }

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

        private LargeBCDType _DefaultBeatsPerMinute = (LargeBCDType)120;
        public const string DefaultBeatsPerMinute_PropertyName = "DefaultBeatsPerMinute";
        [Bindable(true)]
        public double DefaultBeatsPerMinute { get { return (double)_DefaultBeatsPerMinute; } set { double old = (double)_DefaultBeatsPerMinute; _DefaultBeatsPerMinute = (LargeBCDType)value; Patch(value, ref old, DefaultBeatsPerMinute_PropertyName); } }

        private LargeBCDType _OverallVolumeScalingFactor = (LargeBCDType)1;
        public const string OverallVolumeScalingFactor_PropertyName = "OverallVolumeScalingFactor";
        [Bindable(true)]
        public double OverallVolumeScalingFactor { get { return (double)_OverallVolumeScalingFactor; } set { double old = (double)_OverallVolumeScalingFactor; _OverallVolumeScalingFactor = (LargeBCDType)value; Patch(value, ref old, OverallVolumeScalingFactor_PropertyName); } }

        private NumBitsType _OutputNumBits = NumBitsType.eSample16bit;
        public const string OutputNumBits_PropertyName = "OutputNumBits";
        public static Enum[] OutputNumBitsAllowedValues { get { return EnumUtility.GetValues(NumBitsType.eSample8bit.GetType()); } }
        [Bindable(true)]
        public NumBitsType OutputNumBits { get { return _OutputNumBits; } set { PatchObject(value, ref _OutputNumBits, OutputNumBits_PropertyName); } }
        [Bindable(true)]
        public string OutputNumBitsAsString { get { return EnumUtility.GetDescription(_OutputNumBits); } set { string old = EnumUtility.GetDescription(_OutputNumBits); _OutputNumBits = (NumBitsType)EnumUtility.GetValue(NumBitsType.eSample8bit.GetType(), value); PatchObject(value, ref old, OutputNumBits_PropertyName); } }

        private LargeBCDType _ScanningGap = (LargeBCDType)2;
        public const string ScanningGap_PropertyName = "ScanningGap";
        [Bindable(true)]
        public double ScanningGap { get { return (double)_ScanningGap; } set { double old = (double)_ScanningGap; _ScanningGap = (LargeBCDType)value; Patch(value, ref old, ScanningGap_PropertyName); } }

        private LargeBCDType _BufferDuration = (LargeBCDType)2;
        public const string BufferDuration_PropertyName = "BufferDuration";
        [Bindable(true)]
        public double BufferDuration { get { return (double)_BufferDuration; } set { double old = (double)_BufferDuration; _BufferDuration = (LargeBCDType)value; Patch(value, ref old, BufferDuration_PropertyName); } }

        private bool _ClipWarning = true;
        public const string ClipWarning_PropertyName = "ClipWarning";
        [Bindable(true)]
        public bool ClipWarning { get { return _ClipWarning; } set { Patch(value, ref _ClipWarning, ClipWarning_PropertyName); } }

        private bool _ShowSummary;
        public const string ShowSummary_PropertyName = "ShowSummary";
        [Bindable(true)]
        public bool ShowSummary { get { return _ShowSummary; } set { Patch(value, ref _ShowSummary, ShowSummary_PropertyName); } }

        private bool _Deterministic = true;
        public const string Deterministic_PropertyName = "Deterministic";
        [Bindable(true)]
        public bool Deterministic { get { return _Deterministic; } set { Patch(value, ref _Deterministic, Deterministic_PropertyName); } }

        private int _Seed = 1;
        public const string Seed_PropertyName = "Seed";
        [Bindable(true)]
        public int Seed { get { return _Seed; } set { Patch(value, ref _Seed, Seed_PropertyName); } }


        public ScoreEffectsRec _ScoreEffects;
        public const string ScoreEffects_PropertyName = "ScoreEffects";
        [Bindable(true)]
        [Searchable]
        public ScoreEffectsRec ScoreEffects { get { return _ScoreEffects; } }


        private SequencerRec _Sequencer;
        public const string Sequencer_PropertyName = "Sequencer";
        [Bindable(true)]
        [Searchable]
        public SequencerRec Sequencer { get { return _Sequencer; } }


        private readonly MyBindingList<SectionObjectRec> _SectionList = new MyBindingList<SectionObjectRec>();
        public const string SectionList_PropertyName = "SectionList";
        [Bindable(true)]
        [Searchable]
        public MyBindingList<SectionObjectRec> SectionList { get { return _SectionList; } }


        private NewSchoolRec _NewSchool;
        public const string NewSchool_PropertyName = "NewSchool";
        [Bindable(true)]
        [Searchable]
        public NewSchoolRec NewSchool { get { return _NewSchool; } }


        // TODO: These values are not persisted, but perhaps should be

        private short _SavedWindowXLoc;
        public const string SavedWindowXLoc_PropertyName = "SavedWindowXLoc";
        [Bindable(true)]
        public short SavedWindowXLoc { get { return _SavedWindowXLoc; } set { Patch(value, ref _SavedWindowXLoc, SavedWindowXLoc_PropertyName, false/*modified*/); } }

        private short _SavedWindowYLoc;
        public const string SavedWindowYLoc_PropertyName = "SavedWindowYLoc";
        [Bindable(true)]
        public short SavedWindowYLoc { get { return _SavedWindowYLoc; } set { Patch(value, ref _SavedWindowYLoc, SavedWindowYLoc_PropertyName, false/*modified*/); } }

        private short _SavedWindowWidth;
        public const string SavedWindowWidth_PropertyName = "SavedWindowWidth";
        [Bindable(true)]
        public short SavedWindowWidth { get { return _SavedWindowWidth; } set { Patch(value, ref _SavedWindowWidth, SavedWindowWidth_PropertyName, false/*modified*/); } }

        private short _SavedWindowHeight;
        public const string SavedWindowHeight_PropertyName = "SavedWindowHeight";
        [Bindable(true)]
        public short SavedWindowHeight { get { return _SavedWindowHeight; } set { Patch(value, ref _SavedWindowHeight, SavedWindowHeight_PropertyName, false/*modified*/); } }


        // non-persisted properties

        public bool Modified { get { return modified; } set { modified = value; } }

        public readonly CodeCenterRec CodeCenter = new CodeCenterRec();

        public readonly FunctionBuilderProxy functionBuilderProxy;



        public Document()
        {
            _ScoreEffects = new ScoreEffectsRec(this);
            _Sequencer = new SequencerRec(this);
            _NewSchool = new NewSchoolRec(this);

            functionBuilderProxy = new FunctionBuilderProxy(this, _FunctionList);

            _SampleList.ListChanged += SomeListChanged;
            _FunctionList.ListChanged += SomeListChanged;
            _AlgoSampList.ListChanged += SomeListChanged;
            _WaveTableList.ListChanged += SomeListChanged;
            _AlgoWaveTableList.ListChanged += SomeListChanged;
            _InstrumentList.ListChanged += SomeListChanged;
            _TrackList.ListChanged += SomeListChanged;

            _SectionList.ListChanged += SomeListChanged;
        }

        public Document(string path)
            : this()
        {
            using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, Constants.BufferSize))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    Load(reader);
                }
            }
        }

        public Document(BinaryReader reader)
            : this()
        {
            Load(reader);
        }

        public static bool TryLoadDocument(string path, out Document document)
        {
            document = null;
            try
            {
                document = new Document(path);
                return true;
            }
            catch (FileNotFoundException)
            {
            }
            catch (InvalidDataException)
            {
                MessageBox.Show(String.Format("The file \"{0}\" does not appear to be an Out Of Phase document file.", path), "Out Of Phase");
            }
            catch (Exception exception)
            {
                MessageBox.Show(String.Format("Something failed while trying to load the document \"{0}\": {1}", path, exception), "Out Of Phase");
            }
            return false;
        }

        private void Load(BinaryReader reader)
        {
            short FormatVersionNumber;

            /*   4-byte file format version code */
            /*       "Syn1" - first file format */
            /*       "Syn2" - moved samples to end to help compression programs */
            /*       "Syn3" - miscellaneous changes to header */
            {
                string s = reader.ReadFixedStringASCII(4);
                if (String.Equals(s, "Syn1"))
                {
                    FormatVersionNumber = 1;
                }
                else if (String.Equals(s, "Syn2"))
                {
                    FormatVersionNumber = 2;
                }
                else if (String.Equals(s, "Syn3"))
                {
                    FormatVersionNumber = 3;
                }
                else
                {
                    throw new InvalidDataException();
                }
            }

            /*   1-byte unsigned header subblock version code */
            /*       should be 3, 4, or 5 */
            /*       ONLY for version 3 or greater; overrides Syn3 version format */
            if (FormatVersionNumber >= 3)
            {
                byte b = reader.ReadByte();
                if ((b >= 3) && (b <= 6))
                {
                    FormatVersionNumber = b;
                }
                else
                {
                    throw new InvalidDataException();
                }
            }


            LoadContext loadContext = new LoadContext(FormatVersionNumber, this);


            /*   1-byte unsigned tab size code */
            /*       should be in the range of 1..255 */
            _TabSize = Math.Min(Math.Max((int)reader.ReadByte(), Constants.MINTABCOUNT), Constants.MAXTABCOUNT);

            /*   4-byte little endian comment text length (in bytes) */
            /*   n-byte character data for comment text (line feed = 0x0a) */
            _CommentInfo = loadContext.Utf8 ? reader.ReadString4Utf8() : reader.ReadString4Ansi();

            /*   1-byte stereo playback flag */
            /*       0 = mono */
            /*       1 = stereo */
            switch (reader.ReadByte())
            {
                default:
                    throw new InvalidDataException();
                case 0:
                    _NumChannels = NumChannelsType.eSampleMono;
                    break;
                case 1:
                    _NumChannels = NumChannelsType.eSampleStereo;
                    break;
            }

            /*   1-byte surround encoding flag */
            /*       0 = no surround encoding */
            /*       1 = dolby surround encoding */
            /*       ONLY valid for versions 1 and 2 */
            if ((FormatVersionNumber == 1) || (FormatVersionNumber == 2))
            {
                reader.ReadByte(); // discard
                /* don't gratuitously reject if field has bad values */
            }

            /*   4-byte little endian output sampling rate */
            /*       should be in the range of 100..65535 */
            _SamplingRate = Math.Min(Math.Max(reader.ReadInt32(), Constants.MINSAMPLINGRATE), Constants.MAXSAMPLINGRATE);

            /*   4-byte little endian envelope update rate */
            /*       should be in the range of 1..65535 */
            _EnvelopeUpdateRate = Math.Min(Math.Max(reader.ReadInt32(), 1), Constants.MAXSAMPLINGRATE);

            /*   4-byte little endian large integer coded decimal beats per minute */
            /*       large integer coded decimal is decimal * 1000000 with a */
            /*       range of -1999.999999 to 1999.999999 */
            _DefaultBeatsPerMinute = reader.ReadLBCD();

            /*   4-byte little endian large integer coded total volume scaling factor */
            _OverallVolumeScalingFactor = reader.ReadLBCD();

            /*   1-byte number of bits to output */
            /*       for versions before 3:  should be 8, 16, 24, or 32 */
            /*       for version 3:  should be 8, 16, 24 */
            switch (reader.ReadByte())
            {
                default:
                    throw new InvalidDataException();
                case 8:
                    _OutputNumBits = NumBitsType.eSample8bit;
                    break;
                case 16:
                    _OutputNumBits = NumBitsType.eSample16bit;
                    break;
                case 24:
                    _OutputNumBits = NumBitsType.eSample24bit;
                    break;
                case 32:
                    if (FormatVersionNumber < 3)
                    {
                        _OutputNumBits = NumBitsType.eSample24bit;
                    }
                    else
                    {
                        throw new InvalidDataException();
                    }
                    break;
            }

            /*   1-byte oversampling */
            /*       should be in range [1..255].  this byte was previously used for something */
            /*       else, so invalid values should be silently corrected rather than flagged. */
            _Oversampling = Math.Min(Math.Max((int)reader.ReadByte(), Constants.MINOVERSAMPLING), Constants.MAXOVERSAMPLING);

            /*   1-byte undefined */
            reader.ReadByte();

            /*   4-byte little endian large integer coded decimal scanning gap */
            _ScanningGap = reader.ReadLBCD();

            /*   4-byte little endian large integer coded decimal buffer duration (in seconds) */
            _BufferDuration = reader.ReadLBCD();

            /*   1-byte flag for clipping warning */
            /*       0 = don't warn about clipped samples */
            /*       1 = do warn about clipped samples */
            switch (reader.ReadByte())
            {
                default:
                    throw new InvalidDataException();
                case 0:
                    _ClipWarning = false;
                    break;
                case 1:
                    _ClipWarning = true;
                    break;
            }

            /*   1-byte flag for song post processing enabling */
            /*       0 = don't do song postprocessing */
            /*       1 = do song postprocessing */
            /*       ONLY valid for versions 1 and 2 */
            if ((FormatVersionNumber == 1) || (FormatVersionNumber == 2))
            {
                reader.ReadByte(); // discard
                /* don't check value for correctness */
            }

            if (FormatVersionNumber >= 6)
            {
                // 1-byte flag for deterministic mode
                switch (reader.ReadByte())
                {
                    default:
                        throw new InvalidDataException();
                    case 0:
                        _Deterministic = false;
                        break;
                    case 1:
                        _Deterministic = true;
                        break;
                }
                // 4-byte int random number seed value
                _Seed = reader.ReadInt32();

                // n-bytes new features area
                _NewSchool = new NewSchoolRec(reader, loadContext);
            }

            _ScoreEffects = new ScoreEffectsRec(reader, loadContext);

            /*   n-byte section list */
            /*       ONLY valid for version 4 and later */
            if (FormatVersionNumber >= 4)
            {
                /*   4-byte little endian section count */
                int c = reader.ReadInt32();
                if (c < 0)
                {
                    throw new InvalidDataException();
                }

                /* read in all of the objects */
                for (int i = 0; i < c; i++)
                {
                    SectionObjectRec section = new SectionObjectRec(reader, loadContext);
                    _SectionList.Add(section);
                }
            }

            /*   n-byte sequencer config block */
            /*       ONLY valid for version 5 and later */
            if (FormatVersionNumber >= 5)
            {
                _Sequencer = new SequencerRec(reader, loadContext);
            }

            KeyValuePair<IList, CreateObject>[] ordering;
            if (FormatVersionNumber == 1)
            {
                /* old file organization */
                ordering = new KeyValuePair<IList, CreateObject>[]
                {
                    new KeyValuePair<IList, CreateObject>(_SampleList, SampleObjectRec.Create),
                    new KeyValuePair<IList, CreateObject>(_FunctionList, FunctionObjectRec.Create),
                    new KeyValuePair<IList, CreateObject>(_AlgoSampList, AlgoSampObjectRec.Create),
                    new KeyValuePair<IList, CreateObject>(_WaveTableList, WaveTableObjectRec.Create),
                    new KeyValuePair<IList, CreateObject>(_AlgoWaveTableList, AlgoWaveTableObjectRec.Create),
                    new KeyValuePair<IList, CreateObject>(_InstrumentList, InstrObjectRec.Create),
                    new KeyValuePair<IList, CreateObject>(_TrackList, TrackObjectRec.Create),
                };
            }
            else
            {
                /* new file organization, with samples moved to end to help */
                /* compression programs */
                ordering = new KeyValuePair<IList, CreateObject>[]
                {
                    new KeyValuePair<IList, CreateObject>(_TrackList, TrackObjectRec.Create),
                    new KeyValuePair<IList, CreateObject>(_FunctionList, FunctionObjectRec.Create),
                    new KeyValuePair<IList, CreateObject>(_AlgoSampList, AlgoSampObjectRec.Create),
                    new KeyValuePair<IList, CreateObject>(_AlgoWaveTableList, AlgoWaveTableObjectRec.Create),
                    new KeyValuePair<IList, CreateObject>(_InstrumentList, InstrObjectRec.Create),
                    new KeyValuePair<IList, CreateObject>(_WaveTableList, WaveTableObjectRec.Create),
                    new KeyValuePair<IList, CreateObject>(_SampleList, SampleObjectRec.Create),
                };
            }
            foreach (KeyValuePair<IList, CreateObject> item in ordering)
            {
                LoadList(reader, item.Key, loadContext, item.Value);

                // a bit hacky
                if (item.Key == _TrackList)
                {
                    /*   n-bytes data for background display information (a chunk for each track) */
                    for (int i = 0; i < _TrackList.Count; i++)
                    {
                        TrackObjectRec Track = _TrackList[i];
                        Track.LoadBackgroundTrackInfo(reader, _TrackList);
                    }
                }
            }

            reader.ReadEOF();


            // patching cross-reference data during load marks object modified - clear before returning
            Modified = false;
        }

        private delegate object CreateObject(BinaryReader reader, LoadContext loadContext);
        private static void LoadList(BinaryReader reader, IList list, LoadContext loadContext, CreateObject constructor)
        {
            // 4-byte little endian count
            int c = reader.ReadInt32();
            if (c < 0)
            {
                throw new InvalidDataException();
            }

            // n-bytes data
            for (int i = 0; i < c; i++)
            {
                list.Add(constructor(reader, loadContext));
            }
        }

        public void Save(BinaryWriter writer)
        {
            SaveContext saveContext = new SaveContext(this);

            /*   4-byte file format version code */
            /*       "Syn1" - first file format */
            /*       "Syn2" - moved samples to end to help compression programs */
            /*       "Syn3" - miscellaneous changes to header */
            writer.WriteFixedStringASCII(4, "Syn3");

            /*   1-byte unsigned header subblock version code */
            /*       should be 3, 4, 5, or 6 */
            /*       ONLY for version 3 or greater; overrides Syn3 version format */
            Debug.Assert(CurrentFormatVersionNumber == 6);
            writer.WriteByte(6);

            /*   1-byte unsigned tab size code */
            /*       should be in the range of 1..255 */
            writer.WriteByte((byte)_TabSize);

            /*   4-byte little endian comment text length (in bytes) */
            /*   n-byte character data for comment text (line feed = 0x0a) */
            writer.WriteString4Utf8(_CommentInfo);

            /*   1-byte stereo playback flag */
            /*       0 = mono */
            /*       1 = stereo */
            switch (_NumChannels)
            {
                default:
                    throw new ArgumentException();
                case NumChannelsType.eSampleMono:
                    writer.WriteByte(0);
                    break;
                case NumChannelsType.eSampleStereo:
                    writer.WriteByte(1);
                    break;
            }

            /*   4-byte little endian output sampling rate */
            /*       should be in the range of 100..65535 */
            writer.WriteInt32(_SamplingRate);

            /*   4-byte little endian envelope update rate */
            /*       should be in the range of 1..65535 */
            writer.WriteInt32(_EnvelopeUpdateRate);

            /*   4-byte little endian large integer coded decimal beats per minute */
            /*       large integer coded decimal is decimal * 1000000 with a */
            /*       range of -1999.999999 to 1999.999999 */
            writer.WriteLBCD(_DefaultBeatsPerMinute);

            /*   4-byte little endian large integer coded total volume scaling factor */
            writer.WriteLBCD(_OverallVolumeScalingFactor);

            /*   1-byte number of bits to output */
            /*       should be 8, 16, or 24 */
            switch (_OutputNumBits)
            {
                default:
                    throw new ArgumentException();
                case NumBitsType.eSample8bit:
                    writer.WriteByte((byte)8);
                    break;
                case NumBitsType.eSample16bit:
                    writer.WriteByte((byte)16);
                    break;
                case NumBitsType.eSample24bit:
                    writer.WriteByte((byte)24);
                    break;
            }

            /*   1-byte oversampling */
            /*       should be in range [1..255].  this byte was previously used for something */
            /*       else, so invalid values should be silently corrected rather than flagged. */
            writer.WriteByte((byte)_Oversampling);

            /*   1-byte undefined */
            writer.WriteByte(0);

            /*   4-byte little endian large integer coded decimal scanning gap */
            writer.WriteLBCD(_ScanningGap);

            /*   4-byte little endian large integer coded decimal buffer duration (in seconds) */
            writer.WriteLBCD(_BufferDuration);

            /*   1-byte flag for clipping warning */
            /*       0 = don't warn about clipped samples */
            /*       1 = do warn about clipped samples */
            writer.WriteByte(_ClipWarning ? (byte)1 : (byte)0);

            // 1-byte flag for deterministic mode
            writer.WriteByte(_Deterministic ? (byte)1 : (byte)0);
            // 4-byte int random number seed value
            writer.WriteInt32(_Seed);

            // n-bytes new features area
            _NewSchool.Save(writer, saveContext);

            _ScoreEffects.Save(writer, saveContext);

            /*   n-byte section list */
            /*       ONLY valid for version 4 and later */
            {
                /*   4-byte little endian section count */
                writer.WriteInt32(_SectionList.Count);

                /* write those little buggers out */
                for (int i = 0; i < _SectionList.Count; i++)
                {
                    _SectionList[i].Save(writer, saveContext);
                }
            }

            _Sequencer.Save(writer, saveContext);

            KeyValuePair<IList, SaveObject>[] ordering = new KeyValuePair<IList, SaveObject>[]
            {
                new KeyValuePair<IList, SaveObject>(_TrackList, TrackObjectRec.StaticSave),
                new KeyValuePair<IList, SaveObject>(_FunctionList, FunctionObjectRec.StaticSave),
                new KeyValuePair<IList, SaveObject>(_AlgoSampList, AlgoSampObjectRec.StaticSave),
                new KeyValuePair<IList, SaveObject>(_AlgoWaveTableList, AlgoWaveTableObjectRec.StaticSave),
                new KeyValuePair<IList, SaveObject>(_InstrumentList, InstrObjectRec.StaticSave),
                new KeyValuePair<IList, SaveObject>(_WaveTableList, WaveTableObjectRec.StaticSave),
                new KeyValuePair<IList, SaveObject>(_SampleList, SampleObjectRec.StaticSave),
            };
            foreach (KeyValuePair<IList, SaveObject> item in ordering)
            {
                SaveList(writer, item.Key, item.Value, saveContext);

                // a bit hacky
                if (item.Key == _TrackList)
                {
                    /*   n-bytes data for background display information (a chunk for each track) */
                    for (int i = 0; i < _TrackList.Count; i++)
                    {
                        TrackObjectRec Track = _TrackList[i];
                        Track.SaveBackgroundTrackInfo(writer, _TrackList);
                    }
                }
            }
        }

        private delegate void SaveObject(BinaryWriter writer, object o, SaveContext saveContext);
        private static void SaveList(BinaryWriter writer, IList list, SaveObject saver, SaveContext saveContext)
        {
            // 4-byte little endian count
            writer.WriteInt32(list.Count);

            // n-bytes data
            for (int i = 0; i < list.Count; i++)
            {
                saver(writer, list[i], saveContext);
            }
        }


        //

        protected override void Changed(string propertyName, bool modified)
        {
            BuildChangeNotify(propertyName);

            base.Changed(propertyName, modified);
        }

        private void SomeListChanged(object sender, ListChangedEventArgs e)
        {
            switch (e.ListChangedType)
            {
                default:
                    break;

                case ListChangedType.ItemAdded:
                case ListChangedType.ItemDeleted:
                case ListChangedType.ItemMoved:
                    // for changes to the *list*, mark modified
                    SetModified();
                    break;

                case ListChangedType.ItemChanged:
                    // item property changes are propagated by the item
                    break;
            }

            // do not notify - BindingList<> handles notifications to bound UI controls
        }

        public void RemoveFunction(FunctionObjectRec function)
        {
            FunctionList.Remove(function);
            function.Dispose(); // ensure CodeCenter gets notified to remoe compiled code object
        }

        public void RemoveSection(SectionObjectRec section)
        {
            foreach (TrackObjectRec track in TrackList)
            {
                if (track.Section == section)
                {
                    track.Section = null;
                }
            }
            SectionList.Remove(section);
        }
    }

    public enum LoadContextState
    {
        Load,
        Paste,
    }

    public sealed class LoadContext
    {
        public readonly int FormatVersionNumber;
        public readonly Document document;
        public readonly LoadContextState state;

        public LoadContext(int FormatVersionNumber, Document document)
        {
            this.FormatVersionNumber = FormatVersionNumber;
            this.document = document;
            this.state = LoadContextState.Load;
        }

        public LoadContext(int FormatVersionNumber, Document document, LoadContextState state)
        {
            this.FormatVersionNumber = FormatVersionNumber;
            this.document = document;
            this.state = state;
        }

        public bool Utf8 { get { return FormatVersionNumber >= 6; } }
    }

    public sealed class SaveContext
    {
        public readonly Document document;

        public SaveContext(Document document)
        {
            this.document = document;
        }
    }

    public class NewSchoolRec : HierarchicalBindingBase
    {
        public NewSchoolRec(Document document)
            : base(document, Document.NewSchool_PropertyName)
        {
        }

        private const int SubsectionFormatVersionNumber = 1;

        public NewSchoolRec(BinaryReader reader, LoadContext loadContext)
            : this(loadContext.document)
        {
            // 1-byte boolean - object is present [0 or 1]
            byte present = reader.ReadByte();
            if (present != 0)
            {
                Debug.Assert(false);
                throw new InvalidDataException();
            }
        }

        public static NewSchoolRec Create(BinaryReader reader, LoadContext loadContext)
        {
            return new NewSchoolRec(reader, loadContext);
        }

        public void Save(BinaryWriter writer, SaveContext saveContext)
        {
            // 1-byte boolean - object is present [0 or 1]
            writer.WriteByte(0);
        }
    }

    /* All data is stored as single precision float, for 8, 16, and 24 bit. */
    /* Values are not truncated to their bit depth here, allowing algorithmically */
    /* generated samples to retain full precision.  Processing on stored sample data */
    /* should apply truncation as the last step to prevent discrepancies in sound */
    /* quality after a save/reload. */
    /* Data is always stored normalized (-1 to 1). */
    /* NOTE: There is always an extra frame on the end to help interpolation. */
    public class SampleStorageActualRec
    {
        public readonly float[] Buffer;
        public readonly int NumFrames;
        public readonly NumBitsType NumBits;
        public readonly NumChannelsType NumChannels;

        public int PointsPerFrame { get { return NumChannels == NumChannelsType.eSampleStereo ? 2 : 1; } }
        public int NumPoints { get { return NumFrames * PointsPerFrame; } }

        public SampleStorageActualRec(int NumFrames, NumBitsType NumBits, NumChannelsType NumChannels)
        {
            int bufferLength = (NumFrames + 1) * (NumChannels == NumChannelsType.eSampleStereo ? 2 : 1);

            this.NumFrames = NumFrames;
            this.NumBits = NumBits;
            this.NumChannels = NumChannels;
            /* extra frame at end duplicates previous to make interpolation easier */
            this.Buffer = new float[bufferLength];
        }

        public SampleStorageActualRec(int NumFrames, NumBitsType NumBits, NumChannelsType NumChannels, float[] RawData)
        {
            int bufferLength = (NumFrames + 1) * (NumChannels == NumChannelsType.eSampleStereo ? 2 : 1);

            // since we adopt the array, require caller to allocate extra frame at end for interpolation
            if (RawData.Length != bufferLength)
            {
                throw new ArgumentException();
            }

            this.NumFrames = NumFrames;
            this.NumBits = NumBits;
            this.NumChannels = NumChannels;
            /* extra frame at end duplicates previous to make interpolation easier */
            this.Buffer = RawData;

            // Don't require caller to fill in extra frame correctly - we do it here
            EnsureExtraFrame();
        }

        public SampleStorageActualRec(SampleStorageActualRec orig)
        {
            this.NumFrames = orig.NumFrames;
            this.NumBits = orig.NumBits;
            this.NumChannels = orig.NumChannels;
            this.Buffer = (float[])orig.Buffer.Clone();
        }

        private void EnsureExtraFrame()
        {
            if (NumFrames > 0)
            {
                if (NumChannels == NumChannelsType.eSampleStereo)
                {
                    Buffer[2 * NumFrames + 0] = Buffer[2 * (NumFrames - 1) + 0];
                    Buffer[2 * NumFrames + 1] = Buffer[2 * (NumFrames - 1) + 1];
                }
                else
                {
                    Buffer[NumFrames] = Buffer[NumFrames - 1];
                }
            }
        }

        // for stereo, this exposes a single interleaved vector: left is [2*i+0], right is [2*i+1]
        public float this[int index]
        {
            get
            {
                if ((index < 0) || (index >= NumPoints))
                {
                    throw new ArgumentException();
                }
                return Buffer[index];
            }
            set
            {
                int c = NumPoints;
                if ((index < 0) || (index >= c))
                {
                    throw new ArgumentException();
                }
                Buffer[index] = value;
                if (NumChannels == NumChannelsType.eSampleStereo)
                {
                    // left/right channels interleaved
                    if (index >= c - 2)
                    {
                        Buffer[index + 2] = value;
                    }
                }
                else
                {
                    if (index == c - 1)
                    {
                        Buffer[index + 1] = value;
                    }
                }
            }
        }

        public float[] CopyRawData(ChannelType WhichChannel)
        {
            float[] result = new float[NumFrames];
            for (int i = 0; i < NumFrames; i += 1)
            {
                result[i] = GetValue(i, WhichChannel);
            }
            return result;
        }

        // unlike this[], this accessor abstracts away the stereo interleaved pattern
        public float GetValue(int Index, ChannelType WhichChannel)
        {
#if DEBUG
            if (((NumChannels == NumChannelsType.eSampleMono)
                    && (WhichChannel != ChannelType.eMonoChannel))
                || ((NumChannels == NumChannelsType.eSampleStereo)
                    && (WhichChannel != ChannelType.eLeftChannel)
                    && (WhichChannel != ChannelType.eRightChannel)))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif

            int FrameSize = (NumChannels == NumChannelsType.eSampleStereo ? 2 : 1);
            int i = Index * FrameSize + (WhichChannel == ChannelType.eRightChannel ? 1 : 0);
            return Buffer[i];
        }

        // unlike this[], this accessor abstracts away the stereo interleaved pattern
        public void SetValue(int Index, ChannelType WhichChannel, float NewValue)
        {
#if DEBUG
            if (((NumChannels == NumChannelsType.eSampleMono)
                    && (WhichChannel != ChannelType.eMonoChannel))
                || ((NumChannels == NumChannelsType.eSampleStereo)
                    && (WhichChannel != ChannelType.eLeftChannel)
                    && (WhichChannel != ChannelType.eRightChannel)))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif

            if (NewValue > 1)
            {
                NewValue = 1;
            }
            else if (NewValue < -1)
            {
                NewValue = -1;
            }

            int FrameSize = (NumChannels == NumChannelsType.eSampleStereo ? 2 : 1);
            int i = Index * FrameSize + (WhichChannel == ChannelType.eRightChannel ? 1 : 0);
            Buffer[i] = NewValue;
            if (Index == NumFrames - 1)
            {
                Buffer[i + FrameSize] = NewValue; /* interpolation helper */
            }
        }

        public SampleStorageActualRec Sub(int start, int count)
        {
            SampleStorageActualRec copy = new SampleStorageActualRec(count, NumBits, NumChannels);
            Array.Copy(this.Buffer, PointsPerFrame * start, copy.Buffer, 0, PointsPerFrame * count);
            copy.EnsureExtraFrame();
            return copy;
        }

        public SampleStorageActualRec Reduce(int start, int count)
        {
            SampleStorageActualRec copy = new SampleStorageActualRec(NumFrames - count, NumBits, NumChannels);
            Array.Copy(this.Buffer, 0, copy.Buffer, 0, PointsPerFrame * start);
            Array.Copy(this.Buffer, PointsPerFrame * (start + count), copy.Buffer, PointsPerFrame * start, PointsPerFrame * (NumFrames - (start + count)));
            copy.EnsureExtraFrame();
            return copy;
        }

        public SampleStorageActualRec Insert(float[] buffer, int start, int count)
        {
            SampleStorageActualRec copy = new SampleStorageActualRec(NumFrames + count, NumBits, NumChannels);
            Array.Copy(this.Buffer, 0, copy.Buffer, 0, PointsPerFrame * start);
            Array.Copy(buffer, 0, copy.Buffer, PointsPerFrame * start, PointsPerFrame * count);
            Array.Copy(this.Buffer, PointsPerFrame * start, copy.Buffer, PointsPerFrame * (start + count), PointsPerFrame * (NumFrames - start));
            copy.EnsureExtraFrame();
            return copy;
        }

        public SampleStorageActualRec ChangeChannels(NumChannelsType to)
        {
            if (to == NumChannels)
            {
                return new SampleStorageActualRec(this);
            }
            SampleStorageActualRec copy = new SampleStorageActualRec(NumFrames, NumBits, to);
            int c = NumFrames;
            if (to == NumChannelsType.eSampleStereo)
            {
                Debug.Assert(NumChannels == NumChannelsType.eSampleMono);
                for (int i = 0; i < c; i++)
                {
                    copy.Buffer[2 * i + 0] = copy.Buffer[2 * i + 1] = this.Buffer[i];
                }
            }
            else
            {
                Debug.Assert(NumChannels == NumChannelsType.eSampleStereo);
                for (int i = 0; i < c; i++)
                {
                    copy.Buffer[i] = .5f * (this.Buffer[2 * i + 0] + this.Buffer[2 * i + 0]);
                }
            }
            copy.EnsureExtraFrame();
            return copy;
        }

        public void SaveData(BinaryWriter writer)
        {
            /*   n-bytes of data for sample frames */
            /*       stereo samples have the left channel sample point preceding the right */
            /*       channel sample point. */
            /*       sample points that require more than 1 byte are stored little endian */
            /*       all sample data is stored in signed 2's complement form */
            int c = NumPoints;
            /* note: both the disk and in-memory stereo formats are interleaved */
            const int BlockSize = 4096;
            switch (NumBits)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case NumBitsType.eSample8bit:
                    byte[] b = new byte[BlockSize];
                    for (int j = 0; j < c; j += BlockSize)
                    {
                        int c1 = Math.Min(c - j, BlockSize);
                        for (int i = 0; i < c1; i++)
                        {
                            b[i] = SampConv.FloatToSignedByte(this[j + i]);
                        }
                        writer.WriteRaw(b, 0, c1);
                    }
                    break;
                case NumBitsType.eSample16bit:
                    short[] s = new short[BlockSize];
                    for (int j = 0; j < c; j += BlockSize)
                    {
                        int c1 = Math.Min(c - j, BlockSize);
                        for (int i = 0; i < c1; i++)
                        {
                            s[i] = SampConv.FloatToSignedShort(this[j + i]);
                        }
                        writer.WriteInt16s(s, 0, c1);
                    }
                    break;
                case NumBitsType.eSample24bit:
                    for (int i = 0; i < c; i++)
                    {
                        writer.WriteInt24(SampConv.FloatToSignedTribyte(this[i]));
                    }
                    break;
            }
        }

        public void Save(BinaryWriter writer)
        {
            // n-byte variable length integer - number of frames
            writer.WriteUInt32Delta((uint)NumFrames);
            // 1-byte number of channels [1 or 2]
            switch (NumChannels)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case NumChannelsType.eSampleStereo:
                    writer.WriteByte(2);
                    break;
                case NumChannelsType.eSampleMono:
                    writer.WriteByte(1);
                    break;
            }
            // 1 byte number of bits [8, 16, or 24]
            switch (NumBits)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case NumBitsType.eSample8bit:
                    writer.WriteByte(8);
                    break;
                case NumBitsType.eSample16bit:
                    writer.WriteByte(16);
                    break;
                case NumBitsType.eSample24bit:
                    writer.WriteByte(24);
                    break;
            }
            // n-bytes raw data
            SaveData(writer);
        }

        public static SampleStorageActualRec LoadData(BinaryReader reader, int NumFrames, NumBitsType NumBits, NumChannelsType NumChannels)
        {
            /*   n-bytes of data for sample frames */
            /*       stereo samples have the left channel sample point preceding the right */
            /*       channel sample point. */
            /*       sample points that require more than 1 byte are stored little endian */
            /*       all sample data is stored in signed 2's complement form */
            SampleStorageActualRec storage = new SampleStorageActualRec(NumFrames, NumBits, NumChannels);
            /* note: both the disk and in-memory stereo formats are interleaved */
            int c = storage.NumPoints;
            const int BlockSize = 4096;
            switch (NumBits)
            {
                default:
                    throw new InvalidDataException();
                case NumBitsType.eSample8bit:
                    for (int j = 0; j < c; j += BlockSize)
                    {
                        int c1 = Math.Min(c - j, BlockSize);
                        byte[] b = reader.ReadBytes(c1);
                        for (int i = 0; i < c1; i++)
                        {
                            storage[j + i] = SampConv.SignedByteToFloat(b[i]);
                        }
                    }
                    break;
                case NumBitsType.eSample16bit:
                    for (int j = 0; j < c; j += BlockSize)
                    {
                        int c1 = Math.Min(c - j, BlockSize);
                        short[] s = reader.ReadInt16s(c1);
                        for (int i = 0; i < c1; i++)
                        {
                            storage[j + i] = SampConv.SignedShortToFloat(s[i]);
                        }
                    }
                    break;
                case NumBitsType.eSample24bit:
                    for (int i = 0; i < c; i++)
                    {
                        storage[i] = SampConv.SignedTribyteToFloat(reader.ReadInt24());
                    }
                    break;
            }
            return storage;
        }

        public static SampleStorageActualRec Load(BinaryReader reader)
        {
            // n-byte variable length integer - number of frames
            int numFrames = (int)reader.ReadUInt32Delta();
            // 1-byte number of channels [1 or 2]
            NumChannelsType numChannels;
            switch (reader.ReadByte())
            {
                default:
                    throw new InvalidDataException();
                case 2:
                    numChannels = NumChannelsType.eSampleStereo;
                    break;
                case 1:
                    numChannels = NumChannelsType.eSampleMono;
                    break;
            }
            // 1 byte number of bits [8, 16, or 24]
            NumBitsType numBits;
            switch (reader.ReadByte())
            {
                default:
                    throw new InvalidDataException();
                case 8:
                    numBits = NumBitsType.eSample8bit;
                    break;
                case 16:
                    numBits = NumBitsType.eSample16bit;
                    break;
                case 24:
                    numBits = NumBitsType.eSample24bit;
                    break;
            }
            // n-bytes raw data
            return LoadData(reader, numFrames, numBits, numChannels);
        }
    }

    public class SampleObjectRec : HierarchicalBindingBase
    {
        private string _Name = String.Empty;
        public const string Name_PropertyName = "Name";
        [Bindable(true)]
        [Searchable]
        public string Name { get { return _Name; } set { Patch(value, ref _Name, Name_PropertyName); } }


        private SampleStorageActualRec _Actual = new SampleStorageActualRec(0, NumBitsType.eSample24bit, NumChannelsType.eSampleMono);
        public const string SampleData_PropertyName = "SampleData";
        public SampleStorageActualRec SampleData
        {
            get { return _Actual; }
            set
            {
                SampleStorageActualRec old = _Actual;
                PatchReference(value, ref _Actual, SampleData_PropertyName);
                if (SampleDataChanged != null)
                {
                    SampleDataChanged.Invoke(this, new SampleStorageEventArgs(_Actual));
                }
                int f = old.NumFrames;
                Patch(_Actual.NumFrames, ref f, NumFrames_PropertyName);
                NumBitsType b = old.NumBits;
                PatchObject(_Actual.NumBits, ref b, NumBits_PropertyName);
                NumChannelsType c = old.NumChannels;
                PatchObject(_Actual.NumChannels, ref c, NumChannels_PropertyName);
            }
        }
        public class SampleStorageEventArgs : EventArgs
        {
            public readonly SampleStorageActualRec SampleData;
            public SampleStorageEventArgs(SampleStorageActualRec SampleData)
            {
                this.SampleData = SampleData;
            }
        }
        public delegate void SampleStorageEventHandler(object sender, SampleStorageEventArgs e);
        public event SampleStorageEventHandler SampleDataChanged;

        public const string NumBits_PropertyName = "NumBits";
        public static Enum[] NumBitsAllowedValues { get { return EnumUtility.GetValues(NumBitsType.eSample8bit.GetType()); } }
        [Bindable(true)]
        public NumBitsType NumBits { get { return _Actual.NumBits; } /* set { PatchObject(value, ref Actual.NumBits, NumBits_PropertyName); } */ }
        public string NumBitsAsString { get { return EnumUtility.GetDescription(_Actual.NumBits); } /* set { string old = EnumDescription.GetDescription(Actual.NumBits); Actual.NumBits = (NumBitsType)EnumDescription.GetValue(NumBitsType.eSample8bit, value); PatchObject(value, ref old, NumBits_PropertyName); } */ }

        public const string NumChannels_PropertyName = "NumChannels";
        public static Enum[] NumChannelsAllowedValues { get { return EnumUtility.GetValues(NumChannelsType.eSampleMono.GetType()); } }
        [Bindable(true)]
        public NumChannelsType NumChannels { get { return _Actual.NumChannels; } /* set { PatchObject(value, ref Actual.NumChannels, NumChannels_PropertyName); } */ }
        public string NumChannelsAsString { get { return EnumUtility.GetDescription(_Actual.NumChannels); } /* set { string old = EnumDescription.GetDescription(Actual.NumChannels); Actual.NumChannels = (NumChannelsType)EnumDescription.GetValue(NumChannelsType.eSampleMono, value); PatchObject(value, ref old, NumChannels_PropertyName); } */ }

        public const string NumFrames_PropertyName = "NumFrames";
        [Bindable(true)]
        public int NumFrames { get { return _Actual.NumFrames; } /* set { Patch(value, ref Actual.NumFrames, NumFrames_PropertyName); } */ }


        private string _SampleFormula =
            "# This expression applies to the ENTIRE sample." + Environment.NewLine +
            "# loopstart[1-3], loopend[1-3], origin, samplingrate, selectstart,  selectend : int" + Environment.NewLine +
            "# loopbidir[1-3] : bool" + Environment.NewLine +
            "# naturalfrequency : double; [leftdata, rightdata | data] : fixedarray" + Environment.NewLine;
        public const string SampleFormula_PropertyName = "SampleFormula";
        [Bindable(true)]
        [Searchable]
        public string SampleFormula { get { return _SampleFormula; } set { Patch(value, ref _SampleFormula, SampleFormula_PropertyName); } }

        private int _Origin;
        public const string Origin_PropertyName = "Origin";
        [Bindable(true)]
        public int Origin { get { return _Origin; } set { Patch(value, ref _Origin, Origin_PropertyName); } }

        private int _LoopStart1;
        public const string LoopStart1_PropertyName = "LoopStart1";
        [Bindable(true)]
        public int LoopStart1 { get { return _LoopStart1; } set { Patch(value, ref _LoopStart1, LoopStart1_PropertyName); } }

        private int _LoopStart2;
        public const string LoopStart2_PropertyName = "LoopStart2";
        [Bindable(true)]
        public int LoopStart2 { get { return _LoopStart2; } set { Patch(value, ref _LoopStart2, LoopStart2_PropertyName); } }

        private int _LoopStart3;
        public const string LoopStart3_PropertyName = "LoopStart3";
        [Bindable(true)]
        public int LoopStart3 { get { return _LoopStart3; } set { Patch(value, ref _LoopStart3, LoopStart3_PropertyName); } }

        private int _LoopEnd1;
        public const string LoopEnd1_PropertyName = "LoopEnd1";
        [Bindable(true)]
        public int LoopEnd1 { get { return _LoopEnd1; } set { Patch(value, ref _LoopEnd1, LoopEnd1_PropertyName); } }

        private int _LoopEnd2;
        public const string LoopEnd2_PropertyName = "LoopEnd2";
        [Bindable(true)]
        public int LoopEnd2 { get { return _LoopEnd2; } set { Patch(value, ref _LoopEnd2, LoopEnd2_PropertyName); } }

        private int _LoopEnd3;
        public const string LoopEnd3_PropertyName = "LoopEnd3";
        [Bindable(true)]
        public int LoopEnd3 { get { return _LoopEnd3; } set { Patch(value, ref _LoopEnd3, LoopEnd3_PropertyName); } }

        public static Enum[] LoopBidirectionalAllowedValues { get { return EnumUtility.GetValues(LoopBidirectionalType.No.GetType()); } }

        private LoopBidirectionalType _Loop1Bidirectional;
        public const string Loop1Bidirectional_PropertyName = "Loop1Bidirectional";
        [Bindable(true)]
        public LoopBidirectionalType Loop1Bidirectional { get { return _Loop1Bidirectional; } set { PatchObject(value, ref _Loop1Bidirectional, Loop1Bidirectional_PropertyName); } }
        [Bindable(true)]
        public string Loop1BidirectionalAsString { get { return EnumUtility.GetDescription(_Loop1Bidirectional); } set { string old = EnumUtility.GetDescription(_Loop1Bidirectional); _Loop1Bidirectional = (LoopBidirectionalType)EnumUtility.GetValue(LoopBidirectionalType.No.GetType(), value); PatchObject(value, ref old, Loop1Bidirectional_PropertyName); } }

        private LoopBidirectionalType _Loop2Bidirectional;
        public const string Loop2Bidirectional_PropertyName = "Loop2Bidirectional";
        [Bindable(true)]
        public LoopBidirectionalType Loop2Bidirectional { get { return _Loop2Bidirectional; } set { PatchObject(value, ref _Loop2Bidirectional, Loop2Bidirectional_PropertyName); } }
        [Bindable(true)]
        public string Loop2BidirectionalAsString { get { return EnumUtility.GetDescription(_Loop2Bidirectional); } set { string old = EnumUtility.GetDescription(_Loop2Bidirectional); _Loop2Bidirectional = (LoopBidirectionalType)EnumUtility.GetValue(LoopBidirectionalType.No.GetType(), value); PatchObject(value, ref old, Loop2Bidirectional_PropertyName); } }

        private LoopBidirectionalType _Loop3Bidirectional;
        public const string Loop3Bidirectional_PropertyName = "Loop3Bidirectional";
        [Bindable(true)]
        public LoopBidirectionalType Loop3Bidirectional { get { return _Loop3Bidirectional; } set { PatchObject(value, ref _Loop3Bidirectional, Loop3Bidirectional_PropertyName); } }
        [Bindable(true)]
        public string Loop3BidirectionalAsString { get { return EnumUtility.GetDescription(_Loop3Bidirectional); } set { string old = EnumUtility.GetDescription(_Loop3Bidirectional); _Loop3Bidirectional = (LoopBidirectionalType)EnumUtility.GetValue(LoopBidirectionalType.No.GetType(), value); PatchObject(value, ref old, Loop3Bidirectional_PropertyName); } }

        private int _SamplingRate = 44100;
        public const string SamplingRate_PropertyName = "SamplingRate";
        [Bindable(true)]
        public int SamplingRate { get { return _SamplingRate; } set { Patch(value, ref _SamplingRate, SamplingRate_PropertyName); } }

        private double _NaturalFrequency = Constants.MIDDLEC;
        public const string NaturalFrequency_PropertyName = "NaturalFrequency";
        [Bindable(true)]
        public double NaturalFrequency { get { return _NaturalFrequency; } set { Patch(value, ref _NaturalFrequency, NaturalFrequency_PropertyName); } }
        public string NaturalFrequencyAsString
        {
            get
            {
                return _NaturalFrequency.ToString();
            }
            set
            {
                double freq;
                if (Double.TryParse(value, out freq))
                {
                    NaturalFrequency = freq;
                }
                else
                {
                    short Pitch = Constants.CENTERNOTE;
                    NoteFlags SharpFlatThing = 0;
                    SymbolicPitch.StringToNumericPitch(value, ref Pitch, ref SharpFlatThing);
                    NaturalFrequency = Math.Exp(((double)(Pitch - Constants.CENTERNOTE) / 12) * Constants.LOG2) * Constants.MIDDLEC;
                }
            }
        }


        private short _SavedWindowXLoc;
        public const string SavedWindowXLoc_PropertyName = "SavedWindowXLoc";
        [Bindable(true)]
        public short SavedWindowXLoc { get { return _SavedWindowXLoc; } set { Patch(value, ref _SavedWindowXLoc, SavedWindowXLoc_PropertyName, false/*modified*/); } }

        private short _SavedWindowYLoc;
        public const string SavedWindowYLoc_PropertyName = "SavedWindowYLoc";
        [Bindable(true)]
        public short SavedWindowYLoc { get { return _SavedWindowYLoc; } set { Patch(value, ref _SavedWindowYLoc, SavedWindowYLoc_PropertyName, false/*modified*/); } }

        private short _SavedWindowWidth;
        public const string SavedWindowWidth_PropertyName = "SavedWindowWidth";
        [Bindable(true)]
        public short SavedWindowWidth { get { return _SavedWindowWidth; } set { Patch(value, ref _SavedWindowWidth, SavedWindowWidth_PropertyName, false/*modified*/); } }

        private short _SavedWindowHeight;
        public const string SavedWindowHeight_PropertyName = "SavedWindowHeight";
        [Bindable(true)]
        public short SavedWindowHeight { get { return _SavedWindowHeight; } set { Patch(value, ref _SavedWindowHeight, SavedWindowHeight_PropertyName, false/*modified*/); } }


        public SampleObjectRec(Document document)
            : base(document, Document.SampleList_PropertyName)
        {
        }

        public SampleObjectRec(
            Document document,
            float[] RawData,
            int NumFrames,
            NumBitsType NumBits,
            NumChannelsType NumChannels,
            int Origin,
            int LoopStart1,
            int LoopStart2,
            int LoopStart3,
            int LoopEnd1,
            int LoopEnd2,
            int LoopEnd3,
            int SamplingRate,
            double NaturalFrequency)
            : base(document, Document.SampleList_PropertyName)
        {
            this._Origin = Origin;
            this._LoopStart1 = LoopStart1;
            this._LoopStart2 = LoopStart2;
            this._LoopStart3 = LoopStart3;
            this._LoopEnd1 = LoopEnd1;
            this._LoopEnd2 = LoopEnd2;
            this._LoopEnd3 = LoopEnd3;
            this._SamplingRate = SamplingRate;
            this._NaturalFrequency = NaturalFrequency;
            this._Actual = new SampleStorageActualRec(NumFrames, NumBits, NumChannels, RawData);
        }

        public SampleObjectRec(Document document, SampleObjectRec orig)
            : base(document, Document.SampleList_PropertyName)
        {
            this._Name = orig._Name;
            this._Actual = new SampleStorageActualRec(orig._Actual);
            this._SampleFormula = orig._SampleFormula;
            this._Origin = orig._Origin;
            this._LoopStart1 = orig._LoopStart1;
            this._LoopStart2 = orig._LoopStart2;
            this._LoopStart3 = orig._LoopStart3;
            this._LoopEnd1 = orig._LoopEnd1;
            this._LoopEnd2 = orig._LoopEnd2;
            this._LoopEnd3 = orig._LoopEnd3;
            this._Loop1Bidirectional = orig._Loop1Bidirectional;
            this._Loop2Bidirectional = orig._Loop2Bidirectional;
            this._Loop3Bidirectional = orig._Loop3Bidirectional;
            this._SamplingRate = orig._SamplingRate;
            this._NaturalFrequency = orig._NaturalFrequency;
            this._SavedWindowXLoc = orig._SavedWindowXLoc;
            this._SavedWindowYLoc = orig._SavedWindowYLoc;
            this._SavedWindowWidth = orig._SavedWindowWidth;
            this._SavedWindowHeight = orig._SavedWindowHeight;
        }

        public SampleObjectRec(BinaryReader reader, LoadContext loadContext)
            : this(loadContext.document)
        {
            /*   1-byte sample version number */
            /*       should be 1 or 2 */
            int FormatVersionNumber = reader.ReadByte();
            if ((FormatVersionNumber != 1) && (FormatVersionNumber != 2))
            {
                throw new InvalidDataException();
            }

            /*   2-byte little endian window X position (signed, origin at top-left corner) */
            SavedWindowXLoc = reader.ReadInt16();
            /*   2-byte little endian window Y position */
            SavedWindowYLoc = reader.ReadInt16();
            /*   2-byte little endian window width */
            SavedWindowWidth = reader.ReadInt16();
            /*   2-byte little endian window height */
            SavedWindowHeight = reader.ReadInt16();

            /*   4-byte little endian sample name length descriptor (positive 2's complement) */
            /*   n-byte sample name text (line feed = 0x0a) */
            _Name = loadContext.Utf8 ? reader.ReadString4Utf8() : reader.ReadString4Ansi();

            /*   4-byte little endian sample formula length descriptor (positive 2's complement) */
            /*   n-byte sample formula text (line feed = 0x0a) */
            _SampleFormula = loadContext.Utf8 ? reader.ReadString4Utf8() : reader.ReadString4Ansi();

            /*   4-byte little endian sample frame index of sample's origin */
            _Origin = reader.ReadInt32();

            /*   4-byte little endian sample frame index of loop 1 start */
            _LoopStart1 = reader.ReadInt32();

            /*   4-byte little endian sample frame index of loop 1 end */
            _LoopEnd1 = reader.ReadInt32();

            /*   4-byte little endian sample frame index of loop 2 start */
            _LoopStart2 = reader.ReadInt32();

            /*   4-byte little endian sample frame index of loop 2 end */
            _LoopEnd2 = reader.ReadInt32();

            /*   4-byte little endian sample frame index of loop 3 start */
            _LoopStart3 = reader.ReadInt32();

            /*   4-byte little endian sample frame index of loop 3 end */
            _LoopEnd3 = reader.ReadInt32();

            /*   4-byte little endian sampling rate value */
            _SamplingRate = Math.Min(Math.Max(reader.ReadInt32(), Constants.MINSAMPLINGRATE), Constants.MAXSAMPLINGRATE);

            /*   4-byte little endian natural frequency fractional portion */
            /*       unsigned; divide by 2^32 to get the actual fraction */
            {
                uint u = reader.ReadUInt32();
                _NaturalFrequency = u / 4294967296.0;
            }
            /*   4-byte little endian natural frequency integer portion */
            {
                int i = reader.ReadInt32();
                _NaturalFrequency += i;
            }
            _NaturalFrequency = Math.Min(Math.Max(_NaturalFrequency, Constants.MINNATURALFREQ), Constants.MAXNATURALFREQ);

            /*   4-byte total number of sample frames */
            int NumFrames = reader.ReadInt32();
            if (NumFrames < 0)
            {
                throw new InvalidDataException();
            }

            /*   1-byte mono/stereo flag */
            /*       1 = mono */
            /*       2 = stereo */
            NumChannelsType NumChannels;
            switch (reader.ReadByte())
            {
                default:
                    throw new InvalidDataException();
                case 2:
                    NumChannels = NumChannelsType.eSampleStereo;
                    break;
                case 1:
                    NumChannels = NumChannelsType.eSampleMono;
                    break;
            }

            /*   1-byte number of bits per sample point */
            /*       should be 8, 16, or 24 */
            NumBitsType NumBits;
            switch (reader.ReadByte())
            {
                default:
                    throw new InvalidDataException();
                case 8:
                    NumBits = NumBitsType.eSample8bit;
                    break;
                case 16:
                    NumBits = NumBitsType.eSample16bit;
                    break;
                case 24:
                    NumBits = NumBitsType.eSample24bit;
                    break;
            }

            if (FormatVersionNumber >= 2)
            {
                /*   1-byte loop 1 bidirectionality flag */
                /*       0 = unidirectional */
                /*       1 = bidirectional */
                /*       ONLY for version 2 or greater */
                switch (reader.ReadByte())
                {
                    default:
                        throw new InvalidDataException();
                    case 0:
                        _Loop1Bidirectional = LoopBidirectionalType.No;
                        break;
                    case 1:
                        _Loop1Bidirectional = LoopBidirectionalType.Yes;
                        break;
                }

                /*   1-byte loop 2 bidirectionality flag */
                /*       0 = unidirectional */
                /*       1 = bidirectional */
                /*       ONLY for version 2 or greater */
                switch (reader.ReadByte())
                {
                    default:
                        throw new InvalidDataException();
                    case 0:
                        _Loop2Bidirectional = LoopBidirectionalType.No;
                        break;
                    case 1:
                        _Loop2Bidirectional = LoopBidirectionalType.Yes;
                        break;
                }

                /*   1-byte loop 3 bidirectionality flag */
                /*       0 = unidirectional */
                /*       1 = bidirectional */
                /*       ONLY for version 2 or greater */
                switch (reader.ReadByte())
                {
                    default:
                        throw new InvalidDataException();
                    case 0:
                        _Loop3Bidirectional = LoopBidirectionalType.No;
                        break;
                    case 1:
                        _Loop3Bidirectional = LoopBidirectionalType.Yes;
                        break;
                }
            }

            /*   n-bytes of data for sample frames */
            /*       stereo samples have the left channel sample point preceding the right */
            /*       channel sample point. */
            /*       sample points that require more than 1 byte are stored little endian */
            /*       all sample data is stored in signed 2's complement form */
            _Actual = SampleStorageActualRec.LoadData(reader, NumFrames, NumBits, NumChannels);
        }

        public static SampleObjectRec Create(BinaryReader reader, LoadContext loadContext)
        {
            return new SampleObjectRec(reader, loadContext);
        }

        public void Save(BinaryWriter writer, SaveContext saveContext)
        {
            /*   1-byte sample version number */
            /*       should be [1 or] 2*/
            writer.WriteByte(2);

            /*   2-byte little endian window X position (signed, origin at top-left corner) */
            writer.WriteInt16(SavedWindowXLoc);
            /*   2-byte little endian window Y position */
            writer.WriteInt16(SavedWindowYLoc);
            /*   2-byte little endian window width */
            writer.WriteInt16(SavedWindowWidth);
            /*   2-byte little endian window height */
            writer.WriteInt16(SavedWindowHeight);

            /*   4-byte little endian sample name length descriptor (positive 2's complement) */
            /*   n-byte sample name text (line feed = 0x0a) */
            writer.WriteString4Utf8(_Name);

            /*   4-byte little endian sample formula length descriptor (positive 2's complement) */
            /*   n-byte sample formula text (line feed = 0x0a) */
            writer.WriteString4Utf8(_SampleFormula);

            /*   4-byte little endian sample frame index of sample's origin */
            writer.WriteInt32(_Origin);

            /*   4-byte little endian sample frame index of loop 1 start */
            writer.WriteInt32(_LoopStart1);

            /*   4-byte little endian sample frame index of loop 1 end */
            writer.WriteInt32(_LoopEnd1);

            /*   4-byte little endian sample frame index of loop 2 start */
            writer.WriteInt32(_LoopStart2);

            /*   4-byte little endian sample frame index of loop 2 end */
            writer.WriteInt32(_LoopEnd2);

            /*   4-byte little endian sample frame index of loop 3 start */
            writer.WriteInt32(_LoopStart3);

            /*   4-byte little endian sample frame index of loop 3 end */
            writer.WriteInt32(_LoopEnd3);

            /*   4-byte little endian sampling rate value */
            writer.WriteInt32(_SamplingRate);

            /*   4-byte little endian natural frequency fractional portion */
            /*       unsigned; divide by 2^32 to get the actual fraction */
            writer.WriteUInt32((uint)((_NaturalFrequency - Math.Floor(_NaturalFrequency)) * 4294967296.0));
            /*   4-byte little endian natural frequency integer portion */
            writer.WriteInt32((int)Math.Floor(_NaturalFrequency));

            /*   4-byte total number of sample frames */
            writer.WriteInt32(_Actual.NumFrames);

            /*   1-byte mono/stereo flag */
            /*       1 = mono */
            /*       2 = stereo */
            switch (_Actual.NumChannels)
            {
                default:
                    throw new ArgumentException();
                case NumChannelsType.eSampleStereo:
                    writer.WriteByte(2);
                    break;
                case NumChannelsType.eSampleMono:
                    writer.WriteByte(1);
                    break;
            }

            /*   1-byte number of bits per sample point */
            /*       should be 8, 16, or 24 */
            switch (_Actual.NumBits)
            {
                default:
                    throw new ArgumentException();
                case NumBitsType.eSample8bit:
                    writer.WriteByte(8);
                    break;
                case NumBitsType.eSample16bit:
                    writer.WriteByte(16);
                    break;
                case NumBitsType.eSample24bit:
                    writer.WriteByte(24);
                    break;
            }

            /*   1-byte loop 1 bidirectionality flag */
            /*       0 = unidirectional */
            /*       1 = bidirectional */
            /*       ONLY for version 2 or greater */
            switch (_Loop1Bidirectional)
            {
                default:
                    throw new ArgumentException();
                case LoopBidirectionalType.No:
                    writer.WriteByte(0);
                    break;
                case LoopBidirectionalType.Yes:
                    writer.WriteByte(1);
                    break;
            }

            /*   1-byte loop 2 bidirectionality flag */
            /*       0 = unidirectional */
            /*       1 = bidirectional */
            /*       ONLY for version 2 or greater */
            switch (_Loop2Bidirectional)
            {
                default:
                    throw new ArgumentException();
                case LoopBidirectionalType.No:
                    writer.WriteByte(0);
                    break;
                case LoopBidirectionalType.Yes:
                    writer.WriteByte(1);
                    break;
            }

            /*   1-byte loop 3 bidirectionality flag */
            /*       0 = unidirectional */
            /*       1 = bidirectional */
            /*       ONLY for version 2 or greater */
            switch (_Loop3Bidirectional)
            {
                default:
                    throw new ArgumentException();
                case LoopBidirectionalType.No:
                    writer.WriteByte(0);
                    break;
                case LoopBidirectionalType.Yes:
                    writer.WriteByte(1);
                    break;
            }

            /*   n-bytes of data for sample frames */
            /*       stereo samples have the left channel sample point preceding the right */
            /*       channel sample point. */
            /*       sample points that require more than 1 byte are stored little endian */
            /*       all sample data is stored in signed 2's complement form */
            _Actual.SaveData(writer);
        }

        public static void StaticSave(BinaryWriter writer, object o, SaveContext saveContext)
        {
            ((SampleObjectRec)o).Save(writer, saveContext);
        }

        private delegate int GetMethod();
        private delegate void SetMethod(int v);
        private static void ShiftPointsHelper(int Position, int NumAddedFrames, GetMethod get, SetMethod set)
        {
            int Temp = get();
            if (Temp >= Position)
            {
                Temp += NumAddedFrames;
                if (Temp < Position)
                {
                    Temp = Position;
                }
                set(Temp);
            }
        }

        /* this routine updates the loop points when some number of sample frames are */
        /* inserted or removed from the specified point. (for removal, NumAddedFrames < 0) */
        public void ShiftPoints(int Position, int NumAddedFrames)
        {
            ShiftPointsHelper(Position, NumAddedFrames, delegate () { return Origin; }, delegate (int value) { Origin = value; });
            ShiftPointsHelper(Position, NumAddedFrames, delegate () { return LoopStart1; }, delegate (int value) { LoopStart1 = value; });
            ShiftPointsHelper(Position, NumAddedFrames, delegate () { return LoopStart2; }, delegate (int value) { LoopStart2 = value; });
            ShiftPointsHelper(Position, NumAddedFrames, delegate () { return LoopStart3; }, delegate (int value) { LoopStart3 = value; });
            ShiftPointsHelper(Position, NumAddedFrames, delegate () { return LoopEnd1; }, delegate (int value) { LoopEnd1 = value; });
            ShiftPointsHelper(Position, NumAddedFrames, delegate () { return LoopEnd2; }, delegate (int value) { LoopEnd2 = value; });
            ShiftPointsHelper(Position, NumAddedFrames, delegate () { return LoopEnd3; }, delegate (int value) { LoopEnd3 = value; });
        }
    }

    public partial class FunctionObjectRec : HierarchicalBindingBuildable
    {
        private string _Source = String.Empty;
        public const string Source_PropertyName = "Source";
        [Bindable(true)]
        [Searchable]
        public string Source { get { return _Source; } set { Patch(value, ref _Source, Source_PropertyName); } }

        private string _Name = String.Empty;
        public const string Name_PropertyName = "Name";
        [Bindable(true)]
        [Searchable]
        public string Name { get { return _Name; } set { Patch(value, ref _Name, Name_PropertyName); } }


        private short _SavedWindowXLoc;
        public const string SavedWindowXLoc_PropertyName = "SavedWindowXLoc";
        [Bindable(true)]
        public short SavedWindowXLoc { get { return _SavedWindowXLoc; } set { Patch(value, ref _SavedWindowXLoc, SavedWindowXLoc_PropertyName, false/*modified*/); } }

        private short _SavedWindowYLoc;
        public const string SavedWindowYLoc_PropertyName = "SavedWindowYLoc";
        [Bindable(true)]
        public short SavedWindowYLoc { get { return _SavedWindowYLoc; } set { Patch(value, ref _SavedWindowYLoc, SavedWindowYLoc_PropertyName, false/*modified*/); } }

        private short _SavedWindowWidth;
        public const string SavedWindowWidth_PropertyName = "SavedWindowWidth";
        [Bindable(true)]
        public short SavedWindowWidth { get { return _SavedWindowWidth; } set { Patch(value, ref _SavedWindowWidth, SavedWindowWidth_PropertyName, false/*modified*/); } }

        private short _SavedWindowHeight;
        public const string SavedWindowHeight_PropertyName = "SavedWindowHeight";
        [Bindable(true)]
        public short SavedWindowHeight { get { return _SavedWindowHeight; } set { Patch(value, ref _SavedWindowHeight, SavedWindowHeight_PropertyName, false/*modified*/); } }


        // nonpersisted state

        private CodeCenterRec codeCenter;

        private FunctionBuilderProxy functionBuilderProxy;


        public FunctionObjectRec(Document document)
            : base(document, Document.FunctionList_PropertyName)
        {
            codeCenter = document.CodeCenter;
            functionBuilderProxy = document.functionBuilderProxy;
        }

        public FunctionObjectRec(BinaryReader reader, LoadContext loadContext)
            : this(loadContext.document)
        {
            /*   1-byte function object version number */
            /*       should be 1 */
            if (reader.ReadByte() != 1)
            {
                throw new InvalidDataException();
            }

            /*   2-byte little endian window x location (origin at top-left of screen) */
            _SavedWindowXLoc = reader.ReadInt16();
            /*   2-byte little endian window y location */
            _SavedWindowYLoc = reader.ReadInt16();
            /*   2-byte little endian window width */
            _SavedWindowWidth = reader.ReadInt16();
            /*   2-byte little endian window height */
            _SavedWindowHeight = reader.ReadInt16();

            /*   4-byte little endian object name length */
            /*   n-byte name data (line feed = 0x0a) */
            _Name = loadContext.Utf8 ? reader.ReadString4Utf8() : reader.ReadString4Ansi();

            /*   4-byte little endian function source text length */
            /*   n-byte function source text data (line feed = 0x0a) */
            _Source = loadContext.Utf8 ? reader.ReadString4Utf8() : reader.ReadString4Ansi();
        }

        public static FunctionObjectRec Create(BinaryReader reader, LoadContext loadContext)
        {
            return new FunctionObjectRec(reader, loadContext);
        }

        public void Save(BinaryWriter writer, SaveContext saveContext)
        {
            /*   1-byte function object version number */
            /*       should be 1 */
            writer.WriteByte(1);

            /*   2-byte little endian window x location (origin at top-left of screen) */
            writer.WriteInt16(_SavedWindowXLoc);
            /*   2-byte little endian window y location */
            writer.WriteInt16(_SavedWindowYLoc);
            /*   2-byte little endian window width */
            writer.WriteInt16(_SavedWindowWidth);
            /*   2-byte little endian window height */
            writer.WriteInt16(_SavedWindowHeight);

            /*   4-byte little endian object name length */
            /*   n-byte name data (line feed = 0x0a) */
            writer.WriteString4Utf8(_Name);

            /*   4-byte little endian function source text length */
            /*   n-byte function source text data (line feed = 0x0a) */
            writer.WriteString4Utf8(_Source);
        }

        public static void StaticSave(BinaryWriter writer, object o, SaveContext saveContext)
        {
            ((FunctionObjectRec)o).Save(writer, saveContext);
        }
    }

    public partial class AlgoSampObjectRec : HierarchicalBindingBuildable
    {
        private string _Name = String.Empty;
        public const string Name_PropertyName = "Name";
        [Bindable(true)]
        [Searchable]
        public string Name { get { return _Name; } set { Patch(value, ref _Name, Name_PropertyName); } }

        private string _AlgoSampFormula =
            "# samplingrate, origin, loopstart1, loopstart2, loopstart3 : integer" + Environment.NewLine +
            "# loopend1, loopend2, loopend3 : integer" + Environment.NewLine +
            "# naturalfrequency : double; [data | leftdata, rightdata] : fixedarray" + Environment.NewLine;
        public const string AlgoSampFormula_PropertyName = "AlgoSampFormula";
        [Bindable(true)]
        [Searchable]
        public string AlgoSampFormula { get { return _AlgoSampFormula; } set { Patch(value, ref _AlgoSampFormula, AlgoSampFormula_PropertyName); } }

        private NumChannelsType _NumChannels = NumChannelsType.eSampleMono;
        public const string NumChannels_PropertyName = "NumChannels";
        public static Enum[] NumChannelsAllowedValues { get { return EnumUtility.GetValues(NumChannelsType.eSampleMono.GetType()); } }
        [Bindable(true)]
        public NumChannelsType NumChannels { get { return _NumChannels; } set { PatchObject(value, ref _NumChannels, NumChannels_PropertyName); } }
        [Bindable(true)]
        public string NumChannelsAsString { get { return EnumUtility.GetDescription(_NumChannels); } set { string old = EnumUtility.GetDescription(_NumChannels); _NumChannels = (NumChannelsType)EnumUtility.GetValue(NumChannelsType.eSampleMono.GetType(), value); PatchObject(value, ref old, NumChannels_PropertyName); } }

        private int _Origin;
        public const string Origin_PropertyName = "Origin";
        [Bindable(true)]
        public int Origin { get { return _Origin; } set { Patch(value, ref _Origin, Origin_PropertyName); } }

        private int _LoopStart1;
        public const string LoopStart1_PropertyName = "LoopStart1";
        [Bindable(true)]
        public int LoopStart1 { get { return _LoopStart1; } set { Patch(value, ref _LoopStart1, LoopStart1_PropertyName); } }

        private int _LoopStart2;
        public const string LoopStart2_PropertyName = "LoopStart2";
        [Bindable(true)]
        public int LoopStart2 { get { return _LoopStart2; } set { Patch(value, ref _LoopStart2, LoopStart2_PropertyName); } }

        private int _LoopStart3;
        public const string LoopStart3_PropertyName = "LoopStart3";
        [Bindable(true)]
        public int LoopStart3 { get { return _LoopStart3; } set { Patch(value, ref _LoopStart3, LoopStart3_PropertyName); } }

        private int _LoopEnd1;
        public const string LoopEnd1_PropertyName = "LoopEnd1";
        [Bindable(true)]
        public int LoopEnd1 { get { return _LoopEnd1; } set { Patch(value, ref _LoopEnd1, LoopEnd1_PropertyName); } }

        private int _LoopEnd2;
        public const string LoopEnd2_PropertyName = "LoopEnd2";
        [Bindable(true)]
        public int LoopEnd2 { get { return _LoopEnd2; } set { Patch(value, ref _LoopEnd2, LoopEnd2_PropertyName); } }

        private int _LoopEnd3;
        public const string LoopEnd3_PropertyName = "LoopEnd3";
        [Bindable(true)]
        public int LoopEnd3 { get { return _LoopEnd3; } set { Patch(value, ref _LoopEnd3, LoopEnd3_PropertyName); } }

        public static Enum[] LoopBidirectionalAllowedValues { get { return EnumUtility.GetValues(LoopBidirectionalType.No.GetType()); } }

        private LoopBidirectionalType _Loop1Bidirectional;
        public const string Loop1Bidirectional_PropertyName = "Loop1Bidirectional";
        [Bindable(true)]
        public LoopBidirectionalType Loop1Bidirectional { get { return _Loop1Bidirectional; } set { PatchObject(value, ref _Loop1Bidirectional, Loop1Bidirectional_PropertyName); } }
        [Bindable(true)]
        public string Loop1BidirectionalAsString { get { return EnumUtility.GetDescription(_Loop1Bidirectional); } set { string old = EnumUtility.GetDescription(_Loop1Bidirectional); _Loop1Bidirectional = (LoopBidirectionalType)EnumUtility.GetValue(LoopBidirectionalType.No.GetType(), value); PatchObject(value, ref old, Loop1Bidirectional_PropertyName); } }

        private LoopBidirectionalType _Loop2Bidirectional;
        public const string Loop2Bidirectional_PropertyName = "Loop2Bidirectional";
        [Bindable(true)]
        public LoopBidirectionalType Loop2Bidirectional { get { return _Loop2Bidirectional; } set { PatchObject(value, ref _Loop2Bidirectional, Loop2Bidirectional_PropertyName); } }
        [Bindable(true)]
        public string Loop2BidirectionalAsString { get { return EnumUtility.GetDescription(_Loop2Bidirectional); } set { string old = EnumUtility.GetDescription(_Loop2Bidirectional); _Loop2Bidirectional = (LoopBidirectionalType)EnumUtility.GetValue(LoopBidirectionalType.No.GetType(), value); PatchObject(value, ref old, Loop2Bidirectional_PropertyName); } }

        private LoopBidirectionalType _Loop3Bidirectional;
        public const string Loop3Bidirectional_PropertyName = "Loop3Bidirectional";
        [Bindable(true)]
        public LoopBidirectionalType Loop3Bidirectional { get { return _Loop3Bidirectional; } set { PatchObject(value, ref _Loop3Bidirectional, Loop3Bidirectional_PropertyName); } }
        [Bindable(true)]
        public string Loop3BidirectionalAsString { get { return EnumUtility.GetDescription(_Loop3Bidirectional); } set { string old = EnumUtility.GetDescription(_Loop3Bidirectional); _Loop3Bidirectional = (LoopBidirectionalType)EnumUtility.GetValue(LoopBidirectionalType.No.GetType(), value); PatchObject(value, ref old, Loop3Bidirectional_PropertyName); } }

        private int _SamplingRate = 44100;
        public const string SamplingRate_PropertyName = "SamplingRate";
        [Bindable(true)]
        public int SamplingRate { get { return _SamplingRate; } set { Patch(value, ref _SamplingRate, SamplingRate_PropertyName); } }

        private double _NaturalFrequency = Constants.MIDDLEC;
        public const string NaturalFrequency_PropertyName = "NaturalFrequency";
        [Bindable(true)]
        public double NaturalFrequency { get { return _NaturalFrequency; } set { Patch(value, ref _NaturalFrequency, NaturalFrequency_PropertyName); } }
        public string NaturalFrequencyAsString
        {
            get
            {
                return _NaturalFrequency.ToString();
            }
            set
            {
                double freq;
                if (Double.TryParse(value, out freq))
                {
                    NaturalFrequency = freq;
                }
                else
                {
                    short Pitch = Constants.CENTERNOTE;
                    NoteFlags SharpFlatThing = 0;
                    SymbolicPitch.StringToNumericPitch(value, ref Pitch, ref SharpFlatThing);
                    NaturalFrequency = Math.Exp(((double)(Pitch - Constants.CENTERNOTE) / 12) * Constants.LOG2) * Constants.MIDDLEC;
                }
            }
        }

        private short _SavedWindowXLoc;
        public const string SavedWindowXLoc_PropertyName = "SavedWindowXLoc";
        [Bindable(true)]
        public short SavedWindowXLoc { get { return _SavedWindowXLoc; } set { Patch(value, ref _SavedWindowXLoc, SavedWindowXLoc_PropertyName, false/*modified*/); } }

        private short _SavedWindowYLoc;
        public const string SavedWindowYLoc_PropertyName = "SavedWindowYLoc";
        [Bindable(true)]
        public short SavedWindowYLoc { get { return _SavedWindowYLoc; } set { Patch(value, ref _SavedWindowYLoc, SavedWindowYLoc_PropertyName, false/*modified*/); } }

        private short _SavedWindowWidth;
        public const string SavedWindowWidth_PropertyName = "SavedWindowWidth";
        [Bindable(true)]
        public short SavedWindowWidth { get { return _SavedWindowWidth; } set { Patch(value, ref _SavedWindowWidth, SavedWindowWidth_PropertyName, false/*modified*/); } }

        private short _SavedWindowHeight;
        public const string SavedWindowHeight_PropertyName = "SavedWindowHeight";
        [Bindable(true)]
        public short SavedWindowHeight { get { return _SavedWindowHeight; } set { Patch(value, ref _SavedWindowHeight, SavedWindowHeight_PropertyName, false/*modified*/); } }


        public AlgoSampObjectRec(Document document)
            : base(document, Document.AlgoSampList_PropertyName)
        {
        }

        public AlgoSampObjectRec(BinaryReader reader, LoadContext loadContext)
            : this(loadContext.document)
        {
            /*   1-byte format version number */
            /*       should be 1 or 2 */
            int FormatVersionNumber = reader.ReadByte();
            if ((FormatVersionNumber != 1) && (FormatVersionNumber != 2))
            {
                throw new InvalidDataException();
            }

            /*   2-bytes little endian window X location (signed, origin at top-left corner) */
            _SavedWindowXLoc = reader.ReadInt16();
            /*   2-bytes little endian window Y location */
            _SavedWindowYLoc = reader.ReadInt16();
            /*   2-bytes little endian window width */
            _SavedWindowWidth = reader.ReadInt16();
            /*   2-bytes little endian window height */
            _SavedWindowHeight = reader.ReadInt16();

            /*   4-bytes little endian name length descriptor */
            /*   n-bytes name string (line feed = 0x0a) */
            Name = loadContext.Utf8 ? reader.ReadString4Utf8() : reader.ReadString4Ansi();

            /*   4-bytes little endian formula length descriptor */
            /*   n-bytes formula string (line feed = 0x0a) */
            _AlgoSampFormula = loadContext.Utf8 ? reader.ReadString4Utf8() : reader.ReadString4Ansi();

            /*   1-byte number of bits */
            /*       should be 8, 16, or 24 */
            switch (reader.ReadByte())
            {
                default:
                    throw new InvalidDataException();
                case 8:
                case 16:
                case 24:
                    // not used - now always floading point
                    break;
            }

            /*   1-byte number of channels */
            /*       1 = mono */
            /*       2 = stereo */
            switch (reader.ReadByte())
            {
                default:
                    throw new InvalidDataException();
                case 1:
                    _NumChannels = NumChannelsType.eSampleMono;
                    break;
                case 2:
                    _NumChannels = NumChannelsType.eSampleStereo;
                    break;
            }

            /*   4-bytes little endian sample origin */
            _Origin = reader.ReadInt32();

            /*   4-bytes little endian loop 1 start point */
            _LoopStart1 = reader.ReadInt32();

            /*   4-bytes little endian loop 1 end point */
            _LoopEnd1 = reader.ReadInt32();

            /*   4-bytes little endian loop 2 start point */
            _LoopStart2 = reader.ReadInt32();

            /*   4-bytes little endian loop 2 end point */
            _LoopEnd2 = reader.ReadInt32();

            /*   4-bytes little endian loop 3 start point */
            _LoopStart3 = reader.ReadInt32();

            /*   4-bytes little endian loop 3 end point */
            _LoopEnd3 = reader.ReadInt32();

            /*   4-bytes little endian sampling rate */
            _SamplingRate = Math.Min(Math.Max(reader.ReadInt32(), Constants.MINSAMPLINGRATE), Constants.MAXSAMPLINGRATE);

            /*   4-byte little endian natural frequency fractional portion */
            /*       unsigned; divide by 2^32 to get the actual fraction */
            {
                uint u = reader.ReadUInt32();
                _NaturalFrequency = u / 4294967296.0;
            }
            /*   4-byte little endian natural frequency integer portion */
            {
                int i = reader.ReadInt32();
                _NaturalFrequency += i;
            }
            _NaturalFrequency = Math.Min(Math.Max(_NaturalFrequency, Constants.MINNATURALFREQ), Constants.MAXNATURALFREQ);

            if (FormatVersionNumber >= 2)
            {
                /*   1-byte loop 1 bidirectionality flag */
                /*       0 = unidirectional */
                /*       1 = bidirectional */
                /*       ONLY for version 2 or greater */
                switch (reader.ReadByte())
                {
                    default:
                        throw new InvalidDataException();
                    case 0:
                        _Loop1Bidirectional = LoopBidirectionalType.No;
                        break;
                    case 1:
                        _Loop1Bidirectional = LoopBidirectionalType.Yes;
                        break;
                }

                /*   1-byte loop 2 bidirectionality flag */
                /*       0 = unidirectional */
                /*       1 = bidirectional */
                /*       ONLY for version 2 or greater */
                switch (reader.ReadByte())
                {
                    default:
                        throw new InvalidDataException();
                    case 0:
                        _Loop2Bidirectional = LoopBidirectionalType.No;
                        break;
                    case 1:
                        _Loop2Bidirectional = LoopBidirectionalType.Yes;
                        break;
                }

                /*   1-byte loop 3 bidirectionality flag */
                /*       0 = unidirectional */
                /*       1 = bidirectional */
                /*       ONLY for version 2 or greater */
                switch (reader.ReadByte())
                {
                    default:
                        throw new InvalidDataException();
                    case 0:
                        _Loop3Bidirectional = LoopBidirectionalType.No;
                        break;
                    case 1:
                        _Loop3Bidirectional = LoopBidirectionalType.Yes;
                        break;
                }
            }
        }

        public static AlgoSampObjectRec Create(BinaryReader reader, LoadContext loadContext)
        {
            return new AlgoSampObjectRec(reader, loadContext);
        }

        public void Save(BinaryWriter writer, SaveContext saveContext)
        {
            /*   1-byte format version number */
            /*       should be [1 or] 2 */
            writer.WriteByte(2);

            /*   2-bytes little endian window X location (signed, origin at top-left corner) */
            writer.WriteInt16(_SavedWindowXLoc);
            /*   2-bytes little endian window Y location */
            writer.WriteInt16(_SavedWindowYLoc);
            /*   2-bytes little endian window width */
            writer.WriteInt16(_SavedWindowWidth);
            /*   2-bytes little endian window height */
            writer.WriteInt16(_SavedWindowHeight);

            /*   4-bytes little endian name length descriptor */
            /*   n-bytes name string (line feed = 0x0a) */
            writer.WriteString4Utf8(Name);

            /*   4-bytes little endian formula length descriptor */
            /*   n-bytes formula string (line feed = 0x0a) */
            writer.WriteString4Utf8(_AlgoSampFormula);

            /*   1-byte number of bits */
            /*       should be 8, 16, or 24 */
            writer.WriteByte(24);

            /*   1-byte number of channels */
            /*       1 = mono */
            /*       2 = stereo */
            switch (_NumChannels)
            {
                default:
                    throw new ArgumentException();
                case NumChannelsType.eSampleMono:
                    writer.WriteByte(1);
                    break;
                case NumChannelsType.eSampleStereo:
                    writer.WriteByte(2);
                    break;
            }

            /*   4-bytes little endian sample origin */
            writer.WriteInt32(_Origin);

            /*   4-bytes little endian loop 1 start point */
            writer.WriteInt32(_LoopStart1);

            /*   4-bytes little endian loop 1 end point */
            writer.WriteInt32(_LoopEnd1);

            /*   4-bytes little endian loop 2 start point */
            writer.WriteInt32(_LoopStart2);

            /*   4-bytes little endian loop 2 end point */
            writer.WriteInt32(_LoopEnd2);

            /*   4-bytes little endian loop 3 start point */
            writer.WriteInt32(_LoopStart3);

            /*   4-bytes little endian loop 3 end point */
            writer.WriteInt32(_LoopEnd3);

            /*   4-bytes little endian sampling rate */
            /*       should be between 100 and 192000 */
            writer.WriteInt32(_SamplingRate);

            /*   4-byte little endian natural frequency fractional portion */
            /*       unsigned; divide by 2^32 to get the actual fraction */
            writer.WriteUInt32((uint)((_NaturalFrequency - Math.Floor(_NaturalFrequency)) * 4294967296.0));
            /*   4-byte little endian natural frequency integer portion */
            /*       total natural frequency should be between 0.01 and 1e6 */
            writer.WriteInt32((int)Math.Floor(_NaturalFrequency));

            /*   1-byte loop 1 bidirectionality flag */
            /*       0 = unidirectional */
            /*       1 = bidirectional */
            /*       ONLY for version 2 or greater */
            switch (_Loop1Bidirectional)
            {
                default:
                    throw new ArgumentException();
                case LoopBidirectionalType.No:
                    writer.WriteByte(0);
                    break;
                case LoopBidirectionalType.Yes:
                    writer.WriteByte(1);
                    break;
            }

            /*   1-byte loop 2 bidirectionality flag */
            /*       0 = unidirectional */
            /*       1 = bidirectional */
            /*       ONLY for version 2 or greater */
            switch (_Loop2Bidirectional)
            {
                default:
                    throw new ArgumentException();
                case LoopBidirectionalType.No:
                    writer.WriteByte(0);
                    break;
                case LoopBidirectionalType.Yes:
                    writer.WriteByte(1);
                    break;
            }

            /*   1-byte loop 3 bidirectionality flag */
            /*       0 = unidirectional */
            /*       1 = bidirectional */
            /*       ONLY for version 2 or greater */
            switch (_Loop3Bidirectional)
            {
                default:
                    throw new ArgumentException();
                case LoopBidirectionalType.No:
                    writer.WriteByte(0);
                    break;
                case LoopBidirectionalType.Yes:
                    writer.WriteByte(1);
                    break;
            }
        }

        public static void StaticSave(BinaryWriter writer, object o, SaveContext saveContext)
        {
            ((AlgoSampObjectRec)o).Save(writer, saveContext);
        }
    }

    /* All data is stored as single precision float, for 8, 16, and 24 bit. */
    /* Values are not truncated to their bit depth here, allowing algorithmically */
    /* generated samples to retain full precision.  Processing on stored sample data */
    /* should apply truncation as the last step to prevent discrepancies in sound */
    /* quality after a save/reload. */
    /* Data is always stored normalized (-1 to 1). */
    /* NOTE: There is always an extra frame on the end of each table to help interpolation. */
    public class WaveTableStorageRec
    {
        public readonly Table[] ListOfTables;
        public readonly int NumFrames;
        public readonly NumBitsType NumBits;
        public int NumTables { get { return ListOfTables.Length; } }

        public static int[] NumFramesAllowedValues
        {
            get
            {
                int[] values = new int[16];
                for (int i = 1; i <= 16; i++)
                {
                    values[i - 1] = 1 << i;
                }
                return values;
            }
        }

        public static Enum[] NumBitsAllowedValues { get { return EnumUtility.GetValues(NumBitsType.eSample8bit.GetType()); } }

        public struct Table
        {
            public readonly float[] frames;

            public Table(int NumFrames)
            {
                frames = new float[NumFrames + 1];
            }

            public Table(Table orig)
            {
                frames = (float[])orig.frames.Clone();
            }

            public float this[int index]
            {
                get
                {
                    return frames[index];
                }
                set
                {
                    frames[index] = value;
                    if (index == 0)
                    {
                        frames[frames.Length - 1] = value; /* anti-aliasing loopback value */
                    }
                }
            }
        }

        public WaveTableStorageRec(int NumTables, int NumFrames, NumBitsType NumBits)
        {
            if (((NumFrames & (NumFrames - 1)) != 0) || (NumFrames < 2) || (NumFrames > 65536))
            {
                throw new ArgumentException();
            }

            this.NumFrames = NumFrames;
            this.NumBits = NumBits;
            this.ListOfTables = new Table[NumTables];
            for (int i = 0; i < ListOfTables.Length; i++)
            {
                ListOfTables[i] = new Table(NumFrames);
            }
        }

        public WaveTableStorageRec(WaveTableStorageRec orig)
        {
            this.NumFrames = orig.NumFrames;
            this.NumBits = orig.NumBits;
            this.ListOfTables = new Table[orig.ListOfTables.Length];
            for (int i = 0; i < this.ListOfTables.Length; i++)
            {
                this.ListOfTables[i] = new Table(orig.ListOfTables[i]);
            }
        }

        public WaveTableStorageRec(int NumTables, int NumFrames, NumBitsType NumBits, float[] RawCopy)
        {
            if (NumTables * NumFrames != RawCopy.Length)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            this.NumFrames = NumFrames;
            this.NumBits = NumBits;
            this.ListOfTables = new Table[NumTables];
            for (int i = 0; i < this.ListOfTables.Length; i++)
            {
                this.ListOfTables[i] = new Table(NumFrames);
                Array.Copy(RawCopy, i * NumFrames, this.ListOfTables[i].frames, 0, NumFrames);
                this.ListOfTables[i][NumFrames - 1] = this.ListOfTables[i][NumFrames - 1]; // force update extra frame at end
            }
        }

        public WaveTableStorageRec(WaveTableStorageRec orig, Table[] tables)
        {
            this.NumFrames = orig.NumFrames;
            this.NumBits = orig.NumBits;
            this.ListOfTables = tables;
        }


        public float[] GetRawCopy()
        {
            float[] Array = new float[NumTables * NumFrames];
            for (int i = 0; i < NumTables; i++)
            {
                Table table = ListOfTables[i];
                for (int j = 0; j < NumFrames; j++)
                {
                    Array[i * NumFrames + j] = table[j];
                }
            }
            return Array;
        }

        public static WaveTableStorageRec InsertTable(WaveTableStorageRec orig, int index)
        {
            List<Table> tables = new List<Table>();
            for (int i = 0; i < orig.NumTables; i++)
            {
                if (i == index)
                {
                    tables.Add(new Table(orig.NumFrames));
                }
                tables.Add(orig.ListOfTables[i]);
            }
            if (orig.NumTables == index)
            {
                tables.Add(new Table(orig.NumFrames));
            }
            return new WaveTableStorageRec(orig, tables.ToArray());
        }

        public static WaveTableStorageRec DeleteTable(WaveTableStorageRec orig, int index)
        {
            List<Table> tables = new List<Table>();
            for (int i = 0; i < orig.NumTables; i++)
            {
                if (i != index)
                {
                    tables.Add(orig.ListOfTables[i]);
                }
            }
            return new WaveTableStorageRec(orig, tables.ToArray());
        }

        public void TruncateBits() // ensure bit depth is respected
        {
            foreach (Table table in ListOfTables)
            {
                for (int i = 0; i < table.frames.Length; i++)
                {
                    float v = table.frames[i];
                    float v2;
                    switch (NumBits)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();
                        case NumBitsType.eSample8bit:
                            v2 = SampConv.SignedByteToFloat(SampConv.FloatToSignedByte(v));
                            Debug.Assert(v2 == SampConv.SignedByteToFloat(SampConv.FloatToSignedByte(v2))); // verify conversion idempotency (i.e. stability)
                            break;
                        case NumBitsType.eSample16bit:
                            v2 = SampConv.SignedShortToFloat(SampConv.FloatToSignedShort(v));
                            Debug.Assert(v2 == SampConv.SignedShortToFloat(SampConv.FloatToSignedShort(v))); // verify conversion idempotency (i.e. stability)
                            break;
                        case NumBitsType.eSample24bit:
                            v2 = SampConv.SignedTribyteToFloat(SampConv.FloatToSignedTribyte(v));
                            Debug.Assert(v2 == SampConv.SignedTribyteToFloat(SampConv.FloatToSignedTribyte(v))); // verify conversion idempotency (i.e. stability)
                            break;
                    }
                    table.frames[i] = v2;
                }
            }
        }
    }

    public class WaveTableObjectRec : HierarchicalBindingBase
    {
        private string _Name = String.Empty;
        public const string Name_PropertyName = "Name";
        [Bindable(true)]
        [Searchable]
        public string Name { get { return _Name; } set { Patch(value, ref _Name, Name_PropertyName); } }


        private WaveTableStorageRec _WaveTableData = new WaveTableStorageRec(0, 4096, NumBitsType.eSample24bit);
        public const string WaveTableData_PropertyName = "WaveTableData";
        public WaveTableStorageRec WaveTableData
        {
            get { return _WaveTableData; }
            set
            {
                WaveTableStorageRec old = _WaveTableData;
                PatchReference(value, ref _WaveTableData, WaveTableData_PropertyName);
                if (WaveTableDataChanged != null)
                {
                    WaveTableDataChanged.Invoke(this, new WaveTableStorageEventArgs(_WaveTableData));
                }
                int t = old.NumTables;
                Patch(_WaveTableData.NumTables, ref t, NumTables_PropertyName);
                t = old.NumFrames;
                Patch(_WaveTableData.NumFrames, ref t, NumFrames_PropertyName);
                NumBitsType b = old.NumBits;
                PatchObject(_WaveTableData.NumBits, ref b, NumBits_PropertyName);
            }
        }
        public class WaveTableStorageEventArgs : EventArgs
        {
            public readonly WaveTableStorageRec WaveTableData;
            public WaveTableStorageEventArgs(WaveTableStorageRec WaveTableData)
            {
                this.WaveTableData = WaveTableData;
            }
        }
        public delegate void WaveTableStorageEventHandler(object sender, WaveTableStorageEventArgs e);
        public event WaveTableStorageEventHandler WaveTableDataChanged;

        public const string NumBits_PropertyName = "NumBits";
        public static Enum[] NumBitsAllowedValues { get { return WaveTableStorageRec.NumBitsAllowedValues; } }
        [Bindable(true)]
        public NumBitsType NumBits { get { return _WaveTableData.NumBits; } /* set { PatchObject(value, ref WaveTableData.NumBits, NumBits_PropertyName); } */ }
        public string NumBitsAsString { get { return EnumUtility.GetDescription(_WaveTableData.NumBits); } /* set { string old = EnumDescription.GetDescription(WaveTableData.NumBits); WaveTableData.NumBits = (NumBitsType)EnumDescription.GetValue(NumBitsType.eSample8bit, value); PatchObject(value, ref old, NumBits_PropertyName); } */ }

        public const string NumFrames_PropertyName = "NumFrames";
        public static int[] NumFramesAllowedValues { get { return WaveTableStorageRec.NumFramesAllowedValues; } }
        [Bindable(true)]
        public int NumFrames { get { return _WaveTableData.NumFrames; } /* set { Patch(value, ref WaveTableData.NumFrames, NumFrames_PropertyName); } */ }

        public const string NumTables_PropertyName = "NumTables";
        [Bindable(true)]
        public int NumTables { get { return _WaveTableData.NumTables; } /* set { Patch(value, ref WaveTableData.NumTables, NumTables_PropertyName); } */ }


        private string _WaveTableFormula =
            "# data : fixedarray; frames : integer; tables : integer" + Environment.NewLine;
        public const string WaveTableFormula_PropertyName = "WaveTableFormula";
        [Bindable(true)]
        [Searchable]
        public string WaveTableFormula { get { return _WaveTableFormula; } set { Patch(value, ref _WaveTableFormula, WaveTableFormula_PropertyName); } }

        private double _TestAttackDuration = 1;
        public const string TestAttackDuration_PropertyName = "TestAttackDuration";
        [Bindable(true)]
        public double TestAttackDuration { get { return _TestAttackDuration; } set { Patch(value, ref _TestAttackDuration, TestAttackDuration_PropertyName); } }

        private double _TestDecayDuration = 2;
        public const string TestDecayDuration_PropertyName = "TestDecayDuration";
        [Bindable(true)]
        public double TestDecayDuration { get { return _TestDecayDuration; } set { Patch(value, ref _TestDecayDuration, TestDecayDuration_PropertyName); } }

        private double _TestFrequency = Constants.MIDDLEC;
        public const string TestFrequency_PropertyName = "TestFrequency";
        [Bindable(true)]
        public double TestFrequency { get { return _TestFrequency; } set { Patch(value, ref _TestFrequency, TestFrequency_PropertyName); } }
        public string TestFrequencyAsString
        {
            get
            {
                return _TestFrequency.ToString();
            }
            set
            {
                double freq;
                if (Double.TryParse(value, out freq))
                {
                    TestFrequency = freq;
                }
                else
                {
                    short Pitch = Constants.CENTERNOTE;
                    NoteFlags SharpFlatThing = 0;
                    SymbolicPitch.StringToNumericPitch(value, ref Pitch, ref SharpFlatThing);
                    TestFrequency = Math.Exp(((double)(Pitch - Constants.CENTERNOTE) / 12) * Constants.LOG2) * Constants.MIDDLEC;
                }
            }
        }

        private int _TestSamplingRate = 44100;
        public const string TestSamplingRate_PropertyName = "TestSamplingRate";
        [Bindable(true)]
        public int TestSamplingRate { get { return _TestSamplingRate; } set { Patch(value, ref _TestSamplingRate, TestSamplingRate_PropertyName); } }


        private short _SavedWindowXLoc;
        public const string SavedWindowXLoc_PropertyName = "SavedWindowXLoc";
        [Bindable(true)]
        public short SavedWindowXLoc { get { return _SavedWindowXLoc; } set { Patch(value, ref _SavedWindowXLoc, SavedWindowXLoc_PropertyName, false/*modified*/); } }

        private short _SavedWindowYLoc;
        public const string SavedWindowYLoc_PropertyName = "SavedWindowYLoc";
        [Bindable(true)]
        public short SavedWindowYLoc { get { return _SavedWindowYLoc; } set { Patch(value, ref _SavedWindowYLoc, SavedWindowYLoc_PropertyName, false/*modified*/); } }

        private short _SavedWindowWidth;
        public const string SavedWindowWidth_PropertyName = "SavedWindowWidth";
        [Bindable(true)]
        public short SavedWindowWidth { get { return _SavedWindowWidth; } set { Patch(value, ref _SavedWindowWidth, SavedWindowWidth_PropertyName, false/*modified*/); } }

        private short _SavedWindowHeight;
        public const string SavedWindowHeight_PropertyName = "SavedWindowHeight";
        [Bindable(true)]
        public short SavedWindowHeight { get { return _SavedWindowHeight; } set { Patch(value, ref _SavedWindowHeight, SavedWindowHeight_PropertyName, false/*modified*/); } }


        public WaveTableObjectRec(Document document)
            : base(document, Document.WaveTableList_PropertyName)
        {
        }

        public WaveTableObjectRec(Document document, WaveTableStorageRec WaveTableData)

            : this(document)
        {
            this._WaveTableData = WaveTableData;
        }

        public WaveTableObjectRec(BinaryReader reader, LoadContext loadContext)
            : this(loadContext.document)
        {
            /*   1-byte format version number */
            /*       should be 1 */
            if (1 != reader.ReadByte())
            {
                throw new InvalidDataException();
            }

            /*   2-byte little endian window X location (signed; origin at upper left corner) */
            SavedWindowXLoc = reader.ReadInt16();
            /*   2-byte little endian window Y location */
            SavedWindowYLoc = reader.ReadInt16();
            /*   2-byte little endian window width */
            SavedWindowWidth = reader.ReadInt16();
            /*   2-byte little endian window height */
            SavedWindowHeight = reader.ReadInt16();

            /*   4-byte little endian wave table name length descriptor */
            /*   n-byte name string (line feed = 0x0a) */
            Name = loadContext.Utf8 ? reader.ReadString4Utf8() : reader.ReadString4Ansi();

            /*   4-byte little endian wave table formula length descriptor */
            /*   n-byte formula string (line feed = 0x0a) */
            _WaveTableFormula = loadContext.Utf8 ? reader.ReadString4Utf8() : reader.ReadString4Ansi();

            /*   4-byte little endian large integer encoded test attack duration. */
            /*       large integer coded decimal is decimal * 1000000 with a */
            /*       range of -1999.999999 to 1999.999999 */
            _TestAttackDuration = (double)reader.ReadLBCD();
            if (_TestAttackDuration < 0)
            {
                _TestAttackDuration = 0;
            }

            /*   4-byte little endian large integer encoded test decay duration. */
            _TestDecayDuration = (double)reader.ReadLBCD();
            if (_TestDecayDuration < 0)
            {
                _TestDecayDuration = 0;
            }

            /*   4-byte little endian test frequency fractional portion */
            /*       unsigned; divide by 2^32 to get the actual fraction */
            {
                uint u = reader.ReadUInt32();
                _TestFrequency = u / 4294967296.0;
            }
            /*   4-byte little endian test frequency integer portion */
            /*       total test frequency should be between 0.01 and 1e6 */
            {
                int i = reader.ReadInt32();
                _TestFrequency += i;
            }
            _TestFrequency = Math.Min(Math.Max(_TestFrequency, Constants.MINNATURALFREQ), Constants.MAXNATURALFREQ);

            /*   4-byte little endian test sampling rate */
            /*       should be between 100 and 65535 */
            _TestSamplingRate = Math.Min(Math.Max(reader.ReadInt32(), Constants.MINSAMPLINGRATE), Constants.MAXSAMPLINGRATE);

            /*   4-byte little endian number of tables */
            int NumberOfTables = reader.ReadInt32();
            if (NumberOfTables < 0)
            {
                throw new InvalidDataException();
            }

            /*   4-byte little endian number of frames per table */
            /*       must be an integral power or 2 between 2 and 65536 */
            int NumberOfFrames = reader.ReadInt32();
            if (((NumberOfFrames & (NumberOfFrames - 1)) != 0) || (NumberOfFrames < 2) || (NumberOfFrames > 65536))
            {
                throw new InvalidDataException();
            }

            /*   1-byte number of bits specifier */
            /*       must be 8, 16, or 24 */
            NumBitsType NumberOfBits;
            switch (reader.ReadByte())
            {
                default:
                    throw new InvalidDataException();
                case 8:
                    NumberOfBits = NumBitsType.eSample8bit;
                    break;
                case 16:
                    NumberOfBits = NumBitsType.eSample16bit;
                    break;
                case 24:
                    NumberOfBits = NumBitsType.eSample24bit;
                    break;
            }

            /*   n-byte sample data for the wave table */
            /*       data is stored as follows:  each table is stored consecutively starting */
            /*       with the table numbered 0.  in each table, each sample frame is stored */
            /*       consecutively as a signed 2s complement value.  8-bit sample frames */
            /*       use 1 byte each.  16-bit sample frames use 2 bytes and are stored little */
            /*       endian. */
            _WaveTableData = new WaveTableStorageRec(NumberOfTables, NumberOfFrames, NumberOfBits);
            for (int i = 0; i < NumberOfTables; i++)
            {
                switch (NumberOfBits)
                {
                    default:
                        throw new InvalidDataException();
                    case NumBitsType.eSample8bit:
                        for (int j = 0; j < NumberOfFrames; j++)
                        {
                            _WaveTableData.ListOfTables[i][j] = SampConv.SignedByteToFloat(reader.ReadByte());
                        }
                        break;
                    case NumBitsType.eSample16bit:
                        for (int j = 0; j < NumberOfFrames; j++)
                        {
                            _WaveTableData.ListOfTables[i][j] = SampConv.SignedShortToFloat(reader.ReadInt16());
                        }
                        break;
                    case NumBitsType.eSample24bit:
                        for (int j = 0; j < NumberOfFrames; j++)
                        {
                            _WaveTableData.ListOfTables[i][j] = SampConv.SignedTribyteToFloat(reader.ReadInt24());
                        }
                        break;
                }
            }
        }

        public static WaveTableObjectRec Create(BinaryReader reader, LoadContext loadContext)
        {
            return new WaveTableObjectRec(reader, loadContext);
        }

        public void Save(BinaryWriter writer, SaveContext saveContext)
        {
            /*   1-byte format version number */
            /*       should be 1 */
            writer.WriteByte(1);

            /*   2-byte little endian window X location (signed; origin at upper left corner) */
            writer.WriteInt16(SavedWindowXLoc);
            /*   2-byte little endian window Y location */
            writer.WriteInt16(SavedWindowYLoc);
            /*   2-byte little endian window width */
            writer.WriteInt16(SavedWindowWidth);
            /*   2-byte little endian window height */
            writer.WriteInt16(SavedWindowHeight);

            /*   4-byte little endian wave table name length descriptor */
            /*   n-byte name string (line feed = 0x0a) */
            writer.WriteString4Utf8(Name);

            /*   4-byte little endian wave table formula length descriptor */
            /*   n-byte formula string (line feed = 0x0a) */
            writer.WriteString4Utf8(_WaveTableFormula);

            /*   4-byte little endian large integer encoded test attack duration. */
            /*       large integer coded decimal is decimal * 1000000 with a */
            /*       range of -1999.999999 to 1999.999999 */
            writer.WriteLBCD((LargeBCDType)_TestAttackDuration);

            /*   4-byte little endian large integer encoded test decay duration. */
            writer.WriteLBCD((LargeBCDType)_TestDecayDuration);

            /*   4-byte little endian test frequency fractional portion */
            /*       unsigned; divide by 2^32 to get the actual fraction */
            writer.WriteUInt32((uint)((_TestFrequency - Math.Floor(_TestFrequency)) * 4294967296.0));
            /*   4-byte little endian test frequency integer portion */
            /*       total test frequency should be between 0.01 and 1e6 */
            writer.WriteInt32((int)Math.Floor(_TestFrequency));

            /*   4-byte little endian test sampling rate */
            /*       should be between 100 and 65535 */
            writer.WriteInt32(_TestSamplingRate);

            /*   4-byte little endian number of tables */
            writer.WriteInt32(_WaveTableData.ListOfTables.Length);

            /*   4-byte little endian number of frames per table */
            /*       must be an integral power or 2 between 2 and 65536 */
            writer.WriteInt32(_WaveTableData.NumFrames);

            /*   1-byte number of bits specifier */
            /*       must be 8, 16, or 24 */
            switch (_WaveTableData.NumBits)
            {
                default:
                    throw new ArgumentException();
                case NumBitsType.eSample8bit:
                    writer.WriteByte(8);
                    break;
                case NumBitsType.eSample16bit:
                    writer.WriteByte(16);
                    break;
                case NumBitsType.eSample24bit:
                    writer.WriteByte(24);
                    break;
            }

            /*   n-byte sample data for the wave table */
            /*       data is stored as follows:  each table is stored consecutively starting */
            /*       with the table numbered 0.  in each table, each sample frame is stored */
            /*       consecutively as a signed 2s complement value.  8-bit sample frames */
            /*       use 1 byte each.  16-bit sample frames use 2 bytes and are stored little */
            /*       endian. */
            for (int iTable = 0; iTable < _WaveTableData.ListOfTables.Length; iTable++)
            {
                WaveTableStorageRec.Table Slice = _WaveTableData.ListOfTables[iTable];
                switch (_WaveTableData.NumBits)
                {
                    default:
                        throw new ArgumentException();
                    case NumBitsType.eSample8bit:
                        for (int i = 0; i < _WaveTableData.NumFrames; i++)
                        {
                            writer.WriteByte(SampConv.FloatToSignedByte(Slice[i]));
                        }
                        break;
                    case NumBitsType.eSample16bit:
                        for (int i = 0; i < _WaveTableData.NumFrames; i++)
                        {
                            writer.WriteInt16(SampConv.FloatToSignedShort(Slice[i]));
                        }
                        break;
                    case NumBitsType.eSample24bit:
                        for (int i = 0; i < _WaveTableData.NumFrames; i++)
                        {
                            writer.WriteInt24(SampConv.FloatToSignedTribyte(Slice[i]));
                        }
                        break;
                }
            }
        }

        public static void StaticSave(BinaryWriter writer, object o, SaveContext saveContext)
        {
            ((WaveTableObjectRec)o).Save(writer, saveContext);
        }
    }

    public partial class AlgoWaveTableObjectRec : HierarchicalBindingBuildable
    {
        private string _Name = String.Empty;
        public const string Name_PropertyName = "Name";
        [Bindable(true)]
        [Searchable]
        public string Name { get { return _Name; } set { Patch(value, ref _Name, Name_PropertyName); } }

        private string _AlgoWaveTableFormula =
            "# frames : integer; tables : integer; data : fixedarray" + Environment.NewLine;
        public const string AlgoWaveTableFormula_PropertyName = "AlgoWaveTableFormula";
        [Bindable(true)]
        [Searchable]
        public string AlgoWaveTableFormula { get { return _AlgoWaveTableFormula; } set { Patch(value, ref _AlgoWaveTableFormula, AlgoWaveTableFormula_PropertyName); } }

        private int _NumFrames = 4096;
        public const string NumFrames_PropertyName = "NumFrames";
        public static int[] NumFramesAllowedValues { get { return WaveTableStorageRec.NumFramesAllowedValues; } }
        [Bindable(true)]
        public int NumFrames { get { return _NumFrames; } set { Patch(value, ref _NumFrames, NumFrames_PropertyName); } }

        private int _NumTables = 1;
        public const string NumTables_PropertyName = "NumTables";
        [Bindable(true)]
        public int NumTables { get { return _NumTables; } set { Patch(Math.Max(value, 0), ref _NumTables, NumTables_PropertyName); } }

        private short _SavedWindowXLoc;
        public const string SavedWindowXLoc_PropertyName = "SavedWindowXLoc";
        [Bindable(true)]
        public short SavedWindowXLoc { get { return _SavedWindowXLoc; } set { Patch(value, ref _SavedWindowXLoc, SavedWindowXLoc_PropertyName, false/*modified*/); } }

        private short _SavedWindowYLoc;
        public const string SavedWindowYLoc_PropertyName = "SavedWindowYLoc";
        [Bindable(true)]
        public short SavedWindowYLoc { get { return _SavedWindowYLoc; } set { Patch(value, ref _SavedWindowYLoc, SavedWindowYLoc_PropertyName, false/*modified*/); } }

        private short _SavedWindowWidth;
        public const string SavedWindowWidth_PropertyName = "SavedWindowWidth";
        [Bindable(true)]
        public short SavedWindowWidth { get { return _SavedWindowWidth; } set { Patch(value, ref _SavedWindowWidth, SavedWindowWidth_PropertyName, false/*modified*/); } }

        private short _SavedWindowHeight;
        public const string SavedWindowHeight_PropertyName = "SavedWindowHeight";
        [Bindable(true)]
        public short SavedWindowHeight { get { return _SavedWindowHeight; } set { Patch(value, ref _SavedWindowHeight, SavedWindowHeight_PropertyName, false/*modified*/); } }


        public AlgoWaveTableObjectRec(Document document)
            : base(document, Document.AlgoWaveTableList_PropertyName)
        {
        }

        public AlgoWaveTableObjectRec(BinaryReader reader, LoadContext loadContext)
            : this(loadContext.document)
        {
            /*   1-byte format version number */
            /*       should be 1 */
            if (1 != reader.ReadByte())
            {
                throw new InvalidDataException();
            }

            /*   2-byte little endian window X position (signed; origin at top-left corner) */
            _SavedWindowXLoc = reader.ReadInt16();
            /*   2-byte little endian window Y position */
            _SavedWindowYLoc = reader.ReadInt16();
            /*   2-byte little endian window width */
            _SavedWindowWidth = reader.ReadInt16();
            /*   2-byte little endian window height */
            _SavedWindowHeight = reader.ReadInt16();

            /*   4-byte little endian name length descriptor */
            /*   n-byte name string (line feed = 0x0a) */
            Name = loadContext.Utf8 ? reader.ReadString4Utf8() : reader.ReadString4Ansi();

            /*   4-byte little endian formula length descriptor */
            /*   n-byte formula string (line feed = 0x0a) */
            _AlgoWaveTableFormula = loadContext.Utf8 ? reader.ReadString4Utf8() : reader.ReadString4Ansi();

            /*   4-byte little endian number of frames */
            /*       should be an integral power of 2 between 2 and 65536 */
            _NumFrames = reader.ReadInt32();
            if (((_NumFrames & (_NumFrames - 1)) != 0) || (_NumFrames < 2) || (_NumFrames > 65536))
            {
                throw new InvalidDataException();
            }

            /*   4-byte little endian number of tables */
            _NumTables = reader.ReadInt32();
            if (_NumTables < 0)
            {
                throw new InvalidDataException();
            }

            /*   1-byte number of bits */
            /*       should be either 8, 16, or 24 */
            switch (reader.ReadByte())
            {
                default:
                    throw new InvalidDataException();
                case 8:
                case 16:
                case 24:
                    // not used - always float now
                    break;
            }
        }

        public static AlgoWaveTableObjectRec Create(BinaryReader reader, LoadContext loadContext)
        {
            return new AlgoWaveTableObjectRec(reader, loadContext);
        }

        public void Save(BinaryWriter writer, SaveContext saveContext)
        {
            /*   1-byte format version number */
            /*       should be 1 */
            writer.WriteByte(1);

            /*   2-byte little endian window X position (signed; origin at top-left corner) */
            writer.WriteInt16(_SavedWindowXLoc);
            /*   2-byte little endian window Y position */
            writer.WriteInt16(_SavedWindowYLoc);
            /*   2-byte little endian window width */
            writer.WriteInt16(_SavedWindowWidth);
            /*   2-byte little endian window height */
            writer.WriteInt16(_SavedWindowHeight);

            /*   4-byte little endian name length descriptor */
            /*   n-byte name string (line feed = 0x0a) */
            writer.WriteString4Utf8(Name);

            /*   4-byte little endian formula length descriptor */
            /*   n-byte formula string (line feed = 0x0a) */
            writer.WriteString4Utf8(_AlgoWaveTableFormula);

            /*   4-byte little endian number of frames */
            /*       should be an integral power of 2 between 2 and 65536 */
            writer.WriteInt32(_NumFrames);

            /*   4-byte little endian number of tables */
            writer.WriteInt32(_NumTables);

            /*   1-byte number of bits */
            /*       should be either 8, 16, or 24 */
            writer.WriteByte(24);
        }

        public static void StaticSave(BinaryWriter writer, object o, SaveContext saveContext)
        {
            ((AlgoWaveTableObjectRec)o).Save(writer, saveContext);
        }
    }

    public partial class InstrObjectRec : HierarchicalBindingBuildable
    {
        private string _Name = String.Empty;
        public const string Name_PropertyName = "Name";
        [Bindable(true)]
        [Searchable]
        public string Name { get { return _Name; } set { Patch(value, ref _Name, Name_PropertyName); } }

        private string _InstrDefinition = String.Empty;
        public const string InstrDefinition_PropertyName = "InstrDefinition";
        [Bindable(true)]
        [Searchable]
        public string InstrDefinition { get { return _InstrDefinition; } set { Patch(value, ref _InstrDefinition, InstrDefinition_PropertyName); } }

        private short _SavedWindowXLoc;
        public const string SavedWindowXLoc_PropertyName = "SavedWindowXLoc";
        [Bindable(true)]
        public short SavedWindowXLoc { get { return _SavedWindowXLoc; } set { Patch(value, ref _SavedWindowXLoc, SavedWindowXLoc_PropertyName, false/*modified*/); } }

        private short _SavedWindowYLoc;
        public const string SavedWindowYLoc_PropertyName = "SavedWindowYLoc";
        [Bindable(true)]
        public short SavedWindowYLoc { get { return _SavedWindowYLoc; } set { Patch(value, ref _SavedWindowYLoc, SavedWindowYLoc_PropertyName, false/*modified*/); } }

        private short _SavedWindowWidth;
        public const string SavedWindowWidth_PropertyName = "SavedWindowWidth";
        [Bindable(true)]
        public short SavedWindowWidth { get { return _SavedWindowWidth; } set { Patch(value, ref _SavedWindowWidth, SavedWindowWidth_PropertyName, false/*modified*/); } }

        private short _SavedWindowHeight;
        public const string SavedWindowHeight_PropertyName = "SavedWindowHeight";
        [Bindable(true)]
        public short SavedWindowHeight { get { return _SavedWindowHeight; } set { Patch(value, ref _SavedWindowHeight, SavedWindowHeight_PropertyName, false/*modified*/); } }


        public InstrObjectRec(Document document)
            : base(document, Document.InstrumentList_PropertyName)
        {
        }

        public InstrObjectRec(BinaryReader reader, LoadContext loadContext)
            : this(loadContext.document)
        {
            /*   1-byte format version number */
            /*       should be 1 */
            if (1 != reader.ReadByte())
            {
                throw new InvalidDataException();
            }

            /*   2-byte little endian window X location (signed; origin at top-left of screen) */
            _SavedWindowXLoc = reader.ReadInt16();
            /*   2-byte little endian window Y location */
            _SavedWindowYLoc = reader.ReadInt16();
            /*   2-byte little endian window width */
            _SavedWindowWidth = reader.ReadInt16();
            /*   2-byte little endian window height */
            _SavedWindowHeight = reader.ReadInt16();

            /*   4-byte little endian name string length */
            /*   n-byte name string (line feed = 0x0a) */
            Name = loadContext.Utf8 ? reader.ReadString4Utf8() : reader.ReadString4Ansi();

            /*   4-byte little endian instrument definition length */
            /*   n-byte instrument definition string (line feed = 0x0a) */
            _InstrDefinition = loadContext.Utf8 ? reader.ReadString4Utf8() : reader.ReadString4Ansi();
        }

        public static InstrObjectRec Create(BinaryReader reader, LoadContext loadContext)
        {
            return new InstrObjectRec(reader, loadContext);
        }

        public void Save(BinaryWriter writer, SaveContext saveContext)
        {
            /*   1-byte format version number */
            /*       should be 1 */
            writer.WriteByte(1);

            /*   2-byte little endian window X location (signed; origin at top-left of screen) */
            writer.WriteInt16(_SavedWindowXLoc);
            /*   2-byte little endian window Y location */
            writer.WriteInt16(_SavedWindowYLoc);
            /*   2-byte little endian window width */
            writer.WriteInt16(_SavedWindowWidth);
            /*   2-byte little endian window height */
            writer.WriteInt16(_SavedWindowHeight);

            /*   4-byte little endian name string length */
            /*   n-byte name string (line feed = 0x0a) */
            writer.WriteString4Utf8(Name);

            /*   4-byte little endian instrument definition length */
            /*   n-byte instrument definition string (line feed = 0x0a) */
            writer.WriteString4Utf8(_InstrDefinition);
        }

        public static void StaticSave(BinaryWriter writer, object o, SaveContext saveContext)
        {
            ((InstrObjectRec)o).Save(writer, saveContext);
        }
    }

    public partial class SectionObjectRec : HierarchicalBindingBuildable
    {
        private string _Source = String.Empty;
        public const string Source_PropertyName = "Source";
        [Bindable(true)]
        [Searchable]
        public string Source { get { return _Source; } set { Patch(value, ref _Source, Source_PropertyName); } }

        private string _Name = String.Empty;
        public const string Name_PropertyName = "Name";
        [Bindable(true)]
        [Searchable]
        public string Name { get { return _Name; } set { Patch(value, ref _Name, Name_PropertyName); } }


        private short _SavedWindowXLoc;
        public const string SavedWindowXLoc_PropertyName = "SavedWindowXLoc";
        [Bindable(true)]
        public short SavedWindowXLoc { get { return _SavedWindowXLoc; } set { Patch(value, ref _SavedWindowXLoc, SavedWindowXLoc_PropertyName, false/*modified*/); } }

        private short _SavedWindowYLoc;
        public const string SavedWindowYLoc_PropertyName = "SavedWindowYLoc";
        [Bindable(true)]
        public short SavedWindowYLoc { get { return _SavedWindowYLoc; } set { Patch(value, ref _SavedWindowYLoc, SavedWindowYLoc_PropertyName, false/*modified*/); } }

        private short _SavedWindowWidth;
        public const string SavedWindowWidth_PropertyName = "SavedWindowWidth";
        [Bindable(true)]
        public short SavedWindowWidth { get { return _SavedWindowWidth; } set { Patch(value, ref _SavedWindowWidth, SavedWindowWidth_PropertyName, false/*modified*/); } }

        private short _SavedWindowHeight;
        public const string SavedWindowHeight_PropertyName = "SavedWindowHeight";
        [Bindable(true)]
        public short SavedWindowHeight { get { return _SavedWindowHeight; } set { Patch(value, ref _SavedWindowHeight, SavedWindowHeight_PropertyName, false/*modified*/); } }


        public SectionObjectRec(Document document)
            : base(document, Document.SectionList_PropertyName)
        {
        }

        public SectionObjectRec(BinaryReader reader, LoadContext loadContext)
            : this(loadContext.document)
        {
            /*   1-byte section object version number */
            /*       should be 1 */
            if (1 != reader.ReadByte())
            {
                throw new InvalidDataException();
            }

            /*   2-byte little endian window x location (origin at top-left of screen) */
            _SavedWindowXLoc = reader.ReadInt16();
            /*   2-byte little endian window y location */
            _SavedWindowYLoc = reader.ReadInt16();
            /*   2-byte little endian window width */
            _SavedWindowWidth = reader.ReadInt16();
            /*   2-byte little endian window height */
            _SavedWindowHeight = reader.ReadInt16();

            /*   4-byte little endian object name length */
            /*   n-byte name data (line feed = 0x0a) */
            Name = loadContext.Utf8 ? reader.ReadString4Utf8() : reader.ReadString4Ansi();

            /*   4-byte little endian section source text length */
            /*   n-byte section source text data (line feed = 0x0a) */
            _Source = loadContext.Utf8 ? reader.ReadString4Utf8() : reader.ReadString4Ansi();
        }

        public void Save(BinaryWriter writer, SaveContext saveContext)
        {
            /*   1-byte section object version number */
            /*       should be 1 */
            writer.WriteByte(1);

            /*   2-byte little endian window x location (origin at top-left of screen) */
            writer.WriteInt16(_SavedWindowXLoc);
            /*   2-byte little endian window y location */
            writer.WriteInt16(_SavedWindowYLoc);
            /*   2-byte little endian window width */
            writer.WriteInt16(_SavedWindowWidth);
            /*   2-byte little endian window height */
            writer.WriteInt16(_SavedWindowHeight);

            /*   4-byte little endian object name length */
            /*   n-byte name data (line feed = 0x0a) */
            writer.WriteString4Utf8(Name);

            /*   4-byte little endian Section source text length */
            /*   n-byte Section source text data (line feed = 0x0a) */
            writer.WriteString4Utf8(_Source);
        }
    }

    public partial class ScoreEffectsRec : HierarchicalBindingBuildable
    {
        private string _Source =
            "#score effects" + Environment.NewLine;
        public const string Source_PropertyName = "Source";
        [Bindable(true)]
        [Searchable]
        public string Source { get { return _Source; } set { Patch(value, ref _Source, Source_PropertyName); } }


        private short _SavedWindowXLoc;
        public const string SavedWindowXLoc_PropertyName = "SavedWindowXLoc";
        [Bindable(true)]
        public short SavedWindowXLoc { get { return _SavedWindowXLoc; } set { Patch(value, ref _SavedWindowXLoc, SavedWindowXLoc_PropertyName, false/*modified*/); } }

        private short _SavedWindowYLoc;
        public const string SavedWindowYLoc_PropertyName = "SavedWindowYLoc";
        [Bindable(true)]
        public short SavedWindowYLoc { get { return _SavedWindowYLoc; } set { Patch(value, ref _SavedWindowYLoc, SavedWindowYLoc_PropertyName, false/*modified*/); } }

        private short _SavedWindowWidth;
        public const string SavedWindowWidth_PropertyName = "SavedWindowWidth";
        [Bindable(true)]
        public short SavedWindowWidth { get { return _SavedWindowWidth; } set { Patch(value, ref _SavedWindowWidth, SavedWindowWidth_PropertyName, false/*modified*/); } }

        private short _SavedWindowHeight;
        public const string SavedWindowHeight_PropertyName = "SavedWindowHeight";
        [Bindable(true)]
        public short SavedWindowHeight { get { return _SavedWindowHeight; } set { Patch(value, ref _SavedWindowHeight, SavedWindowHeight_PropertyName, false/*modified*/); } }


        public ScoreEffectsRec(Document document)
            : base(document, Document.ScoreEffects_PropertyName)
        {
        }

        public ScoreEffectsRec(BinaryReader reader, LoadContext loadContext)
            : this(loadContext.document)
        {
            /*   4-byte little endian length of song post processing function */
            /*   n-bytes of post processing function text (line fed = 0x0a) */
            _Source = loadContext.Utf8 ? reader.ReadString4Utf8() : reader.ReadString4Ansi();

            /*   2-byte little endian post processing function window X position */
            /*       (signed, origin at top-left corner) */
            /*       ONLY valid for version 3 and later */
            /*   2-byte little endian post processing function window Y position */
            /*       ONLY valid for version 3 and later */
            /*   2-byte little endian post processing function window width */
            /*       ONLY valid for version 3 and later */
            /*   2-byte little endian post processing function window height */
            /*       ONLY valid for version 3 and later */
            if (loadContext.FormatVersionNumber >= 3)
            {
                _SavedWindowXLoc = reader.ReadInt16();
                _SavedWindowYLoc = reader.ReadInt16();
                _SavedWindowWidth = reader.ReadInt16();
                _SavedWindowHeight = reader.ReadInt16();
            }
        }

        public void Save(BinaryWriter writer, SaveContext saveContext)
        {
            /*   4-byte little endian length of song post processing function */
            /*   n-bytes of post processing function text (line fed = 0x0a) */
            writer.WriteString4Utf8(_Source);

            /*   2-byte little endian post processing function window X position */
            /*       (signed, origin at top-left corner) */
            /*       ONLY valid for version 3 and later */
            /*   2-byte little endian post processing function window Y position */
            /*       ONLY valid for version 3 and later */
            /*   2-byte little endian post processing function window width */
            /*       ONLY valid for version 3 and later */
            /*   2-byte little endian post processing function window height */
            /*       ONLY valid for version 3 and later */
            writer.WriteInt16(_SavedWindowXLoc);
            writer.WriteInt16(_SavedWindowYLoc);
            writer.WriteInt16(_SavedWindowWidth);
            writer.WriteInt16(_SavedWindowHeight);
        }
    }


    public partial class SequencerRec : HierarchicalBindingBuildable
    {
        private string _Source =
            "#sequencer configuration" + Environment.NewLine;
        public const string Source_PropertyName = "Source";
        [Bindable(true)]
        [Searchable]
        public string Source { get { return _Source; } set { Patch(value, ref _Source, Source_PropertyName); } }


        private short _SavedWindowXLoc;
        public const string SavedWindowXLoc_PropertyName = "SavedWindowXLoc";
        [Bindable(true)]
        public short SavedWindowXLoc { get { return _SavedWindowXLoc; } set { Patch(value, ref _SavedWindowXLoc, SavedWindowXLoc_PropertyName, false/*modified*/); } }

        private short _SavedWindowYLoc;
        public const string SavedWindowYLoc_PropertyName = "SavedWindowYLoc";
        [Bindable(true)]
        public short SavedWindowYLoc { get { return _SavedWindowYLoc; } set { Patch(value, ref _SavedWindowYLoc, SavedWindowYLoc_PropertyName, false/*modified*/); } }

        private short _SavedWindowWidth;
        public const string SavedWindowWidth_PropertyName = "SavedWindowWidth";
        [Bindable(true)]
        public short SavedWindowWidth { get { return _SavedWindowWidth; } set { Patch(value, ref _SavedWindowWidth, SavedWindowWidth_PropertyName, false/*modified*/); } }

        private short _SavedWindowHeight;
        public const string SavedWindowHeight_PropertyName = "SavedWindowHeight";
        [Bindable(true)]
        public short SavedWindowHeight { get { return _SavedWindowHeight; } set { Patch(value, ref _SavedWindowHeight, SavedWindowHeight_PropertyName, false/*modified*/); } }


        public SequencerRec(Document document)
            : base(document, Document.Sequencer_PropertyName)
        {
        }

        public SequencerRec(BinaryReader reader, LoadContext loadContext)
            : this(loadContext.document)
        {
            /*   4-byte little endian length of sequencer configuration */
            /*   n-bytes of sequencer configuration text (line fed = 0x0a) */
            _Source = loadContext.Utf8 ? reader.ReadString4Utf8() : reader.ReadString4Ansi();

            /*   2-byte little endian sequencer window X position */
            /*       (signed, origin at top-left corner) */
            /*   2-byte little endian sequencer window Y position */
            /*   2-byte little endian sequencer window width */
            /*   2-byte little endian sequencer window height */
            _SavedWindowXLoc = reader.ReadInt16();
            _SavedWindowYLoc = reader.ReadInt16();
            _SavedWindowWidth = reader.ReadInt16();
            _SavedWindowHeight = reader.ReadInt16();
        }

        public void Save(BinaryWriter writer, SaveContext saveContext)
        {
            /* sequencer -- only valid in version 5 or later */
            /*   4-byte little endian length of sequencer config */
            /*   n-bytes of sequencer config text (line fed = 0x0a) */
            writer.WriteString4Utf8(_Source);

            /*   2-byte little endian sequencer config window X position */
            /*       (signed, origin at top-left corner) */
            /*   2-byte little endian sequencer config window Y position */
            /*   2-byte little endian sequencer config window width */
            /*   2-byte little endian sequencer config window height */
            writer.WriteInt16(_SavedWindowXLoc);
            writer.WriteInt16(_SavedWindowYLoc);
            writer.WriteInt16(_SavedWindowWidth);
            writer.WriteInt16(_SavedWindowHeight);
        }
    }


    [Flags]
    public enum Bit : uint
    {
        _0 = 1u << 0,
        _1 = 1u << 1,
        _2 = 1u << 2,
        _3 = 1u << 3,
        _4 = 1u << 4,
        _5 = 1u << 5,
        _6 = 1u << 6,
        _7 = 1u << 7,
        _8 = 1u << 8,
        _9 = 1u << 9,
        _10 = 1u << 10,
        _11 = 1u << 11,
        _12 = 1u << 12,
        _13 = 1u << 13,
        _14 = 1u << 14,
        _15 = 1u << 15,
        _16 = 1u << 16,
        _17 = 1u << 17,
        _18 = 1u << 18,
        _19 = 1u << 19,
        _20 = 1u << 20,
        _21 = 1u << 21,
        _22 = 1u << 22,
        _23 = 1u << 23,
        _24 = 1u << 24,
        _25 = 1u << 25,
        _26 = 1u << 26,
        _27 = 1u << 27,
        _28 = 1u << 28,
        _29 = 1u << 29,
        _30 = 1u << 30,
        _31 = 1u << 31,
    }

    [Flags]
    public enum NoteFlags : uint
    {
        eDurationMask = Bit._0 | Bit._1 | Bit._2 | Bit._3,
        e64thNote = 1 * Bit._0, /* value 0 skipped */
        e32ndNote = 2 * Bit._0,
        e16thNote = 3 * Bit._0,
        e8thNote = 4 * Bit._0,
        e4thNote = 5 * Bit._0,
        e2ndNote = 6 * Bit._0,
        eWholeNote = 7 * Bit._0,
        eDoubleNote = 8 * Bit._0,
        eQuadNote = 9 * Bit._0,

        eDivisionMask = Bit._4 | Bit._5,
        eDiv1Modifier = 0 * Bit._4,
        eDiv3Modifier = 1 * Bit._4,
        eDiv5Modifier = 2 * Bit._4,
        eDiv7Modifier = 3 * Bit._4,

        eDotModifier = Bit._6,
        eFlatModifier = Bit._7,
        eSharpModifier = Bit._8,
        eRestModifier = Bit._9,

        [Description("Default"), Category("ReleasePoint1Origin")]
        eRelease1FromDefault = 1 * Bit._10,
        [Description("From Start"), Category("ReleasePoint1Origin")]
        eRelease1FromStart = 2 * Bit._10,
        [Description("From End"), Category("ReleasePoint1Origin")]
        eRelease1FromEnd = 3 * Bit._10,
        eRelease1OriginMask = Bit._10 | Bit._11,

        [Description("Default"), Category("ReleasePoint2Origin")]
        eRelease2FromDefault = 1 * Bit._12,
        [Description("From Start"), Category("ReleasePoint2Origin")]
        eRelease2FromStart = 2 * Bit._12,
        [Description("From End"), Category("ReleasePoint2Origin")]
        eRelease2FromEnd = 3 * Bit._12,
        eRelease2OriginMask = Bit._12 | Bit._13,

        [Description("From Start"), Category("ReleasePoint3Origin")]
        eRelease3FromStartNotEnd = Bit._14,
        [Description("From End"), Category("ReleasePoint3Origin")]
        eRelease3FromEnd = 0, // for UI only
        eRelease3OriginMask = Bit._14, // for UI only

        [Description("Default"), Category("PitchDisplacementOrigin")]
        ePitchDisplacementStartFromDefault = 1 * Bit._15,
        [Description("From Start"), Category("PitchDisplacementOrigin")]
        ePitchDisplacementStartFromStart = 2 * Bit._15,
        [Description("From End"), Category("PitchDisplacementOrigin")]
        ePitchDisplacementStartFromEnd = 3 * Bit._15,
        ePitchDisplacementStartOriginMask = Bit._15 | Bit._16,

        /* pitch lfo mode control has been moved to the lfo definition */
        eDEALLOCATED17 = Bit._17,
        eDEALLOCATED18 = Bit._18,

        [Description("Default"), Category(NoteNoteObjectRec.DetuningMode_EnumCategoryName)]
        eDetuningModeDefault = 1 * Bit._19,
        [Description("Half Steps"), Category(NoteNoteObjectRec.DetuningMode_EnumCategoryName)]
        eDetuningModeHalfSteps = 2 * Bit._19,
        [Description("Hertz"), Category(NoteNoteObjectRec.DetuningMode_EnumCategoryName)]
        eDetuningModeHertz = 3 * Bit._19,
        eDetuningModeMask = Bit._19 | Bit._20,

        [Description("Default"), Category("DurationAdjustMode")]
        eDurationAdjustDefault = 1 * Bit._21,
        [Description("Additive"), Category("DurationAdjustMode")]
        eDurationAdjustAdditive = 2 * Bit._21,
        [Description("Multiplicative"), Category("DurationAdjustMode")]
        eDurationAdjustMultiplicative = 3 * Bit._21,
        eDurationAdjustMask = Bit._21 | Bit._22,

        eRetriggerEnvelopesOnTieFlag = Bit._23,

        [Description("Hertz"), Category("PortamentoUnits")]
        ePortamentoUnitsHertzNotHalfsteps = Bit._24,
        [Description("Half Steps"), Category("PortamentoUnits")]
        ePortamentoUnitsHalfsteps = 0, // UI only
        ePortamentoUnitsMask = Bit._24, // UI only

        ePortamentoLeadsNote = Bit._25,

        eUnusedBitMask = Bit._17 | Bit._18 | Bit._26 | Bit._27 | Bit._28 | Bit._29 | Bit._30 | Bit._31,

        eCommandFlag = Bit._31,
    }

    /* commands (low order 31 bits of the flag word) */
    /* there are 4 parameters for commands: <1>, <2>, <3>, and <string> */
    /* commands <1>, <2>, and <3> can be interpreted as large BCD numbers (xx.xxxxxx), */
    /* extended small BCD numbers (xxxxx.xxx), or as integers */
    /* large is represented by <_l>, extended small is <_xs>, integer is <_i> */
    public enum NoteCommands
    {
        eCmd_Start,

        /* tempo adjustments */
        eCmdRestoreTempo = eCmd_Start, /* restore the tempo to the default for the score */
        eCmdSetTempo, /* set tempo to <1xs> number of beats per minute */
        eCmdIncTempo, /* add <1xs> to the tempo control */
        eCmdSweepTempoAbs, /* <1xs> = target tempo, <2xs> = # of beats to reach it */
        eCmdSweepTempoRel, /* <1xs> = target adjust (add to tempo), <2xs> = # beats */

        /* stereo positioning adjustments */
        eCmdRestoreStereoPosition, /* restore stereo position to channel's default */
        eCmdSetStereoPosition, /* set position in channel <1l>: -1 = left, 1 = right */
        eCmdIncStereoPosition, /* adjust stereo position by adding <1l> */
        eCmdSweepStereoAbs, /* <1l> = new pos, <2xs> = # of beats to get there */
        eCmdSweepStereoRel, /* <1l> = pos adjust, <2xs> = # beats to get there */

        /* surround positioning adjustments */
        eCmdRestoreSurroundPosition, /* restore surround position to channel's default */
        eCmdSetSurroundPosition, /* set position in channel <1l>: 1 = front, -1 = rear */
        eCmdIncSurroundPosition, /* adjust surround position by adding <1l> */
        eCmdSweepSurroundAbs, /* <1l> = new pos, <2xs> = # of beats to get there */
        eCmdSweepSurroundRel, /* <1l> = pos adjust, <2xs> = # beats to get there */

        /* overall volume adjustments */
        eCmdRestoreVolume, /* restore the volume to the default for the channel */
        eCmdSetVolume, /* set the volume to the specified level (0..1) in <1l> */
        eCmdIncVolume, /* multiply <1l> by the volume control */
        eCmdSweepVolumeAbs, /* <1l> = new volume, <2xs> = # of beats to reach it */
        eCmdSweepVolumeRel, /* <1l> = volume adjust, <2xs> = # of beats to reach it */

        /* default release point adjustment values */
        eCmdRestoreReleasePoint1, /* restore release point to master default */
        eCmdSetReleasePoint1, /* set the default release point to new value <1l> */
        eCmdIncReleasePoint1, /* add <1l> to default release point for adjustment */
        eCmdReleasePointOrigin1, /* <1i> -1 = from start, 0 = from end of note */
        eCmdSweepReleaseAbs1, /* <1l> = new release, <2xs> = # of beats to get there */
        eCmdSweepReleaseRel1, /* <1l> = release adjust, <2xs> = # of beats to get there */

        eCmdRestoreReleasePoint2, /* restore release point to master default */
        eCmdSetReleasePoint2, /* set the default release point to new value <1l> */
        eCmdIncReleasePoint2, /* add <1l> to default release point for adjustment */
        eCmdReleasePointOrigin2, /* <1i> -1 = from start, 0 = from end of note */
        eCmdSweepReleaseAbs2, /* <1l> = new release, <2xs> = # of beats to get there */
        eCmdSweepReleaseRel2, /* <1l> = release adjust, <2xs> = # of beats to get there */

        /* set the default accent values */
        eCmdRestoreAccent1, /* restore accent value to master default */
        eCmdSetAccent1, /* specify the new default accent in <1l> */
        eCmdIncAccent1, /* add <1l> to the default accent */
        eCmdSweepAccentAbs1, /* <1l> = new accent, <2xs> = # of beats to get there */
        eCmdSweepAccentRel1, /* <1l> = accent adjust, <2xs> = # of beats to get there */

        eCmdRestoreAccent2, /* restore accent value to master default */
        eCmdSetAccent2, /* specify the new default accent in <1l> */
        eCmdIncAccent2, /* add <1l> to the default accent */
        eCmdSweepAccentAbs2, /* <1l> = new accent, <2xs> = # of beats to get there */
        eCmdSweepAccentRel2, /* <1l> = accent adjust, <2xs> = # of beats to get there */

        eCmdRestoreAccent3, /* restore accent value to master default */
        eCmdSetAccent3, /* specify the new default accent in <1l> */
        eCmdIncAccent3, /* add <1l> to the default accent */
        eCmdSweepAccentAbs3, /* <1l> = new accent, <2xs> = # of beats to get there */
        eCmdSweepAccentRel3, /* <1l> = accent adjust, <2xs> = # of beats to get there */

        eCmdRestoreAccent4, /* restore accent value to master default */
        eCmdSetAccent4, /* specify the new default accent in <1l> */
        eCmdIncAccent4, /* add <1l> to the default accent */
        eCmdSweepAccentAbs4, /* <1l> = new accent, <2xs> = # of beats to get there */
        eCmdSweepAccentRel4, /* <1l> = accent adjust, <2xs> = # of beats to get there */

        eCmdRestoreAccent5, /* restore accent value to master default */
        eCmdSetAccent5, /* specify the new default accent in <1l> */
        eCmdIncAccent5, /* add <1l> to the default accent */
        eCmdSweepAccentAbs5, /* <1l> = new accent, <2xs> = # of beats to get there */
        eCmdSweepAccentRel5, /* <1l> = accent adjust, <2xs> = # of beats to get there */

        eCmdRestoreAccent6, /* restore accent value to master default */
        eCmdSetAccent6, /* specify the new default accent in <1l> */
        eCmdIncAccent6, /* add <1l> to the default accent */
        eCmdSweepAccentAbs6, /* <1l> = new accent, <2xs> = # of beats to get there */
        eCmdSweepAccentRel6, /* <1l> = accent adjust, <2xs> = # of beats to get there */

        eCmdRestoreAccent7, /* restore accent value to master default */
        eCmdSetAccent7, /* specify the new default accent in <1l> */
        eCmdIncAccent7, /* add <1l> to the default accent */
        eCmdSweepAccentAbs7, /* <1l> = new accent, <2xs> = # of beats to get there */
        eCmdSweepAccentRel7, /* <1l> = accent adjust, <2xs> = # of beats to get there */

        eCmdRestoreAccent8, /* restore accent value to master default */
        eCmdSetAccent8, /* specify the new default accent in <1l> */
        eCmdIncAccent8, /* add <1l> to the default accent */
        eCmdSweepAccentAbs8, /* <1l> = new accent, <2xs> = # of beats to get there */
        eCmdSweepAccentRel8, /* <1l> = accent adjust, <2xs> = # of beats to get there */

        /* set pitch displacement depth adjustment */
        eCmdRestorePitchDispDepth, /* restore max pitch disp depth value to default */
        eCmdSetPitchDispDepth, /* set new max pitch disp depth <1l> */
        eCmdIncPitchDispDepth, /* add <1l> to the default pitch disp depth */
        eCmdSweepPitchDispDepthAbs, /* <1l> = new depth, <2xs> = # of beats */
        eCmdSweepPitchDispDepthRel, /* <1l> = depth adjust, <2xs> = # of beats */

        /* set pitch displacement rate adjustment */
        eCmdRestorePitchDispRate, /* restore max pitch disp rate to the master default */
        eCmdSetPitchDispRate, /* set new max pitch disp rate in seconds to <1l> */
        eCmdIncPitchDispRate, /* add <1l> to the default max pitch disp rate */
        eCmdSweepPitchDispRateAbs, /* <1l> = new rate, <2xs> = # of beats to get there */
        eCmdSweepPitchDispRateRel, /* <1l> = rate adjust, <2xs> = # of beats to get there */

        /* set pitch displacement start point, same way as release point */
        eCmdRestorePitchDispStart, /* restore pitch disp start point to default */
        eCmdSetPitchDispStart, /* set the start point to <1l> */
        eCmdIncPitchDispStart, /* add <1l> to the pitch disp start point */
        eCmdPitchDispStartOrigin, /* specify the origin, same as for release point <1i> */
        eCmdSweepPitchDispStartAbs, /* <1l> = new vib start, <2xs> = # of beats */
        eCmdSweepPitchDispStartRel, /* <1l> = vib adjust, <2xs> = # of beats */

        /* hurry up adjustment */
        eCmdRestoreHurryUp, /* restore default hurryup factor */
        eCmdSetHurryUp, /* set the hurryup factor to <1l> */
        eCmdIncHurryUp, /* add <1l> to the hurryup factor */
        eCmdSweepHurryUpAbs, /* <1l> = new hurryup factor, <2xs> = # of beats */
        eCmdSweepHurryUpRel, /* <1l> = hurryup adjust, <2xs> = # of beats to get there */

        /* default detune */
        eCmdRestoreDetune, /* restore the default detune factor */
        eCmdSetDetune, /* set the detune factor to <1l> */
        eCmdIncDetune, /* add <1l> to current detune factor */
        eCmdDetuneMode, /* <1i>:  -1: Hertz, 0: half-steps */
        eCmdSweepDetuneAbs, /* <1l> = new detune, <2xs> = # of beats */
        eCmdSweepDetuneRel, /* <1l> = detune adjust, <2xs> = # of beats */

        /* default early/late adjust */
        eCmdRestoreEarlyLateAdjust, /* restore the default early/late adjust value */
        eCmdSetEarlyLateAdjust, /* set the early/late adjust value to <1l> */
        eCmdIncEarlyLateAdjust, /* add <1l> to the current early/late adjust value */
        eCmdSweepEarlyLateAbs, /* <1l> = new early/late adjust, <2xs> = # of beats */
        eCmdSweepEarlyLateRel, /* <1l> = early/late delta, <2xs> = # of beats to get there */

        /* default duration adjust */
        eCmdRestoreDurationAdjust, /* restore the default duration adjust value */
        eCmdSetDurationAdjust, /* set duration adjust value to <1l> */
        eCmdIncDurationAdjust, /* add <1l> to the current duration adjust value */
        eCmdSweepDurationAbs, /* <1l> = new duration adjust, <2xs> = # of beats */
        eCmdSweepDurationRel, /* <1l> = duration adjust delta, <2xs> = # of beats */
        eCmdDurationAdjustMode, /* <1i>:  -1: Multiplicative, 0: Additive */

        /* set the meter.  this is used by the editor for placing measure bars. */
        /* measuring restarts immediately after this command */
        eCmdSetMeter, /* <1i> = numerator, <2i> = denominator */
        /* immediately change the measure number */
        eCmdSetMeasureNumber, /* <1i> = new number */

        /* set the track transpose to some number of half-steps */
        eCmdSetTranspose, /* <1i> = signed number of half-steps */
        eCmdAdjustTranspose, /* <1i> = added to the current transpose value */

        /* set 12-step frequency value, in cents */
        eCmdSetFrequencyValue, /* <1i> = 0..11 index, <2l> = normal freq * 1000 */
        eCmdSetFrequencyValueLegacy, /* <1i> = 0..11 index, <2l> = normal freq * 1000 */
        eCmdAdjustFrequencyValue, /* <1i> = 0..11 index, <2l> = scale factor * 1000 */
        eCmdAdjustFrequencyValueLegacy, /* <1i> = 0..11 index, <2l> = scale factor * 1000 */
        eCmdResetFrequencyValue, /* <1i> = 0..11 index */
        eCmdLoadFrequencyModel, // <1s> = model name, <1l> = tonic offset (integer 0..11)
        eCmdSweepFrequencyValue0Absolute, // <1l> = new frequency factor, <2xs> = # of beats to get there
        eCmdSweepFrequencyValue1Absolute, // <1l> = new frequency factor, <2xs> = # of beats to get there
        eCmdSweepFrequencyValue2Absolute, // <1l> = new frequency factor, <2xs> = # of beats to get there
        eCmdSweepFrequencyValue3Absolute, // <1l> = new frequency factor, <2xs> = # of beats to get there
        eCmdSweepFrequencyValue4Absolute, // <1l> = new frequency factor, <2xs> = # of beats to get there
        eCmdSweepFrequencyValue5Absolute, // <1l> = new frequency factor, <2xs> = # of beats to get there
        eCmdSweepFrequencyValue6Absolute, // <1l> = new frequency factor, <2xs> = # of beats to get there
        eCmdSweepFrequencyValue7Absolute, // <1l> = new frequency factor, <2xs> = # of beats to get there
        eCmdSweepFrequencyValue8Absolute, // <1l> = new frequency factor, <2xs> = # of beats to get there
        eCmdSweepFrequencyValue9Absolute, // <1l> = new frequency factor, <2xs> = # of beats to get there
        eCmdSweepFrequencyValue10Absolute, // <1l> = new frequency factor, <2xs> = # of beats to get there
        eCmdSweepFrequencyValue11Absolute, // <1l> = new frequency factor, <2xs> = # of beats to get there
        eCmdSweepFrequencyValue0Relative, // <1l> = frequency factor adjust, <2xs> = # of beats to get there
        eCmdSweepFrequencyValue1Relative, // <1l> = frequency factor adjust, <2xs> = # of beats to get there
        eCmdSweepFrequencyValue2Relative, // <1l> = frequency factor adjust, <2xs> = # of beats to get there
        eCmdSweepFrequencyValue3Relative, // <1l> = frequency factor adjust, <2xs> = # of beats to get there
        eCmdSweepFrequencyValue4Relative, // <1l> = frequency factor adjust, <2xs> = # of beats to get there
        eCmdSweepFrequencyValue5Relative, // <1l> = frequency factor adjust, <2xs> = # of beats to get there
        eCmdSweepFrequencyValue6Relative, // <1l> = frequency factor adjust, <2xs> = # of beats to get there
        eCmdSweepFrequencyValue7Relative, // <1l> = frequency factor adjust, <2xs> = # of beats to get there
        eCmdSweepFrequencyValue8Relative, // <1l> = frequency factor adjust, <2xs> = # of beats to get there
        eCmdSweepFrequencyValue9Relative, // <1l> = frequency factor adjust, <2xs> = # of beats to get there
        eCmdSweepFrequencyValue10Relative, // <1l> = frequency factor adjust, <2xs> = # of beats to get there
        eCmdSweepFrequencyValue11Relative, // <1l> = frequency factor adjust, <2xs> = # of beats to get there

        /* set and adjust effect control parameters */
        eCmdSetEffectParam1, /* specify the new default effect parameter in <1l> */
        eCmdIncEffectParam1, /* add <1l> to the default effect parameter */
        eCmdSweepEffectParamAbs1, /* <1l> = new effect parameter, <2xs> = # of beats to get there */
        eCmdSweepEffectParamRel1, /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */

        eCmdSetEffectParam2, /* specify the new default effect parameter in <1l> */
        eCmdIncEffectParam2, /* add <1l> to the default effect parameter */
        eCmdSweepEffectParamAbs2, /* <1l> = new effect parameter, <2xs> = # of beats to get there */
        eCmdSweepEffectParamRel2, /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */

        eCmdSetEffectParam3, /* specify the new default effect parameter in <1l> */
        eCmdIncEffectParam3, /* add <1l> to the default effect parameter */
        eCmdSweepEffectParamAbs3, /* <1l> = new effect parameter, <2xs> = # of beats to get there */
        eCmdSweepEffectParamRel3, /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */

        eCmdSetEffectParam4, /* specify the new default effect parameter in <1l> */
        eCmdIncEffectParam4, /* add <1l> to the default effect parameter */
        eCmdSweepEffectParamAbs4, /* <1l> = new effect parameter, <2xs> = # of beats to get there */
        eCmdSweepEffectParamRel4, /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */

        eCmdSetEffectParam5, /* specify the new default effect parameter in <1l> */
        eCmdIncEffectParam5, /* add <1l> to the default effect parameter */
        eCmdSweepEffectParamAbs5, /* <1l> = new effect parameter, <2xs> = # of beats to get there */
        eCmdSweepEffectParamRel5, /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */

        eCmdSetEffectParam6, /* specify the new default effect parameter in <1l> */
        eCmdIncEffectParam6, /* add <1l> to the default effect parameter */
        eCmdSweepEffectParamAbs6, /* <1l> = new effect parameter, <2xs> = # of beats to get there */
        eCmdSweepEffectParamRel6, /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */

        eCmdSetEffectParam7, /* specify the new default effect parameter in <1l> */
        eCmdIncEffectParam7, /* add <1l> to the default effect parameter */
        eCmdSweepEffectParamAbs7, /* <1l> = new effect parameter, <2xs> = # of beats to get there */
        eCmdSweepEffectParamRel7, /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */

        eCmdSetEffectParam8, /* specify the new default effect parameter in <1l> */
        eCmdIncEffectParam8, /* add <1l> to the default effect parameter */
        eCmdSweepEffectParamAbs8, /* <1l> = new effect parameter, <2xs> = # of beats to get there */
        eCmdSweepEffectParamRel8, /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */

        /* track effect processor enable switch */
        eCmdTrackEffectEnable, /* <1i>: -1 = enable, 0 = disable */

        /* set and adjust global score effect control parameters */
        eCmdSetScoreEffectParam1, /* specify the new default score effect parameter in <1l> */
        eCmdIncScoreEffectParam1, /* add <1l> to the default score effect parameter */
        eCmdSweepScoreEffectParamAbs1, /* <1l> = new score effect parameter, <2xs> = # of beats to get there */
        eCmdSweepScoreEffectParamRel1, /* <1l> = score effect parameter adjust, <2xs> = # of beats to get there */

        eCmdSetScoreEffectParam2, /* specify the new default score effect parameter in <1l> */
        eCmdIncScoreEffectParam2, /* add <1l> to the default score effect parameter */
        eCmdSweepScoreEffectParamAbs2, /* <1l> = new score effect parameter, <2xs> = # of beats to get there */
        eCmdSweepScoreEffectParamRel2, /* <1l> = score effect parameter adjust, <2xs> = # of beats to get there */

        eCmdSetScoreEffectParam3, /* specify the new default score effect parameter in <1l> */
        eCmdIncScoreEffectParam3, /* add <1l> to the default score effect parameter */
        eCmdSweepScoreEffectParamAbs3, /* <1l> = new score effect parameter, <2xs> = # of beats to get there */
        eCmdSweepScoreEffectParamRel3, /* <1l> = score effect parameter adjust, <2xs> = # of beats to get there */

        eCmdSetScoreEffectParam4, /* specify the new default score effect parameter in <1l> */
        eCmdIncScoreEffectParam4, /* add <1l> to the default score effect parameter */
        eCmdSweepScoreEffectParamAbs4, /* <1l> = new score effect parameter, <2xs> = # of beats to get there */
        eCmdSweepScoreEffectParamRel4, /* <1l> = score effect parameter adjust, <2xs> = # of beats to get there */

        eCmdSetScoreEffectParam5, /* specify the new default score effect parameter in <1l> */
        eCmdIncScoreEffectParam5, /* add <1l> to the default score effect parameter */
        eCmdSweepScoreEffectParamAbs5, /* <1l> = new score effect parameter, <2xs> = # of beats to get there */
        eCmdSweepScoreEffectParamRel5, /* <1l> = score effect parameter adjust, <2xs> = # of beats to get there */

        eCmdSetScoreEffectParam6, /* specify the new default score effect parameter in <1l> */
        eCmdIncScoreEffectParam6, /* add <1l> to the default score effect parameter */
        eCmdSweepScoreEffectParamAbs6, /* <1l> = new score effect parameter, <2xs> = # of beats to get there */
        eCmdSweepScoreEffectParamRel6, /* <1l> = score effect parameter adjust, <2xs> = # of beats to get there */

        eCmdSetScoreEffectParam7, /* specify the new default score effect parameter in <1l> */
        eCmdIncScoreEffectParam7, /* add <1l> to the default score effect parameter */
        eCmdSweepScoreEffectParamAbs7, /* <1l> = new score effect parameter, <2xs> = # of beats to get there */
        eCmdSweepScoreEffectParamRel7, /* <1l> = score effect parameter adjust, <2xs> = # of beats to get there */

        eCmdSetScoreEffectParam8, /* specify the new default score effect parameter in <1l> */
        eCmdIncScoreEffectParam8, /* add <1l> to the default score effect parameter */
        eCmdSweepScoreEffectParamAbs8, /* <1l> = new score effect parameter, <2xs> = # of beats to get there */
        eCmdSweepScoreEffectParamRel8, /* <1l> = score effect parameter adjust, <2xs> = # of beats to get there */

        /* text marker in the score */
        eCmdMarker, /* <string> holds the text */

        /* section effect processor enable switch */
        eCmdSectionEffectEnable, /* <1i>: -1 = enable, 0 = disable */

        /* section effect processor enable switch */
        eCmdScoreEffectEnable, /* <1i>: -1 = enable, 0 = disable */

        /* set and adjust global section effect control parameters */
        eCmdSetSectionEffectParam1, /* specify the new default section effect parameter in <1l> */
        eCmdIncSectionEffectParam1, /* add <1l> to the default section effect parameter */
        eCmdSweepSectionEffectParamAbs1, /* <1l> = new section effect parameter, <2xs> = # of beats to get there */
        eCmdSweepSectionEffectParamRel1, /* <1l> = section effect parameter adjust, <2xs> = # of beats to get there */

        eCmdSetSectionEffectParam2, /* specify the new default section effect parameter in <1l> */
        eCmdIncSectionEffectParam2, /* add <1l> to the default section effect parameter */
        eCmdSweepSectionEffectParamAbs2, /* <1l> = new section effect parameter, <2xs> = # of beats to get there */
        eCmdSweepSectionEffectParamRel2, /* <1l> = section effect parameter adjust, <2xs> = # of beats to get there */

        eCmdSetSectionEffectParam3, /* specify the new default section effect parameter in <1l> */
        eCmdIncSectionEffectParam3, /* add <1l> to the default section effect parameter */
        eCmdSweepSectionEffectParamAbs3, /* <1l> = new section effect parameter, <2xs> = # of beats to get there */
        eCmdSweepSectionEffectParamRel3, /* <1l> = section effect parameter adjust, <2xs> = # of beats to get there */

        eCmdSetSectionEffectParam4, /* specify the new default section effect parameter in <1l> */
        eCmdIncSectionEffectParam4, /* add <1l> to the default section effect parameter */
        eCmdSweepSectionEffectParamAbs4, /* <1l> = new section effect parameter, <2xs> = # of beats to get there */
        eCmdSweepSectionEffectParamRel4, /* <1l> = section effect parameter adjust, <2xs> = # of beats to get there */

        eCmdSetSectionEffectParam5, /* specify the new default section effect parameter in <1l> */
        eCmdIncSectionEffectParam5, /* add <1l> to the default section effect parameter */
        eCmdSweepSectionEffectParamAbs5, /* <1l> = new section effect parameter, <2xs> = # of beats to get there */
        eCmdSweepSectionEffectParamRel5, /* <1l> = section effect parameter adjust, <2xs> = # of beats to get there */

        eCmdSetSectionEffectParam6, /* specify the new default section effect parameter in <1l> */
        eCmdIncSectionEffectParam6, /* add <1l> to the default section effect parameter */
        eCmdSweepSectionEffectParamAbs6, /* <1l> = new section effect parameter, <2xs> = # of beats to get there */
        eCmdSweepSectionEffectParamRel6, /* <1l> = section effect parameter adjust, <2xs> = # of beats to get there */

        eCmdSetSectionEffectParam7, /* specify the new default section effect parameter in <1l> */
        eCmdIncSectionEffectParam7, /* add <1l> to the default section effect parameter */
        eCmdSweepSectionEffectParamAbs7, /* <1l> = new section effect parameter, <2xs> = # of beats to get there */
        eCmdSweepSectionEffectParamRel7, /* <1l> = section effect parameter adjust, <2xs> = # of beats to get there */

        eCmdSetSectionEffectParam8, /* specify the new default section effect parameter in <1l> */
        eCmdIncSectionEffectParam8, /* add <1l> to the default section effect parameter */
        eCmdSweepSectionEffectParamAbs8, /* <1l> = new section effect parameter, <2xs> = # of beats to get there */
        eCmdSweepSectionEffectParamRel8, /* <1l> = section effect parameter adjust, <2xs> = # of beats to get there */

        /* sequencer controls */
        eCmdSequenceBegin, /* <string> holds sequence name */
        eCmdSequenceEnd,
        eCmdSetSequence, /* <string1> holds track/group name, <string2> hold sequence name */
        eCmdSetSequenceDeferred, /* <string1> holds track/group name, <string2> hold sequence name */
        eCmdEndSequencing, /* <string1> holds track/group name */
        eCmdSkip, /* <string1> holds track/group name, <2l> holds number of beats */
        eCmdIgnoreNextCmd, /* <1l> = probability of ignoring next command */

        /* command redirection controls */
        eCmdRedirect, /* <string> holds target track/group name */
        eCmdRedirectEnd,

        /* channel release controls */
        eCmdReleaseAll1,
        eCmdReleaseAll2,
        eCmdReleaseAll3,

        /* overall portamento adjustments */
        eCmdRestorePortamento, /* restore the portamento to the default for the channel */
        eCmdSetPortamento, /* set the portamento to the specified level in <1l> */
        eCmdIncPortamento, /* multiply <1l> by the portamento control */
        eCmdSweepPortamentoAbs, /* <1l> = new portamento, <2xs> = # of beats to reach it */
        eCmdSweepPortamentoRel, /* <1l> = portamento adjust, <2xs> = # of beats to reach it */


        eCmd_End /* this is't a command */
    }

    public enum CommandAddrMode
    {
        eNoParameters,
        e1SmallExtParameter, /* <1xs> */
        e2SmallExtParameters, /* <1xs> <2xs> */
        e1LargeParameter, /* <1l> */
        eFirstLargeSecondSmallExtParameters, /* <1l> <2xs> */
        e1ParamReleaseOrigin, /* origin <1i> */
        e1PitchDisplacementMode, /* hertz/steps <1i> */
        e2IntegerParameters, /* <1i> <2i> */
        e1DurationAdjustMode, /* multiplicative/additive <1i> */
        e1IntegerParameter, /* <1i> */
        e1StringParameter, /* <string> */
        e1StringParameterWithLineFeeds, /* <string> */
        e1TrackEffectsMode, /* enable/disable <1i> */
        eFirstIntSecondLargeParameters, /* <1i> <2l> */
        e2StringParameters, /* <string1> <string2> */
        e1String1LargeBCDParameters /* <string1> <1l> */
    }

    [Flags]
    public enum InlineParamVis : uint
    {
        None = 0,

        [Description("Accent 1")]
        Accent1 = 1U << 0,
        [Description("Accent 2")]
        Accent2 = 1U << 1,
        [Description("Accent 3")]
        Accent3 = 1U << 2,
        [Description("Accent 4")]
        Accent4 = 1U << 3,
        [Description("Accent 5")]
        Accent5 = 1U << 4,
        [Description("Accent 6")]
        Accent6 = 1U << 5,
        [Description("Accent 7")]
        Accent7 = 1U << 6,
        [Description("Accent 8")]
        Accent8 = 1U << 7,

        [Description("Loudness")]
        Loudness = 1U << 8,
        [Description("Early/Late Adjust")]
        EarlyLateAdjust = 1U << 9,
        [Description("Duration Adjust")]
        DurationAdjust = 1U << 10,
        [Description("Duration Adjust Mode")]
        DurationAdjustMode = 1U << 11,
        [Description("Release Point 1")]
        ReleasePoint1 = 1U << 12,
        [Description("Release Point 1 Origin")]
        ReleasePoint1Origin = 1U << 13,
        [Description("Release Point 2")]
        ReleasePoint2 = 1U << 14,
        [Description("Release Point 2 Origin")]
        ReleasePoint2Origin = 1U << 15,
        [Description("Release Point  Origin")]
        ReleasePoint3Origin = 1U << 16,
        [Description("Portamento Duration")]
        PortamentoDuration = 1U << 17,
        [Description("Portamento Units")]
        PortamentoUnits = 1U << 18,
        [Description("Portamento Leads/Follows")]
        PortamentoLeadsFollows = 1U << 19,
        [Description("Stereo Position")]
        StereoPosition = 1U << 20,
        [Description("Hurry-Up Factor")]
        HurryUp = 1U << 21,
        [Description("Retrigger Envelopes on Tie")]
        Retrigger = 1U << 22,
        [Description("Detune Amount")]
        Detune = 1U << 23,
        [Description("Detune Units")]
        DetuneUnits = 1U << 24,
        [Description("Pitch Displacement Depth Adjust")]
        PitchDisplacementDepthAdjust = 1U << 25,
        [Description("Pitch Displacement Rate Adjust")]
        PitchDisplacementRateAdjust = 1U << 26,
        [Description("Pitch Displacement Start Point")]
        PitchDisplacementStartPoint = 1U << 27,
        [Description("Pitch Displacement Start Origin")]
        PitchDisplacementStartOrigin = 1U << 28,
        [Description("Pitch Override For Multisample Selection")]
        PitchOverrideForMultisampleSelection = 1U << 29,
        [Description("Note/Rest")]
        NoteRest = 1U << 30,
        [Description("Surround Position")]
        SurroundPosition = 1U << 31,

        MaximumExponent = 31,
    }

    public class TrackObjectRec : HierarchicalBindingBase
    {
        private string _Name = String.Empty;
        public const string Name_PropertyName = "Name";
        [Bindable(true)]
        [Searchable]
        public string Name { get { return _Name; } set { Patch(value, ref _Name, Name_PropertyName); } }


        /* defaults for per-note parameters */

        private double _DefaultEarlyLateAdjust;
        public const string DefaultEarlyLateAdjust_PropertyName = "DefaultEarlyLateAdjust";
        [Bindable(true)]
        public double DefaultEarlyLateAdjust { get { return _DefaultEarlyLateAdjust; } set { Patch(value, ref _DefaultEarlyLateAdjust, DefaultEarlyLateAdjust_PropertyName); } }

        private double _DefaultReleasePoint1;
        public const string DefaultReleasePoint1_PropertyName = "DefaultReleasePoint1";
        [Bindable(true)]
        public double DefaultReleasePoint1 { get { return _DefaultReleasePoint1; } set { Patch(value, ref _DefaultReleasePoint1, DefaultReleasePoint1_PropertyName); } }

        private NoteFlags _DefaultReleasePoint1ModeFlag = NoteFlags.eRelease1FromEnd; // More generally useful to default to from-end
        public const string DefaultReleasePoint1ModeFlag_PropertyName = "DefaultReleasePoint1ModeFlag";
        public const string DefaultReleasePoint1ModeFlag_EnumCategoryName = NoteNoteObjectRec.ReleasePoint1Origin_EnumCategoryName;
        public static Enum[] DefaultReleasePoint1ModeFlagAllowedValues { get { return new Enum[] { NoteFlags.eRelease1FromStart, NoteFlags.eRelease1FromEnd, }; } }
        [Bindable(true)]
        public NoteFlags DefaultReleasePoint1ModeFlag
        {
            get { return _DefaultReleasePoint1ModeFlag & NoteFlags.eRelease1OriginMask; }
            set { PatchFlags(ref _DefaultReleasePoint1ModeFlag, value, NoteFlags.eRelease1OriginMask, DefaultReleasePoint1ModeFlag_PropertyName); }
        }
        [Bindable(true)]
        public string DefaultReleasePoint1ModeFlagAsString
        {
            get { return EnumUtility.GetDescription(DefaultReleasePoint1ModeFlag, DefaultReleasePoint1ModeFlag_EnumCategoryName); }
            set
            {
                string old = EnumUtility.GetDescription(_DefaultReleasePoint1ModeFlag & NoteFlags.eRelease1OriginMask, DefaultReleasePoint1ModeFlag_EnumCategoryName);
                NoteFlags valueEnum = (NoteFlags)EnumUtility.GetValue(NoteFlags.eCommandFlag.GetType(), value, DefaultReleasePoint1ModeFlag_EnumCategoryName);
                PatchFlags(ref _DefaultReleasePoint1ModeFlag, valueEnum, NoteFlags.eRelease1OriginMask, DefaultReleasePoint1ModeFlag_PropertyName);
            }
        }

        private double _DefaultReleasePoint2;
        public const string DefaultReleasePoint2_PropertyName = "DefaultReleasePoint2";
        [Bindable(true)]
        public double DefaultReleasePoint2 { get { return _DefaultReleasePoint2; } set { Patch(value, ref _DefaultReleasePoint2, DefaultReleasePoint2_PropertyName); } }

        private NoteFlags _DefaultReleasePoint2ModeFlag = NoteFlags.eRelease2FromStart;
        public const string DefaultReleasePoint2ModeFlag_PropertyName = "DefaultReleasePoint2ModeFlag";
        public const string DefaultReleasePoint2ModeFlag_EnumCategoryName = NoteNoteObjectRec.ReleasePoint2Origin_EnumCategoryName;
        public static Enum[] DefaultReleasePoint2ModeFlagAllowedValues { get { return new Enum[] { NoteFlags.eRelease2FromStart, NoteFlags.eRelease2FromEnd, }; } }
        [Bindable(true)]
        public NoteFlags DefaultReleasePoint2ModeFlag
        {
            get { return _DefaultReleasePoint2ModeFlag & NoteFlags.eRelease2OriginMask; }
            set { PatchFlags(ref _DefaultReleasePoint2ModeFlag, value, NoteFlags.eRelease2OriginMask, DefaultReleasePoint2ModeFlag_PropertyName); }
        }
        [Bindable(true)]
        public string DefaultReleasePoint2ModeFlagAsString
        {
            get { return EnumUtility.GetDescription(DefaultReleasePoint2ModeFlag, DefaultReleasePoint2ModeFlag_EnumCategoryName); }
            set
            {
                string old = EnumUtility.GetDescription(_DefaultReleasePoint2ModeFlag & NoteFlags.eRelease2OriginMask, DefaultReleasePoint2ModeFlag_EnumCategoryName);
                NoteFlags valueEnum = (NoteFlags)EnumUtility.GetValue(NoteFlags.eCommandFlag.GetType(), value, DefaultReleasePoint2ModeFlag_EnumCategoryName);
                PatchFlags(ref _DefaultReleasePoint2ModeFlag, valueEnum, NoteFlags.eRelease2OriginMask, DefaultReleasePoint2ModeFlag_PropertyName);
            }
        }

        private double _DefaultOverallLoudness = 1;
        public const string DefaultOverallLoudness_PropertyName = "DefaultOverallLoudness";
        [Bindable(true)]
        public double DefaultOverallLoudness { get { return _DefaultOverallLoudness; } set { Patch(value, ref _DefaultOverallLoudness, DefaultOverallLoudness_PropertyName); } }

        private double _DefaultStereoPositioning;
        public const string DefaultStereoPositioning_PropertyName = "DefaultStereoPositioning";
        [Bindable(true)]
        public double DefaultStereoPositioning { get { return _DefaultStereoPositioning; } set { Patch(value, ref _DefaultStereoPositioning, DefaultStereoPositioning_PropertyName); } }

        private double _DefaultSurroundPositioning;
        public const string DefaultSurroundPositioning_PropertyName = "DefaultSurroundPositioning";
        [Bindable(true)]
        public double DefaultSurroundPositioning { get { return _DefaultSurroundPositioning; } set { Patch(value, ref _DefaultSurroundPositioning, DefaultSurroundPositioning_PropertyName); } }

        private double _DefaultAccent1;
        public const string DefaultAccent1_PropertyName = "DefaultAccent1";
        [Bindable(true)]
        public double DefaultAccent1 { get { return _DefaultAccent1; } set { Patch(value, ref _DefaultAccent1, DefaultAccent1_PropertyName); } }

        private double _DefaultAccent2;
        public const string DefaultAccent2_PropertyName = "DefaultAccent2";
        [Bindable(true)]
        public double DefaultAccent2 { get { return _DefaultAccent2; } set { Patch(value, ref _DefaultAccent2, DefaultAccent2_PropertyName); } }

        private double _DefaultAccent3;
        public const string DefaultAccent3_PropertyName = "DefaultAccent3";
        [Bindable(true)]
        public double DefaultAccent3 { get { return _DefaultAccent3; } set { Patch(value, ref _DefaultAccent3, DefaultAccent3_PropertyName); } }

        private double _DefaultAccent4;
        public const string DefaultAccent4_PropertyName = "DefaultAccent4";
        [Bindable(true)]
        public double DefaultAccent4 { get { return _DefaultAccent4; } set { Patch(value, ref _DefaultAccent4, DefaultAccent4_PropertyName); } }

        private double _DefaultAccent5;
        public const string DefaultAccent5_PropertyName = "DefaultAccent5";
        [Bindable(true)]
        public double DefaultAccent5 { get { return _DefaultAccent5; } set { Patch(value, ref _DefaultAccent5, DefaultAccent5_PropertyName); } }

        private double _DefaultAccent6;
        public const string DefaultAccent6_PropertyName = "DefaultAccent6";
        [Bindable(true)]
        public double DefaultAccent6 { get { return _DefaultAccent6; } set { Patch(value, ref _DefaultAccent6, DefaultAccent6_PropertyName); } }

        private double _DefaultAccent7;
        public const string DefaultAccent7_PropertyName = "DefaultAccent7";
        [Bindable(true)]
        public double DefaultAccent7 { get { return _DefaultAccent7; } set { Patch(value, ref _DefaultAccent7, DefaultAccent7_PropertyName); } }

        private double _DefaultAccent8;
        public const string DefaultAccent8_PropertyName = "DefaultAccent8";
        [Bindable(true)]
        public double DefaultAccent8 { get { return _DefaultAccent8; } set { Patch(value, ref _DefaultAccent8, DefaultAccent8_PropertyName); } }

        private double _DefaultPitchDisplacementDepthAdjust = 1;
        public const string DefaultPitchDisplacementDepthAdjust_PropertyName = "DefaultPitchDisplacementDepthAdjust";
        [Bindable(true)]
        public double DefaultPitchDisplacementDepthAdjust { get { return _DefaultPitchDisplacementDepthAdjust; } set { Patch(value, ref _DefaultPitchDisplacementDepthAdjust, DefaultPitchDisplacementDepthAdjust_PropertyName); } }

#if false // TODO: remove - apparently never implemented
        private NoteFlags _DefaultPitchDisplacementDepthAdjustModeFlag;
        public const string DefaultPitchDisplacementDepthAdjustModeFlag_PropertyName = "DefaultPitchDisplacementDepthAdjustModeFlag";
        [Bindable(true)]
        public NoteFlags DefaultPitchDisplacementDepthAdjustModeFlag { get { return _DefaultPitchDisplacementDepthAdjustModeFlag; } set { PatchObject(value, ref _DefaultPitchDisplacementDepthAdjustModeFlag, DefaultPitchDisplacementDepthAdjustModeFlag_PropertyName); } }
#endif

        private double _DefaultPitchDisplacementRateAdjust = 1;
        public const string DefaultPitchDisplacementRateAdjust_PropertyName = "DefaultPitchDisplacementRateAdjust";
        [Bindable(true)]
        public double DefaultPitchDisplacementRateAdjust { get { return _DefaultPitchDisplacementRateAdjust; } set { Patch(value, ref _DefaultPitchDisplacementRateAdjust, DefaultPitchDisplacementRateAdjust_PropertyName); } }

        private double _DefaultPitchDisplacementStartPoint;
        public const string DefaultPitchDisplacementStartPoint_PropertyName = "DefaultPitchDisplacementStartPoint";
        [Bindable(true)]
        public double DefaultPitchDisplacementStartPoint { get { return _DefaultPitchDisplacementStartPoint; } set { Patch(value, ref _DefaultPitchDisplacementStartPoint, DefaultPitchDisplacementStartPoint_PropertyName); } }

        private NoteFlags _DefaultPitchDisplacementStartPointModeFlag = NoteFlags.ePitchDisplacementStartFromStart;
        public const string DefaultPitchDisplacementStartPointModeFlag_PropertyName = "DefaultPitchDisplacementStartPointModeFlag";
        public const string DefaultPitchDisplacementStartPointModeFlag_EnumCategoryName = NoteNoteObjectRec.PitchDisplacementOrigin_EnumCategoryName;
        public static Enum[] DefaultPitchDisplacementStartPointModeFlagAllowedValues { get { return new Enum[] { NoteFlags.ePitchDisplacementStartFromStart, NoteFlags.ePitchDisplacementStartFromEnd, }; } }
        [Bindable(true)]
        public NoteFlags DefaultPitchDisplacementStartPointModeFlag
        {
            get { return _DefaultPitchDisplacementStartPointModeFlag & NoteFlags.ePitchDisplacementStartOriginMask; }
            set { PatchFlags(ref _DefaultPitchDisplacementStartPointModeFlag, value, NoteFlags.ePitchDisplacementStartOriginMask, DefaultPitchDisplacementStartPointModeFlag_PropertyName); }
        }
        [Bindable(true)]
        public string DefaultPitchDisplacementStartPointModeFlagAsString
        {
            get { return EnumUtility.GetDescription(DefaultPitchDisplacementStartPointModeFlag, DefaultPitchDisplacementStartPointModeFlag_EnumCategoryName); }
            set
            {
                string old = EnumUtility.GetDescription(_DefaultPitchDisplacementStartPointModeFlag & NoteFlags.ePitchDisplacementStartOriginMask, DefaultPitchDisplacementStartPointModeFlag_EnumCategoryName);
                NoteFlags valueEnum = (NoteFlags)EnumUtility.GetValue(NoteFlags.eCommandFlag.GetType(), value, DefaultPitchDisplacementStartPointModeFlag_EnumCategoryName);
                PatchFlags(ref _DefaultPitchDisplacementStartPointModeFlag, valueEnum, NoteFlags.ePitchDisplacementStartOriginMask, DefaultPitchDisplacementStartPointModeFlag_PropertyName);
            }
        }

        private double _DefaultHurryUpFactor = 1;
        public const string DefaultHurryUpFactor_PropertyName = "DefaultHurryUpFactor";
        [Bindable(true)]
        public double DefaultHurryUpFactor { get { return _DefaultHurryUpFactor; } set { Patch(value, ref _DefaultHurryUpFactor, DefaultHurryUpFactor_PropertyName); } }

        private double _DefaultDetune;
        public const string DefaultDetune_PropertyName = "DefaultDetune";
        [Bindable(true)]
        public double DefaultDetune { get { return _DefaultDetune; } set { Patch(value, ref _DefaultDetune, DefaultDetune_PropertyName); } }

        private NoteFlags _DefaultDetuneModeFlag = NoteFlags.eDetuningModeHalfSteps;
        public const string DefaultDetuneModeFlag_PropertyName = "DefaultDetuneModeFlag";
        public const string DefaultDetuneModeFlag_EnumCategoryName = NoteNoteObjectRec.DetuningMode_EnumCategoryName;
        public static Enum[] DefaultDetuneModeFlagAllowedValues { get { return new Enum[] { NoteFlags.eDetuningModeHalfSteps, NoteFlags.eDetuningModeHertz, }; } }
        [Bindable(true)]
        public NoteFlags DefaultDetuneModeFlag
        {
            get { return _DefaultDetuneModeFlag & NoteFlags.eDetuningModeMask; }
            set { PatchFlags(ref _DefaultDetuneModeFlag, value, NoteFlags.eDetuningModeMask, DefaultDetuneModeFlag_PropertyName); }
        }
        [Bindable(true)]
        public string DefaultDetuneModeFlagAsString
        {
            get { return EnumUtility.GetDescription(DefaultDetuneModeFlag, DefaultDetuneModeFlag_EnumCategoryName); }
            set
            {
                string old = EnumUtility.GetDescription(_DefaultDetuneModeFlag & NoteFlags.eDetuningModeMask, DefaultDetuneModeFlag_EnumCategoryName);
                NoteFlags valueEnum = (NoteFlags)EnumUtility.GetValue(NoteFlags.eCommandFlag.GetType(), value, DefaultDetuneModeFlag_EnumCategoryName);
                PatchFlags(ref _DefaultDetuneModeFlag, valueEnum, NoteFlags.eDetuningModeMask, DefaultDetuneModeFlag_PropertyName);
            }
        }

        private double _DefaultDuration;
        public const string DefaultDuration_PropertyName = "DefaultDuration";
        [Bindable(true)]
        public double DefaultDuration { get { return _DefaultDuration; } set { Patch(value, ref _DefaultDuration, DefaultDuration_PropertyName); } }

        private NoteFlags _DefaultDurationModeFlag = NoteFlags.eDurationAdjustAdditive;
        public const string DefaultDurationModeFlag_PropertyName = "DefaultDurationModeFlag";
        public const string DefaultDurationModeFlag_EnumCategoryName = NoteNoteObjectRec.DurationAdjustMode_EnumCategoryName;
        public static Enum[] DefaultDurationModeFlagAllowedValues { get { return new Enum[] { NoteFlags.eDurationAdjustAdditive, NoteFlags.eDurationAdjustMultiplicative, }; } }
        [Bindable(true)]
        public NoteFlags DefaultDurationModeFlag
        {
            get { return _DefaultDurationModeFlag & NoteFlags.eDurationAdjustMask; }
            set { PatchFlags(ref _DefaultDurationModeFlag, value, NoteFlags.eDurationAdjustMask, DefaultDurationModeFlag_PropertyName); }
        }
        [Bindable(true)]
        public string DefaultDurationModeFlagAsString
        {
            get { return EnumUtility.GetDescription(DefaultDurationModeFlag, DefaultDurationModeFlag_EnumCategoryName); }
            set
            {
                string old = EnumUtility.GetDescription(_DefaultDurationModeFlag & NoteFlags.eDurationAdjustMask, DefaultDurationModeFlag_EnumCategoryName);
                NoteFlags valueEnum = (NoteFlags)EnumUtility.GetValue(NoteFlags.eCommandFlag.GetType(), value, DefaultDurationModeFlag_EnumCategoryName);
                PatchFlags(ref _DefaultDurationModeFlag, valueEnum, NoteFlags.eDurationAdjustMask, DefaultDurationModeFlag_PropertyName);
            }
        }


        private bool _IncludeThisTrackInFinalPlayback = true;
        public const string IncludeThisTrackInFinalPlayback_PropertyName = "IncludeThisTrackInFinalPlayback";
        [Bindable(true)]
        public bool IncludeThisTrackInFinalPlayback { get { return _IncludeThisTrackInFinalPlayback; } set { Patch(value, ref _IncludeThisTrackInFinalPlayback, IncludeThisTrackInFinalPlayback_PropertyName); } }

        private bool _MultiInstrument;
        public const string MultiInstrument_PropertyName = "MultiInstrument";
        [Bindable(true)]
        public bool MultiInstrument { get { return _MultiInstrument; } set { Patch(value, ref _MultiInstrument, MultiInstrument_PropertyName); } }


        private string _InstrumentName = String.Empty;
        public const string InstrumentName_PropertyName = "InstrumentName";
        [Bindable(true)]
        [Searchable]
        public string InstrumentName { get { return _InstrumentName; } set { Patch(value, ref _InstrumentName, InstrumentName_PropertyName); } }

        private MyBindingList<FrameObjectRec> _FrameArray = new MyBindingList<FrameObjectRec>();
        public const string FrameArray_PropertyName = "FrameArray";
        [Bindable(true)]
        [Searchable]
        public MyBindingList<FrameObjectRec> FrameArray { get { return _FrameArray; } }

        private MyBindingList<TrackObjectRec> _BackgroundObjects = new MyBindingList<TrackObjectRec>();
        public const string BackgroundObjects_PropertyName = "BackgroundObjects";
        [Bindable(true)]
        public MyBindingList<TrackObjectRec> BackgroundObjects { get { return _BackgroundObjects; } }


        private short _SavedWindowXLoc;
        public const string SavedWindowXLoc_PropertyName = "SavedWindowXLoc";
        [Bindable(true)]
        public short SavedWindowXLoc { get { return _SavedWindowXLoc; } set { Patch(value, ref _SavedWindowXLoc, SavedWindowXLoc_PropertyName, false/*modified*/); } }

        private short _SavedWindowYLoc;
        public const string SavedWindowYLoc_PropertyName = "SavedWindowYLoc";
        [Bindable(true)]
        public short SavedWindowYLoc { get { return _SavedWindowYLoc; } set { Patch(value, ref _SavedWindowYLoc, SavedWindowYLoc_PropertyName, false/*modified*/); } }

        private short _SavedWindowWidth;
        public const string SavedWindowWidth_PropertyName = "SavedWindowWidth";
        [Bindable(true)]
        public short SavedWindowWidth { get { return _SavedWindowWidth; } set { Patch(value, ref _SavedWindowWidth, SavedWindowWidth_PropertyName, false/*modified*/); } }

        private short _SavedWindowHeight;
        public const string SavedWindowHeight_PropertyName = "SavedWindowHeight";
        [Bindable(true)]
        public short SavedWindowHeight { get { return _SavedWindowHeight; } set { Patch(value, ref _SavedWindowHeight, SavedWindowHeight_PropertyName, false/*modified*/); } }


        private InlineParamVis _inlineParamVis;
        public const string InlineParamVis_PropertyName = "InlineParamVis";
        [Bindable(true)]
        public InlineParamVis InlineParamVis { get { return _inlineParamVis; } set { PatchObject(value, ref _inlineParamVis, InlineParamVis_PropertyName, false/*modified*/); } }


        // the H and V scroll offsets are not persisted

        private int _SavedHScrollPos = -1; /* -1 means "don't set; use default" */
        public const string SavedHScrollPos_PropertyName = "SavedHScrollPos";
        [Bindable(true)]
        public int SavedHScrollPos { get { return _SavedHScrollPos; } set { Patch(value, ref _SavedHScrollPos, SavedHScrollPos_PropertyName, false/*modified*/); } }

        private int _SavedVScrollPos = -1; /* -1 means "don't set; use default" */
        public const string SavedVScrollPos_PropertyName = "SavedVScrollPos";
        [Bindable(true)]
        public int SavedVScrollPos { get { return _SavedVScrollPos; } set { Patch(value, ref _SavedVScrollPos, SavedVScrollPos_PropertyName, false/*modified*/); } }


        private SectionObjectRec _Section; // cross-reference
        public const string Section_PropertyName = "Section";
        [Bindable(true)]
        public SectionObjectRec Section { get { return _Section; } set { PatchObject(value, ref _Section, Section_PropertyName); } }


        // nonpersisted properties

        public int AuxVal; // used by playback - breaks encapsulation for the sake of code brevity




        private void PatchFlags(ref NoteFlags _flags, NoteFlags value, NoteFlags mask, string propertyName)
        {
            Debug.Assert((value & ~mask) == 0);
            PatchObject((NoteFlags)(((int)_flags & ~(int)mask) | (int)value), ref _flags, propertyName);
        }

        // fired on changes to note properties
        public event PropertyChangedEventHandler FrameArrayChanged;

        protected override void NotifyFromChild(string propertyName, bool modified)
        {
            base.NotifyFromChild(propertyName, modified);
            if ((propertyName == FrameArray_PropertyName) && (FrameArrayChanged != null))
            {
                FrameArrayChanged.Invoke(this, new PropertyChangedEventArgs(FrameArray_PropertyName));
            }
        }




        public TrackObjectRec(Document document)
            : base(document, Document.TrackList_PropertyName)
        {
        }

        public TrackObjectRec(BinaryReader reader, LoadContext loadContext)
            : this(loadContext.document)
        {
            /*   1-byte format version number */
            /*       should be 1, 2, 3, 4, 5, or 6 */
            int FormatVersionNumber = reader.ReadByte();
            if ((FormatVersionNumber < 1) || (FormatVersionNumber > 6))
            {
                throw new InvalidDataException();
            }

            /*   2-byte little endian window X position (signed; from top-left corner of screen) */
            SavedWindowXLoc = reader.ReadInt16();
            /*   2-byte little endian window Y position */
            SavedWindowYLoc = reader.ReadInt16();
            /*   2-byte little endian window width */
            SavedWindowWidth = reader.ReadInt16();
            /*   2-byte little endian window height */
            SavedWindowHeight = reader.ReadInt16();

            /*   4-byte little endian track name length descriptor */
            /*   n-byte track name string (line feed = 0x0a) */
            Name = loadContext.Utf8 ? reader.ReadString4Utf8() : reader.ReadString4Ansi();

            /*   4-byte little endian large integer coded decimal default early/late adjust */
            /*       large integer coded decimal is decimal * 1000000 with a */
            /*       range of -1999.999999 to 1999.999999 */
            _DefaultEarlyLateAdjust = (double)reader.ReadLBCD();

            /*   4-byte little endian large integer coded decimal default release point 1 */
            _DefaultReleasePoint1 = (double)reader.ReadLBCD();

            /*   1-byte default release point 1 mode flag */
            /*       0 = release from start */
            /*       1 = release from end */
            switch (reader.ReadByte())
            {
                default:
                    throw new InvalidDataException();
                case 0:
                    _DefaultReleasePoint1ModeFlag = NoteFlags.eRelease1FromStart;
                    break;
                case 1:
                    _DefaultReleasePoint1ModeFlag = NoteFlags.eRelease1FromEnd;
                    break;
            }

            /*   4-byte little endian large integer coded decimal default release point 2 */
            _DefaultReleasePoint2 = (double)reader.ReadLBCD();

            /*   1-byte default release point 2 mode flag */
            /*       0 = release from start */
            /*       1 = release from end */
            switch (reader.ReadByte())
            {
                default:
                    throw new InvalidDataException();
                case 0:
                    _DefaultReleasePoint2ModeFlag = NoteFlags.eRelease2FromStart;
                    break;
                case 1:
                    _DefaultReleasePoint2ModeFlag = NoteFlags.eRelease2FromEnd;
                    break;
            }

            /*   4-byte little endian large integer coded decimal default overall loudness */
            _DefaultOverallLoudness = (double)reader.ReadLBCD();

            /*   4-byte little endian large integer coded decimal default stereo positioning */
            _DefaultStereoPositioning = (double)reader.ReadLBCD();

            /*   4-byte little endian large integer coded decimal default surround positioning */
            _DefaultSurroundPositioning = (double)reader.ReadLBCD();

            /*   4-byte little endian large integer coded decimal default accent 1 */
            _DefaultAccent1 = (double)reader.ReadLBCD();

            /*   4-byte little endian large integer coded decimal default accent 2 */
            _DefaultAccent2 = (double)reader.ReadLBCD();

            /*   4-byte little endian large integer coded decimal default accent 3 */
            _DefaultAccent3 = (double)reader.ReadLBCD();

            /*   4-byte little endian large integer coded decimal default accent 4 */
            _DefaultAccent4 = (double)reader.ReadLBCD();

            if (FormatVersionNumber >= 3)
            {
                /*   4-byte little endian large integer coded decimal default accent 5 */
                /*       only with version 3 or later */
                _DefaultAccent5 = (double)reader.ReadLBCD();

                /*   4-byte little endian large integer coded decimal default accent 6 */
                /*       only with version 3 or later */
                _DefaultAccent6 = (double)reader.ReadLBCD();

                /*   4-byte little endian large integer coded decimal default accent 7 */
                /*       only with version 3 or later */
                _DefaultAccent7 = (double)reader.ReadLBCD();

                /*   4-byte little endian large integer coded decimal default accent 8 */
                /*       only with version 3 or later */
                _DefaultAccent8 = (double)reader.ReadLBCD();
            }

            /*   4-byte little endian large integer coded decimal default pitch disp depth adjust */
            _DefaultPitchDisplacementDepthAdjust = (double)reader.ReadLBCD();

            /*   1-byte default pitch displacement depth adjust mode flag */
            /*       0 = half steps */
            /*       1 = hertz */
            /*       only in version 1 format */
            if (FormatVersionNumber == 1)
            {
                reader.ReadByte(); // discard
            }

            /*   4-byte little endian large integer coded decimal default pitch disp rate adjust */
            _DefaultPitchDisplacementRateAdjust = (double)reader.ReadLBCD();

            /*   4-byte little endian large integer coded decimal default pitch disp start point */
            _DefaultPitchDisplacementStartPoint = (double)reader.ReadLBCD();

            /*   1-byte default pitch displacement start point mode flag */
            /*       0 = pitch displacement point from start */
            /*       1 = pitch displacement point from end */
            switch (reader.ReadByte())
            {
                default:
                    throw new InvalidDataException();
                case 0:
                    _DefaultPitchDisplacementStartPointModeFlag = NoteFlags.ePitchDisplacementStartFromStart;
                    break;
                case 1:
                    _DefaultPitchDisplacementStartPointModeFlag = NoteFlags.ePitchDisplacementStartFromEnd;
                    break;
            }

            /*   4-byte little endian large integer coded decimal default hurry-up factor */
            _DefaultHurryUpFactor = (double)reader.ReadLBCD();

            /*   4-byte little endian large integer coded decimal default detuning */
            _DefaultDetune = (double)reader.ReadLBCD();

            /*   1-byte default detuning mode flag */
            /*       0 = half steps */
            /*       1 = hertz */
            switch (reader.ReadByte())
            {
                default:
                    throw new InvalidDataException();
                case 0:
                    _DefaultDetuneModeFlag = NoteFlags.eDetuningModeHalfSteps;
                    break;
                case 1:
                    _DefaultDetuneModeFlag = NoteFlags.eDetuningModeHertz;
                    break;
            }

            /*   4-byte little endian large integer coded decimal default duration */
            _DefaultDuration = (double)reader.ReadLBCD();

            /*   1-byte default duration mode flag */
            /*       0 = duration adjust is multiplicative */
            /*       1 = duration adjust is additive */
            switch (reader.ReadByte())
            {
                default:
                    throw new InvalidDataException();
                case 0:
                    _DefaultDurationModeFlag = NoteFlags.eDurationAdjustMultiplicative;
                    break;
                case 1:
                    _DefaultDurationModeFlag = NoteFlags.eDurationAdjustAdditive;
                    break;
            }

            /*   1-byte flag for playback inclusion */
            /*       0 = don't play track in final playback */
            /*       1 = do play track in final playback */
            switch (reader.ReadByte())
            {
                default:
                    throw new InvalidDataException();
                case 0:
                    _IncludeThisTrackInFinalPlayback = false;
                    break;
                case 1:
                    _IncludeThisTrackInFinalPlayback = true;
                    break;
            }

            /*   4-byte little endian instrument name string length descriptor */
            /*   n-byte instrument name string (line feed = 0x0a) */
            _InstrumentName = loadContext.Utf8 ? reader.ReadString4Utf8() : reader.ReadString4Ansi();

            if (FormatVersionNumber <= 2)
            {
                /*   1-byte flag for channel post processing enabling */
                /*       only in format versions 1 and 2 */
                /*       0 = don't do channel postprocessing */
                /*       1 = do channel postprocessing */
                int i = reader.ReadByte();
                if ((i != 0) && (i != 1))
                {
                    throw new InvalidDataException();
                }

                /*   4-byte little endian postprocessing expression length descriptor */
                /*       only in format versions 1 and 2 */
                /*   n-bytes of postprocessing stuff (line feed = 0x0a) */
                /*       only in format versions 1 and 2 */
                reader.ReadString4Ansi(); // always ANSI - removed before UTF8 was added
            }

            /*   n-bytes of data for note array */
            ReadNoteVector(reader, loadContext);

            /*   4-byte signed little endian section index descriptor (-1 .. #sections-1) */
            /*       only in format versions 4 and later */
            if (FormatVersionNumber >= 4)
            {
                bool forceIntoDefaultSection = (loadContext.state != LoadContextState.Load);

                int i = reader.ReadInt32();
                if (forceIntoDefaultSection)
                {
                    /* force to default section -- used for pasting from clipboard */
                    i = -1;
                }
                if ((i < -1) || (i >= loadContext.document.SectionList.Count))
                {
                    throw new InvalidDataException();
                }

                if (i >= 0)
                {
                    _Section = loadContext.document.SectionList[i];
                }
            }

            /*   1-byte flag for multi-instrument track */
            /*       0 = only one instrument */
            /*       1 = multiple instruments */
            /*       only in format versions 5 and later */
            if (FormatVersionNumber >= 5)
            {
                switch (reader.ReadByte())
                {
                    default:
                        throw new InvalidDataException();
                    case 0:
                        _MultiInstrument = false;
                        break;
                    case 1:
                        _MultiInstrument = true;
                        break;
                }
            }

            // 4-byte bitflags of which note properties are visible in the inline property strip editor
            if (FormatVersionNumber >= 6)
            {
                _inlineParamVis = (InlineParamVis)reader.ReadUInt32();
            }
        }

        public void ReadNoteVector(BinaryReader reader, LoadContext loadContext)
        {
            /*   1-byte format version number */
            /*       should be 1, 2, 3, or 4 */
            int FormatVersionNumber = reader.ReadByte();
            if ((FormatVersionNumber != 1) && (FormatVersionNumber != 2) && (FormatVersionNumber != 3) && (FormatVersionNumber != 4))
            {
                throw new InvalidDataException();
            }

            /*   4-byte little endian number of frames in the vector */
            int NumberOfFrames = reader.ReadInt32();
            if (NumberOfFrames < 0)
            {
                throw new InvalidDataException();
            }

            /*   * for each frame: */
            /*       4-byte little endian number of notes in the frame */
            /*       n-bytes of data for all of the notes */
            for (int iFrame = 0; iFrame < NumberOfFrames; iFrame++)
            {
                /* read number of notes in this frame */
                /*          in versions 1, 2, and 3, this was a 4-byte little endian number */
                /*          in version 4 and up, it is a delta-code */
                uint NumberOfNotes;
                if (FormatVersionNumber >= 4)
                {
                    NumberOfNotes = reader.ReadUInt32Delta();
                }
                else
                {
                    /* old versions (1-3) */
                    NumberOfNotes = reader.ReadUInt32();
                }

                FrameObjectRec FrameObject = new FrameObjectRec();

                /* read notes in the frame */
                for (int iNote = 0; iNote < NumberOfNotes; iNote++)
                {
                    NoteObjectRec Note = NoteObjectRec.Create(reader, FormatVersionNumber, this, loadContext);

                    /* make sure we aren't mixing notes and commands */
                    if (((FrameObject.Count > 1) && Note.IsItACommand) || FrameObject.IsThisACommandFrame)
                    {
                        throw new InvalidDataException();
                    }

                    FrameObject.Add(Note);
                }

                _FrameArray.Add(FrameObject);
            }

            /*   4-byte little endian number of records in the tie matrix */
            int NumberOfTieRecords = reader.ReadInt32();
            if (NumberOfTieRecords < 0)
            {
                throw new InvalidDataException();
            }

            /* thing for keeping track of ties & breaking redundancy */
            List<NoteObjectRec> TieTargetArray = new List<NoteObjectRec>(NumberOfTieRecords);

            /*   * for each tie matrix entry: */
            /*       for versions 1, 2, and 3: */
            /*         4-byte little endian index of the source frame */
            /*         4-byte little endian index of the source note in the frame */
            /*         4-byte little endian index of the target frame */
            /*         4-byte little endian index of the target note in the frame */
            /*       for version 4: */
            /*         delta-coded index of the source frame */
            /*         delta-coded index of the source note in the frame */
            /*         delta-coded index of the target frame */
            /*         delta-coded index of the target note in the frame */
            /*       targets should be unique.  starting with version 3 we are */
            /*       enforcing uniqueness */
            for (int i = 0; i < NumberOfTieRecords; i++)
            {
                uint SourceFrameIndex;
                uint SourceNoteIndex;
                uint TargetFrameIndex;
                uint TargetNoteIndex;
                if (FormatVersionNumber < 4)
                {
                    SourceFrameIndex = reader.ReadUInt32();
                    SourceNoteIndex = reader.ReadUInt32();
                    TargetFrameIndex = reader.ReadUInt32();
                    TargetNoteIndex = reader.ReadUInt32();
                }
                else /* FormatVersionNumber >= 4 */
                {
                    SourceFrameIndex = reader.ReadUInt32Delta();
                    SourceNoteIndex = reader.ReadUInt32Delta();
                    TargetFrameIndex = reader.ReadUInt32Delta();
                    TargetNoteIndex = reader.ReadUInt32Delta();
                }

                /* make sure indices are valid */
                if ((TargetFrameIndex <= SourceFrameIndex)
                    || (TargetFrameIndex >= _FrameArray.Count)
                    || (SourceFrameIndex >= _FrameArray.Count))
                {
                    throw new InvalidDataException();
                }

                /* get frames */
                FrameObjectRec SourceFrame = _FrameArray[(int)SourceFrameIndex];
                FrameObjectRec TargetFrame = _FrameArray[(int)TargetFrameIndex];

                /* make sure indices are valid */
                if ((SourceNoteIndex >= SourceFrame.Count)
                    || (TargetNoteIndex >= TargetFrame.Count))
                {
                    throw new InvalidDataException();
                }

                /* get the tie notes */
                NoteObjectRec SourceNote = SourceFrame[(int)SourceNoteIndex];
                NoteObjectRec TargetNote = TargetFrame[(int)TargetNoteIndex];

                /* make sure tie doesn't involve commands */
                if (SourceNote.IsItACommand || TargetNote.IsItACommand)
                {
                    if (FormatVersionNumber < 3)
                    {
                        /* for versions previous to 3, we did not prevent ties involving */
                        /* commands.  we will silently ignore these ties. */
                        goto SkipTieTargetSettingPoint;
                    }
                    else
                    {
                        /* for version 3 and later, we will report this as an error */
                        throw new InvalidDataException();
                    }
                }

                /* make sure tie isn't redundant */
                if (TieTargetArray.IndexOf(TargetNote) >= 0)
                {
                    if (FormatVersionNumber < 3)
                    {
                        /* for files prior to version 3, we 'fix' multiple ties to the */
                        /* same note by ignoring all but the first. */
                        goto SkipTieTargetSettingPoint;
                    }
                    else
                    {
                        /* for version 3 files and later, we report this as an error. */
                        throw new InvalidDataException();
                    }
                }

                /* save target to avoid redundant ties */
                TieTargetArray.Add(TargetNote);

                ((NoteNoteObjectRec)SourceNote).PutNoteTieTarget((NoteNoteObjectRec)TargetNote);

            /* skip here when not putting tie */
            SkipTieTargetSettingPoint:
                ;
            }
        }

        public void LoadBackgroundTrackInfo(BinaryReader reader, IList<TrackObjectRec> TrackList)
        {
            // 4-byte little endian count of entries
            int c = reader.ReadInt32();

            for (int i = 0; i < c; i++)
            {
                // 4-byte little endian index
                int index = reader.ReadInt32();
                if ((index < 0) || (index >= TrackList.Count))
                {
                    throw new InvalidDataException();
                }
                TrackObjectRec OneToBackground = TrackList[index];
                if (_BackgroundObjects.IndexOf(OneToBackground) >= 0)
                {
                    throw new InvalidDataException(); /* can't have multiple ones */
                }
                _BackgroundObjects.Add(OneToBackground);
            }
        }

        public static TrackObjectRec Create(BinaryReader reader, LoadContext loadContext)
        {
            return new TrackObjectRec(reader, loadContext);
        }

        public void Save(BinaryWriter writer, SaveContext saveContext)
        {
            /*   1-byte format version number */
            /*       should be 1, 2, 3, 4, 5, or 6 */
            writer.WriteByte(6);

            /*   2-byte little endian window X position (signed; from top-left corner of screen) */
            writer.WriteInt16(SavedWindowXLoc);
            /*   2-byte little endian window Y position */
            writer.WriteInt16(SavedWindowYLoc);
            /*   2-byte little endian window width */
            writer.WriteInt16(SavedWindowWidth);
            /*   2-byte little endian window height */
            writer.WriteInt16(SavedWindowHeight);

            /*   4-byte little endian track name length descriptor */
            /*   n-byte track name string (line feed = 0x0a) */
            writer.WriteString4Utf8(Name);

            /*   4-byte little endian large integer coded decimal default early/late adjust */
            /*       large integer coded decimal is decimal * 1000000 with a */
            /*       range of -1999.999999 to 1999.999999 */
            writer.WriteLBCD((LargeBCDType)_DefaultEarlyLateAdjust);

            /*   4-byte little endian large integer coded decimal default release point 1 */
            writer.WriteLBCD((LargeBCDType)_DefaultReleasePoint1);

            /*   1-byte default release point 1 mode flag */
            /*       0 = release from start */
            /*       1 = release from end */
            switch (_DefaultReleasePoint1ModeFlag)
            {
                default:
                    throw new ArgumentException();
                case NoteFlags.eRelease1FromStart:
                    writer.WriteByte(0);
                    break;
                case NoteFlags.eRelease1FromEnd:
                    writer.WriteByte(1);
                    break;
            }

            /*   4-byte little endian large integer coded decimal default release point 2 */
            writer.WriteLBCD((LargeBCDType)_DefaultReleasePoint2);

            /*   1-byte default release point 2 mode flag */
            /*       0 = release from start */
            /*       1 = release from end */
            switch (_DefaultReleasePoint2ModeFlag)
            {
                default:
                    throw new ArgumentException();
                case NoteFlags.eRelease2FromStart:
                    writer.WriteByte(0);
                    break;
                case NoteFlags.eRelease2FromEnd:
                    writer.WriteByte(1);
                    break;
            }

            /*   4-byte little endian large integer coded decimal default overall loudness */
            writer.WriteLBCD((LargeBCDType)_DefaultOverallLoudness);

            /*   4-byte little endian large integer coded decimal default stereo positioning */
            writer.WriteLBCD((LargeBCDType)_DefaultStereoPositioning);

            /*   4-byte little endian large integer coded decimal default surround positioning */
            writer.WriteLBCD((LargeBCDType)_DefaultSurroundPositioning);

            /*   4-byte little endian large integer coded decimal default accent 1 */
            writer.WriteLBCD((LargeBCDType)_DefaultAccent1);

            /*   4-byte little endian large integer coded decimal default accent 2 */
            writer.WriteLBCD((LargeBCDType)_DefaultAccent2);

            /*   4-byte little endian large integer coded decimal default accent 3 */
            writer.WriteLBCD((LargeBCDType)_DefaultAccent3);

            /*   4-byte little endian large integer coded decimal default accent 4 */
            writer.WriteLBCD((LargeBCDType)_DefaultAccent4);

            /*   4-byte little endian large integer coded decimal default accent 5 */
            /*       only with version 3 or later */
            writer.WriteLBCD((LargeBCDType)_DefaultAccent5);

            /*   4-byte little endian large integer coded decimal default accent 6 */
            /*       only with version 3 or later */
            writer.WriteLBCD((LargeBCDType)_DefaultAccent6);

            /*   4-byte little endian large integer coded decimal default accent 7 */
            /*       only with version 3 or later */
            writer.WriteLBCD((LargeBCDType)_DefaultAccent7);

            /*   4-byte little endian large integer coded decimal default accent 8 */
            /*       only with version 3 or later */
            writer.WriteLBCD((LargeBCDType)_DefaultAccent8);

            /*   4-byte little endian large integer coded decimal default pitch disp depth adjust */
            writer.WriteLBCD((LargeBCDType)_DefaultPitchDisplacementDepthAdjust);

            /*   4-byte little endian large integer coded decimal default pitch disp rate adjust */
            writer.WriteLBCD((LargeBCDType)_DefaultPitchDisplacementRateAdjust);

            /*   4-byte little endian large integer coded decimal default pitch disp start point */
            writer.WriteLBCD((LargeBCDType)_DefaultPitchDisplacementStartPoint);

            /*   1-byte default pitch displacement start point mode flag */
            /*       0 = pitch displacement point from start */
            /*       1 = pitch displacement point from end */
            switch (_DefaultPitchDisplacementStartPointModeFlag)
            {
                default:
                    throw new ArgumentException();
                case NoteFlags.ePitchDisplacementStartFromStart:
                    writer.WriteByte(0);
                    break;
                case NoteFlags.ePitchDisplacementStartFromEnd:
                    writer.WriteByte(1);
                    break;
            }

            /*   4-byte little endian large integer coded decimal default hurry-up factor */
            writer.WriteLBCD((LargeBCDType)_DefaultHurryUpFactor);

            /*   4-byte little endian large integer coded decimal default detuning */
            writer.WriteLBCD((LargeBCDType)_DefaultDetune);

            /*   1-byte default detuning mode flag */
            /*       0 = half steps */
            /*       1 = hertz */
            switch (_DefaultDetuneModeFlag)
            {
                default:
                    throw new ArgumentException();
                case NoteFlags.eDetuningModeHalfSteps:
                    writer.WriteByte(0);
                    break;
                case NoteFlags.eDetuningModeHertz:
                    writer.WriteByte(1);
                    break;
            }

            /*   4-byte little endian large integer coded decimal default duration */
            writer.WriteLBCD((LargeBCDType)_DefaultDuration);

            /*   1-byte default duration mode flag */
            /*       0 = duration adjust is multiplicative */
            /*       1 = duration adjust is additive */
            switch (_DefaultDurationModeFlag)
            {
                default:
                    throw new ArgumentException();
                case NoteFlags.eDurationAdjustMultiplicative:
                    writer.WriteByte(0);
                    break;
                case NoteFlags.eDurationAdjustAdditive:
                    writer.WriteByte(1);
                    break;
            }

            /*   1-byte flag for playback inclusion */
            /*       0 = don't play track in final playback */
            /*       1 = do play track in final playback */
            writer.WriteByte(_IncludeThisTrackInFinalPlayback ? (byte)1 : (byte)0);

            /*   4-byte little endian instrument name string length descriptor */
            /*   n-byte instrument name string (line feed = 0x0a) */
            writer.WriteString4Utf8(_InstrumentName);

            /*   n-bytes of data for note array */
            WriteNoteVector(writer);

            /*   4-byte signed little endian section index descriptor (-1 .. #sections-1) */
            /*       only in format versions 4 and later */
            if (_Section == null)
            {
                writer.WriteInt32(-1);
            }
            else
            {
                int i = saveContext.document.SectionList.IndexOf(_Section);
                if (i < 0)
                {
                    throw new ArgumentException();
                }
                writer.WriteInt32(i);
            }

            /*   1-byte flag for multi-instrument track */
            /*       0 = only one instrument */
            /*       1 = multiple instruments */
            /*       only in format versions 5 and later */
            writer.WriteByte(_MultiInstrument ? (byte)1 : (byte)0);

            // 4-byte bitflags of which note properties are visible in the inline property strip editor
            writer.WriteUInt32((uint)_inlineParamVis);
        }

        public static void StaticSave(BinaryWriter writer, object o, SaveContext saveContext)
        {
            ((TrackObjectRec)o).Save(writer, saveContext);
        }

        public void WriteNoteVector(BinaryWriter writer)
        {
            /*   1-byte format version number */
            /*       should be 1, 2, 3, or 4 */
            writer.WriteByte(4);

            /*   4-byte little endian number of frames in the vector */
            writer.WriteInt32(_FrameArray.Count);

            /*   * for each frame: */
            /*       4-byte little endian number of notes in the frame */
            /*       n-bytes of data for all of the notes */
            int NumberOfTieRecords = 0;
            for (int iFrame = 0; iFrame < _FrameArray.Count; iFrame += 1)
            {
                FrameObjectRec Frame = _FrameArray[iFrame];

                /*       4-byte little endian number of notes in the frame */
                writer.WriteUInt32Delta((uint)Frame.Count);

                /* write each note */
                for (int iNote = 0; iNote < Frame.Count; iNote += 1)
                {
                    NoteObjectRec Note = Frame[iNote];
                    if (!Note.IsItACommand && (((NoteNoteObjectRec)Note).GetNoteTieTarget() != null))
                    {
                        NumberOfTieRecords += 1;
                    }
                    Note.Save(writer);
                }
            }

            /*   4-byte little endian number of records in the tie matrix */
            writer.WriteInt32(NumberOfTieRecords);

            /*   * for each tie matrix entry: */
            /*       for version 4: */
            /*         delta-coded index of the source frame */
            /*         delta-coded index of the source note in the frame */
            /*         delta-coded index of the target frame */
            /*         delta-coded index of the target note in the frame */
            for (int iSourceFrame = 0; iSourceFrame < _FrameArray.Count; iSourceFrame += 1)
            {
                FrameObjectRec Frame = _FrameArray[iSourceFrame];
                for (int iSourceNote = 0; iSourceNote < Frame.Count; iSourceNote += 1)
                {
                    NoteObjectRec Note = Frame[iSourceNote];
                    if (!Note.IsItACommand)
                    {
                        NoteObjectRec TieTarget = ((NoteNoteObjectRec)Note).GetNoteTieTarget();
                        if (TieTarget != null)
                        {
                            NumberOfTieRecords -= 1;
                            for (int iTargetFrame = iSourceFrame + 1; iTargetFrame < _FrameArray.Count; iTargetFrame += 1)
                            {
                                FrameObjectRec SearchFrame = _FrameArray[iTargetFrame];
                                for (int iTargetNote = 0; iTargetNote < SearchFrame.Count; iTargetNote += 1)
                                {
                                    NoteObjectRec SearchNote = SearchFrame[iTargetNote];
                                    if (!SearchNote.IsItACommand)
                                    {
                                        if (SearchNote == TieTarget)
                                        {
                                            writer.WriteUInt32Delta((uint)iSourceFrame);
                                            writer.WriteUInt32Delta((uint)iSourceNote);
                                            writer.WriteUInt32Delta((uint)iTargetFrame);
                                            writer.WriteUInt32Delta((uint)iTargetNote);
                                            goto DoneSearchingForTieTargetPoint;
                                        }
                                    }
                                }
                            }
                            // tie target couldn't be found
                            Debug.Assert(false);
                            throw new ArgumentException();

                        /* jump out here when tie target has been found */
                        DoneSearchingForTieTargetPoint:
                            ;
                        }
                    }
                }
            }
            Debug.Assert(NumberOfTieRecords == 0);
        }

        public void SaveBackgroundTrackInfo(BinaryWriter writer, IList<TrackObjectRec> TrackList)
        {
            /*   4-byte little endian number of tracks in the background of this track */
            writer.WriteInt32(_BackgroundObjects.Count);

            /*   for each of those: */
            /*     4-byte index of the track to be in the background */
            /*       the index is respective to the order in which the tracks were loaded */
            /*       from the file, so 0 is the first track, 1 is the second, and so on. */
            for (int i = 0; i < _BackgroundObjects.Count; i += 1)
            {
                int index = TrackList.IndexOf(_BackgroundObjects[i]);
                if (index < 0)
                {
                    throw new ArgumentException(); // track not on master list
                }
                writer.WriteInt32(index);
            }
        }

        /* find any notes that are referencing the specified note via a tie and nullify the tie. */
        public void TrackObjectNullifyTies(NoteObjectRec NoteThatIsDying)
        {
            for (int FrameScan = 0; FrameScan < FrameArray.Count; FrameScan++)
            {
                FrameObjectRec Frame = FrameArray[FrameScan];
                for (int NoteScan = 0; NoteScan < Frame.Count; NoteScan++)
                {
                    NoteObjectRec Note = Frame[NoteScan];
                    if (!Note.IsItACommand)
                    {
                        NoteNoteObjectRec NoteNote = (NoteNoteObjectRec)Note;
                        if (NoteNote.GetNoteTieTarget() == NoteThatIsDying)
                        {
                            NoteNote.PutNoteTieTarget(null);
                        }
                    }
                }
            }
            Changed(FrameArray_PropertyName);
        }

        /* delete a range of frames from the track. */
        public void TrackObjectDeleteFrameRun(int Index, int Count)
        {
            /* this thing is going to have to be redrawn */
            // TrackObjectAltered(TrackObj, Index); -- caller must do this

            /* break all ties entering the region to be deleted */
            int l = FrameArray.Count;
            TrackObjectBreakTiesFromRangeIntoRange(0, Index, Index, Count);
            TrackObjectBreakTiesFromRangeIntoRange(Index + Count, l, Index, Count);

            /* do the deletion */
            for (int i = Index; i < Index + Count; i += 1)
            {
                FrameArray.RemoveAt(Index);
            }
        }

        /* helper to break all ties from a given range to another given range */
        public void TrackObjectBreakTiesFromRangeIntoRange(
            int SourceStart,
            int SourceLength,
            int TargetStart,
            int TargetLength)
        {
            for (int i = SourceStart; i < SourceLength; i += 1)
            {
                FrameObjectRec Frame = FrameArray[i];
                TrackObjectBreakTiesFromFrameIntoRange(Frame, TargetStart, TargetLength);
            }
        }

        /* helper to break all ties in a frame into a given region */
        public void TrackObjectBreakTiesFromFrameIntoRange(
            FrameObjectRec Frame,
            int Start,
            int Length)
        {
            if (!Frame.IsThisACommandFrame)
            {
                for (int i = 0; i < Frame.Count; i += 1)
                {
                    NoteNoteObjectRec Note = (NoteNoteObjectRec)Frame[i];
                    NoteNoteObjectRec TargetOfTieNote = Note.GetNoteTieTarget();
                    if ((TargetOfTieNote != null) && TrackObjectIsNoteInRange(TargetOfTieNote, Start, Length))
                    {
                        Note.PutNoteTieTarget(null);
                    }
                }
            }
        }

        /* helper -- return whether a note is in the given range */
        public bool TrackObjectIsNoteInRange(
            NoteObjectRec Note,
            int Start,
            int Length)
        {
            Debug.Assert((Start >= 0) && (Start <= FrameArray.Count)
                && (Length >= 0) && (Start + Length <= FrameArray.Count));

            for (int i = 0; i < Length; i += 1)
            {
                FrameObjectRec Frame = FrameArray[i + Start];
                for (int j = 0; j < Frame.Count; j += 1)
                {
                    NoteObjectRec TestNote = Frame[j];
                    if (TestNote == Note)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /* get a list of frames and copy them out of the track */
        public FrameObjectRec[] TrackObjectCopyFrameRun(int Index, int Count)
        {
            List<FrameObjectRec> ReturnList = new List<FrameObjectRec>(Count);

            int ReturnFrameScan = Index;
            while (ReturnFrameScan < Index + Count)
            {
                FrameObjectRec OriginalFrame = FrameArray[ReturnFrameScan];
                FrameObjectRec DuplicateFrame = OriginalFrame.Clone();
                ReturnList.Add(DuplicateFrame);
                ReturnFrameScan++;
            }

            /* now we have to patch up ties.  for each tie in the new array, find the */
            /* note in the old array.  if it corresponds to a note we copied to the new */
            /* array, then fix it up, otherwise set it to NIL. */
            int ReturnFrameLimit = ReturnList.Count;
            for (ReturnFrameScan = 0; ReturnFrameScan < ReturnFrameLimit; ReturnFrameScan += 1)
            {
                FrameObjectRec Frame = ReturnList[ReturnFrameScan];
                /* it's either got some notes or one command */
                if (!Frame.IsThisACommandFrame)
                {
                    int ReturnNoteLimit = Frame.Count;
                    for (int ReturnNoteScan = 0; ReturnNoteScan < ReturnNoteLimit; ReturnNoteScan += 1)
                    {
                        NoteNoteObjectRec Note = (NoteNoteObjectRec)Frame[ReturnNoteScan];
                        NoteNoteObjectRec OurNoteTieTarget = Note.GetNoteTieTarget();
                        if (OurNoteTieTarget != null)
                        {
                            int OriginalFrameLimit = FrameArray.Count;
                            for (int OriginalFrameScan = 0; OriginalFrameScan < OriginalFrameLimit; OriginalFrameScan += 1)
                            {
                                FrameObjectRec OrigFrame = FrameArray[OriginalFrameScan];
                                if (!OrigFrame.IsThisACommandFrame)
                                {
                                    int OriginalNoteLimit = OrigFrame.Count;
                                    for (int OriginalNoteScan = 0; OriginalNoteScan < OriginalNoteLimit; OriginalNoteScan += 1)
                                    {
                                        NoteNoteObjectRec Possibility = (NoteNoteObjectRec)OrigFrame[OriginalNoteScan];
                                        if (Possibility == OurNoteTieTarget)
                                        {
                                            /* found the tie target */
                                            if ((OriginalFrameScan >= Index) && (OriginalFrameScan < Index + Count))
                                            {
                                                /* it's a valid tie, so fix it up */
                                                Note.PutNoteTieTarget((NoteNoteObjectRec)ReturnList[OriginalFrameScan - Index][OriginalNoteScan]);
                                            }
                                            else
                                            {
                                                /* it isn't valid, so kill it */
                                                Note.PutNoteTieTarget(null);
                                            }
                                            goto TieValidPoint1;
                                        }
                                    }
                                }
                            } /* end inner frame list scan */
                            Debug.Assert(false); // tie target not found
                        TieValidPoint1:
                            ;
                        }
                    } /* end frame scan */
                }
            } /* end frame list scan */

            return ReturnList.ToArray();
        }


        // methods for track attribute editing dialog

        public static TrackObjectRec CloneProperties(TrackObjectRec source, Document document)
        {
            TrackObjectRec copy = new TrackObjectRec(document);
            copy.CopyPropertiesFrom(source);
            return copy;
        }

        public void CopyPropertiesFrom(TrackObjectRec source)
        {
            this.DefaultAccent1 = source._DefaultAccent1;
            this.DefaultAccent2 = source._DefaultAccent2;
            this.DefaultAccent3 = source._DefaultAccent3;
            this.DefaultAccent4 = source._DefaultAccent4;
            this.DefaultAccent5 = source._DefaultAccent5;
            this.DefaultAccent6 = source._DefaultAccent6;
            this.DefaultAccent7 = source._DefaultAccent7;
            this.DefaultAccent8 = source._DefaultAccent8;
            this.DefaultDetune = source._DefaultDetune;
            this.DefaultDetuneModeFlag = source._DefaultDetuneModeFlag;
            this.DefaultDuration = source._DefaultDuration;
            this.DefaultDurationModeFlag = source._DefaultDurationModeFlag;
            this.DefaultEarlyLateAdjust = source._DefaultEarlyLateAdjust;
            this.DefaultHurryUpFactor = source._DefaultHurryUpFactor;
            this.DefaultOverallLoudness = source._DefaultOverallLoudness;
            this.DefaultPitchDisplacementDepthAdjust = source._DefaultPitchDisplacementDepthAdjust;
            this.DefaultPitchDisplacementRateAdjust = source._DefaultPitchDisplacementRateAdjust;
            this.DefaultPitchDisplacementStartPoint = source._DefaultPitchDisplacementStartPoint;
            this.DefaultPitchDisplacementStartPointModeFlag = source._DefaultPitchDisplacementStartPointModeFlag;
            this.DefaultReleasePoint1 = source._DefaultReleasePoint1;
            this.DefaultReleasePoint1ModeFlag = source._DefaultReleasePoint1ModeFlag;
            this.DefaultReleasePoint2 = source._DefaultReleasePoint2;
            this.DefaultReleasePoint2ModeFlag = source._DefaultReleasePoint2ModeFlag;
            this.DefaultStereoPositioning = source._DefaultStereoPositioning;
            this.DefaultSurroundPositioning = source._DefaultSurroundPositioning;
            this.InstrumentName = source._InstrumentName;
            this.MultiInstrument = source._MultiInstrument;
            this.Name = source._Name;
        }


        // utility

        public bool FindNote(NoteObjectRec note, out int frameIndexOut, out int noteIndexOut)
        {
            frameIndexOut = -1;
            noteIndexOut = -1;
            for (int frameIndex = 0; frameIndex < FrameArray.Count; frameIndex++)
            {
                int noteIndex = FrameArray[frameIndex].IndexOf(note);
                if (noteIndex >= 0)
                {
                    frameIndexOut = frameIndex;
                    noteIndexOut = noteIndex;
                    return true;
                }
            }
            return false;
        }
    }

    public class FrameObjectRec : IList
    {
        private List<NoteObjectRec> NoteArray = new List<NoteObjectRec>();

        public int Count { get { return NoteArray.Count; } }

        public bool IsThisACommandFrame
        {
            get
            {
                return (Count == 1) && NoteArray[0].IsItACommand;
            }
        }

        public void Add(NoteObjectRec note)
        {
            NoteArray.Add(note);
        }

        public void RemoveAt(int index) // DeleteNoteFromFrame
        {
            NoteArray.RemoveAt(index);
        }

        public NoteObjectRec this[int index]
        {
            get
            {
                return NoteArray[index];
            }
            set
            {
                NoteArray[index] = value;
            }
        }

        public int IndexOf(NoteObjectRec note) // FindNoteInFrame
        {
            return NoteArray.IndexOf(note);
        }

        public FrameObjectRec Clone() // DeepDuplicateFrame
        {
            FrameObjectRec frameCopy = new FrameObjectRec();
            for (int i = 0; i < NoteArray.Count; i++)
            {
                NoteObjectRec noteOrig = NoteArray[i];
                NoteObjectRec noteCopy;
                if (noteOrig is NoteNoteObjectRec)
                {
                    noteCopy = new NoteNoteObjectRec((NoteNoteObjectRec)noteOrig, (TrackObjectRec)noteOrig.Parent);
                }
                else
                {
                    noteCopy = new CommandNoteObjectRec((CommandNoteObjectRec)noteOrig, (TrackObjectRec)noteOrig.Parent);
                }
                frameCopy.NoteArray.Add(noteCopy);
            }
            return frameCopy;
        }

        /* find out the duration of the specified frame.  returns the duration of the */
        /* frame as a fraction */
        public void DurationOfFrame(out FractionRec Frac)
        {
            if (IsThisACommandFrame)
            {
                Frac = new FractionRec(0, 0, Constants.Denominator);
                return;
            }
            if (NoteArray.Count < 1)
            {
                // DurationOfFrame called on empty frame
                Debug.Assert(false);
                throw new ArgumentException();
            }
            /* obtain duration of first element */
            Frac = new FractionRec(uint.MaxValue, 0, Constants.Denominator);
            for (int Scan = 0; Scan < NoteArray.Count; Scan += 1)
            {
                FractionRec TempFrac;

                NoteNoteObjectRec Note = (NoteNoteObjectRec)NoteArray[Scan];
                Note.GetNoteDurationFrac(out TempFrac);
                if (FractionRec.FracGreaterThan(Frac, TempFrac))
                {
                    /* choose smallest one */
                    Frac = TempFrac;
                }
            }
        }

        #region IList
        bool IList.IsReadOnly { get { return false; } }
        bool IList.IsFixedSize { get { return false; } }
        int ICollection.Count { get { return Count; } }
        object ICollection.SyncRoot { get { return this; } }
        bool ICollection.IsSynchronized { get { return false; } }

        object IList.this[int index] { get { return this[index]; } set { this[index] = (NoteObjectRec)value; } }

        int IList.Add(object value)
        {
            Add((NoteObjectRec)value);
            return Count - 1;
        }

        bool IList.Contains(object value)
        {
            return IndexOf((NoteObjectRec)value) >= 0;
        }

        void IList.Clear()
        {
            NoteArray.Clear();
        }

        int IList.IndexOf(object value)
        {
            return IndexOf((NoteObjectRec)value);
        }

        void IList.Insert(int index, object value)
        {
            NoteArray.Insert(index, (NoteObjectRec)value);
        }

        void IList.Remove(object value)
        {
            RemoveAt(IndexOf((NoteObjectRec)value));
        }

        void IList.RemoveAt(int index)
        {
            RemoveAt(index);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            for (int i = 0; i < Count; i++)
            {
                array.SetValue(this[i], i);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return NoteArray.GetEnumerator();
        }
        #endregion
    }

    public abstract class NoteObjectRec : HierarchicalBindingBase
    {
        /* flags field.  if high bit is set, then it is a command and the low order */
        /* bits are the opcode.  if high bit is clear, then it is a note and the low */
        /* order bits determine the duration and some other information */
        protected NoteFlags _Flags;
        public const string Flags_PropertyName = "Flags";
        [Bindable(true)]
        public NoteFlags Flags { get { return _Flags; } }
        protected NoteFlags Flags_Settable { get { return _Flags; } set { PatchObject(value, ref _Flags, Flags_PropertyName); } }

        public static NoteObjectRec Create(
            BinaryReader reader,
            int FormatVersionNumber,
            TrackObjectRec track,
            LoadContext loadContext)
        {
            if (FormatVersionNumber > 4)
            {
                throw new InvalidDataException();
            }

            /*   4-byte unsigned little endian opcode field */
            /*       for a command, the high bit will be 1 and the remaining bits will */
            /*       be the opcode.  for a note, the high bit will be 0. */
            NoteFlags Opcode = (NoteFlags)reader.ReadUInt32();

            if ((Opcode & NoteFlags.eCommandFlag) == 0)
            {
                NoteNoteObjectRec Note = new NoteNoteObjectRec(reader, FormatVersionNumber, Opcode, track);
                return Note;
            }
            else
            {
                CommandNoteObjectRec Command = new CommandNoteObjectRec(reader, FormatVersionNumber, Opcode, track, loadContext);
                return Command;
            }
        }

        public abstract void Save(BinaryWriter writer);

        protected NoteObjectRec(NoteFlags Flags, TrackObjectRec track)
            : base(track, TrackObjectRec.FrameArray_PropertyName)
        {
            this._Flags = Flags;
        }

        public bool IsItACommand { get { return (_Flags & NoteFlags.eCommandFlag) != 0; } }
    }

    public class CommandNoteObjectRec : NoteObjectRec
    {
        /* string arguments for commands.  NIL means it hasn't been defined */
        /* unlike in most of the program, 13 is used as the line feed instead */
        /* of 10 for this string */
        public string _StringArgument1 = String.Empty;
        public const string StringArgument1_PropertyName = "StringArgument1";
        [Bindable(true)]
        [Searchable]
        public string StringArgument1 { get { return _StringArgument1; } set { Patch(value, ref _StringArgument1, StringArgument1_PropertyName); } }
        //
        public string _StringArgument2 = String.Empty;
        public const string StringArgument2_PropertyName = "StringArgument2";
        [Bindable(true)]
        [Searchable]
        public string StringArgument2 { get { return _StringArgument2; } set { Patch(value, ref _StringArgument2, StringArgument2_PropertyName); } }

        /* numeric arguments for commands */
        public int _Argument1;
        public const string Argument1_PropertyName = "Argument1";
        [Bindable(true)]
        public int Argument1 { get { return _Argument1; } set { Patch(value, ref _Argument1, Argument1_PropertyName); } }
        //
        public int _Argument2;
        public const string Argument2_PropertyName = "Argument2";
        [Bindable(true)]
        public int Argument2 { get { return _Argument2; } set { Patch(value, ref _Argument2, Argument2_PropertyName); } }

        public CommandNoteObjectRec(TrackObjectRec track)
            : base(NoteFlags.eCommandFlag, track)
        {
        }

        public CommandNoteObjectRec(CommandNoteObjectRec orig, TrackObjectRec track)
            : base(NoteFlags.eCommandFlag, track)
        {
            CopyFrom(orig);
        }

        private void CopyFrom(CommandNoteObjectRec source)
        {
            this._Flags = source._Flags;
            this._StringArgument1 = source._StringArgument1;
            this._StringArgument2 = source._StringArgument2;
            this._Argument1 = source._Argument1;
            this._Argument2 = source._Argument2;
        }

        public CommandNoteObjectRec(
            BinaryReader reader,
            int FormatVersionNumber,
            NoteFlags Opcode,
            TrackObjectRec track,
            LoadContext loadContext)
            : base(Opcode, track)
        {
            /* convert file format opcode to internal enumeration value */

            /* special cases */
            if ((int)(_Flags & ~NoteFlags.eCommandFlag) == 163)
            {
                /* 163 = set pitch displacement depth modulation mode */
                if (FormatVersionNumber == 1)
                {
                    /* for original format files, turn this into an empty marker */
                    _Flags = (NoteFlags)NoteCommands.eCmdMarker;

                    /* discard it's argument from the stream */
                    reader.ReadByte();
                }
                else
                {
                    /* for other files, this is an error */
                    throw new InvalidDataException();
                }
            }
            else
            {
                int FileID = (int)(_Flags & ~NoteFlags.eCommandFlag);
                _Flags = (NoteFlags)CommandMapping.FromFileID(FileID);

                /* read in operands */
                switch (CommandMapping.GetCommandAddressingMode((NoteCommands)(_Flags & ~NoteFlags.eCommandFlag)))
                {
                    default:
                        throw new InvalidDataException();

                    /*   no arguments */
                    case CommandAddrMode.eNoParameters:
                        break;

                    /*   4-byte little endian large/extended integer coded decimal */
                    case CommandAddrMode.e1SmallExtParameter:
                    case CommandAddrMode.e1LargeParameter:
                    case CommandAddrMode.e1IntegerParameter:
                        _Argument1 = reader.ReadInt32();
                        break;

                    /*   4-byte little endian large/extended integer coded decimal */
                    /*   4-byte little endian large/extended integer coded decimal */
                    case CommandAddrMode.e2SmallExtParameters:
                    case CommandAddrMode.eFirstLargeSecondSmallExtParameters:
                    case CommandAddrMode.e2IntegerParameters:
                    case CommandAddrMode.eFirstIntSecondLargeParameters:
                        _Argument1 = reader.ReadInt32();
                        _Argument2 = reader.ReadInt32();
                        break;

                    /*   1-byte origin specifier */
                    /*       0 = -1 */
                    /*       1 = 0 */
                    case CommandAddrMode.e1ParamReleaseOrigin:
                    case CommandAddrMode.e1PitchDisplacementMode:
                    case CommandAddrMode.e1DurationAdjustMode:
                    case CommandAddrMode.e1TrackEffectsMode:
                        switch (reader.ReadByte())
                        {
                            default:
                                throw new InvalidDataException();
                            case 0:
                                _Argument1 = -1;
                                break;
                            case 1:
                                _Argument1 = 0;
                                break;
                        }
                        break;

                    /*   4-byte little endian string2 length */
                    /*   n-byte string2 */
                    /*   4-byte little endian string1 length */
                    /*   n-byte string1 */
                    case CommandAddrMode.e2StringParameters:
                        // yes, 2 comes before 1
                        _StringArgument2 = loadContext.Utf8 ? reader.ReadString4Utf8() : reader.ReadString4Ansi();
                        _StringArgument1 = loadContext.Utf8 ? reader.ReadString4Utf8() : reader.ReadString4Ansi();
                        break;

                    /*   4-byte little endian length of comment text string */
                    /*   n-byte comment text string (line feed = 0x0d, unlike most of */
                    /*     the program) */
                    case CommandAddrMode.e1StringParameter:
                    case CommandAddrMode.e1StringParameterWithLineFeeds:
                        _StringArgument1 = loadContext.Utf8 ? reader.ReadString4Utf8("\r") : reader.ReadString4Ansi("\r");
                        break;

                    /*   4-byte little endian length of comment text string */
                    /*   n-byte comment text string (line feed = 0x0d, unlike most of */
                    /*     the program) */
                    /*   4-bytes large bcd */
                    case CommandAddrMode.e1String1LargeBCDParameters:
                        _StringArgument1 = loadContext.Utf8 ? reader.ReadString4Utf8("\r") : reader.ReadString4Ansi("\r");
                        _Argument1 = reader.ReadInt32();
                        break;
                }
            }

            /* since we changed from the file opcode numbers to the internal */
            /* opcode numbers, we need to put the command bit back in */
            _Flags |= NoteFlags.eCommandFlag;
        }

        public override void Save(BinaryWriter writer)
        {
            // 4-bytes opcode
            int FileID = CommandMapping.ToFileID((NoteCommands)(_Flags & ~NoteFlags.eCommandFlag));
            writer.WriteInt32(FileID | unchecked((int)NoteFlags.eCommandFlag));

            /* write out the arguments */
            switch (CommandMapping.GetCommandAddressingMode((NoteCommands)(_Flags & ~NoteFlags.eCommandFlag)))
            {
                default:
                    throw new ArgumentException();

                /*   no arguments */
                case CommandAddrMode.eNoParameters:
                    break;

                /*   4-byte little endian large/extended integer coded decimal */
                case CommandAddrMode.e1SmallExtParameter:
                case CommandAddrMode.e1LargeParameter:
                case CommandAddrMode.e1IntegerParameter:
                    writer.WriteInt32(_Argument1);
                    break;

                /*   4-byte little endian large/extended integer coded decimal */
                /*   4-byte little endian large/extended integer coded decimal */
                case CommandAddrMode.e2SmallExtParameters:
                case CommandAddrMode.eFirstLargeSecondSmallExtParameters:
                case CommandAddrMode.e2IntegerParameters:
                case CommandAddrMode.eFirstIntSecondLargeParameters:
                    writer.WriteInt32(_Argument1);
                    writer.WriteInt32(_Argument2);
                    break;

                /*   1-byte origin specifier */
                /*       -1 = 0 */
                /*       0 = 1 */
                case CommandAddrMode.e1ParamReleaseOrigin:
                case CommandAddrMode.e1PitchDisplacementMode:
                case CommandAddrMode.e1DurationAdjustMode:
                case CommandAddrMode.e1TrackEffectsMode:
                    writer.WriteByte(_Argument1 < 0 ? (byte)0 : (byte)1);
                    break;

                /*   4-byte little endian string2 length */
                /*   n-byte string2 */
                /*   4-byte little endian string1 length */
                /*   n-byte string1 */
                case CommandAddrMode.e2StringParameters:
                    // yes, 2 comes before 1
                    writer.WriteString4Utf8(_StringArgument2);
                    writer.WriteString4Utf8(_StringArgument1);
                    break;

                /*   4-byte little endian length of comment text string */
                /*   n-byte comment text string (line feed = 0x0d, unlike most */
                /*    of the program) */
                case CommandAddrMode.e1StringParameter:
                case CommandAddrMode.e1StringParameterWithLineFeeds:
                    writer.WriteString4Utf8(_StringArgument1, "\r");
                    break;

                /*   4-byte little endian length of comment text string */
                /*   n-byte comment text string (line feed = 0x0d, unlike most */
                /*    of the program) */
                /*   4-byte large fixed signed */
                case CommandAddrMode.e1String1LargeBCDParameters:
                    writer.WriteString4Utf8(_StringArgument1, "\r");
                    writer.WriteInt32(_Argument1);
                    break;
            }
        }


        // old style property accessors - get values

        public NoteCommands GetCommandOpcode()
        {
            return (NoteCommands)(_Flags & ~NoteFlags.eCommandFlag);
        }

        public int GetCommandNumericArg1()
        {
            return _Argument1;
        }

        public int GetCommandNumericArg2()
        {
            return _Argument2;
        }

        public string GetCommandStringArg1()
        {
            return _StringArgument1;
        }

        public string GetCommandStringArg2()
        {
            return _StringArgument2;
        }


        // old style property accessors - put value
        // These all use the binding property rather than the member variable.
        // Therefore, though it is not written here, these all fire Changed() event.

        public void PutCommandOpcode(NoteCommands NewOpcode)
        {
            if (((uint)NewOpcode & (uint)NoteFlags.eCommandFlag) != 0)
            {
                // command flag is set on new opcode word
                Debug.Assert(false);
                throw new ArgumentException();
            }
            Flags_Settable = (NoteFlags)(((uint)Flags_Settable & (uint)NoteFlags.eCommandFlag) | (uint)NewOpcode);
        }

        /* put a new string into the command.  the command becomes owner of the string. */
        /* unlike in most of the program, 13 is used as the line feed instead */
        /* of 10 for this string */
        public void PutCommandStringArg1(string NewArg)
        {
            StringArgument1 = NewArg;
        }

        public void PutCommandStringArg2(string NewArg)
        {
            StringArgument2 = NewArg;
        }

        /* put a new first numeric argument into the command */
        public void PutCommandNumericArg1(int NewValue)
        {
            Argument1 = NewValue;
        }

        /* put a new second numeric argument into the command */
        public void PutCommandNumericArg2(int NewValue)
        {
            Argument2 = NewValue;
        }
    }

    public class NoteNoteObjectRec : NoteObjectRec
    {
        /* halfstep pitch index */
        public short _Pitch;
        public const string Pitch_PropertyName = "Pitch";
        [Bindable(true)]
        public short Pitch { get { return _Pitch; } set { Patch(value, ref _Pitch, Pitch_PropertyName); } }
        public string PitchAsString
        {
            get
            {
                return SymbolicPitch.NumericPitchToString(_Pitch, _Flags & (NoteFlags.eSharpModifier | NoteFlags.eFlatModifier));
            }
            set
            {
                short PitchValue = GetNotePitch();
                NoteFlags SharpFlatThing = GetNoteFlatOrSharpStatus();
                SymbolicPitch.StringToNumericPitch(value, ref PitchValue, ref SharpFlatThing);
                PutNotePitch(PitchValue);
                PutNoteFlatOrSharpStatus(SharpFlatThing);
            }
        }

        /* portamento rate flag.  this parameter determines how long a portamento */
        /* transition should take, in fractions of a quarter note.  it only has */
        /* effect if this note is the target of a tie.  A value of 0 means it */
        /* should be instantaneous, i.e. no portamento. */
        public SmallBCDType _PortamentoDuration;
        public const string PortamentoDuration_PropertyName = "PortamentoDuration";
        [Bindable(true)]
        public double PortamentoDuration { get { return (double)_PortamentoDuration; } set { double old = (double)_PortamentoDuration; _PortamentoDuration = (SmallBCDType)value; Patch(value, ref old, PortamentoDuration_PropertyName); } }

        /* displacement forward and backward, in fractions of a quarter note. */
        /* this value is added to the current overall early/late adjust factor */
        /* as determined by the default and any adjustments or sweeps in progress */
        /* as initiated by channel commands */
        public SmallBCDType _EarlyLateAdjust;
        public const string EarlyLateAdjust_PropertyName = "EarlyLateAdjust";
        [Bindable(true)]
        public double EarlyLateAdjust { get { return (double)_EarlyLateAdjust; } set { double old = (double)_EarlyLateAdjust; _EarlyLateAdjust = (SmallBCDType)value; Patch(value, ref old, EarlyLateAdjust_PropertyName); } }

        /* note duration adjustment, in fractions of a quarter note.  this value */
        /* is either added to or multiplied by the duration to make the note run */
        /* longer or shorter.  this does not effect when subsequent notes start. */
        public SmallBCDType _DurationAdjust;
        public const string DurationAdjust_PropertyName = "DurationAdjust";
        [Bindable(true)]
        public double DurationAdjust { get { return (double)_DurationAdjust; } set { double old = (double)_DurationAdjust; _DurationAdjust = (SmallBCDType)value; Patch(value, ref old, DurationAdjust_PropertyName); } }

        /* note to tie after completion of this note (NIL = none) */
        public NoteNoteObjectRec _Tie;
        public const string Tie_PropertyName = "Tie";
        [Bindable(true)]
        public NoteNoteObjectRec Tie { get { return _Tie; } set { PatchReference(value, ref _Tie, Tie_PropertyName); } }

        /* these are fine tuning adjustment parameters.  they correspond to */
        /* some of the values that can be set by commands.  the values in */
        /* these parameters have effect over and above the effects of the */
        /* values set in the commands (i.e. the effects are cumulative) when */
        /* appropriate. */

        /* release points as a fraction of the note's duration */
        /* these are relative to either the beginning of the note (key-down) or */
        /* the end of the note (duration counter runout) as determined by some */
        /* bits in the flags word.  If the flag indicates that the default origin */
        /* should be used, then this value is added to the channel overall/default */
        /* value, otherwise the default value is not used. */
        public SmallBCDType _ReleasePoint1;
        public const string ReleasePoint1_PropertyName = "ReleasePoint1";
        [Bindable(true)]
        public double ReleasePoint1 { get { return (double)_ReleasePoint1; } set { double old = (double)_ReleasePoint1; _ReleasePoint1 = (SmallBCDType)value; Patch(value, ref old, ReleasePoint1_PropertyName); } }
        //
        public SmallBCDType _ReleasePoint2;
        public const string ReleasePoint2_PropertyName = "ReleasePoint2";
        [Bindable(true)]
        public double ReleasePoint2 { get { return (double)_ReleasePoint2; } set { double old = (double)_ReleasePoint2; _ReleasePoint2 = (SmallBCDType)value; Patch(value, ref old, ReleasePoint2_PropertyName); } }

        /* adjustment for overall loudness envelope */
        /* this factor multiplicatively scales the output volume of the channel, */
        /* so a value of 1 leaves it unchanged.  This scaling is in addition to */
        /* the channel overall/default scaling, by multiplying the values. */
        public SmallBCDType _OverallLoudnessAdjustment;
        public const string OverallLoudnessAdjustment_PropertyName = "OverallLoudnessAdjustment";
        [Bindable(true)]
        public double OverallLoudnessAdjustment { get { return (double)_OverallLoudnessAdjustment; } set { double old = (double)_OverallLoudnessAdjustment; _OverallLoudnessAdjustment = (SmallBCDType)value; Patch(value, ref old, OverallLoudnessAdjustment_PropertyName); } }

        /* left-right positioning adjustment for the note */
        /* this factor determines where the sound will come from when stereo */
        /* synthesis is being used.  -1 is far left, 1 is far right, and 0 is */
        /* center.  This is added to the channel overall/default position. */
        public SmallBCDType _StereoPositionAdjustment;
        public const string StereoPositionAdjustment_PropertyName = "StereoPositionAdjustment";
        [Bindable(true)]
        public double StereoPositionAdjustment { get { return (double)_StereoPositionAdjustment; } set { double old = (double)_StereoPositionAdjustment; _StereoPositionAdjustment = (SmallBCDType)value; Patch(value, ref old, StereoPositionAdjustment_PropertyName); } }

        /* front-back positioning adjustment for the note */
        /* this factor determines where the sound will come from when surround */
        /* synthesis is being used.  1 is far front and -1 is far rear.  this */
        /* is added to the channel overall/default position */
        public SmallBCDType _SurroundPositionAdjustment;
        public const string SurroundPositionAdjustment_PropertyName = "SurroundPositionAdjustment";
        [Bindable(true)]
        public double SurroundPositionAdjustment { get { return (double)_SurroundPositionAdjustment; } set { double old = (double)_SurroundPositionAdjustment; _SurroundPositionAdjustment = (SmallBCDType)value; Patch(value, ref old, SurroundPositionAdjustment_PropertyName); } }

        /* special accent adjustments value for wave table generators */
        /* these factors are the base-2 log of multiplicative scaling factors. */
        /* therefore, 0 makes no change, 1 doubles the target value, and -1 */
        /* halves it.  the values are combined with the respective global */
        /* channel values via addition. */
        public SmallBCDType _Accent1;
        public const string Accent1_PropertyName = "Accent1";
        [Bindable(true)]
        public double Accent1 { get { return (double)_Accent1; } set { double old = (double)_Accent1; _Accent1 = (SmallBCDType)value; Patch(value, ref old, Accent1_PropertyName); } }
        //
        public SmallBCDType _Accent2;
        public const string Accent2_PropertyName = "Accent2";
        [Bindable(true)]
        public double Accent2 { get { return (double)_Accent2; } set { double old = (double)_Accent2; _Accent2 = (SmallBCDType)value; Patch(value, ref old, Accent2_PropertyName); } }
        //
        public SmallBCDType _Accent3;
        public const string Accent3_PropertyName = "Accent3";
        [Bindable(true)]
        public double Accent3 { get { return (double)_Accent3; } set { double old = (double)_Accent3; _Accent3 = (SmallBCDType)value; Patch(value, ref old, Accent3_PropertyName); } }
        //
        public SmallBCDType _Accent4;
        public const string Accent4_PropertyName = "Accent4";
        [Bindable(true)]
        public double Accent4 { get { return (double)_Accent4; } set { double old = (double)_Accent4; _Accent4 = (SmallBCDType)value; Patch(value, ref old, Accent4_PropertyName); } }
        //
        public SmallBCDType _Accent5;
        public const string Accent5_PropertyName = "Accent5";
        [Bindable(true)]
        public double Accent5 { get { return (double)_Accent5; } set { double old = (double)_Accent5; _Accent5 = (SmallBCDType)value; Patch(value, ref old, Accent5_PropertyName); } }
        //
        public SmallBCDType _Accent6;
        public const string Accent6_PropertyName = "Accent6";
        [Bindable(true)]
        public double Accent6 { get { return (double)_Accent6; } set { double old = (double)_Accent6; _Accent6 = (SmallBCDType)value; Patch(value, ref old, Accent6_PropertyName); } }
        //
        public SmallBCDType _Accent7;
        public const string Accent7_PropertyName = "Accent7";
        [Bindable(true)]
        public double Accent7 { get { return (double)_Accent7; } set { double old = (double)_Accent7; _Accent7 = (SmallBCDType)value; Patch(value, ref old, Accent7_PropertyName); } }
        //
        public SmallBCDType _Accent8;
        public const string Accent8_PropertyName = "Accent8";
        [Bindable(true)]
        public double Accent8 { get { return (double)_Accent8; } set { double old = (double)_Accent8; _Accent8 = (SmallBCDType)value; Patch(value, ref old, Accent8_PropertyName); } }

        /* then this is the pitch to use to select the sample or wave table from */
        /* the multisample list instead of Pitch.  if this is -1, then Pitch */
        /* should be used for sample selection. */
        public short _MultisamplePitchAsIf;
        public const string MultisamplePitchAsIf_PropertyName = "MultisamplePitchAsIf";
        [Bindable(true)]
        public short MultisamplePitchAsIf { get { return _MultisamplePitchAsIf; } set { Patch(value, ref _MultisamplePitchAsIf, MultisamplePitchAsIf_PropertyName); } }
        public string MultisamplePitchAsIfAsString
        {
            get
            {
                return _MultisamplePitchAsIf >= 0
                    ? SymbolicPitch.NumericPitchToString(_MultisamplePitchAsIf, _Flags & (NoteFlags.eSharpModifier | NoteFlags.eFlatModifier))
                    : "default";
            }
            set
            {
                if (value.Contains("def"/*ault*/))
                {
                    PutNoteMultisampleFalsePitch((short)(-1));
                }
                else
                {
                    short PitchValue = GetNotePitch();
                    NoteFlags SharpFlatThing = 0;
                    SymbolicPitch.StringToNumericPitch(value, ref PitchValue, ref SharpFlatThing);
                    PutNoteMultisampleFalsePitch(PitchValue);
                }
            }
        }

        /* adjustment factor for pitch displacement depth envelope amplitude. */
        /* this factor determines the depth of the frequency LFO generators. */
        /* the envelope controlling the depth is taken as a value from 0..1. */
        /* the value is multiplied by the overall channel value. */
        public SmallBCDType _PitchDisplacementDepthAdjustment;
        public const string PitchDisplacementDepthAdjustment_PropertyName = "PitchDisplacementDepthAdjustment";
        [Bindable(true)]
        public double PitchDisplacementDepthAdjustment { get { return (double)_PitchDisplacementDepthAdjustment; } set { double old = (double)_PitchDisplacementDepthAdjustment; _PitchDisplacementDepthAdjustment = (SmallBCDType)value; Patch(value, ref old, PitchDisplacementDepthAdjustment_PropertyName); } }

        /* adjustment factor for pitch displacement rate envelope amplitude. */
        /* this factor determines the rate of the frequency LFO generators.  the */
        /* envelope controlling the rate is taken as a value from 0..1.  this */
        /* parameter provides units, in periods per second, for scaling the rate */
        /* envelope output.  This value is added to the overall channel/default */
        /* value. */
        public SmallBCDType _PitchDisplacementRateAdjustment;
        public const string PitchDisplacementRateAdjustment_PropertyName = "PitchDisplacementRateAdjustment";
        [Bindable(true)]
        public double PitchDisplacementRateAdjustment { get { return (double)_PitchDisplacementRateAdjustment; } set { double old = (double)_PitchDisplacementRateAdjustment; _PitchDisplacementRateAdjustment = (SmallBCDType)value; Patch(value, ref old, PitchDisplacementRateAdjustment_PropertyName); } }

        /* selection of pitch displacement envelope start point. */
        /* this specifies when the pitch displacement LFOs start.  this value */
        /* is relative to the start or end of the note, as determined by some */
        /* bits in the flags word.  If the flags specify that the default origin */
        /* should be used, then the value is added to the default start point, */
        /* otherwise the default start point is not used. */
        public SmallBCDType _PitchDisplacementStartPoint;
        public const string PitchDisplacementStartPoint_PropertyName = "PitchDisplacementStartPoint";
        [Bindable(true)]
        public double PitchDisplacementStartPoint { get { return (double)_PitchDisplacementStartPoint; } set { double old = (double)_PitchDisplacementStartPoint; _PitchDisplacementStartPoint = (SmallBCDType)value; Patch(value, ref old, PitchDisplacementStartPoint_PropertyName); } }

        /* overall envelope rate adjustment. */
        /* this factor scales the total speed with which all envelopes associated */
        /* with the note undergo transitions.  A value of 1 does not change them, */
        /* smaller values accelerate transition.  This value is in addition to */
        /* the channel/default value via multiplication of the values. */
        public SmallBCDType _HurryUpFactor;
        public const string HurryUpFactor_PropertyName = "HurryUpFactor";
        [Bindable(true)]
        public double HurryUpFactor { get { return (double)_HurryUpFactor; } set { double old = (double)_HurryUpFactor; _HurryUpFactor = (SmallBCDType)value; Patch(value, ref old, HurryUpFactor_PropertyName); } }

        /* detuning in either Hertz or halfsteps. */
        /* this value specifies how much to detune the nominal pitch of the note. */
        /* the value is either in units of Hertz or halfsteps, as determined by */
        /* a bit in the flags word, and the detuning is added to the channel */
        /* overall/default detuning.  If the flags indicate that the default */
        /* pitch coversion should be used, then this value is multiplied by */
        /* the channel overall/default value to scale it. */
        public SmallBCDType _Detuning;
        public const string Detuning_PropertyName = "Detuning";
        [Bindable(true)]
        public double Detuning { get { return (double)_Detuning; } set { double old = (double)_Detuning; _Detuning = (SmallBCDType)value; Patch(value, ref old, Detuning_PropertyName); } }


        // properties synthesized out of flags field

        private void PatchFlags(NoteFlags value, NoteFlags mask, string propertyName)
        {
            Debug.Assert((value & ~mask) == 0);
            int flags = (int)_Flags;
            Patch((flags & ~(int)mask) | (int)value, ref flags, propertyName);
            PatchObject((NoteFlags)flags, ref _Flags, Flags_PropertyName);
        }

        public const string ReleasePoint1Origin_PropertyName = "ReleasePoint1Origin";
        public const string ReleasePoint1Origin_EnumCategoryName = "ReleasePoint1Origin";
        public static Enum[] ReleasePoint1OriginAllowedValues { get { return new Enum[] { NoteFlags.eRelease1FromDefault, NoteFlags.eRelease1FromStart, NoteFlags.eRelease1FromEnd, }; } }
        [Bindable(true)]
        public NoteFlags ReleasePoint1Origin
        {
            get { return _Flags & NoteFlags.eRelease1OriginMask; }
            set { PatchFlags(value, NoteFlags.eRelease1OriginMask, ReleasePoint1Origin_PropertyName); }
        }
        [Bindable(true)]
        public string ReleasePoint1OriginAsString
        {
            get { return EnumUtility.GetDescription(ReleasePoint1Origin, ReleasePoint1Origin_EnumCategoryName); }
            set
            {
                string old = EnumUtility.GetDescription(_Flags & NoteFlags.eRelease1OriginMask, ReleasePoint1Origin_EnumCategoryName);
                NoteFlags valueEnum = (NoteFlags)EnumUtility.GetValue(NoteFlags.eCommandFlag.GetType(), value, ReleasePoint1Origin_EnumCategoryName);
                PatchFlags(valueEnum, NoteFlags.eRelease1OriginMask, ReleasePoint1Origin_PropertyName);
            }
        }

        public const string ReleasePoint2Origin_PropertyName = "ReleasePoint2Origin";
        public const string ReleasePoint2Origin_EnumCategoryName = "ReleasePoint2Origin";
        public static Enum[] ReleasePoint2OriginAllowedValues { get { return new Enum[] { NoteFlags.eRelease2FromDefault, NoteFlags.eRelease2FromStart, NoteFlags.eRelease2FromEnd, }; } }
        [Bindable(true)]
        public NoteFlags ReleasePoint2Origin
        {
            get { return _Flags & NoteFlags.eRelease2OriginMask; }
            set { PatchFlags(value, NoteFlags.eRelease2OriginMask, ReleasePoint2Origin_PropertyName); }
        }
        [Bindable(true)]
        public string ReleasePoint2OriginAsString
        {
            get { return EnumUtility.GetDescription(ReleasePoint2Origin, ReleasePoint2Origin_EnumCategoryName); }
            set
            {
                string old = EnumUtility.GetDescription(_Flags & NoteFlags.eRelease2OriginMask, ReleasePoint2Origin_EnumCategoryName);
                NoteFlags valueEnum = (NoteFlags)EnumUtility.GetValue(NoteFlags.eCommandFlag.GetType(), value, ReleasePoint2Origin_EnumCategoryName);
                PatchFlags(valueEnum, NoteFlags.eRelease2OriginMask, ReleasePoint2Origin_PropertyName);
            }
        }

        public const string ReleasePoint3Origin_PropertyName = "ReleasePoint3Origin";
        public const string ReleasePoint3Origin_EnumCategoryName = "ReleasePoint3Origin";
        public static Enum[] ReleasePoint3OriginAllowedValues { get { return new Enum[] { NoteFlags.eRelease3FromStartNotEnd, NoteFlags.eRelease3FromEnd, }; } }
        [Bindable(true)]
        public NoteFlags ReleasePoint3Origin
        {
            get { return _Flags & NoteFlags.eRelease3OriginMask; }
            set { PatchFlags(value, NoteFlags.eRelease3OriginMask, ReleasePoint3Origin_PropertyName); }
        }
        [Bindable(true)]
        public string ReleasePoint3OriginAsString
        {
            get { return EnumUtility.GetDescription(ReleasePoint3Origin, ReleasePoint3Origin_EnumCategoryName); }
            set
            {
                string old = EnumUtility.GetDescription(_Flags & NoteFlags.eRelease3OriginMask, ReleasePoint3Origin_EnumCategoryName);
                NoteFlags valueEnum = (NoteFlags)EnumUtility.GetValue(NoteFlags.eCommandFlag.GetType(), value, ReleasePoint3Origin_EnumCategoryName);
                PatchFlags(valueEnum, NoteFlags.eRelease3OriginMask, ReleasePoint3Origin_PropertyName);
            }
        }

        public const string PitchDisplacementOrigin_PropertyName = "PitchDisplacementOrigin";
        public const string PitchDisplacementOrigin_EnumCategoryName = "PitchDisplacementOrigin";
        public static Enum[] PitchDisplacementOriginAllowedValues { get { return new Enum[] { NoteFlags.ePitchDisplacementStartFromDefault, NoteFlags.ePitchDisplacementStartFromStart, NoteFlags.ePitchDisplacementStartFromEnd, }; } }
        [Bindable(true)]
        public NoteFlags PitchDisplacementOrigin
        {
            get { return _Flags & NoteFlags.ePitchDisplacementStartOriginMask; }
            set { PatchFlags(value, NoteFlags.ePitchDisplacementStartOriginMask, PitchDisplacementOrigin_PropertyName); }
        }
        [Bindable(true)]
        public string PitchDisplacementOriginAsString
        {
            get { return EnumUtility.GetDescription(PitchDisplacementOrigin, PitchDisplacementOrigin_EnumCategoryName); }
            set
            {
                string old = EnumUtility.GetDescription(_Flags & NoteFlags.ePitchDisplacementStartOriginMask, PitchDisplacementOrigin_EnumCategoryName);
                NoteFlags valueEnum = (NoteFlags)EnumUtility.GetValue(NoteFlags.eCommandFlag.GetType(), value, PitchDisplacementOrigin_EnumCategoryName);
                PatchFlags(valueEnum, NoteFlags.ePitchDisplacementStartOriginMask, PitchDisplacementOrigin_PropertyName);
            }
        }

        public const string PortamentoLeadsBeat_PropertyName = "PortamentoLeadsBeat";
        [Bindable(true)]
        public bool PortamentoLeadsBeat
        {
            get { return (_Flags & NoteFlags.ePortamentoLeadsNote) != 0; }
            set { PatchFlags(value ? NoteFlags.ePortamentoLeadsNote : 0, NoteFlags.ePortamentoLeadsNote, PortamentoLeadsBeat_PropertyName); }
        }

        public const string PortamentoUnits_PropertyName = "PortamentoUnits";
        public const string PortamentoUnits_EnumCategoryName = "PortamentoUnits";
        public static Enum[] PortamentoUnitsAllowedValues { get { return new Enum[] { NoteFlags.ePortamentoUnitsHalfsteps, NoteFlags.ePortamentoUnitsHertzNotHalfsteps, }; } }
        [Bindable(true)]
        public NoteFlags PortamentoUnits
        {
            get { return _Flags & NoteFlags.ePortamentoUnitsMask; }
            set { PatchFlags(value, NoteFlags.ePortamentoUnitsMask, PortamentoUnits_PropertyName); }
        }
        [Bindable(true)]
        public string PortamentoUnitsAsString
        {
            get { return EnumUtility.GetDescription(PortamentoUnits, PortamentoUnits_EnumCategoryName); }
            set
            {
                string old = EnumUtility.GetDescription(_Flags & NoteFlags.ePortamentoUnitsMask, PortamentoUnits_EnumCategoryName);
                NoteFlags valueEnum = (NoteFlags)EnumUtility.GetValue(NoteFlags.eCommandFlag.GetType(), value, PortamentoUnits_EnumCategoryName);
                PatchFlags(valueEnum, NoteFlags.ePortamentoUnitsMask, PortamentoUnits_PropertyName);
            }
        }

        public const string DurationAdjustMode_PropertyName = "DurationAdjustMode";
        public const string DurationAdjustMode_EnumCategoryName = "DurationAdjustMode";
        public static Enum[] DurationAdjustModeAllowedValues { get { return new Enum[] { NoteFlags.eDurationAdjustDefault, NoteFlags.eDurationAdjustAdditive, NoteFlags.eDurationAdjustMultiplicative, }; } }
        [Bindable(true)]
        public NoteFlags DurationAdjustMode
        {
            get { return _Flags & NoteFlags.eDurationAdjustMask; }
            set { PatchFlags(value, NoteFlags.eDurationAdjustMask, DurationAdjustMode_PropertyName); }
        }
        [Bindable(true)]
        public string DurationAdjustModeAsString
        {
            get { return EnumUtility.GetDescription(DurationAdjustMode, DurationAdjustMode_EnumCategoryName); }
            set
            {
                string old = EnumUtility.GetDescription(_Flags & NoteFlags.eDurationAdjustMask, DurationAdjustMode_EnumCategoryName);
                NoteFlags valueEnum = (NoteFlags)EnumUtility.GetValue(NoteFlags.eCommandFlag.GetType(), value, DurationAdjustMode_EnumCategoryName);
                PatchFlags(valueEnum, NoteFlags.eDurationAdjustMask, DurationAdjustMode_PropertyName);
            }
        }

        public const string RetriggerEnvelopesOnTie_PropertyName = "RetriggerEnvelopesOnTie";
        [Bindable(true)]
        public bool RetriggerEnvelopesOnTie
        {
            get { return (_Flags & NoteFlags.eRetriggerEnvelopesOnTieFlag) != 0; }
            set { PatchFlags(value ? NoteFlags.eRetriggerEnvelopesOnTieFlag : 0, NoteFlags.eRetriggerEnvelopesOnTieFlag, RetriggerEnvelopesOnTie_PropertyName); }
        }

        public const string NoteIsRest_PropertyName = "NoteIsRest";
        [Bindable(true)]
        public bool NoteIsRest
        {
            get { return (_Flags & NoteFlags.eRestModifier) != 0; }
            set { PatchFlags(value ? NoteFlags.eRestModifier : 0, NoteFlags.eRestModifier, NoteIsRest_PropertyName); }
        }

        public const string DetuningMode_PropertyName = "DetuningMode";
        public const string DetuningMode_EnumCategoryName = "DetuningMode";
        public static Enum[] DetuningModeAllowedValues { get { return new Enum[] { NoteFlags.eDetuningModeDefault, NoteFlags.eDetuningModeHalfSteps, NoteFlags.eDetuningModeHertz, }; } }
        [Bindable(true)]
        public NoteFlags DetuningMode
        {
            get { return _Flags & NoteFlags.eDetuningModeMask; }
            set { PatchFlags(value, NoteFlags.eDetuningModeMask, DetuningMode_PropertyName); }
        }
        [Bindable(true)]
        public string DetuningModeAsString
        {
            get { return EnumUtility.GetDescription(DetuningMode, DetuningMode_EnumCategoryName); }
            set
            {
                string old = EnumUtility.GetDescription(_Flags & NoteFlags.eDetuningModeMask, DetuningMode_EnumCategoryName);
                NoteFlags valueEnum = (NoteFlags)EnumUtility.GetValue(NoteFlags.eCommandFlag.GetType(), value, DetuningMode_EnumCategoryName);
                PatchFlags(valueEnum, NoteFlags.eDetuningModeMask, DetuningMode_PropertyName);
            }
        }

        public const string SharpFlat_PropertyName = "SharpFlat";
        public const string SharpFlat_EnumCategoryName = "SharpFlat";
        [Bindable(true)]
        public NoteFlags SharpFlat
        {
            get { return _Flags & (NoteFlags.eSharpModifier | NoteFlags.eFlatModifier); }
            set { PatchFlags(value, NoteFlags.eSharpModifier | NoteFlags.eFlatModifier, SharpFlat_PropertyName); }
        }

        public const string Duration_PropertyName = "Duration";
        [Bindable(true)]
        public string DurationAsString
        {
            get
            {
                return SymbolicDuration.NumericDurationToString(
                    GetNoteDuration(),
                    GetNoteDotStatus(),
                    GetNoteDurationDivision());
            }
            set
            {
                NoteFlags Duration = GetNoteDuration();
                bool DotFlag /*= GetNoteDotStatus()*/;
                NoteFlags Division /*= GetNoteDurationDivision()*/;
                SymbolicDuration.StringToNumericDuration(value, ref Duration, out DotFlag, out Division);
                PutNoteDuration(Duration);
                PutNoteDotStatus(DotFlag);
                PutNoteDurationDivision(Division);
            }
        }


        private const NoteFlags DefaultNoteFlags = /*duration unspecified | */ NoteFlags.eRelease1FromDefault
            | NoteFlags.eRelease2FromDefault | NoteFlags.ePitchDisplacementStartFromDefault
            | NoteFlags.eDetuningModeDefault | NoteFlags.eDurationAdjustDefault;
        public NoteNoteObjectRec(TrackObjectRec track)
            : base(DefaultNoteFlags, track)
        {
            _OverallLoudnessAdjustment = (SmallBCDType)1;
            _MultisamplePitchAsIf = -1;
            _PitchDisplacementDepthAdjustment = (SmallBCDType)1;
            _PitchDisplacementRateAdjustment = (SmallBCDType)1;
            _HurryUpFactor = (SmallBCDType)1;
        }

        public NoteNoteObjectRec(NoteNoteObjectRec original, TrackObjectRec track)
            : base(DefaultNoteFlags, track)
        {
            CopyFrom(original);
        }

        public NoteNoteObjectRec(BinaryReader reader, int FormatVersionNumber, NoteFlags Opcode, TrackObjectRec track)
            : this(track)
        {
            _Flags = Opcode;

            /* check opcode flags for validity */
            if ((((_Flags & NoteFlags.eDurationMask) != NoteFlags.e64thNote)
                    && ((_Flags & NoteFlags.eDurationMask) != NoteFlags.e32ndNote)
                    && ((_Flags & NoteFlags.eDurationMask) != NoteFlags.e16thNote)
                    && ((_Flags & NoteFlags.eDurationMask) != NoteFlags.e8thNote)
                    && ((_Flags & NoteFlags.eDurationMask) != NoteFlags.e4thNote)
                    && ((_Flags & NoteFlags.eDurationMask) != NoteFlags.e2ndNote)
                    && ((_Flags & NoteFlags.eDurationMask) != NoteFlags.eWholeNote)
                    && ((_Flags & NoteFlags.eDurationMask) != NoteFlags.eDoubleNote)
                    && ((_Flags & NoteFlags.eDurationMask) != NoteFlags.eQuadNote))
                ||
                    (((_Flags & NoteFlags.eFlatModifier) != 0)
                    && ((_Flags & NoteFlags.eSharpModifier) != 0))
                ||
                    (((_Flags & NoteFlags.eRelease1OriginMask) != NoteFlags.eRelease1FromDefault)
                    && ((_Flags & NoteFlags.eRelease1OriginMask) != NoteFlags.eRelease1FromStart)
                    && ((_Flags & NoteFlags.eRelease1OriginMask) != NoteFlags.eRelease1FromEnd))
                ||
                    (((_Flags & NoteFlags.eRelease2OriginMask) != NoteFlags.eRelease2FromDefault)
                    && ((_Flags & NoteFlags.eRelease2OriginMask) != NoteFlags.eRelease2FromStart)
                    && ((_Flags & NoteFlags.eRelease2OriginMask) != NoteFlags.eRelease2FromEnd))
                ||
                    (((_Flags & NoteFlags.ePitchDisplacementStartOriginMask) != NoteFlags.ePitchDisplacementStartFromDefault)
                    && ((_Flags & NoteFlags.ePitchDisplacementStartOriginMask) != NoteFlags.ePitchDisplacementStartFromStart)
                    && ((_Flags & NoteFlags.ePitchDisplacementStartOriginMask) != NoteFlags.ePitchDisplacementStartFromEnd))
                ||
                    (((_Flags & NoteFlags.eDetuningModeMask) != NoteFlags.eDetuningModeDefault)
                    && ((_Flags & NoteFlags.eDetuningModeMask) != NoteFlags.eDetuningModeHalfSteps)
                    && ((_Flags & NoteFlags.eDetuningModeMask) != NoteFlags.eDetuningModeHertz))
                ||
                    (((_Flags & NoteFlags.eDurationAdjustMask) != NoteFlags.eDurationAdjustDefault)
                    && ((_Flags & NoteFlags.eDurationAdjustMask) != NoteFlags.eDurationAdjustAdditive)
                    && ((_Flags & NoteFlags.eDurationAdjustMask) != NoteFlags.eDurationAdjustMultiplicative))
                ||
                    /* we do not check former pitch LFO bits, since someone may have used them, */
                    /* unless we are not in the version 1 file format */
                    ((FormatVersionNumber == 1)
                        && (((_Flags & (NoteFlags.eUnusedBitMask
                            & ~(NoteFlags.eDEALLOCATED17 | NoteFlags.eDEALLOCATED18))) != 0)))
                ||
                    /* non-version 1 formats we check all unused bits */
                    ((FormatVersionNumber != 1) && ((_Flags & NoteFlags.eUnusedBitMask) != 0)))
            {
                throw new InvalidDataException();
            }

            /* this removes former pitch LFO bits, if source was version 1 format file */
            if (FormatVersionNumber == 1)
            {
                _Flags &= ~(NoteFlags.eDEALLOCATED17 | NoteFlags.eDEALLOCATED18);
            }

            /*   2-byte signed little endian pitch index */
            /*       should be a value in the range 0..383.  Middle C (261.6 Hertz) = 192 */
            _Pitch = reader.ReadInt16();
            if ((_Pitch < 0) || (_Pitch >= Constants.NUMNOTES))
            {
                throw new InvalidDataException();
            }

            /*   4-byte unsigned little endian bitmap of presence values.  if a bit */
            /*       is set, then a value of some parameter is explicitly defined */
            /*       in the file, otherwise the default value is used.  bits and */
            /*       defaults are defined as follows: */
            /*          0 - portamento duration (default 0) */
            /*          1 - early/late adjust (default 0) */
            /*          2 - duration adjust (default 0) */
            /*          3 - release 1 location (default 0) */
            /*          4 - release 2 location (default 0) */
            /*          5 - loudness adjust (default 1) */
            /*          6 - stereo position (default 0) */
            /*          7 - surround position (default 0) */
            /*          8 - accent 1 (default 0) */
            /*          9 - accent 2 (default 0) */
            /*         10 - accent 3 (default 0) */
            /*         11 - accent 4 (default 0) */
            /*         12 - accent 5 (default 0) */
            /*         13 - accent 6 (default 0) */
            /*         14 - accent 7 (default 0) */
            /*         15 - accent 8 (default 0) */
            /*         16 - fake pitch (default -1) */
            /*         17 - pitch displacement depth (default 1) */
            /*         18 - pitch displacement rate (default 1) */
            /*         19 - pitch displacement start point (default 0) */
            /*         20 - hurry-up (default 1) */
            /*         21 - detuning (default 0) */
            /*         ALL OTHER BITS SHOULD BE ZERO! */
            Bit Presence;
            if (FormatVersionNumber >= 4)
            {
                /* for version 4 and above, load the presence bitmap and verify */
                Presence = (Bit)reader.ReadUInt32();
                if (0 != (Presence & ~(Bit._0 | Bit._1 | Bit._2 | Bit._3 | Bit._4
                    | Bit._5 | Bit._6 | Bit._7 | Bit._8 | Bit._9 | Bit._10 | Bit._11
                    | Bit._12 | Bit._13 | Bit._14 | Bit._15 | Bit._16 | Bit._17
                    | Bit._18 | Bit._19 | Bit._20 | Bit._21)))
                {
                    throw new InvalidDataException();
                }
            }
            else
            {
                /* for formats 1, 2, and 3, create a fake one which loads the */
                /* verion 3 fields (all except accents 5, 6, 7, and 8) */
                Presence = Bit._0 | Bit._1 | Bit._2 | Bit._3 | Bit._4
                    | Bit._5 | Bit._6 | Bit._7 | Bit._8 | Bit._9 | Bit._10 | Bit._11
                    /*| B.bit12 | B.bit13 | B.bit14 | B.bit15 -- accents 5 through 8 */
                    | Bit._16 | Bit._17 | Bit._18 | Bit._19 | Bit._20 | Bit._21;
            }

            /* use the presence bitmap to load values */

            /*   2-byte little endian small integer coded decimal portamento duration. */
            /*       this determines how long a portamento will last, in fractions of a quarter */
            /*       note.  it only has effect if the note is the target of a tie.  a value of */
            /*       0 means instantaneous, i.e. no portamento. */
            /*       A small integer coded decimal is the decimal * 1000 with a range */
            /*       of -29.999 to 29.999 */
            if (0 != (Presence & Bit._0))
            {
                _PortamentoDuration = reader.ReadSBCD();
            }

            /*   2-byte little endian small integer coded decimal early/late adjustment */
            /*       this determines the displacement in time of the occurrence of the note */
            /*       in frations of a quarter note. */
            if (0 != (Presence & Bit._1))
            {
                _EarlyLateAdjust = reader.ReadSBCD();
            }

            /*   2-byte little endian small integer coded decimal duration adjustment */
            /*       this value changes the duration of the note by being added to the */
            /*       duration or being multiplied by the duration. */
            if (0 != (Presence & Bit._2))
            {
                _DurationAdjust = reader.ReadSBCD();
            }

            /*   2-byte little endian small integer coded decimal release point 1 location */
            /*       this determines when the release of the first sustain/loop will occur */
            /*       in fractions of the current note's duration.  it is relative to the origin */
            /*       as determined by the opcode field. */
            if (0 != (Presence & Bit._3))
            {
                _ReleasePoint1 = reader.ReadSBCD();
            }

            /*   2-byte little endian small integer coded decimal release point 2 location */
            /*       this determines when the release of the second sustain/loop will occur. */
            if (0 != (Presence & Bit._4))
            {
                _ReleasePoint2 = reader.ReadSBCD();
            }

            /*   2-byte little endian small integer coded decimal overall loudness adjustment */
            /*       this factor scales the total volume output of the oscillators for this */
            /*       particular note.  It is multiplied, so a value of 1 makes no change in */
            /*       loudness. */
            if (0 != (Presence & Bit._5))
            {
                _OverallLoudnessAdjustment = reader.ReadSBCD();
            }

            /*   2-byte little endian small integer coded decimal stereo position adjustment. */
            /*       this value adjusts where the sound will be located in stereo.  -1 is */
            /*       the far left, 1 is the far right, and 0 is center. */
            if (0 != (Presence & Bit._6))
            {
                _StereoPositionAdjustment = reader.ReadSBCD();
            }

            /*   2-byte little endian small integer coded decimal surround position adjustment. */
            /*       this value adjusts where the sound will be located in surround. */
            /*       1 is front and -1 is rear. */
            if (0 != (Presence & Bit._7))
            {
                _SurroundPositionAdjustment = reader.ReadSBCD();
            }

            /*   2-byte little endian small integer coded decimal accent 1 value */
            if (0 != (Presence & Bit._8))
            {
                _Accent1 = reader.ReadSBCD();
            }

            /*   2-byte little endian small integer coded decimal accent 2 value */
            if (0 != (Presence & Bit._9))
            {
                _Accent2 = reader.ReadSBCD();
            }

            /*   2-byte little endian small integer coded decimal accent 3 value */
            if (0 != (Presence & Bit._10))
            {
                _Accent3 = reader.ReadSBCD();
            }

            /*   2-byte little endian small integer coded decimal accent 4 value */
            if (0 != (Presence & Bit._11))
            {
                _Accent4 = reader.ReadSBCD();
            }

            /*   2-byte little endian small integer coded decimal accent 5 value */
            /*      (new with version 4 format) */
            if (0 != (Presence & Bit._12))
            {
                _Accent5 = reader.ReadSBCD();
            }

            /*   2-byte little endian small integer coded decimal accent 6 value */
            /*      (new with version 4 format) */
            if (0 != (Presence & Bit._13))
            {
                _Accent6 = reader.ReadSBCD();
            }

            /*   2-byte little endian small integer coded decimal accent 7 value */
            /*      (new with version 4 format) */
            if (0 != (Presence & Bit._14))
            {
                _Accent7 = reader.ReadSBCD();
            }

            /*   2-byte little endian small integer coded decimal accent 8 value */
            /*      (new with version 4 format) */
            if (0 != (Presence & Bit._15))
            {
                _Accent8 = reader.ReadSBCD();
            }

            /*   2-byte little endian fake pitch value */
            /*       this value has a range of -1..383.  If it is not -1, then it will be used */
            /*       to determine which sample a multisampled oscillator will use.  If it is -1 */
            /*       then the actual pitch will be used to select a sample. */
            if (0 != (Presence & Bit._16))
            {
                _MultisamplePitchAsIf = reader.ReadInt16();
                if ((_MultisamplePitchAsIf < -1) || (_MultisamplePitchAsIf >= Constants.NUMNOTES))
                {
                    throw new InvalidDataException();
                }
            }

            /*   2-byte little endian small integer coded decimal pitch disp depth adjustment */
            /*       this adjusts the maximum amplitude of the pitch displacement depth */
            /*       oscillator (vibrato).  The value has units of either half steps or hertz */
            /*       depending on the setting in the opcode word. */
            if (0 != (Presence & Bit._17))
            {
                _PitchDisplacementDepthAdjustment = reader.ReadSBCD();
            }

            /*   2-byte little endian small integer coded decimal pitch displ rate adjustment */
            /*       this adjusts the maximum amplitude of the pitch displacement rate */
            /*       oscillator. the units are periods per second. */
            if (0 != (Presence & Bit._18))
            {
                _PitchDisplacementRateAdjustment = reader.ReadSBCD();
            }

            /*   2-byte little endian small integer coded decimal pitch displ start point adjust */
            /*       this value adjusts when the pitch displacement envelopes start.  the */
            /*       location is from start or end of note, depending on the opcode settings, and */
            /*       is in fractions of the current note's duration. */
            if (0 != (Presence & Bit._19))
            {
                _PitchDisplacementStartPoint = reader.ReadSBCD();
            }

            /*   2-byte little endian small integer coded decimal hurry-up factor */
            /*       this factor scales the total speed at which all envelopes change.  this is */
            /*       multiplicative, so a value of 1 makes no change, and smaller values make */
            /*       transitions go faster. */
            if (0 != (Presence & Bit._20))
            {
                _HurryUpFactor = reader.ReadSBCD();
            }

            /*   2-byte little endian small integer coded decimal detuning value */
            /*       this value is added to the pitch of the note to detune.  its units are */
            /*       either hertz or half steps depending on the opcode word. */
            if (0 != (Presence & Bit._21))
            {
                _Detuning = reader.ReadSBCD();
            }
        }

        public override void Save(BinaryWriter writer)
        {
            /*   4-byte unsigned little endian opcode field */
            /*       for a command, the high bit will be 1 and the remaining bits will */
            /*       be the opcode.  for a note, the high bit will be 0. */
            writer.WriteUInt32((uint)_Flags);

            /*   2-byte signed little endian pitch index */
            /*       should be a value in the range 0..383.  Middle C (261.6 Hertz) = 192 */
            writer.WriteInt16(_Pitch);

            /* now build this bitmap thing */
            /*   4-byte unsigned little endian bitmap of presence values.  if a bit */
            /*       is set, then a value of some parameter is explicitly defined */
            /*       in the file, otherwise the default value is used.  bits and */
            /*       defaults are defined as follows: */
            /*          0 - portamento duration (default 0) */
            /*          1 - early/late adjust (default 0) */
            /*          2 - duration adjust (default 0) */
            /*          3 - release 1 location (default 0) */
            /*          4 - release 2 location (default 0) */
            /*          5 - loudness adjust (default 1) */
            /*          6 - stereo position (default 0) */
            /*          7 - surround position (default 0) */
            /*          8 - accent 1 (default 0) */
            /*          9 - accent 2 (default 0) */
            /*         10 - accent 3 (default 0) */
            /*         11 - accent 4 (default 0) */
            /*         12 - accent 5 (default 0) */
            /*         13 - accent 6 (default 0) */
            /*         14 - accent 7 (default 0) */
            /*         15 - accent 8 (default 0) */
            /*         16 - fake pitch (default -1) */
            /*         17 - pitch displacement depth (default 1) */
            /*         18 - pitch displacement rate (default 1) */
            /*         19 - pitch displacement start point (default 0) */
            /*         20 - hurry-up (default 1) */
            /*         21 - detuning (default 0) */
            /*         ALL OTHER BITS SHOULD BE ZERO! */
            Bit Presence = 0;
            if (_PortamentoDuration.rawInt16 != 0)
            {
                Presence |= Bit._0;
            }
            if (_EarlyLateAdjust.rawInt16 != 0)
            {
                Presence |= Bit._1;
            }
            if (_DurationAdjust.rawInt16 != 0)
            {
                Presence |= Bit._2;
            }
            if (_ReleasePoint1.rawInt16 != 0)
            {
                Presence |= Bit._3;
            }
            if (_ReleasePoint2.rawInt16 != 0)
            {
                Presence |= Bit._4;
            }
            if (_OverallLoudnessAdjustment.rawInt16 != ((SmallBCDType)1).rawInt16)
            {
                Presence |= Bit._5;
            }
            if (_StereoPositionAdjustment.rawInt16 != 0)
            {
                Presence |= Bit._6;
            }
            if (_SurroundPositionAdjustment.rawInt16 != 0)
            {
                Presence |= Bit._7;
            }
            if (_Accent1.rawInt16 != 0)
            {
                Presence |= Bit._8;
            }
            if (_Accent2.rawInt16 != 0)
            {
                Presence |= Bit._9;
            }
            if (_Accent3.rawInt16 != 0)
            {
                Presence |= Bit._10;
            }
            if (_Accent4.rawInt16 != 0)
            {
                Presence |= Bit._11;
            }
            if (_Accent5.rawInt16 != 0)
            {
                Presence |= Bit._12;
            }
            if (_Accent6.rawInt16 != 0)
            {
                Presence |= Bit._13;
            }
            if (_Accent7.rawInt16 != 0)
            {
                Presence |= Bit._14;
            }
            if (_Accent8.rawInt16 != 0)
            {
                Presence |= Bit._15;
            }
            if (_MultisamplePitchAsIf != -1)
            {
                Presence |= Bit._16;
            }
            if (_PitchDisplacementDepthAdjustment.rawInt16 != ((SmallBCDType)1).rawInt16)
            {
                Presence |= Bit._17;
            }
            if (_PitchDisplacementRateAdjustment.rawInt16 != ((SmallBCDType)1).rawInt16)
            {
                Presence |= Bit._18;
            }
            if (_PitchDisplacementStartPoint.rawInt16 != 0)
            {
                Presence |= Bit._19;
            }
            if (_HurryUpFactor.rawInt16 != ((SmallBCDType)1).rawInt16)
            {
                Presence |= Bit._20;
            }
            if (_Detuning.rawInt16 != 0)
            {
                Presence |= Bit._21;
            }
            /* write the thing out */
            writer.WriteUInt32((uint)Presence);

            /* write the rest of the fields as we determined their necessity */

            /*   2-byte little endian small integer coded decimal portamento duration. */
            /*       this determines how long a portamento will last, in fractions of a quarter */
            /*       note.  it only has effect if the note is the target of a tie.  a value of */
            /*       0 means instantaneous, i.e. no portamento. */
            if (0 != (Presence & Bit._0))
            {
                writer.WriteSBCD(_PortamentoDuration);
            }

            /*   2-byte little endian small integer coded decimal early/late adjustment */
            /*       this determines the displacement in time of the occurrence of the note */
            /*       in frations of a quarter note. */
            if (0 != (Presence & Bit._1))
            {
                writer.WriteSBCD(_EarlyLateAdjust);
            }

            /*   2-byte little endian small integer coded decimal duration adjustment */
            /*       this value changes the duration of the note by being added to the */
            /*       duration or being multiplied by the duration. */
            if (0 != (Presence & Bit._2))
            {
                writer.WriteSBCD(_DurationAdjust);
            }

            /*   2-byte little endian small integer coded decimal release point 1 location */
            /*       this determines when the release of the first sustain/loop will occur */
            /*       in fractions of the current note's duration.  it is relative to the origin */
            /*       as determined by the opcode field. */
            if (0 != (Presence & Bit._3))
            {
                writer.WriteSBCD(_ReleasePoint1);
            }

            /*   2-byte little endian small integer coded decimal release point 2 location */
            /*       this determines when the release of the second sustain/loop will occur. */
            if (0 != (Presence & Bit._4))
            {
                writer.WriteSBCD(_ReleasePoint2);
            }

            /*   2-byte little endian small integer coded decimal overall loudness adjustment */
            /*       this factor scales the total volume output of the oscillators for this */
            /*       particular note.  It is multiplied, so a value of 1 makes no change in */
            /*       loudness. */
            if (0 != (Presence & Bit._5))
            {
                writer.WriteSBCD(_OverallLoudnessAdjustment);
            }

            /*   2-byte little endian small integer coded decimal stereo position adjustment. */
            /*       this value adjusts where the sound will be located in stereo.  -1 is */
            /*       the far left, 1 is the far right, and 0 is center. */
            if (0 != (Presence & Bit._6))
            {
                writer.WriteSBCD(_StereoPositionAdjustment);
            }

            /*   2-byte little endian small integer coded decimal surround position adjustment. */
            /*       this value adjusts where the sound will be located in surroud. */
            /*       1 is front and -1 is rear */
            if (0 != (Presence & Bit._7))
            {
                writer.WriteSBCD(_SurroundPositionAdjustment);
            }

            /*   2-byte little endian small integer coded decimal accent 1 value */
            if (0 != (Presence & Bit._8))
            {
                writer.WriteSBCD(_Accent1);
            }

            /*   2-byte little endian small integer coded decimal accent 2 value */
            if (0 != (Presence & Bit._9))
            {
                writer.WriteSBCD(_Accent2);
            }

            /*   2-byte little endian small integer coded decimal accent 3 value */
            if (0 != (Presence & Bit._10))
            {
                writer.WriteSBCD(_Accent3);
            }

            /*   2-byte little endian small integer coded decimal accent 4 value */
            if (0 != (Presence & Bit._11))
            {
                writer.WriteSBCD(_Accent4);
            }

            /*   2-byte little endian small integer coded decimal accent 5 value */
            /*      (new with version 4 format) */
            if (0 != (Presence & Bit._12))
            {
                writer.WriteSBCD(_Accent5);
            }

            /*   2-byte little endian small integer coded decimal accent 6 value */
            /*      (new with version 4 format) */
            if (0 != (Presence & Bit._13))
            {
                writer.WriteSBCD(_Accent6);
            }

            /*   2-byte little endian small integer coded decimal accent 7 value */
            /*      (new with version 4 format) */
            if (0 != (Presence & Bit._14))
            {
                writer.WriteSBCD(_Accent7);
            }

            /*   2-byte little endian small integer coded decimal accent 8 value */
            /*      (new with version 4 format) */
            if (0 != (Presence & Bit._15))
            {
                writer.WriteSBCD(_Accent8);
            }

            /*   2-byte little endian fake pitch value */
            /*       this value has a range of -1..383.  If it is not -1, then it will be used */
            /*       to determine which sample a multisampled oscillator will use.  If it is -1 */
            /*       then the actual pitch will be used to select a sample. */
            if (0 != (Presence & Bit._16))
            {
                writer.WriteInt16(_MultisamplePitchAsIf);
            }

            /*   2-byte little endian small integer coded decimal pitch disp depth adjustment */
            /*       this adjusts the maximum amplitude of the pitch displacement depth */
            /*       oscillator (vibrato).  The value has units of either half steps or hertz */
            /*       depending on the setting in the opcode word. */
            if (0 != (Presence & Bit._17))
            {
                writer.WriteSBCD(_PitchDisplacementDepthAdjustment);
            }

            /*   2-byte little endian small integer coded decimal pitch displ rate adjustment */
            /*       this adjusts the maximum amplitude of the pitch displacement rate */
            /*       oscillator. the units are periods per second. */
            if (0 != (Presence & Bit._18))
            {
                writer.WriteSBCD(_PitchDisplacementRateAdjustment);
            }

            /*   2-byte little endian small integer coded decimal pitch displ start point adjust */
            /*       this value adjusts when the pitch displacement envelopes start.  the */
            /*       location is from start or end of note, depending on the opcode settings, and */
            /*       is in fractions of the current note's duration. */
            if (0 != (Presence & Bit._19))
            {
                writer.WriteSBCD(_PitchDisplacementStartPoint);
            }

            /*   2-byte little endian small integer coded decimal hurry-up factor */
            /*       this factor scales the total speed at which all envelopes change.  this is */
            /*       multiplicative, so a value of 1 makes no change, and smaller values make */
            /*       transitions go faster. */
            if (0 != (Presence & Bit._20))
            {
                writer.WriteSBCD(_HurryUpFactor);
            }

            /*   2-byte little endian small integer coded decimal detuning value */
            /*       this value is added to the pitch of the note to detune.  its units are */
            /*       either hertz or half steps depending on the opcode word. */
            if (0 != (Presence & Bit._21))
            {
                writer.WriteSBCD(_Detuning);
            }
        }


        // Old style accessors - get value

        public NoteNoteObjectRec GetNoteTieTarget()
        {
            return _Tie;
        }

        public short GetNotePitch()
        {
            return _Pitch;
        }

        public NoteFlags GetNoteFlatOrSharpStatus()
        {
            return _Flags & (NoteFlags.eFlatModifier | NoteFlags.eSharpModifier);
        }

        public NoteFlags GetNoteDuration()
        {
            return _Flags & NoteFlags.eDurationMask;
        }

        public NoteFlags GetNoteDurationDivision()
        {
            return _Flags & NoteFlags.eDivisionMask;
        }

        public bool GetNoteDotStatus()
        {
            return (_Flags & NoteFlags.eDotModifier) != 0;
        }

        public bool GetNoteIsItARest()
        {
            return (_Flags & NoteFlags.eRestModifier) != 0;
        }

        public bool GetNoteRetriggerEnvelopesOnTieStatus()
        {
            return (_Flags & NoteFlags.eRetriggerEnvelopesOnTieFlag) != 0;
        }

        public double GetNotePortamentoDuration()
        {
            return (double)_PortamentoDuration;
        }

        /* get a flag indicating whether portamento leads or trails beat */
        public bool GetNotePortamentoLeadsBeatFlag()
        {
            return (_Flags & NoteFlags.ePortamentoLeadsNote) != 0;
        }

        public double GetNoteEarlyLateAdjust()
        {
            return (double)_EarlyLateAdjust;
        }

        public double GetNoteDetuning()
        {
            return (double)_Detuning;
        }

        public NoteFlags GetNoteDetuneConversionMode()
        {
            return Flags & NoteFlags.eDetuningModeMask;
        }

        public double GetNoteOverallLoudnessAdjustment()
        {
            return (double)_OverallLoudnessAdjustment;
        }

        public double GetNoteDurationAdjust()
        {
            return (double)_DurationAdjust;
        }

        public NoteFlags GetNoteDurationAdjustMode()
        {
            return Flags & NoteFlags.eDurationAdjustMask;
        }

        public double GetNoteReleasePoint1()
        {
            return (double)_ReleasePoint1;
        }

        public NoteFlags GetNoteRelease1Origin()
        {
            return Flags & NoteFlags.eRelease1OriginMask;
        }

        public double GetNoteReleasePoint2()
        {
            return (double)_ReleasePoint2;
        }

        public NoteFlags GetNoteRelease2Origin()
        {
            return Flags & NoteFlags.eRelease2OriginMask;
        }

        public bool GetNoteRelease3FromStartInsteadOfEnd()
        {
            return (Flags & NoteFlags.eRelease3FromStartNotEnd) != 0;
        }

        public bool GetNotePortamentoHertzNotHalfstepsFlag()
        {
            return (Flags & NoteFlags.ePortamentoUnitsHertzNotHalfsteps) != 0;
        }

        public double GetNoteStereoPositioning()
        {
            return (double)_StereoPositionAdjustment;
        }

        public double GetNoteSurroundPositioning()
        {
            return (double)_SurroundPositionAdjustment;
        }

        public double GetNotePitchDisplacementDepthAdjust()
        {
            return (double)_PitchDisplacementDepthAdjustment;
        }

        public NoteFlags GetNotePitchDisplacementStartOrigin()
        {
            return Flags & NoteFlags.ePitchDisplacementStartOriginMask;
        }

        public double GetNotePitchDisplacementRateAdjust()
        {
            return (double)_PitchDisplacementRateAdjustment;
        }

        public double GetNotePitchDisplacementStartPoint()
        {
            return (double)_PitchDisplacementStartPoint;
        }

        public double GetNoteHurryUpFactor()
        {
            return (double)_HurryUpFactor;
        }

        /* get the pitch that the table selector should treat the note as using */
        public short GetNoteMultisampleFalsePitch()
        {
            return _MultisamplePitchAsIf;
        }

        public double GetNoteAccent1()
        {
            return (double)_Accent1;
        }

        public double GetNoteAccent2()
        {
            return (double)_Accent2;
        }

        public double GetNoteAccent3()
        {
            return (double)_Accent3;
        }

        public double GetNoteAccent4()
        {
            return (double)_Accent4;
        }

        public double GetNoteAccent5()
        {
            return (double)_Accent5;
        }

        public double GetNoteAccent6()
        {
            return (double)_Accent6;
        }

        public double GetNoteAccent7()
        {
            return (double)_Accent7;
        }

        public double GetNoteAccent8()
        {
            return (double)_Accent8;
        }

        /* convert the duration of the note into a fraction.  the numbers are not reduced */
        /* with the aim of eliminating as much factoring as possible to reduce the number */
        /* of lengthy multiplies and divides. */
        public void GetNoteDurationFrac(out FractionRec Frac)
        {
            ConvertDurationFrac(_Flags, out Frac);
        }

        /* convert a duration opcode into a fraction */
        public static void ConvertDurationFrac(NoteFlags DurationOpcode, out FractionRec Frac)
        {
            uint Temp;

            /* the denominator factors represent the following: */
            /* 64 for 1/64th note, 3*5*7 for divided notes, and 2 for 3/2 dotted notes */
            switch (DurationOpcode & NoteFlags.eDurationMask)
            {
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();
                case NoteFlags.e64thNote:
                    Temp = 1 * Constants.Denominator / 64;
                    break;
                case NoteFlags.e32ndNote:
                    Temp = 1 * Constants.Denominator / 32;
                    break;
                case NoteFlags.e16thNote:
                    Temp = 1 * Constants.Denominator / 16;
                    break;
                case NoteFlags.e8thNote:
                    Temp = 1 * Constants.Denominator / 8;
                    break;
                case NoteFlags.e4thNote:
                    Temp = 1 * Constants.Denominator / 4;
                    break;
                case NoteFlags.e2ndNote:
                    Temp = 1 * Constants.Denominator / 2;
                    break;
                case NoteFlags.eWholeNote:
                    Temp = 1 * Constants.Denominator / 1;
                    break;
                case NoteFlags.eDoubleNote:
                    Temp = 2 * Constants.Denominator / 1;
                    break;
                case NoteFlags.eQuadNote:
                    Temp = 4 * Constants.Denominator / 1;
                    break;
            }
            if ((DurationOpcode & NoteFlags.eDotModifier) != 0)
            {
                /* since the denominator is (64*3*5*7*2), with a 2 in it, we only have */
                /* to multiply note by 3/2 */
                Temp = (3 * Temp) / 2;
            }
            switch (DurationOpcode & NoteFlags.eDivisionMask)
            {
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();
                case NoteFlags.eDiv1Modifier:
                    break;
                case NoteFlags.eDiv3Modifier:
                    /* since the denominator is (64*3*5*7*2), with a 3 in it, we only have */
                    /* to multiply note by 1/3 */
                    Temp = Temp / 3;
                    break;
                case NoteFlags.eDiv5Modifier:
                    /* since the denominator is (64*3*5*7*2), with a 5 in it, we only have */
                    /* to multiply note by 1/5 */
                    Temp = Temp / 5;
                    break;
                case NoteFlags.eDiv7Modifier:
                    /* since the denominator is (64*3*5*7*2), with a 7 in it, we only have */
                    /* to multiply note by 1/7 */
                    Temp = Temp / 7;
                    break;
            }
            Frac = new FractionRec(Temp / Constants.Denominator, Temp % Constants.Denominator, Constants.Denominator);
        }


        // Old-school accessors - put value
        // These all use the binding property rather than the member variable.
        // Therefore, though it is not written here, these all fire Changed() event.

        public void PutNoteTieTarget(NoteNoteObjectRec target)
        {
            Tie = target; // use property.set accessor - fires Changed()
        }

        public void PutNotePitch(short NewPitch)
        {
            if ((NewPitch < 0) || (NewPitch >= Constants.NUMNOTES))
            {
                // pitch value is out of range
                Debug.Assert(false);
                throw new ArgumentException();
            }
            Pitch = NewPitch;
        }

        public void PutNoteDuration(NoteFlags NewDuration)
        {
            if ((NewDuration != NoteFlags.e64thNote) && (NewDuration != NoteFlags.e32ndNote)
                && (NewDuration != NoteFlags.e16thNote) && (NewDuration != NoteFlags.e8thNote)
                && (NewDuration != NoteFlags.e4thNote) && (NewDuration != NoteFlags.e2ndNote)
                && (NewDuration != NoteFlags.eWholeNote) && (NewDuration != NoteFlags.eDoubleNote)
                && (NewDuration != NoteFlags.eQuadNote))
            {
                // bad duration value
                Debug.Assert(false);
                throw new ArgumentException();
            }
            Flags_Settable = (Flags & ~NoteFlags.eDurationMask) | NewDuration;
        }

        public void PutNoteDurationDivision(NoteFlags NewDivision)
        {
            if ((NewDivision != NoteFlags.eDiv1Modifier) && (NewDivision != NoteFlags.eDiv3Modifier)
                && (NewDivision != NoteFlags.eDiv5Modifier) && (NewDivision != NoteFlags.eDiv7Modifier))
            {
                // bad division value
                Debug.Assert(false);
                throw new ArgumentException();
            }
            Flags_Settable = (Flags & ~NoteFlags.eDivisionMask) | NewDivision;
        }

        public void PutNoteDotStatus(bool HasADot)
        {
            if (HasADot)
            {
                Flags_Settable |= NoteFlags.eDotModifier;
            }
            else
            {
                Flags_Settable &= ~NoteFlags.eDotModifier;
            }
        }

        public void PutNoteFlatOrSharpStatus(NoteFlags NewFlatOrSharpStatus)
        {
            if ((NewFlatOrSharpStatus != 0) && (NewFlatOrSharpStatus != NoteFlags.eFlatModifier)
                && (NewFlatOrSharpStatus != NoteFlags.eSharpModifier))
            {
                // bad sharp or flat status
                Debug.Assert(false);
                throw new ArgumentException();
            }
            Flags_Settable = (Flags & ~(NoteFlags.eSharpModifier | NoteFlags.eFlatModifier)) | NewFlatOrSharpStatus;
        }

        /* change the rest status of a note */
        public void PutNoteIsItARest(bool IsARest)
        {
            if (IsARest)
            {
                Flags_Settable |= NoteFlags.eRestModifier;
            }
            else
            {
                Flags_Settable &= ~NoteFlags.eRestModifier;
            }
        }

        /* change the pitch that the table selector should treat the note as using */
        public void PutNoteMultisampleFalsePitch(short NewFalsePitch)
        {
            MultisamplePitchAsIf = NewFalsePitch;
        }

        public void PutNoteOverallLoudnessAdjustment(double NewLoudnessAdjust)
        {
            this.OverallLoudnessAdjustment = NewLoudnessAdjust;
        }

        public void PutNoteEarlyLateAdjust(double NewEarlyLate)
        {
            this.EarlyLateAdjust = NewEarlyLate;
        }

        public void PutNoteDurationAdjust(double NewDurationAdjust)
        {
            this.DurationAdjust = NewDurationAdjust;
        }

        public void PutNoteDurationAdjustMode(NoteFlags NewDurationAdjustMode)
        {
            Debug.Assert((NewDurationAdjustMode == NoteFlags.eDurationAdjustDefault)
                || (NewDurationAdjustMode == NoteFlags.eDurationAdjustAdditive)
                || (NewDurationAdjustMode == NoteFlags.eDurationAdjustMultiplicative));
            this.DurationAdjustMode = NewDurationAdjustMode;
        }

        public void PutNoteReleasePoint1(double NewReleasePoint1)
        {
            this.ReleasePoint1 = NewReleasePoint1;
        }

        public void PutNoteRelease1Origin(NoteFlags NewReleasePoint1Origin)
        {
            Debug.Assert((NewReleasePoint1Origin == NoteFlags.eRelease1FromDefault)
                || (NewReleasePoint1Origin == NoteFlags.eRelease1FromStart)
                || (NewReleasePoint1Origin == NoteFlags.eRelease1FromEnd));
            this.ReleasePoint1Origin = NewReleasePoint1Origin;
        }

        public void PutNoteReleasePoint2(double NewReleasePoint2)
        {
            this.ReleasePoint2 = NewReleasePoint2;
        }

        public void PutNoteRelease2Origin(NoteFlags NewReleasePoint2Origin)
        {
            Debug.Assert((NewReleasePoint2Origin == NoteFlags.eRelease2FromDefault)
                || (NewReleasePoint2Origin == NoteFlags.eRelease2FromStart)
                || (NewReleasePoint2Origin == NoteFlags.eRelease2FromEnd));
            this.ReleasePoint2Origin = NewReleasePoint2Origin;
        }

        public void PutNoteRelease3FromStartInsteadOfEnd(bool ShouldWeReleasePoint3FromStartInsteadOfEnd)
        {
            this.ReleasePoint3Origin = ShouldWeReleasePoint3FromStartInsteadOfEnd ? NoteFlags.eRelease3FromStartNotEnd : 0;
        }

        public void PutNotePortamentoDuration(double NewPortamentoDuration)
        {
            this.PortamentoDuration = NewPortamentoDuration;
        }

        public void PutNotePortamentoHertzNotHalfstepsFlag(bool ShouldWeUseHertzInsteadOfHalfsteps)
        {
            this.PortamentoUnits = ShouldWeUseHertzInsteadOfHalfsteps ? NoteFlags.ePortamentoUnitsHertzNotHalfsteps : 0;
        }

        public void PutNoteStereoPositioning(double NewStereoPosition)
        {
            this.StereoPositionAdjustment = NewStereoPosition;
        }

        public void PutNotePitchDisplacementDepthAdjust(double NewPitchDisplacementDepthAdjust)
        {
            this.PitchDisplacementDepthAdjustment = NewPitchDisplacementDepthAdjust;
        }

        public void PutNotePitchDisplacementStartOrigin(NoteFlags NewPitchDisplacementStartOrigin)
        {
            Debug.Assert((NewPitchDisplacementStartOrigin == NoteFlags.ePitchDisplacementStartFromDefault)
                || (NewPitchDisplacementStartOrigin == NoteFlags.ePitchDisplacementStartFromStart)
                || (NewPitchDisplacementStartOrigin == NoteFlags.ePitchDisplacementStartFromEnd));
            this.PitchDisplacementOrigin = NewPitchDisplacementStartOrigin;
        }

        public void PutNotePitchDisplacementRateAdjust(double NewPitchDisplacementRateAdjust)
        {
            this.PitchDisplacementRateAdjustment = NewPitchDisplacementRateAdjust;
        }

        public void PutNotePitchDisplacementStartPoint(double NewPitchDisplacementStartPoint)
        {
            this.PitchDisplacementStartPoint = NewPitchDisplacementStartPoint;
        }

        public void PutNoteHurryUpFactor(double NewHurryUpFactor)
        {
            this.HurryUpFactor = NewHurryUpFactor;
        }

        public void PutNoteDetuning(double NewDetuning)
        {
            this.Detuning = NewDetuning;
        }

        public void PutNoteDetuneConversionMode(NoteFlags NewDetuneConversionMode)
        {
            Debug.Assert((NewDetuneConversionMode == NoteFlags.eDetuningModeDefault)
                || (NewDetuneConversionMode == NoteFlags.eDetuningModeHalfSteps)
                || (NewDetuneConversionMode == NoteFlags.eDetuningModeHertz));
            this.DetuningMode = NewDetuneConversionMode;
        }

        public void PutNoteSurroundPositioning(double NewSurroundPosition)
        {
            this.SurroundPositionAdjustment = NewSurroundPosition;
        }

        public void PutNotePortamentoLeadsBeatFlag(bool ShouldPortamentoLeadBeat)
        {
            this.PortamentoLeadsBeat = ShouldPortamentoLeadBeat;
        }

        public void PutNoteRetriggerEnvelopesOnTieStatus(bool ShouldWeRetriggerEnvelopesOnTie)
        {
            this.RetriggerEnvelopesOnTie = ShouldWeRetriggerEnvelopesOnTie;
        }

        public void PutNoteAccent1(double NewAccent1)
        {
            this.Accent1 = NewAccent1;
        }

        public void PutNoteAccent2(double NewAccent2)
        {
            this.Accent2 = NewAccent2;
        }

        public void PutNoteAccent3(double NewAccent3)
        {
            this.Accent3 = NewAccent3;
        }

        public void PutNoteAccent4(double NewAccent4)
        {
            this.Accent4 = NewAccent4;
        }

        public void PutNoteAccent5(double NewAccent5)
        {
            this.Accent5 = NewAccent5;
        }

        public void PutNoteAccent6(double NewAccent6)
        {
            this.Accent6 = NewAccent6;
        }

        public void PutNoteAccent7(double NewAccent7)
        {
            this.Accent7 = NewAccent7;
        }

        public void PutNoteAccent8(double NewAccent8)
        {
            this.Accent8 = NewAccent8;
        }

        public void CopyFrom(NoteNoteObjectRec source)
        {
            this.Flags_Settable = source._Flags;
            this.Accent1 = source.Accent1;
            this.Accent2 = source.Accent2;
            this.Accent3 = source.Accent3;
            this.Accent4 = source.Accent4;
            this.Accent5 = source.Accent5;
            this.Accent6 = source.Accent6;
            this.Accent7 = source.Accent7;
            this.Accent8 = source.Accent8;
            this.Detuning = source.Detuning;
            this.DurationAdjust = source.DurationAdjust;
            this.EarlyLateAdjust = source.EarlyLateAdjust;
            this.HurryUpFactor = source.HurryUpFactor;
            this.MultisamplePitchAsIf = source.MultisamplePitchAsIf;
            this.OverallLoudnessAdjustment = source.OverallLoudnessAdjustment;
            this.Pitch = source.Pitch;
            this.PitchDisplacementDepthAdjustment = source.PitchDisplacementDepthAdjustment;
            this.PitchDisplacementRateAdjustment = source.PitchDisplacementRateAdjustment;
            this.PitchDisplacementStartPoint = source.PitchDisplacementStartPoint;
            this.PortamentoDuration = source.PortamentoDuration;
            this.ReleasePoint1 = source.ReleasePoint1;
            this.ReleasePoint2 = source.ReleasePoint2;
            this.StereoPositionAdjustment = source.StereoPositionAdjustment;
            this.SurroundPositionAdjustment = source.SurroundPositionAdjustment;
            this.Tie = source.Tie;
        }
    }
}
