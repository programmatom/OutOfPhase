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
    public static partial class Synthesizer
    {
        public interface IEffect
        {
            SynthErrorCodes Apply(
                float[] workspace,
                int lOffset,
                int rOffset,
                int nActualFrames,
                SynthParamRec SynthParams);

            void Finalize(
                SynthParamRec SynthParams,
                bool writeOutputLogs);
        }

        public interface ITrackEffect : IEffect
        {
            void TrackUpdateState(
                ref AccentRec Accents,
                SynthParamRec SynthParams);
        }

        public interface IOscillatorEffect : IEffect
        {
            void OscFixEnvelopeOrigins(
                int ActualPreOriginTime);

            void OscUpdateEnvelopes(
                double OscillatorFrequency,
                SynthParamRec SynthParams);

            void OscKeyUpSustain1();
            void OscKeyUpSustain2();
            void OscKeyUpSustain3();

            void OscRetriggerEnvelopes(
                ref AccentRec NewAccents,
                double NewHurryUp,
                double NewInitialFrequency,
                bool ActuallyRetrigger,
                SynthParamRec SynthParams);
        }
    }
}
