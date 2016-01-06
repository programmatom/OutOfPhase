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
        public class ResamplerRec : ITrackEffect, IOscillatorEffect
        {
            public double EffectSamplingRate; /* output sampling rate */
            public ResampleMethodType ResampMethod; /* how input is mangled */
            public ResampOutType OutputMethod; /* how output is produced */

            public double CurrentPhase; /* where we are now */
            public double PhaseDifferential; /* increment per sampling period */
            public float Left0Value; /* left side of interpolation */
            public float Left1Value; /* right side of interpolation */
            public float Right0Value; /* left side of interpolation */
            public float Right1Value; /* right side of interpolation */
            public float OldLeft; /* last left sample from last time */
            public float OldRight; /* last left sample from last time */


            /* create a new resampling processor */
            public static ResamplerRec NewResampler(
                ResamplerSpecRec Template,
                SynthParamRec SynthParams)
            {
                ResamplerRec Resampler = new ResamplerRec();

                Resampler.EffectSamplingRate = GetResampleRate(Template);
                Resampler.ResampMethod = GetResamplingMethod(Template);
                Resampler.OutputMethod = GetResampleOutputMethod(Template);
                //Resampler.CurrentPhase = 0;
                Resampler.PhaseDifferential = Resampler.EffectSamplingRate / SynthParams.dSamplingRate;
                //Resampler.Left0Value = 0;
                //Resampler.Left1Value = 0;
                //Resampler.Right0Value = 0;
                //Resampler.Right1Value = 0;
                //Resampler.OldLeft = 0;
                //Resampler.OldRight = 0;

                return Resampler;
            }

            public void TrackUpdateState(ref AccentRec Accents, SynthParamRec SynthParams)
            {
            }

            public void Finalize(
                SynthParamRec SynthParams,
                bool writeOutputLogs)
            {
            }

            public void OscFixEnvelopeOrigins(int ActualPreOriginTime)
            {
            }

            public void OscUpdateEnvelopes(double OscillatorFrequency, SynthParamRec SynthParams)
            {
            }

            public void OscKeyUpSustain1()
            {
            }

            public void OscKeyUpSustain2()
            {
            }

            public void OscKeyUpSustain3()
            {
            }

            public void OscRetriggerEnvelopes(ref AccentRec NewAccents, double NewHurryUp, double NewInitialFrequency, bool ActuallyRetrigger, SynthParamRec SynthParams)
            {
            }

            /* apply resampler to some stuff */
            public SynthErrorCodes Apply(
                float[] workspace,
                int lOffset,
                int rOffset,
                int nActualFrames,
                SynthParamRec SynthParams)
            {
                if (this.EffectSamplingRate < SynthParams.dSamplingRate)
                {
                    double CurrentPhase;
                    double PhaseDifferential;
                    float Left0Value;
                    float Left1Value;
                    float Right0Value;
                    float Right1Value;
                    float CurrentLeft;
                    float CurrentRight;

                    CurrentPhase = this.CurrentPhase;
                    PhaseDifferential = this.PhaseDifferential;
                    Left0Value = this.Left0Value;
                    Left1Value = this.Left1Value;
                    Right0Value = this.Right0Value;
                    Right1Value = this.Right1Value;
                    CurrentLeft = this.OldLeft;
                    CurrentRight = this.OldRight;
                    switch (this.ResampMethod)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();

                        case ResampleMethodType.eResampleTruncating:
                            /* truncation */
                            switch (this.OutputMethod)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new ArgumentException();

                                case ResampOutType.eResampleRectangular:
                                    /* truncation rectangular */
                                    for (int Scan = 0; Scan < nActualFrames; Scan += 1)
                                    {
                                        CurrentPhase += PhaseDifferential;
                                        if (CurrentPhase >= 1)
                                        {
                                            Left0Value = Left1Value;
                                            Left1Value = workspace[Scan + lOffset];
                                            Right0Value = Right1Value;
                                            Right1Value = workspace[Scan + rOffset];
                                            CurrentPhase = CurrentPhase - (int)CurrentPhase;
                                        }
                                        workspace[Scan + lOffset] = Left1Value;
                                        workspace[Scan + rOffset] = Right1Value;
                                    }
                                    break;

                                case ResampOutType.eResampleTriangular:
                                    /* truncation triangular */
                                    for (int Scan = 0; Scan < nActualFrames; Scan += 1)
                                    {
                                        CurrentPhase += PhaseDifferential;
                                        if (CurrentPhase >= 1)
                                        {
                                            Left0Value = Left1Value;
                                            Left1Value = workspace[Scan + lOffset];
                                            Right0Value = Right1Value;
                                            Right1Value = workspace[Scan + rOffset];
                                            CurrentPhase = CurrentPhase - (int)CurrentPhase;
                                        }
                                        workspace[Scan + lOffset] = (float)(Left0Value
                                            + (CurrentPhase * (Left1Value - Left0Value)));
                                        workspace[Scan + rOffset] = (float)(Right0Value
                                            + (CurrentPhase * (Right1Value - Right0Value)));
                                    }
                                    break;
                            }
                            break;

                        case ResampleMethodType.eResampleLinearInterpolation:
                            /* interpolation */
                            /* |   |   |   |   |   |   |   |   |   |   |   |   |   |   |   | */
                            /* 1            2            3            4            5         */
                            /* 'interpolated square' sequence  */
                            /* 0   1   1   1   2   2   2   3   3   3   4   4   4   4   5   5 */
                            /* the values derived from interpolated values from 1..5 are */
                            /* generated after the values are available. */
                            /* when we go past 1, the value at 0 is previous (to the left), */
                            /* and the value at 1 is the current (to the right). */
                            switch (this.OutputMethod)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new ArgumentException();

                                case ResampOutType.eResampleRectangular:
                                    /* interpolation rectangular */
                                    for (int Scan = 0; Scan < nActualFrames; Scan += 1)
                                    {
                                        double PreviousPhase;
                                        float PreviousLeft;
                                        float PreviousRight;

                                        PreviousPhase = CurrentPhase;
                                        CurrentPhase += PhaseDifferential;
                                        PreviousLeft = CurrentLeft; /* advance interpolation pair */
                                        CurrentLeft = workspace[Scan + lOffset];
                                        PreviousRight = CurrentRight;
                                        CurrentRight = workspace[Scan + rOffset];
                                        if (CurrentPhase >= 1)
                                        {
                                            Left0Value = Left1Value;
                                            Right0Value = Right1Value;
                                            Left1Value = (float)(PreviousLeft
                                                + ((1 - PreviousPhase) / PhaseDifferential)
                                                * (CurrentLeft - PreviousLeft));
                                            Right1Value = (float)(PreviousRight
                                                + ((1 - PreviousPhase) / PhaseDifferential)
                                                * (CurrentRight - PreviousRight));
                                            CurrentPhase = CurrentPhase - (int)CurrentPhase;
                                        }
                                        workspace[Scan + lOffset] = Left1Value;
                                        workspace[Scan + rOffset] = Right1Value;
                                    }
                                    break;

                                case ResampOutType.eResampleTriangular:
                                    /* interpolation triangular */
                                    for (int Scan = 0; Scan < nActualFrames; Scan += 1)
                                    {
                                        double PreviousPhase;
                                        float PreviousLeft;
                                        float PreviousRight;

                                        PreviousPhase = CurrentPhase;
                                        CurrentPhase += PhaseDifferential;
                                        PreviousLeft = CurrentLeft; /* advance interpolation pair */
                                        CurrentLeft = workspace[Scan + lOffset];
                                        PreviousRight = CurrentRight;
                                        CurrentRight = workspace[Scan + rOffset];
                                        if (CurrentPhase >= 1)
                                        {
                                            Left0Value = Left1Value;
                                            Right0Value = Right1Value;
                                            Left1Value = (float)(PreviousLeft
                                                + ((1 - PreviousPhase) / PhaseDifferential)
                                                * (CurrentLeft - PreviousLeft));
                                            Right1Value = (float)(PreviousRight
                                                + ((1 - PreviousPhase) / PhaseDifferential)
                                                * (CurrentRight - PreviousRight));
                                            CurrentPhase = CurrentPhase - (int)CurrentPhase;
                                        }
                                        workspace[Scan + lOffset] = (float)(Left0Value
                                            + (CurrentPhase * (Left1Value - Left0Value)));
                                        workspace[Scan + rOffset] = (float)(Right0Value
                                            + (CurrentPhase * (Right1Value - Right0Value)));
                                    }
                                    break;
                            }
                            break;
                    }
                    this.CurrentPhase = CurrentPhase;
                    this.PhaseDifferential = PhaseDifferential;
                    this.Left0Value = Left0Value;
                    this.Left1Value = Left1Value;
                    this.Right0Value = Right0Value;
                    this.Right1Value = Right1Value;
                    this.OldLeft = CurrentLeft;
                    this.OldRight = CurrentRight;
                }

                return SynthErrorCodes.eSynthDone;
            }
        }
    }
}
