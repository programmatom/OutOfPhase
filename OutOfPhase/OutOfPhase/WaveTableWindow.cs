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
using System.ComponentModel;
using System.Diagnostics;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class WaveTableWindow : Form, IMenuStripManagerHandler, IGlobalNameChange
    {
        private readonly Registration registration;
        private readonly WaveTableObjectRec waveTableObject;
        private readonly IMainWindowServices mainWindow;

        private readonly Stack<WaveTableStorageRec> undo = new Stack<WaveTableStorageRec>();
        private readonly Stack<WaveTableStorageRec> redo = new Stack<WaveTableStorageRec>();

        private bool suppressTableOrFrameChange = true; // suppress until after form is loaded - since init fires notifications

        public WaveTableWindow(Registration registration, WaveTableObjectRec waveTableObject, IMainWindowServices mainWindow)
        {
            this.registration = registration;
            this.waveTableObject = waveTableObject;
            this.mainWindow = mainWindow;

            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            this.textBoxFormula.TextService = Program.Config.EnableDirectWrite ? TextEditor.TextService.DirectWrite : TextEditor.TextService.Uniscribe;
            this.textBoxFormula.AutoIndent = Program.Config.AutoIndent;

            DpiChangeHelper.ScaleFont(this, Program.Config.AdditionalUIZoom);

            menuStripManager.SetGlobalHandler(mainWindow);
            menuStripManager.HookUpTextEditorWindowHelper(this.textEditorWindowHelper);
            menuStripManager.HookUpTextBoxWindowHelper(this.textBoxWindowHelper);

            waveTableControl.WaveTableObject = waveTableObject;
            hScrollBarWaveTable.Scroll += new ScrollEventHandler(waveTableControl.OnIndexChanged);

            foreach (int i in WaveTableObjectRec.NumFramesAllowedValues)
            {
                comboBoxNumFrames.Items.Add(i.ToString());
            }

            foreach (string item in EnumUtility.GetDescriptions(WaveTableObjectRec.NumBitsAllowedValues))
            {
                comboBoxNumBits.Items.Add(item);
            }

            documentBindingSource.Add(mainWindow.Document);
            waveTableObjectRecBindingSource.Add(waveTableObject);
            waveTableObject.PropertyChanged += WaveTableObject_PropertyChanged;

            textBoxName.TextChanged += new EventHandler(textBoxName_TextChanged);
            GlobalNameChanged();

            buttonTest.MouseDown += new MouseEventHandler(buttonTest_MouseDown);
            buttonTest.MouseUp += new MouseEventHandler(buttonTest_MouseUp);

            textBoxNumTables.Validated += new EventHandler(textBoxNumTables_TextChanged);
            comboBoxNumFrames.TextChanged += new EventHandler(comboBoxNumFrames_TextChanged);
            comboBoxNumBits.TextChanged += new EventHandler(ComboBoxNumBits_TextChanged);

            tabControlWave.SelectedIndexChanged += TabControlWave_SelectedIndexChanged;
            //
            dataGridViewWave.CellValueNeeded += DataGridViewWave_CellValueNeeded;
            dataGridViewWave.CellValuePushed += DataGridViewWave_CellValuePushed;
            RebuildDataGrid();
            //
            labelScale.Visible = false;
            comboBoxScale.Visible = false;
            comboBoxScale.SelectedIndex = 0;

            registration.Register(waveTableObject, this);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            waveTableObject.PropertyChanged -= WaveTableObject_PropertyChanged;
            registration.Unregister(waveTableObject, this);
            base.OnFormClosed(e);
        }

        protected override void OnShown(EventArgs e)
        {
            PersistWindowRect.OnShown(this, waveTableObject.SavedWindowXLoc, waveTableObject.SavedWindowYLoc, waveTableObject.SavedWindowWidth, waveTableObject.SavedWindowHeight);
            base.OnShown(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            suppressTableOrFrameChange = false; // ui element changes now due to user action
        }

        private void ResizeMove()
        {
            if (Visible)
            {
                short x, y, width, height;
                PersistWindowRect.OnResize(this, out x, out y, out width, out height);
                waveTableObject.SavedWindowXLoc = x;
                waveTableObject.SavedWindowYLoc = y;
                waveTableObject.SavedWindowWidth = width;
                waveTableObject.SavedWindowHeight = height;
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

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            menuStripManager.SetActiveHandler(this);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            menuStripManager.SetActiveHandler(null);
            base.OnDeactivate(e);
        }

        protected override void WndProc(ref Message m)
        {
            dpiChangeHelper.WndProcDelegate(ref m);
            base.WndProc(ref m);
        }


        //

        private void comboBoxNumFrames_TextChanged(object sender, EventArgs e)
        {
            if (suppressTableOrFrameChange)
            {
                return;
            }

            int NumberOfTables = waveTableObject.NumTables;

            int OriginalNumFrames = waveTableObject.NumFrames;
            int NewNumFrames;
            if (!Int32.TryParse(comboBoxNumFrames.Text, out NewNumFrames))
            {
                return; // TODO: error
            }

            if (NewNumFrames == OriginalNumFrames)
            {
                return;
            }

            /* create a new table */
            WaveTableStorageRec NewTable = new WaveTableStorageRec(
                NumberOfTables,
                NewNumFrames,
                waveTableObject.WaveTableData.NumBits);

            if (NewNumFrames > OriginalNumFrames)
            {
                /* this one handles interpolating between entries to expand the table */

                int ExpansionFactor = NewNumFrames / OriginalNumFrames;
                for (int TableScan = 0; TableScan < NumberOfTables; TableScan++)
                {
                    for (int NewFrameScan = 0; NewFrameScan < NewNumFrames; NewFrameScan++)
                    {
                        double PrecisePositioning = (double)NewFrameScan / (double)NewNumFrames
                            * (double)OriginalNumFrames;
                        int LeftIndex = (int)PrecisePositioning;
                        int RightIndex = LeftIndex + 1;
                        double RightWeight = PrecisePositioning - LeftIndex;
                        double LeftWeight = 1 - RightWeight;
                        double LeftValue = waveTableObject.WaveTableData.ListOfTables[TableScan][LeftIndex];
                        double RightValue = waveTableObject.WaveTableData.ListOfTables[TableScan][RightIndex % OriginalNumFrames];
                        double ResultantComposite = LeftValue * LeftWeight + RightValue * RightWeight;
                        NewTable.ListOfTables[TableScan][NewFrameScan] = (float)ResultantComposite;
                    }
                }
            }
            else
            {
                /* this one handles averaging the frames for table compression */

                int FoldingFactor = OriginalNumFrames / NewNumFrames;
                for (int TableScan = 0; TableScan < NumberOfTables; TableScan++)
                {
                    for (int NewFrameScan = 0; NewFrameScan < NewNumFrames; NewFrameScan++)
                    {
                        double Accumulator = 0;
                        for (int SumScan = 0; SumScan < FoldingFactor; SumScan++)
                        {
                            Accumulator += waveTableObject.WaveTableData
                                .ListOfTables[TableScan][NewFrameScan * FoldingFactor + SumScan];
                        }
                        double Average = Accumulator / FoldingFactor;
                        NewTable.ListOfTables[TableScan][NewFrameScan] = (float)Average;
                    }
                }
            }

            NewTable.TruncateBits();

            undo.Push(waveTableObject.WaveTableData);
            redo.Clear();
            waveTableObject.WaveTableData = NewTable;
        }

        private void textBoxNumTables_TextChanged(object sender, EventArgs e)
        {
            if (suppressTableOrFrameChange)
            {
                return;
            }

            int OriginalNumTables = waveTableObject.NumTables;
            int NewNumTables;
            if (!Int32.TryParse(textBoxNumTables.Text, out NewNumTables))
            {
                return; // TODO: error
            }
            if (NewNumTables != OriginalNumTables)
            {
                WaveTableStorageRec NewTable = new WaveTableStorageRec(
                    NewNumTables,
                    waveTableObject.NumFrames,
                    waveTableObject.NumBits);

                if ((NewNumTables != 0) && (OriginalNumTables != 0))
                {
                    int NumFrames = NewTable.NumFrames;
                    /* we use linear interpolation between adjacent tables to create new tables. */
                    for (int TableScan = 0; TableScan < NewNumTables; TableScan++)
                    {
                        double LeftWeight;
                        double RightWeight;
                        int LeftIndex;
                        int RightIndex;
                        double PrecisePositioning;

                        if (NewNumTables > 1)
                        {
                            PrecisePositioning = (double)TableScan / (double)(NewNumTables - 1)
                                * (double)(OriginalNumTables - 1);
                        }
                        else
                        {
                            PrecisePositioning = (double)(OriginalNumTables - 1) / 2;
                        }
                        LeftIndex = (int)PrecisePositioning;
                        RightIndex = LeftIndex + 1;
                        RightWeight = PrecisePositioning - LeftIndex;
                        LeftWeight = 1 - RightWeight;
                        for (int FrameScan = 0; FrameScan < NumFrames; FrameScan++)
                        {
                            double LeftValue;
                            double RightValue;
                            double ResultantComposite;

                            LeftValue = waveTableObject.WaveTableData.ListOfTables[LeftIndex][FrameScan];
                            if (RightIndex < OriginalNumTables)
                            {
                                RightValue = waveTableObject.WaveTableData.ListOfTables[RightIndex][FrameScan];
                            }
                            else
                            {
                                RightValue = 0;
                                Debug.Assert(RightWeight == 0);
                            }
                            ResultantComposite = LeftValue * LeftWeight + RightValue * RightWeight;
                            NewTable.ListOfTables[TableScan][FrameScan] = (float)ResultantComposite;
                        }
                    }
                }

                NewTable.TruncateBits();

                undo.Push(waveTableObject.WaveTableData);
                redo.Clear();
                waveTableObject.WaveTableData = NewTable;
            }
        }

        private void ComboBoxNumBits_TextChanged(object sender, EventArgs e)
        {
            if (suppressTableOrFrameChange)
            {
                return;
            }

            NumBitsType numBits;
            foreach (Enum candidate in WaveTableStorageRec.NumBitsAllowedValues)
            {
                if (String.Equals(comboBoxNumBits.Text, EnumUtility.GetDescription(candidate)))
                {
                    numBits = (NumBitsType)candidate;
                    goto Found;
                }
            }
            Debug.Assert(false);
            throw new ArgumentException();
        Found:

            // copy table with bit conversion
            WaveTableStorageRec newTable = new WaveTableStorageRec(
                waveTableObject.NumTables,
                waveTableObject.NumFrames,
                numBits);
            for (int i = 0; i < waveTableObject.NumTables; i++)
            {
                for (int j = 0; j < waveTableObject.NumFrames; j++)
                {
                    newTable.ListOfTables[i][j] = waveTableObject.WaveTableData.ListOfTables[i][j];
                }
            }
            newTable.TruncateBits();

            undo.Push(waveTableObject.WaveTableData);
            redo.Clear();
            waveTableObject.WaveTableData = newTable;
        }

        private void Eval()
        {
            if (!mainWindow.MakeUpToDate())
            {
                return;
            }

            int ErrorLineNumberCompilation;
            DataTypes ReturnType;
            PcodeRec FuncCode;
            Compiler.ASTExpression AST;
            CompileErrors CompileError = Compiler.CompileSpecialFunction(
                mainWindow.Document.CodeCenter,
                new FunctionParamRec[]
                {
                    new FunctionParamRec("frames", DataTypes.eInteger),
                    new FunctionParamRec("tables", DataTypes.eInteger),
                    new FunctionParamRec("data", DataTypes.eArrayOfFloat),
                },
                out ErrorLineNumberCompilation,
                out ReturnType,
                textBoxFormula.Text,
                false/*suppressCILEmission*/,
                out FuncCode,
                out AST);
            if (CompileError != CompileErrors.eCompileNoError)
            {
                textBoxFormula.Focus();
                textBoxFormula.SetSelectionLine(ErrorLineNumberCompilation - 1);
                textBoxFormula.ScrollToSelection();
                LiteralBuildErrorInfo errorInfo = new LiteralBuildErrorInfo(Compiler.GetCompileErrorString(CompileError), ErrorLineNumberCompilation);
                MessageBox.Show(errorInfo.CompositeErrorMessage, "Error", MessageBoxButtons.OK);
                return;
            }

            using (ParamStackRec ParamList = new ParamStackRec())
            {
                int numFrames = waveTableObject.WaveTableData.NumFrames;
                int numTables = waveTableObject.WaveTableData.NumTables;
                float[] vector = new float[numFrames * numTables];

                ArrayHandleFloat dataHandle = new ArrayHandleFloat(vector);

                int initialCapacity = 1/*frames*/ + 1/*tables*/ + 1/*data*/ + 1/*retaddr*/;
                ParamList.EmptyParamStackEnsureCapacity(initialCapacity);

                ParamList.AddIntegerToStack(numFrames);
                ParamList.AddIntegerToStack(numTables);
                ParamList.AddArrayToStack(dataHandle);
                ParamList.AddIntegerToStack(0); /* return address placeholder */

                for (int i = 0; i < numTables; i++)
                {
                    WaveTableStorageRec.Table table = waveTableObject.WaveTableData.ListOfTables[i];
                    for (int j = 0; j < numFrames; j++)
                    {
                        vector[i * numFrames + j] = table[j];
                    }
                }

                CodeCenterRec CodeCenter = mainWindow.Document.CodeCenter;
                EvalErrInfoRec ErrorInfo;
                EvalErrors EvaluationError = PcodeSystem.EvaluatePcodeThread.EvaluatePcode(
                    ParamList,
                    FuncCode,
                    CodeCenter,
                    out ErrorInfo,
                    new PcodeExterns(mainWindow));
                if (EvaluationError != EvalErrors.eEvalNoError)
                {
                    PcodeEvaluationErrorInfo errorInfo = new PcodeEvaluationErrorInfo(
                        EvaluationError,
                        ErrorInfo,
                        FuncCode,
                        CodeCenter);
                    MessageBox.Show(errorInfo.CompositeErrorMessage, "Error", MessageBoxButtons.OK);
                    return;
                }
                Debug.Assert(ParamList.GetStackNumElements() == initialCapacity); // args - retaddr + return value
#if DEBUG
                ParamList.Elements[2].AssertFloatArray();
#endif
                dataHandle = ParamList.Elements[2].reference.arrayHandleFloat;

                WaveTableStorageRec NewTable = new WaveTableStorageRec(numTables, numFrames, waveTableObject.WaveTableData.NumBits);
                float[] NewData = dataHandle.floats;
                if (NewData.Length != numTables * numFrames)
                {
                    PcodeEvaluationErrorInfo errorInfo = new PcodeEvaluationErrorInfo(
                        "<anonymous>",
                        PcodeSystem.GetPcodeErrorMessage(EvalErrors.eEvalArrayWrongDimensions),
                        1);
                    MessageBox.Show(errorInfo.CompositeErrorMessage, "Error", MessageBoxButtons.OK);
                    return;
                }
                SampConv.QuantizeAndClampVector(NewData, NewTable.NumBits);
                for (int i = 0; i < numTables; i++)
                {
                    WaveTableStorageRec.Table table = NewTable.ListOfTables[i];
                    for (int j = 0; j < numFrames; j++)
                    {
                        table[j] = NewData[i * numFrames + j];
                    }
                }

                undo.Push(waveTableObject.WaveTableData);
                redo.Clear();
                waveTableObject.WaveTableData = NewTable;
            }
        }

        private void Undo()
        {
            suppressTableOrFrameChange = true;
            try
            {
                WaveTableStorageRec w = undo.Pop();
                redo.Push(waveTableObject.WaveTableData);
                waveTableObject.WaveTableData = w;
            }
            finally
            {
                suppressTableOrFrameChange = false;
            }
        }

        private void Redo()
        {
            suppressTableOrFrameChange = true;
            try
            {
                WaveTableStorageRec w = redo.Pop();
                undo.Push(waveTableObject.WaveTableData);
                waveTableObject.WaveTableData = w;
            }
            finally
            {
                suppressTableOrFrameChange = false;
            }
        }


        // Data grid stuff

        private void RebuildDataGrid()
        {
            dataGridViewWave.Columns.Clear();
            dataGridViewWave.Columns.Add("i", "Index");
            dataGridViewWave.Columns[0].ReadOnly = true;
            for (int j = 0; j < waveTableObject.WaveTableData.NumTables; j++)
            {
                dataGridViewWave.Columns.Add(j.ToString(), j.ToString());
            }
            dataGridViewWave.RowCount = waveTableObject.WaveTableData.NumFrames;
        }

        private void WaveTableObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (String.Equals(e.PropertyName, WaveTableObjectRec.WaveTableData_PropertyName))
            {
                RebuildDataGrid();
            }
        }

        private void DataGridViewWave_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.ColumnIndex == 0)
            {
                e.Value = e.RowIndex;
            }
            else
            {
                Debug.Assert(e.ColumnIndex - 1 < waveTableObject.WaveTableData.NumTables);
                e.Value = GridScale(waveTableObject.WaveTableData.ListOfTables[e.ColumnIndex - 1][e.RowIndex]);
            }
        }

        private void DataGridViewWave_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
        {
            Debug.Assert(e.ColumnIndex > 0);
            Debug.Assert(e.ColumnIndex - 1 < waveTableObject.WaveTableData.NumTables);
            float v;
            if (Single.TryParse(e.Value.ToString(), out v))
            {
                undo.Push(waveTableObject.WaveTableData);
                redo.Clear();
                WaveTableStorageRec storage = new WaveTableStorageRec(waveTableObject.WaveTableData);
                storage.ListOfTables[e.ColumnIndex - 1][e.RowIndex] = GridUnscale(v);
                waveTableObject.WaveTableData = storage;
            }
        }

        private float GridScale(float f)
        {
            switch (comboBoxScale.SelectedIndex)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    return f;
                case 1:
                    return f * SampConv.FLOATFACTOR8BIT;
                case 2:
                    return f * SampConv.FLOATFACTOR16BIT;
                case 3:
                    return f * SampConv.FLOATFACTOR24BIT;
            }
        }

        private float GridUnscale(float f)
        {
            switch (comboBoxScale.SelectedIndex)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    return f;
                case 1:
                    return f * (1f / SampConv.FLOATFACTOR8BIT);
                case 2:
                    return f * (1f / SampConv.FLOATFACTOR16BIT);
                case 3:
                    return f * (1f / SampConv.FLOATFACTOR24BIT);
            }
        }

        private void comboBoxScale_SelectedIndexChanged(object sender, EventArgs e)
        {
            RebuildDataGrid();
        }

        private void TabControlWave_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool grid = tabControlWave.Controls[tabControlWave.SelectedIndex] == tabPageWaveGrid;
            labelScale.Visible = grid;
            comboBoxScale.Visible = grid;
        }


        // MenuStripManager methods

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            menuStripManager.ProcessCmdKeyDelegate(ref msg, keyData);

            if (textEditorWindowHelper.ProcessCmdKeyDelegate(ref msg, keyData))
            {
                return true;
            }
            if (textBoxWindowHelper.ProcessCmdKeyDelegate(ref msg, keyData))
            {
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        void IMenuStripManagerHandler.EnableMenuItems(MenuStripManager menuStrip)
        {
            textEditorWindowHelper.MenuActivateDelegate();
            textBoxWindowHelper.MenuActivateDelegate();

            menuStrip.evaluateToolStripMenuItem.Enabled = true;
            menuStrip.undoToolStripMenuItem.Enabled = undo.Count != 0;
            menuStrip.redoToolStripMenuItem.Enabled = redo.Count != 0;

            menuStrip.insertTableToolStripMenuItem.Visible = true;
            menuStrip.insertTableToolStripMenuItem.Enabled = true;
            menuStrip.deleteTableToolStripMenuItem.Visible = true;
            menuStrip.deleteTableToolStripMenuItem.Enabled = waveTableObject.NumTables != 0;

            menuStrip.deleteObjectToolStripMenuItem.Enabled = true;
            menuStrip.deleteObjectToolStripMenuItem.Text = "Delete Wave Table";
        }

        bool IMenuStripManagerHandler.ExecuteMenuItem(MenuStripManager menuStrip, ToolStripMenuItem menuItem)
        {
            if (menuItem == menuStrip.evaluateToolStripMenuItem)
            {
                Eval();
                return true;
            }
            else if (menuItem == menuStrip.undoToolStripMenuItem)
            {
                Undo();
                return true;
            }
            else if (menuItem == menuStrip.redoToolStripMenuItem)
            {
                Redo();
                return true;
            }
            else if (menuItem == menuStrip.insertTableToolStripMenuItem)
            {
                using (CmdDlgOneParam dialog = new CmdDlgOneParam("Insert new wave table at which index?", "Index:", hScrollBarWaveTable.Value.ToString(), CmdDlgOneParam.Options.None))
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        int index;
                        if (Int32.TryParse(dialog.Value, out index) && unchecked((uint)index) <= (uint)waveTableObject.NumTables)
                        {
                            undo.Push(waveTableObject.WaveTableData);
                            redo.Clear();
                            waveTableObject.WaveTableData = WaveTableStorageRec.InsertTable(waveTableObject.WaveTableData, index);
                        }
                    }
                }
                return true;
            }
            else if (menuItem == menuStrip.deleteTableToolStripMenuItem)
            {
                using (CmdDlgOneParam dialog = new CmdDlgOneParam("Delete wave table at which index?", "Index:", hScrollBarWaveTable.Value.ToString(), CmdDlgOneParam.Options.None))
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        int index;
                        if (Int32.TryParse(dialog.Value, out index) && unchecked((uint)index) < (uint)waveTableObject.NumTables)
                        {
                            undo.Push(waveTableObject.WaveTableData);
                            redo.Clear();
                            waveTableObject.WaveTableData = WaveTableStorageRec.DeleteTable(waveTableObject.WaveTableData, index);
                        }
                    }
                }
                return true;
            }
            else if (menuItem == menuStrip.deleteObjectToolStripMenuItem)
            {
                Close();
                mainWindow.DeleteObject(waveTableObject, mainWindow.Document.WaveTableList);
                return true;
            }
            return false;
        }


        //

        private void textBoxName_TextChanged(object sender, EventArgs e)
        {
            GlobalNameChanged();
        }

        public void GlobalNameChanged()
        {
            this.Text = String.Format("{0} - {1}", mainWindow.DisplayName, this.textBoxName.Text);
        }


        // 

        private OutputGeneric<OutputDeviceDestination, WaveTableTestGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>, OutputDeviceArguments> state;
        private WaveTableTestGeneratorParams<OutputDeviceDestination, OutputDeviceArguments> generatorParams;

        private void buttonTest_MouseDown(object sender, MouseEventArgs e)
        {
            const double BufferDuration = .25f;

            IPlayPrefsProvider playPrefsProvider = mainWindow.GetPlayPrefsProvider();

#if true // prevents "Add New Data Source..." from working
            state = WaveTableTestGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.Do(
                mainWindow.DisplayName,
                OutputDeviceEnumerator.OutputDeviceGetDestination,
                OutputDeviceEnumerator.CreateOutputDeviceDestinationHandler,
                new OutputDeviceArguments(BufferDuration),
                WaveTableTestGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.MainLoop,
                generatorParams = new WaveTableTestGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>(
                    waveTableObject.WaveTableData,
                    waveTableObject.TestAttackDuration,
                    waveTableObject.TestDecayDuration,
                    waveTableObject.NumBits,
                    playPrefsProvider.SamplingRate,
                    waveTableObject.TestFrequency),
                WaveTableTestGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.Completion,
                mainWindow,
                NumChannelsType.eSampleMono,
                waveTableObject.NumBits,
                playPrefsProvider.SamplingRate,
                1/*oversampling*/,
                false/*showProgressWindow*/,
                false/*modal*/);
#endif
        }

        private void buttonTest_MouseUp(object sender, MouseEventArgs e)
        {
            state.stopper.Stop();

            if (generatorParams.exception != null)
            {
                MessageBox.Show(generatorParams.exception.ToString());
            }

            state.Dispose();
            state = null;
            generatorParams = null;
        }
    }

#if true // prevents "Add New Data Source..." from working
    public class WaveTableTestGeneratorParams<T, W>
    {
        public WaveTableStorageRec data;
        public double attack;
        public double decay;
        public NumBitsType numBits;
        public int samplingRate;
        public double frequency;

        public Exception exception;

        public WaveTableTestGeneratorParams(
            WaveTableStorageRec data,
            double attack,
            double decay,
            NumBitsType numBits,
            int samplingRate,
            double frequency)
        {
            this.data = data;
            this.attack = attack;
            this.decay = decay;
            this.numBits = numBits;
            this.samplingRate = samplingRate;
            this.frequency = frequency;
        }

        public static OutputGeneric<T, WaveTableTestGeneratorParams<T, W>, W> Do(
            string baseName,
            GetDestinationMethod<T> getDestination,
            CreateDestinationHandlerMethod<T, W> createDestinationHandler,
            W destinationHandlerArguments,
            GeneratorMainLoopMethod<T, WaveTableTestGeneratorParams<T, W>, W> generatorMainLoop,
            WaveTableTestGeneratorParams<T, W> generatorParams,
            GeneratorCompletionMethod<WaveTableTestGeneratorParams<T, W>> generatorCompletion,
            IMainWindowServices mainWindow,
            NumChannelsType channels,
            NumBitsType bits,
            int samplingRate,
            int oversamplingFactor,
            bool showProgressWindow,
            bool modal)
        {
            // prerequisites 

            return OutputGeneric<T, WaveTableTestGeneratorParams<T, W>, W>.Do(
                baseName,
                getDestination,
                createDestinationHandler,
                destinationHandlerArguments,
                generatorMainLoop,
                generatorParams,
                generatorCompletion,
                channels,
                bits,
                samplingRate,
                oversamplingFactor,
                showProgressWindow,
                modal);
        }

        public static void MainLoop<U>(
            WaveTableTestGeneratorParams<T, W> generatorParams,
            Synthesizer.DataOutCallbackMethod<OutputGeneric<T, U, W>> dataCallback,
            OutputGeneric<T, U, W> dataCallbackState,
            Synthesizer.StopTask stopper)
        {
            try
            {
                // Unoptimized - since only one runs at a time this codepath is not perf critical (unlike the
                // resampler loops in the synth engine)

                int NumTables = generatorParams.data.NumTables;
                float[][] ReferenceArray = new float[NumTables][];
                for (int i = 0; i < NumTables; i++)
                {
                    ReferenceArray[i] = generatorParams.data.ListOfTables[i].frames;
                }

                int PlaybackSamplingRate = generatorParams.samplingRate;
                if (PlaybackSamplingRate < Constants.MINSAMPLINGRATE)
                {
                    PlaybackSamplingRate = Constants.MINSAMPLINGRATE;
                }
                if (PlaybackSamplingRate > Constants.MAXSAMPLINGRATE)
                {
                    PlaybackSamplingRate = Constants.MAXSAMPLINGRATE;
                }

                int FramesPerTable = generatorParams.data.NumFrames;

                /* this is the initial index into the wave table */
                Synthesizer.Fixed64 WaveformIndex = new Synthesizer.Fixed64();
                /* this is the 16.16 bit fixed point number used to increment the index */
                /* into the wave table */
                Synthesizer.Fixed64 WaveformIncrementor = new Synthesizer.Fixed64(
                    FramesPerTable * generatorParams.frequency / PlaybackSamplingRate);

                /* the number of times each wave slice has to be used */
                int NumberOfIterationsAttack = (int)(generatorParams.attack * PlaybackSamplingRate);
                int NumberOfIterationsDecay = (int)(generatorParams.decay * PlaybackSamplingRate);

                const int SOUNDBUFFERLENGTHFRAMES = 256;
                float[] OutputBuffer = new float[SOUNDBUFFERLENGTHFRAMES * 2];
                int OutputIndex = 0;

                for (int i = 0; i < NumberOfIterationsAttack + NumberOfIterationsDecay; i++)
                {
                    /* compute wave table index for attack/decay phase */
                    double TableIndex;
                    if (i < NumberOfIterationsAttack)
                    {
                        TableIndex = (double)i / NumberOfIterationsAttack;
                    }
                    else
                    {
                        TableIndex = (double)(NumberOfIterationsDecay + NumberOfIterationsAttack - i) / NumberOfIterationsDecay;
                    }

                    float Value = Synthesizer.WaveTableIndexer(
                        WaveformIndex.Double,
                        TableIndex * (NumTables - 1),
                        NumTables,
                        FramesPerTable,
                        ReferenceArray,
                        true/*EnableCrossWaveTableInterpolation*/);
                    WaveformIndex += WaveformIncrementor;

                    OutputBuffer[2 * OutputIndex + 0] = OutputBuffer[2 * OutputIndex + 1] = Value * .5f;
                    OutputIndex++;
                    if (OutputIndex == SOUNDBUFFERLENGTHFRAMES)
                    {
                        dataCallback(
                            dataCallbackState,
                            OutputBuffer,
                            0,
                            SOUNDBUFFERLENGTHFRAMES);
                        OutputIndex = 0;

                        if (stopper.Stopped)
                        {
                            return;
                        }
                    }
                }
                dataCallback(
                    dataCallbackState,
                    OutputBuffer,
                    0,
                    OutputIndex);
            }
            catch (Exception exception)
            {
                generatorParams.exception = exception;
                stopper.Stop();
            }
        }

        public static void Completion(
            WaveTableTestGeneratorParams<T, W> generatorParams,
            ref ClipInfo clipInfo)
        {
        }
    }
#endif
}
