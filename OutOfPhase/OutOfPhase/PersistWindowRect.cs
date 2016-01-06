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
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    public static class PersistWindowRect
    {
        public static void OnShown(Form form, short x, short y, short width, short height)
        {
            bool maximize = false;
            if ((width < 0) && (height < 0))
            {
                width = (short)-width;
                height = (short)-height;
                maximize = true;
            }
            Rectangle rect = new Rectangle(x, y, width, height);
            if (!rect.IsEmpty)
            {
                form.DesktopBounds = rect;
            }
            if (maximize)
            {
                form.WindowState = FormWindowState.Maximized;
            }
        }

        public static void OnResize(Form form, out short x, out short y, out short width, out short height)
        {
            int sign;
            switch (form.WindowState)
            {
                default:
                case FormWindowState.Normal:
                    x = (short)Math.Min(form.DesktopBounds.Left, Int16.MaxValue);
                    y = (short)Math.Min(form.DesktopBounds.Top, Int16.MaxValue);
                    width = (short)Math.Min(form.DesktopBounds.Width, Int16.MaxValue);
                    height = (short)Math.Min(form.DesktopBounds.Height, Int16.MaxValue);
                    break;
                case FormWindowState.Maximized:
                    sign = -1;
                    goto DoIt;
                case FormWindowState.Minimized:
                    sign = 1;
                DoIt:
                    x = (short)Math.Min(form.RestoreBounds.Left, Int16.MaxValue);
                    y = (short)Math.Min(form.RestoreBounds.Top, Int16.MaxValue);
                    width = (short)(sign * Math.Min(form.RestoreBounds.Width, Int16.MaxValue));
                    height = (short)(sign * Math.Min(form.RestoreBounds.Height, Int16.MaxValue));
                    break;
            }
        }
    }
}
