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
        public class ResonantLowpassRec : IFilter
        {
            /* old stuff */
            public double OldCutoff = -1e300;
            public double OldBandwidth = -1e300;
            public double OldGain = -1e300;

            /* vector size and offset information */
            public int NumLowpassSections;
            public int NumBandpassSections;

            /* resonance gain */
            public float BandpassGain;

            /* vector of iir states.  lowpass sections come first */
            public IIR2DirectIRec[] iir;


            public FilterTypes FilterType { get { return FilterTypes.eFilterResonantLowpass; } }

            public void Init(
                int LowpassOrder,
                int BandpassOrder,
                SynthParamRec SynthParams)
            {
                this.OldCutoff = -1e300;
                this.OldBandwidth = -1e300;
                this.OldGain = -1e300;

#if DEBUG
                if ((LowpassOrder < 0) || (LowpassOrder % 2 != 0))
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
                if ((BandpassOrder < 0) || (BandpassOrder % 2 != 0))
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif

                this.NumLowpassSections = LowpassOrder / 2;
                this.NumBandpassSections = BandpassOrder / 2;

                /* allocate iir workspace */
                this.iir = New(ref SynthParams.freelists.iir2DirectIFreeList, this.NumLowpassSections + this.NumBandpassSections);
            }

            public void Dispose(
                SynthParamRec SynthParams)
            {
                Free(ref SynthParams.freelists.iir2DirectIFreeList, ref this.iir);
            }

            public static void SetResonantLowpassCoefficients(
                ResonantLowpassRec Filter,
                double Cutoff,
                double Bandwidth,
                double Gain,
                double SamplingRate)
            {
                if ((Cutoff == Filter.OldCutoff) && (Bandwidth == Filter.OldBandwidth) && (Gain == Filter.OldGain))
                {
                    return;
                }
                Filter.OldCutoff = Cutoff;
                Filter.OldBandwidth = Bandwidth;
                Filter.OldGain = Gain;

                if (Filter.NumLowpassSections != 0)
                {
                    /* initialize first one */
                    ButterworthLowpassRec.ComputeButterworthLowpassCoefficients(
                        ref Filter.iir[0],
                        Cutoff,
                        SamplingRate);

                    /* copy to the others */
                    for (int i = 1; i < Filter.NumLowpassSections; i += 1)
                    {
                        Filter.iir[i].A0 = Filter.iir[0].A0;
                        Filter.iir[i].A1 = Filter.iir[0].A1;
                        Filter.iir[i].A2 = Filter.iir[0].A2;
                        Filter.iir[i].B1 = Filter.iir[0].B1;
                        Filter.iir[i].B2 = Filter.iir[0].B2;
                    }
                }

                if (Filter.NumBandpassSections != 0)
                {
                    /* initialize first one */
                    ButterworthBandpassRec.ComputeButterworthBandpassCoefficients(
                        ref Filter.iir[Filter.NumLowpassSections + 0],
                        Cutoff,
                        Bandwidth,
                        SamplingRate);

                    /* copy to the others */
                    for (int i = 1; i < Filter.NumBandpassSections; i += 1)
                    {
                        Filter.iir[Filter.NumLowpassSections + i].A0 = Filter.iir[Filter.NumLowpassSections + 0].A0;
                        Filter.iir[Filter.NumLowpassSections + i].A1 = Filter.iir[Filter.NumLowpassSections + 0].A1;
                        Filter.iir[Filter.NumLowpassSections + i].A2 = Filter.iir[Filter.NumLowpassSections + 0].A2;
                        Filter.iir[Filter.NumLowpassSections + i].B1 = Filter.iir[Filter.NumLowpassSections + 0].B1;
                        Filter.iir[Filter.NumLowpassSections + i].B2 = Filter.iir[Filter.NumLowpassSections + 0].B2;
                    }
                }

                Filter.BandpassGain = (float)Gain;
            }

            public void UpdateParams(
                ref FilterParams Params)
            {
                SetResonantLowpassCoefficients(
                    this,
                    Params.Cutoff,
                    Params.BandwidthOrSlope,
                    Params.Gain,
                    Params.SamplingRate);
            }

            /* apply filter to an array of values, adding result to output array */
            public void Apply(
                float[] XinVector,
                int XInVectorOffset,
                float[] YoutVector,
                int YoutVectorOffset,
                int VectorLength,
                float OutputScaling,
                SynthParamRec SynthParams)
            {
                ResonantLowpassRec Filter = this;

                // ScratchWorkspace1* is used in ApplyUnifiedFilterArray()
#if DEBUG
                Debug.Assert(!SynthParams.ScratchWorkspace2InUse);
                SynthParams.ScratchWorkspace2InUse = true;
#endif
                float[] ScratchVector1 = XinVector/*sic*/; // SourceCopy
                int ScratchVector1Offset = SynthParams.ScratchWorkspace2LOffset;
                float[] ScratchVector2 = XinVector/*sic*/; // Workspace
                int ScratchVector2Offset = SynthParams.ScratchWorkspace2ROffset;

                FloatVectorCopy(
                    XinVector,
                    XInVectorOffset,
                    ScratchVector1, // SourceCopy
                    ScratchVector1Offset,
                    VectorLength);

                FloatVectorZero(
                    YoutVector,
                    YoutVectorOffset,
                    VectorLength);

                /* lowpass section */
                if (Filter.NumLowpassSections == 0)
                {
                }
                else if (Filter.NumLowpassSections == 1)
                {
                    IIR2DirectIMAcc(
                        ref Filter.iir[0],
                        ScratchVector1, // SourceCopy
                        ScratchVector1Offset,
                        YoutVector,
                        YoutVectorOffset,
                        VectorLength,
                        OutputScaling);
                }
                else
                {
                    IIR2DirectI(
                        ref Filter.iir[0],
                        ScratchVector1, // SourceCopy
                        ScratchVector1Offset,
                        ScratchVector2, // Workspace
                        ScratchVector2Offset,
                        VectorLength);
                    int i;
                    for (i = 1; i < Filter.NumLowpassSections - 1; i += 1)
                    {
                        IIR2DirectI(
                            ref Filter.iir[i],
                            ScratchVector2, // Workspace
                            ScratchVector2Offset,
                            ScratchVector2, // Workspace
                            ScratchVector2Offset,
                            VectorLength);
                    }
                    IIR2DirectIMAcc(
                        ref Filter.iir[i],
                        ScratchVector2, // Workspace
                        ScratchVector2Offset,
                        YoutVector,
                        YoutVectorOffset,
                        VectorLength,
                        OutputScaling);
                }

                /* bandpass section */
                if (Filter.NumBandpassSections == 0)
                {
                }
                else if (Filter.NumBandpassSections == 1)
                {
                    IIR2DirectIMAcc(
                        ref Filter.iir[Filter.NumLowpassSections + 0],
                        ScratchVector1, // SourceCopy
                        ScratchVector1Offset,
                        YoutVector,
                        YoutVectorOffset,
                        VectorLength,
                        Filter.BandpassGain * OutputScaling);
                }
                else
                {
                    IIR2DirectI(
                        ref Filter.iir[Filter.NumLowpassSections + 0],
                        ScratchVector1, // SourceCopy
                        ScratchVector1Offset,
                        ScratchVector2, // Workspace
                        ScratchVector2Offset,
                        VectorLength);
                    int i;
                    for (i = 1; i < Filter.NumBandpassSections - 1; i += 1)
                    {
                        IIR2DirectI(
                            ref Filter.iir[Filter.NumLowpassSections + i],
                            ScratchVector2, // Workspace
                            ScratchVector2Offset,
                            ScratchVector2, // Workspace
                            ScratchVector2Offset,
                            VectorLength);
                    }
                    IIR2DirectIMAcc(
                        ref Filter.iir[Filter.NumLowpassSections + i],
                        ScratchVector2, // Workspace
                        ScratchVector2Offset,
                        YoutVector,
                        YoutVectorOffset,
                        VectorLength,
                        Filter.BandpassGain * OutputScaling);
                }

#if DEBUG
                SynthParams.ScratchWorkspace2InUse = false;
#endif
            }
        }
    }
}
