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
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    public interface IValueInfoOwner
    {
        void BeginFieldEdit(ValueInfo valueInfo, int noteIndex, string initialText);
        void SaveUndoInfo(string tag);
        Control FocusReturnsTo { get; }
    }

    public abstract class ValueInfo
    {
        public readonly string Tag;
        public readonly string Description;
        public readonly bool SpacerFollows;
        public abstract string GetValue(NoteNoteObjectRec note);
        public abstract string GetDefaultValue();
        public abstract void SetValue(NoteNoteObjectRec note, string value);
        public abstract void DoClick(NoteNoteObjectRec note, int noteIndex, IValueInfoOwner owner);
        public readonly InlineParamVis InlineParam = InlineParamVis.None;

        protected ValueInfo(string Tag, string Description, bool SpacerFollows, InlineParamVis inlineParam)
        {
            this.Tag = Tag;
            this.Description = Description;
            this.SpacerFollows = SpacerFollows;
            this.InlineParam = inlineParam;
        }

        public static ValueInfo FindInlineParamVis(InlineParamVis vis)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if ((values[i] != null) && (values[i].InlineParam == vis))
                {
                    return values[i];
                }
            }
            Debug.Assert(false);
            throw new ArgumentException();
        }


        public delegate T _GetValue<T>(NoteNoteObjectRec note);
        public delegate void _SetValue<T>(NoteNoteObjectRec note, T value);
        public struct VT<T>
        {
            public readonly T value;
            public readonly string text;

            public VT(T value, string text)
            {
                this.value = value;
                this.text = text;
            }
        }


        private class ValueInfoDouble : ValueInfo
        {
            private readonly _GetValue<double> getValue;
            private readonly double defaultValue;
            private readonly _SetValue<double> setValue;

            public ValueInfoDouble(
                string tag,
                _GetValue<double> getValue,
                double defaultValue,
                string description,
                bool spacerFollows,
                _SetValue<double> setValue,
                InlineParamVis inlineParam)
                : base(tag, description, spacerFollows, inlineParam)
            {
                this.getValue = getValue;
                this.defaultValue = defaultValue;
                this.setValue = setValue;
            }

            public override string GetValue(NoteNoteObjectRec note)
            {
                return getValue(note).ToString();
            }

            public override string GetDefaultValue()
            {
                return defaultValue.ToString();
            }

            public override void SetValue(NoteNoteObjectRec note, string value)
            {
                double d;
                if (Double.TryParse(value, out d))
                {
                    setValue(note, d);
                }
            }

            public override void DoClick(NoteNoteObjectRec note, int noteIndex, IValueInfoOwner owner)
            {
                owner.BeginFieldEdit(this, noteIndex, GetValue(note));
            }
        }

        private class ValueInfoEnum<T> : ValueInfo
        {
            private readonly _GetValue<T> getValue;
            private readonly _SetValue<T> setValue;
            private readonly VT<T>[] possibleValues;

            public ValueInfoEnum(
                string tag,
                _GetValue<T> getValue,
                string description,
                bool spacerFollows,
                _SetValue<T> setValue,
                VT<T>[] possibleValues,
                InlineParamVis inlineParam)
                : base(tag, description, spacerFollows, inlineParam)
            {
                this.getValue = getValue;
                this.setValue = setValue;
                this.possibleValues = possibleValues;
            }

            private int FindIndex(T value)
            {
                for (int i = 0; i < possibleValues.Length; i++)
                {
                    if (EqualityComparer<T>.Default.Equals(possibleValues[i].value, value))
                    {
                        return i;
                    }
                }
                Debug.Assert(false);
                throw new ArgumentException();
            }

            public override string GetValue(NoteNoteObjectRec note)
            {
                T value = getValue(note);
                int i = FindIndex(value);
                return possibleValues[i].text;
            }

            public override string GetDefaultValue()
            {
                return possibleValues[0].text;
            }

            public override void SetValue(NoteNoteObjectRec note, string value)
            {
                for (int i = 0; i < possibleValues.Length; i++)
                {
                    if (String.Equals(value, possibleValues[i].text))
                    {
                        setValue(note, possibleValues[i].value);
                        return;
                    }
                }
                Debug.Assert(false);
                throw new ArgumentException();
            }

            public override void DoClick(NoteNoteObjectRec note, int noteIndex, IValueInfoOwner owner)
            {
                owner.SaveUndoInfo("Change Note Property");

                T value = getValue(note);
                int i = FindIndex(value);
                i = (i + 1) % possibleValues.Length;
                setValue(note, possibleValues[i].value);

                if (owner.FocusReturnsTo != null)
                {
                    owner.FocusReturnsTo.Focus();
                }
            }
        }

        private class ValueInfoMultisampleFalsePitch : ValueInfo
        {
            public ValueInfoMultisampleFalsePitch(
                string tag,
                string description,
                bool spacerFollows,
                InlineParamVis inlineParam)
                : base(tag, description, spacerFollows, inlineParam)
            {
            }

            public override string GetValue(NoteNoteObjectRec Note)
            {
                if (-1 == Note.GetNoteMultisampleFalsePitch())
                {
                    return "dflt";
                }
                else
                {
                    return SymbolicPitch.NumericPitchToString(Note.GetNoteMultisampleFalsePitch(), 0);
                }
            }

            public override string GetDefaultValue()
            {
                return "dflt";
            }

            public override void SetValue(NoteNoteObjectRec Note, string value)
            {
                if (String.Equals(value, GetDefaultValue()))
                {
                    Note.PutNoteMultisampleFalsePitch(-1);
                }
                else
                {
                    short pitch = Note.GetNoteMultisampleFalsePitch();
                    NoteFlags sharpFlat = Note.SharpFlat;
                    SymbolicPitch.StringToNumericPitch(value, ref pitch, ref sharpFlat);
                    Note.PutNoteMultisampleFalsePitch(pitch);
                    Note.PutNoteFlatOrSharpStatus(sharpFlat);
                }
            }

            public override void DoClick(NoteNoteObjectRec note, int noteIndex, IValueInfoOwner owner)
            {
                owner.BeginFieldEdit(this, noteIndex, GetValue(note));
            }
        }


        public static ValueInfo[] Values { get { return values; } }

        private static readonly ValueInfo[] values = new ValueInfo[]
        {
            new ValueInfoDouble("Vol=", delegate (NoteNoteObjectRec Note) { return Note.GetNoteOverallLoudnessAdjustment(); }, 1,
                "Note Loudness", true, delegate(NoteNoteObjectRec Note, double v) { Note.PutNoteOverallLoudnessAdjustment(v); },
                InlineParamVis.Loudness),

            new ValueInfoDouble("Start=", delegate (NoteNoteObjectRec Note) { return Note.GetNoteEarlyLateAdjust(); }, 0,
                "Early/Late Adjust", true, delegate(NoteNoteObjectRec Note, double v) { Note.PutNoteEarlyLateAdjust(v); },
                InlineParamVis.EarlyLateAdjust),

            new ValueInfoDouble("DurAdj=", delegate (NoteNoteObjectRec Note) { return Note.GetNoteDurationAdjust(); }, 0,
                "Duration Adjust", false, delegate(NoteNoteObjectRec Note, double v) { Note.PutNoteDurationAdjust(v); },
                InlineParamVis.DurationAdjust),
            new ValueInfoEnum<NoteFlags>("/",  delegate (NoteNoteObjectRec Note) { return Note.GetNoteDurationAdjustMode(); },
                "Duration Adjust Mode", true, delegate (NoteNoteObjectRec Note, NoteFlags v) { Note.PutNoteDurationAdjustMode(v); },
                new VT<NoteFlags>[]
                {
                    new VT<NoteFlags>(NoteFlags.eDurationAdjustDefault, "dflt"),
                    new VT<NoteFlags>(NoteFlags.eDurationAdjustAdditive, "add"),
                    new VT<NoteFlags>(NoteFlags.eDurationAdjustMultiplicative, "mul"),
                },
                InlineParamVis.DurationAdjustMode),

            new ValueInfoDouble("Rls1=", delegate (NoteNoteObjectRec Note) { return Note.GetNoteReleasePoint1(); }, 0,
                "Release Point 1", false, delegate(NoteNoteObjectRec Note, double v) { Note.PutNoteReleasePoint1(v); },
                InlineParamVis.ReleasePoint1),
            new ValueInfoEnum<NoteFlags>("/",  delegate (NoteNoteObjectRec Note) { return Note.GetNoteRelease1Origin(); },
                "Release Point 1 Origin", true, delegate (NoteNoteObjectRec Note, NoteFlags v) { Note.PutNoteRelease1Origin(v); },
                new VT<NoteFlags>[]
                {
                    new VT<NoteFlags>(NoteFlags.eRelease1FromDefault, "dflt"),
                    new VT<NoteFlags>(NoteFlags.eRelease1FromStart, "start"),
                    new VT<NoteFlags>(NoteFlags.eRelease1FromEnd, "end"),
                },
                InlineParamVis.ReleasePoint1Origin),

            new ValueInfoDouble("Rls2=", delegate (NoteNoteObjectRec Note) { return Note.GetNoteReleasePoint2(); }, 0,
                "Release Point 2", false, delegate(NoteNoteObjectRec Note, double v) { Note.PutNoteReleasePoint2(v); },
                InlineParamVis.ReleasePoint2),
            new ValueInfoEnum<NoteFlags>("/",  delegate (NoteNoteObjectRec Note) { return Note.GetNoteRelease2Origin(); },
                "Release Point 2 Origin", true, delegate (NoteNoteObjectRec Note, NoteFlags v) { Note.PutNoteRelease2Origin(v); },
                new VT<NoteFlags>[]
                {
                    new VT<NoteFlags>(NoteFlags.eRelease2FromDefault, "dflt"),
                    new VT<NoteFlags>(NoteFlags.eRelease2FromStart, "start"),
                    new VT<NoteFlags>(NoteFlags.eRelease2FromEnd, "end"),
                },
                InlineParamVis.ReleasePoint2Origin),

            new ValueInfoEnum<bool>("Rls3=",  delegate (NoteNoteObjectRec Note) { return Note.GetNoteRelease3FromStartInsteadOfEnd(); },
                "Release Point 3 Origin", true, delegate (NoteNoteObjectRec Note, bool f) { Note.PutNoteRelease3FromStartInsteadOfEnd(f); },
                new VT<bool>[]
                {
                    new VT<bool>(false, "end"),
                    new VT<bool>(true, "start"),
                },
                InlineParamVis.ReleasePoint3Origin ),

            new ValueInfoDouble("Por=", delegate (NoteNoteObjectRec Note) { return Note.GetNotePortamentoDuration(); }, 0,
                "Portamento Duration", false, delegate(NoteNoteObjectRec Note, double v) { Note.PutNotePortamentoDuration(v); },
                InlineParamVis.PortamentoDuration),
            new ValueInfoEnum<bool>("/",  delegate (NoteNoteObjectRec Note) { return Note.GetNotePortamentoHertzNotHalfstepsFlag(); },
                "Portamento Units", false, delegate (NoteNoteObjectRec Note, bool f) { Note.PutNotePortamentoHertzNotHalfstepsFlag(f); },
                new VT<bool>[]
                {
                    new VT<bool>(false, "hstp"),
                    new VT<bool>(true, "hz"),
                },
                InlineParamVis.PortamentoUnits),
            new ValueInfoEnum<bool>("/",  delegate (NoteNoteObjectRec Note) { return Note.GetNotePortamentoLeadsBeatFlag(); },
                "Portamento Leads/Follows Note Start", true, delegate (NoteNoteObjectRec Note, bool f) { Note.PutNotePortamentoLeadsBeatFlag(f); },
                new VT<bool>[]
                {
                    new VT<bool>(false, "follow"),
                    new VT<bool>(true, "lead"),
                },
                InlineParamVis.PortamentoLeadsFollows),

            new ValueInfoDouble("Bal=", delegate (NoteNoteObjectRec Note) { return Note.GetNoteStereoPositioning(); }, 0,
                "Stereo Positioning", true, delegate(NoteNoteObjectRec Note, double v) { Note.PutNoteStereoPositioning(v); },
                InlineParamVis.StereoPosition),

            new ValueInfoDouble("PDD=", delegate (NoteNoteObjectRec Note) { return Note.GetNotePitchDisplacementDepthAdjust(); }, 1,
                "Pitch Displacement Depth Adjust", true, delegate(NoteNoteObjectRec Note, double v) { Note.PutNotePitchDisplacementDepthAdjust(v); },
                InlineParamVis.PitchDisplacementDepthAdjust),

            new ValueInfoDouble("PDR=", delegate (NoteNoteObjectRec Note) { return Note.GetNotePitchDisplacementRateAdjust(); }, 1,
                "Pitch Displacement Rate Adjust", true, delegate(NoteNoteObjectRec Note, double v) { Note.PutNotePitchDisplacementRateAdjust(v); },
                InlineParamVis.PitchDisplacementRateAdjust),

            new ValueInfoDouble("PDS=", delegate (NoteNoteObjectRec Note) { return Note.GetNotePitchDisplacementStartPoint(); }, 0,
                "Pitch Displacement Start Point", false, delegate(NoteNoteObjectRec Note, double v) { Note.PutNotePitchDisplacementStartPoint(v); },
                InlineParamVis.PitchDisplacementStartPoint),
            new ValueInfoEnum<NoteFlags>("/",  delegate (NoteNoteObjectRec Note) { return Note.GetNotePitchDisplacementStartOrigin(); },
                "Pitch Displacement Start Origin", true, delegate (NoteNoteObjectRec Note, NoteFlags v) { Note.PutNotePitchDisplacementStartOrigin(v); },
                new VT<NoteFlags>[]
                {
                    new VT<NoteFlags>(NoteFlags.ePitchDisplacementStartFromDefault, "dflt"),
                    new VT<NoteFlags>(NoteFlags.ePitchDisplacementStartFromStart, "start"),
                    new VT<NoteFlags>(NoteFlags.ePitchDisplacementStartFromEnd, "end"),
                },
                InlineParamVis.PitchDisplacementStartOrigin),

            new ValueInfoDouble("Hur=", delegate (NoteNoteObjectRec Note) { return Note.GetNoteHurryUpFactor(); }, 1,
                "Hurry-Up Factor", true, delegate(NoteNoteObjectRec Note, double v) { Note.PutNoteHurryUpFactor(v); },
                InlineParamVis.HurryUp),

            new ValueInfoDouble("Dtn=", delegate (NoteNoteObjectRec Note) { return Note.GetNoteDetuning(); }, 0,
                "Note Detuning", false, delegate(NoteNoteObjectRec Note, double v) { Note.PutNoteDetuning(v); },
                InlineParamVis.Detune),
            new ValueInfoEnum<NoteFlags>("/",  delegate (NoteNoteObjectRec Note) { return Note.GetNoteDetuneConversionMode(); },
                "Note Detuning Units", true, delegate (NoteNoteObjectRec Note, NoteFlags v) { Note.PutNoteDetuneConversionMode(v); },
                new VT<NoteFlags>[]
                {
                    new VT<NoteFlags>(NoteFlags.eDetuningModeDefault, "dflt"),
                    new VT<NoteFlags>(NoteFlags.eDetuningModeHalfSteps, "hstp"),
                    new VT<NoteFlags>(NoteFlags.eDetuningModeHertz, "hz"),
                },
                InlineParamVis.DetuneUnits),

            new ValueInfoDouble("Sur=", delegate (NoteNoteObjectRec Note) { return Note.GetNoteSurroundPositioning(); }, 0,
                "Surround Positioning", true, delegate(NoteNoteObjectRec Note, double v) { Note.PutNoteSurroundPositioning(v); },
                InlineParamVis.SurroundPosition),

            new ValueInfoMultisampleFalsePitch("MuS=", "Pitch Override For Multisample Selection", true,
                InlineParamVis.PitchOverrideForMultisampleSelection),

            new ValueInfoEnum<bool>("retrig=",  delegate (NoteNoteObjectRec Note) { return Note.GetNoteRetriggerEnvelopesOnTieStatus(); },
                "Retrigger Envelopes on Tie", true, delegate (NoteNoteObjectRec Note, bool f) { Note.PutNoteRetriggerEnvelopesOnTieStatus(f); },
                new VT<bool>[]
                {
                    new VT<bool>(false, "no"),
                    new VT<bool>(true, "yes"),
                },
                InlineParamVis.Retrigger),

            new ValueInfoEnum<bool>("",  delegate (NoteNoteObjectRec Note) { return Note.GetNoteIsItARest(); },
                "Note or Rest", true, delegate (NoteNoteObjectRec Note, bool f) { Note.PutNoteIsItARest(f); },
                new VT<bool>[]
                {
                    new VT<bool>(false, "note"),
                    new VT<bool>(true, "rest"),
                },
                InlineParamVis.NoteRest),

            null, // line break

            new ValueInfoDouble("Acc1=", delegate (NoteNoteObjectRec Note) { return Note.GetNoteAccent1(); }, 0,
                "Accent 1", true, delegate(NoteNoteObjectRec Note, double v) { Note.PutNoteAccent1(v); },
                InlineParamVis.Accent1),
            new ValueInfoDouble("Acc2=", delegate (NoteNoteObjectRec Note) { return Note.GetNoteAccent2(); }, 0,
                "Accent 2", true, delegate(NoteNoteObjectRec Note, double v) { Note.PutNoteAccent2(v); },
                InlineParamVis.Accent2),
            new ValueInfoDouble("Acc3=", delegate (NoteNoteObjectRec Note) { return Note.GetNoteAccent3(); }, 0,
                "Accent 3", true, delegate(NoteNoteObjectRec Note, double v) { Note.PutNoteAccent3(v); },
                InlineParamVis.Accent3),
            new ValueInfoDouble("Acc4=", delegate (NoteNoteObjectRec Note) { return Note.GetNoteAccent4(); }, 0,
                "Accent 4", true, delegate(NoteNoteObjectRec Note, double v) { Note.PutNoteAccent4(v); },
                InlineParamVis.Accent4),
            new ValueInfoDouble("Acc5=", delegate (NoteNoteObjectRec Note) { return Note.GetNoteAccent5(); }, 0,
                "Accent 5", true, delegate(NoteNoteObjectRec Note, double v) { Note.PutNoteAccent5(v); },
                InlineParamVis.Accent5),
            new ValueInfoDouble("Acc6=", delegate (NoteNoteObjectRec Note) { return Note.GetNoteAccent6(); }, 0,
                "Accent 6", true, delegate(NoteNoteObjectRec Note, double v) { Note.PutNoteAccent6(v); },
                InlineParamVis.Accent6),
            new ValueInfoDouble("Acc7=", delegate (NoteNoteObjectRec Note) { return Note.GetNoteAccent7(); }, 0,
                "Accent 7", true, delegate(NoteNoteObjectRec Note, double v) { Note.PutNoteAccent7(v); },
                InlineParamVis.Accent7),
            new ValueInfoDouble("Acc8=", delegate (NoteNoteObjectRec Note) { return Note.GetNoteAccent8(); }, 0,
                "Accent 8", true, delegate(NoteNoteObjectRec Note, double v) { Note.PutNoteAccent8(v); },
                InlineParamVis.Accent8),
        };
    }
}
