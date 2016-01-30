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
        public class AlignedWorkspace : IDisposable
        {
            private readonly float[] workspace;
            private readonly GCHandle hWorkspace;
            private readonly int offset;
            private readonly int length; // specified, not including padding

            private const int SizeOfFloat = 4;

            public AlignedWorkspace(int count)
            {
                this.length = count;
                this.workspace = new float[count + SynthParamRec.WORKSPACEALIGNBYTES];
                this.hWorkspace = GCHandle.Alloc(this.workspace, GCHandleType.Pinned);
                this.offset = 0;
                SynthParamRec.Align(this.hWorkspace.AddrOfPinnedObject(), ref this.offset, SynthParamRec.WORKSPACEALIGNBYTES, SizeOfFloat);
                Debug.Assert(this.offset + count <= this.workspace.Length);
            }

            public IntPtr BasePtr { get { return hWorkspace.AddrOfPinnedObject(); } }
            public float[] Base { get { return workspace; } }
            public int[] BaseInt { get { return UnsafeArrayCast.AsInts(workspace); } }
            public uint[] BaseUint { get { return UnsafeArrayCast.AsUints(workspace); } }
            public int Offset { get { return offset; } }
            public int Length { get { return length; } } // specified, not including padding

            public void Dispose()
            {
                if (this.hWorkspace.IsAllocated)
                {
                    this.hWorkspace.Free();
                }

                GC.SuppressFinalize(this);
            }

            ~AlignedWorkspace()
            {
#if DEBUG
                Debug.Assert(false, "AlignedWorkspace finalizer invoked - have you forgotten to .Dispose()? " + allocatedFrom.ToString());
#endif
                Dispose();
            }

#if DEBUG
            private readonly StackTrace allocatedFrom = new StackTrace(true);
#endif
        }
    }
}
