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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class MainNewSchoolWindow : Form, Program.ITopLevelWindow
    {
        private readonly Registration registration = new Registration();
        private readonly Dictionary<Document, IMainWindowServices> mainWindowServiceProxies = new Dictionary<Document, IMainWindowServices>();

        private readonly NewSchoolDocument document;
        private string savePath;

        private OutputNewSchool output;
        private readonly MyBindingList<BindableParam> liveParams = new MyBindingList<BindableParam>();

        private readonly MyGridBinder<NewSchoolExpandedSourceRec> expandedSourceBinder;
        private readonly MyGridBinder<NewSchoolParticle> currentBinder;
        private readonly MyGridBinder<NewSchoolParticle> pendingBinder;
        private readonly MyGridBinder<BindableParam> defaultParamBinder;
        private readonly MyGridBinder<BindableParam> liveParamBinder;

        public MainNewSchoolWindow(NewSchoolDocument document, string savePath)
        {
            this.document = document;
            this.savePath = savePath;

            Debug.Assert(!String.IsNullOrEmpty(document.SourcePath));
            document.MakeUpToDate(EnsureMainWindowServiceProxy);


            InitializeComponent();

            this.buttonStart.BackgroundImage = Bitmaps1Class.gdiPlayMouseUp;
            this.buttonStop.BackgroundImage = Bitmaps1Class.gdiStopMouseUp;

            this.sliderDecibelVolume.State = new SliderState(-48.1, 18.1, SliderScale.Linear, 10, new double[] { 0, -42, -36, -30, -24, -18, -12, -6, 6, 12, 18 });

            this.newSchoolDocumentBindingSource.Add(document);

            InitializeParamsGrid(this.myGridTrackParamsDefault, null/*no source yet*/, out this.defaultParamBinder);

            InitializeParamsGrid(this.myGridTrackParamsLive, this.liveParams, out this.liveParamBinder);

            this.myGridSources.DefineColumns(new int[] { 100 });
            this.myGridSources.OnMouseDownEvent += MyGridSources_OnMouseDownEvent;
            this.myGridSources.OnMouseDoubleClickEvent += MyGridSources_OnMouseDoubleClickEvent;
            this.expandedSourceBinder = new MyGridBinder<NewSchoolExpandedSourceRec>(
                this.myGridSources,
                document.ExpandedSources,
                new MyGridBinder<NewSchoolExpandedSourceRec>.CellBinding[]
                {
                    new MyGridBinder<NewSchoolExpandedSourceRec>.CellBinding(
                        null,
                        i => new MyGrid.LabelCell(),
                        s => s.DisplayMoniker,
                        (s, v) => throw new InvalidOperationException()),
                });

            this.myGridCurrent.DefineColumns(new int[] { 100, 100, 100 });
            this.myGridCurrent.OnMouseDownEvent += MyGridCurrent_OnMouseDownEvent;
            this.currentBinder = new MyGridBinder<NewSchoolParticle>(
                this.myGridCurrent,
                document.CurrentParticles,
                new MyGridBinder<NewSchoolParticle>.CellBinding[]
                {
                    new MyGridBinder<NewSchoolParticle>.CellBinding(
                        NewSchoolParticle.Moniker_PropertyName,
                        i => new MyGrid.LabelCell(),
                        s => s.Moniker.Value,
                        (s, v) => throw new InvalidOperationException()),
                    new MyGridBinder<NewSchoolParticle>.CellBinding(
                        NewSchoolParticle.Sequence_PropertyName,
                        i => new MyGrid.LabelCell(),
                        s => s.Sequence,
                        (s, v) => throw new InvalidOperationException()),
                    new MyGridBinder<NewSchoolParticle>.CellBinding(
                        NewSchoolParticle.Sequence2_PropertyName,
                        i => new MyGrid.LabelCell(),
                        s => s.Sequence2,
                        (s, v) => throw new InvalidOperationException()),
                });

            this.myGridPending.DefineColumns(new int[] { 100, 200 });
            this.myGridPending.OnMouseDownEvent += MyGridPending_OnMouseDownEvent;
            this.myGridPending.CreateRow = (g, i) => document.InsertNewPendingParticle(Moniker.Empty, String.Empty);
            this.pendingBinder = new MyGridBinder<NewSchoolParticle>(
                this.myGridPending,
                document.PendingParticles,
                new MyGridBinder<NewSchoolParticle>.CellBinding[]
                {
                    new MyGridBinder<NewSchoolParticle>.CellBinding(
                        NewSchoolParticle.Moniker_PropertyName,
                        i => new MyGrid.TextEditCell(),
                        s => s.Moniker.Value,
                        (s, v) => s.Moniker = Moniker.Parse((string)v)),
                    new MyGridBinder<NewSchoolParticle>.CellBinding(
                        NewSchoolParticle.Sequence_PropertyName,
                        i => new MyGrid.TextEditCell(),
                        s => s.Sequence,
                        (s, v) => s.Sequence = (string)v),
                });

            toolStripDropDownExecutePreset.DropDownOpening += ToolStripDropDownExecutePreset_DropDownOpening;
        }

        private static void InitializeParamsGrid(
            MyGrid myGridParams,
            BindingList<BindableParam> underlying,
            out MyGridBinder<BindableParam> paramsBinder)
        {
            myGridParams.DefineColumns(new int[] { 150, 100, 150, 200 });
            paramsBinder = new MyGridBinder<BindableParam>(
                myGridParams,
                underlying,
                new MyGridBinder<BindableParam>.CellBindingBase[]
                {
                    new MyGridBinder<BindableParam>.CellBinding(
                        null,
                        i => new MyGrid.LabelCell(),
                        p => p.ParamName,
                        (p, v) => throw new InvalidOperationException()),
                    new MyGridBinder<BindableParam>.VariantCellBinding(
                        BindableParam.ParamValue_PropertyName,
                        new MyGridBinder<BindableParam>.OneBinding[]
                        {
                            new MyGridBinder<BindableParam>.OneBinding(
                                p => p.ParamType == typeof(double),
                                p => new MyGrid.TextEditCell(),
                                p => p.ParamValue.ToString(),
                                (p, v) => p.ParamValue = Double.Parse((string)v)),
                            new MyGridBinder<BindableParam>.OneBinding(
                                p => p.ParamType == typeof(int),
                                p => new MyGrid.TextEditCell(),
                                p => p.ParamValue.ToString(),
                                (p, v) => p.ParamValue = Int32.Parse((string)v)),
                            new MyGridBinder<BindableParam>.OneBinding(
                                p => p.ParamType == typeof(bool),
                                p => new MyGrid.OptionCell<bool>(
                                    Array.ConvertAll(p.ParamValueRange, v => (v.Item1, (bool)v.Item2))),
                                p => (bool)p.ParamValue,
                                (p, v) => p.ParamValue = v),
                        },
                        false/*throwOnNoMatch*/),
                    new MyGridBinder<BindableParam>.VariantCellBinding(
                        BindableParam.ParamValue_PropertyName,
                        new MyGridBinder<BindableParam>.OneBinding[]
                        {
                            new MyGridBinder<BindableParam>.OneBinding(
                                p => p.Slidable,
                                p => new MyGrid.SliderCell(p.Metrics),
                                p => p.ParamValue,
                                (p, v) => p.ParamValue = v),
                        },
                        false/*throwOnNoMatch*/),
                    new MyGridBinder<BindableParam>.CellBinding(
                        null,
                        p => new MyGrid.LabelCell(),
                        p => p.ParamHelp,
                        (p, v) => throw new NotSupportedException()),
                });
        }

        public static bool QuerySourcePathForDocument(ref string sourcePath)
        {
            // HACK: based on what WinMerge does - usability is suspect but still much better than FolderBrowserDialog.
            // See: http://www.codeproject.com/Articles/44914/Select-file-or-folder-from-the-same-dialog
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                if (!String.IsNullOrEmpty(sourcePath))
                {
                    dialog.InitialDirectory = sourcePath;
                }
                dialog.ValidateNames = false;
                dialog.CheckFileExists = false;
                dialog.CheckPathExists = false;
                dialog.Title = "Select a Folder";
                dialog.FileName = "Select Folder.";
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return false;
                }
                sourcePath = Path.GetDirectoryName(dialog.FileName);
                return true;
            }
        }

        private void MyGridPending_OnMouseDownEvent(object sender, MouseEventArgs e, int rowIndex, int columnIndex, out bool handled)
        {
            handled = false;

            if (e.Button == MouseButtons.Right)
            {
                if (columnIndex == 1) // sequence
                {
                    Moniker moniker = document.PendingParticles[rowIndex].Moniker;

                    List<ToolStripItem> sequences = new List<ToolStripItem>();

                    List<string> sortedSequenceNames = GetSequenceNames(moniker);
                    foreach (string sequenceName in sortedSequenceNames)
                    {
                        ToolStripMenuItem item = new ToolStripMenuItem(sequenceName);
                        item.Click += delegate (object _sender, EventArgs _e)
                        {
                            document.PendingParticles[rowIndex].Sequence = sequenceName;
                        };
                        sequences.Add(item);
                    }

                    contextMenuStrip.Items.Clear();
                    contextMenuStrip.Items.AddRange(sequences.ToArray());

                    contextMenuStrip.Show(myGridPending, e.Location);

                    handled = true;
                }
                else if (columnIndex == 0) // moniker
                {
                    List<ToolStripItem> monikers = new List<ToolStripItem>();

                    List<Moniker> sortedMonikers = GetMonikers();
                    foreach (Moniker moniker in sortedMonikers)
                    {
                        ToolStripMenuItem item = new ToolStripMenuItem(moniker.Value);
                        item.Click += delegate (object _sender, EventArgs _e)
                        {
                            document.PendingParticles[rowIndex].Moniker = moniker;
                        };
                        monikers.Add(item);
                    }

                    contextMenuStrip.Items.Clear();
                    contextMenuStrip.Items.AddRange(monikers.ToArray());

                    contextMenuStrip.Show(myGridPending, e.Location);

                    handled = true;
                }
            }
        }

        private List<string> GetSequenceNames(Moniker moniker)
        {
            Moniker documentMoniker = moniker.AsSourceOnly();
            string trackMoniker = moniker.Track;
            NewSchoolExpandedSourceRec[] expandedSources = String.IsNullOrEmpty(trackMoniker)
                ? document.ExpandedSources.FindAll(delegate (NewSchoolExpandedSourceRec expandedSource) { return expandedSource.Moniker.AsSourceOnly().Equals(documentMoniker) && (expandedSource.Track != null); })
                : new NewSchoolExpandedSourceRec[] { document.ExpandedSources.Find(delegate (NewSchoolExpandedSourceRec expandedSource) { return moniker.Equals(expandedSource.Moniker); }) };
            Dictionary<string, bool> sequenceNames = new Dictionary<string, bool>();
            foreach (NewSchoolExpandedSourceRec expandedSource in expandedSources)
            {
                COWBindingList<FrameObjectRec> frameArray = expandedSource.Track.FrameArray;
                for (int i = 0; i < frameArray.Count; i++)
                {
                    if (frameArray[i].IsThisACommandFrame)
                    {
                        CommandNoteObjectRec command = (CommandNoteObjectRec)frameArray[i][0];
                        if (command.GetCommandOpcode() == NoteCommands.eCmdSequenceBegin)
                        {
                            string sequenceName = command.GetCommandStringArg1();
                            sequenceNames[sequenceName] = false;
                        }
                    }
                }
            }
            List<string> sortedSequenceNames = new List<string>(sequenceNames.Keys);
            sortedSequenceNames.Sort();
            return sortedSequenceNames;
        }

        private List<Moniker> GetMonikers()
        {
            List<Moniker> monikers = new List<Moniker>(document.ExpandedSources.Count);
            foreach (NewSchoolExpandedSourceRec expandedSource in document.ExpandedSources)
            {
                monikers.Add(expandedSource.Moniker);
            }
            monikers.Sort();
            return monikers;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            BindingList<NewSchoolSourceRec> modified = new BindingList<NewSchoolSourceRec>();
            foreach (NewSchoolSourceRec source in document.Sources)
            {
                if (source.Document.Modified)
                {
                    modified.Add(source);
                }
            }

            if (!registration.CloseAll())
            {
                e.Cancel = true;
                return;
            }

            if (modified.Count != 0)
            {
                using (SaveComponentNewSchoolDialog dialog = new SaveComponentNewSchoolDialog(modified))
                {
                    switch (dialog.ShowDialog())
                    {
                        default:
                        case DialogResult.Cancel:
                            e.Cancel = true;
                            return;
                        case DialogResult.Ignore:
                            break;
                        case DialogResult.OK:
                            foreach (NewSchoolSourceRec source in dialog.GetListOfItemsToSave())
                            {
                                try
                                {
                                    // TODO: use temp file to avoid data loss
                                    using (FileStream stream = new FileStream(source.Path, FileMode.Create))
                                    {
                                        using (BinaryWriter writer = new BinaryWriter(stream))
                                        {
                                            source.Document.Save(writer);
                                        }
                                    }
                                }
                                catch (Exception exception)
                                {
                                    MessageBox.Show(String.Format("Saving failed for {0}: {1}", source.Path, exception.Message));
                                    e.Cancel = true;
                                    return;
                                }
                            }
                            break;
                    }
                }
            }

            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            buttonStop_Click(null, null);

            base.OnFormClosed(e);
        }

        public void SaveCopyAs(string path)
        {
            // TODO:
        }

        public string SavePath { get { return savePath; } }

        private IMainWindowServices EnsureMainWindowServiceProxy(Document document, string savePath)
        {
            IMainWindowServices services;
            if (!mainWindowServiceProxies.TryGetValue(document, out services))
            {
                services = new NewSchoolMainWindowServicesProxy(document, savePath);
                mainWindowServiceProxies.Add(document, services);
            }
            return services;
        }

        private bool SaveHelper(string path, bool clearModified)
        {
            if (registration.EnsureValidateAndCommit())
            {
                document.Save(path);
                Program.ReferenceRecentDocument(path);

                if (clearModified)
                {
                    // TODO:
                    //document.Modified = false;

                    //if (autosaveLastPath != null)
                    //{
                    //    File.Delete(autosaveLastPath);
                    //}
                    //autosaveLastPath = null;
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
                    dialog.Filter = "Out Of Phase 2 File (.oop2)|*.oop2|Any File Type (*)|*";
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

        private void SaveAll()
        {
            if (!SaveOrSaveAsHelper())
            {
                return;
            }

            foreach (NewSchoolSourceRec source in document.Sources)
            {
                if (source.Document.Modified)
                {
                    try
                    {
                        // TODO: use temp file to avoid data loss
                        using (FileStream stream = new FileStream(source.Path, FileMode.Create))
                        {
                            using (BinaryWriter writer = new BinaryWriter(stream))
                            {
                                source.Document.Save(writer);
                                source.Document.Modified = false;
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(String.Format("Saving failed for {0}: {1}", source.Path, exception.Message));
                        return;
                    }
                }
            }
        }


        //
        // Live track parameter binding control
        //

        private NewSchoolExpandedSourceRec currentSource;
        private void SyncParamBoardEntries()
        {
            myGridTrackParamsLive.DeferUpdates();
            try
            {
                NewSchoolExpandedSourceRec source;
                if ((output == null)
                    || !myGridSources.HasSelection
                    || ((source = document.ExpandedSources[myGridSources.SelectedRow]).Track == null))
                {
                    currentSource = null;
                    liveParams.Clear();
                    if (output != null)
                    {
                        output.EnqueueParamBoardEntry(null); // clear synth hooks
                    }
                    return;
                }

                if (currentSource != source)
                {
                    currentSource = source;
                    liveParams.Clear();
                    if (output != null)
                    {
                        output.EnqueueParamBoardEntry(null);
                    }
                    for (int i = 0; i < source.TrackParams.BindableParamList.Count; i++)
                    {
                        BindableParam template = source.TrackParams.BindableParamList[i];

                        ParamBoardEntry entry = new ParamBoardEntry(source.Track, source.Moniker.WithParam(template.ParamName));
                        if (output != null)
                        {
                            output.EnqueueParamBoardEntry(entry);
                        }

                        BindableParamDelegated delegated;
                        if (template.ParamType == typeof(double))
                        {
                            delegated = new BindableParamDelegated<double>(
                                template,
                                () => entry.Value,
                                v => entry.SetValueFromUI(v));
                        }
                        else if (template.ParamType == typeof(int))
                        {
                            delegated = new BindableParamDelegated<int>(
                                template,
                                () => (int)entry.Value,
                                v => entry.SetValueFromUI(v));
                        }
                        else if (template.ParamType == typeof(bool))
                        {
                            delegated = new BindableParamDelegated<bool>(
                                template,
                                () => entry.Value < 0,
                                v => entry.SetValueFromUI(v ? -1 : 0));
                        }
                        else if (template.ParamType == typeof(string))
                        {
                            delegated = new BindableParamDelegated<string>(
                                template,
                                () => String.Empty,
                                v => { });
                        }
                        else
                        {
                            throw new ArgumentException();
                        }

                        delegated.ParamBoardEntry = entry;

                        liveParams.Add(delegated);
                    }
                }

                // sync
                for (int i = 0; i < liveParams.Count; i++)
                {
                    BindableParamDelegated delegated = (BindableParamDelegated)liveParams[i];
                    if (delegated.ParamBoardEntry.GetUpdateUI())
                    {
                        delegated.Poke();
                    }

                    if ((output != null) && !output.ParamBoardEntriesQueued() && !output.ParamBoard.ContainsKey(delegated.ParamBoardEntry))
                    {
                        output.EnqueueParamBoardEntry(delegated.ParamBoardEntry);
                    }
                }
            }
            finally
            {
                myGridTrackParamsLive.UndeferUpdates();
            }
        }

        private void timerUpdateStatus_Tick(object sender, EventArgs e)
        {
            if (output != null)
            {
                myProgressBarLoopPosition.SetAll(1, output.LoopPosition, -output.CriticalThreshhold);

                myProgressBarDutyCycle.Level = output.DutyCycle;

                {
                    (float shortValue, float longValue) = output.MeterLevel;
                    shortValue = (float)Math.Log(Math.Max(shortValue, 1e-6f)) * (float)Constants.LN_DB;
                    longValue = (float)Math.Log(Math.Max(longValue, 1e-6f)) * (float)Constants.LN_DB;
                    meterOutput.SetValues(shortValue, longValue);
                }

                List<TrackStatusRec> statusList = output.DequeueStatus();
                if (statusList != null)
                {
                    // Merge displayed status, recently obtained status, and committed pending changes

                    // TODO: this happens a lot so these lookups need to be made much more efficient!

                    // convert current status
                    List<NewSchoolParticle> particlesList = statusList.ConvertAll(
                        delegate (TrackStatusRec status)
                        {
                            NewSchoolSourceRec source = document.Sources.Find(
                                delegate (NewSchoolSourceRec s) { return s.Document == status.Document; });
                            return new NewSchoolParticle(
                                document,
                                NewSchoolDocument.CurrentParticles_PropertyName,
                                NewSchoolDocument.MakeMoniker(source, status.Track),
                                status.Sequence);
                        });

                    // merge in commmitted request, if any
                    List<TrackRequestRec> requestList = output.QueryRequest();
                    if (requestList != null)
                    {
                        requestList = new List<TrackRequestRec>(requestList);
                        requestList.Sort(
                            delegate (TrackRequestRec l, TrackRequestRec r)
                            {
                                NewSchoolExpandedSourceRec sourceL = document.ExpandedSources.Find(
                                    delegate (NewSchoolExpandedSourceRec s) { return (s.Document == l.Document) && (s.Track == l.Track); });
                                NewSchoolExpandedSourceRec sourceR = document.ExpandedSources.Find(
                                    delegate (NewSchoolExpandedSourceRec s) { return (s.Document == r.Document) && (s.Track == r.Track); });
                                return sourceL.Moniker.CompareTo(sourceR.Moniker);
                            });
                        foreach (TrackRequestRec request in requestList)
                        {
                            NewSchoolExpandedSourceRec source = document.ExpandedSources.Find(
                                delegate (NewSchoolExpandedSourceRec s) { return (s.Document == request.Document) && (s.Track == request.Track); });
                            NewSchoolParticle particle = particlesList.Find(p => p.Moniker.Equals(source.Moniker));
                            if (particle != null)
                            {
                                particle.Sequence2 = request.Sequence;
                            }
                            else
                            {
                                particlesList.Add(
                                    new NewSchoolParticle(
                                        document,
                                        NewSchoolDocument.CurrentParticles_PropertyName,
                                        source.Moniker,
                                        null,
                                        request.Sequence));
                            }
                        }
                    }

                    document.UpdateCurrentParticles(particlesList);
                }
            }
            else
            {
                if (document.CurrentParticles.Count != 0)
                {
                    document.CurrentParticles.Clear();
                }
            }

            SyncParamBoardEntries();
        }

        private void SetPendingToPreset(NewSchoolPresetDefinitionRec preset, bool replace)
        {
            Dictionary<Moniker, bool> touched = replace ? new Dictionary<Moniker, bool>() : null;

            foreach (NewSchoolParticle target in preset.Targets)
            {
                // TODO: speed up indexing

                NewSchoolParticle existing = document.PendingParticles.Find(p => p.Moniker.Equals(target.Moniker));
                if (existing != null)
                {
                    existing.Sequence = target.Sequence;
                }
                else
                {
                    existing = document.InsertNewPendingParticle(target.Moniker, target.Sequence);
                }
                if (replace)
                {
                    touched.Add(existing.Moniker, false);
                }
            }

            if (replace)
            {
                // squelch tracks not belonging to preset

                foreach (NewSchoolParticle existing in document.PendingParticles)
                {
                    if (!touched.ContainsKey(existing.Moniker))
                    {
                        existing.Sequence = OutputNewSchool.EndSequencing;
                        touched.Add(existing.Moniker, false);
                    }
                }

                foreach (NewSchoolParticle existing in document.CurrentParticles)
                {
                    if (!touched.ContainsKey(existing.Moniker))
                    {
                        document.InsertNewPendingParticle(existing.Moniker, OutputNewSchool.EndSequencing);
                        touched.Add(existing.Moniker, false);
                    }
                }
            }
        }


        //
        // Actions
        //

        private void Start()
        {
            if ((output != null) && output.Stopped)
            {
                output.Dispose();
                output = null;
            }
            if (output == null)
            {
                output = new OutputNewSchool(document, new NewSchoolMainWindowServicesProxy());
                output.Mute = checkBoxMute.Checked;
                Commit();
                output.Start();
            }
            buttonStart.Enabled = false;
            buttonStart.BackgroundImage = Bitmaps1Class.gdiPlayingMouseUp;
            tabControlTrackParams.SelectTab(tabPageTrackParamsLive);
        }

        private void Stop()
        {
            if (output != null)
            {
                output.Stop();
                output.Dispose();
                output = null;

                document.CurrentParticles.Clear();
                myProgressBarLoopPosition.Level = 0;
                meterOutput.SetValues(meterOutput.LowLevel, meterOutput.LowLevel);
                myProgressBarDutyCycle.Level = 0;
            }
            buttonStart.Enabled = true;
            buttonStart.BackgroundImage = Bitmaps1Class.gdiPlayMouseUp;
            tabControlTrackParams.SelectTab(tabPageTrackParamsDefault);
        }

        private void Commit()
        {
            myGridPending.CommitWithoutEndEdit();

            // TODO: make these searches more efficient

            Dictionary<Moniker, TrackRequestRec> actions = new Dictionary<Moniker, TrackRequestRec>();

            for (int i = 0; i < document.PendingParticles.Count; i++)
            {
                const string PretendIDontExistToken = ".";

                NewSchoolParticle particle = document.PendingParticles[i];
                if (String.Equals(particle.Sequence, PretendIDontExistToken))
                {
                    continue;
                }

                NewSchoolGroupDefinition group = document.GroupDefinitions.Find(g => particle.Moniker.Equals(new Moniker(g.GroupName)));
                foreach (Moniker moniker in group != null ? (IEnumerable<Moniker>)group.MemberMonikers : new Moniker[] { particle.Moniker })
                {
                    Moniker documentMoniker = moniker.AsSourceOnly();
                    Document particleDocument = document.Sources.Find(delegate (NewSchoolSourceRec source) { return documentMoniker.Equals(source.Moniker); })?.Document;
                    if (particleDocument != null) // TODO: missing name error feedback
                    {
                        string trackName = moniker.Track;
                        NewSchoolExpandedSourceRec[] expandedSources = String.IsNullOrEmpty(trackName)
                            ? document.ExpandedSources.FindAll(x => (x.Document == particleDocument) && (x.Track != null))
                            : new NewSchoolExpandedSourceRec[] { document.ExpandedSources.Find(x => moniker.Equals(x.Moniker)) };
                        foreach (NewSchoolExpandedSourceRec expandedSource in expandedSources)
                        {
                            // later entries may override earlier entries (especially useful if a whole-document command is then
                            // followed by a track-specific command)
                            actions[expandedSource.Moniker] =
                                new TrackRequestRec(
                                    particleDocument,
                                    expandedSource.Track,
                                    expandedSource.TrackParams,
                                    particle.Sequence);

                            NewSchoolParticle current = document.CurrentParticles.Find(p => expandedSource.Moniker.Equals(p.Moniker));
                            if (current != null)
                            {
                                current.Sequence2 = particle.Sequence;
                            }
                            else
                            {
                                document.CurrentParticles.Add(
                                    new NewSchoolParticle(
                                        document,
                                        NewSchoolDocument.CurrentParticles_PropertyName,
                                        expandedSource.Moniker,
                                        null,
                                        particle.Sequence));
                            }
                        }
                    }
                }
            }

            // atomic thread crossing
            if (output != null)
            {
                List<TrackRequestRec> list = new List<TrackRequestRec>(actions.Values);
                output.SetRequest(list);
            }
        }

        private void Settings()
        {
            using (PlayPrefsDialogNewSchool dialog = new PlayPrefsDialogNewSchool(document))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    // TODO: make parameter changes cancellable
                }
            }
        }

        private void MyGridSources_OnMouseDownEvent(object sender, MouseEventArgs e, int rowIndex, int columnIndex, out bool handled)
        {
            handled = false;

            NewSchoolExpandedSourceRec source = document.ExpandedSources[rowIndex];

            // rebind track parameter control panels
            if (source.TrackParams != null)
            {
                defaultParamBinder.SetDataSource(source.TrackParams.BindableParamList);
            }
            else
            {
                defaultParamBinder.SetDataSource(null);
            }

            // immediately create and set focus to the sequence cell for a row in the "pending" control for fast changes
            for (int i = 0; i < document.PendingParticles.Count; i++)
            {
                if (source.Moniker.Equals(document.PendingParticles[i].Moniker))
                {
                    this.myGridPending.SetSelection(i, 1/*sequence name column*/);
                    this.myGridPending.BeginEdit(i, 1/*sequence name column*/, true/*selectAll*/);
                    this.myGridPending.Select();
                    return;
                }
            }
            AddAndSelectNewPendingParticle(source.Moniker, String.Empty);
        }

        private void AddAndSelectNewPendingParticle(Moniker moniker, string sequence)
        {
            document.InsertNewPendingParticle(moniker, sequence);
            this.myGridPending.SetSelection(document.PendingParticles.Count - 1, 1/*sequence name column*/);
            this.myGridPending.BeginEdit(document.PendingParticles.Count - 1, 1/*sequence name column*/, true/*selectAll*/);
            this.myGridPending.Select();
        }

        private void MyGridCurrent_OnMouseDownEvent(object sender, MouseEventArgs e, int rowIndex, int columnIndex, out bool handled)
        {
            handled = false;

            // immediately create and set focus to the sequence cell for a row in the "pending" control for fast changes
            NewSchoolParticle current = document.CurrentParticles[rowIndex];
            for (int i = 0; i < document.PendingParticles.Count; i++)
            {
                if (current.Moniker.Equals(document.PendingParticles[i].Moniker))
                {
                    this.myGridPending.SetSelection(i, 1/*sequence name column*/);
                    this.myGridPending.BeginEdit(i, 1/*sequence name column*/, true/*selectAll*/);
                    this.myGridPending.Select();
                    return;
                }
            }
            AddAndSelectNewPendingParticle(current.Moniker, String.Empty);
        }

        private void MyGridSources_OnMouseDoubleClickEvent(object sender, MouseEventArgs e, int rowIndex, int columnIndex, out bool handled)
        {
            NewSchoolExpandedSourceRec source = document.ExpandedSources[rowIndex];
            if (source.Track != null)
            {
                if (!registration.Activate(source.Track))
                {
                    new TrackWindow(registration, source.Track, EnsureMainWindowServiceProxy(source.Document, source.SourcePath)).Show();
                }
            }
            handled = true;
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAll();
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            myGridPending.Select();
            Start();
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            myGridPending.Select();
            Start();
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            myGridPending.Select();
            Stop();
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            myGridPending.Select();
            Stop();
        }

        private void commitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            myGridPending.Select();
            Start();
            Commit();
        }

        private void buttonCommit_Click(object sender, EventArgs e)
        {
            myGridPending.Select();
            Start();
            Commit();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings();
        }

        private void buttonSettings_Click(object sender, EventArgs e)
        {
            Settings();
        }

        private void changeSourceDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string sourcePath = document.SourcePath;
            if (QuerySourcePathForDocument(ref sourcePath))
            {
                document.SetSourcePath(sourcePath);
            }
        }

        private void groupsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!GroupDefinitionsNewSchoolWindow.TryActivate(document, registration))
            {
                GroupDefinitionsNewSchoolWindow window = new GroupDefinitionsNewSchoolWindow(document, registration);
                window.Show();
            }
        }

        private void sequencePresetsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!PresetDefinitionNewSchoolWindow.TryActivate(document, registration))
            {
                PresetDefinitionNewSchoolWindow window = new PresetDefinitionNewSchoolWindow(document, registration);
                window.Show();
            }
        }

        private void newClassicDocumentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Document document = new Document();
            new MainWindow(document, null).Show();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: don't open files that are open as part of this document (will need global registration and code
            // change in MainWindow.cs as well)

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Out Of Phase File (.oop)|*.oop|Any File Type (*)|*";
                DialogResult result = dialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    string path = dialog.FileName;
                    MainWindow.TryOpenFilePath(path);
                }
            }
        }

        private void checkBoxMute_CheckedChanged(object sender, EventArgs e)
        {
            if (output != null)
            {
                output.Mute = checkBoxMute.Checked;
            }
        }

        private void buttonClearPending_Click(object sender, EventArgs e)
        {
            document.PendingParticles.Clear();
        }

        private void buttonNewPending_Click(object sender, EventArgs e)
        {
            AddAndSelectNewPendingParticle(Moniker.Empty, String.Empty);
        }

        private void toolStripButtonClearPending_Click(object sender, EventArgs e)
        {
            document.PendingParticles.Clear();
        }

        private void toolStripButtonNewPending_Click(object sender, EventArgs e)
        {
            AddAndSelectNewPendingParticle(Moniker.Empty, String.Empty);
        }

        private void ToolStripDropDownExecutePreset_DropDownOpening(object sender, EventArgs e)
        {
            toolStripDropDownExecutePreset.DropDownItems.Clear();
            foreach (NewSchoolPresetDefinitionRec preset in document.PresetDefinitions)
            {
                toolStripDropDownExecutePreset.DropDownItems.Add(preset.PresetName);
                ToolStripItem item = toolStripDropDownExecutePreset.DropDownItems[toolStripDropDownExecutePreset.DropDownItems.Count - 1];
                item.Click += delegate (object _sender, EventArgs _e)
                {
                    SetPendingToPreset(preset, (ModifierKeys & Keys.Control) != 0);
                };
            }
        }
    }

    public class NewSchoolMainWindowServicesProxy : IMainWindowServices
    {
        private readonly Document document;
        private string savePath;


        public NewSchoolMainWindowServicesProxy()
        {
        }

        public NewSchoolMainWindowServicesProxy(Document document, string savePath)
        {
            this.document = document;
            this.savePath = savePath;
        }


        public string DisplayName { get { return !String.IsNullOrEmpty(savePath) ? Path.GetFileNameWithoutExtension(savePath) : "Untitled"; } }

        public Document Document { get { return document; } }

        public string SavePath { get { return savePath; } }

        // From System.Windows.Forms.Control
        // build in this scenario is always done on main thread
        public bool InvokeRequired { get { return false; } }

        public void AddMiscForm(Form form)
        {
            Debugger.Break(); // TODO: remove
            throw new NotSupportedException();
        }

        public Form CreateAndShowEditor(object dataObject)
        {
            Debugger.Break(); // TODO: remove
            throw new NotSupportedException();
        }

        public void DefaultBuildFailedCallback(object sender, BuildErrorInfo errorInfo)
        {
            Debugger.Break(); // TODO: remove
            throw new NotSupportedException();
        }

        public void DeleteObject(object o, IBindingList list)
        {
            Debugger.Break(); // TODO: remove
            throw new NotSupportedException();
        }

        public void EnableMenuItems(MenuStripManager menuStrip)
        {
        }

        public bool ExecuteMenuItem(MenuStripManager menuStrip, ToolStripMenuItem menuItem)
        {
            return false;
        }

        public IInteractionWindowService GetInteractionWindow()
        {
            return new NewSchoolInteractionWindowProxy();
        }

        public IPlayPrefsProvider GetPlayPrefsProvider()
        {
            Debugger.Break(); // TODO: remove
            throw new NotImplementedException();
        }

        // From System.Windows.Forms.Control
        public object Invoke(Delegate method)
        {
            Debugger.Break(); // TODO: remove
            throw new NotSupportedException();
        }

        // From System.Windows.Forms.Control
        public object Invoke(Delegate method, params object[] args)
        {
            Debugger.Break(); // TODO: remove
            throw new NotSupportedException();
        }

        public bool MakeUpToDate()
        {
            return true; // TODO: ?
        }

        public bool MakeUpToDateFunctions()
        {
            Debugger.Break(); // TODO: remove
            throw new NotSupportedException();
        }

        public bool PromptResumableError(string message)
        {
            Debugger.Break(); // TODO: remove
            throw new NotSupportedException();
        }

        public void RemoveMiscForm(Form form)
        {
            Debugger.Break(); // TODO: remove
            throw new NotSupportedException();
        }
    }

    public class NewSchoolInteractionWindowProxy : IInteractionWindowService
    {
        public void Append(string text)
        {
        }

        // From System.Windows.Forms.Control
        public bool InvokeRequired { get { return false; } }

        // From System.Windows.Forms.Control
        public object Invoke(Delegate method)
        {
            Debugger.Break(); // TODO: remove
            throw new NotSupportedException();
        }

        // From System.Windows.Forms.Control
        public object Invoke(Delegate method, params object[] args)
        {
            Debugger.Break(); // TODO: remove
            throw new NotSupportedException();
        }
    }
}
