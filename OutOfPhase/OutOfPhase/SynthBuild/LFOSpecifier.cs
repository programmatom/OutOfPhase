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
using System.Text;

namespace OutOfPhase
{
    public static partial class Synthesizer
    {
        /* what kind of oscillator is the LFO */
        public enum LFOOscTypes
        {
            eLFOConstant1,
            eLFOSignedSine,
            eLFOPositiveSine,
            eLFOSignedTriangle,
            eLFOPositiveTriangle,
            eLFOSignedSquare,
            eLFOPositiveSquare,
            eLFOSignedRamp,
            eLFOPositiveRamp,
            eLFOSignedLinearFuzzTriangle,
            eLFOPositiveLinearFuzzTriangle,
            eLFOSignedLinearFuzzSquare,
            eLFOPositiveLinearFuzzSquare,
            eLFOWaveTable,
        }

        /* ways the LFO can be used to modulate the signal */
        public enum LFOModulationTypes
        {
            eLFOAdditive,
            eLFOMultiplicative,
            eLFOInverseMultiplicative,
        }

        /* this is the algorithm that will be used to combine the signal & modulator */
        public enum LFOAdderMode
        {
            eLFOArithmetic, /* add signals */
            eLFOGeometric, /* add base-2 logs of signals & exp */
            eLFOHalfSteps, /* add base-2 logs of signal and modulator/12 */
        }

        public class LFOSpecRec
        {
            /* envelope that determines the frequency of the LFO oscillator */
            public EnvelopeRec FrequencyEnvelope;

            /* list of LFOs applied to the frequency envelope output */
            public LFOListSpecRec FrequencyLFOList;

            /* envelope that determines the amplitude of the LFO wave */
            public EnvelopeRec AmplitudeEnvelope;

            /* list of LFOs applied to the amplitude envelope output */
            public LFOListSpecRec AmplitudeLFOList;

            /* envelope that determines wave table index -- wave table LFOs only */
            public EnvelopeRec WaveTableIndexEnvelope;

            /* list of LFOs applied to the wave table index envelope output */
            public LFOListSpecRec WaveTableIndexLFOList;

            /* envelope that determines the cutoff frequency for the lowpass filter */
            public EnvelopeRec FilterCutoffEnvelope;

            /* list of LFOs applied to the cutoff envelope output */
            public LFOListSpecRec FilterCutoffLFOList;

            /* envelope that determines the frequency of the sample/hold generator */
            public EnvelopeRec SampleHoldFreqEnvelope;

            /* list of LFOs applied to the sample/hold frequency envelope output */
            public LFOListSpecRec SampleHoldFreqLFOList;

            /* sample / wave table sources */
            public SampleSelectorRec SampleSources;

            /* what kind of wave generator should we use */
            public LFOOscTypes Oscillator;

            /* how should the LFO wave affect the stream being modulated */
            public LFOModulationTypes ModulationMode;

            /* how are the signals combined */
            public LFOAdderMode AddingMode;

            /* extra value */
            public double ExtraValue;

            /* flag remembering whether filter has been specified */
            public bool FilterSpecified;

            /* flag remembering whether sample & hold has been specified */
            public bool SampleHoldSpecified;
        }

        /* create a new LFO specification record */
        public static LFOSpecRec NewLFOSpecifier()
        {
            LFOSpecRec LFOSpec = new LFOSpecRec();

            LFOSpec.FrequencyEnvelope = NewEnvelope();
            LFOSpec.AmplitudeEnvelope = NewEnvelope();
            LFOSpec.WaveTableIndexEnvelope = NewEnvelope();
            LFOSpec.SampleSources = NewSampleSelectorList(0);
            LFOSpec.FrequencyLFOList = NewLFOListSpecifier();
            LFOSpec.AmplitudeLFOList = NewLFOListSpecifier();
            LFOSpec.WaveTableIndexLFOList = NewLFOListSpecifier();
            LFOSpec.FilterCutoffEnvelope = NewEnvelope();
            LFOSpec.FilterCutoffLFOList = NewLFOListSpecifier();
            LFOSpec.SampleHoldFreqEnvelope = NewEnvelope();
            LFOSpec.SampleHoldFreqLFOList = NewLFOListSpecifier();
            LFOSpec.Oscillator = LFOOscTypes.eLFOSignedSine;
            LFOSpec.ModulationMode = LFOModulationTypes.eLFOAdditive;
            LFOSpec.AddingMode = LFOAdderMode.eLFOArithmetic;
            LFOSpec.ExtraValue = 1;
            LFOSpec.FilterSpecified = false;
            LFOSpec.SampleHoldSpecified = false;

            return LFOSpec;
        }

        /* get the frequency envelope record */
        public static EnvelopeRec GetLFOSpecFrequencyEnvelope(LFOSpecRec LFOSpec)
        {
            return LFOSpec.FrequencyEnvelope;
        }

        /* get the frequency lfo list */
        public static LFOListSpecRec GetLFOSpecFrequencyLFOList(LFOSpecRec LFOSpec)
        {
            return LFOSpec.FrequencyLFOList;
        }

        /* get the amplitude envelope record */
        public static EnvelopeRec GetLFOSpecAmplitudeEnvelope(LFOSpecRec LFOSpec)
        {
            return LFOSpec.AmplitudeEnvelope;
        }

        /* get the amplitude lfo list */
        public static LFOListSpecRec GetLFOSpecAmplitudeLFOList(LFOSpecRec LFOSpec)
        {
            return LFOSpec.AmplitudeLFOList;
        }

        /* get the oscillator type for this LFO specifier */
        public static LFOOscTypes LFOSpecGetOscillatorType(LFOSpecRec LFOSpec)
        {
            return LFOSpec.Oscillator;
        }

        /* change the oscillator type */
        public static void SetLFOSpecOscillatorType(LFOSpecRec LFOSpec, LFOOscTypes NewType)
        {
            LFOSpec.Oscillator = NewType;
        }

        /* get the oscillator modulation mode */
        public static LFOModulationTypes LFOSpecGetModulationMode(LFOSpecRec LFOSpec)
        {
            return LFOSpec.ModulationMode;
        }

        /* change the oscillator modulation mode */
        public static void SetLFOSpecModulationMode(LFOSpecRec LFOSpec, LFOModulationTypes NewType)
        {
            LFOSpec.ModulationMode = NewType;
        }

        /* find out what the adding mode of the LFO is */
        public static LFOAdderMode LFOSpecGetAddingMode(LFOSpecRec LFOSpec)
        {
            return LFOSpec.AddingMode;
        }

        /* set a new adding mode for the LFO */
        public static void SetLFOSpecAddingMode(LFOSpecRec LFOSpec, LFOAdderMode NewAddingMode)
        {
            LFOSpec.AddingMode = NewAddingMode;
        }

        /* for wave table lfo oscillators only */
        public static EnvelopeRec GetLFOSpecWaveTableIndexEnvelope(LFOSpecRec LFOSpec)
        {
            return LFOSpec.WaveTableIndexEnvelope;
        }

        /* get wave table index lfo list, for wave table lfos only */
        public static LFOListSpecRec GetLFOSpecWaveTableIndexLFOList(LFOSpecRec LFOSpec)
        {
            return LFOSpec.WaveTableIndexLFOList;
        }

        /* get the sample selector list */
        public static SampleSelectorRec GetLFOSpecSampleSelector(LFOSpecRec LFOSpec)
        {
            return LFOSpec.SampleSources;
        }

        /* set the extra value */
        public static void SetLFOSpecExtraValue(LFOSpecRec LFOSpec, double Value)
        {
            LFOSpec.ExtraValue = Value;
        }

        /* get the extra value */
        public static double GetLFOSpecExtraValue(LFOSpecRec LFOSpec)
        {
            return LFOSpec.ExtraValue;
        }

        /* get the cutoff envelope for the lowpass filter */
        public static EnvelopeRec GetLFOSpecFilterCutoffEnvelope(LFOSpecRec LFOSpec)
        {
            return LFOSpec.FilterCutoffEnvelope;
        }

        /* get the cutoff lfo generator for the lowpass filter */
        public static LFOListSpecRec GetLFOSpecFilterCutoffLFOList(LFOSpecRec LFOSpec)
        {
            return LFOSpec.FilterCutoffLFOList;
        }

        /* indicate that the filter has been specified */
        public static void LFOSpecFilterHasBeenSpecified(LFOSpecRec LFOSpec)
        {
            LFOSpec.FilterSpecified = true;
        }

        /* inquire as to whether the filter has been specified */
        public static bool HasLFOSpecFilterBeenSpecified(LFOSpecRec LFOSpec)
        {
            return LFOSpec.FilterSpecified;
        }

        /* get the sample and hold update frequency */
        public static EnvelopeRec GetLFOSpecSampleHoldFreqEnvelope(LFOSpecRec LFOSpec)
        {
            return LFOSpec.SampleHoldFreqEnvelope;
        }

        /* get the sample and hold frequency LFO modulation list */
        public static LFOListSpecRec GetLFOSpecSampleHoldFreqLFOList(LFOSpecRec LFOSpec)
        {
            return LFOSpec.SampleHoldFreqLFOList;
        }

        /* indicate that the sample/hold processor has been specified */
        public static void LFOSpecSampleHoldHasBeenSpecified(LFOSpecRec LFOSpec)
        {
            LFOSpec.SampleHoldSpecified = true;
        }

        /* inquire as to whether the sample/hold processor has been specified */
        public static bool HasLFOSpecSampleHoldBeenSpecified(LFOSpecRec LFOSpec)
        {
            return LFOSpec.SampleHoldSpecified;
        }
    }
}
