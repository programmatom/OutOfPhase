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

            if (!showClipping)
            {
                labelTotalClippedPoints.Visible = false;
                textTotalClippedPoints.Visible = false;
            }

            UpdateValues();
            this.Text = String.Format("{0} - {1}", baseName, "Synthesis");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            stopCallback.Stop();

            base.OnFormClosing(e);
        }

        private void UpdateValues()
        {
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
            waitThreadExited.WaitOne();
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
