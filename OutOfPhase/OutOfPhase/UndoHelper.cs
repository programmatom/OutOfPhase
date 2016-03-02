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
    public interface IUndoClient
    {
        IUndoUnit CaptureCurrentStateForUndo();
    }

    public interface IUndoUnit
    {
        void Do(IUndoClient client);
    }

    public class UndoHelper : IDisposable
    {
        private readonly IUndoClient client;
        private readonly Stack<UndoUnit> undo = new Stack<UndoUnit>();
        private readonly Stack<UndoUnit> redo = new Stack<UndoUnit>();
        private bool disposed;

        public UndoHelper(IUndoClient client)
        {
            this.client = client;
        }

        private class UndoUnit
        {
            public readonly IUndoUnit Inner;
            public readonly string Label;

            public UndoUnit(IUndoUnit inner, string label)
            {
                this.Inner = inner;
                this.Label = label;
            }
        }

        public bool UndoAvailable
        {
            get
            {
                if (disposed)
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                return undo.Count != 0;
            }
        }

        public bool RedoAvailable
        {
            get
            {
                if (disposed)
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                return redo.Count != 0;
            }
        }

        public string UndoLabel
        {
            get
            {
                if (disposed || !UndoAvailable)
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                return undo.Peek().Label;
            }
        }

        public string RedoLabel
        {
            get
            {
                if (disposed || !RedoAvailable)
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                return redo.Peek().Label;
            }
        }

        public event EventHandler OnBeforeUndoRedo;
        public event EventHandler OnAfterUndoRedo;

        public void Undo()
        {
            if (disposed)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            if (OnBeforeUndoRedo != null)
            {
                OnBeforeUndoRedo.Invoke(this, EventArgs.Empty);
            }

            UndoUnit info = undo.Pop();
            try
            {
                SaveUndoInfo(true/*redo*/, info.Label);
                info.Inner.Do(client);
            }
            finally
            {
                IDisposable dispInfoInner = info.Inner as IDisposable;
                if (dispInfoInner != null)
                {
                    dispInfoInner.Dispose();
                }

                if (OnAfterUndoRedo != null)
                {
                    OnAfterUndoRedo.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void Redo()
        {
            if (disposed)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            if (OnBeforeUndoRedo != null)
            {
                OnBeforeUndoRedo.Invoke(this, EventArgs.Empty);
            }

            UndoUnit info = redo.Pop();
            try
            {
                SaveUndoInfo(false/*redo*/, false/*purgeRedo*/, info.Label);
                info.Inner.Do(client);
            }
            finally
            {
                IDisposable dispInfoInner = info.Inner as IDisposable;
                if (dispInfoInner != null)
                {
                    dispInfoInner.Dispose();
                }

                if (OnAfterUndoRedo != null)
                {
                    OnAfterUndoRedo.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void SaveUndoInfo(
            bool forRedo, // forRedo == false for normal operations, true for 'undo' command
            bool purgeRedo,
            string label)
        {
            if (disposed)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            IUndoUnit info = client.CaptureCurrentStateForUndo();
            if (!forRedo)
            {
                undo.Push(new UndoUnit(info, label));
                if (purgeRedo)
                {
                    ClearRedo();
                }
            }
            else
            {
                redo.Push(new UndoUnit(info, label));
            }
        }

        public void SaveUndoInfo(
            bool forRedo, // forRedo == false for normal operations, true for 'undo' command
            string label)
        {
            if (disposed)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            SaveUndoInfo(forRedo, true/*purgeRedo*/, label);
        }

        private void ClearRedo()
        {
            foreach (UndoUnit info in redo)
            {
                IDisposable dispInfoInner = info.Inner as IDisposable;
                if (dispInfoInner != null)
                {
                    dispInfoInner.Dispose();
                }
            }
            redo.Clear();
        }

        private void ClearUndo()
        {
            foreach (UndoUnit info in undo)
            {
                IDisposable dispInfoInner = info.Inner as IDisposable;
                if (dispInfoInner != null)
                {
                    dispInfoInner.Dispose();
                }
            }
            undo.Clear();
        }

        public void Dispose()
        {
            if (disposed)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            disposed = true;

            ClearRedo();
            ClearUndo();
        }
    }
}
