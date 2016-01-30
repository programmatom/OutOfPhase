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
            /* data processing formula. */
            public PcodeRec DataFunc;

            // Some user-provided effects might be parametrically unstable and exhibit bad behavior when sampling
            // rates are well out of expected range. An example (which motivated this feature) is the "TB-303 VCF" by
            // Hans Mikelson (implemented in csound), wherein a reasonably-behaved score at 44.1kHz fails completely
            // at 88.2kHz. This option allows the use of the original parameter range with oversampling for the rest
            // of the score, at a cost in terms of aliasing in the audio chain containing this effect processor.
            public bool disableOversampling;
            public float lastLeft, lastRight;

            /* parameter vector */
            public int paramCount;
            /* vector of parameter evaluations */
            // exactly one of these is non-null
            public UserEffectParamRec_Track[] params_Track;
            public UserEffectParamRec_Osc[] params_Osc;
            /* vector of parameter eval results */
            public double[] paramResultsPrevious;
            public double[] paramResults;

            // reused workspaces for staging sample data in/out during
            public float[] leftWorkspace;
            public ArrayHandleFloat leftWorkspaceHandle;
            public float[] rightWorkspace;
            public ArrayHandleFloat rightWorkspaceHandle;

            // user-request state arrays
            public ArrayHandle[] userState;

            // smoothing workspaces
            public SmoothingEntry[] smoothingBuffers;

            public struct SmoothingEntry
            {
                public ArrayHandleFloat arrayHandle;
                public float[] vector;
                public bool degraded;
            }


            /* shared initialization */
            private static SynthErrorCodes UserEffectSharedInit(
                UserEffectProcRec Proc,
                UserEffectSpecRec Template,
                SynthParamRec SynthParams)
            {
                Proc.disableOversampling = GetUserEffectSpecNoOversampling(Template);

                double sr = SynthParams.dSamplingRate;
                if (!((SynthParams.iOversampling == 1) || !Proc.disableOversampling))
                {
                    sr /= SynthParams.iOversampling;
                }

                /* init func */
                {
                    string FuncName = GetUserEffectSpecInitFuncName(Template);
                    if (FuncName != null)
                    {
                        FuncCodeRec FuncCode = SynthParams.CodeCenter.ObtainFunctionHandle(FuncName);
                        if (FuncCode == null)
                        {
                            // Function missing; should have been found by CheckUnreferencedThings
                            Debug.Assert(false);
                            throw new ArgumentException();
                        }

                        DataTypes[] argsTypes;
                        DataTypes returnType;
                        UserEffectGetInitSignature(Template, out argsTypes, out returnType);
                        FunctionSignature expectedSignature = new FunctionSignature(argsTypes, returnType);
                        FunctionSignature actualSignature = new FunctionSignature(
                            FuncCode.GetFunctionParameterTypeList(),
                            FuncCode.GetFunctionReturnType());
                        if (!FunctionSignature.Equals(expectedSignature, actualSignature))
                        {
                            // Function type mismatch; should have been found by CheckUnreferencedThings
                            Debug.Assert(false);
                            throw new ArgumentException();
                        }

                        Proc.InitFunc = FuncCode.GetFunctionPcode();
                    }
                }

                /* data func */
                {
                    DataTypes[] argsTypes;
                    DataTypes returnType;
                    UserEffectGetDataSignature(Template, out argsTypes, out returnType);
                    FunctionSignature expectedSignature = new FunctionSignature(argsTypes, returnType);

                    foreach (string FuncName in GetUserEffectSpecProcessDataFuncNames(Template))
                    {
                        FuncCodeRec FuncCode = SynthParams.CodeCenter.ObtainFunctionHandle(FuncName);
                        if (FuncCode == null)
                        {
                            // Function missing; should have been found by CheckUnreferencedThings
                            Debug.Assert(false);
                            throw new ArgumentException();
                        }

                        FunctionSignature actualSignature = new FunctionSignature(
                            FuncCode.GetFunctionParameterTypeList(),
                            FuncCode.GetFunctionReturnType());
                        if (FunctionSignature.Equals(expectedSignature, actualSignature))
                        {
                            Proc.DataFunc = FuncCode.GetFunctionPcode();
                            break;
                        }
                    }
                    if (Proc.DataFunc == null)
                    {
                        // None matched -- should have been found by CheckUnreferencedThings
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
                }

                Proc.paramCount = GetUserEffectSpecParamCount(Template);
                Proc.paramResults = new double[Proc.paramCount];
                Proc.paramResultsPrevious = new double[Proc.paramCount];

                // create state objects
                DataTypes[] stateTypes = UserEffectGetWorkspaceTypes(Template);
                Proc.userState = new ArrayHandle[stateTypes.Length];
                for (int i = 0; i < Proc.userState.Length; i++)
                {
                    switch (stateTypes[i])
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();
                        case DataTypes.eArrayOfInteger:
                            Proc.userState[i] = new ArrayHandleInt32(new int[0]);
                            break;
                        case DataTypes.eArrayOfFloat:
                            Proc.userState[i] = new ArrayHandleFloat(new float[0]);
                            break;
                        case DataTypes.eArrayOfDouble:
                            Proc.userState[i] = new ArrayHandleDouble(new double[0]);
                            break;
                    }
                }

                Proc.smoothingBuffers = new SmoothingEntry[Template.Items.Length];
                for (int i = 0; i < Template.Items.Length; i++)
                {
                    if (Template.Items[i].Smoothed)
                    {
                        float[] vector = new float[SynthParams.nAllocatedPointsOneChannel];
                        Proc.smoothingBuffers[i].vector = vector;
                        Proc.smoothingBuffers[i].arrayHandle = new ArrayHandleFloat(vector);
                    }
                }

                /* initialize user state */
                if (Proc.InitFunc != null)
                {
                    int argCount = 1/*retval*/
                        + 1/*t*/
                        + 1/*bpm*/
                        + 1/*samplingRate*/
                        + 1/*maxSampleCount*/
                        + Proc.userState.Length/*user state arrays*/
                        + 1/*retaddr*/;
                    SynthParams.FormulaEvalContext.EmptyParamStackEnsureCapacity(argCount);

                    StackElement[] StackBase;
                    int StackNumElements;
                    SynthParams.FormulaEvalContext.GetRawStack(out StackBase, out StackNumElements);

                    StackBase[StackNumElements++].Data.Double = SynthParams.dElapsedTimeInSeconds; /* t */

                    StackBase[StackNumElements++].Data.Double = SynthParams.dCurrentBeatsPerMinute; /* bpm */

                    StackBase[StackNumElements++].Data.Double = sr; /* samplingRate */

                    StackBase[StackNumElements++].Data.Integer = SynthParams.nAllocatedPointsOneChannel; /* maxSampleCount */

                    for (int i = 0; i < Proc.userState.Length; i++)
                    {
                        StackBase[StackNumElements++].reference.arrayHandleGeneric = Proc.userState[i]; // user state
                    }

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
                        SynthParams.ErrorInfo.UserEvalErrorCode = Error;
                        SynthParams.ErrorInfo.UserEvalErrorInfo = ErrorInfo;
                        return SynthErrorCodes.eSynthErrorEx;
                    }
                    Debug.Assert(SynthParams.FormulaEvalContext.GetStackNumElements() == 1); // return value

                    SynthParams.FormulaEvalContext.Clear();
                }

                // initialize sample data in/out staging areas
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

                Proc.params_Track = new UserEffectParamRec_Track[Proc.paramCount];

                /* initialize argument evaluators */
                for (int i = 0; i < Proc.paramCount; i += 1)
                {
                    GetUserEffectSpecParamAgg(
                        Template,
                        i,
                        out Proc.params_Track[i].Eval);
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

                Proc.params_Osc = new UserEffectParamRec_Osc[Proc.paramCount];

                /* initialize argument evaluators */
                int MaxPreOrigin = 0;
                for (int i = 0; i < Proc.paramCount; i += 1)
                {
                    int OnePreOrigin;

                    Proc.params_Osc[i].Envelope = NewEnvelopeStateRecord(
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

                    Proc.params_Osc[i].LFO = NewLFOGenerator(
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

                for (int i = 0; i < Template.Items.Length; i++)
                {
                    if (Template.Items[i].Smoothed)
                    {
                        if (IsLFOSampleAndHold(Proc.params_Osc[i].LFO))
                        {
                            // degrade sample & hold, since smoothing is probably not what was intended
                            Proc.smoothingBuffers[i].degraded = true;
                        }
                    }
                }

                effectOut = Proc;
                return SynthErrorCodes.eSynthDone;
            }

            /* fix up the origin time so that envelopes start at the proper times */
            public void OscFixEnvelopeOrigins(
                int ActualPreOriginTime)
            {
                for (int i = 0; i < this.paramCount; i += 1)
                {
                    EnvelopeStateFixUpInitialDelay(
                        this.params_Osc[i].Envelope,
                        ActualPreOriginTime);
                    LFOGeneratorFixEnvelopeOrigins(
                        this.params_Osc[i].LFO,
                        ActualPreOriginTime);
                }
            }

            /* update user effect processor state with accent information */
            public SynthErrorCodes TrackUpdateState(
                ref AccentRec Accents,
                SynthParamRec SynthParams)
            {
                double[] paramResultsTemp = this.paramResultsPrevious;
                this.paramResultsPrevious = this.paramResults;
                this.paramResults = paramResultsTemp;

                for (int i = 0; i < this.paramCount; i += 1)
                {
                    SynthErrorCodes error = ScalarParamEval(
                        this.params_Track[i].Eval,
                        ref Accents,
                        SynthParams,
                        out this.paramResults[i]);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }
                }

                return SynthErrorCodes.eSynthDone;
            }

            /* update user effect processor state with accent information */
            public SynthErrorCodes OscUpdateEnvelopes(
                double OscillatorFrequency,
                SynthParamRec SynthParams)
            {
                double[] paramResultsTemp = this.paramResultsPrevious;
                this.paramResultsPrevious = this.paramResults;
                this.paramResults = paramResultsTemp;

                for (int i = 0; i < this.paramCount; i += 1)
                {
                    SynthErrorCodes error = SynthErrorCodes.eSynthDone;
                    this.paramResults[i] = LFOGenUpdateCycle(
                        this.params_Osc[i].LFO,
                        EnvelopeUpdate(
                            this.params_Osc[i].Envelope,
                            OscillatorFrequency,
                            SynthParams,
                            ref error),
                        OscillatorFrequency,
                        SynthParams,
                        ref error);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }
                }

                return SynthErrorCodes.eSynthDone;
            }

            /* create key-up impulse */
            public void OscKeyUpSustain1()
            {
                for (int i = 0; i < this.paramCount; i += 1)
                {
                    EnvelopeKeyUpSustain1(
                        this.params_Osc[i].Envelope);
                    LFOGeneratorKeyUpSustain1(
                        this.params_Osc[i].LFO);
                }
            }

            /* create key-up impulse */
            public void OscKeyUpSustain2()
            {
                for (int i = 0; i < this.paramCount; i += 1)
                {
                    EnvelopeKeyUpSustain2(
                        this.params_Osc[i].Envelope);
                    LFOGeneratorKeyUpSustain2(
                        this.params_Osc[i].LFO);
                }
            }

            /* create key-up impulse */
            public void OscKeyUpSustain3()
            {
                for (int i = 0; i < this.paramCount; i += 1)
                {
                    EnvelopeKeyUpSustain3(
                        this.params_Osc[i].Envelope);
                    LFOGeneratorKeyUpSustain3(
                        this.params_Osc[i].LFO);
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
                for (int i = 0; i < this.paramCount; i += 1)
                {
                    EnvelopeRetriggerFromOrigin(
                        this.params_Osc[i].Envelope,
                        ref NewAccents,
                        NewInitialFrequency,
                        1,
                        NewHurryUp,
                        ActuallyRetrigger,
                        SynthParams);
                    LFOGeneratorRetriggerFromOrigin(
                        this.params_Osc[i].LFO,
                        ref NewAccents,
                        NewInitialFrequency,
                        NewHurryUp,
                        1,
                        1,
                        ActuallyRetrigger,
                        SynthParams);
                }
            }

            // linear interpolation upsample (TODO: ought to replace with band-limited upsample)
            // unoptimized loop - optimize when scenarios become available
            private void Upsample(
                float[] source,
                int sourceOffset,
                float[] target,
                int targetOffset,
                int count,
                int rate,
                ref float last)
            {
                float left = last;

                for (int i = 0, n = 0; i < count; i++)
                {
                    Debug.Assert(n == i * rate);

                    float right = source[i + sourceOffset];

                    target[n + targetOffset] = left;
                    n++;
                    for (int c = 1; c < rate; c++, n++)
                    {
                        float rWeight = (float)c / rate;
                        target[n + targetOffset] = left + rWeight * (right - left);
                    }

                    left = right;
                }

                last = left;
            }

            /* apply user effect processing to some stuff */
            public SynthErrorCodes Apply(
                float[] workspace,
                int lOffset,
                int rOffset,
                int nActualFrames,
                SynthParamRec SynthParams)
            {
                Debug.Assert(nActualFrames % SynthParams.iOversampling == 0);

                // reinit each cycle because user code can reallocate or remove them
                leftWorkspaceHandle.floats = leftWorkspace;
                rightWorkspaceHandle.floats = rightWorkspace;

                int c = nActualFrames;
                double sr = SynthParams.dSamplingRate;

                if ((SynthParams.iOversampling == 1) || !disableOversampling)
                {
                    FloatVectorCopyUnaligned(
                        workspace,
                        lOffset,
                        leftWorkspaceHandle.floats, // not vector-aligned
                        0,
                        nActualFrames);
                    FloatVectorCopyUnaligned(
                        workspace,
                        rOffset,
                        rightWorkspaceHandle.floats, // not vector-aligned
                        0,
                        nActualFrames);
                }
                else
                {
                    // downsample
                    c /= SynthParams.iOversampling;
                    sr /= SynthParams.iOversampling;
                    for (int i = 0, j = 0; i < nActualFrames; i += SynthParams.iOversampling, j++)
                    {
                        leftWorkspaceHandle.floats[j] = workspace[i + lOffset];
                        rightWorkspaceHandle.floats[j] = workspace[i + rOffset];
                    }
                }

                int argCount = 1/*retval*/
                    + 1/*t*/
                    + 1/*bpm*/
                    + 1/*samplingRate*/
                    + 1/*leftdata*/
                    + 1/*rightdata*/
                    + 1/*count*/
                    + userState.Length/*user state*/
                    + paramCount/*user params*/
                    + 1/*retaddr*/;
                SynthParams.FormulaEvalContext.EmptyParamStackEnsureCapacity(argCount);

                int StackNumElements;
                StackElement[] StackBase;
                SynthParams.FormulaEvalContext.GetRawStack(out StackBase, out StackNumElements);

                StackBase[StackNumElements++].Data.Double = SynthParams.dElapsedTimeInSeconds; /* t */

                StackBase[StackNumElements++].Data.Double = SynthParams.dCurrentBeatsPerMinute; /* bpm */

                StackBase[StackNumElements++].Data.Double = sr; /* samplingRate */

                StackBase[StackNumElements++].reference.arrayHandleFloat = leftWorkspaceHandle; // leftdata

                StackBase[StackNumElements++].reference.arrayHandleFloat = rightWorkspaceHandle; // rightdata

                StackBase[StackNumElements++].Data.Integer = c;

                for (int i = 0; i < userState.Length; i++)
                {
                    StackBase[StackNumElements++].reference.arrayHandleGeneric = userState[i]; // user state
                }

                for (int i = 0; i < paramCount; i += 1)
                {
                    if (smoothingBuffers[i].arrayHandle == null)
                    {
                        StackBase[StackNumElements++].Data.Double = paramResults[i]; // user params
                    }
                    else
                    {
                        // re-initialize handle in case user code cleared it last time
                        smoothingBuffers[i].arrayHandle.floats = smoothingBuffers[i].vector;

                        // compute smoothed data
#if DEBUG
                        Debug.Assert(!SynthParams.ScratchWorkspace1InUse);
#endif
                        if (smoothingBuffers[i].degraded)
                        {
                            FloatVectorSet(
                                SynthParams.workspace,
                                SynthParams.ScratchWorkspace1LOffset,
                                nActualFrames,
                                (float)paramResults[i]);
                        }
                        else
                        {
                            if ((params_Osc == null) || !EnvelopeCurrentSegmentExponential(params_Osc[i].Envelope))
                            {
                                // linear
                                FloatVectorAdditiveRecurrence(
                                    SynthParams.workspace,
                                    SynthParams.ScratchWorkspace1LOffset,
                                    (float)paramResultsPrevious[i],
                                    (float)paramResults[i],
                                    nActualFrames);
                            }
                            else
                            {
                                // geometric
                                FloatVectorMultiplicativeRecurrence(
                                    SynthParams.workspace,
                                    SynthParams.ScratchWorkspace1LOffset,
                                    (float)paramResultsPrevious[i],
                                    (float)paramResults[i],
                                    nActualFrames);
                            }
                        }
                        FloatVectorCopyUnaligned(
                            SynthParams.workspace,
                            SynthParams.ScratchWorkspace1LOffset,
                            smoothingBuffers[i].vector, // target not aligned
                            0,
                            nActualFrames);

                        StackBase[StackNumElements++].reference.arrayHandleFloat = smoothingBuffers[i].arrayHandle; // user params
                    }
                }

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
                    SynthParams.ErrorInfo.UserEvalErrorCode = Error;
                    SynthParams.ErrorInfo.UserEvalErrorInfo = ErrorInfo;
                    return SynthErrorCodes.eSynthErrorEx;
                }
                Debug.Assert(SynthParams.FormulaEvalContext.GetStackNumElements() == 1); // return value

                SynthParams.FormulaEvalContext.Clear();

                if ((leftWorkspaceHandle.floats != null) && (leftWorkspaceHandle.floats.Length >= c)
                    && (rightWorkspaceHandle.floats != null) && (rightWorkspaceHandle.floats.Length >= c))
                {
                    if ((SynthParams.iOversampling == 1) || !disableOversampling)
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
                    else
                    {
                        // upsample
                        Upsample(
                            leftWorkspaceHandle.floats,
                            0,
                            workspace,
                            lOffset,
                            c,
                            SynthParams.iOversampling,
                            ref lastLeft);
                        Upsample(
                            rightWorkspaceHandle.floats,
                            0,
                            workspace,
                            rOffset,
                            c,
                            SynthParams.iOversampling,
                            ref lastRight);
                        // remove NaN/Infinity - prevent user effect misbehavior from taking rest of system down
                        FloatVectorCopyReplaceNaNInf(
                            workspace,
                            lOffset,
                            workspace,
                            lOffset,
                            nActualFrames);
                        FloatVectorCopyReplaceNaNInf(
                            workspace,
                            rOffset,
                            workspace,
                            rOffset,
                            nActualFrames);
                    }
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
