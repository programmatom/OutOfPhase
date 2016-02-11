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
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class CommandChooser : Form
    {
        private bool accepted;
        private NoteCommands acceptedCommand;

        private static int lastSelectIndex = -1;

        public CommandChooser()
        {
            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            DpiChangeHelper.ScaleFont(this, Program.Config.AdditionalUIZoom);

            listBoxCommands.ValueMember = "Key";
            listBoxCommands.DisplayMember = "Value";
            foreach (NoteCommands command in CommandList)
            {
                if (command != NoteCommands.eCmd_End)
                {
                    KeyValuePair<NoteCommands, string> entry = new KeyValuePair<NoteCommands, string>(command, CommandMapping.GetCommandName(command));
                    listBoxCommands.Items.Add(entry);
                }
                else
                {
                    listBoxCommands.Items.Add(new KeyValuePair<NoteCommands, string>(NoteCommands.eCmd_End, String.Empty));
                }
            }
            if (lastSelectIndex >= 0)
            {
                listBoxCommands.SelectedIndex = lastSelectIndex;
            }
            listBoxCommands.DoubleClick += buttonOK_Click;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            lastSelectIndex = listBoxCommands.SelectedIndex;
            base.OnFormClosed(e);
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            if (listBoxCommands.SelectedIndex >= 0)
            {
                KeyValuePair<NoteCommands, string> entry = (KeyValuePair<NoteCommands, string>)listBoxCommands.SelectedItem;
                acceptedCommand = entry.Key;
                if (acceptedCommand != NoteCommands.eCmd_End)
                {
                    accepted = true;
                }
            }

            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if ((keyData & Keys.KeyCode) == Keys.Enter)
            {
                buttonOK_Click(this, EventArgs.Empty);
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }

        protected override void WndProc(ref Message m)
        {
            dpiChangeHelper.WndProcDelegate(ref m);
            base.WndProc(ref m);
        }


        //

        public static bool ChooseCommandFromList(out NoteCommands command)
        {
            command = NoteCommands.eCmd_End;
            using (CommandChooser chooser = new CommandChooser())
            {
                DialogResult result = chooser.ShowDialog();
                command = chooser.acceptedCommand;
                return chooser.accepted;
            }
        }

        private static readonly NoteCommands[] CommandList = new NoteCommands[]
        {
		    /* tempo adjustments */
		    NoteCommands.eCmdRestoreTempo,
            NoteCommands.eCmdSetTempo,
            NoteCommands.eCmdIncTempo,
            NoteCommands.eCmdSweepTempoAbs,
            NoteCommands.eCmdSweepTempoRel,

            NoteCommands.eCmd_End,

		    /* stereo positioning adjustments */
		    NoteCommands.eCmdRestoreStereoPosition,
            NoteCommands.eCmdSetStereoPosition,
            NoteCommands.eCmdIncStereoPosition,
            NoteCommands.eCmdSweepStereoAbs,
            NoteCommands.eCmdSweepStereoRel,

            NoteCommands.eCmd_End,

		    /* surround positioning adjustments */
		    NoteCommands.eCmdRestoreSurroundPosition,
            NoteCommands.eCmdSetSurroundPosition,
            NoteCommands.eCmdIncSurroundPosition,
            NoteCommands.eCmdSweepSurroundAbs,
            NoteCommands.eCmdSweepSurroundRel,

            NoteCommands.eCmd_End,

		    /* overall volume adjustments */
		    NoteCommands.eCmdRestoreVolume,
            NoteCommands.eCmdSetVolume,
            NoteCommands.eCmdIncVolume,
            NoteCommands.eCmdSweepVolumeAbs,
            NoteCommands.eCmdSweepVolumeRel,

            NoteCommands.eCmd_End,

		    /* default release point adjustment values */
		    NoteCommands.eCmdRestoreReleasePoint1,
            NoteCommands.eCmdSetReleasePoint1,
            NoteCommands.eCmdIncReleasePoint1,
            NoteCommands.eCmdReleasePointOrigin1,
            NoteCommands.eCmdSweepReleaseAbs1,
            NoteCommands.eCmdSweepReleaseRel1,

            NoteCommands.eCmdRestoreReleasePoint2,
            NoteCommands.eCmdSetReleasePoint2,
            NoteCommands.eCmdIncReleasePoint2,
            NoteCommands.eCmdReleasePointOrigin2,
            NoteCommands.eCmdSweepReleaseAbs2,
            NoteCommands.eCmdSweepReleaseRel2,

            NoteCommands.eCmd_End,

		    /* set the default accent values */
		    NoteCommands.eCmdRestoreAccent1,
            NoteCommands.eCmdSetAccent1,
            NoteCommands.eCmdIncAccent1,
            NoteCommands.eCmdSweepAccentAbs1,
            NoteCommands.eCmdSweepAccentRel1,

            NoteCommands.eCmdRestoreAccent2,
            NoteCommands.eCmdSetAccent2,
            NoteCommands.eCmdIncAccent2,
            NoteCommands.eCmdSweepAccentAbs2,
            NoteCommands.eCmdSweepAccentRel2,

            NoteCommands.eCmdRestoreAccent3,
            NoteCommands.eCmdSetAccent3,
            NoteCommands.eCmdIncAccent3,
            NoteCommands.eCmdSweepAccentAbs3,
            NoteCommands.eCmdSweepAccentRel3,

            NoteCommands.eCmdRestoreAccent4,
            NoteCommands.eCmdSetAccent4,
            NoteCommands.eCmdIncAccent4,
            NoteCommands.eCmdSweepAccentAbs4,
            NoteCommands.eCmdSweepAccentRel4,

            NoteCommands.eCmdRestoreAccent5,
            NoteCommands.eCmdSetAccent5,
            NoteCommands.eCmdIncAccent5,
            NoteCommands.eCmdSweepAccentAbs5,
            NoteCommands.eCmdSweepAccentRel5,

            NoteCommands.eCmdRestoreAccent6,
            NoteCommands.eCmdSetAccent6,
            NoteCommands.eCmdIncAccent6,
            NoteCommands.eCmdSweepAccentAbs6,
            NoteCommands.eCmdSweepAccentRel6,

            NoteCommands.eCmdRestoreAccent7,
            NoteCommands.eCmdSetAccent7,
            NoteCommands.eCmdIncAccent7,
            NoteCommands.eCmdSweepAccentAbs7,
            NoteCommands.eCmdSweepAccentRel7,

            NoteCommands.eCmdRestoreAccent8,
            NoteCommands.eCmdSetAccent8,
            NoteCommands.eCmdIncAccent8,
            NoteCommands.eCmdSweepAccentAbs8,
            NoteCommands.eCmdSweepAccentRel8,

            NoteCommands.eCmd_End,

		    /* set pitch displacement depth adjustment */
		    NoteCommands.eCmdRestorePitchDispDepth,
            NoteCommands.eCmdSetPitchDispDepth,
            NoteCommands.eCmdIncPitchDispDepth,
            NoteCommands.eCmdSweepPitchDispDepthAbs,
            NoteCommands.eCmdSweepPitchDispDepthRel,

		    /* set pitch displacement rate adjustment */
		    NoteCommands.eCmdRestorePitchDispRate,
            NoteCommands.eCmdSetPitchDispRate,
            NoteCommands.eCmdIncPitchDispRate,
            NoteCommands.eCmdSweepPitchDispRateAbs,
            NoteCommands.eCmdSweepPitchDispRateRel,

		    /* set pitch displacement start point, same way as release point */
		    NoteCommands.eCmdRestorePitchDispStart,
            NoteCommands.eCmdSetPitchDispStart,
            NoteCommands.eCmdIncPitchDispStart,
            NoteCommands.eCmdPitchDispStartOrigin,
            NoteCommands.eCmdSweepPitchDispStartAbs,
            NoteCommands.eCmdSweepPitchDispStartRel,

            NoteCommands.eCmd_End,

		    /* hurry up adjustment */
		    NoteCommands.eCmdRestoreHurryUp,
            NoteCommands.eCmdSetHurryUp,
            NoteCommands.eCmdIncHurryUp,
            NoteCommands.eCmdSweepHurryUpAbs,
            NoteCommands.eCmdSweepHurryUpRel,

            NoteCommands.eCmd_End,

		    /* default detune */
		    NoteCommands.eCmdRestoreDetune,
            NoteCommands.eCmdSetDetune,
            NoteCommands.eCmdIncDetune,
            NoteCommands.eCmdDetuneMode,
            NoteCommands.eCmdSweepDetuneAbs,
            NoteCommands.eCmdSweepDetuneRel,

            NoteCommands.eCmd_End,

		    /* default early/late adjust */
		    NoteCommands.eCmdRestoreEarlyLateAdjust,
            NoteCommands.eCmdSetEarlyLateAdjust,
            NoteCommands.eCmdIncEarlyLateAdjust,
            NoteCommands.eCmdSweepEarlyLateAbs,
            NoteCommands.eCmdSweepEarlyLateRel,

		    /* default duration adjust */
		    NoteCommands.eCmdRestoreDurationAdjust,
            NoteCommands.eCmdSetDurationAdjust,
            NoteCommands.eCmdIncDurationAdjust,
            NoteCommands.eCmdSweepDurationAbs,
            NoteCommands.eCmdSweepDurationRel,
            NoteCommands.eCmdDurationAdjustMode,

            NoteCommands.eCmd_End,

		    /* overall portamento adjustments */
		    NoteCommands.eCmdRestorePortamento,
            NoteCommands.eCmdSetPortamento,
            NoteCommands.eCmdIncPortamento,
            NoteCommands.eCmdSweepPortamentoAbs,
            NoteCommands.eCmdSweepPortamentoRel,

            NoteCommands.eCmd_End,

		    /* set the meter.  this is used by the editor for placing measure bars. */
		    /* measuring restarts immediately after this command */
		    NoteCommands.eCmdSetMeter,
		    /* set the measure number. */
		    NoteCommands.eCmdSetMeasureNumber,

            NoteCommands.eCmd_End,

		    /* transpose controls */
		    NoteCommands.eCmdSetTranspose,
            NoteCommands.eCmdAdjustTranspose,

            NoteCommands.eCmd_End,

		    /* pitch stuff */
		    NoteCommands.eCmdSetFrequencyValue,
            NoteCommands.eCmdSetFrequencyValueLegacy,
            NoteCommands.eCmdAdjustFrequencyValue,
            NoteCommands.eCmdAdjustFrequencyValueLegacy,
            NoteCommands.eCmdResetFrequencyValue,
            NoteCommands.eCmdLoadFrequencyModel,
            NoteCommands.eCmdSweepFrequencyValue0Absolute,
            NoteCommands.eCmdSweepFrequencyValue0Relative,
            NoteCommands.eCmdSweepFrequencyValue1Absolute,
            NoteCommands.eCmdSweepFrequencyValue1Relative,
            NoteCommands.eCmdSweepFrequencyValue2Absolute,
            NoteCommands.eCmdSweepFrequencyValue2Relative,
            NoteCommands.eCmdSweepFrequencyValue3Absolute,
            NoteCommands.eCmdSweepFrequencyValue3Relative,
            NoteCommands.eCmdSweepFrequencyValue4Absolute,
            NoteCommands.eCmdSweepFrequencyValue4Relative,
            NoteCommands.eCmdSweepFrequencyValue5Absolute,
            NoteCommands.eCmdSweepFrequencyValue5Relative,
            NoteCommands.eCmdSweepFrequencyValue6Absolute,
            NoteCommands.eCmdSweepFrequencyValue6Relative,
            NoteCommands.eCmdSweepFrequencyValue7Absolute,
            NoteCommands.eCmdSweepFrequencyValue7Relative,
            NoteCommands.eCmdSweepFrequencyValue8Absolute,
            NoteCommands.eCmdSweepFrequencyValue8Relative,
            NoteCommands.eCmdSweepFrequencyValue9Absolute,
            NoteCommands.eCmdSweepFrequencyValue9Relative,
            NoteCommands.eCmdSweepFrequencyValue10Absolute,
            NoteCommands.eCmdSweepFrequencyValue10Relative,
            NoteCommands.eCmdSweepFrequencyValue11Absolute,
            NoteCommands.eCmdSweepFrequencyValue11Relative,


            NoteCommands.eCmd_End,

		    /* set and adjust effect control parameters */
		    NoteCommands.eCmdSetEffectParam1,
            NoteCommands.eCmdIncEffectParam1,
            NoteCommands.eCmdSweepEffectParamAbs1,
            NoteCommands.eCmdSweepEffectParamRel1,

            NoteCommands.eCmdSetEffectParam2,
            NoteCommands.eCmdIncEffectParam2,
            NoteCommands.eCmdSweepEffectParamAbs2,
            NoteCommands.eCmdSweepEffectParamRel2,

            NoteCommands.eCmdSetEffectParam3,
            NoteCommands.eCmdIncEffectParam3,
            NoteCommands.eCmdSweepEffectParamAbs3,
            NoteCommands.eCmdSweepEffectParamRel3,

            NoteCommands.eCmdSetEffectParam4,
            NoteCommands.eCmdIncEffectParam4,
            NoteCommands.eCmdSweepEffectParamAbs4,
            NoteCommands.eCmdSweepEffectParamRel4,

            NoteCommands.eCmdSetEffectParam5,
            NoteCommands.eCmdIncEffectParam5,
            NoteCommands.eCmdSweepEffectParamAbs5,
            NoteCommands.eCmdSweepEffectParamRel5,

            NoteCommands.eCmdSetEffectParam6,
            NoteCommands.eCmdIncEffectParam6,
            NoteCommands.eCmdSweepEffectParamAbs6,
            NoteCommands.eCmdSweepEffectParamRel6,

            NoteCommands.eCmdSetEffectParam7,
            NoteCommands.eCmdIncEffectParam7,
            NoteCommands.eCmdSweepEffectParamAbs7,
            NoteCommands.eCmdSweepEffectParamRel7,

            NoteCommands.eCmdSetEffectParam8,
            NoteCommands.eCmdIncEffectParam8,
            NoteCommands.eCmdSweepEffectParamAbs8,
            NoteCommands.eCmdSweepEffectParamRel8,

            NoteCommands.eCmd_End,

		    /* track effect processor enable switch */
		    NoteCommands.eCmdTrackEffectEnable,

            NoteCommands.eCmd_End,

		    /* set and adjust global section effect control parameters */
		    NoteCommands.eCmdSetSectionEffectParam1,
            NoteCommands.eCmdIncSectionEffectParam1,
            NoteCommands.eCmdSweepSectionEffectParamAbs1,
            NoteCommands.eCmdSweepSectionEffectParamRel1,

            NoteCommands.eCmdSetSectionEffectParam2,
            NoteCommands.eCmdIncSectionEffectParam2,
            NoteCommands.eCmdSweepSectionEffectParamAbs2,
            NoteCommands.eCmdSweepSectionEffectParamRel2,

            NoteCommands.eCmdSetSectionEffectParam3,
            NoteCommands.eCmdIncSectionEffectParam3,
            NoteCommands.eCmdSweepSectionEffectParamAbs3,
            NoteCommands.eCmdSweepSectionEffectParamRel3,

            NoteCommands.eCmdSetSectionEffectParam4,
            NoteCommands.eCmdIncSectionEffectParam4,
            NoteCommands.eCmdSweepSectionEffectParamAbs4,
            NoteCommands.eCmdSweepSectionEffectParamRel4,

            NoteCommands.eCmdSetSectionEffectParam5,
            NoteCommands.eCmdIncSectionEffectParam5,
            NoteCommands.eCmdSweepSectionEffectParamAbs5,
            NoteCommands.eCmdSweepSectionEffectParamRel5,

            NoteCommands.eCmdSetSectionEffectParam6,
            NoteCommands.eCmdIncSectionEffectParam6,
            NoteCommands.eCmdSweepSectionEffectParamAbs6,
            NoteCommands.eCmdSweepSectionEffectParamRel6,

            NoteCommands.eCmdSetSectionEffectParam7,
            NoteCommands.eCmdIncSectionEffectParam7,
            NoteCommands.eCmdSweepSectionEffectParamAbs7,
            NoteCommands.eCmdSweepSectionEffectParamRel7,

            NoteCommands.eCmdSetSectionEffectParam8,
            NoteCommands.eCmdIncSectionEffectParam8,
            NoteCommands.eCmdSweepSectionEffectParamAbs8,
            NoteCommands.eCmdSweepSectionEffectParamRel8,

            NoteCommands.eCmd_End,

            NoteCommands.eCmdSectionEffectEnable,

            NoteCommands.eCmd_End,

		    /* set and adjust global score effect control parameters */
		    NoteCommands.eCmdSetScoreEffectParam1,
            NoteCommands.eCmdIncScoreEffectParam1,
            NoteCommands.eCmdSweepScoreEffectParamAbs1,
            NoteCommands.eCmdSweepScoreEffectParamRel1,

            NoteCommands.eCmdSetScoreEffectParam2,
            NoteCommands.eCmdIncScoreEffectParam2,
            NoteCommands.eCmdSweepScoreEffectParamAbs2,
            NoteCommands.eCmdSweepScoreEffectParamRel2,

            NoteCommands.eCmdSetScoreEffectParam3,
            NoteCommands.eCmdIncScoreEffectParam3,
            NoteCommands.eCmdSweepScoreEffectParamAbs3,
            NoteCommands.eCmdSweepScoreEffectParamRel3,

            NoteCommands.eCmdSetScoreEffectParam4,
            NoteCommands.eCmdIncScoreEffectParam4,
            NoteCommands.eCmdSweepScoreEffectParamAbs4,
            NoteCommands.eCmdSweepScoreEffectParamRel4,

            NoteCommands.eCmdSetScoreEffectParam5,
            NoteCommands.eCmdIncScoreEffectParam5,
            NoteCommands.eCmdSweepScoreEffectParamAbs5,
            NoteCommands.eCmdSweepScoreEffectParamRel5,

            NoteCommands.eCmdSetScoreEffectParam6,
            NoteCommands.eCmdIncScoreEffectParam6,
            NoteCommands.eCmdSweepScoreEffectParamAbs6,
            NoteCommands.eCmdSweepScoreEffectParamRel6,

            NoteCommands.eCmdSetScoreEffectParam7,
            NoteCommands.eCmdIncScoreEffectParam7,
            NoteCommands.eCmdSweepScoreEffectParamAbs7,
            NoteCommands.eCmdSweepScoreEffectParamRel7,

            NoteCommands.eCmdSetScoreEffectParam8,
            NoteCommands.eCmdIncScoreEffectParam8,
            NoteCommands.eCmdSweepScoreEffectParamAbs8,
            NoteCommands.eCmdSweepScoreEffectParamRel8,

            NoteCommands.eCmd_End,

            NoteCommands.eCmdScoreEffectEnable,

            NoteCommands.eCmd_End,

            NoteCommands.eCmdSequenceBegin,
            NoteCommands.eCmdSequenceEnd,
            NoteCommands.eCmdSetSequence,
            NoteCommands.eCmdSetSequenceDeferred,
            NoteCommands.eCmdEndSequencing,
            NoteCommands.eCmdSkip,
            NoteCommands.eCmdIgnoreNextCmd,

            NoteCommands.eCmd_End,

            NoteCommands.eCmdRedirect,
            NoteCommands.eCmdRedirectEnd,

            NoteCommands.eCmd_End,

            NoteCommands.eCmdReleaseAll1,
            NoteCommands.eCmdReleaseAll2,
            NoteCommands.eCmdReleaseAll3,

            NoteCommands.eCmd_End,

		    /* text marker in the score */
		    NoteCommands.eCmdMarker,
        };
    }
}
