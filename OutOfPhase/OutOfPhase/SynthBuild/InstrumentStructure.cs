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
        public class InstrumentRec
        {
            /* this is the overall loudness factor for the instrument, which can be used */
            /* to differentiate "loud" on a quite instrument vs. "loud" on a loud instrument. */
            public double OverallLoudness;

            /* list of LFO operators on the frequency */
            public LFOListSpecRec FrequencyLFOList;

            /* list of oscillators */
            public OscillatorListRec OscillatorList;

            /* list of track effects */
            public EffectSpecListRec TrackEffectSpecList;

            /* list of combined oscillator effects */
            public EffectSpecListRec CombinedOscEffectSpecList;
        }

        /* create a new instrument specification record */
        public static InstrumentRec NewInstrumentSpecifier()
        {
            InstrumentRec Instr = new InstrumentRec();

            Instr.OverallLoudness = 1;
            Instr.FrequencyLFOList = NewLFOListSpecifier();
            Instr.OscillatorList = NewOscillatorListSpecifier();
            Instr.TrackEffectSpecList = NewEffectSpecList();
            Instr.CombinedOscEffectSpecList = NewEffectSpecList();

            return Instr;
        }

        /* get the overall loudness of the instrument */
        public static double GetInstrumentOverallLoudness(InstrumentRec Instr)
        {
            return Instr.OverallLoudness;
        }

        /* put a new value for overall loudness */
        public static void InstrumentSetOverallLoudness(InstrumentRec Instr, double NewLoudness)
        {
            Instr.OverallLoudness = NewLoudness;
        }

        /* get the instrument's frequency LFO list */
        public static LFOListSpecRec GetInstrumentFrequencyLFOList(InstrumentRec Instr)
        {
            return Instr.FrequencyLFOList;
        }

        /* get the instrument's oscillator list */
        public static OscillatorListRec GetInstrumentOscillatorList(InstrumentRec Instr)
        {
            return Instr.OscillatorList;
        }

        /* get the track effect specifier list */
        public static EffectSpecListRec GetInstrumentEffectSpecList(InstrumentRec Instr)
        {
            return Instr.TrackEffectSpecList;
        }

        /* get the combined oscillator effect specifier list */
        public static EffectSpecListRec GetInstrumentCombinedOscEffectSpecList(InstrumentRec Instr)
        {
            return Instr.CombinedOscEffectSpecList;
        }
    }
}
