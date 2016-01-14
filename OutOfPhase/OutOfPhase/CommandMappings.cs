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
using System.IO;

namespace OutOfPhase
{
    static class CommandMapping
    {
        // must be in sorted order on .Key
        private static readonly KeyValuePair<int, NoteCommands>[] FromFileIDMapping = new KeyValuePair<int, NoteCommands>[]
        {
            new KeyValuePair<int, NoteCommands>(16, NoteCommands.eCmdRestoreTempo),
            new KeyValuePair<int, NoteCommands>(17, NoteCommands.eCmdSetTempo),
            new KeyValuePair<int, NoteCommands>(18, NoteCommands.eCmdIncTempo),
            new KeyValuePair<int, NoteCommands>(19, NoteCommands.eCmdSweepTempoAbs),
            new KeyValuePair<int, NoteCommands>(20, NoteCommands.eCmdSweepTempoRel),
            new KeyValuePair<int, NoteCommands>(32, NoteCommands.eCmdRestoreStereoPosition),
            new KeyValuePair<int, NoteCommands>(33, NoteCommands.eCmdSetStereoPosition),
            new KeyValuePair<int, NoteCommands>(34, NoteCommands.eCmdIncStereoPosition),
            new KeyValuePair<int, NoteCommands>(35, NoteCommands.eCmdSweepStereoAbs),
            new KeyValuePair<int, NoteCommands>(36, NoteCommands.eCmdSweepStereoRel),
            new KeyValuePair<int, NoteCommands>(48, NoteCommands.eCmdRestoreVolume),
            new KeyValuePair<int, NoteCommands>(49, NoteCommands.eCmdSetVolume),
            new KeyValuePair<int, NoteCommands>(50, NoteCommands.eCmdIncVolume),
            new KeyValuePair<int, NoteCommands>(51, NoteCommands.eCmdSweepVolumeAbs),
            new KeyValuePair<int, NoteCommands>(52, NoteCommands.eCmdSweepVolumeRel),
            new KeyValuePair<int, NoteCommands>(64, NoteCommands.eCmdRestoreReleasePoint1),
            new KeyValuePair<int, NoteCommands>(65, NoteCommands.eCmdSetReleasePoint1),
            new KeyValuePair<int, NoteCommands>(66, NoteCommands.eCmdIncReleasePoint1),
            new KeyValuePair<int, NoteCommands>(67, NoteCommands.eCmdReleasePointOrigin1),
            new KeyValuePair<int, NoteCommands>(68, NoteCommands.eCmdSweepReleaseAbs1),
            new KeyValuePair<int, NoteCommands>(69, NoteCommands.eCmdSweepReleaseRel1),
            new KeyValuePair<int, NoteCommands>(80, NoteCommands.eCmdRestoreReleasePoint2),
            new KeyValuePair<int, NoteCommands>(81, NoteCommands.eCmdSetReleasePoint2),
            new KeyValuePair<int, NoteCommands>(82, NoteCommands.eCmdIncReleasePoint2),
            new KeyValuePair<int, NoteCommands>(83, NoteCommands.eCmdReleasePointOrigin2),
            new KeyValuePair<int, NoteCommands>(84, NoteCommands.eCmdSweepReleaseAbs2),
            new KeyValuePair<int, NoteCommands>(85, NoteCommands.eCmdSweepReleaseRel2),
            new KeyValuePair<int, NoteCommands>(96, NoteCommands.eCmdRestoreAccent1),
            new KeyValuePair<int, NoteCommands>(97, NoteCommands.eCmdSetAccent1),
            new KeyValuePair<int, NoteCommands>(98, NoteCommands.eCmdIncAccent1),
            new KeyValuePair<int, NoteCommands>(99, NoteCommands.eCmdSweepAccentAbs1),
            new KeyValuePair<int, NoteCommands>(100, NoteCommands.eCmdSweepAccentRel1),
            new KeyValuePair<int, NoteCommands>(112, NoteCommands.eCmdRestoreAccent2),
            new KeyValuePair<int, NoteCommands>(113, NoteCommands.eCmdSetAccent2),
            new KeyValuePair<int, NoteCommands>(114, NoteCommands.eCmdIncAccent2),
            new KeyValuePair<int, NoteCommands>(115, NoteCommands.eCmdSweepAccentAbs2),
            new KeyValuePair<int, NoteCommands>(116, NoteCommands.eCmdSweepAccentRel2),
            new KeyValuePair<int, NoteCommands>(128, NoteCommands.eCmdRestoreAccent3),
            new KeyValuePair<int, NoteCommands>(129, NoteCommands.eCmdSetAccent3),
            new KeyValuePair<int, NoteCommands>(130, NoteCommands.eCmdIncAccent3),
            new KeyValuePair<int, NoteCommands>(131, NoteCommands.eCmdSweepAccentAbs3),
            new KeyValuePair<int, NoteCommands>(132, NoteCommands.eCmdSweepAccentRel3),
            new KeyValuePair<int, NoteCommands>(144, NoteCommands.eCmdRestoreAccent4),
            new KeyValuePair<int, NoteCommands>(145, NoteCommands.eCmdSetAccent4),
            new KeyValuePair<int, NoteCommands>(146, NoteCommands.eCmdIncAccent4),
            new KeyValuePair<int, NoteCommands>(147, NoteCommands.eCmdSweepAccentAbs4),
            new KeyValuePair<int, NoteCommands>(148, NoteCommands.eCmdSweepAccentRel4),
            new KeyValuePair<int, NoteCommands>(160, NoteCommands.eCmdRestorePitchDispDepth),
            new KeyValuePair<int, NoteCommands>(161, NoteCommands.eCmdSetPitchDispDepth),
            new KeyValuePair<int, NoteCommands>(162, NoteCommands.eCmdIncPitchDispDepth),
		    /*new KeyValuePair<int, NoteCommands>(163, set pitch displacement depth modulation mode), */
		    new KeyValuePair<int, NoteCommands>(164, NoteCommands.eCmdSweepPitchDispDepthAbs),
            new KeyValuePair<int, NoteCommands>(165, NoteCommands.eCmdSweepPitchDispDepthRel),
            new KeyValuePair<int, NoteCommands>(176, NoteCommands.eCmdRestorePitchDispRate),
            new KeyValuePair<int, NoteCommands>(177, NoteCommands.eCmdSetPitchDispRate),
            new KeyValuePair<int, NoteCommands>(178, NoteCommands.eCmdIncPitchDispRate),
            new KeyValuePair<int, NoteCommands>(179, NoteCommands.eCmdSweepPitchDispRateAbs),
            new KeyValuePair<int, NoteCommands>(180, NoteCommands.eCmdSweepPitchDispRateRel),
            new KeyValuePair<int, NoteCommands>(192, NoteCommands.eCmdRestorePitchDispStart),
            new KeyValuePair<int, NoteCommands>(193, NoteCommands.eCmdSetPitchDispStart),
            new KeyValuePair<int, NoteCommands>(194, NoteCommands.eCmdIncPitchDispStart),
            new KeyValuePair<int, NoteCommands>(195, NoteCommands.eCmdPitchDispStartOrigin),
            new KeyValuePair<int, NoteCommands>(196, NoteCommands.eCmdSweepPitchDispStartAbs),
            new KeyValuePair<int, NoteCommands>(197, NoteCommands.eCmdSweepPitchDispStartRel),
            new KeyValuePair<int, NoteCommands>(208, NoteCommands.eCmdRestoreHurryUp),
            new KeyValuePair<int, NoteCommands>(209, NoteCommands.eCmdSetHurryUp),
            new KeyValuePair<int, NoteCommands>(210, NoteCommands.eCmdIncHurryUp),
            new KeyValuePair<int, NoteCommands>(211, NoteCommands.eCmdSweepHurryUpAbs),
            new KeyValuePair<int, NoteCommands>(212, NoteCommands.eCmdSweepHurryUpRel),
            new KeyValuePair<int, NoteCommands>(224, NoteCommands.eCmdRestoreDetune),
            new KeyValuePair<int, NoteCommands>(225, NoteCommands.eCmdSetDetune),
            new KeyValuePair<int, NoteCommands>(226, NoteCommands.eCmdIncDetune),
            new KeyValuePair<int, NoteCommands>(227, NoteCommands.eCmdDetuneMode),
            new KeyValuePair<int, NoteCommands>(228, NoteCommands.eCmdSweepDetuneAbs),
            new KeyValuePair<int, NoteCommands>(229, NoteCommands.eCmdSweepDetuneRel),
            new KeyValuePair<int, NoteCommands>(240, NoteCommands.eCmdRestoreEarlyLateAdjust),
            new KeyValuePair<int, NoteCommands>(241, NoteCommands.eCmdSetEarlyLateAdjust),
            new KeyValuePair<int, NoteCommands>(242, NoteCommands.eCmdIncEarlyLateAdjust),
            new KeyValuePair<int, NoteCommands>(243, NoteCommands.eCmdSweepEarlyLateAbs),
            new KeyValuePair<int, NoteCommands>(244, NoteCommands.eCmdSweepEarlyLateRel),
            new KeyValuePair<int, NoteCommands>(256, NoteCommands.eCmdRestoreDurationAdjust),
            new KeyValuePair<int, NoteCommands>(257, NoteCommands.eCmdSetDurationAdjust),
            new KeyValuePair<int, NoteCommands>(258, NoteCommands.eCmdIncDurationAdjust),
            new KeyValuePair<int, NoteCommands>(259, NoteCommands.eCmdSweepDurationAbs),
            new KeyValuePair<int, NoteCommands>(260, NoteCommands.eCmdSweepDurationRel),
            new KeyValuePair<int, NoteCommands>(261, NoteCommands.eCmdDurationAdjustMode),
            new KeyValuePair<int, NoteCommands>(272, NoteCommands.eCmdSetMeter),
            new KeyValuePair<int, NoteCommands>(273, NoteCommands.eCmdSetMeasureNumber),
            new KeyValuePair<int, NoteCommands>(288, NoteCommands.eCmdMarker),
            new KeyValuePair<int, NoteCommands>(304, NoteCommands.eCmdRestoreSurroundPosition),
            new KeyValuePair<int, NoteCommands>(305, NoteCommands.eCmdSetSurroundPosition),
            new KeyValuePair<int, NoteCommands>(306, NoteCommands.eCmdIncSurroundPosition),
            new KeyValuePair<int, NoteCommands>(307, NoteCommands.eCmdSweepSurroundAbs),
            new KeyValuePair<int, NoteCommands>(308, NoteCommands.eCmdSweepSurroundRel),
            new KeyValuePair<int, NoteCommands>(320, NoteCommands.eCmdSetTranspose),
            new KeyValuePair<int, NoteCommands>(321, NoteCommands.eCmdAdjustTranspose),
            new KeyValuePair<int, NoteCommands>(336, NoteCommands.eCmdSetEffectParam1),
            new KeyValuePair<int, NoteCommands>(337, NoteCommands.eCmdIncEffectParam1),
            new KeyValuePair<int, NoteCommands>(338, NoteCommands.eCmdSweepEffectParamAbs1),
            new KeyValuePair<int, NoteCommands>(339, NoteCommands.eCmdSweepEffectParamRel1),
            new KeyValuePair<int, NoteCommands>(352, NoteCommands.eCmdSetEffectParam2),
            new KeyValuePair<int, NoteCommands>(353, NoteCommands.eCmdIncEffectParam2),
            new KeyValuePair<int, NoteCommands>(354, NoteCommands.eCmdSweepEffectParamAbs2),
            new KeyValuePair<int, NoteCommands>(355, NoteCommands.eCmdSweepEffectParamRel2),
            new KeyValuePair<int, NoteCommands>(368, NoteCommands.eCmdSetEffectParam3),
            new KeyValuePair<int, NoteCommands>(369, NoteCommands.eCmdIncEffectParam3),
            new KeyValuePair<int, NoteCommands>(370, NoteCommands.eCmdSweepEffectParamAbs3),
            new KeyValuePair<int, NoteCommands>(371, NoteCommands.eCmdSweepEffectParamRel3),
            new KeyValuePair<int, NoteCommands>(384, NoteCommands.eCmdSetEffectParam4),
            new KeyValuePair<int, NoteCommands>(385, NoteCommands.eCmdIncEffectParam4),
            new KeyValuePair<int, NoteCommands>(386, NoteCommands.eCmdSweepEffectParamAbs4),
            new KeyValuePair<int, NoteCommands>(387, NoteCommands.eCmdSweepEffectParamRel4),
            new KeyValuePair<int, NoteCommands>(400, NoteCommands.eCmdSetScoreEffectParam1),
            new KeyValuePair<int, NoteCommands>(401, NoteCommands.eCmdIncScoreEffectParam1),
            new KeyValuePair<int, NoteCommands>(402, NoteCommands.eCmdSweepScoreEffectParamAbs1),
            new KeyValuePair<int, NoteCommands>(403, NoteCommands.eCmdSweepScoreEffectParamRel1),
            new KeyValuePair<int, NoteCommands>(416, NoteCommands.eCmdSetScoreEffectParam2),
            new KeyValuePair<int, NoteCommands>(417, NoteCommands.eCmdIncScoreEffectParam2),
            new KeyValuePair<int, NoteCommands>(418, NoteCommands.eCmdSweepScoreEffectParamAbs2),
            new KeyValuePair<int, NoteCommands>(419, NoteCommands.eCmdSweepScoreEffectParamRel2),
            new KeyValuePair<int, NoteCommands>(432, NoteCommands.eCmdSetScoreEffectParam3),
            new KeyValuePair<int, NoteCommands>(433, NoteCommands.eCmdIncScoreEffectParam3),
            new KeyValuePair<int, NoteCommands>(434, NoteCommands.eCmdSweepScoreEffectParamAbs3),
            new KeyValuePair<int, NoteCommands>(435, NoteCommands.eCmdSweepScoreEffectParamRel3),
            new KeyValuePair<int, NoteCommands>(448, NoteCommands.eCmdSetScoreEffectParam4),
            new KeyValuePair<int, NoteCommands>(449, NoteCommands.eCmdIncScoreEffectParam4),
            new KeyValuePair<int, NoteCommands>(450, NoteCommands.eCmdSweepScoreEffectParamAbs4),
            new KeyValuePair<int, NoteCommands>(451, NoteCommands.eCmdSweepScoreEffectParamRel4),
            new KeyValuePair<int, NoteCommands>(464, NoteCommands.eCmdTrackEffectEnable),
            new KeyValuePair<int, NoteCommands>(480, NoteCommands.eCmdRestoreAccent5),
            new KeyValuePair<int, NoteCommands>(481, NoteCommands.eCmdSetAccent5),
            new KeyValuePair<int, NoteCommands>(482, NoteCommands.eCmdIncAccent5),
            new KeyValuePair<int, NoteCommands>(483, NoteCommands.eCmdSweepAccentAbs5),
            new KeyValuePair<int, NoteCommands>(484, NoteCommands.eCmdSweepAccentRel5),
            new KeyValuePair<int, NoteCommands>(496, NoteCommands.eCmdRestoreAccent6),
            new KeyValuePair<int, NoteCommands>(497, NoteCommands.eCmdSetAccent6),
            new KeyValuePair<int, NoteCommands>(498, NoteCommands.eCmdIncAccent6),
            new KeyValuePair<int, NoteCommands>(499, NoteCommands.eCmdSweepAccentAbs6),
            new KeyValuePair<int, NoteCommands>(500, NoteCommands.eCmdSweepAccentRel6),
            new KeyValuePair<int, NoteCommands>(512, NoteCommands.eCmdRestoreAccent7),
            new KeyValuePair<int, NoteCommands>(513, NoteCommands.eCmdSetAccent7),
            new KeyValuePair<int, NoteCommands>(514, NoteCommands.eCmdIncAccent7),
            new KeyValuePair<int, NoteCommands>(515, NoteCommands.eCmdSweepAccentAbs7),
            new KeyValuePair<int, NoteCommands>(516, NoteCommands.eCmdSweepAccentRel7),
            new KeyValuePair<int, NoteCommands>(528, NoteCommands.eCmdRestoreAccent8),
            new KeyValuePair<int, NoteCommands>(529, NoteCommands.eCmdSetAccent8),
            new KeyValuePair<int, NoteCommands>(530, NoteCommands.eCmdIncAccent8),
            new KeyValuePair<int, NoteCommands>(531, NoteCommands.eCmdSweepAccentAbs8),
            new KeyValuePair<int, NoteCommands>(532, NoteCommands.eCmdSweepAccentRel8),
            new KeyValuePair<int, NoteCommands>(548, NoteCommands.eCmdSetEffectParam5),
            new KeyValuePair<int, NoteCommands>(549, NoteCommands.eCmdIncEffectParam5),
            new KeyValuePair<int, NoteCommands>(550, NoteCommands.eCmdSweepEffectParamAbs5),
            new KeyValuePair<int, NoteCommands>(551, NoteCommands.eCmdSweepEffectParamRel5),
            new KeyValuePair<int, NoteCommands>(564, NoteCommands.eCmdSetEffectParam6),
            new KeyValuePair<int, NoteCommands>(565, NoteCommands.eCmdIncEffectParam6),
            new KeyValuePair<int, NoteCommands>(566, NoteCommands.eCmdSweepEffectParamAbs6),
            new KeyValuePair<int, NoteCommands>(567, NoteCommands.eCmdSweepEffectParamRel6),
            new KeyValuePair<int, NoteCommands>(580, NoteCommands.eCmdSetEffectParam7),
            new KeyValuePair<int, NoteCommands>(581, NoteCommands.eCmdIncEffectParam7),
            new KeyValuePair<int, NoteCommands>(582, NoteCommands.eCmdSweepEffectParamAbs7),
            new KeyValuePair<int, NoteCommands>(583, NoteCommands.eCmdSweepEffectParamRel7),
            new KeyValuePair<int, NoteCommands>(596, NoteCommands.eCmdSetEffectParam8),
            new KeyValuePair<int, NoteCommands>(597, NoteCommands.eCmdIncEffectParam8),
            new KeyValuePair<int, NoteCommands>(598, NoteCommands.eCmdSweepEffectParamAbs8),
            new KeyValuePair<int, NoteCommands>(599, NoteCommands.eCmdSweepEffectParamRel8),
            new KeyValuePair<int, NoteCommands>(612, NoteCommands.eCmdSetScoreEffectParam5),
            new KeyValuePair<int, NoteCommands>(613, NoteCommands.eCmdIncScoreEffectParam5),
            new KeyValuePair<int, NoteCommands>(614, NoteCommands.eCmdSweepScoreEffectParamAbs5),
            new KeyValuePair<int, NoteCommands>(615, NoteCommands.eCmdSweepScoreEffectParamRel5),
            new KeyValuePair<int, NoteCommands>(628, NoteCommands.eCmdSetScoreEffectParam6),
            new KeyValuePair<int, NoteCommands>(629, NoteCommands.eCmdIncScoreEffectParam6),
            new KeyValuePair<int, NoteCommands>(630, NoteCommands.eCmdSweepScoreEffectParamAbs6),
            new KeyValuePair<int, NoteCommands>(631, NoteCommands.eCmdSweepScoreEffectParamRel6),
            new KeyValuePair<int, NoteCommands>(644, NoteCommands.eCmdSetScoreEffectParam7),
            new KeyValuePair<int, NoteCommands>(645, NoteCommands.eCmdIncScoreEffectParam7),
            new KeyValuePair<int, NoteCommands>(646, NoteCommands.eCmdSweepScoreEffectParamAbs7),
            new KeyValuePair<int, NoteCommands>(647, NoteCommands.eCmdSweepScoreEffectParamRel7),
            new KeyValuePair<int, NoteCommands>(660, NoteCommands.eCmdSetScoreEffectParam8),
            new KeyValuePair<int, NoteCommands>(661, NoteCommands.eCmdIncScoreEffectParam8),
            new KeyValuePair<int, NoteCommands>(662, NoteCommands.eCmdSweepScoreEffectParamAbs8),
            new KeyValuePair<int, NoteCommands>(663, NoteCommands.eCmdSweepScoreEffectParamRel8),
            new KeyValuePair<int, NoteCommands>(676, NoteCommands.eCmdSectionEffectEnable),
            new KeyValuePair<int, NoteCommands>(692, NoteCommands.eCmdSetFrequencyValueLegacy),
            new KeyValuePair<int, NoteCommands>(693, NoteCommands.eCmdAdjustFrequencyValueLegacy),
            new KeyValuePair<int, NoteCommands>(694, NoteCommands.eCmdResetFrequencyValue),
            new KeyValuePair<int, NoteCommands>(708, NoteCommands.eCmdSetSectionEffectParam1),
            new KeyValuePair<int, NoteCommands>(709, NoteCommands.eCmdIncSectionEffectParam1),
            new KeyValuePair<int, NoteCommands>(710, NoteCommands.eCmdSweepSectionEffectParamAbs1),
            new KeyValuePair<int, NoteCommands>(711, NoteCommands.eCmdSweepSectionEffectParamRel1),
            new KeyValuePair<int, NoteCommands>(724, NoteCommands.eCmdSetSectionEffectParam2),
            new KeyValuePair<int, NoteCommands>(725, NoteCommands.eCmdIncSectionEffectParam2),
            new KeyValuePair<int, NoteCommands>(726, NoteCommands.eCmdSweepSectionEffectParamAbs2),
            new KeyValuePair<int, NoteCommands>(727, NoteCommands.eCmdSweepSectionEffectParamRel2),
            new KeyValuePair<int, NoteCommands>(740, NoteCommands.eCmdSetSectionEffectParam3),
            new KeyValuePair<int, NoteCommands>(741, NoteCommands.eCmdIncSectionEffectParam3),
            new KeyValuePair<int, NoteCommands>(742, NoteCommands.eCmdSweepSectionEffectParamAbs3),
            new KeyValuePair<int, NoteCommands>(743, NoteCommands.eCmdSweepSectionEffectParamRel3),
            new KeyValuePair<int, NoteCommands>(756, NoteCommands.eCmdSetSectionEffectParam4),
            new KeyValuePair<int, NoteCommands>(757, NoteCommands.eCmdIncSectionEffectParam4),
            new KeyValuePair<int, NoteCommands>(758, NoteCommands.eCmdSweepSectionEffectParamAbs4),
            new KeyValuePair<int, NoteCommands>(759, NoteCommands.eCmdSweepSectionEffectParamRel4),
            new KeyValuePair<int, NoteCommands>(772, NoteCommands.eCmdSetSectionEffectParam5),
            new KeyValuePair<int, NoteCommands>(773, NoteCommands.eCmdIncSectionEffectParam5),
            new KeyValuePair<int, NoteCommands>(774, NoteCommands.eCmdSweepSectionEffectParamAbs5),
            new KeyValuePair<int, NoteCommands>(775, NoteCommands.eCmdSweepSectionEffectParamRel5),
            new KeyValuePair<int, NoteCommands>(788, NoteCommands.eCmdSetSectionEffectParam6),
            new KeyValuePair<int, NoteCommands>(789, NoteCommands.eCmdIncSectionEffectParam6),
            new KeyValuePair<int, NoteCommands>(790, NoteCommands.eCmdSweepSectionEffectParamAbs6),
            new KeyValuePair<int, NoteCommands>(791, NoteCommands.eCmdSweepSectionEffectParamRel6),
            new KeyValuePair<int, NoteCommands>(804, NoteCommands.eCmdSetSectionEffectParam7),
            new KeyValuePair<int, NoteCommands>(805, NoteCommands.eCmdIncSectionEffectParam7),
            new KeyValuePair<int, NoteCommands>(806, NoteCommands.eCmdSweepSectionEffectParamAbs7),
            new KeyValuePair<int, NoteCommands>(807, NoteCommands.eCmdSweepSectionEffectParamRel7),
            new KeyValuePair<int, NoteCommands>(820, NoteCommands.eCmdSetSectionEffectParam8),
            new KeyValuePair<int, NoteCommands>(821, NoteCommands.eCmdIncSectionEffectParam8),
            new KeyValuePair<int, NoteCommands>(822, NoteCommands.eCmdSweepSectionEffectParamAbs8),
            new KeyValuePair<int, NoteCommands>(823, NoteCommands.eCmdSweepSectionEffectParamRel8),
            new KeyValuePair<int, NoteCommands>(836, NoteCommands.eCmdScoreEffectEnable),
            new KeyValuePair<int, NoteCommands>(868, NoteCommands.eCmdSequenceBegin),
            new KeyValuePair<int, NoteCommands>(869, NoteCommands.eCmdSequenceEnd),
            new KeyValuePair<int, NoteCommands>(870, NoteCommands.eCmdSetSequence),
            new KeyValuePair<int, NoteCommands>(871, NoteCommands.eCmdEndSequencing),
            new KeyValuePair<int, NoteCommands>(872, NoteCommands.eCmdSetSequenceDeferred),
            new KeyValuePair<int, NoteCommands>(873, NoteCommands.eCmdReleaseAll1),
            new KeyValuePair<int, NoteCommands>(874, NoteCommands.eCmdReleaseAll2),
            new KeyValuePair<int, NoteCommands>(875, NoteCommands.eCmdReleaseAll3),
            new KeyValuePair<int, NoteCommands>(876, NoteCommands.eCmdRedirect),
            new KeyValuePair<int, NoteCommands>(877, NoteCommands.eCmdRedirectEnd),
            new KeyValuePair<int, NoteCommands>(878, NoteCommands.eCmdSkip),
            new KeyValuePair<int, NoteCommands>(879, NoteCommands.eCmdIgnoreNextCmd),
            new KeyValuePair<int, NoteCommands>(896, NoteCommands.eCmdRestorePortamento),
            new KeyValuePair<int, NoteCommands>(897, NoteCommands.eCmdSetPortamento),
            new KeyValuePair<int, NoteCommands>(898, NoteCommands.eCmdIncPortamento),
            new KeyValuePair<int, NoteCommands>(899, NoteCommands.eCmdSweepPortamentoAbs),
            new KeyValuePair<int, NoteCommands>(900, NoteCommands.eCmdSweepPortamentoRel),
            new KeyValuePair<int, NoteCommands>(940, NoteCommands.eCmdLoadFrequencyModel),
            new KeyValuePair<int, NoteCommands>(941, NoteCommands.eCmdSetFrequencyValue),
            new KeyValuePair<int, NoteCommands>(942, NoteCommands.eCmdAdjustFrequencyValue),
            new KeyValuePair<int, NoteCommands>(951, NoteCommands.eCmdSweepFrequencyValue0Absolute),
            new KeyValuePair<int, NoteCommands>(952, NoteCommands.eCmdSweepFrequencyValue1Absolute),
            new KeyValuePair<int, NoteCommands>(953, NoteCommands.eCmdSweepFrequencyValue2Absolute),
            new KeyValuePair<int, NoteCommands>(954, NoteCommands.eCmdSweepFrequencyValue3Absolute),
            new KeyValuePair<int, NoteCommands>(955, NoteCommands.eCmdSweepFrequencyValue4Absolute),
            new KeyValuePair<int, NoteCommands>(956, NoteCommands.eCmdSweepFrequencyValue5Absolute),
            new KeyValuePair<int, NoteCommands>(957, NoteCommands.eCmdSweepFrequencyValue6Absolute),
            new KeyValuePair<int, NoteCommands>(958, NoteCommands.eCmdSweepFrequencyValue7Absolute),
            new KeyValuePair<int, NoteCommands>(959, NoteCommands.eCmdSweepFrequencyValue8Absolute),
            new KeyValuePair<int, NoteCommands>(960, NoteCommands.eCmdSweepFrequencyValue9Absolute),
            new KeyValuePair<int, NoteCommands>(961, NoteCommands.eCmdSweepFrequencyValue10Absolute),
            new KeyValuePair<int, NoteCommands>(962, NoteCommands.eCmdSweepFrequencyValue11Absolute),
            new KeyValuePair<int, NoteCommands>(963, NoteCommands.eCmdSweepFrequencyValue0Relative),
            new KeyValuePair<int, NoteCommands>(964, NoteCommands.eCmdSweepFrequencyValue1Relative),
            new KeyValuePair<int, NoteCommands>(965, NoteCommands.eCmdSweepFrequencyValue2Relative),
            new KeyValuePair<int, NoteCommands>(966, NoteCommands.eCmdSweepFrequencyValue3Relative),
            new KeyValuePair<int, NoteCommands>(967, NoteCommands.eCmdSweepFrequencyValue4Relative),
            new KeyValuePair<int, NoteCommands>(968, NoteCommands.eCmdSweepFrequencyValue5Relative),
            new KeyValuePair<int, NoteCommands>(969, NoteCommands.eCmdSweepFrequencyValue6Relative),
            new KeyValuePair<int, NoteCommands>(970, NoteCommands.eCmdSweepFrequencyValue7Relative),
            new KeyValuePair<int, NoteCommands>(971, NoteCommands.eCmdSweepFrequencyValue8Relative),
            new KeyValuePair<int, NoteCommands>(972, NoteCommands.eCmdSweepFrequencyValue9Relative),
            new KeyValuePair<int, NoteCommands>(973, NoteCommands.eCmdSweepFrequencyValue10Relative),
            new KeyValuePair<int, NoteCommands>(974, NoteCommands.eCmdSweepFrequencyValue11Relative),
        };

        private static bool fCheckedFromFileIDMapping; // for debug
        public static NoteCommands FromFileID(int FileID)
        {
            #region code correctness check
            /* verify structure */
            if (!fCheckedFromFileIDMapping)
            {
                for (int j = 1; j < FromFileIDMapping.Length; j++)
                {
                    /* we use binary search, so make sure it is sorted by fileid */
                    if (!(FromFileIDMapping[j].Key > FromFileIDMapping[j - 1].Key))
                    {
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                }
                fCheckedFromFileIDMapping = true;
            }
            #endregion

            int i = Array.BinarySearch(FromFileIDMapping, new KeyValuePair<int, NoteCommands>(FileID, NoteCommands.eCmd_Start/*bogus*/), new FromFileIDComparer());
            if (i < 0)
            {
                throw new InvalidDataException();
            }
            return FromFileIDMapping[i].Value;
        }

        private class FromFileIDComparer : IComparer<KeyValuePair<int, NoteCommands>>
        {
            public int Compare(KeyValuePair<int, NoteCommands> x, KeyValuePair<int, NoteCommands> y)
            {
                return x.Key.CompareTo(y.Key);
            }
        }

        private static readonly KeyValuePair<NoteCommands, int>[] ToFileIDMapping = new KeyValuePair<NoteCommands, int>[]
        {
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRestoreTempo, 16),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetTempo, 17),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncTempo, 18),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepTempoAbs, 19),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepTempoRel, 20),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRestoreStereoPosition, 32),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetStereoPosition, 33),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncStereoPosition, 34),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepStereoAbs, 35),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepStereoRel, 36),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRestoreSurroundPosition, 304),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetSurroundPosition, 305),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncSurroundPosition, 306),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepSurroundAbs, 307),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepSurroundRel, 308),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRestoreVolume, 48),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetVolume, 49),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncVolume, 50),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepVolumeAbs, 51),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepVolumeRel, 52),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRestoreReleasePoint1, 64),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetReleasePoint1, 65),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncReleasePoint1, 66),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdReleasePointOrigin1, 67),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepReleaseAbs1, 68),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepReleaseRel1, 69),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRestoreReleasePoint2, 80),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetReleasePoint2, 81),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncReleasePoint2, 82),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdReleasePointOrigin2, 83),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepReleaseAbs2, 84),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepReleaseRel2, 85),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRestoreAccent1, 96),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetAccent1, 97),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncAccent1, 98),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepAccentAbs1, 99),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepAccentRel1, 100),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRestoreAccent2, 112),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetAccent2, 113),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncAccent2, 114),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepAccentAbs2, 115),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepAccentRel2, 116),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRestoreAccent3, 128),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetAccent3, 129),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncAccent3, 130),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepAccentAbs3, 131),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepAccentRel3, 132),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRestoreAccent4, 144),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetAccent4, 145),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncAccent4, 146),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepAccentAbs4, 147),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepAccentRel4, 148),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRestoreAccent5, 480),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetAccent5, 481),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncAccent5, 482),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepAccentAbs5, 483),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepAccentRel5, 484),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRestoreAccent6, 496),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetAccent6, 497),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncAccent6, 498),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepAccentAbs6, 499),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepAccentRel6, 500),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRestoreAccent7, 512),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetAccent7, 513),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncAccent7, 514),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepAccentAbs7, 515),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepAccentRel7, 516),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRestoreAccent8, 528),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetAccent8, 529),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncAccent8, 530),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepAccentAbs8, 531),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepAccentRel8, 532),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRestorePitchDispDepth, 160),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetPitchDispDepth, 161),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncPitchDispDepth, 162),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepPitchDispDepthAbs, 164),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepPitchDispDepthRel, 165),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRestorePitchDispRate, 176),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetPitchDispRate, 177),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncPitchDispRate, 178),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepPitchDispRateAbs, 179),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepPitchDispRateRel, 180),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRestorePitchDispStart, 192),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetPitchDispStart, 193),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncPitchDispStart, 194),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdPitchDispStartOrigin, 195),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepPitchDispStartAbs, 196),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepPitchDispStartRel, 197),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRestoreHurryUp, 208),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetHurryUp, 209),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncHurryUp, 210),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepHurryUpAbs, 211),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepHurryUpRel, 212),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRestoreDetune, 224),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetDetune, 225),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncDetune, 226),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdDetuneMode, 227),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepDetuneAbs, 228),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepDetuneRel, 229),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRestoreEarlyLateAdjust, 240),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetEarlyLateAdjust, 241),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncEarlyLateAdjust, 242),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepEarlyLateAbs, 243),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepEarlyLateRel, 244),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRestoreDurationAdjust, 256),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetDurationAdjust, 257),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncDurationAdjust, 258),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepDurationAbs, 259),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepDurationRel, 260),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdDurationAdjustMode, 261),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetMeter, 272),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetMeasureNumber, 273),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetTranspose, 320),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdAdjustTranspose, 321),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetFrequencyValue, 941),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetFrequencyValueLegacy, 692),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdAdjustFrequencyValue, 942),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdAdjustFrequencyValueLegacy, 693),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdResetFrequencyValue, 694),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdLoadFrequencyModel, 940),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue0Absolute, 951),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue1Absolute, 952),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue2Absolute, 953),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue3Absolute, 954),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue4Absolute, 955),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue5Absolute, 956),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue6Absolute, 957),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue7Absolute, 958),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue8Absolute, 959),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue9Absolute, 960),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue10Absolute, 961),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue11Absolute, 962),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue0Relative, 963),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue1Relative, 964),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue2Relative, 965),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue3Relative, 966),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue4Relative, 967),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue5Relative, 968),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue6Relative, 969),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue7Relative, 970),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue8Relative, 971),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue9Relative, 972),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue10Relative, 973),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepFrequencyValue11Relative, 974),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetEffectParam1, 336),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncEffectParam1, 337),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepEffectParamAbs1, 338),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepEffectParamRel1, 339),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetEffectParam2, 352),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncEffectParam2, 353),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepEffectParamAbs2, 354),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepEffectParamRel2, 355),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetEffectParam3, 368),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncEffectParam3, 369),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepEffectParamAbs3, 370),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepEffectParamRel3, 371),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetEffectParam4, 384),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncEffectParam4, 385),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepEffectParamAbs4, 386),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepEffectParamRel4, 387),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetEffectParam5, 548),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncEffectParam5, 549),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepEffectParamAbs5, 550),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepEffectParamRel5, 551),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetEffectParam6, 564),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncEffectParam6, 565),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepEffectParamAbs6, 566),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepEffectParamRel6, 567),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetEffectParam7, 580),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncEffectParam7, 581),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepEffectParamAbs7, 582),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepEffectParamRel7, 583),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetEffectParam8, 596),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncEffectParam8, 597),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepEffectParamAbs8, 598),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepEffectParamRel8, 599),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdTrackEffectEnable, 464),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetScoreEffectParam1, 400),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncScoreEffectParam1, 401),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepScoreEffectParamAbs1, 402),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepScoreEffectParamRel1, 403),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetScoreEffectParam2, 416),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncScoreEffectParam2, 417),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepScoreEffectParamAbs2, 418),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepScoreEffectParamRel2, 419),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetScoreEffectParam3, 432),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncScoreEffectParam3, 433),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepScoreEffectParamAbs3, 434),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepScoreEffectParamRel3, 435),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetScoreEffectParam4, 448),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncScoreEffectParam4, 449),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepScoreEffectParamAbs4, 450),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepScoreEffectParamRel4, 451),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetScoreEffectParam5, 612),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncScoreEffectParam5, 613),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepScoreEffectParamAbs5, 614),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepScoreEffectParamRel5, 615),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetScoreEffectParam6, 628),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncScoreEffectParam6, 629),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepScoreEffectParamAbs6, 630),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepScoreEffectParamRel6, 631),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetScoreEffectParam7, 644),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncScoreEffectParam7, 645),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepScoreEffectParamAbs7, 646),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepScoreEffectParamRel7, 647),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetScoreEffectParam8, 660),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncScoreEffectParam8, 661),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepScoreEffectParamAbs8, 662),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepScoreEffectParamRel8, 663),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdMarker, 288),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSectionEffectEnable, 676),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdScoreEffectEnable, 836),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetSectionEffectParam1, 708),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncSectionEffectParam1, 709),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepSectionEffectParamAbs1, 710),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepSectionEffectParamRel1, 711),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetSectionEffectParam2, 724),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncSectionEffectParam2, 725),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepSectionEffectParamAbs2, 726),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepSectionEffectParamRel2, 727),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetSectionEffectParam3, 740),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncSectionEffectParam3, 741),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepSectionEffectParamAbs3, 742),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepSectionEffectParamRel3, 743),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetSectionEffectParam4, 756),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncSectionEffectParam4, 757),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepSectionEffectParamAbs4, 758),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepSectionEffectParamRel4, 759),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetSectionEffectParam5, 772),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncSectionEffectParam5, 773),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepSectionEffectParamAbs5, 774),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepSectionEffectParamRel5, 775),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetSectionEffectParam6, 788),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncSectionEffectParam6, 789),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepSectionEffectParamAbs6, 790),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepSectionEffectParamRel6, 791),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetSectionEffectParam7, 804),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncSectionEffectParam7, 805),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepSectionEffectParamAbs7, 806),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepSectionEffectParamRel7, 807),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetSectionEffectParam8, 820),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncSectionEffectParam8, 821),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepSectionEffectParamAbs8, 822),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepSectionEffectParamRel8, 823),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSequenceBegin, 868),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSequenceEnd, 869),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetSequence, 870),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetSequenceDeferred, 872),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdEndSequencing, 871),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSkip, 878),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIgnoreNextCmd, 879),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRedirect, 876),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRedirectEnd, 877),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdReleaseAll1, 873),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdReleaseAll2, 874),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdReleaseAll3, 875),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdRestorePortamento, 896),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSetPortamento, 897),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdIncPortamento, 898),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepPortamentoAbs, 899),
            new KeyValuePair<NoteCommands, int>(NoteCommands.eCmdSweepPortamentoRel, 900),
        };

        private static bool fCheckedToFileIDMapping; // for debug
        public static int ToFileID(NoteCommands Command)
        {
            #region code correctness check
            /* verify structure */
            if (!fCheckedToFileIDMapping)
            {
                for (int j = 0; j < ToFileIDMapping.Length; j += 1)
                {
                    if (ToFileIDMapping[j].Key != j + NoteCommands.eCmd_Start)
                    {
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                }
                fCheckedToFileIDMapping = true;
            }
            #endregion

            /* direct index */
            return ToFileIDMapping[Command - NoteCommands.eCmd_Start].Value;
        }

        // must be direct mapping (e.g. .Key of element is the index into the array for that element)
        private static readonly KeyValuePair<NoteCommands, CommandAddrMode>[] CommandAddrModes = new KeyValuePair<NoteCommands, CommandAddrMode>[]
        {
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRestoreTempo, /* no parameters */
			    CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetTempo, /* <1xs> */
			    CommandAddrMode.e1SmallExtParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncTempo, /* <1xs> */
			    CommandAddrMode.e1SmallExtParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepTempoAbs, /* <1xs> <2xs> */
			    CommandAddrMode.e2SmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepTempoRel, /* <1xs> <2xs> */
			    CommandAddrMode.e2SmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRestoreStereoPosition, /* no parameters */
			    CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetStereoPosition, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncStereoPosition, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepStereoAbs, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepStereoRel, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRestoreSurroundPosition, /* no parameters */
			    CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetSurroundPosition, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncSurroundPosition, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepSurroundAbs, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepSurroundRel, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRestoreVolume, /* no parameters */
			    CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetVolume, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncVolume, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepVolumeAbs, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepVolumeRel, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRestoreReleasePoint1, /* no parameters */
			    CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetReleasePoint1, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncReleasePoint1, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdReleasePointOrigin1, /* origin <1i> */
			    CommandAddrMode.e1ParamReleaseOrigin),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepReleaseAbs1, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepReleaseRel1, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRestoreReleasePoint2, /* no parameters */
			    CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetReleasePoint2, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncReleasePoint2, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdReleasePointOrigin2, /* origin <1i> */
			    CommandAddrMode.e1ParamReleaseOrigin),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepReleaseAbs2, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepReleaseRel2, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRestoreAccent1, /* no parameters */
			    CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetAccent1, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncAccent1, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepAccentAbs1, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepAccentRel1, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRestoreAccent2, /* no parameters */
			    CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetAccent2, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncAccent2, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepAccentAbs2, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepAccentRel2, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRestoreAccent3, /* no parameters */
			    CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetAccent3, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncAccent3, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepAccentAbs3, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepAccentRel3, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRestoreAccent4, /* no parameters */
			    CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetAccent4, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncAccent4, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepAccentAbs4, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepAccentRel4, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRestoreAccent5, /* no parameters */
			    CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetAccent5, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncAccent5, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepAccentAbs5, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepAccentRel5, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRestoreAccent6, /* no parameters */
			    CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetAccent6, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncAccent6, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepAccentAbs6, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepAccentRel6, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRestoreAccent7, /* no parameters */
			    CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetAccent7, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncAccent7, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepAccentAbs7, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepAccentRel7, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRestoreAccent8, /* no parameters */
			    CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetAccent8, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncAccent8, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepAccentAbs8, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepAccentRel8, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRestorePitchDispDepth, /* no parameters */
			    CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetPitchDispDepth, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncPitchDispDepth, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepPitchDispDepthAbs, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepPitchDispDepthRel, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRestorePitchDispRate, /* no parameters */
			    CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetPitchDispRate, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncPitchDispRate, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepPitchDispRateAbs, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepPitchDispRateRel, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRestorePitchDispStart, /* no parameters */
			    CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetPitchDispStart, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncPitchDispStart, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdPitchDispStartOrigin, /* origin <1i> */
			    CommandAddrMode.e1ParamReleaseOrigin),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepPitchDispStartAbs, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepPitchDispStartRel, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRestoreHurryUp, /* no parameters */
			    CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetHurryUp, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncHurryUp, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepHurryUpAbs, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepHurryUpRel, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRestoreDetune, /* no parameters */
			    CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetDetune, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncDetune, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdDetuneMode, /* hertz/steps <1i> */
			    CommandAddrMode.e1PitchDisplacementMode),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepDetuneAbs, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepDetuneRel, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRestoreEarlyLateAdjust, /* no parameters */
			    CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetEarlyLateAdjust, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncEarlyLateAdjust, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepEarlyLateAbs, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepEarlyLateRel, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRestoreDurationAdjust, /* no parameters */
			    CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetDurationAdjust, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncDurationAdjust, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepDurationAbs, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepDurationRel, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdDurationAdjustMode, /* multiplicative/additive <1i> */
			    CommandAddrMode.e1DurationAdjustMode),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetMeter, /* <1i> <2i> */
			    CommandAddrMode.e2IntegerParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetMeasureNumber, /* <1i> */
			    CommandAddrMode.e1IntegerParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetTranspose, /* <1i> */
			    CommandAddrMode.e1IntegerParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdAdjustTranspose, /* <1i> */
			    CommandAddrMode.e1IntegerParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetFrequencyValue, /* <1l> <2xs> */
			    CommandAddrMode.eFirstIntSecondLargeParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetFrequencyValueLegacy, /* <1l> <2xs> */
			    CommandAddrMode.eFirstIntSecondLargeParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdAdjustFrequencyValue, /* <1l> <2xs> */
			    CommandAddrMode.eFirstIntSecondLargeParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdAdjustFrequencyValueLegacy, /* <1l> <2xs> */
			    CommandAddrMode.eFirstIntSecondLargeParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdResetFrequencyValue, /* <1i> */
			    CommandAddrMode.e1IntegerParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdLoadFrequencyModel, // <1s> <1l>
			    CommandAddrMode.e1String1LargeBCDParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue0Absolute, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue1Absolute, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue2Absolute, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue3Absolute, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue4Absolute, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue5Absolute, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue6Absolute, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue7Absolute, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue8Absolute, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue9Absolute, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue10Absolute, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue11Absolute, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue0Relative, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue1Relative, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue2Relative, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue3Relative, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue4Relative, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue5Relative, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue6Relative, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue7Relative, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue8Relative, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue9Relative, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue10Relative, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepFrequencyValue11Relative, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetEffectParam1, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncEffectParam1, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepEffectParamAbs1, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepEffectParamRel1, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetEffectParam2, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncEffectParam2, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepEffectParamAbs2, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepEffectParamRel2, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetEffectParam3, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncEffectParam3, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepEffectParamAbs3, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepEffectParamRel3, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetEffectParam4, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncEffectParam4, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepEffectParamAbs4, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepEffectParamRel4, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetEffectParam5, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncEffectParam5, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepEffectParamAbs5, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepEffectParamRel5, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetEffectParam6, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncEffectParam6, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepEffectParamAbs6, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepEffectParamRel6, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetEffectParam7, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncEffectParam7, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepEffectParamAbs7, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepEffectParamRel7, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetEffectParam8, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncEffectParam8, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepEffectParamAbs8, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepEffectParamRel8, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdTrackEffectEnable, /* enable/disable <1i> */
			    CommandAddrMode.e1TrackEffectsMode),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetScoreEffectParam1, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncScoreEffectParam1, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepScoreEffectParamAbs1, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepScoreEffectParamRel1, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetScoreEffectParam2, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncScoreEffectParam2, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepScoreEffectParamAbs2, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepScoreEffectParamRel2, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetScoreEffectParam3, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncScoreEffectParam3, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepScoreEffectParamAbs3, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepScoreEffectParamRel3, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetScoreEffectParam4, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncScoreEffectParam4, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepScoreEffectParamAbs4, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepScoreEffectParamRel4, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetScoreEffectParam5, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncScoreEffectParam5, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepScoreEffectParamAbs5, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepScoreEffectParamRel5, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetScoreEffectParam6, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncScoreEffectParam6, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepScoreEffectParamAbs6, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepScoreEffectParamRel6, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetScoreEffectParam7, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncScoreEffectParam7, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepScoreEffectParamAbs7, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepScoreEffectParamRel7, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetScoreEffectParam8, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncScoreEffectParam8, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepScoreEffectParamAbs8, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepScoreEffectParamRel8, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdMarker, /* <string> */
			    CommandAddrMode.e1StringParameterWithLineFeeds),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSectionEffectEnable, /* enable/disable <1i> */
			    CommandAddrMode.e1TrackEffectsMode),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdScoreEffectEnable, /* enable/disable <1i> */
			    CommandAddrMode.e1TrackEffectsMode),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetSectionEffectParam1, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncSectionEffectParam1, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepSectionEffectParamAbs1, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepSectionEffectParamRel1, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetSectionEffectParam2, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncSectionEffectParam2, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepSectionEffectParamAbs2, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepSectionEffectParamRel2, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetSectionEffectParam3, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncSectionEffectParam3, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepSectionEffectParamAbs3, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepSectionEffectParamRel3, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetSectionEffectParam4, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncSectionEffectParam4, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepSectionEffectParamAbs4, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepSectionEffectParamRel4, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetSectionEffectParam5, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncSectionEffectParam5, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepSectionEffectParamAbs5, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepSectionEffectParamRel5, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetSectionEffectParam6, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncSectionEffectParam6, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepSectionEffectParamAbs6, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepSectionEffectParamRel6, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetSectionEffectParam7, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncSectionEffectParam7, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepSectionEffectParamAbs7, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepSectionEffectParamRel7, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetSectionEffectParam8, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncSectionEffectParam8, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepSectionEffectParamAbs8, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepSectionEffectParamRel8, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSequenceBegin, /* <string> holds sequence name */
			    CommandAddrMode.e1StringParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSequenceEnd,
                CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetSequence, /* <string1> holds track/group name, <string2> hold sequence name */
			    CommandAddrMode.e2StringParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetSequenceDeferred, /* <string1> holds track/group name, <string2> hold sequence name */
			    CommandAddrMode.e2StringParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdEndSequencing, /* <string1> holds track/group name */
			    CommandAddrMode.e1StringParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSkip, /* <1l> holds number of beats */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIgnoreNextCmd, /* <1l> = probability of ignoring next command */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRedirect, /* <string> holds target track/group name */
			    CommandAddrMode.e1StringParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRedirectEnd,
                CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdReleaseAll1,
                CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdReleaseAll2,
                CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdReleaseAll3,
                CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdRestorePortamento, /* no parameters */
			    CommandAddrMode.eNoParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSetPortamento, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdIncPortamento, /* <1l> */
			    CommandAddrMode.e1LargeParameter),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepPortamentoAbs, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters),
            new KeyValuePair<NoteCommands, CommandAddrMode>(NoteCommands.eCmdSweepPortamentoRel, /* <1l> <2xs> */
			    CommandAddrMode.eFirstLargeSecondSmallExtParameters)
        };

        private static bool fVerifiedGetCommandAddressingMode;
        public static CommandAddrMode GetCommandAddressingMode(NoteCommands Command)
        {
            #region code correctness check
            /* verify structure */
            if (!fVerifiedGetCommandAddressingMode)
            {
                for (int i = 0; i < CommandAddrModes.Length; i++)
                {
                    if (CommandAddrModes[i].Key != i + NoteCommands.eCmd_Start)
                    {
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                }
                fVerifiedGetCommandAddressingMode = true;
            }
            #endregion

            /* direct index */
            return CommandAddrModes[Command - NoteCommands.eCmd_Start].Value;
        }


        private static readonly KeyValuePair<NoteCommands, string>[] CommandNames = new KeyValuePair<NoteCommands, string>[]
        {
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRestoreTempo,
                "Restore Tempo"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetTempo,
                "Set Tempo"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncTempo,
                "Adjust Tempo"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepTempoAbs,
                "Sweep Tempo Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepTempoRel,
                "Sweep Tempo Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRestoreStereoPosition,
                "Restore Stereo Position"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetStereoPosition,
                "Set Stereo Position"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncStereoPosition,
                "Adjust Stereo Position"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepStereoAbs,
                "Sweep Stereo Position Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepStereoRel,
                "Sweep Stereo Position Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRestoreSurroundPosition,
                "Restore Surround Position"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetSurroundPosition,
                "Set Surround Position"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncSurroundPosition,
                "Adjust Surround Position"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepSurroundAbs,
                "Sweep Surround Position Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepSurroundRel,
                "Sweep Surround Position Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRestoreVolume,
                "Restore Volume"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetVolume,
                "Set Volume"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncVolume,
                "Adjust Volume"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepVolumeAbs,
                "Sweep Volume Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepVolumeRel,
                "Sweep Volume Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRestoreReleasePoint1,
                "Restore Release Point 1"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetReleasePoint1,
                "Set Release Point 1"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncReleasePoint1,
                "Adjust Release Point 1"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdReleasePointOrigin1,
                "Set Release Point 1 Origin"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepReleaseAbs1,
                "Sweep Release Point 1 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepReleaseRel1,
                "Sweep Release Point 1 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRestoreReleasePoint2,
                "Restore Release Point 2"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetReleasePoint2,
                "Set Release Point 2"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncReleasePoint2,
                "Adjust Release Point 2"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdReleasePointOrigin2,
                "Set Release Point 2 Origin"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepReleaseAbs2,
                "Sweep Release Point 2 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepReleaseRel2,
                "Sweep Release Point 2 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRestoreAccent1,
                "Restore Accent 1"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetAccent1,
                "Set Accent 1"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncAccent1,
                "Adjust Accent 1"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepAccentAbs1,
                "Sweep Accent 1 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepAccentRel1,
                "Sweep Accent 1 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRestoreAccent2,
                "Restore Accent 2"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetAccent2,
                "Set Accent 2"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncAccent2,
                "Adjust Accent 2"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepAccentAbs2,
                "Sweep Accent 2 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepAccentRel2,
                "Sweep Accent 2 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRestoreAccent3,
                "Restore Accent 3"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetAccent3,
                "Set Accent 3"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncAccent3,
                "Adjust Accent 3"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepAccentAbs3,
                "Sweep Accent 3 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepAccentRel3,
                "Sweep Accent 3 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRestoreAccent4,
                "Restore Accent 4"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetAccent4,
                "Set Accent 4"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncAccent4,
                "Adjust Accent 4"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepAccentAbs4,
                "Sweep Accent 4 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepAccentRel4,
                "Sweep Accent 4 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRestoreAccent5,
                "Restore Accent 5"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetAccent5,
                "Set Accent 5"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncAccent5,
                "Adjust Accent 5"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepAccentAbs5,
                "Sweep Accent 5 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepAccentRel5,
                "Sweep Accent 5 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRestoreAccent6,
                "Restore Accent 6"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetAccent6,
                "Set Accent 6"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncAccent6,
                "Adjust Accent 6"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepAccentAbs6,
                "Sweep Accent 6 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepAccentRel6,
                "Sweep Accent 6 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRestoreAccent7,
                "Restore Accent 7"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetAccent7,
                "Set Accent 7"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncAccent7,
                "Adjust Accent 7"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepAccentAbs7,
                "Sweep Accent 7 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepAccentRel7,
                "Sweep Accent 7 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRestoreAccent8,
                "Restore Accent 8"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetAccent8,
                "Set Accent 8"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncAccent8,
                "Adjust Accent 8"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepAccentAbs8,
                "Sweep Accent 8 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepAccentRel8,
                "Sweep Accent 8 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRestorePitchDispDepth,
                "Restore Pitch Displacement Depth"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetPitchDispDepth,
                "Set Pitch Displacement Depth"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncPitchDispDepth,
                "Adjust Pitch Displacement Depth"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepPitchDispDepthAbs,
                "Sweep Pitch Displacement Depth Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepPitchDispDepthRel,
                "Sweep Pitch Displacement Depth Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRestorePitchDispRate,
                "Restore Pitch Displacement Rate"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetPitchDispRate,
                "Set Pitch Displacement Rate"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncPitchDispRate,
                "Adjust Pitch Displacement Rate"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepPitchDispRateAbs,
                "Sweep Pitch Displacement Rate Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepPitchDispRateRel,
                "Sweep Pitch Displacement Rate Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRestorePitchDispStart,
                "Restore Pitch Displacement Start Point"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetPitchDispStart,
                "Set Pitch Displacement Start Point"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncPitchDispStart,
                "Adjust Pitch Displacement Start Point"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdPitchDispStartOrigin,
                "Set Pitch Displacement Start Point Origin"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepPitchDispStartAbs,
                "Sweep Pitch Displacement Start Point Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepPitchDispStartRel,
                "Sweep Pitch Displacement Start Point Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRestoreHurryUp,
                "Restore Hurry-Up"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetHurryUp,
                "Set Hurry-Up"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncHurryUp,
                "Adjust Hurry-Up"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepHurryUpAbs,
                "Sweep Hurry-Up Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepHurryUpRel,
                "Sweep Hurry-Up Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRestoreDetune,
                "Restore Detuning"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetDetune,
                "Set Detuning"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncDetune,
                "Adjust Detuning"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdDetuneMode,
                "Set Detuning Mode"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepDetuneAbs,
                "Sweep Detuning Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepDetuneRel,
                "Sweep Detuning Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRestoreEarlyLateAdjust,
                "Restore Hit Time"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetEarlyLateAdjust,
                "Set Hit Time"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncEarlyLateAdjust,
                "Adjust Hit Time"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepEarlyLateAbs,
                "Sweep Hit Time Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepEarlyLateRel,
                "Sweep Hit Time Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRestoreDurationAdjust,
                "Restore Duration"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetDurationAdjust,
                "Set Duration"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncDurationAdjust,
                "Adjust Duration"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepDurationAbs,
                "Sweep Duration Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepDurationRel,
                "Sweep Duration Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdDurationAdjustMode,
                "Set Duration Mode"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetMeter,
                "Set Meter"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetMeasureNumber,
                "Set Measure Number"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetTranspose,
                "Set Transpose"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdAdjustTranspose,
                "Adjust Transpose"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetFrequencyValue,
                "Set Pitch"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetFrequencyValueLegacy,
                "Set Pitch (Legacy)"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdAdjustFrequencyValue,
                "Adjust Pitch"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdAdjustFrequencyValueLegacy,
                "Adjust Pitch (Legacy)"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdResetFrequencyValue,
                "Reset Pitch"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdLoadFrequencyModel,
                "Load Pitch Table"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue0Absolute,
                "Sweep Pitch 0 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue1Absolute,
                "Sweep Pitch 1 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue2Absolute,
                "Sweep Pitch 2 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue3Absolute,
                "Sweep Pitch 3 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue4Absolute,
                "Sweep Pitch 4 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue5Absolute,
                "Sweep Pitch 5 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue6Absolute,
                "Sweep Pitch 6 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue7Absolute,
                "Sweep Pitch 7 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue8Absolute,
                "Sweep Pitch 8 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue9Absolute,
                "Sweep Pitch 9 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue10Absolute,
                "Sweep Pitch 10 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue11Absolute,
                "Sweep Pitch 11 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue0Relative,
                "Sweep Pitch 0 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue1Relative,
                "Sweep Pitch 1 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue2Relative,
                "Sweep Pitch 2 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue3Relative,
                "Sweep Pitch 3 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue4Relative,
                "Sweep Pitch 4 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue5Relative,
                "Sweep Pitch 5 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue6Relative,
                "Sweep Pitch 6 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue7Relative,
                "Sweep Pitch 7 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue8Relative,
                "Sweep Pitch 8 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue9Relative,
                "Sweep Pitch 9 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue10Relative,
                "Sweep Pitch 10 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepFrequencyValue11Relative,
                "Sweep Pitch 11 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetEffectParam1,
                "Set Effect Accent 1"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncEffectParam1,
                "Adjust Effect Accent 1"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepEffectParamAbs1,
                "Sweep Effect Accent 1 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepEffectParamRel1,
                "Sweep Effect Accent 1 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetEffectParam2,
                "Set Effect Accent 2"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncEffectParam2,
                "Adjust Effect Accent 2"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepEffectParamAbs2,
                "Sweep Effect Accent 2 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepEffectParamRel2,
                "Sweep Effect Accent 2 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetEffectParam3,
                "Set Effect Accent 3"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncEffectParam3,
                "Adjust Effect Accent 3"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepEffectParamAbs3,
                "Sweep Effect Accent 3 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepEffectParamRel3,
                "Sweep Effect Accent 3 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetEffectParam4,
                "Set Effect Accent 4"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncEffectParam4,
                "Adjust Effect Accent 4"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepEffectParamAbs4,
                "Sweep Effect Accent 4 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepEffectParamRel4,
                "Sweep Effect Accent 4 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetEffectParam5,
                "Set Effect Accent 5"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncEffectParam5,
                "Adjust Effect Accent 5"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepEffectParamAbs5,
                "Sweep Effect Accent 5 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepEffectParamRel5,
                "Sweep Effect Accent 5 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetEffectParam6,
                "Set Effect Accent 6"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncEffectParam6,
                "Adjust Effect Accent 6"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepEffectParamAbs6,
                "Sweep Effect Accent 6 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepEffectParamRel6,
                "Sweep Effect Accent 6 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetEffectParam7,
                "Set Effect Accent 7"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncEffectParam7,
                "Adjust Effect Accent 7"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepEffectParamAbs7,
                "Sweep Effect Accent 7 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepEffectParamRel7,
                "Sweep Effect Accent 7 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetEffectParam8,
                "Set Effect Accent 8"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncEffectParam8,
                "Adjust Effect Accent 8"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepEffectParamAbs8,
                "Sweep Effect Accent 8 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepEffectParamRel8,
                "Sweep Effect Accent 8 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdTrackEffectEnable,
                "Track Effect Switch"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetScoreEffectParam1,
                "Set Global Score Effect Accent 1"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncScoreEffectParam1,
                "Adjust Global Score Effect Accent 1"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepScoreEffectParamAbs1,
                "Sweep Global Score Effect Accent 1 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepScoreEffectParamRel1,
                "Sweep Global Score Effect Accent 1 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetScoreEffectParam2,
                "Set Global Score Effect Accent 2"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncScoreEffectParam2,
                "Adjust Global Score Effect Accent 2"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepScoreEffectParamAbs2,
                "Sweep Global Score Effect Accent 2 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepScoreEffectParamRel2,
                "Sweep Global Score Effect Accent 2 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetScoreEffectParam3,
                "Set Global Score Effect Accent 3"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncScoreEffectParam3,
                "Adjust Global Score Effect Accent 3"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepScoreEffectParamAbs3,
                "Sweep Global Score Effect Accent 3 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepScoreEffectParamRel3,
                "Sweep Global Score Effect Accent 3 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetScoreEffectParam4,
                "Set Global Score Effect Accent 4"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncScoreEffectParam4,
                "Adjust Global Score Effect Accent 4"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepScoreEffectParamAbs4,
                "Sweep Global Score Effect Accent 4 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepScoreEffectParamRel4,
                "Sweep Global Score Effect Accent 4 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetScoreEffectParam5,
                "Set Global Score Effect Accent 5"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncScoreEffectParam5,
                "Adjust Global Score Effect Accent 5"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepScoreEffectParamAbs5,
                "Sweep Global Score Effect Accent 5 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepScoreEffectParamRel5,
                "Sweep Global Score Effect Accent 5 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetScoreEffectParam6,
                "Set Global Score Effect Accent 6"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncScoreEffectParam6,
                "Adjust Global Score Effect Accent 6"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepScoreEffectParamAbs6,
                "Sweep Global Score Effect Accent 6 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepScoreEffectParamRel6,
                "Sweep Global Score Effect Accent 6 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetScoreEffectParam7,
                "Set Global Score Effect Accent 7"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncScoreEffectParam7,
                "Adjust Global Score Effect Accent 7"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepScoreEffectParamAbs7,
                "Sweep Global Score Effect Accent 7 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepScoreEffectParamRel7,
                "Sweep Global Score Effect Accent 7 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetScoreEffectParam8,
                "Set Global Score Effect Accent 8"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncScoreEffectParam8,
                "Adjust Global Score Effect Accent 8"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepScoreEffectParamAbs8,
                "Sweep Global Score Effect Accent 8 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepScoreEffectParamRel8,
                "Sweep Global Score Effect Accent 8 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdMarker,
                "Marker"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSectionEffectEnable,
                "Section Effect Switch"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdScoreEffectEnable,
                "Score Effect Switch"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetSectionEffectParam1,
                "Set Section Effect Accent 1"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncSectionEffectParam1,
                "Adjust Section Effect Accent 1"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepSectionEffectParamAbs1,
                "Sweep Section Effect Accent 1 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepSectionEffectParamRel1,
                "Sweep Section Effect Accent 1 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetSectionEffectParam2,
                "Set Section Effect Accent 2"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncSectionEffectParam2,
                "Adjust Section Effect Accent 2"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepSectionEffectParamAbs2,
                "Sweep Section Effect Accent 2 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepSectionEffectParamRel2,
                "Sweep Section Effect Accent 2 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetSectionEffectParam3,
                "Set Section Effect Accent 3"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncSectionEffectParam3,
                "Adjust Section Effect Accent 3"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepSectionEffectParamAbs3,
                "Sweep Section Effect Accent 3 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepSectionEffectParamRel3,
                "Sweep Section Effect Accent 3 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetSectionEffectParam4,
                "Set Section Effect Accent 4"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncSectionEffectParam4,
                "Adjust Section Effect Accent 4"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepSectionEffectParamAbs4,
                "Sweep Section Effect Accent 4 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepSectionEffectParamRel4,
                "Sweep Section Effect Accent 4 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetSectionEffectParam5,
                "Set Section Effect Accent 5"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncSectionEffectParam5,
                "Adjust Section Effect Accent 5"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepSectionEffectParamAbs5,
                "Sweep Section Effect Accent 5 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepSectionEffectParamRel5,
                "Sweep Section Effect Accent 5 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetSectionEffectParam6,
                "Set Section Effect Accent 6"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncSectionEffectParam6,
                "Adjust Section Effect Accent 6"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepSectionEffectParamAbs6,
                "Sweep Section Effect Accent 6 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepSectionEffectParamRel6,
                "Sweep Section Effect Accent 6 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetSectionEffectParam7,
                "Set Section Effect Accent 7"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncSectionEffectParam7,
                "Adjust Section Effect Accent 7"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepSectionEffectParamAbs7,
                "Sweep Section Effect Accent 7 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepSectionEffectParamRel7,
                "Sweep Section Effect Accent 7 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetSectionEffectParam8,
                "Set Section Effect Accent 8"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncSectionEffectParam8,
                "Adjust Section Effect Accent 8"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepSectionEffectParamAbs8,
                "Sweep Section Effect Accent 8 Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepSectionEffectParamRel8,
                "Sweep Section Effect Accent 8 Relative"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSequenceBegin,
                "Sequence Begin"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSequenceEnd,
                "Sequence End"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetSequence,
                "Set Sequence"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetSequenceDeferred,
                "Set Sequence On Next Loop"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdEndSequencing,
                "End Sequencing"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSkip,
                "Skip"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIgnoreNextCmd,
                "Ignore Next Command"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRedirect,
                "Redirect Commands"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRedirectEnd,
                "End Redirect Commands"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdReleaseAll1,
                "Release 1 All"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdReleaseAll2,
                "Release 2 All"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdReleaseAll3,
                "Release 3 All"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdRestorePortamento,
                "Restore Portamento"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSetPortamento,
                "Set Portamento"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdIncPortamento,
                "Adjust Portamento"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepPortamentoAbs,
                "Sweep Portamento Absolute"),
            new KeyValuePair<NoteCommands, string>(NoteCommands.eCmdSweepPortamentoRel,
                "Sweep Portamento Relative")
        };

        /* get a static null terminated string literal containing the name of a command */
        private static bool fVerifiedCommandNames;
        public static string GetCommandName(NoteCommands Command)
        {
            string S = "[INTERNAL ERROR]";

            #region code correctness check
            /* verify structure */
            if (!fVerifiedCommandNames)
            {
                for (int j = 0; j < CommandNames.Length; j += 1)
                {
                    if (CommandNames[j].Key != j + NoteCommands.eCmd_Start)
                    {
                        // table inconsistency
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                }
                fVerifiedCommandNames = true;
            }
            #endregion

            /* direct index */
            int index = Command - NoteCommands.eCmd_Start;
            if ((index < 0) || (index >= CommandNames.Length))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            else
            {
                if (CommandNames[index].Key != Command)
                {
                    // table inconsistency
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
                S = CommandNames[index].Value;
            }

            return S;
        }
    }
}
