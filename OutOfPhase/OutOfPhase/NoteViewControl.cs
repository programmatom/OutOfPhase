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
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class NoteViewControl : UserControl
    {
        private Font boldFont;

        public NoteViewControl()
        {
            DoubleBuffered = true;

            InitializeComponent();
        }

        protected override Size DefaultMinimumSize
        {
            get
            {
                return new Size(0, NUMLINES * Font.Height + 2 * PIXELINSET);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            boldFont = new Font(Font, FontStyle.Bold);
        }


        private NoteNoteObjectRec note;
        public NoteNoteObjectRec Note { get { return note; } set { note = value; Invalidate(); } }

        private const int PIXELINSET = 2;
        private const int NUMLINES = 3;

        protected override void OnPaint(PaintEventArgs pe)
        {
            // custom paint code here
            pe.Graphics.DrawRectangle(Pens.Black, ClientRectangle);
            pe.Graphics.FillRectangle(Brushes.White, Rectangle.Inflate(ClientRectangle, -1, -1));
            if (note != null)
            {
                double Number;


                // line 1

                int y = PIXELINSET;
                float x = PIXELINSET;

                AddText(pe.Graphics, "Vol=", ref x, y, Font);
                Number = Note.GetNoteOverallLoudnessAdjustment();
                AddText(pe.Graphics, Number.ToString(), ref x, y, (Number != 1) ? boldFont : Font);

                AddText(pe.Graphics, "  Start=", ref x, y, Font);
                Number = Note.GetNoteEarlyLateAdjust();
                AddText(pe.Graphics, Number.ToString(), ref x, y, (Number != 0) ? boldFont : Font);

                AddText(pe.Graphics, "  DurAdj=", ref x, y, Font);
                Number = Note.GetNoteDurationAdjust();
                AddText(pe.Graphics, Number.ToString(), ref x, y, (Number != 0) ? boldFont : Font);
                AddText(pe.Graphics, "/", ref x, y, Font);
                switch (Note.GetNoteDurationAdjustMode())
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case NoteFlags.eDurationAdjustDefault:
                        AddText(pe.Graphics, "dflt", ref x, y, Font);
                        break;
                    case NoteFlags.eDurationAdjustAdditive:
                        AddText(pe.Graphics, "add", ref x, y, boldFont);
                        break;
                    case NoteFlags.eDurationAdjustMultiplicative:
                        AddText(pe.Graphics, "mul", ref x, y, boldFont);
                        break;
                }

                AddText(pe.Graphics, "  Rls1=", ref x, y, Font);
                Number = Note.GetNoteReleasePoint1();
                AddText(pe.Graphics, Number.ToString(), ref x, y, (Number != 0) ? boldFont : Font);
                AddText(pe.Graphics, "/", ref x, y, Font);
                switch (Note.GetNoteRelease1Origin())
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case NoteFlags.eRelease1FromDefault:
                        AddText(pe.Graphics, "dflt", ref x, y, Font);
                        break;
                    case NoteFlags.eRelease1FromStart:
                        AddText(pe.Graphics, "start", ref x, y, boldFont);
                        break;
                    case NoteFlags.eRelease1FromEnd:
                        AddText(pe.Graphics, "end", ref x, y, boldFont);
                        break;
                }

                AddText(pe.Graphics, "  Rls2=", ref x, y, Font);
                Number = Note.GetNoteReleasePoint2();
                AddText(pe.Graphics, Number.ToString(), ref x, y, (Number != 0) ? boldFont : Font);
                AddText(pe.Graphics, "/", ref x, y, Font);
                switch (Note.GetNoteRelease2Origin())
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case NoteFlags.eRelease2FromDefault:
                        AddText(pe.Graphics, "dflt", ref x, y, Font);
                        break;
                    case NoteFlags.eRelease2FromStart:
                        AddText(pe.Graphics, "start", ref x, y, boldFont);
                        break;
                    case NoteFlags.eRelease2FromEnd:
                        AddText(pe.Graphics, "end", ref x, y, boldFont);
                        break;
                }

                AddText(pe.Graphics, "  Rls3=", ref x, y, Font);
                if (Note.GetNoteRelease3FromStartInsteadOfEnd())
                {
                    AddText(pe.Graphics, "start", ref x, y, boldFont);
                }
                else
                {
                    AddText(pe.Graphics, "end", ref x, y, Font);
                }

                AddText(pe.Graphics, "  ", ref x, y, Font);
                if (Note.GetNotePortamentoHertzNotHalfstepsFlag())
                {
                    AddText(pe.Graphics, "PorHz", ref x, y, boldFont);
                }
                else
                {
                    AddText(pe.Graphics, "PorHStp", ref x, y, Font);
                }


                // line 2

                y += Font.Height;
                x = PIXELINSET;

                AddText(pe.Graphics, "Por=", ref x, y, Font);
                Number = Note.GetNotePortamentoDuration();
                AddText(pe.Graphics, Number.ToString(), ref x, y, (Number != 0) ? boldFont : Font);

                AddText(pe.Graphics, "  Bal=", ref x, y, Font);
                Number = Note.GetNoteStereoPositioning();
                AddText(pe.Graphics, Number.ToString(), ref x, y, (Number != 0) ? boldFont : Font);

                AddText(pe.Graphics, "  PDD=", ref x, y, Font);
                Number = Note.GetNotePitchDisplacementDepthAdjust();
                AddText(pe.Graphics, Number.ToString(), ref x, y, (Number != 1) ? boldFont : Font);
                AddText(pe.Graphics, "/", ref x, y, Font);
                switch (Note.GetNotePitchDisplacementStartOrigin())
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case NoteFlags.ePitchDisplacementStartFromDefault:
                        AddText(pe.Graphics, "dflt", ref x, y, Font);
                        break;
                    case NoteFlags.ePitchDisplacementStartFromStart:
                        AddText(pe.Graphics, "start", ref x, y, boldFont);
                        break;
                    case NoteFlags.ePitchDisplacementStartFromEnd:
                        AddText(pe.Graphics, "end", ref x, y, boldFont);
                        break;
                }

                AddText(pe.Graphics, "  PDR=", ref x, y, Font);
                Number = Note.GetNotePitchDisplacementRateAdjust();
                AddText(pe.Graphics, Number.ToString(), ref x, y, (Number != 1) ? boldFont : Font);

                AddText(pe.Graphics, "  PDS=", ref x, y, Font);
                Number = Note.GetNotePitchDisplacementStartPoint();
                AddText(pe.Graphics, Number.ToString(), ref x, y, (Number != 0) ? boldFont : Font);

                AddText(pe.Graphics, "  Hur=", ref x, y, Font);
                Number = Note.GetNoteHurryUpFactor();
                AddText(pe.Graphics, Number.ToString(), ref x, y, (Number != 1) ? boldFont : Font);

                AddText(pe.Graphics, "  Dtn=", ref x, y, Font);
                Number = Note.GetNoteDetuning();
                AddText(pe.Graphics, Number.ToString(), ref x, y, (Number != 0) ? boldFont : Font);
                AddText(pe.Graphics, "/", ref x, y, Font);
                switch (Note.GetNoteDetuneConversionMode())
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case NoteFlags.eDetuningModeDefault:
                        AddText(pe.Graphics, "dflt", ref x, y, Font);
                        break;
                    case NoteFlags.eDetuningModeHalfSteps:
                        AddText(pe.Graphics, "HStp", ref x, y, boldFont);
                        break;
                    case NoteFlags.eDetuningModeHertz:
                        AddText(pe.Graphics, "Hz", ref x, y, boldFont);
                        break;
                }

                AddText(pe.Graphics, "  Sur=", ref x, y, Font);
                Number = Note.GetNoteSurroundPositioning();
                AddText(pe.Graphics, Number.ToString(), ref x, y, (Number != 0) ? boldFont : Font);


                // line 3

                y += Font.Height;
                x = PIXELINSET;

                AddText(pe.Graphics, "MuS=", ref x, y, Font);
                if (-1 == Note.GetNoteMultisampleFalsePitch())
                {
                    AddText(pe.Graphics, "dflt", ref x, y, Font);
                }
                else
                {
                    AddText(pe.Graphics, SymbolicPitch.NumericPitchToString(Note.GetNoteMultisampleFalsePitch(), 0), ref x, y, boldFont);
                }

                AddText(pe.Graphics, "    Accents:  1=", ref x, y, Font);
                Number = Note.GetNoteAccent1();
                AddText(pe.Graphics, Number.ToString(), ref x, y, (Number != 0) ? boldFont : Font);

                AddText(pe.Graphics, "  2=", ref x, y, Font);
                Number = Note.GetNoteAccent2();
                AddText(pe.Graphics, Number.ToString(), ref x, y, (Number != 0) ? boldFont : Font);

                AddText(pe.Graphics, "  3=", ref x, y, Font);
                Number = Note.GetNoteAccent3();
                AddText(pe.Graphics, Number.ToString(), ref x, y, (Number != 0) ? boldFont : Font);

                AddText(pe.Graphics, "  4=", ref x, y, Font);
                Number = Note.GetNoteAccent4();
                AddText(pe.Graphics, Number.ToString(), ref x, y, (Number != 0) ? boldFont : Font);

                AddText(pe.Graphics, "  5=", ref x, y, Font);
                Number = Note.GetNoteAccent5();
                AddText(pe.Graphics, Number.ToString(), ref x, y, (Number != 0) ? boldFont : Font);

                AddText(pe.Graphics, "  6=", ref x, y, Font);
                Number = Note.GetNoteAccent6();
                AddText(pe.Graphics, Number.ToString(), ref x, y, (Number != 0) ? boldFont : Font);

                AddText(pe.Graphics, "  7=", ref x, y, Font);
                Number = Note.GetNoteAccent7();
                AddText(pe.Graphics, Number.ToString(), ref x, y, (Number != 0) ? boldFont : Font);

                AddText(pe.Graphics, "  8=", ref x, y, Font);
                Number = Note.GetNoteAccent8();
                AddText(pe.Graphics, Number.ToString(), ref x, y, (Number != 0) ? boldFont : Font);
            }

            // Calling the base class OnPaint
            base.OnPaint(pe);
        }

        private void AddText(Graphics graphics, string text, ref float x, int y, Font font)
        {
            StringFormat format = new StringFormat();
            RectangleF rect = new RectangleF(0, 0, 1000, font.Height);
            CharacterRange[] ranges = new CharacterRange[] { new CharacterRange(0, text.Length) };
            Region[] regions = new Region[1];
            format.SetMeasurableCharacterRanges(ranges);
            regions = graphics.MeasureCharacterRanges(text, font, rect, format);
            rect = regions[0].GetBounds(graphics);
            float width = rect.Width;

            graphics.DrawString(text, font, Brushes.Black, x, y);
            x += width;
        }
    }
}
