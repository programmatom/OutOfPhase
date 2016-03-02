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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class DpiChangeHelper : Component
    {
        private Form form;

        public DpiChangeHelper()
        {
            InitializeComponent();
        }

        public DpiChangeHelper(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }

        [Category("Context")]
        public Form Form { get { return form; } set { form = value; } }

        public const int WM_DPICHANGED = 0x02E0;

        private float currentDpi = 96f;

        public delegate void ScaleCallback(float scale);

        public bool WndProcDelegate(ref Message m, ScaleCallback callback)
        {
            switch (m.Msg)
            {
                case WM_DPICHANGED:
                    float oldDpi = currentDpi;
                    currentDpi = (short)m.WParam.ToInt32();
                    if ((oldDpi != currentDpi) && (oldDpi != 0))
                    {
                        float scale = currentDpi / oldDpi;
                        ScaleFont(form, scale);
                        if (callback != null)
                        {
                            callback(scale);
                        }
                        return true;
                    }
                    break;
            }
            return false;
        }

        public bool WndProcDelegate(ref Message m)
        {
            return WndProcDelegate(ref m, (ScaleCallback)null);
        }

        public delegate Control[] GetControlsMethod();

        public bool WndProcDelegate(ref Message m, GetControlsMethod explicitScaleNeeded)
        {
            return WndProcDelegate(ref m, delegate (float scale) { ScaleControls(scale, explicitScaleNeeded()); });
        }

        private static void ScaleControls(float scale, Control[] controls)
        {
            foreach (Control control in controls)
            {
                ScaleFont(control, scale);
            }
        }

        public static void ScaleFont(Control control, float scale, bool force)
        {
            if (force || (scale != 1))
            {
                Font newFont = new Font(control.Font.FontFamily, control.Font.Size * scale, control.Font.Style);
                control.Font = newFont;
            }
        }

        public static void ScaleFont(Control control, float scale)
        {
            ScaleFont(control, scale, false/*force*/);
        }

        public static void ScaleFont(Control control, float scale, GetControlsMethod explicitScaleNeeded)
        {
            ScaleFont(control, scale);
            ScaleControls(scale, explicitScaleNeeded());
        }
    }
}
