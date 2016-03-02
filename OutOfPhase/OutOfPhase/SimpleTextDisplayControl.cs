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
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using TextEditor;

namespace OutOfPhase
{
    public partial class SimpleTextDisplayControl : Control
    {
        private string text;

        public SimpleTextDisplayControl()
        {
            InitializeComponent();
        }

        public override string Text
        {
            get
            {
                return text;
            }

            set
            {
                text = value;
                Invalidate();
            }
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (GDIOffscreenBitmap hBitmapOffscreen = new GDIOffscreenBitmap(e.Graphics, ClientSize.Width, ClientSize.Height))
            {
                using (Brush backBrush = new SolidBrush(BackColor))
                {
                    hBitmapOffscreen.Graphics.FillRectangle(backBrush, ClientRectangle);
                }
                MyTextRenderer.DrawText(hBitmapOffscreen.Graphics, text, Font, new Point(), ForeColor);
                using (GDIRegion gdiRgnClip = new GDIRegion(e.Graphics.Clip.GetHrgn(e.Graphics)))
                {
                    using (GraphicsHDC hDC = new GraphicsHDC(e.Graphics))
                    {
                        // Graphics/GDI+ doesn't pass clip region through so we have to reset it explicitly
                        GDI.SelectClipRgn(hDC, gdiRgnClip);

                        bool f = GDI.BitBlt(
                            hDC,
                            0,
                            0,
                            hBitmapOffscreen.Width,
                            hBitmapOffscreen.Height,
                            hBitmapOffscreen.HDC,
                            0,
                            0,
                            GDI.SRCCOPY);
                    }
                }
            }
            base.OnPaint(e);
        }
    }
}
