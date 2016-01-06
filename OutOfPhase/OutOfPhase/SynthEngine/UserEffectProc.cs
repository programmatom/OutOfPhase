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
        public struct UserEffectParamRec_Track
        {
            public ScalarParamEvalRec Eval;
        }

        public struct UserEffectParamRec_Osc
        {
            public EvalEnvelopeRec Envelope;
            public LFOGenRec LFO;
        }

        public class UserEffectProcRec : ITrackEffect, IOscillatorEffect
        {
            /* initialization formula.  null = not specified. */
            public PcodeRec InitFunc;
            /* param update formula.  null = not specified. */
            public PcodeRec UpdateFunc;
            /* data processing formula. */
            public PcodeRec DataFunc;

            /* parameter vector */
            public int cParams;
            /* vector of parameter evaluations */
            // exactly one of these is non-null
            public UserEffectParamRec_Track[] rgParams_Track;
            public UserEffectParamRec_Osc[] rgParams_Osc;
            /* vector of parameter eval results */
            public double[] rgParamResults;

            /* user's state */
            public ArrayHandleDouble pLeftState;
            public ArrayHandleDouble pRightState;
            public ArrayHandleFloat fLeftState;
            public ArrayHandleFloat fRightState;

            // reused workspaces
            public float[] leftWorkspace;
            public ArrayHandleFloat leftWorkspaceHandle;
            public float[] rightWorkspace;
            public ArrayHandleFloat rightWorkspaceHandle;


            /* shared initialization */
            private static SynthErrorCodes UserEffectSharedInit(
                UserEffectProcRec Proc,
                UserEffectSpecRec Template,
                SynthParamRec SynthParams)
            {
                string FuncName;
                FuncCodeRec FuncCode;

                /* init func */
                FuncName = GetUserEffectSpecInitFuncName(Template);
                if (FuncName != null)
                {
                    FuncCode = SynthParams.CodeCenter.ObtainFunctionHandle(FuncName);
                    if (FuncCode == null)
                    {
                        // Function missing; should have been found by CheckUnreferencedThings
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
                    if (!UserEffectValidateTypeInit(FuncCode))
                    {
                        // Function type mismatch; should have been found by CheckUnreferencedThings
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
                    Proc.InitFunc = FuncCode.GetFunctionPcode();
                }

                /* update func */
                FuncName = GetUserEffectSpecArgUpdateFuncName(Template);
                if (FuncName != null)
                {
                    FuncCode = SynthParams.CodeCenter.ObtainFunctionHandle(FuncName);
                    if (FuncCode == null)
                    {
                        // Function missing; should have been found by CheckUnreferencedThings
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
                    if (!UserEffectValidateTypeUpdate(FuncCode, GetUserEffectSpecParamCount(Template)))
                    {
                        // Function type mismatch; should have been found by CheckUnreferencedThings
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
                    Proc.UpdateFunc = FuncCode.GetFunctionPcode();
                }

                /* data func */
                FuncName = GetUserEffectSpecProcessDataFuncName(Template);
                FuncCode = SynthParams.CodeCenter.ObtainFunctionHandle(FuncName);
                if (FuncCode == null)
                {
                    // Function missing; should have been found by CheckUnreferencedThings
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
                if (!UserEffectValidateTypeData(FuncCode))
                {
                    // Function type mismatch; should have been found by CheckUnreferencedThings
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
                Proc.DataFunc = FuncCode.GetFunctionPcode();

                Proc.cParams = GetUserEffectSpecParamCount(Template);
                Proc.rgParamResults = new double[Proc.cParams];

                // create state objects
                Proc.pLeftState = new ArrayHandleDouble(new double[0]);
                Proc.pRightState = new ArrayHandleDouble(new double[0]);
                Proc.fLeftState = new ArrayHandleFloat(new float[0]);
                Proc.fRightState = new ArrayHandleFloat(new float[0]);
                /* initialize user state */
                if (Proc.InitFunc != null)
                {
                    SynthParams.FormulaEvalContext.EmptyParamStackEnsureCapacity(
                        1/*retval*/ + 1/*dleftstate*/ + 1/*drightstate*/ + 1/*fleftstate*/ + 1/*frightstate*/ +
                        1/*t*/ + 1/*bpm*/ + 1/*sr*/ + 1/*retaddr*/);

                    StackElement[] StackBase;
                    int StackNumElements;
                    SynthParams.FormulaEvalContext.GetRawStack(out StackBase, out StackNumElements);

                    StackBase[StackNumElements++].reference.arrayHandleDouble = Proc.pLeftState;
                    StackBase[StackNumElements++].reference.arrayHandleDouble = Proc.pRightState;

                    StackBase[StackNumElements++].reference.arrayHandleFloat = Proc.fLeftState;
                    StackBase[StackNumElements++].reference.arrayHandleFloat = Proc.fRightState;

                    StackBase[StackNumElements++].Data.Double = SynthParams.dElapsedTimeInSeconds; /* t */

                    StackBase[StackNumElements++].Data.Double = SynthParams.dCurrentBeatsPerMinute; /* bpm */

                    StackBase[StackNumElements++].Data.Double = SynthParams.dSamplingRate; /* sr */

                    StackNumElements++; /* return address placeholder */

                    SynthParams.FormulaEvalContext.UpdateRawStack(StackBase, StackNumElements);

                    EvalErrInfoRec ErrorInfo;
                    EvalErrors Error = PcodeSystem.EvaluatePcode(
                        SynthParams.FormulaEvalContext,
                        Proc.InitFunc,
                        SynthParams.CodeCenter,
                        out ErrorInfo,
                        null/*EvaluateContext*/,
                        ref SynthParams.pcodeThreadContext);
                    if (Error != EvalErrors.eEvalNoError)
                    {
                        SynthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUserEffectFunctionEvalError;
                        SynthParams.ErrorInfo.userEvalErrorCode = Error;
                        SynthParams.ErrorInfo.userEvalErrorInfo = ErrorInfo;
                        return SynthErrorCodes.eSynthErrorEx;
                    }
                    Debug.Assert(SynthParams.FormulaEvalContext.GetStackNumElements() == 1); // return value

                    SynthParams.FormulaEvalContext.Clear();
                }

                Proc.leftWorkspace = new float[SynthParams.nAllocatedPointsOneChannel];
                Proc.leftWorkspaceHandle = new ArrayHandleFloat(null);
                Proc.rightWorkspace = new float[SynthParams.nAllocatedPointsOneChannel];
                Proc.rightWorkspaceHandle = new ArrayHandleFloat(null);

                return SynthErrorCodes.eSynthDone;
            }

            /* create a new track user effect processor */
            public static SynthErrorCodes NewTrackUserEffectProc(
                UserEffectSpecRec Template,
                SynthParamRec SynthParams,
                out UserEffectProcRec effectOut)
            {
                effectOut = null;

                /* allocate structure */
                UserEffectProcRec Proc = new UserEffectProcRec();

                SynthErrorCodes error = UserEffectSharedInit(Proc, Template, SynthParams);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                Proc.rgParams_Track = new UserEffectParamRec_Track[Proc.cParams];

                /* initialize argument evaluators */
                for (int i = 0; i < Proc.cParams; i += 1)
                {
                    GetUserEffectSpecParamAgg(
                        Template,
                        i,
                        out Proc.rgParams_Track[i].Eval);
                }

                effectOut = Proc;
                return SynthErrorCodes.eSynthDone;
            }

            /* create a new oscillator user effect processor */
            public static SynthErrorCodes NewOscUserEffectProc(
                UserEffectSpecRec Template,
                ref AccentRec Accents,
                double HurryUp,
                double InitialFrequency,
                double FreqForMultisampling,
                out int PreOriginTimeOut,
                PlayTrackInfoRec TrackInfo,
                SynthParamRec SynthParams,
                out UserEffectProcRec effectOut)
            {
                effectOut = null;
                PreOriginTimeOut = 0;

                /* allocate structure */
                UserEffectProcRec Proc = new UserEffectProcRec();

                SynthErrorCodes error = UserEffectSharedInit(Proc, Template, SynthParams);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                Proc.rgParams_Osc = new UserEffectParamRec_Osc[Proc.cParams];

                /* initialize argument evaluators */
                int MaxPreOrigin = 0;
                for (int i = 0; i < Proc.cParams; i += 1)
                {
                    int OnePreOrigin;

                    Proc.rgParams_Osc[i].Envelope = NewEnvelopeStateRecord(
                        GetUserEffectSpecParamEnvelope(Template, i),
                        ref Accents,
                        InitialFrequency,
                        1,
                        HurryUp,
                        out OnePreOrigin,
                        _PlayTrackParamGetter,
                        TrackInfo,
                        SynthParams);
                    if (OnePreOrigin > MaxPreOrigin)
                    {
                        MaxPreOrigin = OnePreOrigin;
                    }

                    Proc.rgParams_Osc[i].LFO = NewLFOGenerator(
                        GetUserEffectSpecParamLFO(Template, i),
                        out OnePreOrigin,
                        ref Accents,
                        InitialFrequency,
                        HurryUp,
                        1,
                        1,
                        FreqForMultisampling,
                        _PlayTrackParamGetter,
                        TrackInfo,
                        SynthParams);
                    if (OnePreOrigin > MaxPreOrigin)
                    {
                        MaxPreOrigin = OnePreOrigin;
                    }
                }

                PreOriginTimeOut = MaxPreOrigin;

                effectOut = Proc;
                return SynthErrorCodes.eSynthDone;
            }

            /* fix up the origin time so that envelopes start at the proper times */
            public void OscFixEnvelopeOrigins(
                int ActualPreOriginTime)
            {
                for (int i = 0; i < this.cParams; i += 1)
                {
                    EnvelopeStateFixUpInitialDelay(
                        this.rgParams_Osc[i].Envelope,
                        ActualPreOriginTime);
                    LFOGeneratorFixEnvelopeOrigins(
                        this.rgParams_Osc[i].LFO,
                        ActualPreOriginTime);
                }
            }

            /* helper to execute update function */
            /* assumes rgParamResults has been filled in */
            private static SynthErrorCodes UserEffectUpdateFunctionHelper(
                UserEffectProcRec Proc,
                SynthParamRec SynthParams)
            {
                SynthParams.FormulaEvalContext.EmptyParamStackEnsureCapacity(
                    1/*retval*/ + 1/*leftstate*/ + 1/*rightstate*/ + 1/*fleftstate*/ + 1/*frightstate*/ +
                    1/*t*/ + 1/*bpm*/ + 1/*sr*/ + Proc.cParams + 1/*retaddr*/);

                int StackNumElements;
                StackElement[] StackBase;
                SynthParams.FormulaEvalContext.GetRawStack(out StackBase, out StackNumElements);

                StackBase[StackNumElements++].reference.arrayHandleDouble = Proc.pLeftState;
                StackBase[StackNumElements++].reference.arrayHandleDouble = Proc.pRightState;

                StackBase[StackNumElements++].reference.arrayHandleFloat = Proc.fLeftState;
                StackBase[StackNumElements++].reference.arrayHandleFloat = Proc.fRightState;

                StackBase[StackNumElements++].Data.Double = SynthParams.dElapsedTimeInSeconds; /* t */

                StackBase[StackNumElements++].Data.Double = SynthParams.dCurrentBeatsPerMinute; /* bpm */

                StackBase[StackNumElements++].Data.Double = SynthParams.dSamplingRate; /* sr */

                for (int i = 0; i < Proc.cParams; i += 1)
                {
                    StackBase[StackNumElements++].Data.Double = Proc.rgParamResults[i];
                }

                StackNumElements++; /* return address placeholder */

                SynthParams.FormulaEvalContext.UpdateRawStack(StackBase, StackNumElements);

                EvalErrInfoRec ErrorInfo;
                EvalErrors Error = PcodeSystem.EvaluatePcode(
                    SynthParams.FormulaEvalContext,
                    Proc.UpdateFunc,
                    SynthParams.CodeCenter,
                    out ErrorInfo,
                    null/*EvaluateContext*/,
                    ref SynthParams.pcodeThreadContext);
                if (Error != EvalErrors.eEvalNoError)
                {
                    SynthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUserEffectFunctionEvalError;
                    SynthParams.ErrorInfo.userEvalErrorCode = Error;
                    SynthParams.ErrorInfo.userEvalErrorInfo = ErrorInfo;
                    return SynthErrorCodes.eSynthErrorEx;
                }
                Debug.Assert(SynthParams.FormulaEvalContext.GetStackNumElements() == 1); // return value

                SynthParams.FormulaEvalContext.Clear();

                return SynthErrorCodes.eSynthDone;
            }

            /* update user effect processor state with accent information */
            public void TrackUpdateState(
                ref AccentRec Accents,
                SynthParamRec SynthParams)
            {
                if (this.UpdateFunc != null)
                {
                    /* compute additional arguments to the update function */
                    for (int i = 0; i < this.cParams; i += 1)
                    {
                        ScalarParamEval(
                            this.rgParams_Track[i].Eval,
                            ref Accents,
                            SynthParams,
                            out this.rgParamResults[i]);
                    }

                    UserEffectUpdateFunctionHelper(this, SynthParams);
                }
            }

            /* update user effect processor state with accent information */
            public void OscUpdateEnvelopes(
                double OscillatorFrequency,
                SynthParamRec SynthParams)
            {
                if (this.UpdateFunc != null)
                {
                    /* compute additional arguments to the update function */
                    for (int i = 0; i < this.cParams; i += 1)
                    {
                        this.rgParamResults[i] = LFOGenUpdateCycle(
                            this.rgParams_Osc[i].LFO,
                            EnvelopeUpdate(
                                this.rgParams_Osc[i].Envelope,
                                OscillatorFrequency,
                                SynthParams),
                            OscillatorFrequency,
                            SynthParams);
                    }

                    UserEffectUpdateFunctionHelper(this, SynthParams);
                }
            }

            /* create key-up impulse */
            public void OscKeyUpSustain1()
            {
                if (this.UpdateFunc != null)
                {
                    /* compute additional arguments to the update function */
                    for (int i = 0; i < this.cParams; i += 1)
                    {
                        EnvelopeKeyUpSustain1(
                            this.rgParams_Osc[i].Envelope);
                        LFOGeneratorKeyUpSustain1(
                            this.rgParams_Osc[i].LFO);
                    }
                }
            }

            /* create key-up impulse */
            public void OscKeyUpSustain2()
            {
                if (this.UpdateFunc != null)
                {
                    /* compute additional arguments to the update function */
                    for (int i = 0; i < this.cParams; i += 1)
                    {
                        EnvelopeKeyUpSustain2(
                            this.rgParams_Osc[i].Envelope);
                        LFOGeneratorKeyUpSustain2(
                            this.rgParams_Osc[i].LFO);
                    }
                }
            }

            /* create key-up impulse */
            public void OscKeyUpSustain3()
            {
                if (this.UpdateFunc != null)
                {
                    /* compute additional arguments to the update function */
                    for (int i = 0; i < this.cParams; i += 1)
                    {
                        EnvelopeKeyUpSustain3(
                            this.rgParams_Osc[i].Envelope);
                        LFOGeneratorKeyUpSustain3(
                            this.rgParams_Osc[i].LFO);
                    }
                }
            }

            /* retrigger effect envelopes from the origin point */
            public void OscRetriggerEnvelopes(
                ref AccentRec NewAccents,
                double NewHurryUp,
                double NewInitialFrequency,
                bool ActuallyRetrigger,
                SynthParamRec SynthParams)
            {
                if (this.UpdateFunc != null)
                {
                    /* compute additional arguments to the update function */
                    for (int i = 0; i < this.cParams; i += 1)
                    {
                        EnvelopeRetriggerFromOrigin(
                            this.rgParams_Osc[i].Envelope,
                            ref NewAccents,
                            NewInitialFrequency,
                            1,
                            NewHurryUp,
                            ActuallyRetrigger,
                            SynthParams);
                        LFOGeneratorRetriggerFromOrigin(
                            this.rgParams_Osc[i].LFO,
                            ref NewAccents,
                            NewInitialFrequency,
                            NewHurryUp,
                            1,
                            1,
                            ActuallyRetrigger,
                            SynthParams);
                    }
                }
            }

            /* apply user effect processing to some stuff */
            public SynthErrorCodes Apply(
                float[] workspace,
                int lOffset,
                int rOffset,
                int nActualFrames,
                SynthParamRec SynthParams)
            {
                // reinit each ccycle because user code can reallocate or remove them
                leftWorkspaceHandle.floats = leftWorkspace;
                rightWorkspaceHandle.floats = rightWorkspace;

                Array.Copy(
                    workspace,
                    lOffset,
                    leftWorkspaceHandle.floats, // not vector-aligned
                    0,
                    nActualFrames);
                Array.Copy(
                    workspace,
                    rOffset,
                    rightWorkspaceHandle.floats, // not vector-aligned
                    0,
                    nActualFrames);

                SynthParams.FormulaEvalContext.EmptyParamStackEnsureCapacity(
                    1/*retval*/ + 1/*leftstate*/ + 1/*rightstate*/ + 1/*fleftstate*/ + 1/*frightstate*/ +
                    1/*leftdata*/ + 1/*rightdata*/ + 1/*count*/ + 1/*sr*/ + 1/*retaddr*/);

                int StackNumElements;
                StackElement[] StackBase;
                SynthParams.FormulaEvalContext.GetRawStack(out StackBase, out StackNumElements);

                StackBase[StackNumElements++].reference.arrayHandleDouble = pLeftState;
                StackBase[StackNumElements++].reference.arrayHandleDouble = pRightState;

                StackBase[StackNumElements++].reference.arrayHandleFloat = fLeftState;
                StackBase[StackNumElements++].reference.arrayHandleFloat = fRightState;

                StackBase[StackNumElements++].reference.arrayHandleFloat = leftWorkspaceHandle;
                StackBase[StackNumElements++].reference.arrayHandleFloat = rightWorkspaceHandle;

                StackBase[StackNumElements++].Data.Integer = nActualFrames;

                StackBase[StackNumElements++].Data.Double = SynthParams.dSamplingRate;

                StackNumElements++; /* return address placeholder */

                SynthParams.FormulaEvalContext.UpdateRawStack(StackBase, StackNumElements);

                EvalErrInfoRec ErrorInfo;
                EvalErrors Error = PcodeSystem.EvaluatePcode(
                    SynthParams.FormulaEvalContext,
                    this.DataFunc,
                    SynthParams.CodeCenter,
                    out ErrorInfo,
                    null/*EvaluateContext*/,
                    ref SynthParams.pcodeThreadContext);
                if (Error != EvalErrors.eEvalNoError)
                {
                    SynthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUserEffectFunctionEvalError;
                    SynthParams.ErrorInfo.userEvalErrorCode = Error;
                    SynthParams.ErrorInfo.userEvalErrorInfo = ErrorInfo;
                    return SynthErrorCodes.eSynthErrorEx;
                }
                Debug.Assert(SynthParams.FormulaEvalContext.GetStackNumElements() == 1); // return value

                SynthParams.FormulaEvalContext.Clear();

                if ((leftWorkspaceHandle.floats != null) && (leftWorkspaceHandle.floats.Length >= nActualFrames)
                    && (rightWorkspaceHandle.floats != null) && (rightWorkspaceHandle.floats.Length >= nActualFrames))
                {
                    // remove NaN/Infinity - prevent user effect misbehavior from taking rest of system down
                    FloatVectorCopyReplaceNaNInf(
                        leftWorkspaceHandle.floats, // unaligned permitted
                        0,
                        workspace,
                        lOffset,
                        nActualFrames);
                    FloatVectorCopyReplaceNaNInf(
                        rightWorkspaceHandle.floats, // unaligned permitted
                        0,
                        workspace,
                        rOffset,
                        nActualFrames);
                }

                return SynthErrorCodes.eSynthDone;
            }

            public void Finalize(
                SynthParamRec SynthParams,
                bool writeOutputLogs)
            {
            }
        }
    }
}
