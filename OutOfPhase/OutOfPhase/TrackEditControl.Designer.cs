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
namespace OutOfPhase
{
    partial class TrackEditControl
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.trackViewControl = new OutOfPhase.TrackViewControl();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonArrow = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonCommand = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonSixtyFourth = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonThirtySecond = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonSixteenth = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonEighth = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonQuarter = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonHalf = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonWhole = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonDouble = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonQuad = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonSharp = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonFlat = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonNatural = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonNoteVsRest = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonNoteVsRest2 = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonNoDot = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonYesDot = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonDiv1 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonDiv3 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonDiv5 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonDiv7 = new System.Windows.Forms.ToolStripButton();
            this.noteViewControl = new OutOfPhase.NoteViewControl();
            this.noteParamStrip = new OutOfPhase.NoteParamStrip();
            this.trackObjectRecBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackObjectRecBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.trackViewControl, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.noteViewControl, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.noteParamStrip, 0, 3);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 5;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(918, 373);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // trackViewControl
            // 
            this.trackViewControl.AutoScroll = true;
            this.trackViewControl.BackColor = System.Drawing.SystemColors.Window;
            this.trackViewControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.trackViewControl.Location = new System.Drawing.Point(0, 53);
            this.trackViewControl.Margin = new System.Windows.Forms.Padding(0);
            this.trackViewControl.Name = "trackViewControl";
            this.trackViewControl.Size = new System.Drawing.Size(918, 320);
            this.trackViewControl.TabIndex = 6;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.Controls.Add(this.toolStrip);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(918, 25);
            this.flowLayoutPanel1.TabIndex = 1;
            // 
            // toolStrip
            // 
            this.toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonArrow,
            this.toolStripSeparator1,
            this.toolStripButtonCommand,
            this.toolStripSeparator2,
            this.toolStripButtonSixtyFourth,
            this.toolStripButtonThirtySecond,
            this.toolStripButtonSixteenth,
            this.toolStripButtonEighth,
            this.toolStripButtonQuarter,
            this.toolStripButtonHalf,
            this.toolStripButtonWhole,
            this.toolStripButtonDouble,
            this.toolStripButtonQuad,
            this.toolStripSeparator3,
            this.toolStripButtonSharp,
            this.toolStripButtonFlat,
            this.toolStripButtonNatural,
            this.toolStripSeparator4,
            this.toolStripButtonNoteVsRest,
            this.toolStripButtonNoteVsRest2,
            this.toolStripSeparator5,
            this.toolStripButtonNoDot,
            this.toolStripButtonYesDot,
            this.toolStripSeparator6,
            this.toolStripButtonDiv1,
            this.toolStripButtonDiv3,
            this.toolStripButtonDiv5,
            this.toolStripButtonDiv7});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Padding = new System.Windows.Forms.Padding(0);
            this.toolStrip.Size = new System.Drawing.Size(566, 25);
            this.toolStrip.TabIndex = 1;
            // 
            // toolStripButtonArrow
            // 
            this.toolStripButtonArrow.AutoSize = false;
            this.toolStripButtonArrow.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonArrow.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonArrow.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonArrow.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripButtonArrow.Name = "toolStripButtonArrow";
            this.toolStripButtonArrow.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonArrow.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonCommand
            // 
            this.toolStripButtonCommand.AutoSize = false;
            this.toolStripButtonCommand.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonCommand.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonCommand.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonCommand.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripButtonCommand.Name = "toolStripButtonCommand";
            this.toolStripButtonCommand.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonCommand.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonSixtyFourth
            // 
            this.toolStripButtonSixtyFourth.AutoSize = false;
            this.toolStripButtonSixtyFourth.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonSixtyFourth.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonSixtyFourth.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSixtyFourth.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripButtonSixtyFourth.Name = "toolStripButtonSixtyFourth";
            this.toolStripButtonSixtyFourth.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonSixtyFourth.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripButtonThirtySecond
            // 
            this.toolStripButtonThirtySecond.AutoSize = false;
            this.toolStripButtonThirtySecond.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonThirtySecond.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonThirtySecond.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonThirtySecond.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripButtonThirtySecond.Name = "toolStripButtonThirtySecond";
            this.toolStripButtonThirtySecond.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonThirtySecond.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripButtonSixteenth
            // 
            this.toolStripButtonSixteenth.AutoSize = false;
            this.toolStripButtonSixteenth.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonSixteenth.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonSixteenth.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSixteenth.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripButtonSixteenth.Name = "toolStripButtonSixteenth";
            this.toolStripButtonSixteenth.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonSixteenth.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripButtonEighth
            // 
            this.toolStripButtonEighth.AutoSize = false;
            this.toolStripButtonEighth.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonEighth.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonEighth.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonEighth.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripButtonEighth.Name = "toolStripButtonEighth";
            this.toolStripButtonEighth.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonEighth.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripButtonQuarter
            // 
            this.toolStripButtonQuarter.AutoSize = false;
            this.toolStripButtonQuarter.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonQuarter.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonQuarter.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonQuarter.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripButtonQuarter.Name = "toolStripButtonQuarter";
            this.toolStripButtonQuarter.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonQuarter.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripButtonHalf
            // 
            this.toolStripButtonHalf.AutoSize = false;
            this.toolStripButtonHalf.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonHalf.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonHalf.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonHalf.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripButtonHalf.Name = "toolStripButtonHalf";
            this.toolStripButtonHalf.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonHalf.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripButtonWhole
            // 
            this.toolStripButtonWhole.AutoSize = false;
            this.toolStripButtonWhole.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonWhole.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonWhole.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonWhole.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripButtonWhole.Name = "toolStripButtonWhole";
            this.toolStripButtonWhole.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonWhole.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripButtonDouble
            // 
            this.toolStripButtonDouble.AutoSize = false;
            this.toolStripButtonDouble.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonDouble.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonDouble.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonDouble.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripButtonDouble.Name = "toolStripButtonDouble";
            this.toolStripButtonDouble.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonDouble.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripButtonQuad
            // 
            this.toolStripButtonQuad.AutoSize = false;
            this.toolStripButtonQuad.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonQuad.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonQuad.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonQuad.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripButtonQuad.Name = "toolStripButtonQuad";
            this.toolStripButtonQuad.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonQuad.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonSharp
            // 
            this.toolStripButtonSharp.AutoSize = false;
            this.toolStripButtonSharp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonSharp.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonSharp.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSharp.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripButtonSharp.Name = "toolStripButtonSharp";
            this.toolStripButtonSharp.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonSharp.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripButtonFlat
            // 
            this.toolStripButtonFlat.AutoSize = false;
            this.toolStripButtonFlat.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonFlat.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonFlat.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonFlat.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripButtonFlat.Name = "toolStripButtonFlat";
            this.toolStripButtonFlat.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonFlat.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripButtonNatural
            // 
            this.toolStripButtonNatural.AutoSize = false;
            this.toolStripButtonNatural.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonNatural.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonNatural.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonNatural.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripButtonNatural.Name = "toolStripButtonNatural";
            this.toolStripButtonNatural.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonNatural.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonNoteVsRest
            // 
            this.toolStripButtonNoteVsRest.AutoSize = false;
            this.toolStripButtonNoteVsRest.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonNoteVsRest.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonNoteVsRest.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonNoteVsRest.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripButtonNoteVsRest.Name = "toolStripButtonNoteVsRest";
            this.toolStripButtonNoteVsRest.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonNoteVsRest.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripButtonNoteVsRest2
            // 
            this.toolStripButtonNoteVsRest2.AutoSize = false;
            this.toolStripButtonNoteVsRest2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonNoteVsRest2.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonNoteVsRest2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonNoteVsRest2.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripButtonNoteVsRest2.Name = "toolStripButtonNoteVsRest2";
            this.toolStripButtonNoteVsRest2.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonNoteVsRest2.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonNoDot
            // 
            this.toolStripButtonNoDot.AutoSize = false;
            this.toolStripButtonNoDot.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonNoDot.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonNoDot.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonNoDot.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripButtonNoDot.Name = "toolStripButtonNoDot";
            this.toolStripButtonNoDot.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonNoDot.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripButtonYesDot
            // 
            this.toolStripButtonYesDot.AutoSize = false;
            this.toolStripButtonYesDot.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonYesDot.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonYesDot.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonYesDot.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripButtonYesDot.Name = "toolStripButtonYesDot";
            this.toolStripButtonYesDot.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonYesDot.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripSeparator6
            // 
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            this.toolStripSeparator6.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonDiv1
            // 
            this.toolStripButtonDiv1.AutoSize = false;
            this.toolStripButtonDiv1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonDiv1.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonDiv1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonDiv1.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripButtonDiv1.Name = "toolStripButtonDiv1";
            this.toolStripButtonDiv1.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonDiv1.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripButtonDiv3
            // 
            this.toolStripButtonDiv3.AutoSize = false;
            this.toolStripButtonDiv3.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonDiv3.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonDiv3.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonDiv3.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripButtonDiv3.Name = "toolStripButtonDiv3";
            this.toolStripButtonDiv3.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonDiv3.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripButtonDiv5
            // 
            this.toolStripButtonDiv5.AutoSize = false;
            this.toolStripButtonDiv5.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonDiv5.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonDiv5.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonDiv5.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripButtonDiv5.Name = "toolStripButtonDiv5";
            this.toolStripButtonDiv5.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonDiv5.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripButtonDiv7
            // 
            this.toolStripButtonDiv7.AutoSize = false;
            this.toolStripButtonDiv7.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonDiv7.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonDiv7.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonDiv7.Margin = new System.Windows.Forms.Padding(0);
            this.toolStripButtonDiv7.Name = "toolStripButtonDiv7";
            this.toolStripButtonDiv7.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonDiv7.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // noteViewControl
            // 
            this.noteViewControl.AutoSize = true;
            this.noteViewControl.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.noteViewControl.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.noteViewControl.Dock = System.Windows.Forms.DockStyle.Top;
            this.noteViewControl.FocusReturnsTo = this.trackViewControl;
            this.noteViewControl.HighlightBackColor = System.Drawing.SystemColors.Window;
            this.noteViewControl.Location = new System.Drawing.Point(1, 26);
            this.noteViewControl.Margin = new System.Windows.Forms.Padding(1);
            this.noteViewControl.MinimumSize = new System.Drawing.Size(2, 26);
            this.noteViewControl.Name = "noteViewControl";
            this.noteViewControl.Size = new System.Drawing.Size(916, 26);
            this.noteViewControl.TabIndex = 3;
            // 
            // noteParamStrip
            // 
            this.noteParamStrip.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.noteParamStrip.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.noteParamStrip.FocusReturnsTo = this.trackViewControl;
            this.noteParamStrip.HighlightBackColor = System.Drawing.SystemColors.Window;
            this.noteParamStrip.Location = new System.Drawing.Point(0, 53);
            this.noteParamStrip.Margin = new System.Windows.Forms.Padding(0);
            this.noteParamStrip.Name = "noteParamStrip";
            this.noteParamStrip.Size = new System.Drawing.Size(918, 0);
            this.noteParamStrip.TabIndex = 7;
            this.noteParamStrip.TrackView = this.trackViewControl;
            // 
            // trackObjectRecBindingSource
            // 
            this.trackObjectRecBindingSource.DataSource = typeof(OutOfPhase.TrackObjectRec);
            // 
            // TrackEditControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "TrackEditControl";
            this.Size = new System.Drawing.Size(918, 373);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackObjectRecBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.BindingSource trackObjectRecBindingSource;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private NoteViewControl noteViewControl;
        private TrackViewControl trackViewControl;
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton toolStripButtonArrow;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton toolStripButtonCommand;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton toolStripButtonSixtyFourth;
        private System.Windows.Forms.ToolStripButton toolStripButtonThirtySecond;
        private System.Windows.Forms.ToolStripButton toolStripButtonSixteenth;
        private System.Windows.Forms.ToolStripButton toolStripButtonEighth;
        private System.Windows.Forms.ToolStripButton toolStripButtonQuarter;
        private System.Windows.Forms.ToolStripButton toolStripButtonHalf;
        private System.Windows.Forms.ToolStripButton toolStripButtonWhole;
        private System.Windows.Forms.ToolStripButton toolStripButtonDouble;
        private System.Windows.Forms.ToolStripButton toolStripButtonQuad;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripButton toolStripButtonSharp;
        private System.Windows.Forms.ToolStripButton toolStripButtonFlat;
        private System.Windows.Forms.ToolStripButton toolStripButtonNatural;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripButton toolStripButtonNoteVsRest;
        private System.Windows.Forms.ToolStripButton toolStripButtonNoteVsRest2;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripButton toolStripButtonNoDot;
        private System.Windows.Forms.ToolStripButton toolStripButtonYesDot;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        private System.Windows.Forms.ToolStripButton toolStripButtonDiv1;
        private System.Windows.Forms.ToolStripButton toolStripButtonDiv3;
        private System.Windows.Forms.ToolStripButton toolStripButtonDiv5;
        private System.Windows.Forms.ToolStripButton toolStripButtonDiv7;
        private NoteParamStrip noteParamStrip;
    }
}
