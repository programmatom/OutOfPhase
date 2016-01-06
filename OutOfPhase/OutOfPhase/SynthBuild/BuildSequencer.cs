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
        /* syntax errors */
        public enum BuildSeqErrors
        {
            eBuildSeqNoError,
            eBuildSeqExpectedTrack,
            eBuildSeqExpectedTrackName,
            eBuildSeqExpectedGroup,
            eBuildSeqExpectedGroupName,
            eBuildSeqExpectedSemicolon,
        }

        /* token enumeration definitions */
        public enum SeqKeywordType
        {
            eSeqKeywordTrack,
            eSeqKeywordGroup,
        }

        public static readonly KeywordRec<SeqKeywordType>[] SeqKeywordTable = new KeywordRec<SeqKeywordType>[]
	    {
		    new KeywordRec<SeqKeywordType>("group", SeqKeywordType.eSeqKeywordGroup),
		    new KeywordRec<SeqKeywordType>("track", SeqKeywordType.eSeqKeywordTrack),
	    };

        private static BuildSeqErrors ParseSequencer(
            SequencerConfigSpecRec Sequencer,
            ScannerRec<SeqKeywordType> Scanner,
            out int ErrorLine)
        {
            TokenRec<SeqKeywordType> Token;
            TokenRec<SeqKeywordType> TokenName;
            TokenRec<SeqKeywordType> TokenGroup;

            ErrorLine = -1;

            while (true)
            {
                Token = Scanner.GetNextToken();
                if (Token.GetTokenType() == TokenTypes.eTokenEndOfInput)
                {
                    break;
                }
                if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    || (Token.GetTokenKeywordTag() != SeqKeywordType.eSeqKeywordTrack))
                {
                    ErrorLine = Scanner.GetCurrentLineNumber();
                    return BuildSeqErrors.eBuildSeqExpectedTrack;
                }

                /* parse the track */

                /* "name" */
                TokenName = Scanner.GetNextToken();
                if (TokenName.GetTokenType() != TokenTypes.eTokenString)
                {
                    ErrorLine = Scanner.GetCurrentLineNumber();
                    return BuildSeqErrors.eBuildSeqExpectedTrackName;
                }

                /* group */
                Token = Scanner.GetNextToken();
                if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    || (Token.GetTokenKeywordTag() != SeqKeywordType.eSeqKeywordGroup))
                {
                    ErrorLine = Scanner.GetCurrentLineNumber();
                    return BuildSeqErrors.eBuildSeqExpectedGroup;
                }

                /* "group" */
                TokenGroup = Scanner.GetNextToken();
                if (TokenGroup.GetTokenType() != TokenTypes.eTokenString)
                {
                    ErrorLine = Scanner.GetCurrentLineNumber();
                    return BuildSeqErrors.eBuildSeqExpectedGroupName;
                }

                /* ; */
                Token = Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
                {
                    ErrorLine = Scanner.GetCurrentLineNumber();
                    return BuildSeqErrors.eBuildSeqExpectedSemicolon;
                }

                /* add it */
                AddTrackToSequencerConfigSpec(
                    Sequencer,
                    TokenName.GetTokenStringValue(),
                    TokenGroup.GetTokenStringValue());
            }

            return BuildSeqErrors.eBuildSeqNoError;
        }

        /* take a block of text and parse it into a sequencer config.  it returns an */
        /* error code.  if an error occurs, then *SeqOut is invalid, otherwise it will */
        /* be valid.  the text file remains unaltered.  *ErrorLine is numbered from 1. */
        public static BuildSeqErrors BuildSequencerFromText(
            string TextFile,
            out int ErrorLine,
            out SequencerConfigSpecRec SeqOut)
        {
            ScannerRec<SeqKeywordType> Scanner;
            SequencerConfigSpecRec Sequencer;
            BuildSeqErrors Error;

            SeqOut = null;
            ErrorLine = -1;

            Sequencer = NewSequencerConfigSpec();
            Scanner = new ScannerRec<SeqKeywordType>(TextFile, SeqKeywordTable);
            Error = ParseSequencer(
                Sequencer,
                Scanner,
                out ErrorLine);
            if (Error == BuildSeqErrors.eBuildSeqNoError)
            {
                /* success */
                SeqOut = Sequencer;
            }
            return Error;
        }

        /* get a static null terminated string describing the error */
        public static string BuildSeqGetErrorMessageText(BuildSeqErrors ErrorCode)
        {
            switch (ErrorCode)
            {
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();
                case BuildSeqErrors.eBuildSeqExpectedTrack:
                    return "Expected 'track'";
                case BuildSeqErrors.eBuildSeqExpectedTrackName:
                    return "Expected track name";
                case BuildSeqErrors.eBuildSeqExpectedGroup:
                    return "Expected 'group'";
                case BuildSeqErrors.eBuildSeqExpectedGroupName:
                    return "Expected group name";
                case BuildSeqErrors.eBuildSeqExpectedSemicolon:
                    return "Expected ';'";
            }
        }
    }
}
