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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class MainWindow : Form, IMenuStripManagerHandler, IGlobalNameChange
    {
        private readonly Registration registration = new Registration();

        private readonly MainWindowLocalMenuHandler localMenuHandler;

        private string savePath;
        private readonly Document document;
        private readonly KeyValuePair<MyListBox, IBindingList>[] lists;

        private readonly List<Form> miscForms = new List<Form>();
        private InteractionWindow interactionWindow;

        private readonly Stack<UndoRecord> undoStack = new Stack<UndoRecord>();
        private readonly Stack<UndoRecord> redoStack = new Stack<UndoRecord>();

        private DateTime lastAutosave;
        private string autosavePathTemplate;
        private bool autosaveNeeded;
        private int autosaveNumber;
        private string autosaveLastPath;

        public MainWindow(Document document, string path)
        {
            this.document = document;
            this.savePath = path;

            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            menuStripManager.SetGlobalHandler(this);
            localMenuHandler = new MainWindowLocalMenuHandler(this);
            menuStripManager.HookUpTextEditorWindowHelper(this.textEditorWindowHelper);
            menuStripManager.deleteObjectToolStripMenuItem.ShortcutKeys = Keys.Delete; // shortcut in main window only

            documentBindingSource.Add(document);

            document.OnSetModified += new EventHandler(document_OnSetModified);

            lists = new KeyValuePair<MyListBox, IBindingList>[]
            {
                new KeyValuePair<MyListBox, IBindingList>(myListBoxSamples, document.SampleList),
                new KeyValuePair<MyListBox, IBindingList>(myListBoxFunctions, document.FunctionList),
                new KeyValuePair<MyListBox, IBindingList>(myListBoxAlgoSamples, document.AlgoSampList),
                new KeyValuePair<MyListBox, IBindingList>(myListBoxWaveTables, document.WaveTableList),
                new KeyValuePair<MyListBox, IBindingList>(myListBoxAlgoWaveTables, document.AlgoWaveTableList),
                new KeyValuePair<MyListBox, IBindingList>(myListBoxInstruments, document.InstrumentList),
                new KeyValuePair<MyListBox, IBindingList>(myListBoxTracks, document.TrackList),
            };
            foreach (KeyValuePair<MyListBox, IBindingList> item in lists)
            {
                MyListBox listBox = item.Key;
                listBox.SelectionChanged += listBoxSelectionChanged;
            }

            myListBoxFunctions.SetUnderlying(document.FunctionList, delegate(object obj) { return ((FunctionObjectRec)obj).Name; });
            myListBoxAlgoWaveTables.SetUnderlying(document.AlgoWaveTableList, delegate(object obj) { return ((AlgoWaveTableObjectRec)obj).Name; });
            myListBoxInstruments.SetUnderlying(document.InstrumentList, delegate(object obj) { return ((InstrObjectRec)obj).Name; });
            myListBoxAlgoSamples.SetUnderlying(document.AlgoSampList, delegate(object obj) { return ((AlgoSampObjectRec)obj).Name; });
            myListBoxSamples.SetUnderlying(document.SampleList, delegate(object obj) { return ((SampleObjectRec)obj).Name; });
            myListBoxWaveTables.SetUnderlying(document.WaveTableList, delegate(object obj) { return ((WaveTableObjectRec)obj).Name; });
            myListBoxTracks.SetUnderlying(document.TrackList, delegate(object obj) { return ((TrackObjectRec)obj).Name; });

            myListBoxFunctions.DoubleClick2 += new MyListBox.DoubleClick2EventHandler(listBoxFunctions_DoubleClick);
            myListBoxAlgoWaveTables.DoubleClick2 += new MyListBox.DoubleClick2EventHandler(listBoxAlgoWaveTables_DoubleClick);
            myListBoxInstruments.DoubleClick2 += new MyListBox.DoubleClick2EventHandler(listBoxInstruments_DoubleClick);
            myListBoxAlgoSamples.DoubleClick2 += new MyListBox.DoubleClick2EventHandler(listBoxAlgoSamples_DoubleClick);
            myListBoxSamples.DoubleClick2 += new MyListBox.DoubleClick2EventHandler(listBoxSamples_DoubleClick);
            myListBoxWaveTables.DoubleClick2 += new MyListBox.DoubleClick2EventHandler(listBoxWaveTables_DoubleClick);
            myListBoxTracks.DoubleClick2 += new MyListBox.DoubleClick2EventHandler(listBoxTracks_DoubleClick);

            registration.Register(document, this);
            registration.NotifyGlobalNameChanged();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (interactionWindow != null)
            {
                interactionWindow.Close();
                interactionWindow = null;
            }
            CloseMiscForms();

            if (autosaveLastPath != null)
            {
                File.Delete(autosaveLastPath);
            }
            autosaveLastPath = null;

            //registration.Unregister(document, this); - done in OnFormClosing

            base.OnFormClosed(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (Closable())
            {
                registration.Unregister(document, this);
                registration.CloseAll();
            }
            else
            {
                e.Cancel = true;
            }

            base.OnFormClosing(e);
        }

        public bool Closable()
        {
            if (!registration.EnsureValidateAndCommit())
            {
                return false;
            }
            if (!document.Modified)
            {
                return true;
            }

            string path = null; // TODO
            using (UnsavedDialog dialog = new UnsavedDialog(path != null ? Path.GetFileName(path) : "Untitled"))
            {
                switch (dialog.ShowDialog())
                {
                    default:
                        Debug.Assert(false);
                        return false;
                    case DialogResult.Yes:
                        return SaveOrSaveAsHelper(); // user can cancel the "Save As" dialog
                    case DialogResult.No:
                        document.Modified = false;
                        return true;
                    case DialogResult.Cancel:
                        return false;
                }
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            menuStripManager.SetActiveHandler(localMenuHandler);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            menuStripManager.SetActiveHandler(null);
            base.OnDeactivate(e);
        }


        //

        public Document Document { get { return document; } }


        //

        protected override void OnShown(EventArgs e)
        {
            PersistWindowRect.OnShown(this, document.SavedWindowXLoc, document.SavedWindowYLoc, document.SavedWindowWidth, document.SavedWindowHeight);
            base.OnShown(e);
        }

        private void ResizeMove()
        {
            if (Visible)
            {
                short x, y, width, height;
                PersistWindowRect.OnResize(this, out x, out y, out width, out height);
                document.SavedWindowXLoc = x;
                document.SavedWindowYLoc = y;
                document.SavedWindowWidth = width;
                document.SavedWindowHeight = height;
            }
        }

        protected override void OnMove(EventArgs e)
        {
            ResizeMove();
            base.OnMove(e);
        }

        protected override void OnResize(EventArgs e)
        {
            ResizeMove();
            base.OnResize(e);
        }


        //

        private void listBoxSelectionChanged(object sender, EventArgs e)
        {
            // deselect other boxes - only one can be selected in the window
            if (((MyListBox)sender).SelectedIndex >= 0)
            {
                foreach (KeyValuePair<MyListBox, IBindingList> item in lists)
                {
                    MyListBox listBox = item.Key;
                    if (listBox != sender)
                    {
                        listBox.SelectNone();
                    }
                }
            }
        }


        //

        private void listBoxFunctions_DoubleClick(object sender, MyListBox.DoubleClick2EventArgs e)
        {
            if (!registration.Activate(e.Item))
            {
                new FunctionWindow(registration, (FunctionObjectRec)e.Item, document, this).Show();
            }
        }

        private void listBoxAlgoWaveTables_DoubleClick(object sender, MyListBox.DoubleClick2EventArgs e)
        {
            if (!registration.Activate(e.Item))
            {
                new AlgoWaveTableWindow(registration, (AlgoWaveTableObjectRec)e.Item, this).Show();
            }
        }

        private void listBoxInstruments_DoubleClick(object sender, MyListBox.DoubleClick2EventArgs e)
        {
            if (!registration.Activate(e.Item))
            {
                new InstrumentWindow(registration, (InstrObjectRec)e.Item, this).Show();
            }
        }

        private void listBoxAlgoSamples_DoubleClick(object sender, MyListBox.DoubleClick2EventArgs e)
        {
            if (!registration.Activate(e.Item))
            {
                new AlgoSampWindow(registration, (AlgoSampObjectRec)e.Item, this).Show();
            }
        }

        private void listBoxTracks_DoubleClick(object sender, MyListBox.DoubleClick2EventArgs e)
        {
            if (!registration.Activate(e.Item))
            {
                new TrackWindow(registration, (TrackObjectRec)e.Item, this).Show();
            }
        }

        private void listBoxWaveTables_DoubleClick(object sender, MyListBox.DoubleClick2EventArgs e)
        {
            if (!registration.Activate(e.Item))
            {
                new WaveTableWindow(registration, (WaveTableObjectRec)e.Item, this).Show();
            }
        }

        private void listBoxSamples_DoubleClick(object sender, MyListBox.DoubleClick2EventArgs e)
        {
            if (!registration.Activate(e.Item))
            {
                new SampleWindow(registration, (SampleObjectRec)e.Item, this).Show();
            }
        }


        //

        public void AddMiscForm(Form form)
        {
            miscForms.Add(form);
        }

        public void RemoveMiscForm(Form form)
        {
            miscForms.Remove(form);
        }

        private void CloseMiscForms()
        {
            foreach (Form form in new List<Form>(miscForms))
            {
                form.Close();
            }
            Debug.Assert(miscForms.Count == 0);
        }

        public delegate InteractionWindow GetInteractionWindowDelegate();
        public InteractionWindow GetInteractionWindow()
        {
            if (interactionWindow == null)
            {
                interactionWindow = new InteractionWindow(this);
                interactionWindow.Disposed += new EventHandler(interactionWindow_Disposed);
                interactionWindow.Show();
            }
            return interactionWindow;
        }

        public bool MainWindowDoesInteractionWindowExist()
        {
            return interactionWindow != null;
        }

        private void interactionWindow_Disposed(object sender, EventArgs e)
        {
            interactionWindow = null;
        }

        public delegate bool PromptResumableErrorDelegate(string message);
        public bool PromptResumableError(string message)
        {
            DialogResult result = MessageBox.Show(String.Format("An message was raised by code in the document: \"{0}\" Continue?", message), "Out Of Phase", MessageBoxButtons.YesNo);
            return result == DialogResult.Yes;
        }


        //

        // Requires that all active controls have been committed by calling .Validate() on them. That is the
        // responsibility of the window managing the UI that invokes this method, since this method can't possibly
        // know about all the possible controls wherever they may be.
        public bool MakeUpToDate()
        {
            if (!registration.EnsureValidateAndCommit())
            {
                return false;
            }

            return document.EnsureBuilt(
                false/*force*/,
                new PcodeExterns(this),
                delegate(object sender, BuildErrorInfo errorInfo)
                {
                    Form editor;
                    if (!registration.Activate(sender, out editor))
                    {
                        if (sender is FunctionObjectRec)
                        {
                            editor = new FunctionWindow(registration, (FunctionObjectRec)sender, document, this);
                        }
                        else if (sender is AlgoWaveTableObjectRec)
                        {
                            editor = new AlgoWaveTableWindow(registration, (AlgoWaveTableObjectRec)sender, this);
                        }
                        else if (sender is AlgoSampObjectRec)
                        {
                            editor = new AlgoSampWindow(registration, (AlgoSampObjectRec)sender, this);
                        }
                        else if (sender is InstrObjectRec)
                        {
                            editor = new InstrumentWindow(registration, (InstrObjectRec)sender, this);
                        }
                        else if (sender is ScoreEffectsRec)
                        {
                            editor = new ScoreEffectWindow(registration, (ScoreEffectsRec)sender, this);
                        }
                        else if (sender is SectionObjectRec)
                        {
                            editor = new SectionWindow(registration, (SectionObjectRec)sender, this);
                        }
                        else if (sender is SequencerRec)
                        {
                            editor = new SequencerConfigWindow(registration, (SequencerRec)sender, this);
                        }
                        else
                        {
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        editor.Show();
                    }

                    ((IHighlightLine)editor).HighlightLine(errorInfo.LineNumber);
                    MessageBox.Show(errorInfo.CompositeErrorMessage, "Build Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                });
        }

        public void DefaultBuildFailedCallback(object sender, BuildErrorInfo errorInfo)
        {
            Form editor;
            if (!registration.Activate(sender, out editor))
            {
                if (sender is FunctionObjectRec)
                {
                    editor = new FunctionWindow(registration, (FunctionObjectRec)sender, document, this);
                }
                else
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
                editor.Show();
            }

            ((IHighlightLine)editor).HighlightLine(errorInfo.LineNumber);
            MessageBox.Show(errorInfo.CompositeErrorMessage, "Build Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }

        public bool MakeUpToDateFunctions()
        {
            if (!registration.EnsureValidateAndCommit())
            {
                return false;
            }

            return document.EnsureBuiltFunctions(
                new PcodeExterns(this),
                DefaultBuildFailedCallback);
        }

        public void MakeClean()
        {
            document.Unbuild();
        }

        public IBindingList GetUnderlyingList(MyListBox listBox)
        {
            int listBoxIndex = Array.FindIndex(lists, delegate(KeyValuePair<MyListBox, IBindingList> candidate) { return candidate.Key == listBox; });
            if (listBoxIndex < 0)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            return lists[listBoxIndex].Value;
        }

        public void DeleteObject(object o, IBindingList list)
        {
            int index;

            index = Array.FindIndex(lists, delegate(KeyValuePair<MyListBox, IBindingList> candidate) { return candidate.Value == list; });
            if (index < 0)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }

            index = list.IndexOf(o);
            if (index < 0)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }

            registration.CloseAll(o);
            list.RemoveAt(index);

            undoStack.Push(new DeletedUndoRecord(o, list, index));
            redoStack.Clear();
        }

        private void CopyObject(object o, IBindingList list)
        {
            if (o is FunctionObjectRec)
            {
                FunctionObjectRec function = (FunctionObjectRec)o;
                FunctionClipboard clipboard = new FunctionClipboard(function, document);
                Clipboard.SetData(FunctionClipboard.ClipboardIdentifer, clipboard);
            }
            else if (o is InstrObjectRec)
            {
                InstrObjectRec instrument = (InstrObjectRec)o;
                InstrumentClipboard clipboard = new InstrumentClipboard(instrument, document);
                Clipboard.SetData(InstrumentClipboard.ClipboardIdentifer, clipboard);
            }
            else if (o is TrackObjectRec)
            {
                TrackObjectRec track = (TrackObjectRec)o;
                TrackClipboard clipboard = new TrackClipboard(track, document);
                Clipboard.SetData(TrackClipboard.ClipboardIdentifer, clipboard);
            }
            else if (o is SampleObjectRec)
            {
                SampleObjectRec sample = (SampleObjectRec)o;
                SampleClipboard clipboard = new SampleClipboard(sample, document);
                Clipboard.SetData(SampleClipboard.ClipboardIdentifer, clipboard);
            }
            else if (o is WaveTableObjectRec)
            {
                WaveTableObjectRec waveTable = (WaveTableObjectRec)o;
                WaveTableClipboard clipboard = new WaveTableClipboard(waveTable, document);
                Clipboard.SetData(WaveTableClipboard.ClipboardIdentifer, clipboard);
            }
            else if (o is AlgoSampObjectRec)
            {
                AlgoSampObjectRec algoSamp = (AlgoSampObjectRec)o;
                AlgoSampClipboard clipboard = new AlgoSampClipboard(algoSamp, document);
                Clipboard.SetData(AlgoSampClipboard.ClipboardIdentifer, clipboard);
            }
            else if (o is AlgoWaveTableObjectRec)
            {
                AlgoWaveTableObjectRec algoWaveTable = (AlgoWaveTableObjectRec)o;
                AlgoWaveTableClipboard clipboard = new AlgoWaveTableClipboard(algoWaveTable, document);
                Clipboard.SetData(AlgoWaveTableClipboard.ClipboardIdentifer, clipboard);
            }
            else
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
        }

        private bool PasteObject(bool paste, out string name)
        {
            name = null;
            IDataObject dataObject = Clipboard.GetDataObject();
            foreach (string format in dataObject.GetFormats())
            {
                switch (format)
                {
                    default:
                        // unrecognized
                        break;
                    case FunctionClipboard.ClipboardIdentifer:
                        name = "Function Module";
                        if (paste)
                        {
                            FunctionClipboard clipboard = (FunctionClipboard)dataObject.GetData(FunctionClipboard.ClipboardIdentifer);
                            object o = clipboard.Reconstitute(document);
                            document.FunctionList.Add((FunctionObjectRec)o);
                            myListBoxFunctions.SelectItem(document.FunctionList.Count - 1, true/*clearOther*/);
                        }
                        return true;
                    case InstrumentClipboard.ClipboardIdentifer:
                        name = "Instrument";
                        if (paste)
                        {
                            InstrumentClipboard clipboard = (InstrumentClipboard)dataObject.GetData(InstrumentClipboard.ClipboardIdentifer);
                            object o = clipboard.Reconstitute(document);
                            document.InstrumentList.Add((InstrObjectRec)o);
                            myListBoxInstruments.SelectItem(document.InstrumentList.Count - 1, true/*clearOther*/);
                        }
                        return true;
                    case TrackClipboard.ClipboardIdentifer:
                        name = "Track";
                        if (paste)
                        {
                            TrackClipboard clipboard = (TrackClipboard)dataObject.GetData(TrackClipboard.ClipboardIdentifer);
                            object o = clipboard.Reconstitute(document);
                            document.TrackList.Add((TrackObjectRec)o);
                            myListBoxTracks.SelectItem(document.TrackList.Count - 1, true/*clearOther*/);
                        }
                        return true;
                    case SampleClipboard.ClipboardIdentifer:
                        name = "Sample";
                        if (paste)
                        {
                            SampleClipboard clipboard = (SampleClipboard)dataObject.GetData(SampleClipboard.ClipboardIdentifer);
                            object o = clipboard.Reconstitute(document);
                            document.SampleList.Add((SampleObjectRec)o);
                            myListBoxSamples.SelectItem(document.SampleList.Count - 1, true/*clearOther*/);
                        }
                        return true;
                    case WaveTableClipboard.ClipboardIdentifer:
                        name = "Wave Table";
                        if (paste)
                        {
                            WaveTableClipboard clipboard = (WaveTableClipboard)dataObject.GetData(WaveTableClipboard.ClipboardIdentifer);
                            object o = clipboard.Reconstitute(document);
                            document.WaveTableList.Add((WaveTableObjectRec)o);
                            myListBoxWaveTables.SelectItem(document.WaveTableList.Count - 1, true/*clearOther*/);
                        }
                        return true;
                    case AlgoSampClipboard.ClipboardIdentifer:
                        name = "Algorithmic Sample";
                        if (paste)
                        {
                            AlgoSampClipboard clipboard = (AlgoSampClipboard)dataObject.GetData(AlgoSampClipboard.ClipboardIdentifer);
                            object o = clipboard.Reconstitute(document);
                            document.AlgoSampList.Add((AlgoSampObjectRec)o);
                            myListBoxAlgoSamples.SelectItem(document.AlgoSampList.Count - 1, true/*clearOther*/);
                        }
                        return true;
                    case AlgoWaveTableClipboard.ClipboardIdentifer:
                        name = "Algorithmic Wave Table";
                        if (paste)
                        {
                            AlgoWaveTableClipboard clipboard = (AlgoWaveTableClipboard)dataObject.GetData(AlgoWaveTableClipboard.ClipboardIdentifer);
                            object o = clipboard.Reconstitute(document);
                            document.AlgoWaveTableList.Add((AlgoWaveTableObjectRec)o);
                            myListBoxAlgoWaveTables.SelectItem(document.AlgoWaveTableList.Count - 1, true/*clearOther*/);
                        }
                        return true;
                }
            }
            return false;
        }

        private bool SaveHelper(string path, bool clearModified)
        {
            if (registration.EnsureValidateAndCommit())
            {
                using (Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, Constants.BufferSize))
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        document.Save(writer);
                    }
                }

                if (clearModified)
                {
                    document.Modified = false;

                    if (autosaveLastPath != null)
                    {
                        File.Delete(autosaveLastPath);
                    }
                    autosaveLastPath = null;
                }

                return true;
            }
            return false;
        }

        private bool SaveAsHelper(bool saveAs)
        {
            if (registration.EnsureValidateAndCommit())
            {
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.Title = saveAs ? "Save As" : "Save";
                    dialog.Filter = "Out Of Phase File (.oop)|*.oop|Any File Type (*)|*";
                    DialogResult result = dialog.ShowDialog();
                    if (result != DialogResult.OK)
                    {
                        return false;
                    }
                    savePath = dialog.FileName;
                    registration.NotifyGlobalNameChanged();
                }
                return SaveHelper(savePath, true/*clearModified*/);
            }
            return false;
        }

        private bool SaveOrSaveAsHelper()
        {
            if (String.IsNullOrEmpty(savePath))
            {
                return SaveAsHelper(false/*saveAs*/);
            }
            else
            {
                return SaveHelper(savePath, true/*clearModified*/);
            }
        }

        private bool SaveACopyAsHelper()
        {
            if (registration.EnsureValidateAndCommit())
            {
                string path;
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.Title = "Save A Copy As";
                    dialog.Filter = "Out Of Phase File (.oop)|*.oop|Any File Type (*)|*";
                    DialogResult result = dialog.ShowDialog();
                    if (result != DialogResult.OK)
                    {
                        return false;
                    }
                    path = dialog.FileName;
                }
                return SaveHelper(path, false/*clearModified*/);
            }
            return false;
        }

        public string SavePath { get { return savePath; } }

        public void SaveCopyAs(string targetPath)
        {
            SaveHelper(targetPath, false/*clearModified*/);
        }


        // MenuStripManager methods

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            menuStripManager.ProcessCmdKeyDelegate(ref msg, keyData);
            localMenuHandler.ProcessCmdKeyDelegate(ref msg, keyData);

            if (textEditorWindowHelper.ProcessCmdKeyDelegate(ref msg, keyData))
            {
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // local

        void EnableMenuItems_Local(MenuStripManager menuStrip)
        {
            textEditorWindowHelper.MenuActivateDelegate();

            menuStrip.closeDocumentToolStripMenuItem.Enabled = true;

            menuStrip.exitToolStripMenuItem.Enabled = true;
            menuStrip.exitToolStripMenuItem.Visible = true;

            if (ActiveControl is MyListBox)
            {
                MyListBox listBox = (MyListBox)ActiveControl;
                int listBoxIndex = Array.FindIndex(lists, delegate(KeyValuePair<MyListBox, IBindingList> candidate) { return candidate.Key == listBox; });
                if (listBoxIndex < 0)
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                menuStrip.moveUpToolStripMenuItem.Enabled = listBox.SelectedIndex > 0;
                menuStrip.moveDownToolStripMenuItem.Enabled = listBox.SelectedIndex < listBox.Count - 1;

                if (undoStack.Count != 0)
                {
                    menuStrip.undoToolStripMenuItem.Enabled = true;
                    menuStrip.undoToolStripMenuItem.Text = String.Format("Undo {0}", undoStack.Peek().GetMenuText(false/*redo*/));
                }
                if (redoStack.Count != 0)
                {
                    menuStrip.redoToolStripMenuItem.Enabled = true;
                    menuStrip.redoToolStripMenuItem.Text = String.Format("Redo {0}", redoStack.Peek().GetMenuText(true/*redo*/));
                }

                if (listBox.SelectedIndex >= 0)
                {
                    string identity = IdentifyObject(lists[listBoxIndex].Value);

                    menuStrip.deleteObjectToolStripMenuItem.Enabled = true;
                    menuStrip.deleteObjectToolStripMenuItem.Text = String.Format("Delete {0}", identity);

                    menuStrip.copyToolStripMenuItem.Enabled = true;
                    menuStrip.copyToolStripMenuItem.Text = String.Format("Copy {0}", identity);

                    menuStrip.cutToolStripMenuItem.Enabled = true;
                    menuStrip.cutToolStripMenuItem.Text = String.Format("Cut {0}", identity);
                }

                string clipName;
                if (PasteObject(false/*paste*/, out clipName))
                {
                    menuStrip.pasteToolStripMenuItem.Enabled = true;
                    menuStrip.pasteToolStripMenuItem.Text = String.Format("Paste {0}", clipName);
                }
            }
        }

        bool ExecuteMenuItem_Local(MenuStripManager menuStrip, ToolStripMenuItem menuItem)
        {
            if ((menuItem == menuStrip.moveUpToolStripMenuItem) || (menuItem == menuStrip.moveDownToolStripMenuItem))
            {
                // TODO: fix bug: if using one arrow key, switching to the other key without releasing the
                // control key causes this handler to not get called.

                MyListBox listBox = (MyListBox)ActiveControl;
                IBindingList list = GetUnderlyingList(listBox);

                int index = listBox.SelectedIndex;
                object o = list[index];
                if (menuItem == menuStrip.moveUpToolStripMenuItem)
                {
                    if (index > 0)
                    {
                        list.RemoveAt(index);
                        index--;
                        list.Insert(index, o);

                        undoStack.Push(new MovedUndoRecord(list, index, 1));
                        redoStack.Clear();
                    }
                }
                else if (menuItem == menuStrip.moveDownToolStripMenuItem)
                {
                    if (index < list.Count - 1)
                    {
                        list.RemoveAt(index);
                        index++;
                        list.Insert(index, o);

                        undoStack.Push(new MovedUndoRecord(list, index, -1));
                        redoStack.Clear();
                    }
                }
                else
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
                listBox.SelectItem(index, true/*clearOther*/);
                listBox.ScrollToCursor();
                return true;
            }
            else if (menuItem == menuStrip.undoToolStripMenuItem)
            {
                if (undoStack.Count != 0)
                {
                    UndoRecord one = undoStack.Pop();
                    one.Undo(this, redoStack);
                }
                return true;
            }
            else if (menuItem == menuStrip.redoToolStripMenuItem)
            {
                if (redoStack.Count != 0)
                {
                    UndoRecord one = redoStack.Pop();
                    one.Undo(this, undoStack);
                }
                return true;
            }
            else if (menuItem == menuStrip.deleteObjectToolStripMenuItem)
            {
                MyListBox listBox = (MyListBox)ActiveControl;
                if (listBox.SelectedIndex >= 0)
                {
                    IBindingList list = GetUnderlyingList(listBox);
                    DeleteObject(list[listBox.SelectedIndex], list);
                }
                return true;
            }
            else if (menuItem == menuStrip.copyToolStripMenuItem)
            {
                MyListBox listBox = (MyListBox)ActiveControl;
                if (listBox.SelectedIndex >= 0)
                {
                    IBindingList list = GetUnderlyingList(listBox);
                    CopyObject(list[listBox.SelectedIndex], list);
                }
                return true;
            }
            else if (menuItem == menuStrip.cutToolStripMenuItem)
            {
                MyListBox listBox = (MyListBox)ActiveControl;
                if (listBox.SelectedIndex >= 0)
                {
                    IBindingList list = GetUnderlyingList(listBox);
                    CopyObject(list[listBox.SelectedIndex], list);
                    DeleteObject(list[listBox.SelectedIndex], list);
                }
                return true;
            }
            else if (menuItem == menuStrip.pasteToolStripMenuItem)
            {
                string name;
                PasteObject(true/*paste*/, out name);
                return true;
            }
            else if (menuItem == menuStrip.closeDocumentToolStripMenuItem)
            {
                Close();
                return true;
            }
            else if (menuItem == menuStrip.exitToolStripMenuItem)
            {
                // Application.Exit() is supposed to do all this but does not work because it enumerates the
                // list Application.OpenForms directly - and showing our "Save unsaved changes?" dialog modifies
                // that list, causing their enumerator the throw an exception.

                List<Form> openForms = new List<Form>();
                foreach (Form form in Application.OpenForms)
                {
                    openForms.Add(form);
                }
                foreach (Form form in openForms)
                {
                    if (form is MainWindow)
                    {
                        if (((MainWindow)form).Closable())
                        {
                            form.Close();
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        form.Close();
                    }
                }
                // main loop will exit when there are no windows
                return true;
            }

            return false;
        }

        // global

        void IMenuStripManagerHandler.EnableMenuItems(MenuStripManager menuStrip)
        {
            menuStrip.newToolStripMenuItem.Enabled = true;
            menuStrip.openToolStripMenuItem.Enabled = true;
            menuStrip.saveToolStripMenuItem.Enabled = true;
            menuStrip.saveAsToolStripMenuItem.Enabled = true;
            menuStrip.saveACopyAsToolStripMenuItem.Enabled = true;
            menuStrip.playToolStripMenuItem.Enabled = true;
            menuStrip.playAudioFileToolStripMenuItem.Enabled = true;
            menuStrip.playAudioFileWithEffectsToolStripMenuItem.Enabled = true;
            menuStrip.importAIFFToolStripMenuItem.Enabled = true;
            menuStrip.importWAVSampleToolStripMenuItem.Enabled = true;
            menuStrip.importRawSampleToolStripMenuItem.Enabled = true;
            menuStrip.importMIDIScoreToolStripMenuItem.Enabled = true;

            menuStrip.buildAllToolStripMenuItem.Enabled = true;
            menuStrip.unbuildAllToolStripMenuItem.Enabled = true;
            menuStrip.newSampleToolStripMenuItem.Enabled = true;
            menuStrip.newAlgorithmicSampleToolStripMenuItem.Enabled = true;
            menuStrip.newWaveTableToolStripMenuItem.Enabled = true;
            menuStrip.newAlgorithmicWaveTableToolStripMenuItem.Enabled = true;
            menuStrip.newFunctionModuleToolStripMenuItem.Enabled = true;
            menuStrip.newInstrumentToolStripMenuItem.Enabled = true;
            menuStrip.newTrackToolStripMenuItem.Enabled = true;

            menuStrip.calculatorToolStripMenuItem.Enabled = true;
            menuStrip.scoreEffectsToolStripMenuItem.Enabled = true;
            menuStrip.sectionEffectsToolStripMenuItem.Enabled = true;
            menuStrip.sequencerConfigurationToolStripMenuItem.Enabled = true;

            menuStrip.tabSizeToolStripMenuItem.Enabled = true;
            menuStrip.tabSizeToolStripMenuItem.Text = String.Format("Tab Size ({0})...", document.TabSize);
            menuStrip.globalSettingsToolStripMenuItem.Enabled = true;
            menuStrip.aboutToolStripMenuItem.Enabled = true;
        }

        bool IMenuStripManagerHandler.ExecuteMenuItem(MenuStripManager menuStrip, ToolStripMenuItem menuItem)
        {
            if (menuItem == menuStrip.newToolStripMenuItem)
            {
                new MainWindow(new Document(), null).Show();
                return true;
            }
            else if (menuItem == menuStrip.openToolStripMenuItem)
            {
                using (OpenFileDialog dialog = new OpenFileDialog())
                {
                    dialog.Filter = "Out Of Phase File (.oop)|*.oop|Any File Type (*)|*";
                    DialogResult result = dialog.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        string path = dialog.FileName;
                        new MainWindow(new Document(path), path).Show();
                    }
                }
                return true;
            }
            else if (menuItem == menuStrip.saveToolStripMenuItem)
            {
                SaveOrSaveAsHelper();
                return true;
            }
            else if (menuItem == menuStrip.saveAsToolStripMenuItem)
            {
                SaveAsHelper(true);
                return true;
            }
            else if (menuItem == menuStrip.saveACopyAsToolStripMenuItem)
            {
                SaveACopyAsHelper();
                return true;
            }
            else if (menuItem == menuStrip.playToolStripMenuItem)
            {
                using (PlayPrefsDialog dialog = new PlayPrefsDialog(this, document))
                {
                    dialog.ShowDialog();
                }
                return true;
            }
            else if (menuItem == menuStrip.playAudioFileToolStripMenuItem)
            {
                using (OpenFileDialog dialog = new OpenFileDialog())
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        string path = dialog.FileName;
                        PlayFile playFile = new PlayFile();
                        playFile.Play(path, this);
                    }
                }
                return true;
            }
            else if (menuItem == menuStrip.playAudioFileWithEffectsToolStripMenuItem)
            {
                using (PlayAudioFileWithEffects dialog = new PlayAudioFileWithEffects(this))
                {
                    dialog.ShowDialog();
                }
                return true;
            }
            else if (menuItem == menuStrip.importAIFFToolStripMenuItem)
            {
                int index;
                if (Import.ImportAIFFSample(registration, this, out index))
                {
                    undoStack.Push(new InsertedUndoRecord(document.SampleList, index));
                    redoStack.Clear();
                }
                return true;
            }
            else if (menuItem == menuStrip.importWAVSampleToolStripMenuItem)
            {
                int index;
                if (Import.ImportWAVSample(registration, this, out index))
                {
                    undoStack.Push(new InsertedUndoRecord(document.SampleList, index));
                    redoStack.Clear();
                }
                return true;
            }
            else if (menuItem == menuStrip.importRawSampleToolStripMenuItem)
            {
                int index;
                if (Import.ImportRawSample(registration, this, out index))
                {
                    undoStack.Push(new InsertedUndoRecord(document.SampleList, index));
                    redoStack.Clear();
                }
                return true;
            }
            else if (menuItem == menuStrip.importMIDIScoreToolStripMenuItem)
            {
                MIDIImport.ImportMIDIFile();
                return true;
            }


            else if (menuItem == menuStrip.buildAllToolStripMenuItem)
            {
                if (registration.EnsureValidateAndCommit()) // ensure data is committed to store for all forms
                {
                    MakeUpToDate(); // ignore return code for UI
                }
                return true;
            }
            else if (menuItem == menuStrip.unbuildAllToolStripMenuItem)
            {
                MakeClean();
                return true;
            }
            else if (menuItem == menuStrip.newSampleToolStripMenuItem)
            {
                SampleObjectRec sampleObject = new SampleObjectRec(document);
                document.SampleList.Add(sampleObject);

                undoStack.Push(new InsertedUndoRecord(document.SampleList, document.SampleList.Count - 1));
                redoStack.Clear();

                new SampleWindow(registration, sampleObject, this).Show();
                return true;
            }
            else if (menuItem == menuStrip.newAlgorithmicSampleToolStripMenuItem)
            {
                AlgoSampObjectRec algoSampObject = new AlgoSampObjectRec(document);
                document.AlgoSampList.Add(algoSampObject);

                undoStack.Push(new InsertedUndoRecord(document.AlgoSampList, document.AlgoSampList.Count - 1));
                redoStack.Clear();

                new AlgoSampWindow(registration, algoSampObject, this).Show();
                return true;
            }
            else if (menuItem == menuStrip.newWaveTableToolStripMenuItem)
            {
                WaveTableObjectRec waveTableObject = new WaveTableObjectRec(document);
                document.WaveTableList.Add(waveTableObject);

                undoStack.Push(new InsertedUndoRecord(document.WaveTableList, document.WaveTableList.Count - 1));
                redoStack.Clear();

                new WaveTableWindow(registration, waveTableObject, this).Show();
                return true;
            }
            else if (menuItem == menuStrip.newAlgorithmicWaveTableToolStripMenuItem)
            {
                AlgoWaveTableObjectRec algoWaveTableObject = new AlgoWaveTableObjectRec(document);
                document.AlgoWaveTableList.Add(algoWaveTableObject);

                undoStack.Push(new InsertedUndoRecord(document.AlgoWaveTableList, document.AlgoWaveTableList.Count - 1));
                redoStack.Clear();

                new AlgoWaveTableWindow(registration, algoWaveTableObject, this).Show();
                return true;
            }
            else if (menuItem == menuStrip.newFunctionModuleToolStripMenuItem)
            {
                FunctionObjectRec functionObject = new FunctionObjectRec(document);
                document.FunctionList.Add(functionObject);

                undoStack.Push(new InsertedUndoRecord(document.FunctionList, document.FunctionList.Count - 1));
                redoStack.Clear();

                new FunctionWindow(registration, functionObject, document, this).Show();
                return true;
            }
            else if (menuItem == menuStrip.newInstrumentToolStripMenuItem)
            {
                InstrObjectRec instrObject = new InstrObjectRec(document);
                document.InstrumentList.Add(instrObject);

                undoStack.Push(new InsertedUndoRecord(document.InstrumentList, document.InstrumentList.Count - 1));
                redoStack.Clear();

                new InstrumentWindow(registration, instrObject, this).Show();
                return true;
            }
            else if (menuItem == menuStrip.newTrackToolStripMenuItem)
            {
                TrackObjectRec trackObject = new TrackObjectRec(document);
                document.TrackList.Add(trackObject);

                undoStack.Push(new InsertedUndoRecord(document.TrackList, document.TrackList.Count - 1));
                redoStack.Clear();

                new TrackWindow(registration, trackObject, this).Show();
                return true;
            }


            else if (menuItem == menuStrip.calculatorToolStripMenuItem)
            {
                new CalculatorWindow(document, this).Show();
                return true;
            }
            else if (menuItem == menuStrip.scoreEffectsToolStripMenuItem)
            {
                if (!registration.Activate(document.ScoreEffects))
                {
                    new ScoreEffectWindow(registration, document.ScoreEffects, this).Show();
                }
                return true;
            }
            else if (menuItem == menuStrip.sectionEffectsToolStripMenuItem)
            {
                using (SectionEditDialog dialog = new SectionEditDialog(registration, document, this))
                {
                    dialog.ShowDialog();
                }
                return true;
            }
            else if (menuItem == menuStrip.sequencerConfigurationToolStripMenuItem)
            {
                if (!registration.Activate(document.Sequencer))
                {
                    new SequencerConfigWindow(registration, document.Sequencer, this).Show();
                }
                return true;
            }


            else if (menuItem == menuStrip.tabSizeToolStripMenuItem)
            {
                using (CmdDlgOneParam dialog = new CmdDlgOneParam("Enter Tab Size:", "Tab Size:", document.TabSize.ToString(), CmdDlgOneParam.Options.None))
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        int tabSize;
                        if (Int32.TryParse(dialog.Value, out tabSize))
                        {
                            document.TabSize = Math.Min(Math.Max(tabSize, Constants.MINTABCOUNT), Constants.MAXTABCOUNT);
                        }
                    }
                }
                return true;
            }
            else if (menuItem == menuStrip.globalSettingsToolStripMenuItem)
            {
                using (GlobalPrefsDialog dialog = new GlobalPrefsDialog())
                {
                    dialog.ShowDialog();
                }
                return true;
            }
            else if (menuItem == menuStrip.aboutToolStripMenuItem)
            {
                new AboutBox().Show();
                return true;
            }


            return false;
        }

        private class MainWindowLocalMenuHandler : IMenuStripManagerHandler
        {
            private MainWindow mainWindow;

            public MainWindowLocalMenuHandler(MainWindow mainWindow)
            {
                this.mainWindow = mainWindow;
            }

            public void ProcessCmdKeyDelegate(ref Message msg, Keys keyData)
            {
            }

            void IMenuStripManagerHandler.EnableMenuItems(MenuStripManager menuStrip)
            {
                mainWindow.EnableMenuItems_Local(menuStrip);
            }

            bool IMenuStripManagerHandler.ExecuteMenuItem(MenuStripManager menuStrip, ToolStripMenuItem menuItem)
            {
                return mainWindow.ExecuteMenuItem_Local(menuStrip, menuItem);
            }
        }


        // undo

        private static string IdentifyObject(object o)
        {
            if ((o is FunctionObjectRec) || (o is BindingList<FunctionObjectRec>))
            {
                return "Function Module";
            }
            else if ((o is InstrObjectRec) || (o is BindingList<InstrObjectRec>))
            {
                return "Instrument";
            }
            else if ((o is TrackObjectRec) || (o is BindingList<TrackObjectRec>))
            {
                return "Track";
            }
            else if ((o is SampleObjectRec) || (o is BindingList<SampleObjectRec>))
            {
                return "Sample";
            }
            else if ((o is WaveTableObjectRec) || (o is BindingList<WaveTableObjectRec>))
            {
                return "Wave Table";
            }
            else if ((o is AlgoSampObjectRec) || (o is BindingList<AlgoSampObjectRec>))
            {
                return "Algorithmic Sample";
            }
            else if ((o is AlgoWaveTableObjectRec) || (o is BindingList<AlgoWaveTableObjectRec>))
            {
                return "Algorithmic Wave Table";
            }
            else
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
        }

        private abstract class UndoRecord
        {
            protected readonly string MenuText;

            public abstract void Undo(MainWindow mainWindow, Stack<UndoRecord> redoStack);

            public abstract string GetMenuText(bool redo);

            public UndoRecord(string menuText)
            {
                this.MenuText = menuText;
            }
        }

        private class DeletedUndoRecord : UndoRecord
        {
            public readonly object Deleted;
            public readonly IBindingList OwningList;
            public readonly int Index;

            public DeletedUndoRecord(object deleted, IBindingList owningList, int index)
                : base(IdentifyObject(deleted))
            {
                this.Deleted = deleted;
                this.OwningList = owningList;
                this.Index = index;
            }

            public override void Undo(MainWindow mainWindow, Stack<UndoRecord> redoStack)
            {
                OwningList.Insert(Index, Deleted);

                if (redoStack != null)
                {
                    redoStack.Push(new InsertedUndoRecord(OwningList, Index));
                }
            }

            public override string GetMenuText(bool redo)
            {
                return String.Format(!redo ? "Delete {0}" : "Insert {0}", MenuText);
            }
        }

        private class InsertedUndoRecord : UndoRecord
        {
            public readonly IBindingList OwningList;
            public readonly int Index;

            public InsertedUndoRecord(IBindingList owningList, int index)
                : base(IdentifyObject(owningList[index]))
            {
                this.OwningList = owningList;
                this.Index = index;
            }

            public override void Undo(MainWindow mainWindow, Stack<UndoRecord> redoStack)
            {
                if (redoStack != null)
                {
                    redoStack.Push(new DeletedUndoRecord(OwningList[Index], OwningList, Index));
                }

                OwningList.RemoveAt(Index);
            }

            public override string GetMenuText(bool redo)
            {
                return String.Format(!redo ? "Insert {0}" : "Delete {0}", MenuText);
            }
        }

        private class MovedUndoRecord : UndoRecord
        {
            public readonly IBindingList OwningList;
            public readonly int Index;
            public readonly int Offset;

            public MovedUndoRecord(IBindingList owningList, int index, int offset)
                : base(IdentifyObject(owningList[index]))
            {
                this.OwningList = owningList;
                this.Index = index;
                this.Offset = offset;
            }

            public override void Undo(MainWindow mainWindow, Stack<UndoRecord> redoStack)
            {
                object o = OwningList[Index];
                OwningList.RemoveAt(Index);
                OwningList.Insert(Index + Offset, o);

                if (redoStack != null)
                {
                    redoStack.Push(new MovedUndoRecord(OwningList, Index + Offset, -Offset));
                }
            }

            public override string GetMenuText(bool redo)
            {
                return String.Format("Move {0} {1}", MenuText, ((Offset < 0 ? 1 : 0) ^ (!redo ? 1 : 0)) != 0 ? "Up" : "Down");
            }
        }


        //

        public string DisplayName
        {
            get
            {
                return !String.IsNullOrEmpty(savePath) ? Path.GetFileNameWithoutExtension(savePath) : "Untitled";
            }
        }

        public void GlobalNameChanged()
        {
            this.Text = this.DisplayName;
        }


        // 

        private void document_OnSetModified(object sender, EventArgs e)
        {
            if (!autosaveNeeded)
            {
                lastAutosave = DateTime.UtcNow;
            }
            autosaveNeeded = true;
        }

        private void timerAutosave_Tick(object sender, EventArgs e)
        {
            if ((DateTime.UtcNow - lastAutosave).TotalSeconds >= Program.Config.AutosaveInterval)
            {
                DoAutosave();
            }
        }

        // TODO: callers to this probably would want to save ALL open documents, not just theirs
        public void DoAutosave()
        {
            if (Program.Config.AutosaveEnabled && autosaveNeeded)
            {
                Cursor.Current = Cursors.WaitCursor;

                bool fallback = false;

                if (autosavePathTemplate == null)
                {
                    if (savePath != null)
                    {
                        autosavePathTemplate = Path.Combine(Path.GetDirectoryName(savePath), Path.GetFileNameWithoutExtension(savePath) + "-Autosave-{0}.oop");
                    }
                    else
                    {
                        fallback = true;
                        autosavePathTemplate = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), @"Desktop\Untitled-Autosave-{0}.oop");
                    }
                }

                string path = null;
                Stream stream = null;
                try
                {
                    while (stream == null)
                    {
                        path = String.Format(autosavePathTemplate, ++autosaveNumber);
                        bool tryFallback = false;
                        try
                        {
                            stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, Constants.BufferSize);
                        }
                        catch (IOException exception)
                        {
                            int hr = Marshal.GetHRForException(exception);
                            if (hr != unchecked((int)0x80070050)/*file exists*/)
                            {
                                tryFallback = true;
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // no write permission, disk full, other catastrophes
                            if (fallback)
                            {
                                throw;
                            }
                            tryFallback = true;
                        }
                        if (tryFallback)
                        {
                            fallback = true;
                            autosavePathTemplate = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), String.Concat(@"Desktop\", savePath != null ? Path.GetFileNameWithoutExtension(savePath) : "Untitled", "-autosave{0}.oop"));
                        }
                    }
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        document.Save(writer);
                    }
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Close();
                    }
                }

                if (autosaveLastPath != null)
                {
                    File.Delete(autosaveLastPath);
                }
                autosaveLastPath = path;
            }

            lastAutosave = DateTime.UtcNow;
            autosaveNeeded = false;

            Cursor.Current = Cursors.Default;
        }
    }
}
