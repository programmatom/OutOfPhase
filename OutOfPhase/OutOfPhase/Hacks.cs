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
using System.Runtime.InteropServices;
using System.Text;

namespace OutOfPhase
{
    public static class Hacks
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, uint wParam, uint lParam);

        // the idea for this came from:
        // http://umaranis.com/2013/10/13/programmatically-scroll-listbox-in-net-windows-forms-c/
        public static void ScrollListBox(IntPtr hWnd, int vIndex)
        {
            const uint WM_VSCROLL = 0x0115;
            const uint SB_THUMBPOSITION = 4;

            SendMessage(hWnd, WM_VSCROLL, ((uint)vIndex << 16) | (SB_THUMBPOSITION & 0xffff), 0);
        }
    }
}
