/*
 *  Copyright © 1994-2002, 2015-2017 Thomas R. Lawrence
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
using System.Drawing;
using System.Windows.Forms;

namespace OutOfPhase
{
    public class MyLabel : Label
    {
        private Size? preferredSize;

        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle face = DeflateRect(ClientRectangle, Padding);
            TextFormatFlags flags = 0;
            if ((TextAlign & (ContentAlignment.BottomLeft | ContentAlignment.MiddleLeft | ContentAlignment.TopLeft)) != 0)
            {
                flags |= TextFormatFlags.Left;
            }
            if ((TextAlign & (ContentAlignment.BottomRight | ContentAlignment.MiddleRight | ContentAlignment.TopRight)) != 0)
            {
                flags |= TextFormatFlags.Right;
            }
            if ((TextAlign & (ContentAlignment.BottomCenter | ContentAlignment.MiddleCenter | ContentAlignment.TopCenter)) != 0)
            {
                flags |= TextFormatFlags.HorizontalCenter;
            }
            MyTextRenderer.DrawText(e.Graphics, Text, Font, face, ForeColor, flags);
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            if (!preferredSize.HasValue)
            {
                using (Graphics graphics = CreateGraphics())
                {
                    preferredSize = MyTextRenderer.MeasureText(graphics, Text, Font);
                }
            }
            return preferredSize.Value;
        }

        private static Rectangle DeflateRect(Rectangle rect, Padding padding)
        {
            rect.X += padding.Left;
            rect.Y += padding.Top;
            rect.Width -= padding.Horizontal;
            rect.Height -= padding.Vertical;
            return rect;
        }

        protected override void OnTextChanged(EventArgs e)
        {
            preferredSize = null;
            base.OnTextChanged(e);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            preferredSize = null;
            base.OnFontChanged(e);
        }
    }
}
