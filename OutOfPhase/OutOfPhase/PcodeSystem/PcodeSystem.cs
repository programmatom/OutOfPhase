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
using System.Threading;
using System.Windows.Forms;

namespace OutOfPhase
{
    /* the errors that can occur when evaluating a pcode program */
    public enum EvalErrors
    {
        eEvalNoError,
        eEvalUndefinedFunction,
        eEvalFunctionSignatureMismatch,
        eEvalErrorTrapEncountered,
        eEvalUserCancelled,
        eEvalDivideByZero,
        eEvalOutOfMemory,
        eEvalArrayDoesntExist,
        eEvalArraySubscriptOutOfRange,
        eEvalGetSampleNotDefined,
        eEvalGetSampleWrongChannelType,
        eEvalUnableToImportFile,
        eEvalArrayWrongDimensions,
    }

    /* error information */
    [StructLayout(LayoutKind.Auto)]
    public struct EvalErrInfoRec
    {
        public OpcodeRec[] OffendingPcode;
        public int OffendingInstruction;
    }

    public static class PcodeSystem
    {
        /* return a static null terminated string describing an error code. */
        public static string GetPcodeErrorMessage(EvalErrors Error)
        {
            switch (Error)
            {
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();
                case EvalErrors.eEvalNoError:
                    //return "No error";
                    Debug.Assert(false);
                    throw new ArgumentException();
                case EvalErrors.eEvalUndefinedFunction:
                    return "Called an undefined function.";
                case EvalErrors.eEvalFunctionSignatureMismatch:
                    return "Function call type signature does not match target function's type.";
                case EvalErrors.eEvalErrorTrapEncountered:
                    return "Error trap encountered; user cancelled execution.";
                case EvalErrors.eEvalUserCancelled:
                    return "User cancelled.";
                case EvalErrors.eEvalDivideByZero:
                    return "Divide by zero.";
                case EvalErrors.eEvalOutOfMemory: // not relevant on .NET
                    return "Out of memory.";
                case EvalErrors.eEvalArrayDoesntExist:
                    return "Use of unallocated (NIL) array.";
                case EvalErrors.eEvalArraySubscriptOutOfRange:
                    return "Array subscript out of range.";
                case EvalErrors.eEvalGetSampleNotDefined:
                    return "Attempt to access undefined sample or wave table.";
                case EvalErrors.eEvalGetSampleWrongChannelType:
                    return "Attempt to load sample array with wrong channel type.";
                case EvalErrors.eEvalUnableToImportFile:
                    return "Unable to import sample from file.";
                case EvalErrors.eEvalArrayWrongDimensions:
                    return "The array has the wrong dimensions.";
            }
        }

        // Discussion of infinite loops and thread aborting:
        //
        // There are two conflicting goals: UI responsiveness and execution efficiency. In the (old pre-OSX
        // Macintosh) code, the execution loop maintained a counter/clock and periodically checked the UI for
        // a cancel gesture. In the new world, this is difficult to do, and the overhead of maintaining the
        // counter over each loop iteration for something that rarely happens is problematic.
        //
        // In .NET, thanks to how the execution engine works, one way to handle the situation is to run
        // code on a task thread and use Thread.Abort() to cancel it, which triggers a ThreadAbortException
        // at a 'safe' point in the execution. The Pcode evaluator catches the exception, calls Thread.ResetAbort()
        // to ensure orderly continuation, and then returns to it's caller with the eEvalUserCancelled result code.
        //
        // However, running the code on the main UI thread (as is done for pre-synthesis build operations) is
        // a problem because the UI can't breathe enough to receive a cancel gesture, unless the execution loop
        // is modified to have the old counter and periodic message pump. For expediency, a decision was made to
        // not support execution on the main thread. Therefore, the helper class EvaluatePcodeThread is provided
        // to make it easy to provide cancel UI and cancelable execution on a task thread.

        // Wrapper class that allows the main UI thread to conveniently evaluate a synchronously on a
        // task thread.
        public class EvaluatePcodeThread
        {
            private const int LongRunningTaskLatency = 5; // seconds

            private bool done;

            private EvalErrors Result;
            private EvalErrInfoRec ErrorInfo;

            private PcodeThreadContext threadContext;
            private ManualResetEvent finished; // scoped, IDisposable.Dispose() not needed

            private readonly ParamStackRec Prep;
            private readonly PcodeRec Pcode;
            private readonly CodeCenterRec CodeCenter;
            private readonly IEvaluationContext EvaluateContext;

            public EvaluatePcodeThread(
                ParamStackRec Prep,
                PcodeRec Pcode,
                CodeCenterRec CodeCenter,
                IEvaluationContext EvaluateContext)
            {
                this.Prep = Prep;
                this.Pcode = Pcode;
                this.CodeCenter = CodeCenter;
                this.EvaluateContext = EvaluateContext;
            }

            private void ThreadMain()
            {
                try
                {
                    Result = OutOfPhase.PcodeSystem.EvaluatePcode(
                        Prep,
                        Pcode,
                        CodeCenter,
                        out ErrorInfo,
                        EvaluateContext,
                        ref threadContext);
                }
                finally
                {
                    finished.Set();
                }
            }

            public EvalErrors Do(
                out EvalErrInfoRec ErrorInfo)
            {
                if (done)
                {
                    Debug.Assert(false); // they are single use only
                    throw new InvalidOperationException();
                }
                done = true;

                using (finished = new ManualResetEvent(false))
                {
                    DateTime start = DateTime.UtcNow;

                    Thread thread = new Thread(
                        new ThreadStart(
                            delegate ()
                            {
                                ThreadMain();
                            }));
                    thread.Start();

                    LongRunningOperationWindow userCancelWindow = null;
                    bool cancelled = false;
                    try
                    {
                        int delay = 250;
                        while (!finished.WaitOne(delay))
                        {
                            if ((DateTime.UtcNow - start).TotalSeconds >= LongRunningTaskLatency)
                            {
                                if (userCancelWindow == null)
                                {
                                    userCancelWindow = new LongRunningOperationWindow();
                                    userCancelWindow.Show();
                                }
                                else
                                {
                                    Application.DoEvents();
                                }
                            }
                            if ((userCancelWindow != null) && userCancelWindow.Cancelled && !cancelled)
                            {
                                cancelled = true;

                                SafeCancel(thread, ref threadContext);

                                // EvaluatePcode will eventually receive ThreadAbortException, which will terminate
                                // the evaluation loop and exit the thread, setting the 'finished' event.
                            }
                            if (((EvaluateContext as PcodeExterns) != null) && ((PcodeExterns)EvaluateContext).Interaction)
                            {
                                if (delay == 0) // must get modal dialog posted before this is permitted
                                {
                                    Application.DoEvents();
                                }
                                start = DateTime.MinValue;
                                delay = 0;
                            }
                        }
                    }
                    finally
                    {
                        if (((EvaluateContext as PcodeExterns) != null) && ((PcodeExterns)EvaluateContext).Interaction)
                        {
                            // reset it in case context is reused for another evaluation
                            ((PcodeExterns)EvaluateContext).Interaction = false;
                        }
                        if (userCancelWindow != null)
                        {
                            userCancelWindow.Dispose();
                        }
                    }
                }

                ErrorInfo = this.ErrorInfo;
                return this.Result;
            }

            // The entire purpose of this method is to cancel runaway execution in user-provided functions. If not
            // for that, synchronization primmitives would suffice entirely.
            public static void SafeCancel(
                Thread thread,
                ref PcodeThreadContext threadContext)
            {
                bool suspended = false;
                try
                {
                    Thread.VolatileWrite(ref threadContext.GlobalCancelPending, 1);

                    // First, give the thread some time to exit normally. This covers the 90% case (cancelling the synth engine)
                    // where user functions will finish very quickly.
                    if (thread.Join(250))
                    {
                        return;
                    }

                    // What follows is somewhat unstable. It works most of the time but deadlocks or leaves orphaned threads
                    // often enough. Well, there's always auto-save...

                    // if thread has already stopped, this may throw:
                    thread.Suspend();
                    while ((thread.ThreadState == System.Threading.ThreadState.Running)
                        || (thread.ThreadState == System.Threading.ThreadState.SuspendRequested))
                    {
                        thread.Join(0);
                    }
                    suspended = true;

                    // check thread if it's in pcodesystem execution. eventually timeout if not.
                    if (Thread.VolatileRead(ref threadContext.InEval) != 0)
                    {
                        // set abort request on thread
                        thread.Abort();
                    }
                    // Else - thread is not in pcode eval loop. Something else must be going wrong. This
                    // could be one of:
                    // 1. A program bug (infinite loop) elsewhere
                    // 2. Very slow program algorithm, or program invoking pcode eval in a long-running
                    //    loop, even though each individual pcode eval may be quick. This case is mitigated
                    //    by the GlobalCancelPending field of the thread context.
                }
                catch (ThreadStateException)
                {
                    // since thread is suspended, capture this exception
                    // ThreadState will have AbortRequested on it
                }
                if (suspended)
                {
                    try
                    {
                        thread.Resume();
                    }
                    catch (ThreadStateException exception)
                    {
                        Debug.Assert(false, exception.ToString());
                    }
                }

                // EvaluatePcode will eventually receive ThreadAbortException, which will terminate
                // the evaluation loop and exit the thread, setting the 'finished' event.
            }

            public static EvalErrors EvaluatePcode(
                ParamStackRec Prep,
                PcodeRec Pcode,
                CodeCenterRec CodeCenter,
                out EvalErrInfoRec ErrorInfo,
                IEvaluationContext EvaluateContext)
            {
                EvaluatePcodeThread thread = new EvaluatePcodeThread(
                    Prep,
                    Pcode,
                    CodeCenter,
                    EvaluateContext);
                return thread.Do(out ErrorInfo);
            }
        }

        [StructLayout(LayoutKind.Auto)]
        public struct PcodeThreadContext
        {
            public int InEval; // non-zero: pcode eval loop is executing on stack (may have called out to extern or reentered)
            public int GlobalCancelPending;
        }

        public interface IEvaluationContext
        {
            EvalErrors Invoke(string name, object[] args, out object returnValue);
        }

        // Only call this from auxilliary threads!
        public static EvalErrors EvaluatePcode(
            ParamStackRec Prep,
            PcodeRec Pcode,
            CodeCenterRec CodeCenter,
            out EvalErrInfoRec ErrorInfo,
            IEvaluationContext EvaluationContext,
            ref PcodeThreadContext threadContext)
        {
            ErrorInfo.OffendingPcode = null;
            ErrorInfo.OffendingInstruction = 0;

            int ProgramCounter = 0;
            OpcodeRec[] CurrentProcedure = null;

            Interlocked.Increment(ref threadContext.InEval);
            try
            {
                // Heuristic check to trap execution from the main UI thread
                Debug.Assert(Thread.CurrentThread.GetApartmentState() != ApartmentState.STA);

                // current procedure and pointers extracted locally for simplified reference
                PcodeRec CurrentProcedurePcode = Pcode;
                CurrentProcedure = Pcode.GetOpcodeFromPcode(); // simplified reference
                double[] CurrentProcedureDoubles = Pcode.doubles; // simplified reference

                EvalErrors ErrorCode;

                StackElement[] Stack;
                int StackPtr;
                Prep.GetRawStack(out Stack, out StackPtr);
                StackPtr--;

                if (CILObject.EnableCIL && (Pcode.cilObject != null))
                {
                    CILThreadLocalStorage.Push(
                        EvaluationContext,
                        CodeCenter.ManagedFunctionLinker.FunctionPointers,
                        CodeCenter.ManagedFunctionLinker.FunctionSignatures);
                    try
                    {
                        ErrorCode = Pcode.cilObject.InvokeShim(Stack, ref StackPtr);
                        if (ErrorCode != EvalErrors.eEvalNoError)
                        {
                            goto ExceptionPoint; // TODO: need more error handling
                        }
                    }
                    finally
                    {
                        CILThreadLocalStorage.Pop();
                    }
                    goto TotallyDonePoint;
                }

                // ensure stack capacity
                Debug.Assert(Pcode.MaxStackDepth >= 0);
                // add current stack pointer for args, retaddr, and return value slot
                // note that StackPtr and MaxStackDepth are stack references (elements), so must be
                // incremented by 1 each for size.
                int maxStackCapacity = (Pcode.MaxStackDepth + 1) + (StackPtr + 1);
                if (Stack.Length < maxStackCapacity)
                {
                    Array.Resize(ref Stack, maxStackCapacity);
                }

                if (Thread.VolatileRead(ref threadContext.GlobalCancelPending) != 0)
                {
                    ErrorCode = EvalErrors.eEvalUserCancelled;
                    goto ExceptionPoint;
                }

                /* main execution loop.  this ends when there is nothing on the stack and */
                /* something tries to execute a return from subroutine, which means the outermost */
                /* procedure is returning.  the final return value of the program will be in P. */
                int FuncCallDepth = 0;
                while (true)
                {
                    switch (CurrentProcedure[ProgramCounter++].Opcode)
                    {
                        // There are a number of performance issues with the code contained herein. They include (among
                        // others) inefficient clearing (i.e. generic clearing even though static type is known) and
                        // redundant nullcheck/boundscheck. However, this code is deprecated since CIL generation will
                        // always be more performant; it's maintained as a reference and debugging aid. Therefore,
                        // performance improvement work is not being done here any more.

                        default:
                            //ErrorCode = EvalErrors.eEvalErrorTrapEncountered;
                            //goto ExceptionPoint;
                            Debug.Assert(false);
                            throw new InvalidOperationException();

                        case Pcodes.epFuncCallUnresolved:
                            /* an unresolved function call.  try to resolve it */
                            /*     -1           0               1           2             3        4 */
                            /* <opcode> ^"<functionname>" ^[paramlist] <returntype> <reserved> <reserved> */

                            if (CodeCenter == null)
                            {
                                ErrorCode = EvalErrors.eEvalUndefinedFunction;
                                goto ExceptionPoint;
                            }
                            string name = CurrentProcedurePcode.strings[
                                CurrentProcedure[ProgramCounter + 0].ImmediateString_Ref];
                            FuncCodeRec Function = CodeCenter.ObtainFunctionHandle(name);
                            if (Function == null)
                            {
                                ErrorCode = EvalErrors.eEvalUndefinedFunction;
                                goto ExceptionPoint;
                            }
                            DataTypes[] ParameterListExpected = CurrentProcedurePcode.dataTypesArrays[
                                CurrentProcedure[ProgramCounter + 1].DataTypeArray_Ref];
                            DataTypes[] ParameterListActual = Function.GetFunctionParameterTypeList();
                            if (ParameterListExpected.Length != ParameterListActual.Length)
                            {
                                /* different number of parameters */
                                ErrorCode = EvalErrors.eEvalFunctionSignatureMismatch;
                                goto ExceptionPoint;
                            }
                            for (int i = 0; i < ParameterListExpected.Length; i++)
                            {
                                if (ParameterListExpected[i] != ParameterListActual[i])
                                {
                                    /* parameters of different types */
                                    ErrorCode = EvalErrors.eEvalFunctionSignatureMismatch;
                                    goto ExceptionPoint;
                                }
                            }
                            if (CurrentProcedure[ProgramCounter + 2].DataType != Function.GetFunctionReturnType())
                            {
                                /* different return types */
                                ErrorCode = EvalErrors.eEvalFunctionSignatureMismatch;
                                goto ExceptionPoint;
                            }
                            /* finally, the function appears to be the one we want. */
                            /* first, install the PcodeRec in the instruction */
                            CurrentProcedure[ProgramCounter + 3].FunctionPcodeRecPtr_Ref
                                = CurrentProcedurePcode.AppendExternRef(Function.GetFunctionPcode());
                            CurrentProcedure[ProgramCounter + 4].ImmediateInteger
                                = Function.GetFunctionPcode().MaxStackDepth;
                            /* next, change the instruction to epFuncCallResolved */
                            // do this last so that another thread won't see a partially constructed 'resolved' call
                            CurrentProcedure[ProgramCounter - 1].Opcode = Pcodes.epFuncCallResolved;

                            goto epFunctionCallResolvedPoint;

                        case Pcodes.epFuncCallResolved:
                        epFunctionCallResolvedPoint:
                            /* a function call whose destination is known. */
                            /*     -1           0               1           2             3    */
                            /* <opcode> ^"<functionname>" ^[paramlist] <returntype> ^<OpcodeRec> */
                            StackPtr++;
                            Debug.Assert(StackPtr < Stack.Length);
                            /* store return address on stack.  the return address is the address */
                            /* of the NEXT instruction. */
#if DEBUG
                            Stack[StackPtr].AssertClear();
#endif
                            Stack[StackPtr].reference.Procedure = CurrentProcedurePcode;
                            Stack[StackPtr].Data.Integer = ProgramCounter + 5; /* 5 words to skip */
                            // reserve stack size required by this function
                            maxStackCapacity = (CurrentProcedure[ProgramCounter + 4].ImmediateInteger + 1) + (StackPtr + 1);
                            if (Stack.Length < maxStackCapacity)
                            {
                                Array.Resize(ref Stack, maxStackCapacity);
                            }
                            /* perform the jump */
                            CurrentProcedurePcode = CurrentProcedurePcode.externRefs[
                                CurrentProcedure[ProgramCounter + 3].FunctionPcodeRecPtr_Ref];
                            CurrentProcedure = CurrentProcedurePcode.GetOpcodeFromPcode(); // simplified reference
                            CurrentProcedureDoubles = CurrentProcedurePcode.doubles; // simplified reference
                            ProgramCounter = 0;
                            /* increment function call depth counter */
                            FuncCallDepth++;
                            break;

                        /* invoke external environment method. semantics are same as epFuncCallUnresolved. */
                        case Pcodes.epFuncCallExternal:
                            /*     -1           0              1           2      */
                            /* <opcode> ^"<methodname>" ^[paramlist] <returntype> */
                            if (EvaluationContext != null)
                            {
                                string methodName = CurrentProcedurePcode.strings[CurrentProcedure[ProgramCounter + 0].ImmediateString_Ref];
                                DataTypes[] methodArgsTypes = CurrentProcedurePcode.dataTypesArrays[CurrentProcedure[ProgramCounter + 1].DataTypeArray_Ref];
                                DataTypes methodReturnType = (DataTypes)(CurrentProcedure[ProgramCounter + 2].ImmediateInteger);

                                // prepare managed argument list
                                object[] methodArgs = new object[methodArgsTypes.Length];
                                for (int i = 0; i < methodArgs.Length; i++)
                                {
                                    PcodeExterns.MarshalToManaged(
                                        ref Stack[StackPtr - methodArgs.Length + 1 + i],
                                        methodArgsTypes[i],
                                        out methodArgs[i]);
                                }

                                // invoke
                                object returnValue;
                                ErrorCode = EvaluationContext.Invoke(methodName, methodArgs, out returnValue);
                                if (ErrorCode != EvalErrors.eEvalNoError)
                                {
                                    goto ExceptionPoint;
                                }
                                ErrorCode = PcodeExterns.TypeCheckValue(returnValue, methodReturnType);
                                if (ErrorCode != EvalErrors.eEvalNoError)
                                {
                                    goto ExceptionPoint;
                                }

                                // pop args
                                for (int i = 0; i < methodArgs.Length; i++)
                                {
                                    Stack[StackPtr--].Clear();
                                }

                                // push result
                                StackPtr++;
                                Debug.Assert(StackPtr < Stack.Length);
                                PcodeExterns.MarshalToPcode(returnValue, ref Stack[StackPtr], methodReturnType);

                                ProgramCounter += 3;
                            }
                            else
                            {
                                ErrorCode = EvalErrors.eEvalUndefinedFunction;
                                goto ExceptionPoint;
                            }
                            break;

                        case Pcodes.epBranchUnconditional:
                            /*    -1            0    */
                            /* <opcode> <branchoffset> */
                            ProgramCounter = CurrentProcedure[ProgramCounter + 0].ImmediateInteger;
                            break;

                        case Pcodes.epBranchIfZero:
                            /*    -1            0    */
                            /* <opcode> <branchoffset> */
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            if (Stack[StackPtr].Data.Integer == 0)
                            {
                                ProgramCounter = CurrentProcedure[ProgramCounter + 0].ImmediateInteger;
                            }
                            else
                            {
                                ProgramCounter++;
                            }
                            Stack[StackPtr].Clear();
                            StackPtr--;
                            break;

                        case Pcodes.epBranchIfNotZero:
                            /*    -1            0    */
                            /* <opcode> <branchoffset> */
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            if (Stack[StackPtr].Data.Integer != 0)
                            {
                                ProgramCounter = CurrentProcedure[ProgramCounter + 0].ImmediateInteger;
                            }
                            else
                            {
                                ProgramCounter++;
                            }
                            Stack[StackPtr].Clear();
                            StackPtr--;
                            break;

                        case Pcodes.epReturnFromSubroutine:
                            /* save number of slots to pop */
                            int PopCount = CurrentProcedure[ProgramCounter + 0].ImmediateInteger + 1/*for retaddr*/;
                            /* update instruction pointer */
                            if (FuncCallDepth > 0)
                            {
#if DEBUG
                                Stack[StackPtr - 1].AssertReturnAddress();
#endif
                                ProgramCounter = Stack[StackPtr - 1].Data.Integer;
                                /* perform the jump */
                                Debug.Assert(Stack[StackPtr - 1].reference.Procedure is PcodeRec);
                                CurrentProcedurePcode = Stack[StackPtr - 1].reference.Procedure;
                                CurrentProcedure = CurrentProcedurePcode.GetOpcodeFromPcode(); // simplified reference
                                CurrentProcedureDoubles = CurrentProcedurePcode.doubles; // simplified reference
                            }
                            /* deallocate arguments (popmultipleunder) */
                            int OldReturnValueIndex = StackPtr; /* save old stack pointer */
                            while (PopCount > 0)
                            {
                                Stack[StackPtr - 1].Clear();
                                StackPtr--;
                                PopCount--;
                            }
                            /* move top element (retval) to new top */
                            Stack[StackPtr] = Stack[OldReturnValueIndex];
                            if (StackPtr != OldReturnValueIndex)
                            {
                                Stack[OldReturnValueIndex] = new StackElement();
                            }
                            /* if we are returning from root, then it's the end */
                            if (FuncCallDepth == 0)
                            {
                                goto TotallyDonePoint;
                            }
                            FuncCallDepth--;
                            break;

                        /* arithmetic operations */
                        /* the right hand argument for binary operators is on top of stack */
                        case Pcodes.epOperationIntegerEqual:
                        case Pcodes.epOperationBooleanEqual:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Integer == Stack[StackPtr].Data.Integer ? 1 : 0;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationIntegerNotEqual:
                        case Pcodes.epOperationBooleanNotEqual:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Integer != Stack[StackPtr].Data.Integer ? 1 : 0;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationIntegerXor:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Integer ^ Stack[StackPtr].Data.Integer;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationBooleanAnd:
                        case Pcodes.epOperationIntegerAnd:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Integer & Stack[StackPtr].Data.Integer;
                            /* note:  bitwise and! */
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationBooleanOr:
                        case Pcodes.epOperationIntegerOr:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Integer | Stack[StackPtr].Data.Integer;
                            /* note:  bitwise or! */
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationBooleanNot:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Integer = Stack[StackPtr].Data.Integer ^ 1;
                            break;
                        case Pcodes.epOperationIntegerNot:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Integer = ~Stack[StackPtr].Data.Integer;
                            break;
                        case Pcodes.epOperationBooleanToInteger:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            break;
                        case Pcodes.epOperationTestIntegerNegative:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Integer = Stack[StackPtr].Data.Integer < 0 ? 1 : 0;
                            break;
                        case Pcodes.epOperationTestFloatNegative:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Integer = Stack[StackPtr].Data.Float < 0 ? 1 : 0;
                            break;
                        case Pcodes.epOperationTestDoubleNegative:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Integer = Stack[StackPtr].Data.Double < 0 ? 1 : 0;
                            break;
                        case Pcodes.epOperationGetSignInteger:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            if (Stack[StackPtr].Data.Integer < 0)
                            {
                                Stack[StackPtr].Data.Integer = -1;
                            }
                            else if (Stack[StackPtr].Data.Integer > 0)
                            {
                                Stack[StackPtr].Data.Integer = 1;
                            }
                            else
                            {
                                Stack[StackPtr].Data.Integer = 0;
                            }
                            break;
                        case Pcodes.epOperationGetSignFloat:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            if (Stack[StackPtr].Data.Float < 0)
                            {
                                Stack[StackPtr].Data.Integer = -1;
                            }
                            else if (Stack[StackPtr].Data.Float > 0)
                            {
                                Stack[StackPtr].Data.Integer = 1;
                            }
                            else
                            {
                                Stack[StackPtr].Data.Integer = 0;
                            }
                            break;
                        case Pcodes.epOperationGetSignDouble:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            if (Stack[StackPtr].Data.Double < 0)
                            {
                                Stack[StackPtr].Data.Integer = -1;
                            }
                            else if (Stack[StackPtr].Data.Double > 0)
                            {
                                Stack[StackPtr].Data.Integer = 1;
                            }
                            else
                            {
                                Stack[StackPtr].Data.Integer = 0;
                            }
                            break;
                        case Pcodes.epOperationIntegerToFloat:
                        case Pcodes.epOperationBooleanToFloat:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Float = (float)Stack[StackPtr].Data.Integer;
                            break;
                        case Pcodes.epOperationIntegerToDouble:
                        case Pcodes.epOperationBooleanToDouble:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Double = (double)Stack[StackPtr].Data.Integer;
                            break;
                        case Pcodes.epOperationIntegerNegation:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Integer = -Stack[StackPtr].Data.Integer;
                            break;
                        case Pcodes.epOperationIntegerAdd:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Integer + Stack[StackPtr].Data.Integer;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationIntegerSubtract:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Integer - Stack[StackPtr].Data.Integer;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationIntegerMultiply:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Integer * Stack[StackPtr].Data.Integer;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationIntegerDivide:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            if (Stack[StackPtr].Data.Integer == 0)
                            {
                                ErrorCode = EvalErrors.eEvalDivideByZero;
                                goto ExceptionPoint;
                            }
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Integer / Stack[StackPtr].Data.Integer;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationIntegerModulo:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            if (Stack[StackPtr].Data.Integer == 0)
                            {
                                ErrorCode = EvalErrors.eEvalDivideByZero;
                                goto ExceptionPoint;
                            }
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Integer % Stack[StackPtr].Data.Integer;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationIntegerAbs:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            if (Stack[StackPtr].Data.Integer < 0)
                            {
                                Stack[StackPtr].Data.Integer = -Stack[StackPtr].Data.Integer;
                            }
                            break;
                        case Pcodes.epOperationIntegerShiftLeft:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Integer << Stack[StackPtr].Data.Integer;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationIntegerShiftRight:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Integer >> Stack[StackPtr].Data.Integer;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationIntegerGreaterThan:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Integer > Stack[StackPtr].Data.Integer ? 1 : 0;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationIntegerLessThan:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Integer < Stack[StackPtr].Data.Integer ? 1 : 0;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationIntegerGreaterThanOrEqual:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Integer >= Stack[StackPtr].Data.Integer ? 1 : 0;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationIntegerLessThanOrEqual:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Integer <= Stack[StackPtr].Data.Integer ? 1 : 0;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationIntegerToBoolean:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Integer = Stack[StackPtr].Data.Integer != 0 ? 1 : 0;
                            break;
                        case Pcodes.epOperationFloatAdd:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Float =
                                Stack[StackPtr - 1].Data.Float + Stack[StackPtr].Data.Float;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationFloatSubtract:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Float =
                                Stack[StackPtr - 1].Data.Float - Stack[StackPtr].Data.Float;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationFloatNegation:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Float = -Stack[StackPtr].Data.Float;
                            break;
                        case Pcodes.epOperationFloatMultiply:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Float =
                                Stack[StackPtr - 1].Data.Float * Stack[StackPtr].Data.Float;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationFloatDivide:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Float =
                                Stack[StackPtr - 1].Data.Float / Stack[StackPtr].Data.Float;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationFloatGreaterThan:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Float > Stack[StackPtr].Data.Float ? 1 : 0;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationFloatLessThan:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Float < Stack[StackPtr].Data.Float ? 1 : 0;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationFloatGreaterThanOrEqual:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Float >= Stack[StackPtr].Data.Float ? 1 : 0;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationFloatLessThanOrEqual:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Float <= Stack[StackPtr].Data.Float ? 1 : 0;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationFloatEqual:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Float == Stack[StackPtr].Data.Float ? 1 : 0;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationFloatNotEqual:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Float != Stack[StackPtr].Data.Float ? 1 : 0;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationFloatAbs:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Float = Math.Abs(Stack[StackPtr].Data.Float);
                            break;
                        case Pcodes.epOperationFloatToBoolean:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Integer = Stack[StackPtr].Data.Float != 0 ? 1 : 0;
                            break;
                        case Pcodes.epOperationFloatToInteger:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Integer = (int)Stack[StackPtr].Data.Float;
                            break;
                        case Pcodes.epOperationFloatToDouble:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Double = (double)Stack[StackPtr].Data.Float;
                            break;
                        case Pcodes.epOperationDoubleAdd:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Double =
                                Stack[StackPtr - 1].Data.Double + Stack[StackPtr].Data.Double;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationDoubleSubtract:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Double =
                                Stack[StackPtr - 1].Data.Double - Stack[StackPtr].Data.Double;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationDoubleNegation:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Double = -Stack[StackPtr].Data.Double;
                            break;
                        case Pcodes.epOperationDoubleMultiply:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Double =
                                Stack[StackPtr - 1].Data.Double * Stack[StackPtr].Data.Double;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationDoubleDivide:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Double =
                                Stack[StackPtr - 1].Data.Double / Stack[StackPtr].Data.Double;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationDoubleGreaterThan:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Double > Stack[StackPtr].Data.Double ? 1 : 0;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationDoubleLessThan:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Double < Stack[StackPtr].Data.Double ? 1 : 0;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationDoubleGreaterThanOrEqual:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Double >= Stack[StackPtr].Data.Double ? 1 : 0;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationDoubleLessThanOrEqual:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Double <= Stack[StackPtr].Data.Double ? 1 : 0;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationDoubleEqual:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Double == Stack[StackPtr].Data.Double ? 1 : 0;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationDoubleNotEqual:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer =
                                Stack[StackPtr - 1].Data.Double != Stack[StackPtr].Data.Double ? 1 : 0;
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationDoubleAbs:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Double = Math.Abs(Stack[StackPtr].Data.Double);
                            break;
                        case Pcodes.epOperationDoubleToBoolean:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Integer = Stack[StackPtr].Data.Double != 0 ? 1 : 0;
                            break;
                        case Pcodes.epOperationDoubleToInteger:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Integer = (int)Stack[StackPtr].Data.Double;
                            break;
                        case Pcodes.epOperationDoubleToFloat:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Float = (float)Stack[StackPtr].Data.Double;
                            break;
                        case Pcodes.epOperationDoubleSinF:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Float = (float)Math.Sin(Stack[StackPtr].Data.Float);
                            break;
                        case Pcodes.epOperationDoubleSinD:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Double = Math.Sin(Stack[StackPtr].Data.Double);
                            break;
                        case Pcodes.epOperationDoubleCosF:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Float = (float)Math.Cos(Stack[StackPtr].Data.Float);
                            break;
                        case Pcodes.epOperationDoubleCosD:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Double = Math.Cos(Stack[StackPtr].Data.Double);
                            break;
                        case Pcodes.epOperationDoubleTanF:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Float = (float)Math.Tan(Stack[StackPtr].Data.Float);
                            break;
                        case Pcodes.epOperationDoubleTanD:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Double = Math.Tan(Stack[StackPtr].Data.Double);
                            break;
                        case Pcodes.epOperationDoubleAsinF:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Float = (float)Math.Asin(Stack[StackPtr].Data.Float);
                            break;
                        case Pcodes.epOperationDoubleAsinD:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Double = Math.Asin(Stack[StackPtr].Data.Double);
                            break;
                        case Pcodes.epOperationDoubleAcosF:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Float = (float)Math.Acos(Stack[StackPtr].Data.Float);
                            break;
                        case Pcodes.epOperationDoubleAcosD:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Double = Math.Acos(Stack[StackPtr].Data.Double);
                            break;
                        case Pcodes.epOperationDoubleAtanF:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Float = (float)Math.Atan(Stack[StackPtr].Data.Float);
                            break;
                        case Pcodes.epOperationDoubleAtanD:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Double = Math.Atan(Stack[StackPtr].Data.Double);
                            break;
                        case Pcodes.epOperationDoubleLnF:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Float = (float)Math.Log(Stack[StackPtr].Data.Float);
                            break;
                        case Pcodes.epOperationDoubleLnD:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Double = Math.Log(Stack[StackPtr].Data.Double);
                            break;
                        case Pcodes.epOperationDoubleExpF:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Float = (float)Math.Exp(Stack[StackPtr].Data.Float);
                            break;
                        case Pcodes.epOperationDoubleExpD:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Double = Math.Exp(Stack[StackPtr].Data.Double);
                            break;
                        case Pcodes.epOperationDoubleSqrtF:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Float = (float)Math.Sqrt(Stack[StackPtr].Data.Float);
                            break;
                        case Pcodes.epOperationDoubleSqrtD:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Double = Math.Sqrt(Stack[StackPtr].Data.Double);
                            break;
                        case Pcodes.epOperationDoubleSqrF:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Float = Stack[StackPtr].Data.Float * Stack[StackPtr].Data.Float;
                            break;
                        case Pcodes.epOperationDoubleSqrD:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Double = Stack[StackPtr].Data.Double * Stack[StackPtr].Data.Double;
                            break;
                        case Pcodes.epOperationDoubleFloorF:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Float = (float)Math.Floor(Stack[StackPtr].Data.Float);
                            break;
                        case Pcodes.epOperationDoubleFloorD:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Double = Math.Floor(Stack[StackPtr].Data.Double);
                            break;
                        case Pcodes.epOperationDoubleCeilF:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Float = (float)Math.Ceiling(Stack[StackPtr].Data.Float);
                            break;
                        case Pcodes.epOperationDoubleCeilD:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Double = Math.Ceiling(Stack[StackPtr].Data.Double);
                            break;
                        case Pcodes.epOperationDoubleRoundF:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Float = (float)Math.Round(Stack[StackPtr].Data.Float);
                            break;
                        case Pcodes.epOperationDoubleRoundD:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Double = Math.Round(Stack[StackPtr].Data.Double);
                            break;
                        case Pcodes.epOperationDoubleCoshF:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Float = (float)Math.Cosh(Stack[StackPtr].Data.Float);
                            break;
                        case Pcodes.epOperationDoubleCoshD:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Double = Math.Cosh(Stack[StackPtr].Data.Double);
                            break;
                        case Pcodes.epOperationDoubleSinhF:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Float = (float)Math.Sinh(Stack[StackPtr].Data.Float);
                            break;
                        case Pcodes.epOperationDoubleSinhD:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Double = Math.Sinh(Stack[StackPtr].Data.Double);
                            break;
                        case Pcodes.epOperationDoubleTanhF:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Float = (float)Math.Tanh(Stack[StackPtr].Data.Float);
                            break;
                        case Pcodes.epOperationDoubleTanhD:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
#endif
                            Stack[StackPtr].Data.Double = Math.Tanh(Stack[StackPtr].Data.Double);
                            break;
                        case Pcodes.epOperationDoublePowerF:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Float = (float)Math.Pow(Stack[StackPtr - 1].Data.Float, Stack[StackPtr].Data.Float);
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epOperationDoublePowerD:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Double =
                                Math.Pow(Stack[StackPtr - 1].Data.Double, Stack[StackPtr].Data.Double);
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;

                        /* array sizing stuff. */
                        case Pcodes.epGetByteArraySize:
                            {
#if DEBUG
                                Stack[StackPtr].AssertByteArray();
#endif
                                if (Stack[StackPtr].reference.arrayHandleByte == null)
                                {
                                    ErrorCode = EvalErrors.eEvalArrayDoesntExist;
                                    goto ExceptionPoint;
                                }
                                int c = Stack[StackPtr].reference.arrayHandleByte.Length;
                                Stack[StackPtr].ClearArray();
                                Stack[StackPtr].Data.Integer = c;
                            }
                            break;
                        case Pcodes.epGetIntegerArraySize:
                            {
#if DEBUG
                                Stack[StackPtr].AssertIntegerArray();
#endif
                                if (Stack[StackPtr].reference.arrayHandleInt32 == null)
                                {
                                    ErrorCode = EvalErrors.eEvalArrayDoesntExist;
                                    goto ExceptionPoint;
                                }
                                int c = Stack[StackPtr].reference.arrayHandleInt32.Length;
                                Stack[StackPtr].ClearArray();
                                Stack[StackPtr].Data.Integer = c;
                            }
                            break;
                        case Pcodes.epGetFloatArraySize:
                            {
#if DEBUG
                                Stack[StackPtr].AssertFloatArray();
#endif
                                if (Stack[StackPtr].reference.arrayHandleFloat == null)
                                {
                                    ErrorCode = EvalErrors.eEvalArrayDoesntExist;
                                    goto ExceptionPoint;
                                }
                                int c = Stack[StackPtr].reference.arrayHandleFloat.Length;
                                Stack[StackPtr].ClearArray();
                                Stack[StackPtr].Data.Integer = c;
                            }
                            break;
                        case Pcodes.epGetDoubleArraySize:
                            {
#if DEBUG
                                Stack[StackPtr].AssertDoubleArray();
#endif
                                if (Stack[StackPtr].reference.arrayHandleDouble == null)
                                {
                                    ErrorCode = EvalErrors.eEvalArrayDoesntExist;
                                    goto ExceptionPoint;
                                }
                                int c = Stack[StackPtr].reference.arrayHandleDouble.Length;
                                Stack[StackPtr].ClearArray();
                                Stack[StackPtr].Data.Integer = c;
                            }
                            break;

                        // these are identical because the variance is the virtual method ArrayHandle.Resize()
                        case Pcodes.epResizeByteArray2:
                        case Pcodes.epResizeIntegerArray2:
                        case Pcodes.epResizeFloatArray2:
                        case Pcodes.epResizeDoubleArray2:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertAnyArray();
#endif
                            if (Stack[StackPtr - 1].reference.arrayHandleGeneric == null)
                            {
                                ErrorCode = EvalErrors.eEvalArrayDoesntExist;
                                goto ExceptionPoint;
                            }
                            if (Stack[StackPtr].Data.Integer < 0)
                            {
                                ErrorCode = EvalErrors.eEvalArraySubscriptOutOfRange;
                                goto ExceptionPoint;
                            }
                            Stack[StackPtr - 1].reference.arrayHandleGeneric.Resize(Stack[StackPtr].Data.Integer);
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;

                        case Pcodes.epStoreIntegerOnStack:
                            {
                                /*    -1         0    */
                                /* <opcode> <stackindex> */
#if DEBUG
                                Stack[StackPtr].AssertScalar();
#endif
                                int Index = StackPtr + CurrentProcedure[ProgramCounter + 0].ImmediateInteger;
#if DEBUG
                                Stack[Index].AssertScalar();
#endif
                                Stack[Index].Data.Integer = Stack[StackPtr].Data.Integer;
                                ProgramCounter++;
                                /* don't pop the value from the stack though */
                            }
                            break;
                        case Pcodes.epStoreFloatOnStack:
                            {
                                /*    -1         0    */
                                /* <opcode> <stackindex> */
#if DEBUG
                                Stack[StackPtr].AssertScalar();
#endif
                                int Index = StackPtr + CurrentProcedure[ProgramCounter + 0].ImmediateInteger;
#if DEBUG
                                Stack[Index].AssertScalar();
#endif
                                Stack[Index].Data.Float = Stack[StackPtr].Data.Float;
                                ProgramCounter++;
                                /* don't pop the value from the stack though */
                            }
                            break;
                        case Pcodes.epStoreDoubleOnStack:
                            {
                                /*    -1         0    */
                                /* <opcode> <stackindex> */
#if DEBUG
                                Stack[StackPtr].AssertScalar();
#endif
                                int Index = StackPtr + CurrentProcedure[ProgramCounter + 0].ImmediateInteger;
#if DEBUG
                                Stack[Index].AssertScalar();
#endif
                                Stack[Index].Data.Double = Stack[StackPtr].Data.Double;
                                ProgramCounter++;
                                /* don't pop the value from the stack though */
                            }
                            break;
                        case Pcodes.epStoreArrayOfByteOnStack:
                            {
                                /*    -1         0    */
                                /* <opcode> <stackindex> */
#if DEBUG
                                Stack[StackPtr].AssertByteArray();
#endif
                                int Index = StackPtr + CurrentProcedure[ProgramCounter + 0].ImmediateInteger;
#if DEBUG
                                Stack[Index].AssertByteArray();
#endif
                                Stack[Index].reference.arrayHandleByte = Stack[StackPtr].reference.arrayHandleByte;
                                ProgramCounter++;
                                /* don't pop the value from the stack though */
                            }
                            break;
                        case Pcodes.epStoreArrayOfInt32OnStack:
                            {
                                /*    -1         0    */
                                /* <opcode> <stackindex> */
#if DEBUG
                                Stack[StackPtr].AssertIntegerArray();
#endif
                                int Index = StackPtr + CurrentProcedure[ProgramCounter + 0].ImmediateInteger;
#if DEBUG
                                Stack[Index].AssertIntegerArray();
#endif
                                Stack[Index].reference.arrayHandleInt32 = Stack[StackPtr].reference.arrayHandleInt32;
                                ProgramCounter++;
                                /* don't pop the value from the stack though */
                            }
                            break;
                        case Pcodes.epStoreArrayOfFloatOnStack:
                            {
                                /*    -1         0    */
                                /* <opcode> <stackindex> */
#if DEBUG
                                Stack[StackPtr].AssertFloatArray();
#endif
                                int Index = StackPtr + CurrentProcedure[ProgramCounter + 0].ImmediateInteger;
#if DEBUG
                                Stack[Index].AssertFloatArray();
#endif
                                Stack[Index].reference.arrayHandleFloat = Stack[StackPtr].reference.arrayHandleFloat;
                                ProgramCounter++;
                                /* don't pop the value from the stack though */
                            }
                            break;
                        case Pcodes.epStoreArrayOfDoubleOnStack:
                            {
                                /*    -1         0    */
                                /* <opcode> <stackindex> */
#if DEBUG
                                Stack[StackPtr].AssertDoubleArray();
#endif
                                int Index = StackPtr + CurrentProcedure[ProgramCounter + 0].ImmediateInteger;
#if DEBUG
                                Stack[Index].AssertDoubleArray();
#endif
                                Stack[Index].reference.arrayHandleDouble = Stack[StackPtr].reference.arrayHandleDouble;
                                ProgramCounter++;
                                /* don't pop the value from the stack though */
                            }
                            break;
                        case Pcodes.epLoadIntegerFromStack:
                            {
                                /*    -1         0    */
                                /* <opcode> <stackindex> */
                                int Index = StackPtr + CurrentProcedure[ProgramCounter + 0].ImmediateInteger;
#if DEBUG
                                Stack[Index].AssertScalar();
#endif
                                StackPtr++;
                                Debug.Assert(StackPtr < Stack.Length);
                                Stack[StackPtr].Data.Integer = Stack[Index].Data.Integer;
                                ProgramCounter++;
                            }
                            break;
                        case Pcodes.epLoadFloatFromStack:
                            {
                                /*    -1         0    */
                                /* <opcode> <stackindex> */
                                int Index = StackPtr + CurrentProcedure[ProgramCounter + 0].ImmediateInteger;
#if DEBUG
                                Stack[Index].AssertScalar();
#endif
                                StackPtr++;
                                Debug.Assert(StackPtr < Stack.Length);
                                Stack[StackPtr].Data.Float = Stack[Index].Data.Float;
                                ProgramCounter++;
                            }
                            break;
                        case Pcodes.epLoadDoubleFromStack:
                            {
                                /*    -1         0    */
                                /* <opcode> <stackindex> */
                                int Index = StackPtr + CurrentProcedure[ProgramCounter + 0].ImmediateInteger;
#if DEBUG
                                Stack[Index].AssertScalar();
#endif
                                StackPtr++;
                                Debug.Assert(StackPtr < Stack.Length);
                                Stack[StackPtr].Data.Double = Stack[Index].Data.Double;
                                ProgramCounter++;
                            }
                            break;
                        case Pcodes.epLoadArrayFromStack:
                            {
                                /*    -1         0    */
                                /* <opcode> <stackindex> */
                                int Index = StackPtr + CurrentProcedure[ProgramCounter + 0].ImmediateInteger;
#if DEBUG
                                Stack[Index].AssertAnyArray();
#endif
                                StackPtr++;
                                Debug.Assert(StackPtr < Stack.Length);
                                Stack[StackPtr].reference.arrayHandleGeneric = Stack[Index].reference.arrayHandleGeneric;
#if false // TODO: what was this for?
                                Stack[StackPtr].reference.generic = Stack[Index].reference.generic;
#endif
                                ProgramCounter++;
                            }
                            break;

                        // TODO: would be more efficient to generate a Pcodes.epStackPopScalar when datatype is known

                        case Pcodes.epStackPop:
                            Stack[StackPtr].Clear();
                            StackPtr--;
                            break;

                        case Pcodes.epStackPopMultiple:
                            {
                                /*     -1       0   */
                                /* <opcode> <numwords> */
                                int Index = CurrentProcedure[ProgramCounter + 0].ImmediateInteger;
                                while (Index > 0)
                                {
                                    Stack[StackPtr].Clear();
                                    StackPtr--;
                                    Index--;
                                }
                                ProgramCounter++;
                            }
                            break;

                        /* deallocate multiple cells under the current top */
                        case Pcodes.epStackDeallocateUnder:
                            {
                                /*    -1        0   */
                                /* <opcode> <numwords> */

                                /* get the number of cells to deallocate */
                                Debug.Assert(CurrentProcedure[ProgramCounter + 0].ImmediateInteger > 0);
                                int Index = CurrentProcedure[ProgramCounter + 0].ImmediateInteger;
                                int OldStackPtr = StackPtr; /* save old stack pointer */
                                while (Index > 0)
                                {
                                    Stack[StackPtr - 1].Clear();
                                    StackPtr--;
                                    Index--;
                                }
                                Stack[StackPtr] = Stack[OldStackPtr]; /* move top element to new top */
                                Stack[OldStackPtr] = new StackElement();
                                ProgramCounter++;
                            }
                            break;

                        /* duplicate the top word of the stack */
                        case Pcodes.epDuplicate:
                            StackPtr++;
                            Debug.Assert(StackPtr < Stack.Length);
                            Stack[StackPtr].CopyFrom(ref Stack[StackPtr - 1]);
                            break;

                        case Pcodes.epNop:
                            break;

                        /* new array allocation procedures */
                        case Pcodes.epMakeByteArray:
                            {
#if DEBUG
                                Stack[StackPtr].AssertScalar();
#endif
                                int length = Stack[StackPtr].Data.Integer;
                                Stack[StackPtr].ClearScalar();
                                Stack[StackPtr].reference.arrayHandleByte = new ArrayHandleByte(new byte[length]);
                            }
                            break;
                        case Pcodes.epMakeIntegerArray:
                            {
#if DEBUG
                                Stack[StackPtr].AssertScalar();
#endif
                                int length = Stack[StackPtr].Data.Integer;
                                Stack[StackPtr].ClearScalar();
                                Stack[StackPtr].reference.arrayHandleInt32 = new ArrayHandleInt32(new int[length]);
                            }
                            break;
                        case Pcodes.epMakeFloatArray:
                            {
#if DEBUG
                                Stack[StackPtr].AssertScalar();
#endif
                                int length = Stack[StackPtr].Data.Integer;
                                Stack[StackPtr].ClearScalar();
                                Stack[StackPtr].reference.arrayHandleFloat = new ArrayHandleFloat(new float[length]);
                            }
                            break;
                        case Pcodes.epMakeDoubleArray:
                            {
#if DEBUG
                                Stack[StackPtr].AssertScalar();
#endif
                                int length = Stack[StackPtr].Data.Integer;
                                Stack[StackPtr].ClearScalar();
                                Stack[StackPtr].reference.arrayHandleDouble = new ArrayHandleDouble(new double[length]);
                            }
                            break;
                        case Pcodes.epMakeByteArrayFromString: /* <opcode> ^"<data>" */
                            StackPtr++;
                            Debug.Assert(StackPtr < Stack.Length);
                            Stack[StackPtr].ClearScalar();
                            Stack[StackPtr].reference.arrayHandleByte = new ArrayHandleByte(
                                CurrentProcedurePcode.strings[CurrentProcedure[ProgramCounter + 0].ImmediateString_Ref]);
                            ProgramCounter++;
                            break;

                        case Pcodes.epStoreByteIntoArray2:
                            {
#if DEBUG
                                Stack[StackPtr].AssertScalar();
                                Stack[StackPtr - 1].AssertByteArray();
                                Stack[StackPtr - 2].AssertScalar();
#endif
                                if (Stack[StackPtr - 1].reference.arrayHandleByte == null)
                                {
                                    ErrorCode = EvalErrors.eEvalArrayDoesntExist;
                                    goto ExceptionPoint;
                                }
                                int offset = Stack[StackPtr].Data.Integer;
                                if (unchecked((uint)offset >= (uint)Stack[StackPtr - 1].reference.arrayHandleByte.Length))
                                {
                                    ErrorCode = EvalErrors.eEvalArraySubscriptOutOfRange;
                                    goto ExceptionPoint;
                                }
                                Stack[StackPtr - 1].reference.arrayHandleByte.bytes[offset]
                                    = unchecked((byte)Stack[StackPtr - 2].Data.Integer);
                                Stack[StackPtr].ClearScalar();
                                Stack[StackPtr - 1].ClearArray();
                                StackPtr -= 2; /* pop subscript and reference */
                            }
                            break;
                        case Pcodes.epStoreIntegerIntoArray2:
                            {
#if DEBUG
                                Stack[StackPtr].AssertScalar();
                                Stack[StackPtr - 1].AssertIntegerArray();
                                Stack[StackPtr - 2].AssertScalar();
#endif
                                if (Stack[StackPtr - 1].reference.arrayHandleInt32 == null)
                                {
                                    ErrorCode = EvalErrors.eEvalArrayDoesntExist;
                                    goto ExceptionPoint;
                                }
                                int offset = Stack[StackPtr].Data.Integer;
                                if (unchecked((uint)offset >= (uint)Stack[StackPtr - 1].reference.arrayHandleInt32.Length))
                                {
                                    ErrorCode = EvalErrors.eEvalArraySubscriptOutOfRange;
                                    goto ExceptionPoint;
                                }
                                Stack[StackPtr - 1].reference.arrayHandleInt32.ints[offset] = Stack[StackPtr - 2].Data.Integer;
                                Stack[StackPtr].ClearScalar();
                                Stack[StackPtr - 1].ClearArray();
                                StackPtr -= 2; /* pop subscript and reference */
                            }
                            break;
                        case Pcodes.epStoreFloatIntoArray2:
                            {
#if DEBUG
                                Stack[StackPtr].AssertScalar();
                                Stack[StackPtr - 1].AssertFloatArray();
                                Stack[StackPtr - 2].AssertScalar();
#endif
                                if (Stack[StackPtr - 1].reference.arrayHandleFloat == null)
                                {
                                    ErrorCode = EvalErrors.eEvalArrayDoesntExist;
                                    goto ExceptionPoint;
                                }
                                int offset = Stack[StackPtr].Data.Integer;
                                if (unchecked((uint)offset >= (uint)Stack[StackPtr - 1].reference.arrayHandleFloat.Length))
                                {
                                    ErrorCode = EvalErrors.eEvalArraySubscriptOutOfRange;
                                    goto ExceptionPoint;
                                }
                                Stack[StackPtr - 1].reference.arrayHandleFloat.floats[offset] = Stack[StackPtr - 2].Data.Float;
                                Stack[StackPtr].ClearScalar();
                                Stack[StackPtr - 1].ClearArray();
                                StackPtr -= 2; /* pop subscript and reference */
                            }
                            break;
                        case Pcodes.epStoreDoubleIntoArray2:
                            {
#if DEBUG
                                Stack[StackPtr].AssertScalar();
                                Stack[StackPtr - 1].AssertDoubleArray();
                                Stack[StackPtr - 2].AssertScalar();
#endif
                                if (Stack[StackPtr - 1].reference.arrayHandleDouble == null)
                                {
                                    ErrorCode = EvalErrors.eEvalArrayDoesntExist;
                                    goto ExceptionPoint;
                                }
                                int offset = Stack[StackPtr].Data.Integer;
                                if (unchecked((uint)offset >= (uint)Stack[StackPtr - 1].reference.arrayHandleDouble.Length))
                                {
                                    ErrorCode = EvalErrors.eEvalArraySubscriptOutOfRange;
                                    goto ExceptionPoint;
                                }
                                Stack[StackPtr - 1].reference.arrayHandleDouble.doubles[offset] = Stack[StackPtr - 2].Data.Double;
                                Stack[StackPtr].ClearScalar();
                                Stack[StackPtr - 1].ClearArray();
                                StackPtr -= 2; /* pop subscript and reference */
                            }
                            break;

                        case Pcodes.epLoadByteFromArray2:
                            {
#if DEBUG
                                Stack[StackPtr].AssertScalar();
                                Stack[StackPtr - 1].AssertByteArray();
#endif
                                if (Stack[StackPtr - 1].reference.arrayHandleByte == null)
                                {
                                    ErrorCode = EvalErrors.eEvalArrayDoesntExist;
                                    goto ExceptionPoint;
                                }
                                int offset = Stack[StackPtr].Data.Integer;
                                if (unchecked((uint)offset >= (uint)Stack[StackPtr - 1].reference.arrayHandleByte.Length))
                                {
                                    ErrorCode = EvalErrors.eEvalArraySubscriptOutOfRange;
                                    goto ExceptionPoint;
                                }
                                byte temp = Stack[StackPtr - 1].reference.arrayHandleByte.bytes[offset];
                                Stack[StackPtr].ClearScalar();
                                StackPtr--;
                                Stack[StackPtr].ClearArray();
                                Stack[StackPtr].Data.Integer = temp;
                            }
                            break;
                        case Pcodes.epLoadIntegerFromArray2:
                            {
#if DEBUG
                                Stack[StackPtr].AssertScalar();
                                Stack[StackPtr - 1].AssertIntegerArray();
#endif
                                if (Stack[StackPtr - 1].reference.arrayHandleInt32 == null)
                                {
                                    ErrorCode = EvalErrors.eEvalArrayDoesntExist;
                                    goto ExceptionPoint;
                                }
                                int offset = Stack[StackPtr].Data.Integer;
                                if (unchecked((uint)offset >= (uint)Stack[StackPtr - 1].reference.arrayHandleInt32.Length))
                                {
                                    ErrorCode = EvalErrors.eEvalArraySubscriptOutOfRange;
                                    goto ExceptionPoint;
                                }
                                int temp = Stack[StackPtr - 1].reference.arrayHandleInt32.ints[offset];
                                Stack[StackPtr].ClearScalar();
                                StackPtr--;
                                Stack[StackPtr].ClearArray();
                                Stack[StackPtr].Data.Integer = temp;
                            }
                            break;
                        case Pcodes.epLoadFloatFromArray2:
                            {
#if DEBUG
                                Stack[StackPtr].AssertScalar();
                                Stack[StackPtr - 1].AssertFloatArray();
#endif
                                if (Stack[StackPtr - 1].reference.arrayHandleFloat == null)
                                {
                                    ErrorCode = EvalErrors.eEvalArrayDoesntExist;
                                    goto ExceptionPoint;
                                }
                                int offset = Stack[StackPtr].Data.Integer;
                                if (unchecked((uint)offset >= (uint)Stack[StackPtr - 1].reference.arrayHandleFloat.Length))
                                {
                                    ErrorCode = EvalErrors.eEvalArraySubscriptOutOfRange;
                                    goto ExceptionPoint;
                                }
                                float temp = Stack[StackPtr - 1].reference.arrayHandleFloat.floats[offset];
                                Stack[StackPtr].ClearScalar();
                                StackPtr--;
                                Stack[StackPtr].ClearArray();
                                Stack[StackPtr].Data.Float = temp;
                            }
                            break;
                        case Pcodes.epLoadDoubleFromArray2:
                            {
#if DEBUG
                                Stack[StackPtr].AssertScalar();
                                Stack[StackPtr - 1].AssertDoubleArray();
#endif
                                if (Stack[StackPtr - 1].reference.arrayHandleDouble == null)
                                {
                                    ErrorCode = EvalErrors.eEvalArrayDoesntExist;
                                    goto ExceptionPoint;
                                }
                                int offset = Stack[StackPtr].Data.Integer;
                                if (unchecked((uint)offset >= (uint)Stack[StackPtr - 1].reference.arrayHandleDouble.Length))
                                {
                                    ErrorCode = EvalErrors.eEvalArraySubscriptOutOfRange;
                                    goto ExceptionPoint;
                                }
                                double temp = Stack[StackPtr - 1].reference.arrayHandleDouble.doubles[offset];
                                Stack[StackPtr].ClearScalar();
                                StackPtr--;
                                Stack[StackPtr].ClearArray();
                                Stack[StackPtr].Data.Double = temp;
                            }
                            break;

                        /* load immediate values */
                        case Pcodes.epLoadImmediateInteger:
                            /*    -1        0   */
                            /* <opcode> <integer>; also used for boolean & fixed */
                            StackPtr++;
                            Debug.Assert(StackPtr < Stack.Length);
                            Stack[StackPtr].Data.Integer
                                = CurrentProcedure[ProgramCounter + 0].ImmediateInteger;
                            ProgramCounter++;
                            break;
                        case Pcodes.epLoadImmediateFloat:
                            /*    -1        0   */
                            /* <opcode> <integer>; also used for boolean & fixed */
                            StackPtr++;
                            Debug.Assert(StackPtr < Stack.Length);
                            Stack[StackPtr].Data.Float
                                = CurrentProcedure[ProgramCounter + 0].ImmediateFloat;
                            ProgramCounter++;
                            break;
                        case Pcodes.epLoadImmediateDouble:
                            /*    -1        0   */
                            /* <opcode> <integer>; also used for boolean & fixed */
                            StackPtr++;
                            Debug.Assert(StackPtr < Stack.Length);
                            Stack[StackPtr].Data.Double
                                = CurrentProcedureDoubles[CurrentProcedure[ProgramCounter + 0].ImmediateDouble_Ref];
                            ProgramCounter++;
                            break;
                        case Pcodes.epLoadImmediateNILArray:
                            /* <opcode> */
                            StackPtr++;
                            Debug.Assert(StackPtr < Stack.Length);
                            break;

                        case Pcodes.epCopyArrayByte:
                            {
#if DEBUG
                                Stack[StackPtr].AssertAnyArray();
#endif
                                if (Stack[StackPtr].reference.arrayHandleGeneric == null)
#if false // TODO: what was this for?
                                if (Stack[StackPtr].reference.generic == null)
#endif
                                {
                                    ErrorCode = EvalErrors.eEvalArrayDoesntExist;
                                    goto ExceptionPoint;
                                }
                                Debug.Assert(Stack[StackPtr].reference.generic is ArrayHandleByte);
                                Stack[StackPtr].reference.arrayHandleByte
                                    = (ArrayHandleByte)Stack[StackPtr].reference.arrayHandleByte.Duplicate();
                            }
                            break;
                        case Pcodes.epCopyArrayInteger:
                            {
#if DEBUG
                                Stack[StackPtr].AssertAnyArray();
#endif
                                if (Stack[StackPtr].reference.arrayHandleGeneric == null)
#if false // TODO: what was this for?
                                if (Stack[StackPtr].reference.generic == null)
#endif
                                {
                                    ErrorCode = EvalErrors.eEvalArrayDoesntExist;
                                    goto ExceptionPoint;
                                }
                                Debug.Assert(Stack[StackPtr].reference.generic is ArrayHandleInt32);
                                Stack[StackPtr].reference.arrayHandleInt32
                                    = (ArrayHandleInt32)Stack[StackPtr].reference.arrayHandleInt32.Duplicate();
                            }
                            break;
                        case Pcodes.epCopyArrayFloat:
                            {
#if DEBUG
                                Stack[StackPtr].AssertAnyArray();
#endif
                                if (Stack[StackPtr].reference.arrayHandleGeneric == null)
#if false // TODO: what was this for?
                                if (Stack[StackPtr].reference.generic == null)
#endif
                                {
                                    ErrorCode = EvalErrors.eEvalArrayDoesntExist;
                                    goto ExceptionPoint;
                                }
                                Debug.Assert(Stack[StackPtr].reference.generic is ArrayHandleFloat);
                                Stack[StackPtr].reference.arrayHandleFloat
                                    = (ArrayHandleFloat)Stack[StackPtr].reference.arrayHandleFloat.Duplicate();
                            }
                            break;
                        case Pcodes.epCopyArrayDouble:
                            {
#if DEBUG
                                Stack[StackPtr].AssertAnyArray();
#endif
                                if (Stack[StackPtr].reference.arrayHandleGeneric == null)
#if false // TODO: what was this for?
                                if (Stack[StackPtr].reference.generic == null)
#endif
                                {
                                    ErrorCode = EvalErrors.eEvalArrayDoesntExist;
                                    goto ExceptionPoint;
                                }
                                Debug.Assert(Stack[StackPtr].reference.generic is ArrayHandleDouble);
                                Stack[StackPtr].reference.arrayHandleDouble
                                    = (ArrayHandleDouble)Stack[StackPtr].reference.arrayHandleDouble.Duplicate();
                            }
                            break;

                        case Pcodes.epMinInt:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer = Math.Min(Stack[StackPtr - 1].Data.Integer, Stack[StackPtr].Data.Integer);
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epMinFloat:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Float = Math.Min(Stack[StackPtr - 1].Data.Float, Stack[StackPtr].Data.Float);
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epMinDouble:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Double = Math.Min(Stack[StackPtr - 1].Data.Double, Stack[StackPtr].Data.Double);
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epMaxInt:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Integer = Math.Max(Stack[StackPtr - 1].Data.Integer, Stack[StackPtr].Data.Integer);
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epMaxFloat:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Float = Math.Max(Stack[StackPtr - 1].Data.Float, Stack[StackPtr].Data.Float);
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epMaxDouble:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Double = Math.Max(Stack[StackPtr - 1].Data.Double, Stack[StackPtr].Data.Double);
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;

                        case Pcodes.epMinMaxInt:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
                            Stack[StackPtr - 2].AssertScalar();
#endif
                            Stack[StackPtr - 2].Data.Integer = Math.Min(Math.Max(Stack[StackPtr - 2].Data.Integer, Stack[StackPtr - 1].Data.Integer), Stack[StackPtr].Data.Integer);
                            Stack[StackPtr].ClearScalar();
                            Stack[StackPtr - 1].ClearScalar();
                            StackPtr -= 2;
                            break;
                        case Pcodes.epMinMaxFloat:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
                            Stack[StackPtr - 2].AssertScalar();
#endif
                            Stack[StackPtr - 2].Data.Float = Math.Min(Math.Max(Stack[StackPtr - 2].Data.Float, Stack[StackPtr - 1].Data.Float), Stack[StackPtr].Data.Float);
                            Stack[StackPtr].ClearScalar();
                            Stack[StackPtr - 1].ClearScalar();
                            StackPtr -= 2;
                            break;
                        case Pcodes.epMinMaxDouble:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
                            Stack[StackPtr - 2].AssertScalar();
#endif
                            Stack[StackPtr - 2].Data.Double = Math.Min(Math.Max(Stack[StackPtr - 2].Data.Double, Stack[StackPtr - 1].Data.Double), Stack[StackPtr].Data.Double);
                            Stack[StackPtr].ClearScalar();
                            Stack[StackPtr - 1].ClearScalar();
                            StackPtr -= 2;
                            break;
                        case Pcodes.epAtan2Float:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Float = (float)Math.Atan2(Stack[StackPtr - 1].Data.Float, Stack[StackPtr].Data.Float);
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;
                        case Pcodes.epAtan2Double:
#if DEBUG
                            Stack[StackPtr].AssertScalar();
                            Stack[StackPtr - 1].AssertScalar();
#endif
                            Stack[StackPtr - 1].Data.Double = Math.Atan2(Stack[StackPtr - 1].Data.Double, Stack[StackPtr].Data.Double);
                            Stack[StackPtr].ClearScalar();
                            StackPtr--;
                            break;

                    } /* end switch */
                } /* end while */


                /* when something bad happens, set the ErrorCode with the error code and */
                /* jump here.  this will release all allocated arrays on the stack and in */
                /* the registers, set the offending function pcode, and return */
                ;
            ExceptionPoint:

                // in error case, clear entire stack
                while (StackPtr >= 0)
                {
                    Stack[StackPtr].Clear();
                    StackPtr--;
                }
#if DEBUG
                for (int i = StackPtr + 1; i < Stack.Length; i++)
                {
                    Stack[i].AssertClear();
                }
#endif

                /* then set up the error values */
                ErrorInfo.OffendingPcode = CurrentProcedure;
                if (ProgramCounter - 1 >= 0)
                {
                    ErrorInfo.OffendingInstruction = ProgramCounter - 1;
                }
                else
                {
                    ErrorInfo.OffendingInstruction = 0;
                }

                /* write back values that might have changed */
                Prep.UpdateRawStack(Stack, StackPtr + 1);
                return ErrorCode;


                /* when execution finishes, jump here.  The lowest element [0] in the stack will */
                /* have the return value, placed there according to calling conventions */
                ;
            TotallyDonePoint:

                /* write back values that might have changed */
                Prep.UpdateRawStack(Stack, StackPtr + 1);

                /* return message that indicates everything went fine */
                return EvalErrors.eEvalNoError;
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();

                ErrorInfo.OffendingPcode = CurrentProcedure;
                if (ProgramCounter - 1 >= 0)
                {
                    ErrorInfo.OffendingInstruction = ProgramCounter - 1;
                }
                else
                {
                    ErrorInfo.OffendingInstruction = 0;
                }

                return EvalErrors.eEvalUserCancelled;
            }
            finally
            {
                int c = Interlocked.Decrement(ref threadContext.InEval);
                Debug.Assert(c >= 0);
            }
        }
    }
}
