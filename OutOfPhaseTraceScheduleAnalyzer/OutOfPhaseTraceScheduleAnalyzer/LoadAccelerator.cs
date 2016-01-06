/*
 *  Copyright © 2015-2016 Thomas R. Lawrence
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

namespace OutOfPhaseTraceScheduleAnalyzer
{
    public class Accelerators
    {
        public static readonly Accelerators Schedule = new Accelerators(Environment.ExpandEnvironmentVariables(@"%TEMP%\OutOfPhaseTraceScheduleAnalyzerTOC.txt"));
        public static readonly Accelerators Events = new Accelerators(Environment.ExpandEnvironmentVariables(@"%TEMP%\OutOfPhaseTraceScheduleAnalyzerTOC-Events.txt"));

        private readonly string TableOfContentsPath;

        public Accelerators(string path)
        {
            this.TableOfContentsPath = path;
        }

        public string QueryAcceleratorPath(string logPath, Stream logStream, int version)
        {
            logPath = Path.GetFullPath(logPath);
            using (Stream stream = new FileStream(TableOfContentsPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                using (TextReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Split('\t');
                        if (String.Equals(parts[0], logPath, StringComparison.OrdinalIgnoreCase))
                        {
                            if ((Int64.Parse(parts[1]) == logStream.Length)
                                && (DateTime.FromBinary(Int64.Parse(parts[2])) == File.GetCreationTimeUtc(logPath))
                                && (DateTime.FromBinary(Int64.Parse(parts[3])) == File.GetLastWriteTimeUtc(logPath))
                                && (Int32.Parse(parts[4]) == version)
                                && File.Exists(parts[5]))
                            {
                                return parts[5];
                            }
                        }
                    }
                }
            }
            return null;
        }

        private static long GetFileLength(string path)
        {
            using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return stream.Length;
            }
        }

        public void RecordAcceleratorPath(string logPath, Stream logStream, string acceleratorPath, int version)
        {
            logPath = Path.GetFullPath(logPath);

            List<string> lines = new List<string>();
            using (Stream stream = new FileStream(TableOfContentsPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                using (TextReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                }
            }

            using (Stream stream = new FileStream(TableOfContentsPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.SetLength(0);
                using (TextWriter writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    Dictionary<string, bool> acceleratorFiles = new Dictionary<string, bool>();
                    writer.WriteLine(
                        "{0}\t{1}\t{2}\t{3}\t{4}\t{5}",
                        logPath,
                        logStream.Length,
                        File.GetCreationTimeUtc(logPath).ToBinary(),
                        File.GetLastWriteTimeUtc(logPath).ToBinary(),
                        version,
                        acceleratorPath);
                    acceleratorFiles[acceleratorPath] = true;
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split('\t');
                        Debug.Assert(!acceleratorFiles.ContainsKey(parts[5]));
                        acceleratorFiles[parts[5]] = false;
                        if (!String.Equals(parts[0], logPath, StringComparison.OrdinalIgnoreCase)
                            && File.Exists(parts[0])
                            && (Int64.Parse(parts[1]) == GetFileLength(parts[0]))
                            && (DateTime.FromBinary(Int64.Parse(parts[2])) == File.GetCreationTimeUtc(parts[0]))
                            && (DateTime.FromBinary(Int64.Parse(parts[3])) == File.GetLastWriteTimeUtc(parts[0]))
                            // && (Int32.Parse(parts[4]) == version) -- can't check now; some false positives here tolerable
                            && File.Exists(parts[5]))
                        {
                            acceleratorFiles[parts[5]] = true;
                            writer.WriteLine(line);
                        }
                    }
                    foreach (KeyValuePair<string, bool> acceleratorFile in acceleratorFiles)
                    {
                        if (!acceleratorFile.Value && File.Exists(acceleratorFile.Key))
                        {
                            File.Delete(acceleratorFile.Key);
                        }
                    }
                }
            }
        }
    }
}
