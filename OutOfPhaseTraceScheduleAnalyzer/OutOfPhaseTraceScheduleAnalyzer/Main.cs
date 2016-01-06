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
using System.Windows.Forms;

namespace OutOfPhaseTraceScheduleAnalyzer
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            #region Debugger Attach Helper
            {
                bool waitDebugger = false;
                if ((args.Length > 0) && String.Equals(args[0], "-waitdebugger"))
                {
                    waitDebugger = true;
                    Array.Copy(args, 1, args, 0, args.Length - 1);
                    Array.Resize(ref args, args.Length - 1);
                }

                bool debuggerBreak = false;
                if ((args.Length > 0) && String.Equals(args[0], "-break"))
                {
                    debuggerBreak = true;
                    Array.Copy(args, 1, args, 0, args.Length - 1);
                    Array.Resize(ref args, args.Length - 1);
                }

                if (waitDebugger)
                {
                    while (!Debugger.IsAttached)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
                if (debuggerBreak)
                {
                    Debugger.Break();
                }
            }
            #endregion

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Idle += new EventHandler(
                delegate(object sender, EventArgs e)
                {
                    if (Application.OpenForms.Count == 0)
                    {
                        Application.Exit();
                    }
                });

            if (args.Length == 0)
            {
                using (OpenFileDialog dialog = new OpenFileDialog())
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        args = new string[] { dialog.FileName };
                    }
                }
            }

            foreach (string arg in args)
            {
                Top top = DataRegistry.QueryAddref(arg);
                if (top == null)
                {
                    top = Top.Read(
                        arg,
                        new FileStream(arg, FileMode.Open, FileAccess.Read, FileShare.Read));
                    DataRegistry.Add(arg, top);
                }
                new VisualizerForm(top, arg).Show();
            }

            Application.Run();
        }
    }
}
