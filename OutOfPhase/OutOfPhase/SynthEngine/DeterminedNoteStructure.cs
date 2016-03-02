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
using System.Runtime.InteropServices;
using System.Text;

namespace OutOfPhase
{
    public static partial class Synthesizer
    {
        [StructLayout(LayoutKind.Auto)]
        public struct FrozenNoteRec
        {
            /* reference to the note that defines this note.  this is used for determining */
            /* the following things: */
            /*   - hertz or halfsteps for portamento */
            /*   - the target of a tie */
            public NoteNoteObjectRec OriginalNote;

            /* frequency determined by pitch index + detuning, in Hertz */
            public double NominalFrequency;

            /* frequency used for doing multisampling, in Hertz */
            public double MultisampleFrequency;

            /* acceleration of envelopes */
            public double HurryUpFactor;

            /* portamento duration, in envelope ticks */
            public int PortamentoDuration;
            public bool PortamentoBeforeNote;

            /* duration, in envelope ticks */
            public int Duration;

            /* first release point, in envelope ticks after start of note */
            public int ReleasePoint1;
            public bool Release1FromStart;
            /* second release point, in envelope ticks after start of note */
            public int ReleasePoint2;
            public bool Release2FromStart;
            /* third release point, in envelope ticks after start of note */
            public int ReleasePoint3;
            public bool Release3FromStart;

            /* overall loudness adjustment for envelopes, including global volume scaling */
            public double LoudnessAdjust;

            /* stereo positioning for note */
            public double StereoPosition;

            /* accent values for controlling envelopes */
            public AccentRec Accents;

            /* pitch displacement maximum depth */
            public double PitchDisplacementDepthLimit;

            /* pitch displacement maximum rate, in LFO Hertz */
            public double PitchDisplacementRateLimit;

            /* pitch displacement start point, in envelope clocks after start of note */
            public int PitchDisplacementStartPoint;
        }

        /* build a new note object with all parameters determined.  *StartAdjustOut */
        /* indicates how many ticks before (negative) or after (positive) now that */
        /* the key-down should occur.  this is added to the scanning gap size and envelope */
        /* origins to figure out how to schedule the note */
        public static void FixNoteParameters(
            IncrParamUpdateRec GlobalParamSource,
            NoteNoteObjectRec Note,
            out int StartAdjustOut,
            double EnvelopeTicksPerDurationTick,
            short PitchIndexAdjust,
            ref FrozenNoteRec FrozenNote,
            SynthParamRec SynthParams)
        {
            // must assign all fields

            /* reference to the note that defines this note. */
            FrozenNote.OriginalNote = Note;

            /* frequency determined by pitch index + detuning, in Hertz */
            double NominalFrequency;
            {
                int i = Note._Pitch
                    + GlobalParamSource.TransposeHalfsteps
                    + PitchIndexAdjust;
                if (i < 0)
                {
                    i = 0;
                }
                else if (i > Constants.NUMNOTES - 1)
                {
                    i = Constants.NUMNOTES - 1;
                }
                /* compute frequency from index */
#if DEBUG
                if ((Constants.CENTERNOTE % 12) != 0)
                {
                    // CENTERNOTE multiple of 12
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif
                double d = GlobalParamSource.FrequencyTable[i % 12].nd.Current;
                i = (i / 12) - (Constants.CENTERNOTE / 12);
                d = d * Math.Exp(i * Constants.LOG2) * Constants.MIDDLEC;
                /* apply detuning */
                double e;
                switch (Note.Flags & NoteFlags.eDetuningModeMask)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case NoteFlags.eDetuningModeDefault:
                        e = (double)Note._Detuning * GlobalParamSource.Detune.nd.Current;
                        if (GlobalParamSource.DetuneHertz)
                        {
                            goto DetuneHertzPoint;
                        }
                        else
                        {
                            goto DetuneHalfStepsPoint;
                        }
                    case NoteFlags.eDetuningModeHalfSteps:
                        e = (double)(Note._Detuning) + GlobalParamSource.Detune.nd.Current;
                    DetuneHalfStepsPoint:
                        NominalFrequency = d * Math.Exp((e / 12) * Constants.LOG2);
                        break;
                    case NoteFlags.eDetuningModeHertz:
                        e = (double)Note._Detuning + GlobalParamSource.Detune.nd.Current;
                    DetuneHertzPoint:
                        NominalFrequency = d + e;
                        break;
                }
            }
            FrozenNote.NominalFrequency = NominalFrequency;

            /* frequency used for doing multisampling, in Hertz */
            if (Note._MultisamplePitchAsIf != -1)
            {
                /* compute frequency from index */
                int i = Note._MultisamplePitchAsIf;
#if DEBUG
                if ((Constants.CENTERNOTE % 12) != 0)
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif
                double d = GlobalParamSource.FrequencyTable[i % 12].nd.Current;
                i = (i / 12) - (Constants.CENTERNOTE / 12);
                d = d * Math.Exp(i * Constants.LOG2) * Constants.MIDDLEC;
                FrozenNote.MultisampleFrequency = d;
            }
            else
            {
                FrozenNote.MultisampleFrequency = NominalFrequency;
            }

            /* acceleration of envelopes */
            FrozenNote.HurryUpFactor = (double)Note._HurryUpFactor * GlobalParamSource.HurryUp.nd.Current;

            /* duration, in envelope ticks */
            int Duration;
            {
                int i;
                switch (Note.Flags & NoteFlags.eDurationMask)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case NoteFlags.e64thNote:
                        i = DURATIONUPDATECLOCKRESOLUTION / 64;
                        break;
                    case NoteFlags.e32ndNote:
                        i = DURATIONUPDATECLOCKRESOLUTION / 32;
                        break;
                    case NoteFlags.e16thNote:
                        i = DURATIONUPDATECLOCKRESOLUTION / 16;
                        break;
                    case NoteFlags.e8thNote:
                        i = DURATIONUPDATECLOCKRESOLUTION / 8;
                        break;
                    case NoteFlags.e4thNote:
                        i = DURATIONUPDATECLOCKRESOLUTION / 4;
                        break;
                    case NoteFlags.e2ndNote:
                        i = DURATIONUPDATECLOCKRESOLUTION / 2;
                        break;
                    case NoteFlags.eWholeNote:
                        i = DURATIONUPDATECLOCKRESOLUTION;
                        break;
                    case NoteFlags.eDoubleNote:
                        i = DURATIONUPDATECLOCKRESOLUTION * 2;
                        break;
                    case NoteFlags.eQuadNote:
                        i = DURATIONUPDATECLOCKRESOLUTION * 4;
                        break;
                }
                switch (Note.Flags & NoteFlags.eDivisionMask)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case NoteFlags.eDiv1Modifier:
                        break;
                    case NoteFlags.eDiv3Modifier:
                        i = i / 3;
                        break;
                    case NoteFlags.eDiv5Modifier:
                        i = i / 5;
                        break;
                    case NoteFlags.eDiv7Modifier:
                        i = i / 7;
                        break;
                }
                if ((Note.Flags & NoteFlags.eDotModifier) != 0)
                {
                    i = (i * 3) / 2;
                }
                double d = i;
                switch (Note.Flags & NoteFlags.eDurationAdjustMask)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case NoteFlags.eDurationAdjustDefault:
                        if (GlobalParamSource.DurationAdjustAdditive)
                        {
                            goto DurationAdjustAddPoint;
                        }
                        else
                        {
                            goto DurationAdjustMultPoint;
                        }
                    case NoteFlags.eDurationAdjustAdditive:
                    DurationAdjustAddPoint:
                        d = d + (double)Note._DurationAdjust * (DURATIONUPDATECLOCKRESOLUTION / 4);
                        break;
                    case NoteFlags.eDurationAdjustMultiplicative:
                    DurationAdjustMultPoint:
                        d = d * (double)Note._DurationAdjust;
                        break;
                }
                if (GlobalParamSource.DurationAdjustAdditive)
                {
                    d = d + GlobalParamSource.DurationAdjust.nd.Current * (DURATIONUPDATECLOCKRESOLUTION / 4);
                }
                else
                {
                    d = d * GlobalParamSource.DurationAdjust.nd.Current;
                }
                /* this line is what converts from duration update ticks to envelope ticks */
                Duration = (int)(d * EnvelopeTicksPerDurationTick);
            }
            FrozenNote.Duration = Duration;

            /* portamento duration, in envelope ticks */
            FrozenNote.PortamentoDuration = (int)(((double)Note._PortamentoDuration + GlobalParamSource.Portamento.nd.Current)
                * (DURATIONUPDATECLOCKRESOLUTION / 4) * EnvelopeTicksPerDurationTick);

            /* see if portamento occurs before note retrigger */
            FrozenNote.PortamentoBeforeNote = ((Note.Flags & NoteFlags.ePortamentoLeadsNote) != 0);

            /* first release point, in envelope ticks after start of note */
            switch (Note.Flags & NoteFlags.eRelease1OriginMask)
            {
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();
                case NoteFlags.eRelease1FromStart:
                    FrozenNote.ReleasePoint1 = (int)((double)Note._ReleasePoint1 * Duration);
                    FrozenNote.Release1FromStart = true;
                    break;
                case NoteFlags.eRelease1FromEnd:
                    FrozenNote.ReleasePoint1 = (int)((1 - (double)Note._ReleasePoint1) * Duration);
                    FrozenNote.Release1FromStart = false;
                    break;
                case NoteFlags.eRelease1FromDefault:
                    if (GlobalParamSource.ReleasePoint1FromStart)
                    {
                        FrozenNote.ReleasePoint1 = (int)(((double)Note._ReleasePoint1
                            + GlobalParamSource.ReleasePoint1.nd.Current)
                            * Duration);
                        FrozenNote.Release1FromStart = true;
                    }
                    else
                    {
                        FrozenNote.ReleasePoint1 = (int)((1 - ((double)Note._ReleasePoint1
                            + GlobalParamSource.ReleasePoint1.nd.Current))
                            * Duration);
                        FrozenNote.Release1FromStart = false;
                    }
                    break;
            }

            /* second release point, in envelope ticks after start of note */
            switch (Note.Flags & NoteFlags.eRelease2OriginMask)
            {
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();
                case NoteFlags.eRelease2FromStart:
                    FrozenNote.ReleasePoint2 = (int)((double)Note._ReleasePoint2 * Duration);
                    FrozenNote.Release2FromStart = true;
                    break;
                case NoteFlags.eRelease2FromEnd:
                    FrozenNote.ReleasePoint2 = (int)((1 - (double)Note._ReleasePoint2) * Duration);
                    FrozenNote.Release2FromStart = false;
                    break;
                case NoteFlags.eRelease2FromDefault:
                    if (GlobalParamSource.ReleasePoint2FromStart)
                    {
                        FrozenNote.ReleasePoint2 = (int)(((double)Note._ReleasePoint2
                            + GlobalParamSource.ReleasePoint2.nd.Current)
                            * Duration);
                        FrozenNote.Release2FromStart = true;
                    }
                    else
                    {
                        FrozenNote.ReleasePoint2 = (int)((1 - ((double)Note._ReleasePoint2
                            + GlobalParamSource.ReleasePoint2.nd.Current))
                            * Duration);
                        FrozenNote.Release2FromStart = false;
                    }
                    break;
            }

            /* third release point, in envelope ticks after start of note */
            if ((Note.Flags & NoteFlags.eRelease3FromStartNotEnd) != 0)
            {
                FrozenNote.ReleasePoint3 = 0;
                FrozenNote.Release3FromStart = true;
            }
            else
            {
                FrozenNote.ReleasePoint3 = Duration;
                FrozenNote.Release3FromStart = false;
            }

            /* overall loudness adjustment for envelopes, including global volume scaling */
            FrozenNote.LoudnessAdjust = (double)Note._OverallLoudnessAdjustment * GlobalParamSource.Volume.nd.Current;

            /* stereo positioning for note */
            {
                double d = (double)Note._StereoPositionAdjustment + GlobalParamSource.StereoPosition.nd.Current;
                if (d < -1)
                {
                    d = -1;
                }
                else if (d > 1)
                {
                    d = 1;
                }
                FrozenNote.StereoPosition = d;
            }

            /* accent values for controlling envelopes */
            InitializeAccent(
                ref FrozenNote.Accents,
                (double)Note._Accent1 + GlobalParamSource.Accent1.nd.Current,
                (double)Note._Accent2 + GlobalParamSource.Accent2.nd.Current,
                (double)Note._Accent3 + GlobalParamSource.Accent3.nd.Current,
                (double)Note._Accent4 + GlobalParamSource.Accent4.nd.Current,
                (double)Note._Accent5 + GlobalParamSource.Accent5.nd.Current,
                (double)Note._Accent6 + GlobalParamSource.Accent6.nd.Current,
                (double)Note._Accent7 + GlobalParamSource.Accent7.nd.Current,
                (double)Note._Accent8 + GlobalParamSource.Accent8.nd.Current);

            /* pitch displacement maximum depth, in tonal Hertz */
            FrozenNote.PitchDisplacementDepthLimit = (double)Note._PitchDisplacementDepthAdjustment
                * GlobalParamSource.PitchDisplacementDepthLimit.nd.Current;

            /* pitch displacement maximum rate, in LFO Hertz */
            FrozenNote.PitchDisplacementRateLimit = (double)Note._PitchDisplacementRateAdjustment
                * GlobalParamSource.PitchDisplacementRateLimit.nd.Current;

            /* pitch displacement start point, in envelope clocks after start of note */
            switch (Note.Flags & NoteFlags.ePitchDisplacementStartOriginMask)
            {
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();
                case NoteFlags.ePitchDisplacementStartFromStart:
                    FrozenNote.PitchDisplacementStartPoint = (int)(Duration
                        * (double)Note._PitchDisplacementStartPoint);
                    break;
                case NoteFlags.ePitchDisplacementStartFromEnd:
                    FrozenNote.PitchDisplacementStartPoint = (int)(Duration
                        * (1 - (double)Note._PitchDisplacementStartPoint));
                    break;
                case NoteFlags.ePitchDisplacementStartFromDefault:
                    if (GlobalParamSource.PitchDisplacementStartPointFromStart)
                    {
                        FrozenNote.PitchDisplacementStartPoint = (int)(Duration
                            * ((double)Note._PitchDisplacementStartPoint
                            + GlobalParamSource.PitchDisplacementStartPoint.nd.Current));
                    }
                    else
                    {
                        FrozenNote.PitchDisplacementStartPoint = (int)(Duration
                            * (1 - ((double)Note._PitchDisplacementStartPoint
                            + GlobalParamSource.PitchDisplacementStartPoint.nd.Current)));
                    }
                    break;
            }

            StartAdjustOut = (int)(((double)Note._EarlyLateAdjust
                + GlobalParamSource.EarlyLateAdjust.nd.Current) * Duration);
        }
    }
}
