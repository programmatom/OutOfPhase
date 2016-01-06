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
        /* types of statements */
        public enum FMSynthStmtType
        {
            eFMSynthWave,
            eFMSynthMuladd,
            eFMSynthEnvelope,
        }

        /* default outputs */
        public const int FMSYNTH_ZERO = 0;
        public const int FMSYNTH_ONE = 1;
        public const int FMSYNTH_LEFT = 2;
        public const int FMSYNTH_RIGHT = 3;

        public class FMSynthStmtRootRec
        {
            public FMSynthStmtType Type;
            public FMSynthSpecRec Owner;

            /* flag -- True means the statement can be evaluated once at the beginning */
            /* of each cycle rather than for each sample frame. */
            public bool Optimize;

            /* target variable for each statement */
            public int Target;
        }

        public class FMSynthStmtWaveRec : FMSynthStmtRootRec
        {
            public double FrequencyMultiplier;
            public double FrequencyAdder;
            public int FrequencyDivisor;
            public int PhaseSource;
            public int PhaseGainSource;
            public EnvelopeRec IndexEnvelope;
            public LFOListSpecRec IndexLFOList;
            public SampleSelectorRec SampleSelector;
        }

        public class FMSynthStmtMulAddRec : FMSynthStmtRootRec
        {
            public int Source;
            public int Source2;
            public int Factor2;
            public double Factor;
            public double Addend;
        }

        public class FMSynthStmtEnvelopeRec : FMSynthStmtRootRec
        {
            public EnvelopeRec Envelope;
            public LFOListSpecRec LFOList;
        }

        public class FMSynthSpecRec
        {
            /* array of statement objects (FMSynthStmtRootRec*, derived types) */
            public FMSynthStmtRootRec[] Stmts;

            /* array of copied names of variables.  during optimization, NILs may be inserted */
            /* here, but things like EnsureVariable should never be called after optimization. */
            public string[] Variables;
        }

        /* helper method -- ensure variable exists, return it's index */
        public static void EnsureVariable(
            FMSynthSpecRec Spec,
            string Name,
            out int IndexOut)
        {
            /* look for existing */
            for (int i = 2/*never match 'zero' or 'one' which are first */; i < Spec.Variables.Length; i++)
            {
                if (String.Equals(Name, Spec.Variables[i]))
                {
                    IndexOut = i;
                    return;
                }
            }

            // not found - add
            IndexOut = Spec.Variables.Length;
            Array.Resize(ref Spec.Variables, Spec.Variables.Length + 1);
            Spec.Variables[IndexOut] = Name;
        }

        /* create new fm synth specification record */
        public static FMSynthSpecRec NewFMSynthSpec()
        {
            FMSynthSpecRec Spec = new FMSynthSpecRec();

            Spec.Stmts = new FMSynthStmtRootRec[0];
            Spec.Variables = new string[0];

            int i;
            EnsureVariable(Spec, "zero", out i); /* must be first */
            Debug.Assert(i == 0);
            EnsureVariable(Spec, "one", out i); /* must be second */
            Debug.Assert(i == 1);
            EnsureVariable(Spec, "left", out i);
            Debug.Assert(i == 2);
            EnsureVariable(Spec, "right", out i);
            Debug.Assert(i == 3);

            return Spec;
        }

        /* how many statements */
        public static int FMSynthGetNumStatements(FMSynthSpecRec Spec)
        {
            return Spec.Stmts.Length;
        }

        /* how many statements can be optimized */
        public static int FMSynthGetNumOptimizableStatements(FMSynthSpecRec Spec)
        {
            int Optimizable = 0;
            for (int i = 0; i < Spec.Stmts.Length; i += 1)
            {
                if (Spec.Stmts[i].Optimize)
                {
                    Optimizable++;
                }
            }
            return Optimizable;
        }

        /* get type of statement */
        public static FMSynthStmtType FMSynthGetStatementType(
            FMSynthSpecRec Spec,
            int Index)
        {
            return Spec.Stmts[Index].Type;
        }

        public static FMSynthStmtWaveRec FMSynthGetWaveStatement(
            FMSynthSpecRec Spec,
            int Index)
        {
#if DEBUG
            if ((Spec.Stmts[Index].Type != FMSynthStmtType.eFMSynthWave)
                || !(Spec.Stmts[Index] is FMSynthStmtWaveRec))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return (FMSynthStmtWaveRec)Spec.Stmts[Index];
        }

        public static FMSynthStmtMulAddRec FMSynthGetMulAddStatement(
            FMSynthSpecRec Spec,
            int Index)
        {
#if DEBUG
            if ((Spec.Stmts[Index].Type != FMSynthStmtType.eFMSynthMuladd)
                || !(Spec.Stmts[Index] is FMSynthStmtMulAddRec))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return (FMSynthStmtMulAddRec)Spec.Stmts[Index];
        }

        public static FMSynthStmtEnvelopeRec FMSynthGetEnvelopeStatement(
            FMSynthSpecRec Spec,
            int Index)
        {
#if DEBUG
            if ((Spec.Stmts[Index].Type != FMSynthStmtType.eFMSynthEnvelope)
                || !(Spec.Stmts[Index] is FMSynthStmtEnvelopeRec))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return (FMSynthStmtEnvelopeRec)Spec.Stmts[Index];
        }

        /* is this statement optimizable */
        public static bool FMSynthStmtCanBeOptimized(
            FMSynthSpecRec Spec,
            int Index)
        {
            return Spec.Stmts[Index].Optimize;
        }

        /* get total number of variables */
        public static int FMSynthGetNumVariables(FMSynthSpecRec Spec)
        {
            return Spec.Variables.Length;
        }

        /* accessors to add a statement */
        public static void FMSynthAddStatement(
            FMSynthSpecRec Spec,
            FMSynthStmtType Type)
        {
            FMSynthStmtRootRec Base;

            switch (Type)
            {
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();

                case FMSynthStmtType.eFMSynthWave:
                    FMSynthStmtWaveRec WaveStmt = new FMSynthStmtWaveRec();
                    Base = WaveStmt;

                    WaveStmt.Type = FMSynthStmtType.eFMSynthWave;
                    WaveStmt.Owner = Spec;

                    WaveStmt.FrequencyMultiplier = 1;
                    WaveStmt.FrequencyAdder = 0;
                    WaveStmt.FrequencyDivisor = 1;
                    WaveStmt.Target = -1;
                    WaveStmt.PhaseSource = FMSYNTH_ZERO;
                    WaveStmt.PhaseGainSource = FMSYNTH_ONE;
                    WaveStmt.SampleSelector = NewSampleSelectorList(0);
                    WaveStmt.IndexEnvelope = NewEnvelope();
                    WaveStmt.IndexLFOList = NewLFOListSpecifier();

                    break;

                case FMSynthStmtType.eFMSynthMuladd:
                    FMSynthStmtMulAddRec MuladdStmt = new FMSynthStmtMulAddRec();
                    Base = MuladdStmt;

                    MuladdStmt.Type = FMSynthStmtType.eFMSynthMuladd;
                    MuladdStmt.Owner = Spec;

                    MuladdStmt.Source = FMSYNTH_ZERO;
                    MuladdStmt.Source2 = FMSYNTH_ZERO;
                    MuladdStmt.Target = -1;
                    MuladdStmt.Factor = 1;
                    MuladdStmt.Addend = 0;
                    MuladdStmt.Factor2 = FMSYNTH_ONE;

                    break;

                case FMSynthStmtType.eFMSynthEnvelope:
                    FMSynthStmtEnvelopeRec EnvStmt = new FMSynthStmtEnvelopeRec();
                    Base = EnvStmt;

                    EnvStmt.Type = FMSynthStmtType.eFMSynthEnvelope;
                    EnvStmt.Owner = Spec;

                    EnvStmt.Target = -1;
                    EnvStmt.Envelope = NewEnvelope();
                    EnvStmt.LFOList = NewLFOListSpecifier();

                    break;
            }

            Array.Resize(ref Spec.Stmts, Spec.Stmts.Length + 1);
            Spec.Stmts[Spec.Stmts.Length - 1] = Base;
        }

        /* get the frequency multiplier factor */
        public static double FMSynthWaveStmtGetFrequencyMultiplier(FMSynthStmtWaveRec Spec)
        {
            return Spec.FrequencyMultiplier;
        }

        /* get the frequency divisor integer */
        public static long FMSynthWaveStmtGetFrequencyDivisor(FMSynthStmtWaveRec Spec)
        {
            return Spec.FrequencyDivisor;
        }

        /* get the frequency adder thing */
        public static double FMSynthWaveStmtGetFrequencyAdder(FMSynthStmtWaveRec Spec)
        {
            return Spec.FrequencyAdder;
        }

        /* change the frequency adjust factors */
        public static void FMSynthWaveStmtPutFrequencyMultiplier(
            FMSynthStmtWaveRec Spec,
            double NewMultipler)
        {
            Spec.FrequencyMultiplier = NewMultipler;
        }

        /* change the frequency adjust factors */
        public static void FMSynthWaveStmtPutFrequencyDivisor(
            FMSynthStmtWaveRec Spec,
            int NewDivisor)
        {
            Spec.FrequencyDivisor = NewDivisor;
        }

        /* put a new frequency adder value */
        public static void FMSynthWaveStmtPutFrequencyAdder(
            FMSynthStmtWaveRec Spec,
            double NewAdder)
        {
            Spec.FrequencyAdder = NewAdder;
        }

        /* get target */
        public static int FMSynthWaveStmtGetTarget(FMSynthStmtWaveRec Spec)
        {
            return Spec.Target;
        }

        /* put target */
        public static void FMSynthWaveStmtPutTarget(
            FMSynthStmtWaveRec Spec,
            string Target)
        {
            EnsureVariable(
                Spec.Owner,
                Target,
                out Spec.Target);
        }

        /* get phase source */
        public static int FMSynthWaveStmtGetPhaseSource(FMSynthStmtWaveRec Spec)
        {
            return Spec.PhaseSource;
        }

        /* put phase source */
        public static void FMSynthWaveStmtPutPhaseSource(
            FMSynthStmtWaveRec Spec,
            string PhaseSource)
        {
            EnsureVariable(
                Spec.Owner,
                PhaseSource,
                out Spec.PhaseSource);
        }

        /* get phase gain source */
        public static int FMSynthWaveStmtGetPhaseGainSource(FMSynthStmtWaveRec Spec)
        {
            return Spec.PhaseGainSource;
        }

        /* put phase gain source */
        public static void FMSynthWaveStmtPutPhaseGainSource(
                                                                FMSynthStmtWaveRec Spec,
                                                                string PhaseGainSource)
        {
            EnsureVariable(
                Spec.Owner,
                PhaseGainSource,
                out Spec.PhaseGainSource);
        }

        /* get the wavetable index envelope for the wave */
        public static EnvelopeRec FMSynthWaveStmtGetIndexEnvelope(FMSynthStmtWaveRec Spec)
        {
            return Spec.IndexEnvelope;
        }

        /* get the wavetable index LFO for the wave */
        public static LFOListSpecRec FMSynthWaveStmtGetIndexLFOList(FMSynthStmtWaveRec Spec)
        {
            return Spec.IndexLFOList;
        }

        /* get the pitch interval -. sample mapping */
        public static SampleSelectorRec FMSynthWaveStmtGetSampleIntervalList(FMSynthStmtWaveRec Spec)
        {
            return Spec.SampleSelector;
        }

        /* put data source */
        public static void FMSynthMulAddStmtPutDataSource(
            FMSynthStmtMulAddRec Spec,
            string DataSource)
        {
            EnsureVariable(
                Spec.Owner,
                DataSource,
                out Spec.Source);
        }

        /* get data source */
        public static int FMSynthMulAddStmtGetDataSource(FMSynthStmtMulAddRec Spec)
        {
            return Spec.Source;
        }

        /* put data target */
        public static void FMSynthMulAddStmtPutDataTarget(
            FMSynthStmtMulAddRec Spec,
            string DataTarget)
        {
            EnsureVariable(
                Spec.Owner,
                DataTarget,
                out Spec.Target);
        }

        /* get data target */
        public static int FMSynthMulAddStmtGetDataTarget(FMSynthStmtMulAddRec Spec)
        {
            return Spec.Target;
        }

        /* put factor2 */
        public static void FMSynthMulAddStmtPutDataFactor2(
            FMSynthStmtMulAddRec Spec,
            string DataFactor2)
        {
            EnsureVariable(
                Spec.Owner,
                DataFactor2,
                out Spec.Factor2);
        }

        /* get factor2 */
        public static int FMSynthMulAddStmtGetDataFactor2(FMSynthStmtMulAddRec Spec)
        {
            return Spec.Factor2;
        }

        /* put source2 */
        public static void FMSynthMulAddStmtPutDataSource2(
            FMSynthStmtMulAddRec Spec,
            string DataSource2)
        {
            EnsureVariable(
                Spec.Owner,
                DataSource2,
                out Spec.Source2);
        }

        /* get source2 */
        public static int FMSynthMulAddStmtGetDataSource2(FMSynthStmtMulAddRec Spec)
        {
            return Spec.Source2;
        }

        /* get the factor */
        public static double FMSynthMulAddStmtGetFactor(FMSynthStmtMulAddRec Spec)
        {
            return Spec.Factor;
        }

        /* change the factor */
        public static void FMSynthMulAddStmtPutFactor(
            FMSynthStmtMulAddRec Spec,
            double Factor)
        {
            Spec.Factor = Factor;
        }

        /* get the addend */
        public static double FMSynthMulAddStmtGetAddend(FMSynthStmtMulAddRec Spec)
        {
            return Spec.Addend;
        }

        /* change the addend */
        public static void FMSynthMulAddStmtPutAddend(
            FMSynthStmtMulAddRec Spec,
            double Addend)
        {
            Spec.Addend = Addend;
        }

        /* put data target */
        public static void FMSynthEnvelopeStmtPutDataTarget(
            FMSynthStmtEnvelopeRec Spec,
            string DataTarget)
        {
            EnsureVariable(
                Spec.Owner,
                DataTarget,
                out Spec.Target);
        }

        /* get data target */
        public static int FMSynthEnvelopeStmtGetDataTarget(FMSynthStmtEnvelopeRec Spec)
        {
            return Spec.Target;
        }

        /* get the wavetable index envelope for the wave */
        public static EnvelopeRec FMSynthEnvelopeStmtGetEnvelope(FMSynthStmtEnvelopeRec Spec)
        {
            return Spec.Envelope;
        }

        /* get the wavetable index LFO for the wave */
        public static LFOListSpecRec FMSynthEnvelopeStmtGetLFOList(FMSynthStmtEnvelopeRec Spec)
        {
            return Spec.LFOList;
        }



        private enum FMSynthOptRate
        {
            FMSYNTH_CONTROLRATE = 0,
            FMSYNTH_DATARATE = 1,
        }

        /* apply optimizations */
        public static void FMSynthOptimize(FMSynthSpecRec Spec)
        {
            /* reference disambiguation */

            /* there may be multiple non-overlapping lifetimes for the same variable.  if these */
            /* are not separated, then data rate of one lifetime will apply to all lifetimes, */
            /* resulting in suboptimal performance. */
            /* each update creates a new lifetime for the variable.  while there are more than */
            /* one update of a given variable, rename the 1st update and all references. */

            int cStmts = Spec.Stmts.Length;
            int cVars = Spec.Variables.Length;
            for (int i = 4/*zero and one are never written, and refs to left, right vars can't be split*/;
                i < cVars/*note: vars we add here should never have multiple updates*/;
                i++)
            {
                /* find first update */
                int iFirstUpdate = 0;
                while (iFirstUpdate < cStmts)
                {
                    if (Spec.Stmts[iFirstUpdate].Target == i)
                    {
                        break;
                    }
                    iFirstUpdate++;
                }
                if (iFirstUpdate == cStmts)
                {
                    /* no updates at all, so don't do anything */
                    continue;
                }

                /* find second update */
                int iSecondUpdate = (iFirstUpdate + 1) % cStmts;
                while (iSecondUpdate != iFirstUpdate)
                {
                    if (Spec.Stmts[iSecondUpdate].Target == i)
                    {
                        break;
                    }
                    iSecondUpdate = (iSecondUpdate + 1) % cStmts;
                }
                if (iSecondUpdate == iFirstUpdate)
                {
                    /* only one update, so don't do anything */
                    continue;
                }

                /* create new anonymous variable */
                int iNewVariable = Spec.Variables.Length;
                Array.Resize(ref Spec.Variables, Spec.Variables.Length + 1);

                /* walk stmt range and update all references */
                int j = iFirstUpdate;
                //goto StartFirstIteration; /* loop is inclusive on both ends */
                j--; // C# can't jump into middle of loop body - so counteract first increment of j
                do
                {
                    j = (j + 1) % cStmts;

                StartFirstIteration:
                    FMSynthStmtRootRec Stmt = Spec.Stmts[j];

                    /* for first statement, only update the target */
                    if (j == iFirstUpdate)
                    {
#if DEBUG
                        if (Stmt.Target != i)
                        {
                            //  first update target is wrong
                            Debug.Assert(false);
                            throw new ArgumentException();
                        }
#endif
                        Stmt.Target = iNewVariable;

                        /* inflow vars to first stmt are outside of the lifetime, so */
                        /* they must not be updated */
                    }
                    /* otherwise, update the references */
                    else
                    {
#if DEBUG
                        if ((j != iSecondUpdate) && (Stmt.Target == i))
                        {
                            //  intervening update was missed
                            Debug.Assert(false);
                            throw new ArgumentException();
                        }
#endif
                        switch (Stmt.Type)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();

                            case FMSynthStmtType.eFMSynthWave:
                                FMSynthStmtWaveRec WaveStmt = (FMSynthStmtWaveRec)Stmt;
                                if (WaveStmt.PhaseSource == i)
                                {
                                    WaveStmt.PhaseSource = iNewVariable;
                                }
                                if (WaveStmt.PhaseGainSource == i)
                                {
                                    WaveStmt.PhaseGainSource = iNewVariable;
                                }
                                break;

                            case FMSynthStmtType.eFMSynthMuladd:
                                FMSynthStmtMulAddRec MuladdStmt = (FMSynthStmtMulAddRec)Stmt;
                                if (MuladdStmt.Source == i)
                                {
                                    MuladdStmt.Source = iNewVariable;
                                }
                                if (MuladdStmt.Source2 == i)
                                {
                                    MuladdStmt.Source2 = iNewVariable;
                                }
                                if (MuladdStmt.Factor2 == i)
                                {
                                    MuladdStmt.Factor2 = iNewVariable;
                                }
                                break;

                            case FMSynthStmtType.eFMSynthEnvelope:
                                /* no inputs */
                                break;
                        }
                    }

                } while (j != iSecondUpdate);
            }

            cVars = Spec.Variables.Length; /* reset cuz we added some vars */


            /* initialize states to unoptimized case */

            FMSynthOptRate[] rgStmtState = new FMSynthOptRate[cStmts];
            for (int i = 0; i < cStmts; i++)
            {
                Spec.Stmts[i].Optimize = true; /* start out assuming everything is control rate */
                rgStmtState[i] = FMSynthOptRate.FMSYNTH_CONTROLRATE;
            }

            FMSynthOptRate[] rgVarState = new FMSynthOptRate[cVars];
            for (int i = 0; i < cVars; i += 1)
            {
                rgVarState[i] = FMSynthOptRate.FMSYNTH_CONTROLRATE;
            }
            rgVarState[FMSYNTH_LEFT] = FMSynthOptRate.FMSYNTH_DATARATE;
            rgVarState[FMSYNTH_RIGHT] = FMSynthOptRate.FMSYNTH_DATARATE;


            /* convert control variables to data variables.  those left at the end are */
            /* really control variables. */
            /* rules for statement conversion: */
            /*  - wave is always a data statement */
            /*  - a statement with any data input variables is a data statement */
            /*  - a statement that updates any data variable is a data statement */
            /* rules for variable conversion: */
            /*  - any variable updated by a data statement is a data variable */
            /* note that flow wraps around: a read can occur before a write, which means it */
            /* reads the value written from the previous cycle.  in this case, the 3rd rule */
            /* for statements converts the updating statement into a data statement even if it */
            /* has no data rate dependencies.  In accordance, we treat variables as infinite */
            /* lifetime, so a state change in one place affects the variable everywhere. */
            /* one final rule: */
            /*  - if a control-rate update of a variable occurs after a data rate use of it, */
            /*    then there is a data-rate lifetime wraparound so the update must be data rate. */
            /*    that's because optimization isn't merely control vs. data rate, but also */
            /*    indicates whether the statement can be executed before any data rate statement. */
            bool SomethingChanged = true;
            while (SomethingChanged)
            {
                SomethingChanged = false;

                for (int i = 0; i < cStmts; i += 1)
                {
                    FMSynthStmtRootRec Stmt = Spec.Stmts[i];

                    /* apply statement and variable rules */
                    switch (Stmt.Type)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();

                        case FMSynthStmtType.eFMSynthWave:
                            FMSynthStmtWaveRec WaveStmt = (FMSynthStmtWaveRec)Stmt;

                            /* waves are always data rate */
                            WaveStmt.Optimize = false;
                            SomethingChanged = SomethingChanged
                                || (rgStmtState[i] != FMSynthOptRate.FMSYNTH_DATARATE);
                            rgStmtState[i] = FMSynthOptRate.FMSYNTH_DATARATE;

                            /* mark output variable as data rate */
                            SomethingChanged = SomethingChanged
                                || (rgVarState[WaveStmt.Target] != FMSynthOptRate.FMSYNTH_DATARATE);
                            rgVarState[WaveStmt.Target] = FMSynthOptRate.FMSYNTH_DATARATE;

                            /* don't bother checking inputs */

                            break;

                        case FMSynthStmtType.eFMSynthMuladd:
                            FMSynthStmtMulAddRec MuladdStmt = (FMSynthStmtMulAddRec)Stmt;

                            /* if updating a data var, or any input is a data, then stmt is a */
                            /* data and it's output var is */
                            if ((rgStmtState[i] == FMSynthOptRate.FMSYNTH_DATARATE) /* stmt is already data rate */
                                || (rgVarState[MuladdStmt.Target] == FMSynthOptRate.FMSYNTH_DATARATE) /* output var is already data rate */
                                || (rgVarState[MuladdStmt.Source] == FMSynthOptRate.FMSYNTH_DATARATE) /* any input is data rate */
                                || (rgVarState[MuladdStmt.Factor2] == FMSynthOptRate.FMSYNTH_DATARATE) /* any input is data rate */
                                || (rgVarState[MuladdStmt.Source2] == FMSynthOptRate.FMSYNTH_DATARATE)) /* any input is data rate */
                            {
                                /* update stmt */
                                MuladdStmt.Optimize = false;
                                SomethingChanged = SomethingChanged
                                    || (rgStmtState[i] != FMSynthOptRate.FMSYNTH_DATARATE);
                                rgStmtState[i] = FMSynthOptRate.FMSYNTH_DATARATE;

                                /* update output var */
                                SomethingChanged = SomethingChanged
                                    || (rgVarState[MuladdStmt.Target] != FMSynthOptRate.FMSYNTH_DATARATE);
                                rgVarState[MuladdStmt.Target] = FMSynthOptRate.FMSYNTH_DATARATE;
                            }

                            break;

                        case FMSynthStmtType.eFMSynthEnvelope:
                            FMSynthStmtEnvelopeRec EnvStmt = (FMSynthStmtEnvelopeRec)Stmt;

                            /* if updating a data var, then become data stmt (note, no inputs for this one) */
                            if (rgVarState[EnvStmt.Target] == FMSynthOptRate.FMSYNTH_DATARATE)
                            {
                                /* update stmt */
                                EnvStmt.Optimize = false;
                                SomethingChanged = SomethingChanged
                                    || (rgStmtState[i] != FMSynthOptRate.FMSYNTH_DATARATE);
                                rgStmtState[i] = FMSynthOptRate.FMSYNTH_DATARATE;

                                /* update output var -- not strictly necessary due to if-stmt condition */
                                SomethingChanged = SomethingChanged
                                    || (rgVarState[EnvStmt.Target] != FMSynthOptRate.FMSYNTH_DATARATE);
                                rgVarState[EnvStmt.Target] = FMSynthOptRate.FMSYNTH_DATARATE;
                            }

                            break;
                    }

                    /* apply wrap-around reordering rule */
                    /* this algorithm assumes all references have been disambiguated.  this */
                    /* is not true of 'left' and 'right', but anything using them will be */
                    /* at data rate already due to the above loop. */
                    if (rgVarState[Stmt.Target] == FMSynthOptRate.FMSYNTH_CONTROLRATE)
                    {
                        /* this is a control update, look for preceding uses (including in */
                        /* the inflow of this statement) */
                        for (int j = 0; j <= i; j++)
                        {
                            FMSynthStmtRootRec PrecStmt = Spec.Stmts[j];
#if DEBUG
                            if ((j < i) && (PrecStmt.Target == Stmt.Target))
                            {
                                // assignment disambiguation incomplete
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            switch (PrecStmt.Type)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new ArgumentException();

                                case FMSynthStmtType.eFMSynthWave:
                                    FMSynthStmtWaveRec PrecWaveStmt = (FMSynthStmtWaveRec)PrecStmt;

                                    if ((PrecWaveStmt.PhaseSource == Stmt.Target)
                                        || (PrecWaveStmt.PhaseGainSource == Stmt.Target))
                                    {
                                        if (rgStmtState[j] == FMSynthOptRate.FMSYNTH_DATARATE)
                                        {
                                            SomethingChanged = true;
                                            rgStmtState[i] = FMSynthOptRate.FMSYNTH_DATARATE;
                                        }
                                    }

                                    break;

                                case FMSynthStmtType.eFMSynthMuladd:
                                    FMSynthStmtMulAddRec PrecMuladdStmt = (FMSynthStmtMulAddRec)PrecStmt;

                                    if ((PrecMuladdStmt.Source == Stmt.Target)
                                        || (PrecMuladdStmt.Source2 == Stmt.Target)
                                        || (PrecMuladdStmt.Factor2 == Stmt.Target))
                                    {
                                        if (rgStmtState[j] == FMSynthOptRate.FMSYNTH_DATARATE)
                                        {
                                            SomethingChanged = true;
                                            rgStmtState[i] = FMSynthOptRate.FMSYNTH_DATARATE;
                                        }
                                    }

                                    break;

                                case FMSynthStmtType.eFMSynthEnvelope:
                                    /* no inputs */
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }
}
