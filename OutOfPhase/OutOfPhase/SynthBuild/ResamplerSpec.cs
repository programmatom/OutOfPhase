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
        /* method of resampling */
        public enum ResampleMethodType
        {
            eResampleTruncating, /* no interpolation -- rectangular */
            eResampleLinearInterpolation, /* use linear interpolation */
        }

        /* method of output */
        public enum ResampOutType
        {
            eResampleRectangular, /* sample and hold */
            eResampleTriangular, /* interpolation */
        }

        public class ResamplerSpecRec
        {
            public double SamplingRate; /* output sampling rate */
            public ResampleMethodType ResampMethod; /* how input is mangled */
            public ResampOutType OutputMethod; /* how output is produced */
        }

        /* create a new resampler specifier */
        public static ResamplerSpecRec NewResamplerSpecifier(
                                                    ResampleMethodType ResampleMethod,
                                                    ResampOutType OutputMethod,
                                                    double SamplingRate)
        {
#if DEBUG
            if ((ResampleMethod != ResampleMethodType.eResampleTruncating)
                && (ResampleMethod != ResampleMethodType.eResampleLinearInterpolation))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if ((OutputMethod != ResampOutType.eResampleRectangular)
                && (OutputMethod != ResampOutType.eResampleTriangular))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif

            ResamplerSpecRec ResamplerSpec = new ResamplerSpecRec();

            ResamplerSpec.SamplingRate = SamplingRate;
            ResamplerSpec.ResampMethod = ResampleMethod;
            ResamplerSpec.OutputMethod = OutputMethod;

            return ResamplerSpec;
        }

        /* get resampling method */
        public static ResampleMethodType GetResamplingMethod(ResamplerSpecRec ResamplerSpec)
        {
            return ResamplerSpec.ResampMethod;
        }

        /* get output generation method */
        public static ResampOutType GetResampleOutputMethod(ResamplerSpecRec ResamplerSpec)
        {
            return ResamplerSpec.OutputMethod;
        }

        /* get sampling rate */
        public static double GetResampleRate(ResamplerSpecRec ResamplerSpec)
        {
            return ResamplerSpec.SamplingRate;
        }
    }
}
