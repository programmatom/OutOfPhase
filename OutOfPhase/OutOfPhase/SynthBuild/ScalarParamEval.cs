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
using System.Diagnostics;
using System.Text;

namespace OutOfPhase
{
    public static partial class Synthesizer
    {
        // TODO: make callers of these methods handle the error codes

        public class ScalarParamEvalRec
        {
            /* original value specified in the instrument definition by user */
            public double SpecifiedValue;

            /* accents specified in the instrument definition by user.  NIL if not initialized, */
            /* must not be NIL during evalution */
            public AccentRec SpecifiedAccentModifiers;

            /* formula specified in instrument definition.  NIL if no formula is to be used. */
            public PcodeRec SpecifiedFormula;
        }

        // TODO: these invoke an anonymous function which either evaluates an expression or invokes
        // a previously defined function. In the case of just calling a function, the args are pushed
        // twice. TODO for function-only case, add a direct invoke method that avoids the duplicate work.

        /* compiler's template for arguments available to effect paramters */
        public static readonly FunctionParamRec[] TrackEffectFormulaArgsDefs = new FunctionParamRec[]
        {
            new FunctionParamRec("trackaccent1", DataTypes.eDouble),
            new FunctionParamRec("trackaccent2", DataTypes.eDouble),
            new FunctionParamRec("trackaccent3", DataTypes.eDouble),
            new FunctionParamRec("trackaccent4", DataTypes.eDouble),
            new FunctionParamRec("trackaccent5", DataTypes.eDouble),
            new FunctionParamRec("trackaccent6", DataTypes.eDouble),
            new FunctionParamRec("trackaccent7", DataTypes.eDouble),
            new FunctionParamRec("trackaccent8", DataTypes.eDouble),
            new FunctionParamRec("t", DataTypes.eDouble),
            new FunctionParamRec("x", DataTypes.eDouble),
            new FunctionParamRec("bpm", DataTypes.eDouble),
        };

        /* compiler's template for arguments available to envelope paramters */
        public static readonly FunctionParamRec[] EnvelopeFormulaArgsDefs = new FunctionParamRec[]
        {
            new FunctionParamRec("accent1", DataTypes.eDouble),
            new FunctionParamRec("accent2", DataTypes.eDouble),
            new FunctionParamRec("accent3", DataTypes.eDouble),
            new FunctionParamRec("accent4", DataTypes.eDouble),
            new FunctionParamRec("accent5", DataTypes.eDouble),
            new FunctionParamRec("accent6", DataTypes.eDouble),
            new FunctionParamRec("accent7", DataTypes.eDouble),
            new FunctionParamRec("accent8", DataTypes.eDouble),
            new FunctionParamRec("trackaccent1", DataTypes.eDouble),
            new FunctionParamRec("trackaccent2", DataTypes.eDouble),
            new FunctionParamRec("trackaccent3", DataTypes.eDouble),
            new FunctionParamRec("trackaccent4", DataTypes.eDouble),
            new FunctionParamRec("trackaccent5", DataTypes.eDouble),
            new FunctionParamRec("trackaccent6", DataTypes.eDouble),
            new FunctionParamRec("trackaccent7", DataTypes.eDouble),
            new FunctionParamRec("trackaccent8", DataTypes.eDouble),
            new FunctionParamRec("t", DataTypes.eDouble),
            new FunctionParamRec("x", DataTypes.eDouble),
            new FunctionParamRec("bpm", DataTypes.eDouble),
        };

        /* compiler's template for arguments available to envelope paramters */
        public static readonly FunctionParamRec[] EnvelopeInitFormulaArgsDefs = new FunctionParamRec[]
        {
            new FunctionParamRec("accent1", DataTypes.eDouble),
            new FunctionParamRec("accent2", DataTypes.eDouble),
            new FunctionParamRec("accent3", DataTypes.eDouble),
            new FunctionParamRec("accent4", DataTypes.eDouble),
            new FunctionParamRec("accent5", DataTypes.eDouble),
            new FunctionParamRec("accent6", DataTypes.eDouble),
            new FunctionParamRec("accent7", DataTypes.eDouble),
            new FunctionParamRec("accent8", DataTypes.eDouble),
            new FunctionParamRec("trackaccent1", DataTypes.eDouble),
            new FunctionParamRec("trackaccent2", DataTypes.eDouble),
            new FunctionParamRec("trackaccent3", DataTypes.eDouble),
            new FunctionParamRec("trackaccent4", DataTypes.eDouble),
            new FunctionParamRec("trackaccent5", DataTypes.eDouble),
            new FunctionParamRec("trackaccent6", DataTypes.eDouble),
            new FunctionParamRec("trackaccent7", DataTypes.eDouble),
            new FunctionParamRec("trackaccent8", DataTypes.eDouble),
            new FunctionParamRec("t", DataTypes.eDouble),
            new FunctionParamRec("bpm", DataTypes.eDouble),
        };

        /* initialize the scalar parameter evaluator */
        public static void InitScalarParamEval(
            double SpecifiedValue,
            ref AccentRec SpecifiedAccentModifiers,
            PcodeRec SpecifiedFormula,
            out ScalarParamEvalRec EvalOut)
        {
            EvalOut = new ScalarParamEvalRec();
            EvalOut.SpecifiedValue = SpecifiedValue;
            EvalOut.SpecifiedAccentModifiers = SpecifiedAccentModifiers;
            EvalOut.SpecifiedFormula = SpecifiedFormula;
        }

        /* do a scalar param evaluation */
        public static SynthErrorCodes ScalarParamEval(
            ScalarParamEvalRec Eval,
            ref AccentRec CurrentParameters,
            SynthParamRec SynthParams,
            out double ResultOut)
        {
            ResultOut = 0;

            double Temp = Eval.SpecifiedValue;

            /* ordering is somewhat arbitrary because if user wants it the other way, he can */
            /* always write it into the formula. */

            /* apply accent first */
            Temp = AccentProductAdd(
                Temp,
                ref CurrentParameters,
                ref Eval.SpecifiedAccentModifiers);

            /* compute formula second. */
            if (Eval.SpecifiedFormula != null)
            {
                SynthErrorCodes Error = StaticEval(
                    Temp,
                    Eval.SpecifiedFormula,
                    ref CurrentParameters,
                    SynthParams,
                    out Temp);
                if (Error != SynthErrorCodes.eSynthDone)
                {
                    return Error;
                }
            }

            ResultOut = Temp;

            return SynthErrorCodes.eSynthDone;
        }

        /* do a scalar param evaluation */
        public static SynthErrorCodes StaticEval(
            double X,
            PcodeRec SpecifiedFormula,
            ref AccentRec CurrentParameters,
            SynthParamRec SynthParams,
            out double ResultOut)
        {
            ResultOut = 0;

            int initialCapacity = 1/*retval*/ + 8 + 1/*t*/ + 1/*x*/ + 1/*bpm*/;
            SynthParams.FormulaEvalContext.EmptyParamStackEnsureCapacity(initialCapacity);

            StackElement[] Stack;
            int StackNumElements;
            SynthParams.FormulaEvalContext.GetRawStack(out Stack, out StackNumElements);

            Stack[StackNumElements++].Data.Double = CurrentParameters.Accent0;
            Stack[StackNumElements++].Data.Double = CurrentParameters.Accent1;
            Stack[StackNumElements++].Data.Double = CurrentParameters.Accent2;
            Stack[StackNumElements++].Data.Double = CurrentParameters.Accent3;
            Stack[StackNumElements++].Data.Double = CurrentParameters.Accent4;
            Stack[StackNumElements++].Data.Double = CurrentParameters.Accent5;
            Stack[StackNumElements++].Data.Double = CurrentParameters.Accent6;
            Stack[StackNumElements++].Data.Double = CurrentParameters.Accent7;

            Stack[StackNumElements++].Data.Double = SynthParams.dElapsedTimeInSeconds; /* t */

            Stack[StackNumElements++].Data.Double = X; /* x */

            Stack[StackNumElements++].Data.Double = SynthParams.dCurrentBeatsPerMinute; /* bpm */

            StackNumElements++; /* return address placeholder */

            SynthParams.FormulaEvalContext.UpdateRawStack(Stack, StackNumElements);

            EvalErrInfoRec ErrorInfo;
            EvalErrors Error = PcodeSystem.EvaluatePcode(
                SynthParams.FormulaEvalContext,
                SpecifiedFormula,
                SynthParams.perTrack.CodeCenter,
                out ErrorInfo,
                PcodeExternsNull.Default,
                ref SynthParams.pcodeThreadContext);
            if (Error != EvalErrors.eEvalNoError)
            {
                SynthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUserParamFunctionEvalError;
                SynthParams.ErrorInfo.UserEvalErrorCode = Error;
                SynthParams.ErrorInfo.UserEvalErrorInfo = ErrorInfo;
                return SynthErrorCodes.eSynthErrorEx;
            }
            Debug.Assert(SynthParams.FormulaEvalContext.GetStackNumElements() == initialCapacity); // args - retaddr + return value

            ResultOut = SynthParams.FormulaEvalContext.GetStackDouble(initialCapacity - 1);

            return SynthErrorCodes.eSynthDone;
        }

        /* do a scalar param evaluation, as used by envelope generator */
        public static SynthErrorCodes EnvelopeParamEval(
            double X,
            PcodeRec Formula,
            ref AccentRec NoteAccents,
            ref AccentRec TrackAccents,
            SynthParamRec SynthParams,
            out double ResultOut)
        {
            ResultOut = 0;

            int initialCapacity = 1/*retval*/ + 8 + 8 + 1/*t*/ + 1/*x*/ + 1/*bpm*/;
            SynthParams.FormulaEvalContext.EmptyParamStackEnsureCapacity(initialCapacity);

            StackElement[] Stack;
            int StackNumElements;
            SynthParams.FormulaEvalContext.GetRawStack(out Stack, out StackNumElements);

            Stack[StackNumElements++].Data.Double = NoteAccents.Accent0;
            Stack[StackNumElements++].Data.Double = NoteAccents.Accent1;
            Stack[StackNumElements++].Data.Double = NoteAccents.Accent2;
            Stack[StackNumElements++].Data.Double = NoteAccents.Accent3;
            Stack[StackNumElements++].Data.Double = NoteAccents.Accent4;
            Stack[StackNumElements++].Data.Double = NoteAccents.Accent5;
            Stack[StackNumElements++].Data.Double = NoteAccents.Accent6;
            Stack[StackNumElements++].Data.Double = NoteAccents.Accent7;

            Stack[StackNumElements++].Data.Double = TrackAccents.Accent0;
            Stack[StackNumElements++].Data.Double = TrackAccents.Accent1;
            Stack[StackNumElements++].Data.Double = TrackAccents.Accent2;
            Stack[StackNumElements++].Data.Double = TrackAccents.Accent3;
            Stack[StackNumElements++].Data.Double = TrackAccents.Accent4;
            Stack[StackNumElements++].Data.Double = TrackAccents.Accent5;
            Stack[StackNumElements++].Data.Double = TrackAccents.Accent6;
            Stack[StackNumElements++].Data.Double = TrackAccents.Accent7;

            Stack[StackNumElements++].Data.Double = SynthParams.dElapsedTimeInSeconds; /* t */

            Stack[StackNumElements++].Data.Double = X; /* x */

            Stack[StackNumElements++].Data.Double = SynthParams.dCurrentBeatsPerMinute; /* bpm */

            StackNumElements++; /* return address placeholder */

            SynthParams.FormulaEvalContext.UpdateRawStack(Stack, StackNumElements);

            EvalErrInfoRec ErrorInfo;
            EvalErrors Error = PcodeSystem.EvaluatePcode(
                SynthParams.FormulaEvalContext,
                Formula,
                SynthParams.perTrack.CodeCenter,
                out ErrorInfo,
                PcodeExternsNull.Default,
                ref SynthParams.pcodeThreadContext);
            if (Error != EvalErrors.eEvalNoError)
            {
                SynthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUserParamFunctionEvalError;
                SynthParams.ErrorInfo.UserEvalErrorCode = Error;
                SynthParams.ErrorInfo.UserEvalErrorInfo = ErrorInfo;
                return SynthErrorCodes.eSynthErrorEx;
            }
            Debug.Assert(SynthParams.FormulaEvalContext.GetStackNumElements() == initialCapacity); // args - retaddr + return value

            /* get result */
            ResultOut = SynthParams.FormulaEvalContext.GetStackDouble(initialCapacity - 1);

            return SynthErrorCodes.eSynthDone;
        }

        // this non-X-param evaluator is used for envelope segment initializers
        public static SynthErrorCodes EnvelopeInitParamEval(
            PcodeRec Formula,
            ref AccentRec NoteAccents,
            ref AccentRec TrackAccents,
            SynthParamRec SynthParams,
            out double ResultOut)
        {
            ResultOut = 0;

            int initialCapacity = 1/*retval*/ + 8 + 8 + 1/*t*/ + 1/*bpm*/;
            SynthParams.FormulaEvalContext.EmptyParamStackEnsureCapacity(initialCapacity);

            StackElement[] Stack;
            int StackNumElements;
            SynthParams.FormulaEvalContext.GetRawStack(out Stack, out StackNumElements);

            Stack[StackNumElements++].Data.Double = NoteAccents.Accent0;
            Stack[StackNumElements++].Data.Double = NoteAccents.Accent1;
            Stack[StackNumElements++].Data.Double = NoteAccents.Accent2;
            Stack[StackNumElements++].Data.Double = NoteAccents.Accent3;
            Stack[StackNumElements++].Data.Double = NoteAccents.Accent4;
            Stack[StackNumElements++].Data.Double = NoteAccents.Accent5;
            Stack[StackNumElements++].Data.Double = NoteAccents.Accent6;
            Stack[StackNumElements++].Data.Double = NoteAccents.Accent7;

            Stack[StackNumElements++].Data.Double = TrackAccents.Accent0;
            Stack[StackNumElements++].Data.Double = TrackAccents.Accent1;
            Stack[StackNumElements++].Data.Double = TrackAccents.Accent2;
            Stack[StackNumElements++].Data.Double = TrackAccents.Accent3;
            Stack[StackNumElements++].Data.Double = TrackAccents.Accent4;
            Stack[StackNumElements++].Data.Double = TrackAccents.Accent5;
            Stack[StackNumElements++].Data.Double = TrackAccents.Accent6;
            Stack[StackNumElements++].Data.Double = TrackAccents.Accent7;

            Stack[StackNumElements++].Data.Double = SynthParams.dElapsedTimeInSeconds; /* t */

            Stack[StackNumElements++].Data.Double = SynthParams.dCurrentBeatsPerMinute; /* bpm */

            StackNumElements++; /* return address placeholder */

            SynthParams.FormulaEvalContext.UpdateRawStack(Stack, StackNumElements);

            EvalErrInfoRec ErrorInfo;
            EvalErrors Error = PcodeSystem.EvaluatePcode(
                SynthParams.FormulaEvalContext,
                Formula,
                SynthParams.perTrack.CodeCenter,
                out ErrorInfo,
                PcodeExternsNull.Default,
                ref SynthParams.pcodeThreadContext);
            if (Error != EvalErrors.eEvalNoError)
            {
                SynthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUserParamFunctionEvalError;
                SynthParams.ErrorInfo.UserEvalErrorCode = Error;
                SynthParams.ErrorInfo.UserEvalErrorInfo = ErrorInfo;
                return SynthErrorCodes.eSynthErrorEx;
            }
            Debug.Assert(SynthParams.FormulaEvalContext.GetStackNumElements() == initialCapacity); // args - retaddr + return value

            /* get result */
            ResultOut = SynthParams.FormulaEvalContext.GetStackDouble(initialCapacity - 1);

            return SynthErrorCodes.eSynthDone;
        }
    }
}
