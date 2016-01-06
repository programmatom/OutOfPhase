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
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    public static partial class MIDIImport
    {
        /* import midi file from existing file pointer */
        public static void ImportMIDIFileSpecified(string Where)
        {
            RawMIDIScoreRec Score = NewRawMIDIScore();
            using (Stream stream = new FileStream(Where, FileMode.Open, FileAccess.Read, FileShare.Read, Constants.BufferSize))
            {
                using (BinaryReader BufferedFile = new BinaryReader(stream))
                {
                    MIDIParseErrorType result = MIDIParseErrorType.eMIDIParseFileReadError;
                    try
                    {
                        result = ParseMIDIFile(BufferedFile, Score);
                    }
                    catch (InvalidDataException)
                    {
                        // generally - unexpected eof
                        Debug.Assert(result == MIDIParseErrorType.eMIDIParseFileReadError);
                    }
                    switch (result)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();
                        case MIDIParseErrorType.eMIDIParseNoError:
                            break;
                        case MIDIParseErrorType.eMIDIParseFileReadError:
                            MessageBox.Show(
                                "An error occurred while reading the file. Attempting to import as much of the file as possible.",
                                "Import Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            break;
                        case MIDIParseErrorType.eMIDIParseBadFormat:
                            MessageBox.Show(
                                "The file is not a valid MIDI file. Attempting to import as much of the file as possible.",
                                "Import Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            break;
                    }
                }
            }
            if (GetRawMIDIScoreNumTracks(Score) > 0)
            {
                /* create the new document to put the tracks into */
                bool TrackWasCreated = false;
                Document Document = new Document();

                /* iterate over MIDI tracks */
                int TrackLimit = GetRawMIDIScoreNumTracks(Score);
                for (int TrackScan = 0; TrackScan < TrackLimit; TrackScan++)
                {
                    /* get track */
                    RawMIDITrackRec Track = GetRawMIDIScoreIndexedTrack(Score, TrackScan);

                    /* process channels */
                    for (short ChannelScan = 1; ChannelScan <= 16; ChannelScan++)
                    {
                        bool KeepFlag;
                        IntervalTrackRec IntervalTrack = NewIntervalTrack();
                        ConvertRawToInterval(Track, IntervalTrack, ChannelScan, out KeepFlag);

                        /* process cooked track into one of our track objects */
                        if (KeepFlag)
                        {
                            QuantizedTrackRec QuantizedTrack;
                            TrackObjectRec DocumentTrack;

                            /* make sure we can handle this track */
                            if (MIDITimingType.eMIDIMeteredTime != RawMIDIScoreGetTimingMode(Score))
                            {
                                MessageBox.Show(
                                    "Can't import real-time MIDI files.",
                                    "Import Error",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Stop);
                                return;
                            }

                            QuantizedTrack = NewQuantizedTrack();
                            ConvertIntervalToQuantized(IntervalTrack, QuantizedTrack,
                                RawMIDIScoreGetPartsPerQuarterNote(Score));

                            DocumentTrack = new TrackObjectRec(Document);
                            Document.TrackList.Add(DocumentTrack);
                            TrackWasCreated = true;
                            ConvertQuantToNote(QuantizedTrack, DocumentTrack);

                            /* set track release point 1 to be from end */
                            DocumentTrack.DefaultReleasePoint1ModeFlag = NoteFlags.eRelease1FromEnd;
                        }
                    }
                }

                /* clear the dirty flag, since nothing was actually modified */
                Document.Modified = false;

                /* remove track if we didn't actually find anything in the file */
                if (!TrackWasCreated)
                {
                    MessageBox.Show(
                        "No tracks were found in the MIDI file.",
                        "Import Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                MainWindow MainWindow = new MainWindow(Document, null);
                MainWindow.Show();
            }
        }

        /* import a MIDI file, prompting for file location */
        public static void ImportMIDIFile()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "MIDI Audio File (.mid)|*.mid|MIDI File (.midi)|*.midi|Any File Type (*)|*";
                DialogResult result = dialog.ShowDialog();
                if (result != DialogResult.OK)
                {
                    return;
                }
                ImportMIDIFileSpecified(dialog.FileName);
            }
        }
    }
}
