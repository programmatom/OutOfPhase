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

namespace OutOfPhase
{
    public static class PcodePeepholeOptimizer
    {
        /* this routine scans the entire code block to see if any branches are made */
        /* to any but the first instruction specified in the run of instructions. */
        /* Start points to the first instruction, Extent specifies the number of WORDS */
        /* (not instructions).  Branches MAY point to Prog[Start], however. */
        private static bool NoBranchToInterior(List<OpcodeRec> Prog, int Start, int Extent)
        {
            int i;
            for (i = 0; i < Prog.Count; i += PcodeRec.GetInstructionLength(Prog[i].Opcode))
            {
                if ((Prog[i].Opcode == Pcodes.epBranchUnconditional)
                    || (Prog[i].Opcode == Pcodes.epBranchIfZero)
                    || (Prog[i].Opcode == Pcodes.epBranchIfNotZero))
                {
                    /* branch operation detected, test index */
                    if ((Prog[i + 1].ImmediateInteger > Start)
                        && (Prog[i + 1].ImmediateInteger < Start + Extent))
                    {
                        /* branch to interior found, so return false. */
                        return false;
                    }
                }
            }
            Debug.Assert(i == Prog.Count); // internal instruction alignment error
            return true;
        }

        /* this routine eliminates the specified segment of code from the program */
        /* and updates all branches pointing to areas beyond it.  The new length */
        /* is returned.  it also disposes of any additional storage used by the */
        /* instructions being deleted */
        private static void DropCodeSegment(List<OpcodeRec> Prog, List<int> LineNumberArray, int Start, int Extent)
        {
            /* sanity check */
            Debug.Assert(NoBranchToInterior(Prog, Start, Extent)); // branches to interior not permitted
            /* patch up branches */
            int Scan;
            for (Scan = 0; Scan < Prog.Count; Scan += PcodeRec.GetInstructionLength(Prog[Scan].Opcode))
            {
                /* looking for branch instructions */
                if ((Prog[Scan].Opcode == Pcodes.epBranchUnconditional)
                    || (Prog[Scan].Opcode == Pcodes.epBranchIfZero)
                    || (Prog[Scan].Opcode == Pcodes.epBranchIfNotZero))
                {
                    /* found a branch instruction.  does it need to be patched? */
                    if (Prog[Scan + 1].ImmediateInteger > Start)
                    {
                        /* branch is beyond segment being dropped, so decrement it's address */
                        /* by the length of the segment */
                        OpcodeRec opcode = Prog[Scan + 1];
                        opcode.ImmediateInteger -= Extent;
                        Prog[Scan + 1] = opcode;
                    }
                }
            }
            Debug.Assert(Scan == Prog.Count); // internal instruction alignment error
            /* delete the code segment */
            Prog.RemoveRange(Start, Extent);
            LineNumberArray.RemoveRange(Start, Extent);
        }

        /* this routine looks for indivisible dup/pop operations (with no interior */
        /* branches) and eliminates them.  *Flag is set if some change was made, and the */
        /* new length of Prog[] is returned. */
        private static void EliminateDupPop(List<OpcodeRec> Prog, List<int> LineNumberArray, out bool Flag)
        {
            Flag = false;
            int Scan = 0;
            while (Scan < Prog.Count)
            {
                /* look to see if this part can be dropped.  if it can, we don't increment */
                /* Scan so that we can look at the part that will be moved into where this */
                /* is as well.  otherwise, we do increment */
                if ((Prog[Scan].Opcode == Pcodes.epDuplicate)
                    && (Prog[Scan + 1].Opcode == Pcodes.epStackPop)
                    && NoBranchToInterior(Prog, Scan, 2))
                {
                    /* found one! */
                    DropCodeSegment(Prog, LineNumberArray, Scan, 2);
                    Flag = true;
                }
                else
                {
                    /* increment only if not found */
                    Scan += PcodeRec.GetInstructionLength(Prog[Scan].Opcode);
                }
            }
            Debug.Assert(Scan == Prog.Count); // internal instruction alignment error
        }

        /* perform the optimizations */
        public static void OptimizePcode(List<OpcodeRec> Prog, List<int> LineNumberArray)
        {
            bool OptimizationFound;
            do
            {
                EliminateDupPop(Prog, LineNumberArray, out OptimizationFound);
            } while (OptimizationFound);
        }
    }
}
