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
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class OutputProgressWindow : Form
    {
        private readonly IProgressInfo progressInfo;
        private readonly IBufferLoading bufferLoading;
        private readonly Synthesizer.IStopTask stopCallback; // us telling them to stop
        private readonly IFinished finished; // them telling us they stopped
        private readonly IWaitFinished waitFinished; // us waiting for them to complete the stopping process
        private readonly ShowCompletionMethod showCompletion;

        private bool layoutSuspendable;
        private bool layoutSuspended;

        private static int lastX = -1, lastY = -1;

        public OutputProgressWindow(
            string baseName,
            bool showClipping,
            IProgressInfo progressInfo,
            IBufferLoading bufferLoading,
            Synthesizer.IStopTask stopCallback,
            IFinished finished,
            IWaitFinished waitFinished,
            ShowCompletionMethod showError)
        {
            this.stopCallback = stopCallback;
            this.bufferLoading = bufferLoading;
            this.progressInfo = progressInfo;
            this.finished = finished;
            this.waitFinished = waitFinished;
            this.showCompletion = showError;

            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            DpiChangeHelper.ScaleFont(this, Program.Config.AdditionalUIZoom);

            if (!showClipping)
            {
                labelTotalClippedPoints.Visible = false;
                textTotalClippedPoints.Visible = false;
            }

            UpdateValues();
            this.Text = String.Format("{0} - {1}", baseName, "Synthesis");
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == DpiChangeHelper.WM_DPICHANGED)
            {
                SafeResumeLayout(); // one of the rare cases in which the layout might actually change
                dpiChangeHelper.WndProcDelegate(ref m);
            }

            base.WndProc(ref m);
        }

        protected override void OnShown(EventArgs e)
        {
            if ((lastX >= 0) && (lastY >= 0))
            {
                SetDesktopLocation(lastX, lastY);
            }
            base.OnShown(e);
        }

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            if (Visible)
            {
                lastX = DesktopLocation.X;
                lastY = DesktopLocation.Y;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            stopCallback.Stop();

            base.OnFormClosing(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Defeat Alt-F4 to close window -- forcing user to use the "stop" button -- beause it's too easy to accidentally
            // terminate playback and the cost of terminating playback is high.
            // Escape is still permitted. Rationale: Alt-F4 is more likely to be used for cleaning up open document windows
            // and more likely to be unintentional to a modal dialog. Escape is more often used for modal dialogs and less likely
            // to be accidental.
            if (keyData == (Keys.F4 | Keys.Alt))
            {
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void SafeSuspendLayout()
        {
            if (layoutSuspendable && !layoutSuspended)
            {
                layoutSuspended = true;
                //SuspendLayout();
            }
        }

        private void SafeResumeLayout()
        {
            if (layoutSuspendable && layoutSuspended)
            {
                layoutSuspended = false;
                //ResumeLayout();
            }
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            if (!layoutSuspended)
            {
                base.OnLayout(levent);
            }
        }

        private void UpdateValues()
        {
            SuspendLayout();

            Decimal elapsedSeconds = Decimal.Round((Decimal)progressInfo.ElapsedAudioSeconds, 3);
            textElapsedAudioSeconds.Text = String.Format(
                elapsedSeconds <= 60 ? "{0:0.000}" : elapsedSeconds <= 60 * 60 ? "{0:0.000}  ({2:00}:{3:00.000})" : "{0:0.000}  ({1}:{2:00}:{3:00.000})",
                elapsedSeconds,
                Decimal.Floor(elapsedSeconds / (60 * 60)),
                Decimal.Floor(elapsedSeconds / 60),
                elapsedSeconds % 60);
            textTotalFrames.Text = progressInfo.TotalFrames.ToString();
            textTotalClippedPoints.Text = progressInfo.TotalClippedPoints.ToString();

            if ((bufferLoading == null) || !bufferLoading.Available)
            {
                labelBufferLoading.Visible = false;
                myProgressBarBufferLoading.Visible = false;
                labelBufferSeconds.Visible = false;
            }
            else
            {
                labelBufferLoading.Visible = true;
                myProgressBarBufferLoading.Visible = true;
                labelBufferSeconds.Visible = true;

                myProgressBarBufferLoading.Maximum = bufferLoading.Maximum;
                myProgressBarBufferLoading.CritThreshhold = bufferLoading.Critical;
                myProgressBarBufferLoading.Level = bufferLoading.Level;
                labelBufferSeconds.Text = String.Format("{0:0.0} sec", bufferLoading.Maximum);
            }

            ResumeLayout();
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            // TODO: prompt "are you sure"? for file playback (but not audio)

            stopCallback.Stop();

            waitFinished.WaitFinished();

            UpdateValues();

            ShowErrorAndClose();
        }

        private void timerUpdate_Tick(object sender, EventArgs e)
        {
            layoutSuspendable = true; // when idle happens, it's safe to suspend layout
            // Try to suspend layout as often as possible since this window generally doesn't need it and it allocates a lot of memory.
            SafeSuspendLayout();

            UpdateValues();
            if (finished.Finished)
            {
                ShowErrorAndClose();
            }
        }

        private void ShowErrorAndClose()
        {
            timerUpdate.Stop();

            showCompletion();

            Close();
        }
    }

    public delegate void ShowCompletionMethod();

    public interface IProgressInfo
    {
        double ElapsedAudioSeconds { get; }
        int TotalFrames { get; }
        int TotalClippedPoints { get; }
    }

    public interface IBufferLoading
    {
        bool Available { get; }
        float Level { get; } // seconds
        float Maximum { get; } // seconds
        float Critical { get; } // seconds
    }

    public interface IWaitFinished
    {
        void WaitFinished();
    }

    public interface INotifyFinished
    {
        void NotifyFinished();
    }

    public interface IFinished
    {
        bool Finished { get; }
    }

    public class WaitFinishedHelper : IWaitFinished, INotifyFinished, IFinished, IDisposable
    {
        private bool exited;
        private EventWaitHandle waitThreadExited = new EventWaitHandle(false/*initialState*/, EventResetMode.ManualReset);

        public void WaitFinished()
        {
            while (!waitThreadExited.WaitOne(1000))
            {
            }
        }

        public void NotifyFinished()
        {
            waitThreadExited.Set();
            exited = true;
        }

        public bool Finished { get { return exited; } }

        public void Dispose()
        {
            if (waitThreadExited != null)
            {
                waitThreadExited.Close();
                waitThreadExited = null;
            }
        }
    }
}
