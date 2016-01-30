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

namespace OutOfPhase
{
    public static partial class Synthesizer
    {
        public struct IncrParamOneNDRec
        {
            public double Current;
            public LinearTransRec Change;
            public int ChangeCountdown;
        }

        public struct IncrParamOneRec
        {
            public IncrParamOneNDRec nd;
            public double Default;
        }

        // TODO: if perf becomes an issue here, convert these to indexable entities and keep a worklist of objects that
        // are undergoing transitions, to avoid updating all of them every time (most of them are inactivate most of the
        // time). See ExecuteParamUpdate().

        public class IncrParamUpdateRec
        {
            public IncrParamOneRec StereoPosition;
            public IncrParamOneRec Volume;
            public IncrParamOneRec ReleasePoint1;
            public IncrParamOneRec ReleasePoint2;
            public IncrParamOneRec Accent1;
            public IncrParamOneRec Accent2;
            public IncrParamOneRec Accent3;
            public IncrParamOneRec Accent4;
            public IncrParamOneRec Accent5;
            public IncrParamOneRec Accent6;
            public IncrParamOneRec Accent7;
            public IncrParamOneRec Accent8;
            public IncrParamOneRec PitchDisplacementDepthLimit;
            public IncrParamOneRec PitchDisplacementRateLimit;
            public IncrParamOneRec PitchDisplacementStartPoint;
            public IncrParamOneRec HurryUp;
            public IncrParamOneRec Detune;
            public IncrParamOneRec EarlyLateAdjust;
            public IncrParamOneRec DurationAdjust;
            public IncrParamOneRec Portamento;

            /* table of normalized frequencies, [0] = 1 */
            public IncrParamOneRec[] FrequencyTable; // Length == 12
            public double[] FrequencyTableLastLoaded; // Length == 12

            public TempoControlRec TempoControl;

            public int TransposeHalfsteps;

            public bool ReleasePoint1FromStart; /* True = start, False = end */
            public bool ReleasePoint2FromStart; /* True = start, False = end */
            public bool PitchDisplacementStartPointFromStart; /* True = start, False = end */
            public bool DetuneHertz; /* True = hertz, False = Halfsteps */
            public bool DurationAdjustAdditive; /* True = additive, False = multiplicative */
        }

        private delegate double GetDefaultValueMethod(TrackObjectRec TrackObj);

        /* init tracker helper */
        private static void InitTrackerHelper(
            TrackObjectRec Template,
            GetDefaultValueMethod GetDefaultValue,
            ref IncrParamOneRec Tracker)
        {
            Tracker.Default = 0;
            if (GetDefaultValue != null)
            {
                Tracker.Default = GetDefaultValue(Template);
            }

            Tracker.nd.Current = Tracker.Default;

            ResetLinearTransition(
                ref Tracker.nd.Change,
                0,
                0,
                1);

            Tracker.nd.ChangeCountdown = 0;
        }

        /* build a new incremental parameter updator */
        public static IncrParamUpdateRec NewInitializedParamUpdator(
            TrackObjectRec Template,
            TempoControlRec TempoControl,
            SynthParamRec SynthParams)
        {
            IncrParamUpdateRec Updator = new IncrParamUpdateRec();

            Updator.FrequencyTable = new IncrParamOneRec[12];
            Updator.FrequencyTableLastLoaded = new double[12];
            for (int i_enum = 0; i_enum < 12; i_enum++)
            {
                int i = i_enum;
                Updator.FrequencyTableLastLoaded[i] = Math.Pow(2, i / 12d);
                InitTrackerHelper(
                    Template,
                    delegate (TrackObjectRec track) { return Updator.FrequencyTableLastLoaded[i]; },
                    ref Updator.FrequencyTable[i]);
            }

            InitTrackerHelper(
                Template,
                delegate (TrackObjectRec track) { return track.DefaultStereoPositioning; },
                ref Updator.StereoPosition);

            InitTrackerHelper(
                Template,
                delegate (TrackObjectRec track) { return track.DefaultOverallLoudness; },
                ref Updator.Volume);

            InitTrackerHelper(
                Template,
                delegate (TrackObjectRec track) { return track.DefaultReleasePoint1; },
                ref Updator.ReleasePoint1);
#if DEBUG
            if ((Template.DefaultReleasePoint1ModeFlag != NoteFlags.eRelease1FromStart)
                && (Template.DefaultReleasePoint1ModeFlag != NoteFlags.eRelease1FromEnd))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Updator.ReleasePoint1FromStart = Template.DefaultReleasePoint1ModeFlag == NoteFlags.eRelease1FromStart;

            InitTrackerHelper(
                Template,
                delegate (TrackObjectRec track) { return track.DefaultReleasePoint2; },
                ref Updator.ReleasePoint2);
#if DEBUG
            if ((Template.DefaultReleasePoint2ModeFlag != NoteFlags.eRelease2FromStart)
                && (Template.DefaultReleasePoint2ModeFlag != NoteFlags.eRelease2FromEnd))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Updator.ReleasePoint2FromStart = Template.DefaultReleasePoint2ModeFlag == NoteFlags.eRelease2FromStart;

            InitTrackerHelper(
                Template,
                delegate (TrackObjectRec track) { return track.DefaultAccent1; },
                ref Updator.Accent1);
            InitTrackerHelper(
                Template,
                delegate (TrackObjectRec track) { return track.DefaultAccent2; },
                ref Updator.Accent2);
            InitTrackerHelper(
                Template,
                delegate (TrackObjectRec track) { return track.DefaultAccent3; },
                ref Updator.Accent3);
            InitTrackerHelper(
                Template,
                delegate (TrackObjectRec track) { return track.DefaultAccent4; },
                ref Updator.Accent4);
            InitTrackerHelper(
                Template,
                delegate (TrackObjectRec track) { return track.DefaultAccent5; },
                ref Updator.Accent5);
            InitTrackerHelper(
                Template,
                delegate (TrackObjectRec track) { return track.DefaultAccent6; },
                ref Updator.Accent6);
            InitTrackerHelper(
                Template,
                delegate (TrackObjectRec track) { return track.DefaultAccent7; },
                ref Updator.Accent7);
            InitTrackerHelper(
                Template,
                delegate (TrackObjectRec track) { return track.DefaultAccent8; },
                ref Updator.Accent8);

            InitTrackerHelper(
                Template,
                delegate (TrackObjectRec track) { return track.DefaultPitchDisplacementDepthAdjust; },
                ref Updator.PitchDisplacementDepthLimit);

            InitTrackerHelper(
                Template,
                delegate (TrackObjectRec track) { return track.DefaultPitchDisplacementRateAdjust; },
                ref Updator.PitchDisplacementRateLimit);

            InitTrackerHelper(
                Template,
                delegate (TrackObjectRec track) { return track.DefaultPitchDisplacementStartPoint; },
                ref Updator.PitchDisplacementStartPoint);
#if DEBUG
            if ((Template.DefaultPitchDisplacementStartPointModeFlag != NoteFlags.ePitchDisplacementStartFromStart)
                && (Template.DefaultPitchDisplacementStartPointModeFlag != NoteFlags.ePitchDisplacementStartFromEnd))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Updator.PitchDisplacementStartPointFromStart = Template.DefaultPitchDisplacementStartPointModeFlag == NoteFlags.ePitchDisplacementStartFromStart;

            InitTrackerHelper(
                Template,
                delegate (TrackObjectRec track) { return track.DefaultHurryUpFactor; },
                ref Updator.HurryUp);

            InitTrackerHelper(
                Template,
                delegate (TrackObjectRec track) { return track.DefaultDetune; },
                ref Updator.Detune);
#if DEBUG
            if ((Template.DefaultDetuneModeFlag != NoteFlags.eDetuningModeHalfSteps)
                && (Template.DefaultDetuneModeFlag != NoteFlags.eDetuningModeHertz))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Updator.DetuneHertz = Template.DefaultDetuneModeFlag == NoteFlags.eDetuningModeHertz;

            InitTrackerHelper(
                Template,
                delegate (TrackObjectRec track) { return track.DefaultEarlyLateAdjust; },
                ref Updator.EarlyLateAdjust);

            InitTrackerHelper(
                Template,
                delegate (TrackObjectRec track) { return track.DefaultDuration; },
                ref Updator.DurationAdjust);
#if DEBUG
            if ((Template.DefaultDurationModeFlag != NoteFlags.eDurationAdjustAdditive)
                && (Template.DefaultDurationModeFlag != NoteFlags.eDurationAdjustMultiplicative))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Updator.DurationAdjustAdditive = Template.DefaultDurationModeFlag == NoteFlags.eDurationAdjustAdditive;

            InitTrackerHelper(
                Template,
                delegate (TrackObjectRec track) { return track.DefaultDuration; },
                ref Updator.Portamento);

            Updator.TempoControl = TempoControl;
            Updator.TransposeHalfsteps = 0;

            return Updator;
        }

        /* update a transition changer */
        public static void UpdateOne(
            ref IncrParamOneNDRec nd,
            int NumTicks)
        {
            if (nd.ChangeCountdown > 0)
            {
                int Ticks;

                /* advance for smaller number of ticks */
                if (NumTicks < nd.ChangeCountdown)
                {
                    Ticks = NumTicks;
                }
                else
                {
                    Ticks = nd.ChangeCountdown;
                }

                nd.Current = LinearTransitionUpdateMultiple(ref nd.Change, Ticks);
                nd.ChangeCountdown -= Ticks;
            }
        }

        /* execute a series of update cycles.  the value passed in is the number of */
        /* duration ticks.  there are DURATIONUPDATECLOCKRESOLUTION (64*2*3*5*7) ticks */
        /* in a whole note */
        public static void ExecuteParamUpdate(
            IncrParamUpdateRec Updator,
            int NumTicks)
        {
            for (int i = 0; i < Updator.FrequencyTable.Length; i++)
            {
                UpdateOne(
                    ref Updator.FrequencyTable[i].nd,
                    NumTicks);
            }
            UpdateOne(
                ref Updator.StereoPosition.nd,
                NumTicks);
            UpdateOne(
                ref Updator.Volume.nd,
                NumTicks);
            UpdateOne(
                ref Updator.ReleasePoint1.nd,
                NumTicks);
            UpdateOne(
                ref Updator.ReleasePoint2.nd,
                NumTicks);
            UpdateOne(
                ref Updator.Accent1.nd,
                NumTicks);
            UpdateOne(
                ref Updator.Accent2.nd,
                NumTicks);
            UpdateOne(
                ref Updator.Accent3.nd,
                NumTicks);
            UpdateOne(
                ref Updator.Accent4.nd,
                NumTicks);
            UpdateOne(
                ref Updator.Accent5.nd,
                NumTicks);
            UpdateOne(
                ref Updator.Accent6.nd,
                NumTicks);
            UpdateOne(
                ref Updator.Accent7.nd,
                NumTicks);
            UpdateOne(
                ref Updator.Accent8.nd,
                NumTicks);
            UpdateOne(
                ref Updator.PitchDisplacementDepthLimit.nd,
                NumTicks);
            UpdateOne(
                ref Updator.PitchDisplacementRateLimit.nd,
                NumTicks);
            UpdateOne(
                ref Updator.PitchDisplacementStartPoint.nd,
                NumTicks);
            UpdateOne(
                ref Updator.HurryUp.nd,
                NumTicks);
            UpdateOne(
                ref Updator.Detune.nd,
                NumTicks);
            UpdateOne(
                ref Updator.EarlyLateAdjust.nd,
                NumTicks);
            UpdateOne(
                ref Updator.DurationAdjust.nd,
                NumTicks);
            UpdateOne(
                ref Updator.Portamento.nd,
                NumTicks);
        }

        /* sweep the value to a new value */
        public static void SweepToNewValue(
            ref IncrParamOneNDRec nd,
            LargeBCDType TargetValue,
            SmallExtBCDType NumBeatsToReach)
        {
            nd.ChangeCountdown = (int)((double)NumBeatsToReach * (DURATIONUPDATECLOCKRESOLUTION / 4));
            if (nd.ChangeCountdown != 0)
            {
                ResetLinearTransition(
                    ref nd.Change,
                    nd.Current,
                    (double)TargetValue,
                    nd.ChangeCountdown);
            }
            else
            {
                nd.Current = (double)TargetValue;
            }
        }

        /* sweep to a new value relative to the current value */
        public static void SweepToAdjustedValue(
            ref IncrParamOneNDRec nd,
            LargeBCDType TargetValue,
            SmallExtBCDType NumBeatsToReach)
        {
            nd.ChangeCountdown = (int)((double)NumBeatsToReach * (DURATIONUPDATECLOCKRESOLUTION / 4));
            if (nd.ChangeCountdown != 0)
            {
                ResetLinearTransition(
                    ref nd.Change,
                    nd.Current,
                    (double)TargetValue + nd.Current,
                    nd.ChangeCountdown);
            }
            else
            {
                nd.Current = (double)TargetValue;
            }
        }

        /* sweep to a new value relative to the current value */
        public static void SweepToAdjustedValueMultiplicatively(
            ref IncrParamOneNDRec nd,
            LargeBCDType TargetValue,
            SmallExtBCDType NumBeatsToReach)
        {
            nd.ChangeCountdown = (int)((double)NumBeatsToReach * (DURATIONUPDATECLOCKRESOLUTION / 4));
            if (nd.ChangeCountdown != 0)
            {
                ResetLinearTransition(
                    ref nd.Change,
                    nd.Current,
                    (double)TargetValue * nd.Current,
                    nd.ChangeCountdown);
            }
            else
            {
                nd.Current = (double)TargetValue * nd.Current;
            }
        }

        // TODO: figure out how to support enharmonics in non-et tunings

        private enum PitchMode
        {
            CentsRelative,
            CentsAbsolute,
        }

        private class PitchEntry
        {
            public readonly string Name;
            public readonly PitchMode Mode;
            public readonly double[] Table;

            public PitchEntry(
                string Name,
                PitchMode Mode,
                double[] Table)
            {
                Debug.Assert(Table.Length == 12);
                this.Name = Name;
                this.Mode = Mode;
                this.Table = Table;
                // Normalize all tables for constant C for consistency
                switch (Mode)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case PitchMode.CentsAbsolute:
                    case PitchMode.CentsRelative:
                        {
                            // Note, if choosing other than C for normalization point, CentsAbsolute case would need a different
                            // loop, with correction calculated as (e.g. for A): double correction = 900 - Table[9];
                            double correction = -Table[0];
                            for (int i = 0; i < 12; i++)
                            {
                                Table[i] = Table[i] + correction;
                            }
                        }
                        break;
                }
            }

            public PitchEntry(
                string Name)
            {
                this.Name = Name;
            }
        }

        private static readonly PitchEntry[] PitchTables = new PitchEntry[]
        {
            //      C        C#/Db      D        D#/Eb      E         F        F#/Gb      G        G#/Ab      A        A#/Bb      B

            new PitchEntry("1/3 syntonic comma meantone", PitchMode.CentsAbsolute, new double[] // http://www-personal.umich.edu/~bpl/temper.html
                {   0.00,    63.50,   189.57,   315.64,   379.14,   505.21,   568.72,   694.79,   758.29,   884.36,  1010.43,  1073.93  }),
            new PitchEntry("1/4 syntonic comma meantone", PitchMode.CentsAbsolute, new double[] // http://www-personal.umich.edu/~bpl/temper.html
                {   0.00,    76.05,   193.16,   310.26,   386.31,   503.42,   579.47,   696.58,   772.63,   889.74,  1006.84,  1082.89  }),
            new PitchEntry("1/5 syntonic comma meantone", PitchMode.CentsAbsolute, new double[] // http://www-personal.umich.edu/~bpl/temper.html
                {   0.00,    83.58,   195.31,   307.04,   390.61,   502.35,   585.92,   697.65,   781.23,   892.96,  1004.69,  1088.27  }),
            new PitchEntry("1/6 Pythagorean comma", PitchMode.CentsAbsolute, new double[] // http://www-personal.umich.edu/~bpl/temper.html
                {   0.00,    86.31,   196.09,   305.87,   392.18,   501.96,   588.27,   698.04,   784.36,   894.13,  1003.91,  1090.22  }),
            new PitchEntry("1/6 syntonic comma meantone", PitchMode.CentsAbsolute, new double[] // http://www-personal.umich.edu/~bpl/temper.html
                {   0.00,    88.59,   196.74,   304.89,   393.48,   501.63,   590.22,   698.37,   786.96,   895.11,  1003.26,  1091.85  }),
            new PitchEntry("Bach 1722", PitchMode.CentsAbsolute, new double[] // http://www-personal.umich.edu/~bpl/temper.html
                {   0.00,    98.04,   196.09,   298.04,   392.18,   501.96,   596.09,   698.04,   798.04,   894.13,   998.04,  1094.13  }),
            new PitchEntry("equal temperament", PitchMode.CentsRelative, new double[]
                {   0,        0,        0,        0,        0,        0,        0,        0,        0,        0,        0,        0     }),
            new PitchEntry("just", PitchMode.CentsAbsolute, new double[] // http://www-personal.umich.edu/~bpl/temper.html
                {   0.00,   111.73,   203.91,   315.64,   386.31,   498.04,   590.22,   701.96,   813.69,   884.36,  1017.60,  1088.27  }),
            new PitchEntry("Kirnberger 2", PitchMode.CentsAbsolute, new double[] // http://www-personal.umich.edu/~bpl/temper.html
                {   0.00,    92.18,   203.91,   294.13,   386.31,   498.04,   590.22,   701.96,   794.13,   895.11,   996.09,  1088.27  }),
            new PitchEntry("Kirnberger 3", PitchMode.CentsAbsolute, new double[] // http://www-personal.umich.edu/~bpl/temper.html
                {   0.00,    90.22,   193.16,   294.13,   386.31,   498.04,   588.27,   696.58,   792.18,   889.74,   996.09,  1088.27  }),
            new PitchEntry("Pythagorean", PitchMode.CentsAbsolute, new double[] // http://www-personal.umich.edu/~bpl/temper.html
                {   0.00,    90.22,   203.91,   294.13,   407.82,   498.04,   588.27,   701.96,   792.18,   905.87,   996.09,  1109.78  }),
            new PitchEntry("temperament ordinaire", PitchMode.CentsAbsolute, new double[] // http://www-personal.umich.edu/~bpl/temper.html
                {   0.00,    86.80,   193.16,   296.09,   386.31,   503.42,   584.85,   696.58,   788.76,   889.74,  1005.21,  1082.89  }),
            new PitchEntry("Vallotti", PitchMode.CentsAbsolute, new double[] // http://www-personal.umich.edu/~bpl/temper.html - similar to "Young 2"
                {   0.00,    94.13,   196.09,   298.04,   392.18,   501.96,   592.18,   698.04,   796.09,   894.13,  1000.00,  1090.22  }),
            new PitchEntry("Werckmeister III", PitchMode.CentsAbsolute, new double[] // http://www-personal.umich.edu/~bpl/temper.html
                {   0.00,    90.22,   192.18,   294.13,   390.22,   498.04,   588.27,   696.09,   792.18,   888.27,   996.09,  1092.18  }),
            new PitchEntry("Werckmeister IV", PitchMode.CentsAbsolute, new double[] // https://en.wikipedia.org/wiki/Werckmeister_temperament
                {   0,       82,      196,      294,      392,      498,      588,      694,      784,      890,     1004,     1086     }),
            new PitchEntry("Werckmeister IV monochord", PitchMode.CentsAbsolute, new double[] // https://en.wikipedia.org/wiki/Werckmeister_temperament
                {   0,       85.8,    195.3,    295.0,    393.5,    498.0,    590.2,    693.3,    787.7,    891.6,   1003.8,   1088.3   }),
            new PitchEntry("Werckmeister V", PitchMode.CentsAbsolute, new double[] // https://en.wikipedia.org/wiki/Werckmeister_temperament
                {   0,       96,      204,      300,      396,      504,      600,      702,      792,      900,     1002,     1098     }),
            new PitchEntry("Werckmeister VI", PitchMode.CentsAbsolute, new double[] // https://en.wikipedia.org/wiki/Werckmeister_temperament
                {   0,       91,      196,      298,      395,      498,      595,      698,      793,      893,     1000,     1097     }),
            //                        ^^^ corrected (monochord length 175 instead of 176 - use 186 cents for latter)
            new PitchEntry("Young 1", PitchMode.CentsRelative, new double[] // https://en.wikipedia.org/wiki/Young_temperament
                {   6.2,      0.1,      2.1,      4.0,     -2.1,      6.1,     -1.8,      4.2,      2.1,      0,        6.0,     -2.0   }),
            new PitchEntry("Young 2", PitchMode.CentsAbsolute, new double[] // http://www-personal.umich.edu/~bpl/temper.html
                {   0.00,    90.22,   196.09,   294.13,   392.18,   498.04,   588.27,   698.04,   792.18,   894.13,   996.09,  1090.22  }),
        };

        private class PitchTableComparer : IComparer<PitchEntry>
        {
            public int Compare(PitchEntry x, PitchEntry y)
            {
                return String.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static SynthErrorCodes LoadPitchTable(
            string tableName,
            int tonicOffset,
            bool relative,
            IncrParamOneRec[] freqTable,
            double[] freqTableLastLoaded,
            SynthParamRec SynthParams)
        {
#if DEBUG
            PitchTableComparer debug_comparer = new PitchTableComparer();
            for (int i = 1; i < PitchTables.Length; i++)
            {
                Debug.Assert(debug_comparer.Compare(PitchTables[i - 1], PitchTables[i]) < 0, "static sorted table invariant");
            }
#endif

            PitchMode mode;
            double[] table;
            if (!(tableName.StartsWith("!") || tableName.StartsWith("+!")))
            {
                // built-in

                int modelIndex = Array.BinarySearch(
                    PitchTables,
                    new PitchEntry(tableName),
                    new PitchTableComparer());
                if (modelIndex < 0)
                {
                    SynthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUndefinedPitchTable;
                    SynthParams.ErrorInfo.GenericName = tableName;
                    return SynthErrorCodes.eSynthErrorEx;
                }
                mode = PitchTables[modelIndex].Mode;
                table = PitchTables[modelIndex].Table;
            }
            else
            {
                // user-provided

                mode = PitchMode.CentsAbsolute;
                if (tableName.StartsWith("+"))
                {
                    mode = PitchMode.CentsRelative;
                    tableName = tableName.Substring(1);
                }
                Debug.Assert(tableName.StartsWith("!"));
                tableName = tableName.Substring(1);

                FuncCodeRec userFunction = SynthParams.CodeCenter.ObtainFunctionHandle(tableName);
                if (userFunction == null)
                {
                    SynthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUserParamFunctionEvalError;
                    SynthParams.ErrorInfo.GenericName = tableName;
                    SynthParams.ErrorInfo.UserEvalErrorCode = EvalErrors.eEvalUndefinedFunction;
                    SynthParams.ErrorInfo.UserEvalErrorInfo = new EvalErrInfoRec();
                    return SynthErrorCodes.eSynthErrorEx;
                }
                if ((userFunction.GetFunctionReturnType() != DataTypes.eArrayOfDouble)
                    || (userFunction.GetFunctionParameterTypeList().Length != 0))
                {
                    SynthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUserParamFunctionEvalError;
                    SynthParams.ErrorInfo.GenericName = tableName;
                    SynthParams.ErrorInfo.UserEvalErrorCode = EvalErrors.eEvalFunctionSignatureMismatch;
                    SynthParams.ErrorInfo.UserEvalErrorInfo = new EvalErrInfoRec();
                    return SynthErrorCodes.eSynthErrorEx;
                }

                SynthParams.FormulaEvalContext.EmptyParamStackEnsureCapacity(
                    1/*retval*/);

                StackElement[] Stack;
                int StackNumElements;
                SynthParams.FormulaEvalContext.GetRawStack(out Stack, out StackNumElements);

                StackNumElements++; /* return address placeholder */

                SynthParams.FormulaEvalContext.UpdateRawStack(Stack, StackNumElements);

                EvalErrInfoRec ErrorInfo;
                EvalErrors Error = PcodeSystem.EvaluatePcode(
                    SynthParams.FormulaEvalContext,
                    userFunction.GetFunctionPcode(),
                    SynthParams.CodeCenter,
                    out ErrorInfo,
                    null/*EvaluateContext*/,
                    ref SynthParams.pcodeThreadContext);
                if (Error != EvalErrors.eEvalNoError)
                {
                    SynthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUserParamFunctionEvalError;
                    SynthParams.ErrorInfo.UserEvalErrorCode = Error;
                    SynthParams.ErrorInfo.UserEvalErrorInfo = ErrorInfo;
                    return SynthErrorCodes.eSynthErrorEx;
                }
                Debug.Assert(SynthParams.FormulaEvalContext.GetStackNumElements() == 1); // return value

                table = (double[])SynthParams.FormulaEvalContext.GetStackArray(0);
                if (table == null)
                {
                    SynthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUserParamFunctionEvalError;
                    SynthParams.ErrorInfo.UserEvalErrorCode = EvalErrors.eEvalArraySubscriptOutOfRange;
                    SynthParams.ErrorInfo.UserEvalErrorInfo = new EvalErrInfoRec();
                    return SynthErrorCodes.eSynthErrorEx;
                }
                if (table.Length != 12)
                {
                    SynthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUserParamFunctionEvalError;
                    SynthParams.ErrorInfo.UserEvalErrorCode = EvalErrors.eEvalArraySubscriptOutOfRange;
                    SynthParams.ErrorInfo.UserEvalErrorInfo = new EvalErrInfoRec();
                    return SynthErrorCodes.eSynthErrorEx;
                }
            }

            double factor = 1;
            if (relative)
            {
                // preserve ratio of current tonic to that tonic's concert pitch equal temperament value
                factor = freqTable[tonicOffset].nd.Current / Math.Pow(2, tonicOffset / 12d);
            }

            for (int n = 0; n < 12; n++)
            {
                int i = (n + tonicOffset) % 12;
                switch (mode)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case PitchMode.CentsRelative:
                        freqTable[i].nd.Current = factor * Math.Pow(2, (100 * i + table[n]) / 1200d);
                        break;
                    case PitchMode.CentsAbsolute:
                        freqTable[i].nd.Current = factor * Math.Pow(2, (table[n] + 100 * (i - n)) / 1200d);
                        break;
                }
                freqTableLastLoaded[i] = freqTable[i].nd.Current;
                freqTable[i].nd.ChangeCountdown = 0;
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* evaluate a command frame & set any parameters accordingly */
        public static SynthErrorCodes ExecuteParamCommandFrame(
            IncrParamUpdateRec Updator,
            CommandNoteObjectRec Note,
            SynthParamRec SynthParams)
        {
            switch ((NoteCommands)(Note.Flags & ~NoteFlags.eCommandFlag))
            {
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();

                case NoteCommands.eCmdRestoreTempo: /* restore the tempo to the default for the score */
                    TempoControlRestoreDefault(
                        Updator.TempoControl);
                    break;
                case NoteCommands.eCmdSetTempo: /* set tempo to <1xs> number of beats per second */
                    TempoControlSetBeatsPerMinute(
                        Updator.TempoControl,
                        (LargeBCDType)SmallExtBCDType.FromRawInt32(Note._Argument1));
                    break;
                case NoteCommands.eCmdIncTempo: /* add <1xs> to the tempo control */
                    TempoControlAdjustBeatsPerMinute(
                        Updator.TempoControl,
                        (LargeBCDType)SmallExtBCDType.FromRawInt32(Note._Argument1));
                    break;
                case NoteCommands.eCmdSweepTempoAbs: /* <1xs> = target tempo, <2xs> = # of beats to reach it */
                    TempoControlSweepToNewValue(
                        Updator.TempoControl,
                        (LargeBCDType)SmallExtBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;
                case NoteCommands.eCmdSweepTempoRel: /* <1xs> = target adjust (add to tempo), <2xs> = # beats */
                    TempoControlSweepToAdjustedValue(
                        Updator.TempoControl,
                        (LargeBCDType)SmallExtBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;

                case NoteCommands.eCmdRestoreStereoPosition: /* restore stereo position to channel's default */
                    Updator.StereoPosition.nd.Current = Updator.StereoPosition.Default;
                    Updator.StereoPosition.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSetStereoPosition: /* set position in channel <1l>: -1 = left, 1 = right */
                    Updator.StereoPosition.nd.Current = (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.StereoPosition.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdIncStereoPosition: /* adjust stereo position by adding <1l> */
                    Updator.StereoPosition.nd.Current += (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.StereoPosition.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSweepStereoAbs: /* <1l> = new pos, <2xs> = # of beats to get there */
                    SweepToNewValue(
                        ref Updator.StereoPosition.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;
                case NoteCommands.eCmdSweepStereoRel: /* <1l> = pos adjust, <2xs> = # beats to get there */
                    SweepToAdjustedValue(
                        ref Updator.StereoPosition.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;

                case NoteCommands.eCmdRestoreSurroundPosition: /* restore surround position to channel's default */
                    break;
                case NoteCommands.eCmdSetSurroundPosition: /* set position in channel <1l>: 1 = front, -1 = rear */
                    break;
                case NoteCommands.eCmdIncSurroundPosition: /* adjust surround position by adding <1l> */
                    break;
                case NoteCommands.eCmdSweepSurroundAbs: /* <1l> = new pos, <2xs> = # of beats to get there */
                    break;
                case NoteCommands.eCmdSweepSurroundRel: /* <1l> = pos adjust, <2xs> = # beats to get there */
                    break;

                case NoteCommands.eCmdRestoreVolume: /* restore the volume to the default for the channel */
                    Updator.Volume.nd.Current = Updator.Volume.Default;
                    Updator.Volume.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSetVolume: /* set the volume to the specified level (0..1) in <1l> */
                    Updator.Volume.nd.Current = (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.Volume.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdIncVolume: /* multiply <1l> by the volume control */
                    Updator.Volume.nd.Current = (double)LargeBCDType.FromRawInt32(Note._Argument1)
                        * Updator.Volume.nd.Current;
                    Updator.Volume.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSweepVolumeAbs: /* <1l> = new volume, <2xs> = # of beats to reach it */
                    SweepToNewValue(
                        ref Updator.Volume.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;
                case NoteCommands.eCmdSweepVolumeRel: /* <1l> = volume adjust, <2xs> = # of beats to reach it */
                    SweepToAdjustedValueMultiplicatively(
                        ref Updator.Volume.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;

                case NoteCommands.eCmdRestoreReleasePoint1: /* restore release point to master default */
                    Updator.ReleasePoint1.nd.Current = Updator.ReleasePoint1.Default;
                    Updator.ReleasePoint1.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSetReleasePoint1: /* set the default release point to new value <1l> */
                    Updator.ReleasePoint1.nd.Current = (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.ReleasePoint1.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdIncReleasePoint1: /* add <1l> to default release point for adjustment */
                    Updator.ReleasePoint1.nd.Current += (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.ReleasePoint1.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdReleasePointOrigin1: /* <1i> -1 = from start, 0 = from end of note */
                    Updator.ReleasePoint1FromStart = (Note._Argument1 < 0);
                    break;
                case NoteCommands.eCmdSweepReleaseAbs1: /* <1l> = new release, <2xs> = # of beats to get there */
                    SweepToNewValue(
                        ref Updator.ReleasePoint1.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;
                case NoteCommands.eCmdSweepReleaseRel1: /* <1l> = release adjust, <2xs> = # of beats to get there */
                    SweepToAdjustedValue(
                        ref Updator.ReleasePoint1.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;

                case NoteCommands.eCmdRestoreReleasePoint2: /* restore release point to master default */
                    Updator.ReleasePoint2.nd.Current = Updator.ReleasePoint2.Default;
                    Updator.ReleasePoint2.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSetReleasePoint2: /* set the default release point to new value <1l> */
                    Updator.ReleasePoint2.nd.Current = (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.ReleasePoint2.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdIncReleasePoint2: /* add <1l> to default release point for adjustment */
                    Updator.ReleasePoint2.nd.Current += (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.ReleasePoint2.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdReleasePointOrigin2: /* <1i> -1 = from start, 0 = from end of note */
                    Updator.ReleasePoint2FromStart = (Note._Argument1 < 0);
                    break;
                case NoteCommands.eCmdSweepReleaseAbs2: /* <1l> = new release, <2xs> = # of beats to get there */
                    SweepToNewValue(
                        ref Updator.ReleasePoint2.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;
                case NoteCommands.eCmdSweepReleaseRel2: /* <1l> = release adjust, <2xs> = # of beats to get there */
                    SweepToAdjustedValue(
                        ref Updator.ReleasePoint2.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;

                case NoteCommands.eCmdRestoreAccent1: /* restore accent value to master default */
                    Updator.Accent1.nd.Current = Updator.Accent1.Default;
                    Updator.Accent1.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSetAccent1: /* specify the new default accent in <1l> */
                    Updator.Accent1.nd.Current = (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.Accent1.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdIncAccent1: /* add <1l> to the default accent */
                    Updator.Accent1.nd.Current += (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.Accent1.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSweepAccentAbs1: /* <1l> = new accent, <2xs> = # of beats to get there */
                    SweepToNewValue(
                        ref Updator.Accent1.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;
                case NoteCommands.eCmdSweepAccentRel1: /* <1l> = accent adjust, <2xs> = # of beats to get there */
                    SweepToAdjustedValue(
                        ref Updator.Accent1.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;

                case NoteCommands.eCmdRestoreAccent2: /* restore accent value to master default */
                    Updator.Accent2.nd.Current = Updator.Accent2.Default;
                    Updator.Accent2.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSetAccent2: /* specify the new default accent in <1l> */
                    Updator.Accent2.nd.Current = (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.Accent2.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdIncAccent2: /* add <1l> to the default accent */
                    Updator.Accent2.nd.Current += (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.Accent2.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSweepAccentAbs2: /* <1l> = new accent, <2xs> = # of beats to get there */
                    SweepToNewValue(
                        ref Updator.Accent2.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;
                case NoteCommands.eCmdSweepAccentRel2: /* <1l> = accent adjust, <2xs> = # of beats to get there */
                    SweepToAdjustedValue(
                        ref Updator.Accent2.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;

                case NoteCommands.eCmdRestoreAccent3: /* restore accent value to master default */
                    Updator.Accent3.nd.Current = Updator.Accent3.Default;
                    Updator.Accent3.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSetAccent3: /* specify the new default accent in <1l> */
                    Updator.Accent3.nd.Current = (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.Accent3.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdIncAccent3: /* add <1l> to the default accent */
                    Updator.Accent3.nd.Current += (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.Accent3.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSweepAccentAbs3: /* <1l> = new accent, <2xs> = # of beats to get there */
                    SweepToNewValue(
                        ref Updator.Accent3.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;
                case NoteCommands.eCmdSweepAccentRel3: /* <1l> = accent adjust, <2xs> = # of beats to get there */
                    SweepToAdjustedValue(
                        ref Updator.Accent3.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;

                case NoteCommands.eCmdRestoreAccent4: /* restore accent value to master default */
                    Updator.Accent4.nd.Current = Updator.Accent4.Default;
                    Updator.Accent4.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSetAccent4: /* specify the new default accent in <1l> */
                    Updator.Accent4.nd.Current = (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.Accent4.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdIncAccent4: /* add <1l> to the default accent */
                    Updator.Accent4.nd.Current += (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.Accent4.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSweepAccentAbs4: /* <1l> = new accent, <2xs> = # of beats to get there */
                    SweepToNewValue(
                        ref Updator.Accent4.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;
                case NoteCommands.eCmdSweepAccentRel4: /* <1l> = accent adjust, <2xs> = # of beats to get there */
                    SweepToAdjustedValue(
                        ref Updator.Accent4.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;

                case NoteCommands.eCmdRestoreAccent5: /* restore accent value to master default */
                    Updator.Accent5.nd.Current = Updator.Accent5.Default;
                    Updator.Accent5.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSetAccent5: /* specify the new default accent in <1l> */
                    Updator.Accent5.nd.Current = (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.Accent5.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdIncAccent5: /* add <1l> to the default accent */
                    Updator.Accent5.nd.Current += (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.Accent5.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSweepAccentAbs5: /* <1l> = new accent, <2xs> = # of beats to get there */
                    SweepToNewValue(
                        ref Updator.Accent5.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;
                case NoteCommands.eCmdSweepAccentRel5: /* <1l> = accent adjust, <2xs> = # of beats to get there */
                    SweepToAdjustedValue(
                        ref Updator.Accent5.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;

                case NoteCommands.eCmdRestoreAccent6: /* restore accent value to master default */
                    Updator.Accent6.nd.Current = Updator.Accent6.Default;
                    Updator.Accent6.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSetAccent6: /* specify the new default accent in <1l> */
                    Updator.Accent6.nd.Current = (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.Accent6.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdIncAccent6: /* add <1l> to the default accent */
                    Updator.Accent6.nd.Current += (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.Accent6.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSweepAccentAbs6: /* <1l> = new accent, <2xs> = # of beats to get there */
                    SweepToNewValue(
                        ref Updator.Accent6.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;
                case NoteCommands.eCmdSweepAccentRel6: /* <1l> = accent adjust, <2xs> = # of beats to get there */
                    SweepToAdjustedValue(
                        ref Updator.Accent6.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;

                case NoteCommands.eCmdRestoreAccent7: /* restore accent value to master default */
                    Updator.Accent7.nd.Current = Updator.Accent7.Default;
                    Updator.Accent7.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSetAccent7: /* specify the new default accent in <1l> */
                    Updator.Accent7.nd.Current = (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.Accent7.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdIncAccent7: /* add <1l> to the default accent */
                    Updator.Accent7.nd.Current += (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.Accent7.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSweepAccentAbs7: /* <1l> = new accent, <2xs> = # of beats to get there */
                    SweepToNewValue(
                        ref Updator.Accent7.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;
                case NoteCommands.eCmdSweepAccentRel7: /* <1l> = accent adjust, <2xs> = # of beats to get there */
                    SweepToAdjustedValue(
                        ref Updator.Accent7.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;

                case NoteCommands.eCmdRestoreAccent8: /* restore accent value to master default */
                    Updator.Accent8.nd.Current = Updator.Accent8.Default;
                    Updator.Accent8.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSetAccent8: /* specify the new default accent in <1l> */
                    Updator.Accent8.nd.Current = (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.Accent8.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdIncAccent8: /* add <1l> to the default accent */
                    Updator.Accent8.nd.Current += (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.Accent8.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSweepAccentAbs8: /* <1l> = new accent, <2xs> = # of beats to get there */
                    SweepToNewValue(
                        ref Updator.Accent8.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;
                case NoteCommands.eCmdSweepAccentRel8: /* <1l> = accent adjust, <2xs> = # of beats to get there */
                    SweepToAdjustedValue(
                        ref Updator.Accent8.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;

                case NoteCommands.eCmdRestorePitchDispDepth: /* restore max pitch disp depth value to default */
                    Updator.PitchDisplacementDepthLimit.nd.Current = Updator.PitchDisplacementDepthLimit.Default;
                    Updator.PitchDisplacementDepthLimit.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSetPitchDispDepth: /* set new max pitch disp depth <1l> */
                    Updator.PitchDisplacementDepthLimit.nd.Current
                        = (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.PitchDisplacementDepthLimit.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdIncPitchDispDepth: /* add <1l> to the default pitch disp depth */
                    Updator.PitchDisplacementDepthLimit.nd.Current
                        += (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.PitchDisplacementDepthLimit.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSweepPitchDispDepthAbs: /* <1l> = new depth, <2xs> = # of beats */
                    SweepToNewValue(
                        ref Updator.PitchDisplacementDepthLimit.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;
                case NoteCommands.eCmdSweepPitchDispDepthRel: /* <1l> = depth adjust, <2xs> = # of beats */
                    SweepToAdjustedValue(
                        ref Updator.PitchDisplacementDepthLimit.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;

                case NoteCommands.eCmdRestorePitchDispRate: /* restore max pitch disp rate to the master default */
                    Updator.PitchDisplacementRateLimit.nd.Current = Updator.PitchDisplacementRateLimit.Default;
                    Updator.PitchDisplacementRateLimit.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSetPitchDispRate: /* set new max pitch disp rate in seconds to <1l> */
                    Updator.PitchDisplacementRateLimit.nd.Current
                        = (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.PitchDisplacementRateLimit.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdIncPitchDispRate: /* add <1l> to the default max pitch disp rate */
                    Updator.PitchDisplacementRateLimit.nd.Current
                        += (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.PitchDisplacementRateLimit.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSweepPitchDispRateAbs: /* <1l> = new rate, <2xs> = # of beats to get there */
                    SweepToNewValue(
                        ref Updator.PitchDisplacementRateLimit.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;
                case NoteCommands.eCmdSweepPitchDispRateRel: /* <1l> = rate adjust, <2xs> = # of beats to get there */
                    SweepToAdjustedValue(
                        ref Updator.PitchDisplacementRateLimit.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;

                case NoteCommands.eCmdRestorePitchDispStart: /* restore pitch disp start point to default */
                    Updator.PitchDisplacementStartPoint.nd.Current = Updator.PitchDisplacementStartPoint.Default;
                    Updator.PitchDisplacementStartPoint.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSetPitchDispStart: /* set the start point to <1l> */
                    Updator.PitchDisplacementStartPoint.nd.Current
                        = (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.PitchDisplacementStartPoint.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdIncPitchDispStart: /* add <1l> to the pitch disp start point */
                    Updator.PitchDisplacementStartPoint.nd.Current
                        += (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.PitchDisplacementStartPoint.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdPitchDispStartOrigin: /* specify the origin, same as for release point <1i> */
                    Updator.PitchDisplacementStartPointFromStart = (Note._Argument1 < 0);
                    break;
                case NoteCommands.eCmdSweepPitchDispStartAbs: /* <1l> = new vib start, <2xs> = # of beats */
                    SweepToNewValue(
                        ref Updator.PitchDisplacementStartPoint.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;
                case NoteCommands.eCmdSweepPitchDispStartRel: /* <1l> = vib adjust, <2xs> = # of beats */
                    SweepToAdjustedValue(
                        ref Updator.PitchDisplacementStartPoint.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;

                case NoteCommands.eCmdRestoreHurryUp: /* restore default hurryup factor */
                    Updator.HurryUp.nd.Current = Updator.HurryUp.Default;
                    Updator.HurryUp.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSetHurryUp: /* set the hurryup factor to <1l> */
                    Updator.HurryUp.nd.Current = (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.HurryUp.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdIncHurryUp: /* multiply <1l> by the hurryup factor */
                    Updator.HurryUp.nd.Current *= (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.HurryUp.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSweepHurryUpAbs: /* <1l> = new hurryup factor, <2xs> = # of beats */
                    SweepToNewValue(
                        ref Updator.HurryUp.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;
                case NoteCommands.eCmdSweepHurryUpRel: /* <1l> = hurryup adjust, <2xs> = # of beats to get there */
                    SweepToAdjustedValue(
                        ref Updator.HurryUp.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;

                case NoteCommands.eCmdRestoreDetune: /* restore the default detune factor */
                    Updator.Detune.nd.Current = Updator.Detune.Default;
                    Updator.Detune.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSetDetune: /* set the detune factor to <1l> */
                    Updator.Detune.nd.Current = (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.Detune.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdIncDetune: /* add <1l> to current detune factor */
                    Updator.Detune.nd.Current += (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.Detune.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdDetuneMode: /* <1i>:  -1: Hertz, 0: half-steps */
                    Updator.DetuneHertz = (Note._Argument1 < 0);
                    break;
                case NoteCommands.eCmdSweepDetuneAbs: /* <1l> = new detune, <2xs> = # of beats */
                    SweepToNewValue(
                        ref Updator.Detune.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;
                case NoteCommands.eCmdSweepDetuneRel: /* <1l> = detune adjust, <2xs> = # of beats */
                    SweepToAdjustedValue(
                        ref Updator.Detune.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;

                case NoteCommands.eCmdRestoreEarlyLateAdjust: /* restore the default early/late adjust value */
                    Updator.EarlyLateAdjust.nd.Current = Updator.EarlyLateAdjust.Default;
                    Updator.EarlyLateAdjust.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSetEarlyLateAdjust: /* set the early/late adjust value to <1l> */
                    Updator.EarlyLateAdjust.nd.Current = (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.EarlyLateAdjust.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdIncEarlyLateAdjust: /* add <1l> to the current early/late adjust value */
                    Updator.EarlyLateAdjust.nd.Current += (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.EarlyLateAdjust.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSweepEarlyLateAbs: /* <1l> = new early/late adjust, <2xs> = # of beats */
                    SweepToNewValue(
                        ref Updator.EarlyLateAdjust.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;
                case NoteCommands.eCmdSweepEarlyLateRel: /* <1l> = early/late delta, <2xs> = # of beats to get there */
                    SweepToAdjustedValue(
                        ref Updator.EarlyLateAdjust.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;

                case NoteCommands.eCmdRestoreDurationAdjust: /* restore the default duration adjust value */
                    Updator.DurationAdjust.nd.Current = Updator.DurationAdjust.Default;
                    Updator.DurationAdjust.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSetDurationAdjust: /* set duration adjust value to <1l> */
                    Updator.DurationAdjust.nd.Current = (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.DurationAdjust.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdIncDurationAdjust: /* add <1l> to the current duration adjust value */
                    Updator.DurationAdjust.nd.Current += (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.DurationAdjust.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSweepDurationAbs: /* <1l> = new duration adjust, <2xs> = # of beats */
                    SweepToNewValue(
                        ref Updator.DurationAdjust.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;
                case NoteCommands.eCmdSweepDurationRel: /* <1l> = duration adjust delta, <2xs> = # of beats */
                    SweepToAdjustedValue(
                        ref Updator.DurationAdjust.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;
                case NoteCommands.eCmdDurationAdjustMode: /* <1i>:  -1: Multiplicative, 0: Additive */
                    Updator.DurationAdjustAdditive = (Note._Argument1 >= 0);
                    break;

                case NoteCommands.eCmdSetMeter: /* <1i> = numerator, <2i> = denominator */
                    break;
                case NoteCommands.eCmdSetMeasureNumber: /* <1i> = new number */
                    break;

                case NoteCommands.eCmdSetTranspose: /* <1i> = new transpose value */
                    Updator.TransposeHalfsteps = Note._Argument1;
                    break;
                case NoteCommands.eCmdAdjustTranspose: /* <1i> = transpose adjustor */
                    Updator.TransposeHalfsteps += Note._Argument1;
                    break;

                case NoteCommands.eCmdSetFrequencyValue: /* <1i> = 0..11 index, <2xs> = cents */
                    {
                        int Index = Note._Argument1;
                        if ((Index >= 0) && (Index < 12))
                        {
                            double cents = (double)LargeBCDType.FromRawInt32(Note._Argument2);
                            Updator.FrequencyTable[Index].nd.Current = Math.Pow(2, cents / 1200d);
                            Updator.FrequencyTable[Index].nd.ChangeCountdown = 0;
                        }
                    }
                    break;
                case NoteCommands.eCmdAdjustFrequencyValue: /* <1i> = 0..11 index, <2xs> = cents adjust */
                    {
                        int Index = Note._Argument1;
                        if ((Index >= 0) && (Index < 12))
                        {
                            double cents = (double)LargeBCDType.FromRawInt32(Note._Argument2);
                            Updator.FrequencyTable[Index].nd.Current *= Math.Pow(2, cents / 1200d);
                            Updator.FrequencyTable[Index].nd.ChangeCountdown = 0;
                        }
                    }
                    break;
                case NoteCommands.eCmdSetFrequencyValueLegacy: /* <1i> = 0..11 index, <2xs> = normal freq * 1000 */
                    {
                        int Index = Note._Argument1;
                        if ((Index >= 0) && (Index < 12))
                        {
                            double refVal = (double)0.001 * (double)LargeBCDType.FromRawInt32(Note._Argument2);
                            Updator.FrequencyTable[Index].nd.Current = refVal;
                            Updator.FrequencyTable[Index].nd.ChangeCountdown = 0;
                        }
                    }
                    break;
                case NoteCommands.eCmdAdjustFrequencyValueLegacy: /* <1i> = 0..11 index, <2xs> = scale factor * 1000 */
                    {
                        int Index = Note._Argument1;
                        if ((Index >= 0) && (Index < 12))
                        {
                            double refVal = (double)0.001 * (double)LargeBCDType.FromRawInt32(Note._Argument2);
                            Updator.FrequencyTable[Index].nd.Current *= refVal;
                            Updator.FrequencyTable[Index].nd.ChangeCountdown = 0;
                        }
                    }
                    break;
                case NoteCommands.eCmdResetFrequencyValue: /* <1i> = 0..11 index */
                    {
                        int Index = Note._Argument1;
                        if ((Index >= 0) && (Index < 12))
                        {
                            Updator.FrequencyTable[Index].nd.Current = Updator.FrequencyTableLastLoaded[Index];
                            Updator.FrequencyTable[Index].nd.ChangeCountdown = 0;
                        }
                    }
                    break;
                case NoteCommands.eCmdLoadFrequencyModel: // <1s> = model name, <1l> = tonic offset (integer 0..11)
                    {
                        // <1l> arg:
                        //  - tonic offset (absolute magnitude, integer part, 0..11 modulo 12)
                        //  - sign: negative: relative to existing tonic; non-neg: reset relative to standard concert pitch
                        //    (use -12 to specify for tonic C since 0 can't be made negative)
                        LargeBCDType arg = LargeBCDType.FromRawInt32(Note._Argument1);
                        int tonicOffset = (int)Math.Abs((double)arg) % 12;
                        bool relativeToCurrent = (double)arg < 0;
                        SynthErrorCodes error = LoadPitchTable(
                            Note._StringArgument1,
                            tonicOffset,
                            relativeToCurrent,
                            Updator.FrequencyTable,
                            Updator.FrequencyTableLastLoaded,
                            SynthParams);
                        if (error != SynthErrorCodes.eSynthDone)
                        {
                            return error;
                        }
                    }
                    break;
                case NoteCommands.eCmdSweepFrequencyValue0Absolute: /* <1l> <2xs> */
                case NoteCommands.eCmdSweepFrequencyValue1Absolute: /* <1l> <2xs> */
                case NoteCommands.eCmdSweepFrequencyValue2Absolute: /* <1l> <2xs> */
                case NoteCommands.eCmdSweepFrequencyValue3Absolute: /* <1l> <2xs> */
                case NoteCommands.eCmdSweepFrequencyValue4Absolute: /* <1l> <2xs> */
                case NoteCommands.eCmdSweepFrequencyValue5Absolute: /* <1l> <2xs> */
                case NoteCommands.eCmdSweepFrequencyValue6Absolute: /* <1l> <2xs> */
                case NoteCommands.eCmdSweepFrequencyValue7Absolute: /* <1l> <2xs> */
                case NoteCommands.eCmdSweepFrequencyValue8Absolute: /* <1l> <2xs> */
                case NoteCommands.eCmdSweepFrequencyValue9Absolute: /* <1l> <2xs> */
                case NoteCommands.eCmdSweepFrequencyValue10Absolute: /* <1l> <2xs> */
                case NoteCommands.eCmdSweepFrequencyValue11Absolute: /* <1l> <2xs> */
                    {
                        int Index = (NoteCommands)(Note.Flags & ~NoteFlags.eCommandFlag) - NoteCommands.eCmdSweepFrequencyValue0Absolute;
                        Debug.Assert((Index >= 0) && (Index < 12));
                        double cents = (double)LargeBCDType.FromRawInt32(Note._Argument1);
                        SweepToNewValue(
                            ref Updator.FrequencyTable[Index].nd,
                            (LargeBCDType)Math.Pow(2, cents / 1200d),
                            SmallExtBCDType.FromRawInt32(Note._Argument2));
                    }
                    break;
                case NoteCommands.eCmdSweepFrequencyValue0Relative: /* <1l> <2xs> */
                case NoteCommands.eCmdSweepFrequencyValue1Relative: /* <1l> <2xs> */
                case NoteCommands.eCmdSweepFrequencyValue2Relative: /* <1l> <2xs> */
                case NoteCommands.eCmdSweepFrequencyValue3Relative: /* <1l> <2xs> */
                case NoteCommands.eCmdSweepFrequencyValue4Relative: /* <1l> <2xs> */
                case NoteCommands.eCmdSweepFrequencyValue5Relative: /* <1l> <2xs> */
                case NoteCommands.eCmdSweepFrequencyValue6Relative: /* <1l> <2xs> */
                case NoteCommands.eCmdSweepFrequencyValue7Relative: /* <1l> <2xs> */
                case NoteCommands.eCmdSweepFrequencyValue8Relative: /* <1l> <2xs> */
                case NoteCommands.eCmdSweepFrequencyValue9Relative: /* <1l> <2xs> */
                case NoteCommands.eCmdSweepFrequencyValue10Relative: /* <1l> <2xs> */
                case NoteCommands.eCmdSweepFrequencyValue11Relative: /* <1l> <2xs> */
                    {
                        int Index = (NoteCommands)(Note.Flags & ~NoteFlags.eCommandFlag) - NoteCommands.eCmdSweepFrequencyValue0Relative;
                        Debug.Assert((Index >= 0) && (Index < 12));
                        double cents = (double)LargeBCDType.FromRawInt32(Note._Argument1);
                        SweepToAdjustedValueMultiplicatively(
                            ref Updator.FrequencyTable[Index].nd,
                            (LargeBCDType)Math.Pow(2, cents / 1200d),
                            SmallExtBCDType.FromRawInt32(Note._Argument2));
                    }
                    break;

                case NoteCommands.eCmdMarker: /* <string> holds the text */
                    break;

                case NoteCommands.eCmdSequenceBegin: /* <string> holds sequence name */
                case NoteCommands.eCmdSequenceEnd:
                case NoteCommands.eCmdSetSequence: /* <string1> holds track/group name, <string2> hold sequence name */
                case NoteCommands.eCmdSetSequenceDeferred: /* <string1> holds track/group name, <string2> hold sequence name */
                case NoteCommands.eCmdEndSequencing: /* <string1> holds track/group name */
                    break;

                case NoteCommands.eCmdRedirect: /* <string> holds target track/group name */
                case NoteCommands.eCmdRedirectEnd:
                    break;

                case NoteCommands.eCmdReleaseAll1:
                case NoteCommands.eCmdReleaseAll2:
                case NoteCommands.eCmdReleaseAll3:
                    break;

                case NoteCommands.eCmdRestorePortamento: /* restore the portamento to the default for the channel */
                    Updator.Portamento.nd.Current = Updator.Portamento.Default;
                    Updator.Portamento.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSetPortamento: /* set the portamento to the specified level (0..1) in <1l> */
                    Updator.Portamento.nd.Current = (double)LargeBCDType.FromRawInt32(Note._Argument1);
                    Updator.Portamento.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdIncPortamento: /* multiply <1l> by the portamento control */
                    Updator.Portamento.nd.Current = (double)LargeBCDType.FromRawInt32(Note._Argument1)
                        * Updator.Portamento.nd.Current;
                    Updator.Portamento.nd.ChangeCountdown = 0;
                    break;
                case NoteCommands.eCmdSweepPortamentoAbs: /* <1l> = new portamento, <2xs> = # of beats to reach it */
                    SweepToNewValue(
                        ref Updator.Portamento.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;
                case NoteCommands.eCmdSweepPortamentoRel: /* <1l> = portamento adjust, <2xs> = # of beats to reach it */
                    SweepToAdjustedValueMultiplicatively(
                        ref Updator.Portamento.nd,
                        LargeBCDType.FromRawInt32(Note._Argument1),
                        SmallExtBCDType.FromRawInt32(Note._Argument2));
                    break;
            }

            return SynthErrorCodes.eSynthDone;
        }
    }
}
