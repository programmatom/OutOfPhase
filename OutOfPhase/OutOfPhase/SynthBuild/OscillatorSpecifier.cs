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
        /* what kind of oscillator is this */
        public enum OscillatorTypes
        {
            eOscillatorSampled, /* unstructured sample playback */
            eOscillatorWaveTable, /* indexed wave table (vector) synthesis */
            eOscillatorFOF, /* FOF pulse synthesis */
            eOscillatorAlgorithm, /* Algorithmically generated waveform */
            eOscillatorFMSynth, /* use phase modulation network */
            eOscillatorPluggable, // extensible interface
        }

        /* what happens when FOF pitch increases so that one grain isn't */
        /* finished when the next one occurs. */
        public enum OscFOFCompressType
        {
            eOscFOFOverlap, /* additive overlap previous grain with current */
            eOscFOFDiscard, /* discard previous grain */
        }

        /* what happens when FOF pitch decreases to leave gaps between grains */
        public enum OscFOFExpandType
        {
            eOscFOFSilenceFill, /* fill with silence */
            eOscFOFRestart, /* wrap to start of waveform and continue */
        }

        /* types of algorithmic oscillators */
        public enum OscAlgoType
        {
            eOscAlgoPulse, /* variable pulse wave */
            eOscAlgoRamp, /* variable between ramp and triangle wave */
        }

        public class OscillatorRec
        {
            /* what kind of oscillator is it */
            public OscillatorTypes OscillatorType;

            /* algorithmic oscillator type */
            public OscAlgoType Algorithm;

            /* mapping from pitch to sample */
            public SampleSelectorRec SampleIntervalList;

            /* this scales our output so that we can be set relative to other oscillators */
            public double OutputLoudness;

            /* these are used to make our frequency a multiple of the instrument's */
            /* overall frequency */
            public double FrequencyAdjustMultiplier;
            public int FrequencyAdjustDivisor;
            public double FrequencyAdjustAdder;

            /* this envelope determines the total output level with time */
            public EnvelopeRec LoudnessEnvelope;

            /* this LFO list modulates the output of the loudness envelope */
            public LFOListSpecRec LoudnessLFOList;

            /* this envelope determines the wave table selection index with time.  this is */
            /* only used for wave table synthesis, not for sampling */
            public EnvelopeRec ExcitationEnvelope;

            /* this LFO list modulates the output of the excitation envelope.  it is only */
            /* used for wave table synthesis. */
            public LFOListSpecRec ExcitationLFOList;

            // enable cross-wavetable interpolation; only used for wave table synthesis
            public bool EnableCrossWaveTableInterpolation;
            public bool EnableCrossWaveTableInterpolationExplicitlySet;

            /* stereo bias -- fixed amount to move this oscillator left or right by */
            public double StereoBias;
            /* surround bias -- fixed amount to move this oscillator front or back by */
            public double SurroundBias;

            /* time displacement -- how much earlier / later to start sample */
            public double TimeDisplacement;

            /* list of LFO operators on the frequency for this oscillator */
            public LFOListSpecRec FrequencyLFOList;

            /* list of effects applied to oscillator */
            public EffectSpecListRec EffectSpecList;

            /* FOF compression mode */
            public OscFOFCompressType FOFCompression;
            /* FOF expansion mode */
            public OscFOFExpandType FOFExpansion;

            /* FOF sampling rate for waveform */
            public double FOFSamplingRate;

            /* envelope generator controlling FOF sampling rate */
            public EnvelopeRec FOFSamplingRateEnvelope;
            /* LFO generator controlling FOF sampling rate */
            public LFOListSpecRec FOFSamplingRateLFOList;

            /* network definition for FM synth */
            public FMSynthSpecRec FMSynthSpec;

            // definition for ploggable oscillator -- overrides all parameters here
            public PluggableOscSpec PluggableSpec;
        }

        /* create a new oscillator public static ure */
        public static OscillatorRec NewOscillatorSpecifier()
        {
            OscillatorRec Osc = new OscillatorRec();

            Osc.SampleIntervalList = NewSampleSelectorList(0);
            Osc.LoudnessEnvelope = NewEnvelope();
            Osc.LoudnessLFOList = NewLFOListSpecifier();
            Osc.ExcitationEnvelope = NewEnvelope();
            Osc.ExcitationLFOList = NewLFOListSpecifier();
            Osc.FrequencyLFOList = NewLFOListSpecifier();
            Osc.EffectSpecList = NewEffectSpecList();
            Osc.FOFSamplingRateEnvelope = NewEnvelope();
            Osc.FOFSamplingRateLFOList = NewLFOListSpecifier();
            Osc.FMSynthSpec = NewFMSynthSpec();

            Osc.OscillatorType = OscillatorTypes.eOscillatorSampled; /* default -- this is kinda ugly */
            Osc.OutputLoudness = 1;
            Osc.FrequencyAdjustMultiplier = 1;
            Osc.FrequencyAdjustDivisor = 1;
            Osc.FrequencyAdjustAdder = 0;
            Osc.StereoBias = 0;
            Osc.TimeDisplacement = 0;
            Osc.FOFCompression = OscFOFCompressType.eOscFOFOverlap;
            Osc.FOFExpansion = OscFOFExpandType.eOscFOFSilenceFill;
            Osc.FOFSamplingRate = 0;
            Osc.Algorithm = OscAlgoType.eOscAlgoPulse;
            Osc.EnableCrossWaveTableInterpolation = true;

            return Osc;
        }

        /* set the oscillator type */
        public static void OscillatorSetTheType(
            OscillatorRec Osc,
            OscillatorTypes WhatKindOfOscillator)
        {
#if DEBUG
            if ((WhatKindOfOscillator != OscillatorTypes.eOscillatorSampled)
                && (WhatKindOfOscillator != OscillatorTypes.eOscillatorWaveTable)
                && (WhatKindOfOscillator != OscillatorTypes.eOscillatorFOF)
                && (WhatKindOfOscillator != OscillatorTypes.eOscillatorAlgorithm)
                && (WhatKindOfOscillator != OscillatorTypes.eOscillatorFMSynth)
                && (WhatKindOfOscillator != OscillatorTypes.eOscillatorPluggable))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Osc.OscillatorType = WhatKindOfOscillator;
        }

        /* find out what kind of oscillator this is */
        public static OscillatorTypes OscillatorGetWhatKindItIs(OscillatorRec Osc)
        {
            return Osc.OscillatorType;
        }

        /* get the pitch interval -. sample mapping */
        public static SampleSelectorRec OscillatorGetSampleIntervalList(OscillatorRec Osc)
        {
            return Osc.SampleIntervalList;
        }

        /* get the output loudness of the oscillator */
        public static double OscillatorGetOutputLoudness(OscillatorRec Osc)
        {
            return Osc.OutputLoudness;
        }

        /* put a new output loudness in for the oscillator */
        public static void PutOscillatorNewOutputLoudness(
            OscillatorRec Osc,
            double NewOutputLevel)
        {
            Osc.OutputLoudness = NewOutputLevel;
        }

        /* get the frequency multiplier factor */
        public static double OscillatorGetFrequencyMultiplier(OscillatorRec Osc)
        {
            return Osc.FrequencyAdjustMultiplier;
        }

        /* get the frequency divisor integer */
        public static int OscillatorGetFrequencyDivisor(OscillatorRec Osc)
        {
            return Osc.FrequencyAdjustDivisor;
        }

        /* get the frequency adder thing */
        public static double OscillatorGetFrequencyAdder(OscillatorRec Osc)
        {
            return Osc.FrequencyAdjustAdder;
        }

        /* change the frequency adjust factors */
        public static void PutOscillatorNewFrequencyFactors(
            OscillatorRec Osc,
            double NewMultipler,
            int NewDivisor)
        {
            Osc.FrequencyAdjustMultiplier = NewMultipler;
            Osc.FrequencyAdjustDivisor = NewDivisor;
        }

        /* put a new frequency adder value */
        public static void PutOscillatorFrequencyAdder(
            OscillatorRec Osc,
            double NewAdder)
        {
            Osc.FrequencyAdjustAdder = NewAdder;
        }

        /* get the loudness envelope for the oscillator */
        public static EnvelopeRec OscillatorGetLoudnessEnvelope(OscillatorRec Osc)
        {
            return Osc.LoudnessEnvelope;
        }

        /* get the list of LFO oscillators modulating the loudness envelope output */
        public static LFOListSpecRec OscillatorGetLoudnessLFOList(OscillatorRec Osc)
        {
            return Osc.LoudnessLFOList;
        }

        /* get the excitation envelope for the oscillator */
        public static EnvelopeRec OscillatorGetExcitationEnvelope(OscillatorRec Osc)
        {
            return Osc.ExcitationEnvelope;
        }

        /* get the list of LFO oscillators modulating the excitation envelope output */
        public static LFOListSpecRec OscillatorGetExcitationLFOList(OscillatorRec Osc)
        {
            return Osc.ExcitationLFOList;
        }

        public static bool OscillatorGetEnableCrossWaveTableInterpolation(OscillatorRec Osc)
        {
            return Osc.EnableCrossWaveTableInterpolation;
        }

        public static bool OscillatorGetEnableCrossWaveTableInterpolationExplicitlySet(OscillatorRec Osc)
        {
            return Osc.EnableCrossWaveTableInterpolationExplicitlySet;
        }

        public static void OscillatorSetEnableCrossWaveTableInterpolation(OscillatorRec Osc, bool enable)
        {
            Osc.EnableCrossWaveTableInterpolation = enable;
            Osc.EnableCrossWaveTableInterpolationExplicitlySet = true;
        }

        /* get the stereo bias factor */
        public static double OscillatorGetStereoBias(OscillatorRec Osc)
        {
            return Osc.StereoBias;
        }

        /* put a new value for the stereo bias factor */
        public static void OscillatorPutStereoBias(
            OscillatorRec Osc,
            double NewStereoBias)
        {
            Osc.StereoBias = NewStereoBias;
        }

        /* get the surround bias factor */
        public static double OscillatorGetSurroundBias(OscillatorRec Osc)
        {
            return Osc.SurroundBias;
        }

        /* put a new value for the surround bias factor */
        public static void OscillatorPutSurroundBias(
            OscillatorRec Osc,
            double NewSurroundBias)
        {
            Osc.SurroundBias = NewSurroundBias;
        }

        /* get the time displacement factor */
        public static double OscillatorGetTimeDisplacement(OscillatorRec Osc)
        {
            return Osc.TimeDisplacement;
        }

        /* put a new value for the time displacement factor */
        public static void OscillatorPutTimeDisplacement(
            OscillatorRec Osc,
            double NewTimeDisplacement)
        {
            Osc.TimeDisplacement = NewTimeDisplacement;
        }

        /* get the oscillator's frequency LFO list */
        public static LFOListSpecRec GetOscillatorFrequencyLFOList(OscillatorRec Osc)
        {
            return Osc.FrequencyLFOList;
        }

        /* get the effect list for the oscillator */
        public static EffectSpecListRec GetOscillatorEffectList(OscillatorRec Osc)
        {
            return Osc.EffectSpecList;
        }

        /* set the oscillator's FOF compression mode */
        public static void PutOscillatorFOFCompress(
            OscillatorRec Osc,
            OscFOFCompressType FOFCompressMode)
        {
#if DEBUG
            if ((FOFCompressMode != OscFOFCompressType.eOscFOFDiscard) && (FOFCompressMode != OscFOFCompressType.eOscFOFOverlap))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Osc.FOFCompression = FOFCompressMode;
        }


        /* get the oscillator's FOF compression mode */
        public static OscFOFCompressType GetOscillatorFOFCompress(OscillatorRec Osc)
        {
            return Osc.FOFCompression;
        }

        /* set the oscillator's FOF expansion mode */
        public static void PutOscillatorFOFExpand(
            OscillatorRec Osc,
            OscFOFExpandType FOFExpandMode)
        {
#if DEBUG
            if ((FOFExpandMode != OscFOFExpandType.eOscFOFSilenceFill)
                && (FOFExpandMode != OscFOFExpandType.eOscFOFRestart))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Osc.FOFExpansion = FOFExpandMode;
        }

        /* get the oscillator's FOF expansion mode */
        public static OscFOFExpandType GetOscillatorFOFExpand(OscillatorRec Osc)
        {
            return Osc.FOFExpansion;
        }

        /* set the oscillator's algorithm */
        public static void PutOscillatorAlgorithm(
            OscillatorRec Osc,
            OscAlgoType Algorithm)
        {
#if DEBUG
            if (Osc.OscillatorType != OscillatorTypes.eOscillatorAlgorithm)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if ((Algorithm != OscAlgoType.eOscAlgoPulse) && (Algorithm != OscAlgoType.eOscAlgoRamp))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Osc.Algorithm = Algorithm;
        }

        /* get the oscillator's algorithm */
        public static OscAlgoType GetOscillatorAlgorithm(OscillatorRec Osc)
        {
            return Osc.Algorithm;
        }

        /* set the oscillator's FOF sampling rate */
        public static void PutOscillatorFOFSamplingRate(
            OscillatorRec Osc,
            double SamplingRate)
        {
            Osc.FOFSamplingRate = SamplingRate;
        }

        /* get the oscillator's FOF sampling rate */
        public static double GetOscillatorFOFSamplingRate(OscillatorRec Osc)
        {
            return Osc.FOFSamplingRate;
        }

        /* get the FOF sampling rate envelope for the oscillator */
        public static EnvelopeRec OscillatorGetFOFRateEnvelope(OscillatorRec Osc)
        {
            return Osc.FOFSamplingRateEnvelope;
        }

        /* get the list of LFO oscillators modulating the FOF sampling rate envelope output */
        public static LFOListSpecRec OscillatorGetFOFRateLFOList(OscillatorRec Osc)
        {
            return Osc.FOFSamplingRateLFOList;
        }

        /* get the FM synth spec */
        public static FMSynthSpecRec OscillatorGetFMSynthSpec(OscillatorRec Osc)
        {
#if DEBUG
            if (Osc.OscillatorType != OscillatorTypes.eOscillatorFMSynth)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return Osc.FMSynthSpec;
        }

        public static void PutOscillatorPluggableSpec(
            OscillatorRec Osc,
            PluggableOscSpec pluggableSpec)
        {
#if DEBUG
            if (Osc.OscillatorType != OscillatorTypes.eOscillatorPluggable)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Osc.PluggableSpec = pluggableSpec;
        }

        public static PluggableOscSpec GetOscillatorPluggableSpec(OscillatorRec Osc)
        {
#if DEBUG
            if (Osc.OscillatorType != OscillatorTypes.eOscillatorPluggable)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return Osc.PluggableSpec;
        }
    }
}
