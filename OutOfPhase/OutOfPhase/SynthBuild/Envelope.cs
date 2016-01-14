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
        /* possible patterns of the envelope transition */
        public enum EnvTransTypes
        {
            eEnvelopeLinearInAmplitude,
            eEnvelopeLinearInDecibels,
        }

        /* possible ways of obtaining the new target value */
        public enum EnvTargetTypes
        {
            eEnvelopeTargetAbsolute, /* target is new amplitude */
            eEnvelopeTargetScaling, /* target is current amplitude * scaling factor */
        }

        /* these are the kinds of sustain points there are */
        public enum SustainTypes
        {
            eEnvelopeSustainPointSkip, /* hold this point, skip here on release */
            eEnvelopeReleasePointSkip, /* don't hold this point, but skip here on release */
            eEnvelopeSustainPointNoSkip, /* hold this point, but don't skip ahead on release */
            eEnvelopeReleasePointNoSkip, /* don't hold this point and don't skip ahead on release */
        }

        public class EnvStepRec
        {
            /* transition duration (seconds) */
            public double Duration;

            /* destination amplitude (0..1) or scaling factor */
            public double EndPoint;

            /* transition type */
            public EnvTransTypes TransitionType;

            /* way of deriving the target */
            public EnvTargetTypes TargetType;

            /* adjustment factors */
            public AccentRec AccentAmp;
            public double FrequencyAmpRolloff;
            public double FrequencyAmpNormalization;
            public AccentRec AccentRate;
            public double FrequencyRateRolloff;
            public double FrequencyRateNormalization;

            // alternative initializers
            public PcodeRec DurationFunction; // null == constant (above - old behavior), signature for EnvelopeInitParamEval()
            public PcodeRec EndPointFunction; // null == constant (above - old behavior), signature for EnvelopeInitParamEval()
        }

        // multiple threads may init the field, so for safety, the type is 'int' to take up a machine word (in case of non-Intel memory models)
        public enum EnvelopeContainsFormulaType : int { NotInitialized = 0, No, Yes };

        public class EnvelopeRec
        {
            /* number of envelope phases. 0 phases means the envelope produces constant value */
            public int NumPhases;

            /* list of definitions for each transition phase */
            public EnvStepRec[] PhaseArray;

            /* phase at which first sustain occurs. (released by key-up) */
            /* if the value of this is N, then sustain will occur after phase N has */
            /* completed.  if it is 0, then sustain will occur after phase 0 has completed. */
            /* if it is -1, then sustain will not occur.  sustain may not be NumPhases since */
            /* that would be the end of envelope and would be the same as final value hold. */
            public int SustainPhase1;
            public SustainTypes SustainPhase1Type;
            /* phase at which second sustain occurs. */
            public int SustainPhase2;
            public SustainTypes SustainPhase2Type;
            /* phase at which note-end sustain occurs */
            public int SustainPhase3;
            public SustainTypes SustainPhase3Type;

            /* phase used to align envelope timing with other envelopes */
            public int Origin;

            /* parameters which control how the instantaneous pitch affects the output */
            /* of the envelope generator. */
            public double GlobalPitchRateRolloff;
            public double GlobalPitchRateNormalization;

            /* multiplicative factor that scales the output of the envelope generator. */
            public double OverallScalingFactor;

            /* formula for envelope processor.  NIL means no formula was specified. */
            public PcodeRec Formula;

            /* constant envelope value */
            public bool ConstantShortcut;
            public double ConstantShortcutValue;

            // used for optimizing formula evaluation
            public EnvelopeContainsFormulaType containsFormula;
        }

        /* create a new envelope record with nothing in it */
        public static EnvelopeRec NewEnvelope()
        {
            EnvelopeRec Envelope = new EnvelopeRec();

            Envelope.PhaseArray = new EnvStepRec[0];

            Envelope.SustainPhase1 = -1;
            Envelope.SustainPhase2 = -1;
            Envelope.SustainPhase3 = -1;
            Envelope.SustainPhase1Type = SustainTypes.eEnvelopeReleasePointNoSkip;
            Envelope.SustainPhase2Type = SustainTypes.eEnvelopeReleasePointNoSkip;
            Envelope.SustainPhase3Type = SustainTypes.eEnvelopeReleasePointNoSkip;
            //Envelope.Origin = 0;
            //Envelope.NumPhases = 0;
            //Envelope.GlobalPitchRateRolloff = 0;
            Envelope.GlobalPitchRateNormalization = Constants.MIDDLEC;
            Envelope.OverallScalingFactor = 1;
            //Envelope.Formula = null;
            Envelope.ConstantShortcut = false;

            return Envelope;
        }

        /* find out how many frames there are in the envelope */
        public static int GetEnvelopeNumFrames(EnvelopeRec Envelope)
        {
            return Envelope.NumPhases;
        }

        /* set a release point.  -1 means this release point is ignored */
        public static void EnvelopeSetReleasePoint1(
            EnvelopeRec Envelope,
            int Release,
            SustainTypes ReleaseType)
        {
#if DEBUG
            if ((Release < -1) || (Release > Envelope.NumPhases))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if ((ReleaseType != SustainTypes.eEnvelopeSustainPointSkip)
                && (ReleaseType != SustainTypes.eEnvelopeReleasePointSkip)
                && (ReleaseType != SustainTypes.eEnvelopeSustainPointNoSkip)
                && (ReleaseType != SustainTypes.eEnvelopeReleasePointNoSkip))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Envelope.SustainPhase1 = Release;
            Envelope.SustainPhase1Type = ReleaseType;
        }

        /* set a release point.  -1 means this release point is ignored */
        public static void EnvelopeSetReleasePoint2(
            EnvelopeRec Envelope,
            int Release,
            SustainTypes ReleaseType)
        {
#if DEBUG
            if ((Release < -1) || (Release > Envelope.NumPhases))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if ((ReleaseType != SustainTypes.eEnvelopeSustainPointSkip)
                && (ReleaseType != SustainTypes.eEnvelopeReleasePointSkip)
                && (ReleaseType != SustainTypes.eEnvelopeSustainPointNoSkip)
                && (ReleaseType != SustainTypes.eEnvelopeReleasePointNoSkip))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Envelope.SustainPhase2 = Release;
            Envelope.SustainPhase2Type = ReleaseType;
        }

        /* set a release point.  -1 means this release point is ignored */
        public static void EnvelopeSetReleasePoint3(
            EnvelopeRec Envelope,
            int Release,
            SustainTypes ReleaseType)
        {
#if DEBUG
            if ((Release < -1) || (Release > Envelope.NumPhases))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if ((ReleaseType != SustainTypes.eEnvelopeSustainPointSkip)
                && (ReleaseType != SustainTypes.eEnvelopeReleasePointSkip)
                && (ReleaseType != SustainTypes.eEnvelopeSustainPointNoSkip)
                && (ReleaseType != SustainTypes.eEnvelopeReleasePointNoSkip))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Envelope.SustainPhase3 = Release;
            Envelope.SustainPhase3Type = ReleaseType;
        }

        /* get the value of a release point */
        public static int GetEnvelopeReleasePoint1(EnvelopeRec Envelope)
        {
            return Envelope.SustainPhase1;
        }

        /* get the value of a release point */
        public static int GetEnvelopeReleasePoint2(EnvelopeRec Envelope)
        {
            return Envelope.SustainPhase2;
        }

        /* get the value of a release point */
        public static int GetEnvelopeReleasePoint3(EnvelopeRec Envelope)
        {
            return Envelope.SustainPhase3;
        }

        /* get the release point type */
        public static SustainTypes GetEnvelopeReleaseType1(EnvelopeRec Envelope)
        {
            return Envelope.SustainPhase1Type;
        }

        public static SustainTypes GetEnvelopeReleaseType2(EnvelopeRec Envelope)
        {
            return Envelope.SustainPhase2Type;
        }

        /* get the release point type */
        public static SustainTypes GetEnvelopeReleaseType3(EnvelopeRec Envelope)
        {
            return Envelope.SustainPhase3Type;
        }

        /* set the origin of the envelope */
        public static void EnvelopeSetOrigin(
            EnvelopeRec Envelope,
            int Origin)
        {
            Envelope.Origin = Origin;
        }

        /* get the origin from the envelope */
        public static int GetEnvelopeOrigin(EnvelopeRec Envelope)
        {
            return Envelope.Origin;
        }

        /* get the global pitch rate rolloff factor */
        public static double GetGlobalEnvelopePitchRateRolloff(EnvelopeRec Envelope)
        {
            return Envelope.GlobalPitchRateRolloff;
        }

        /* get the global pitch rate normalization factor */
        public static double GetGlobalEnvelopePitchRateNormalization(EnvelopeRec Envelope)
        {
            return Envelope.GlobalPitchRateNormalization;
        }

        /* change the global pitch rate rolloff factor */
        public static void PutGlobalEnvelopePitchRateRolloff(
            EnvelopeRec Envelope,
            double NewGlobalPitchRateRolloff)
        {
            Envelope.GlobalPitchRateRolloff = NewGlobalPitchRateRolloff;
        }

        /* change the global pitch rate normalization factor */
        public static void PutGlobalEnvelopePitchRateNormalization(
            EnvelopeRec Envelope,
            double NewGlobalPitchRateNormalization)
        {
            Envelope.GlobalPitchRateNormalization = NewGlobalPitchRateNormalization;
        }

        public static void EnvelopeSetAccentAmp(
            EnvelopeRec Envelope,
            double Val,
            int Phase,
            int AccentNumber)
        {
#if DEBUG
            if ((Phase < 0) || (Phase >= Envelope.NumPhases))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            SetAccentMemberValue(ref Envelope.PhaseArray[Phase].AccentAmp, AccentNumber, Val);
        }

        public static void EnvelopeSetFreqAmpRolloff(
            EnvelopeRec Envelope,
            double Val,
            int Phase)
        {
#if DEBUG
            if ((Phase < 0) || (Phase >= Envelope.NumPhases))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Envelope.PhaseArray[Phase].FrequencyAmpRolloff = Val;
        }

        public static void EnvelopeSetFreqAmpNormalization(
            EnvelopeRec Envelope,
            double Val,
            int Phase)
        {
#if DEBUG
            if ((Phase < 0) || (Phase >= Envelope.NumPhases))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Envelope.PhaseArray[Phase].FrequencyAmpNormalization = Val;
        }

        public static void EnvelopeSetAccentRate(
            EnvelopeRec Envelope,
            double Val,
            int Phase,
            int AccentNumber)
        {
#if DEBUG
            if ((Phase < 0) || (Phase >= Envelope.NumPhases))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            SetAccentMemberValue(ref Envelope.PhaseArray[Phase].AccentRate, AccentNumber, Val);
        }

        public static void EnvelopeSetFreqRateRolloff(
            EnvelopeRec Envelope,
            double Val,
            int Phase)
        {
#if DEBUG
            if ((Phase < 0) || (Phase >= Envelope.NumPhases))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Envelope.PhaseArray[Phase].FrequencyRateRolloff = Val;
        }

        public static void EnvelopeSetFreqRateNormalization(
            EnvelopeRec Envelope,
            double Val,
            int Phase)
        {
#if DEBUG
            if ((Phase < 0) || (Phase >= Envelope.NumPhases))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Envelope.PhaseArray[Phase].FrequencyRateNormalization = Val;
        }

        /* set the overall amplitude scaling factor */
        public static void EnvelopeSetOverallAmplitude(
            EnvelopeRec Envelope,
            double OverallAmplitude)
        {
            Envelope.OverallScalingFactor = OverallAmplitude;
        }

        /* get the overall amplitude */
        public static double GetEnvelopeOverallAmplitude(EnvelopeRec Envelope)
        {
            return Envelope.OverallScalingFactor;
        }

        /* insert a new phase at the specified position.  Values are undefined */
        public static void EnvelopeInsertPhase(
            EnvelopeRec Envelope,
            int Index)
        {
#if DEBUG
            if ((Index < 0) || (Index > Envelope.NumPhases))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif

            Envelope.NumPhases++;
            Array.Resize(ref Envelope.PhaseArray, Envelope.NumPhases);
            Array.Copy(Envelope.PhaseArray, Index, Envelope.PhaseArray, Index + 1, Envelope.NumPhases - Index - 1);

            Envelope.PhaseArray[Index] = new EnvStepRec();
            //Envelope.PhaseArray[Index].Duration = 0;
            //Envelope.PhaseArray[Index].EndPoint = 0;
            Envelope.PhaseArray[Index].TransitionType = EnvTransTypes.eEnvelopeLinearInAmplitude;
            Envelope.PhaseArray[Index].TargetType = EnvTargetTypes.eEnvelopeTargetAbsolute;
            //InitializeAccentZero(out Envelope.PhaseArray[Index].AccentAmp);
            //Envelope.PhaseArray[Index].FrequencyAmpRolloff = 0;
            Envelope.PhaseArray[Index].FrequencyAmpNormalization = Constants.MIDDLEC;
            //InitializeAccentZero(out Envelope.PhaseArray[Index].AccentRate);
            //Envelope.PhaseArray[Index].FrequencyRateRolloff = 0;
            Envelope.PhaseArray[Index].FrequencyRateNormalization = Constants.MIDDLEC;
        }

        /* set a new value for the specified phase's duration */
        public static void EnvelopeSetPhaseDuration(
            EnvelopeRec Envelope,
            int Index,
            double Duration)
        {
#if DEBUG
            if ((Index < 0) || (Index > Envelope.NumPhases))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Envelope.PhaseArray[Index].Duration = Duration;
        }

        public static void EnvelopeSetPhaseDurationFormula(
            EnvelopeRec Envelope,
            int Index,
            PcodeRec DurationFunction)
        {
#if DEBUG
            if ((Index < 0) || (Index > Envelope.NumPhases))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Envelope.PhaseArray[Index].DurationFunction = DurationFunction;
        }

        /* get the duration from the specified envelope position */
        public static double GetEnvelopePhaseDuration(
            EnvelopeRec Envelope,
            int Index)
        {
#if DEBUG
            if ((Index < 0) || (Index > Envelope.NumPhases))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return Envelope.PhaseArray[Index].Duration;
        }

        public static PcodeRec GetEnvelopePhaseDurationFormula(
            EnvelopeRec Envelope,
            int Index)
        {
#if DEBUG
            if ((Index < 0) || (Index > Envelope.NumPhases))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return Envelope.PhaseArray[Index].DurationFunction;
        }

        /* set a new value for the specified phase's ultimate value */
        public static void EnvelopeSetPhaseFinalValue(
            EnvelopeRec Envelope,
            int Index,
            double FinalValue)
        {
#if DEBUG
            if ((Index < 0) || (Index > Envelope.NumPhases))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Envelope.PhaseArray[Index].EndPoint = FinalValue;
        }

        public static void EnvelopeSetPhaseFinalValueFormula(
            EnvelopeRec Envelope,
            int Index,
            PcodeRec FinalValueFunction)
        {
#if DEBUG
            if ((Index < 0) || (Index > Envelope.NumPhases))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Envelope.PhaseArray[Index].EndPointFunction = FinalValueFunction;
        }

        /* get the phase's ultimate value */
        public static double GetEnvelopePhaseFinalValue(
            EnvelopeRec Envelope,
            int Index)
        {
#if DEBUG
            if ((Index < 0) || (Index > Envelope.NumPhases))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return Envelope.PhaseArray[Index].EndPoint;
        }

        public static PcodeRec GetEnvelopePhaseFinalValueFormula(
            EnvelopeRec Envelope,
            int Index)
        {
#if DEBUG
            if ((Index < 0) || (Index > Envelope.NumPhases))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return Envelope.PhaseArray[Index].EndPointFunction;
        }

        /* set the value for a phase's transition type */
        public static void EnvelopeSetPhaseTransitionType(
            EnvelopeRec Envelope,
            int Index,
            EnvTransTypes TransitionType)
        {
#if DEBUG
            if ((Index < 0) || (Index > Envelope.NumPhases))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Envelope.PhaseArray[Index].TransitionType = TransitionType;
        }

        /* obtain the value in a phase's transition type */
        public static EnvTransTypes GetEnvelopePhaseTransitionType(
            EnvelopeRec Envelope,
            int Index)
        {
#if DEBUG
            if ((Index < 0) || (Index > Envelope.NumPhases))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return Envelope.PhaseArray[Index].TransitionType;
        }

        /* set the value for a phase's target type */
        public static void EnvelopeSetPhaseTargetType(
            EnvelopeRec Envelope,
            int Index,
            EnvTargetTypes TargetType)
        {
#if DEBUG
            if ((Index < 0) || (Index > Envelope.NumPhases))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Envelope.PhaseArray[Index].TargetType = TargetType;
        }

        /* get the target type for a phase */
        public static EnvTargetTypes GetEnvelopePhaseTargetType(
            EnvelopeRec Envelope,
            int Index)
        {
#if DEBUG
            if ((Index < 0) || (Index > Envelope.NumPhases))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return Envelope.PhaseArray[Index].TargetType;
        }

        /* specify a formula for the envelope generator. */
        public static void EnvelopeSetFormula(
            EnvelopeRec Envelope,
            PcodeRec Formula)
        {
            if (Envelope.Formula != null)
            {
                // EnvelopeSetFormula: formula has already been specified
                Debug.Assert(false);
                throw new ArgumentException();
            }
            Envelope.Formula = Formula;
        }

        /* get the formula for the envelope generator.  returns NIL if none specified. */
        public static PcodeRec GetEnvelopeFormula(EnvelopeRec Envelope)
        {
            return Envelope.Formula;
        }

        /* set the constant shortcut value */
        public static void EnvelopeSetConstantShortcut(
            EnvelopeRec Envelope,
            double Value)
        {
            Envelope.ConstantShortcut = true;
            Envelope.ConstantShortcutValue = Value;
        }

        /* is shortcut constant specified? */
        public static bool GetEnvelopeUseConstantShortcut(EnvelopeRec Envelope)
        {
            return Envelope.ConstantShortcut;
        }

        /* get value for constant shortcut */
        public static double GetEnvelopeConstantShortcutValue(EnvelopeRec Envelope)
        {
            if (!Envelope.ConstantShortcut)
            {
                // GetEnvelopeConstantShortcutValue: constant shortcut not enabled
                Debug.Assert(false);
                throw new ArgumentException();
            }
            return Envelope.ConstantShortcutValue;
        }

        public static bool EnvelopeContainsFormula(EnvelopeRec Envelope)
        {
            if (Envelope.containsFormula == EnvelopeContainsFormulaType.NotInitialized)
            {
                Envelope.containsFormula = EnvelopeContainsFormulaType.No;
                if (Envelope.Formula != null)
                {
                    Envelope.containsFormula = EnvelopeContainsFormulaType.Yes;
                }
                for (int i = 0; i < Envelope.PhaseArray.Length; i++)
                {
                    if ((Envelope.PhaseArray[i].DurationFunction != null)
                        || (Envelope.PhaseArray[i].EndPointFunction != null))
                    {
                        Envelope.containsFormula = EnvelopeContainsFormulaType.Yes;
                        break;
                    }
                }
            }
            return Envelope.containsFormula != EnvelopeContainsFormulaType.No;
        }
    }
}
