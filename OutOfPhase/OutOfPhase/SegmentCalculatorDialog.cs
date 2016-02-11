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
using System.Windows.Forms;

namespace OutOfPhase
{
    // TODO: enable this so that envelope segment can be selected and parameters will be parsed out and filled into
    // the text edits automatically.

    // TODO: add startOffset and endOffset that can be edited in terms of duration rather than fraction

    public partial class SegmentCalculatorDialog : Form
    {
        private static double savedInitial;
        private static double savedDelay;
        private static double savedFinal;
        private static bool savedExponential;
        private static double savedNewStart = 0;
        private static double savedNewEnd = 1;

        private readonly InstrumentWindow owner;

        private string statement;

        public SegmentCalculatorDialog(
            InstrumentWindow owner)
            : this(owner, savedInitial, savedDelay, savedFinal, savedExponential)
        {
        }

        public SegmentCalculatorDialog(
            InstrumentWindow owner,
            double initial,
            double delay,
            double final,
            bool exponential)
        {
            this.owner = owner;

            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            DpiChangeHelper.ScaleFont(this, Program.Config.AdditionalUIZoom);

            this.textBoxInitial.Text = initial.ToString();
            this.textBoxDuration.Text = delay.ToString();
            this.textBoxFinal.Text = final.ToString();
            this.textBoxStartOffset.Text = savedNewStart.ToString();
            this.textBoxEndOffset.Text = savedNewEnd.ToString();

            this.comboBoxFunction.Items.Add("Linear");
            this.comboBoxFunction.Items.Add("Exponential");
            this.comboBoxFunction.SelectedIndex = exponential ? 1 : 0;

            this.textBoxInitial.TextChanged += new System.EventHandler(this.textBoxInitial_TextChanged);
            this.textBoxDuration.TextChanged += new System.EventHandler(this.textBoxDuration_TextChanged);
            this.textBoxFinal.TextChanged += new System.EventHandler(this.textBoxFinal_TextChanged);
            this.textBoxStartOffset.TextChanged += new System.EventHandler(this.textBoxStartOffset_TextChanged);
            this.textBoxEndOffset.TextChanged += new System.EventHandler(this.textBoxEndOffset_TextChanged);
            this.textBoxNewFinal.TextChanged += new System.EventHandler(this.textBoxNewFinal_TextChanged);
            this.textBoxNewInitial.TextChanged += new System.EventHandler(this.textBoxNewInitial_TextChanged);
            this.comboBoxFunction.SelectedIndexChanged += new System.EventHandler(this.comboBoxFunction_SelectedIndexChanged);

            Recalculate(true/*updateInitialFinal*/);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            owner.SegmentCalculatorClosed(this);

            base.OnFormClosed(e);
        }

        private int recalcReentranceGuard;
        private void Recalculate(bool updateInitialFinal)
        {
            recalcReentranceGuard++;
            try
            {
                if (recalcReentranceGuard == 1)
                {
                    double duration;
                    Double.TryParse(textBoxDuration.Text, out duration);

                    double initial, final;
                    Double.TryParse(textBoxInitial.Text, out initial);
                    Double.TryParse(textBoxFinal.Text, out final);

                    double newStart, newEnd;
                    Double.TryParse(textBoxStartOffset.Text, out newStart);
                    Double.TryParse(textBoxEndOffset.Text, out newEnd);

                    bool exponential = comboBoxFunction.SelectedIndex != 0;

                    double newInitial, newFinal;
                    if (!exponential)
                    {
                        // linear
                        newInitial = initial + newStart * (final - initial);
                        newFinal = initial + newEnd * (final - initial);
                    }
                    else
                    {
                        // exponential

                        double initialDecibels = Synthesizer.ExpSegEndpointToLog(initial);
                        double finalDecibels = Synthesizer.ExpSegEndpointToLog(final);

                        newInitial = initialDecibels + newStart * (finalDecibels - initialDecibels);
                        newFinal = initialDecibels + newEnd * (finalDecibels - initialDecibels);

                        newInitial = Synthesizer.ExpSegEndpointToLinear(newInitial);
                        newFinal = Synthesizer.ExpSegEndpointToLinear(newFinal);
                    }

                    newInitial = Math.Round(newInitial, 8);
                    newFinal = Math.Round(newFinal, 8);

                    if (updateInitialFinal)
                    {
                        textBoxNewInitial.Text = newInitial.ToString();
                        textBoxNewFinal.Text = newFinal.ToString();
                    }
                    statement = String.Format(
                        "delay {0} level {1}{2};",
                        duration * (newEnd - newStart),
                        newFinal,
                        exponential ? " exponential" : String.Empty);
                    textBoxStatement.Text = String.Format(
                        "[{0}]: {1}",
                        newInitial,
                        statement);

                    segmentCalculatorGraph.Update(initial, final, duration, newStart, newEnd, exponential);

                    savedInitial = initial;
                    savedDelay = duration;
                    savedFinal = final;
                    savedExponential = exponential;
                    savedNewStart = newStart;
                    savedNewEnd = newEnd;
                }
            }
            finally
            {
                recalcReentranceGuard--;
            }
        }


        //

        private void textBoxInitial_TextChanged(object sender, EventArgs e)
        {
            Recalculate(true/*updateInitialFinal*/);
        }

        private void textBoxDuration_TextChanged(object sender, EventArgs e)
        {
            Recalculate(true/*updateInitialFinal*/);
        }

        private void textBoxFinal_TextChanged(object sender, EventArgs e)
        {
            Recalculate(true/*updateInitialFinal*/);
        }

        private void textBoxStartOffset_TextChanged(object sender, EventArgs e)
        {
            Recalculate(true/*updateInitialFinal*/);
        }

        private void textBoxEndOffset_TextChanged(object sender, EventArgs e)
        {
            Recalculate(true/*updateInitialFinal*/);
        }

        private void comboBoxFunction_SelectedIndexChanged(object sender, EventArgs e)
        {
            Recalculate(true/*updateInitialFinal*/);
        }

        private void textBoxNewInitial_TextChanged(object sender, EventArgs e)
        {
            recalcReentranceGuard++;
            try
            {
                if (recalcReentranceGuard == 1)
                {
                    // reverse-engineer other parameters from this desired value

                    double duration;
                    Double.TryParse(textBoxDuration.Text, out duration);

                    double initial, final;
                    Double.TryParse(textBoxInitial.Text, out initial);
                    Double.TryParse(textBoxFinal.Text, out final);

                    double newStart;
                    Double.TryParse(textBoxStartOffset.Text, out newStart);

                    double newInitial;
                    Double.TryParse(textBoxNewInitial.Text, out newInitial);

                    bool exponential = comboBoxFunction.SelectedIndex != 0;

                    if (!exponential)
                    {
                        // linear
                        newStart = (newInitial - initial) / (final - initial);
                    }
                    else
                    {
                        // exponential

                        double initialDecibels = Synthesizer.ExpSegEndpointToLog(initial);
                        double finalDecibels = Synthesizer.ExpSegEndpointToLog(final);
                        double newInitialDecibels = Synthesizer.ExpSegEndpointToLog(newInitial);

                        newStart = (newInitialDecibels - initialDecibels) / (finalDecibels - initialDecibels);
                    }

                    textBoxStartOffset.Text = newStart.ToString();
                }
            }
            finally
            {
                recalcReentranceGuard--;
            }

            Recalculate(false/*updateInitialFinal*/);
        }

        private void textBoxNewFinal_TextChanged(object sender, EventArgs e)
        {
            recalcReentranceGuard++;
            try
            {
                if (recalcReentranceGuard == 1)
                {
                    // reverse-engineer other parameters from this desired value

                    double duration;
                    Double.TryParse(textBoxDuration.Text, out duration);

                    double initial, final;
                    Double.TryParse(textBoxInitial.Text, out initial);
                    Double.TryParse(textBoxFinal.Text, out final);

                    double newEnd;
                    Double.TryParse(textBoxEndOffset.Text, out newEnd);

                    double newFinal;
                    Double.TryParse(textBoxNewFinal.Text, out newFinal);

                    bool exponential = comboBoxFunction.SelectedIndex != 0;

                    if (!exponential)
                    {
                        // linear
                        newEnd = (newFinal - initial) / (final - initial);
                    }
                    else
                    {
                        // exponential

                        double initialDecibels = Synthesizer.ExpSegEndpointToLog(initial);
                        double finalDecibels = Synthesizer.ExpSegEndpointToLog(final);
                        double newFinalDecibels = Synthesizer.ExpSegEndpointToLog(newFinal);

                        newEnd = (newFinalDecibels - initialDecibels) / (finalDecibels - initialDecibels);
                    }

                    textBoxEndOffset.Text = newEnd.ToString();
                }
            }
            finally
            {
                recalcReentranceGuard--;
            }

            Recalculate(false/*updateInitialFinal*/);
        }


        //

        private void buttonReset_Click(object sender, EventArgs e)
        {
            segmentCalculatorGraph.Reset();
        }

        private void buttonCopy_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(statement);
        }
    }
}
