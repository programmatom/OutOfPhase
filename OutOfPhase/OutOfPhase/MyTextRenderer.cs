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
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

using TextEditor;

namespace OutOfPhase
{
    public static class MyTextRenderer
    {
        public static void DrawText(Graphics graphics, string text, Font font, Point pt, Color foreColor)
        {
        Retry:
            try
            {
                if (!Program.Config.EnableDirectWrite || fallback)
                {
                    TextRenderer.DrawText(graphics, text, font, pt, foreColor);
                }
                else
                {
                    DirectWriteTextRenderer.DrawText(graphics, text, font, pt, foreColor);
                }
            }
            catch (FileNotFoundException exception)
            {
                if (Fallback())
                {
                    goto Retry;
                }
                ShowError(exception);
                throw;
            }
        }

        public static void DrawText(Graphics graphics, string text, Font font, Rectangle bounds, Color foreColor)
        {
        Retry:
            try
            {
                if (!Program.Config.EnableDirectWrite || fallback)
                {
                    TextRenderer.DrawText(graphics, text, font, bounds, foreColor);
                }
                else
                {
                    DirectWriteTextRenderer.DrawText(graphics, text, font, bounds, foreColor);
                }
            }
            catch (FileNotFoundException exception)
            {
                if (Fallback())
                {
                    goto Retry;
                }
                ShowError(exception);
                throw;
            }
        }

        public static void DrawText(Graphics graphics, string text, Font font, Point pt, Color foreColor, Color backColor)
        {
        Retry:
            try
            {
                if (!Program.Config.EnableDirectWrite || fallback)
                {
                    TextRenderer.DrawText(graphics, text, font, pt, foreColor, backColor);
                }
                else
                {
                    DirectWriteTextRenderer.DrawText(graphics, text, font, pt, foreColor, backColor);
                }
            }
            catch (FileNotFoundException exception)
            {
                if (Fallback())
                {
                    goto Retry;
                }
                ShowError(exception);
                throw;
            }
        }

        public static void DrawText(Graphics graphics, string text, Font font, Point pt, Color foreColor, TextFormatFlags flags)
        {
        Retry:
            try
            {
                if (!Program.Config.EnableDirectWrite || fallback)
                {
                    TextRenderer.DrawText(graphics, text, font, pt, foreColor, flags);
                }
                else
                {
                    DirectWriteTextRenderer.DrawText(graphics, text, font, pt, foreColor, flags);
                }
            }
            catch (FileNotFoundException exception)
            {
                if (Fallback())
                {
                    goto Retry;
                }
                ShowError(exception);
                throw;
            }
        }

        public static void DrawText(Graphics graphics, string text, Font font, Rectangle bounds, Color foreColor, Color backColor)
        {
        Retry:
            try
            {
                if (!Program.Config.EnableDirectWrite || fallback)
                {
                    TextRenderer.DrawText(graphics, text, font, bounds, foreColor, backColor);
                }
                else
                {
                    DirectWriteTextRenderer.DrawText(graphics, text, font, bounds, foreColor, backColor);
                }
            }
            catch (FileNotFoundException exception)
            {
                if (Fallback())
                {
                    goto Retry;
                }
                ShowError(exception);
                throw;
            }
        }

        public static void DrawText(Graphics graphics, string text, Font font, Rectangle bounds, Color foreColor, TextFormatFlags flags)
        {
        Retry:
            try
            {
                if (!Program.Config.EnableDirectWrite || fallback)
                {
                    TextRenderer.DrawText(graphics, text, font, bounds, foreColor, flags);
                }
                else
                {
                    DirectWriteTextRenderer.DrawText(graphics, text, font, bounds, foreColor, flags);
                }
            }
            catch (FileNotFoundException exception)
            {
                if (Fallback())
                {
                    goto Retry;
                }
                ShowError(exception);
                throw;
            }
        }

        public static void DrawText(Graphics graphics, string text, Font font, Point pt, Color foreColor, Color backColor, TextFormatFlags flags)
        {
        Retry:
            try
            {
                if (!Program.Config.EnableDirectWrite || fallback)
                {
                    TextRenderer.DrawText(graphics, text, font, pt, foreColor, backColor, flags);
                }
                else
                {
                    DirectWriteTextRenderer.DrawText(graphics, text, font, pt, foreColor, backColor, flags);
                }
            }
            catch (FileNotFoundException exception)
            {
                if (Fallback())
                {
                    goto Retry;
                }
                ShowError(exception);
                throw;
            }
        }

        public static void DrawText(Graphics graphics, string text, Font font, Rectangle bounds, Color foreColor, Color backColor, TextFormatFlags flags)
        {
        Retry:
            try
            {
                if (!Program.Config.EnableDirectWrite || fallback)
                {
                    TextRenderer.DrawText(graphics, text, font, bounds, foreColor, backColor, flags);
                }
                else
                {
                    DirectWriteTextRenderer.DrawText(graphics, text, font, bounds, foreColor, backColor, flags);
                }
            }
            catch (FileNotFoundException exception)
            {
                if (Fallback())
                {
                    goto Retry;
                }
                ShowError(exception);
                throw;
            }
        }

#if false // not supporting non-dc versions
        public static Size MeasureText(string text, Font font)
        {
        Retry:
            try
            {
                if (!Program.Config.EnableDirectWrite || fallback)
                {
                    return TextRenderer.MeasureText(text, font);
                }
                else
                {
                    return DirectWriteTextRenderer.MeasureText(text, font);
                }
            }
            catch (FileNotFoundException exception)
            {
                if (Fallback())
                {
                    goto Retry;
                }
                ShowError(exception);
                throw;
            }
        }
#endif

        public static Size MeasureText(Graphics graphics, string text, Font font)
        {
        Retry:
            try
            {
                if (!Program.Config.EnableDirectWrite || fallback)
                {
                    return TextRenderer.MeasureText(graphics, text, font);
                }
                else
                {
                    return DirectWriteTextRenderer.MeasureText(graphics, text, font);
                }
            }
            catch (FileNotFoundException exception)
            {
                if (Fallback())
                {
                    goto Retry;
                }
                ShowError(exception);
                throw;
            }
        }

#if false // not supporting non-dc versions
        public static Size MeasureText(string text, Font font, Size proposedSize)
        {
        Retry:
            try
            {
                if (!Program.Config.EnableDirectWrite || fallback)
                {
                    return TextRenderer.MeasureText(text, font, proposedSize);
                }
                else
                {
                    return DirectWriteTextRenderer.MeasureText(text, font, proposedSize);
                }
            }
            catch (FileNotFoundException exception)
            {
                if (Fallback())
                {
                    goto Retry;
                }
                ShowError(exception);
                throw;
            }
        }
#endif

        public static Size MeasureText(Graphics graphics, string text, Font font, Size proposedSize)
        {
        Retry:
            try
            {
                if (!Program.Config.EnableDirectWrite || fallback)
                {
                    return TextRenderer.MeasureText(graphics, text, font, proposedSize);
                }
                else
                {
                    return DirectWriteTextRenderer.MeasureText(graphics, text, font, proposedSize);
                }
            }
            catch (FileNotFoundException exception)
            {
                if (Fallback())
                {
                    goto Retry;
                }
                ShowError(exception);
                throw;
            }
        }

#if false // not supporting non-dc versions
        public static Size MeasureText(string text, Font font, Size proposedSize, TextFormatFlags flags)
        {
        Retry:
            try
            {
                if (!Program.Config.EnableDirectWrite || fallback)
                {
                    return TextRenderer.MeasureText(text, font, proposedSize, flags);
                }
                else
                {
                    return DirectWriteTextRenderer.MeasureText(text, font, proposedSize, flags);
                }
            }
            catch (FileNotFoundException exception)
            {
                if (Fallback())
                {
                    goto Retry;
                }
                ShowError(exception);
                throw;
            }
        }
#endif

        public static Size MeasureText(Graphics graphics, string text, Font font, Size proposedSize, TextFormatFlags flags)
        {
        Retry:
            try
            {
                if (!Program.Config.EnableDirectWrite || fallback)
                {
                    return TextRenderer.MeasureText(graphics, text, font, proposedSize, flags);
                }
                else
                {
                    return DirectWriteTextRenderer.MeasureText(graphics, text, font, proposedSize, flags);
                }
            }
            catch (FileNotFoundException exception)
            {
                if (Fallback())
                {
                    goto Retry;
                }
                ShowError(exception);
                throw;
            }
        }

        private static void ShowError(Exception exception)
        {
            string platform = String.Empty;
            switch (Assembly.GetExecutingAssembly().GetName().ProcessorArchitecture)
            {
                default:
                    break;
                case ProcessorArchitecture.X86:
                    platform = " (x86)";
                    break;
                case ProcessorArchitecture.Amd64:
                    platform = " (x64)";
                    break;
            }
            MessageBox.Show(String.Format("Unable to load program component. To solve this problem, make sure the Visual Studio 2015 Redistributable{1} is installed on the computer. (Internal exception: {0})", exception.Message, platform));
        }

        private static bool fallback = false;
        private static bool Fallback()
        {
            // HACK to fall back to GDI at design time if the TextEditorDirectWrite dll fails to load, since keeping the dlls
            // design-happy seems to be so fragile
            if (System.Diagnostics.Process.GetCurrentProcess().ProcessName == "devenv")
            {
                fallback = true;
            }
            return fallback;
        }

        public static void FinalizeBeforeShutdown()
        {
            if (!(!Program.Config.EnableDirectWrite || fallback))
            {
                DirectWriteTextRenderer.FinalizeBeforeShutdown();
            }
        }
    }
}
