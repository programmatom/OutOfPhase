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
using System.Text;

namespace OutOfPhase
{
    public static class EditCommandParameters
    {
        // public interface

        public static void EditCommandAttributes(CommandNoteObjectRec NoteCommand)
        {
            /* debug validation for the map table */
            if (CommandDialogMap.Length != NoteCommands.eCmd_End - NoteCommands.eCmd_Start)
            {
                // map table is the wrong size
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
#if DEBUG
            for (int i = 0; i < CommandDialogMap.Length; i++)
            {
                if (CommandDialogMap[i].Opcode - NoteCommands.eCmd_Start != i)
                {
                    // opcodes not in order
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
            }
#endif

            /* present the appropriate dialog */
            int index = NoteCommand.GetCommandOpcode() - NoteCommands.eCmd_Start;
            if ((index < 0) || (index >= CommandDialogMap.Length))
            {
                // opcode out of range
                Debug.Assert(false);
                throw new ArgumentException();
            }
            else
            {
                CommandDialogMap[index].Conversion(NoteCommand);
            }
        }


        // implementation

        /* dialog box for command with <1xs> parameter */
        private static bool OneXS(
            CommandNoteObjectRec NoteCommand,
            string Prompt,
            string Box1Text)
        {
            double OneDouble;
            bool ChangedFlag;

            OneDouble = (double)SmallExtBCDType.FromRawInt32(NoteCommand.GetCommandNumericArg1());
            ChangedFlag = CmdDlgOneParam.CommandDialogOneParam(Prompt, Box1Text, ref OneDouble);
            if (ChangedFlag)
            {
                NoteCommand.PutCommandNumericArg1(((SmallExtBCDType)OneDouble).rawInt32);
            }
            return ChangedFlag;
        }

        /* dialog box for command with <1xs> and <2xs> parameters */
        private static bool TwoXS(
            CommandNoteObjectRec NoteCommand,
            string Prompt,
            string Box1Text,
            string Box2Text)
        {
            double OneDouble;
            double TwoDouble;
            bool ChangedFlag;

            OneDouble = (double)SmallExtBCDType.FromRawInt32(NoteCommand.GetCommandNumericArg1());
            TwoDouble = (double)SmallExtBCDType.FromRawInt32(NoteCommand.GetCommandNumericArg2());
            ChangedFlag = CmdDlgTwoParams.CommandDialogTwoParams(Prompt, Box1Text, ref OneDouble, Box2Text, ref TwoDouble);
            if (ChangedFlag)
            {
                NoteCommand.PutCommandNumericArg1(((SmallExtBCDType)OneDouble).rawInt32);
                NoteCommand.PutCommandNumericArg2(((SmallExtBCDType)TwoDouble).rawInt32);
            }
            return ChangedFlag;
        }

        /* dialog box for command with <1l> parameter */
        private static bool OneL(
            CommandNoteObjectRec NoteCommand,
            string Prompt,
            string Box1Text)
        {
            double OneDouble;
            bool ChangedFlag;

            OneDouble = (double)LargeBCDType.FromRawInt32(NoteCommand.GetCommandNumericArg1());
            ChangedFlag = CmdDlgOneParam.CommandDialogOneParam(Prompt, Box1Text, ref OneDouble);
            if (ChangedFlag)
            {
                NoteCommand.PutCommandNumericArg1(((LargeBCDType)OneDouble).rawInt32);
            }
            return ChangedFlag;
        }

        /* dialog box for 2 param command with first param <1l> and second <2xs> */
        private static bool OneLTwoXS(
            CommandNoteObjectRec NoteCommand,
            string Prompt,
            string Box1Text,
            string Box2Text)
        {
            double OneDouble;
            double TwoDouble;
            bool ChangedFlag;

            OneDouble = (double)LargeBCDType.FromRawInt32(NoteCommand.GetCommandNumericArg1());
            TwoDouble = (double)SmallExtBCDType.FromRawInt32(NoteCommand.GetCommandNumericArg2());
            ChangedFlag = CmdDlgTwoParams.CommandDialogTwoParams(Prompt, Box1Text, ref OneDouble, Box2Text, ref TwoDouble);
            if (ChangedFlag)
            {
                NoteCommand.PutCommandNumericArg1(((LargeBCDType)OneDouble).rawInt32);
                NoteCommand.PutCommandNumericArg2(((SmallExtBCDType)TwoDouble).rawInt32);
            }
            return ChangedFlag;
        }

        /* dialog box for 2 param command with first param <1i> and second <2l> */
        private static bool OneITwoL(
            CommandNoteObjectRec NoteCommand,
            string Prompt,
            string Box1Text,
            string Box2Text)
        {
            double OneDouble;
            double TwoDouble;
            bool ChangedFlag;

            OneDouble = NoteCommand.GetCommandNumericArg1();
            TwoDouble = (double)LargeBCDType.FromRawInt32(NoteCommand.GetCommandNumericArg2());
            ChangedFlag = CmdDlgTwoParams.CommandDialogTwoParams(Prompt, Box1Text, ref OneDouble, Box2Text, ref TwoDouble);
            if (ChangedFlag)
            {
                NoteCommand.PutCommandNumericArg1((int)OneDouble);
                NoteCommand.PutCommandNumericArg2(((LargeBCDType)TwoDouble).rawInt32);
            }
            return ChangedFlag;
        }

        /* dialog box where the value being negative means one thing and the value */
        /* being zero or positive means another. */
        private static bool OneBool(
            CommandNoteObjectRec NoteCommand,
            string Prompt,
            string Negative,
            string ZeroOrPositive)
        {
            bool Flag;
            bool ChangedFlag;

            if (NoteCommand.GetCommandNumericArg1() < 0)
            {
                /* negative */
                Flag = true;
            }
            else
            {
                /* zero or positive */
                Flag = false;
            }
            ChangedFlag = CmdDlgOneBinaryChoice.CommandDialogOneBinaryChoice(Prompt, Negative, ZeroOrPositive, ref Flag);
            if (ChangedFlag)
            {
                if (Flag)
                {
                    /* true = negative */
                    NoteCommand.PutCommandNumericArg1(-1);
                }
                else
                {
                    /* false = zero or positive */
                    NoteCommand.PutCommandNumericArg1(0);
                }
            }
            return ChangedFlag;
        }

        /* dialog box for command with <1i> and <2i> parameters */
        private static bool TwoI(
            CommandNoteObjectRec NoteCommand,
            string Prompt,
            string Box1Text,
            string Box2Text)
        {
            double OneDouble;
            double TwoDouble;
            bool ChangedFlag;

            OneDouble = NoteCommand.GetCommandNumericArg1();
            TwoDouble = NoteCommand.GetCommandNumericArg2();
            ChangedFlag = CmdDlgTwoParams.CommandDialogTwoParams(Prompt, Box1Text, ref OneDouble, Box2Text, ref TwoDouble);
            if (ChangedFlag)
            {
                NoteCommand.PutCommandNumericArg1((int)OneDouble);
                NoteCommand.PutCommandNumericArg2((int)TwoDouble);
            }
            return ChangedFlag;
        }

        /* dialog box for command with <1s> parameter */
        private static bool OneStr(
            CommandNoteObjectRec NoteCommand,
            string Prompt,
            string Box1Text)
        {
            string StringParameter;
            bool ChangedFlag;

            StringParameter = NoteCommand.GetCommandStringArg1();
            if (StringParameter == null)
            {
                StringParameter = String.Empty;
            }
            ChangedFlag = CmdDlgOneParam.CommandDialogOneString2(
                Prompt,
                Box1Text,
                ref StringParameter);
            if (ChangedFlag)
            {
                NoteCommand.PutCommandStringArg1(StringParameter);
            }
            return ChangedFlag;
        }

        /* dialog box for command with <1s-lf> parameter */
        private static bool OneStrLF(
            CommandNoteObjectRec NoteCommand,
            string Prompt,
            string Box1Text)
        {
            string StringParameter;
            bool ChangedFlag;

            StringParameter = NoteCommand.GetCommandStringArg1();
            if (StringParameter == null)
            {
                StringParameter = String.Empty;
            }
            ChangedFlag = CmdDlgOneParam.CommandDialogOneString(
                Prompt,
                Box1Text,
                ref StringParameter);
            if (ChangedFlag)
            {
                NoteCommand.PutCommandStringArg1(StringParameter);
            }
            return ChangedFlag;
        }

        /* dialog box for command with <1s> <2s> parameters */
        private static bool TwoStr(
            CommandNoteObjectRec NoteCommand,
            string Prompt,
            string Box1Text,
            string Box2Text)
        {
            string StringParameter1;
            string StringParameter2;
            bool ChangedFlag;

            StringParameter1 = NoteCommand.GetCommandStringArg1();
            if (StringParameter1 == null)
            {
                StringParameter1 = String.Empty;
            }
            StringParameter2 = NoteCommand.GetCommandStringArg2();
            if (StringParameter2 == null)
            {
                StringParameter2 = String.Empty;
            }
            ChangedFlag = CmdDlgTwoParams.CommandDialogTwoStrings(
                Prompt,
                Box1Text,
                ref StringParameter1,
                Box2Text,
                ref StringParameter2);
            if (ChangedFlag)
            {
                NoteCommand.PutCommandStringArg1(StringParameter1);
                NoteCommand.PutCommandStringArg2(StringParameter2);
            }
            return ChangedFlag;
        }

        /* dialog box for command with <1l> parameter */
        private static bool OneI(
            CommandNoteObjectRec NoteCommand,
            string Prompt,
            string Box1Text)
        {
            double OneDouble;
            bool ChangedFlag;

            OneDouble = NoteCommand.GetCommandNumericArg1();
            ChangedFlag = CmdDlgOneParam.CommandDialogOneParam(Prompt, Box1Text, ref OneDouble);
            if (ChangedFlag)
            {
                NoteCommand.PutCommandNumericArg1((int)OneDouble);
            }
            return ChangedFlag;
        }

        /* dialog box for command with <1string> <2l> parameters */
        private static bool OneStrTwoL(
            CommandNoteObjectRec NoteCommand,
            string Prompt,
            string Box1Text,
            string Box2Text)
        {
            string StringParameter1;
            double Parameter2;
            bool ChangedFlag;

            StringParameter1 = NoteCommand.GetCommandStringArg1();
            if (StringParameter1 == null)
            {
                StringParameter1 = String.Empty;
            }
            Parameter2 = (double)LargeBCDType.FromRawInt32(NoteCommand.GetCommandNumericArg1());
            ChangedFlag = CmdDlgTwoParams.CommandDialogOneStringOneParam(
                Prompt,
                Box1Text,
                ref StringParameter1,
                Box2Text,
                ref Parameter2);
            if (ChangedFlag)
            {
                NoteCommand.PutCommandStringArg1(StringParameter1);
                NoteCommand.PutCommandNumericArg1(((LargeBCDType)Parameter2).rawInt32);
            }
            return ChangedFlag;
        }

        // dialog box for pitch table command -- with <1string> <2l> parameters
        private static bool PitchTable(
            CommandNoteObjectRec NoteCommand)
        {
            string StringParameter1;
            double Parameter2;
            bool ChangedFlag;

            StringParameter1 = NoteCommand.GetCommandStringArg1();
            if (StringParameter1 == null)
            {
                StringParameter1 = String.Empty;
            }
            Parameter2 = (double)LargeBCDType.FromRawInt32(NoteCommand.GetCommandNumericArg1());
            ChangedFlag = CmdDlgLoadPitchTable.CommandDialogOneStringOneParam(
                ref StringParameter1,
                ref Parameter2);
            if (ChangedFlag)
            {
                NoteCommand.PutCommandStringArg1(StringParameter1);
                NoteCommand.PutCommandNumericArg1(((LargeBCDType)Parameter2).rawInt32);
            }
            return ChangedFlag;
        }


        /* base class */

        private abstract class BaseRec
        {
            public readonly NoteCommands Opcode;

            public BaseRec(NoteCommands Opcode)
            {
                this.Opcode = Opcode;
            }

            public abstract bool Conversion(CommandNoteObjectRec NoteCommand);
        }


        /* dialog box for command with no parameters */

        private class XZero : BaseRec
        {
            public XZero(
                NoteCommands Opcode)
                : base(Opcode)
            {
            }

            public override bool Conversion(CommandNoteObjectRec NoteCommand)
            {
                Debug.Assert(CommandMapping.GetCommandAddressingMode(NoteCommand.GetCommandOpcode()) == CommandAddrMode.eNoParameters);
                return false;
            }
        }


        /* dialog box for command with <1xs> parameter */

        private class XOneXS : BaseRec
        {
            private readonly string Prompt;
            private readonly string Box1Text;

            public XOneXS(
                NoteCommands Opcode,
                string Prompt,
                string Box1Text)
                : base(Opcode)
            {
                this.Prompt = Prompt;
                this.Box1Text = Box1Text;
            }

            public override bool Conversion(CommandNoteObjectRec NoteCommand)
            {
                Debug.Assert(CommandMapping.GetCommandAddressingMode(NoteCommand.GetCommandOpcode()) == CommandAddrMode.e1SmallExtParameter);
                return OneXS(
                    NoteCommand,
                    this.Prompt,
                    this.Box1Text);
            }
        }


        /* dialog box for command with <1xs> and <2xs> parameters */

        private class XTwoXS : BaseRec
        {
            private readonly string Prompt;
            private readonly string Box1Text;
            private readonly string Box2Text;

            public XTwoXS(
                NoteCommands Opcode,
                string Prompt,
                string Box1Text,
                string Box2Text)
                : base(Opcode)
            {
                this.Prompt = Prompt;
                this.Box1Text = Box1Text;
                this.Box2Text = Box2Text;
            }

            public override bool Conversion(CommandNoteObjectRec NoteCommand)
            {
                Debug.Assert(CommandMapping.GetCommandAddressingMode(NoteCommand.GetCommandOpcode()) == CommandAddrMode.e2SmallExtParameters);
                return TwoXS(
                    NoteCommand,
                    this.Prompt,
                    this.Box1Text,
                    this.Box2Text);
            }
        }


        /* dialog box for command with <1l> parameter */

        private class XOneL : BaseRec
        {
            private readonly string Prompt;
            private readonly string Box1Text;

            public XOneL(
                NoteCommands Opcode,
                string Prompt,
                string Box1Text)
                : base(Opcode)
            {
                this.Prompt = Prompt;
                this.Box1Text = Box1Text;
            }

            public override bool Conversion(CommandNoteObjectRec NoteCommand)
            {
                Debug.Assert(CommandMapping.GetCommandAddressingMode(NoteCommand.GetCommandOpcode()) == CommandAddrMode.e1LargeParameter);
                return OneL(
                    NoteCommand,
                    this.Prompt,
                    this.Box1Text);
            }
        }


        /* dialog box for 2 param command with first param <1l> and second <2xs> */

        private class XOneLTwoXS : BaseRec
        {
            private readonly string Prompt;
            private readonly string Box1Text;
            private readonly string Box2Text;

            public XOneLTwoXS(
                NoteCommands Opcode,
                string Prompt,
                string Box1Text,
                string Box2Text)
                : base(Opcode)
            {
                this.Prompt = Prompt;
                this.Box1Text = Box1Text;
                this.Box2Text = Box2Text;
            }

            public override bool Conversion(CommandNoteObjectRec NoteCommand)
            {
                Debug.Assert(CommandMapping.GetCommandAddressingMode(NoteCommand.GetCommandOpcode()) == CommandAddrMode.eFirstLargeSecondSmallExtParameters);
                return OneLTwoXS(
                    NoteCommand,
                    this.Prompt,
                    this.Box1Text,
                    this.Box2Text);
            }
        }


        /* dialog box for 2 param command with first param <1i> and second <2l> */

        private class XOneITwoL : BaseRec
        {
            private readonly string Prompt;
            private readonly string Box1Text;
            private readonly string Box2Text;

            public XOneITwoL(
                NoteCommands Opcode,
                string Prompt,
                string Box1Text,
                string Box2Text)
                : base(Opcode)
            {
                this.Prompt = Prompt;
                this.Box1Text = Box1Text;
                this.Box2Text = Box2Text;
            }

            public override bool Conversion(CommandNoteObjectRec NoteCommand)
            {
                Debug.Assert(CommandMapping.GetCommandAddressingMode(NoteCommand.GetCommandOpcode()) == CommandAddrMode.eFirstIntSecondLargeParameters);
                return OneITwoL(
                    NoteCommand,
                    this.Prompt,
                    this.Box1Text,
                    this.Box2Text);
            }
        }


        /* dialog box where the value being negative means one thing and the value */
        /* being zero or positive means another. */

        private class XOneBool : BaseRec
        {
            private readonly string Prompt;
            private readonly string Negative;
            private readonly string ZeroOrPositive;

            public XOneBool(
                NoteCommands Opcode,
                string Prompt,
                string Negative,
                string ZeroOrPositive)
                : base(Opcode)
            {
                this.Prompt = Prompt;
                this.Negative = Negative;
                this.ZeroOrPositive = ZeroOrPositive;
            }

            public override bool Conversion(CommandNoteObjectRec NoteCommand)
            {
                Debug.Assert((CommandAddrMode.e1PitchDisplacementMode == CommandMapping.GetCommandAddressingMode(NoteCommand.GetCommandOpcode()))
                    || (CommandAddrMode.e1ParamReleaseOrigin == CommandMapping.GetCommandAddressingMode(NoteCommand.GetCommandOpcode()))
                    || (CommandAddrMode.e1TrackEffectsMode == CommandMapping.GetCommandAddressingMode(NoteCommand.GetCommandOpcode()))
                    || (CommandAddrMode.e1DurationAdjustMode == CommandMapping.GetCommandAddressingMode(NoteCommand.GetCommandOpcode())));
                return OneBool(
                    NoteCommand,
                    this.Prompt,
                    this.Negative,
                    this.ZeroOrPositive);
            }
        }


        /* dialog box for command with <1i> and <2i> parameters */

        private class XTwoI : BaseRec
        {
            private readonly string Prompt;
            private readonly string Box1Text;
            private readonly string Box2Text;

            public XTwoI(
                NoteCommands Opcode,
                string Prompt,
                string Box1Text,
                string Box2Text)
                : base(Opcode)
            {
                this.Prompt = Prompt;
                this.Box1Text = Box1Text;
                this.Box2Text = Box2Text;
            }

            public override bool Conversion(CommandNoteObjectRec NoteCommand)
            {
                Debug.Assert(CommandMapping.GetCommandAddressingMode(NoteCommand.GetCommandOpcode()) == CommandAddrMode.e2IntegerParameters);
                return TwoI(
                    NoteCommand,
                    this.Prompt,
                    this.Box1Text,
                    this.Box2Text);
            }
        }


        /* dialog box for command with <1s> parameter */

        private class XOneStr : BaseRec
        {
            private readonly string Prompt;
            private readonly string Box1Text;

            public XOneStr(
                NoteCommands Opcode,
                string Prompt,
                string Box1Text)
                : base(Opcode)
            {
                this.Prompt = Prompt;
                this.Box1Text = Box1Text;
            }

            public override bool Conversion(CommandNoteObjectRec NoteCommand)
            {
                Debug.Assert(CommandMapping.GetCommandAddressingMode(NoteCommand.GetCommandOpcode()) == CommandAddrMode.e1StringParameter);
                return OneStr(
                    NoteCommand,
                    this.Prompt,
                    this.Box1Text);
            }
        }


        /* dialog box for command with <1s-lf> parameter */

        private class XOneStrLF : BaseRec
        {
            private readonly string Prompt;
            private readonly string Box1Text;

            public XOneStrLF(
                NoteCommands Opcode,
                string Prompt,
                string Box1Text)
                : base(Opcode)
            {
                this.Prompt = Prompt;
                this.Box1Text = Box1Text;
            }

            public override bool Conversion(CommandNoteObjectRec NoteCommand)
            {
                Debug.Assert(CommandMapping.GetCommandAddressingMode(NoteCommand.GetCommandOpcode()) == CommandAddrMode.e1StringParameterWithLineFeeds);
                return OneStrLF(
                    NoteCommand,
                    this.Prompt,
                    this.Box1Text);
            }
        }


        /* dialog box for command with <1s> <2s> parameters */

        private class XTwoStr : BaseRec
        {
            private readonly string Prompt;
            private readonly string Box1Text;
            private readonly string Box2Text;

            public XTwoStr(
                NoteCommands Opcode,
                string Prompt,
                string Box1Text,
                string Box2Text)
                : base(Opcode)
            {
                this.Prompt = Prompt;
                this.Box1Text = Box1Text;
                this.Box2Text = Box2Text;
            }

            public override bool Conversion(CommandNoteObjectRec NoteCommand)
            {
                Debug.Assert(CommandMapping.GetCommandAddressingMode(NoteCommand.GetCommandOpcode()) == CommandAddrMode.e2StringParameters);
                return TwoStr(
                    NoteCommand,
                    this.Prompt,
                    this.Box1Text,
                    this.Box2Text);
            }
        }


        /* dialog box for command with <1i> parameter */

        private class XOneI : BaseRec
        {
            private readonly string Prompt;
            private readonly string Box1Text;

            public XOneI(
                NoteCommands Opcode,
                string Prompt,
                string Box1Text)
                : base(Opcode)
            {
                this.Prompt = Prompt;
                this.Box1Text = Box1Text;
            }

            public override bool Conversion(CommandNoteObjectRec NoteCommand)
            {
                Debug.Assert(CommandMapping.GetCommandAddressingMode(NoteCommand.GetCommandOpcode()) == CommandAddrMode.e1IntegerParameter);
                return OneI(
                    NoteCommand,
                    this.Prompt,
                    this.Box1Text);
            }
        }


        /* dialog box for command with <1string> <2l> parameters */

        private class XOneStrTwoL : BaseRec
        {
            private readonly string Prompt;
            private readonly string Box1Text;
            private readonly string Box2Text;

            public XOneStrTwoL(
                NoteCommands Opcode,
                string Prompt,
                string Box1Text,
                string Box2Text)
                : base(Opcode)
            {
                this.Prompt = Prompt;
                this.Box1Text = Box1Text;
                this.Box2Text = Box2Text;
            }

            public override bool Conversion(CommandNoteObjectRec NoteCommand)
            {
                Debug.Assert(CommandMapping.GetCommandAddressingMode(NoteCommand.GetCommandOpcode()) == CommandAddrMode.e1String1LargeBCDParameters);
                return OneStrTwoL(
                    NoteCommand,
                    this.Prompt,
                    this.Box1Text,
                    this.Box2Text);
            }
        }


        /* dialog box for command with <1string> <2l> parameters */

        private class XPitchTable : BaseRec
        {
            public XPitchTable(
                NoteCommands Opcode)
                : base(Opcode)
            {
            }

            public override bool Conversion(CommandNoteObjectRec NoteCommand)
            {
                Debug.Assert(CommandMapping.GetCommandAddressingMode(NoteCommand.GetCommandOpcode()) == CommandAddrMode.e1String1LargeBCDParameters);
                return PitchTable(
                    NoteCommand);
            }
        }


        /* list implementation */

        private const string LAccentValue = "New Accent Value:";
        private const string LAccentAdjustment = "Accent Adjustment:";
        private const string LFinalValue = "Final Value:";
        private const string LFinalAdjustment = "Final Adjustment:";
        private const string LDuration = "Duration:";

        private static readonly BaseRec[] CommandDialogMap = new BaseRec[]
        {
            /* restore the tempo to the default for the score */
            new XZero(
                NoteCommands.eCmdRestoreTempo),

            /* set tempo to <1xs> number of beats per second */
            new XOneXS(
                NoteCommands.eCmdSetTempo,
                "Set Tempo: Enter a new tempo value.",
                "Beats per Minute:"),

            /* add <1xs> to the tempo control */
            new XOneXS(
                NoteCommands.eCmdIncTempo,
                "Increment Tempo: Enter the number of beats per minute to change the tempo by.",
                "BPM Adjustment:"),

            /* <1xs> = target tempo, <2xs> = # of beats to reach it */
            new XTwoXS(
                NoteCommands.eCmdSweepTempoAbs,
                "Sweep Tempo Absolute: Enter new tempo destination value and the number of beats to spread the transition across.",
                "Final BPM:",
                LDuration),

            /* <1xs> = target adjust (add to tempo), <2xs> = # beats */
            new XTwoXS(
                NoteCommands.eCmdSweepTempoRel,
                "Sweep Tempo Relative: Enter a tempo adjustment value and the number of beats to spread the transition across.",
                "Final BPM Adjustment:",
                LDuration),

            /* restore stereo position to channel's default */
            new XZero(
                NoteCommands.eCmdRestoreStereoPosition),

            /* set position in channel <1l>: -1 = left, 1 = right */
            new XOneL(
                NoteCommands.eCmdSetStereoPosition,
                "Set Stereo Position: Enter a stereo position value (-1 = hard left ... 1 = hard right).",
                "Stereo Position:"),

            /* adjust stereo position by adding <1l> */
            new XOneL(
                NoteCommands.eCmdIncStereoPosition,
                "Adjust Stereo Position: Enter an adjustment value for the stereo position (negative values move the channel left; positive values move the channel right).",
                "Stereo Position Adjustment:"),

            /* <1l> = new pos, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepStereoAbs,
                "Sweep Stereo Absolute: Enter a destination value for the stereo position and the number of beats to spread the transition across.",
                "Final Stereo Position:",
                LDuration),

            /* <1l> = pos adjust, <2xs> = # beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepStereoRel,
                "Sweep Stereo Relative: Enter a stereo position adjustment value and the number of beats to spread the transition across.",
                "Final Position Adjustment:",
                LDuration),

            /* restore surround position to channel's default */
            new XZero(
                NoteCommands.eCmdRestoreSurroundPosition),

            /* set surround position in channel <1l>: 1 = front, -1 = rear */
            new XOneL(
                NoteCommands.eCmdSetSurroundPosition,
                "Set Surround Position: Enter a surround position value (1 = front ... -1 = rear).",
                "Surround Position:"),

            /* adjust surround position by adding <1l> */
            new XOneL(
                NoteCommands.eCmdIncSurroundPosition,
                "Adjust Surround Position: Enter an adjustment value for the surround position (positive values move the channel forward; negative values move the channel backward).",
                "Surround Position Adjustment:"),

            /* <1l> = new pos, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepSurroundAbs,
                "Sweep Surround Absolute: Enter a destination value for the surround position and the number of beats to spread the transition across.",
                "Final Surround Position:",
                LDuration),

            /* <1l> = pos adjust, <2xs> = # beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepSurroundRel,
                "Sweep Surround Relative: Enter a surround position adjustment value and the number of beats to spread the transition across.",
                "Final Position Adjustment:",
                LDuration),

            /* restore the volume to the default for the channel */
            new XZero(
                NoteCommands.eCmdRestoreVolume),

            /* set the volume to the specified level (0..1) in <1l> */
            new XOneL(
                NoteCommands.eCmdSetVolume,
                "Set Volume: Enter an overall volume level value (0 = silent ... 1 = full volume).",
                "Volume:"),

            /* add <1l> to the volume control */
            new XOneL(
                NoteCommands.eCmdIncVolume,
                "Adjust Volume: Enter a volume adjustment value (values less than 1 make sound quieter, values greater than 1 make sound louder).",
                "Volume Adjustment:"),

            /* <1l> = new volume, <2xs> = # of beats to reach it */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepVolumeAbs,
                "Sweep Volume Absolute: Enter a new volume value and the number of beats to spread the transition across.",
                "Final Volume:",
                LDuration),

            /* <1l> = volume adjust, <2xs> = # of beats to reach it */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepVolumeRel,
                "Sweep Volume Relative: Enter a volume adjustment value and the number of beats to spread the transition across.",
                "Final Volume Adjustment:",
                LDuration),

            /* restore release point to master default */
            new XZero(
                NoteCommands.eCmdRestoreReleasePoint1),

            /* set the default release point to new value <1l> */
            new XOneL(
                NoteCommands.eCmdSetReleasePoint1,
                "Set Release Point 1: Enter the first release point location (0 = start of note; 1 = end of note; values beyond range are allowed).",
                "Release Point 1:"),

            /* add <1l> to default release point for adjustment */
            new XOneL(
                NoteCommands.eCmdIncReleasePoint1,
                "Adjust Release Point 1: Enter an adjustment value for the first release point (negative values move the release earlier; positive values move it later).",
                "Release Point 1 Adjust:"),

            /* if <1i> is < 0, then from start, else from end of note */
            new XOneBool(
                NoteCommands.eCmdReleasePointOrigin1,
                "Release Point 1 Origin: Choose where the first release point should be measured from.",
                "From Start of Note",
                "From End of Note"),

            /* <1l> = new release, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepReleaseAbs1,
                "Sweep Release Point 1 Absolute: Enter a destination time for the first release point and the number of beats to spread the transition across.",
                "Release Point 1:",
                LDuration),

            /* <1l> = release adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepReleaseRel1,
                "Sweep Release Point 1 Relative: Enter an adjustment value for the first release point and the number of beats to spread the transition across.",
                "Release Point 1 Adjust:",
                LDuration),

            /* restore release point to master default */
            new XZero(
                NoteCommands.eCmdRestoreReleasePoint2),

            /* set the default release point to new value <1l> */
            new XOneL(
                NoteCommands.eCmdSetReleasePoint2,
                "Set Release Point 2: Enter the second release point location (0 = start of note; 1 = end of note; values beyond range are allowed).",
                "Release Point 2:"),

            /* add <1l> to default release point for adjustment */
            new XOneL(
                NoteCommands.eCmdIncReleasePoint2,
                "Adjust Release Point 2: Enter an adjustment value for the second release point (negative values move the release earlier; positive values move it later).",
                "Release Point 2 Adjust:"),

            /* if <1i> is < 0, then from start, else from end of note */
            new XOneBool(
                NoteCommands.eCmdReleasePointOrigin2,
                "Release Point 2 Origin: Choose where the second release point should be measured from.",
                "From Start of Note",
                "From End of Note"),

            /* <1l> = new release, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepReleaseAbs2,
                "Sweep Release Point 2 Absolute: Enter a destination time for the second release point and the number of beats to spread the transition across.",
                "Release Point 2:",
                LDuration),

            /* <1l> = release adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepReleaseRel2,
                "Sweep Release Point 2 Relative: Enter an adjustment value for the second release point and the number of beats to spread the transition across.",
                "Release Point 2 Adjust:",
                LDuration),

            /* restore accent value to master default */
            new XZero(
                NoteCommands.eCmdRestoreAccent1),

            /* specify the new default accent in <1l> */
            new XOneL(
                NoteCommands.eCmdSetAccent1,
                "Set Accent 1: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default accent */
            new XOneL(
                NoteCommands.eCmdIncAccent1,
                "Adjust Accent 1: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new accent, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepAccentAbs1,
                "Sweep Accent 1 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = accent adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepAccentRel1,
                "Sweep Accent 1 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* restore accent value to master default */
            new XZero(
                NoteCommands.eCmdRestoreAccent2),

            /* specify the new default accent in <1l> */
            new XOneL(
                NoteCommands.eCmdSetAccent2,
                "Set Accent 2: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default accent */
            new XOneL(
                NoteCommands.eCmdIncAccent2,
                "Adjust Accent 2: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new accent, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepAccentAbs2,
                "Sweep Accent 2 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = accent adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepAccentRel2,
                "Sweep Accent 2 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* restore accent value to master default */
            new XZero(
                NoteCommands.eCmdRestoreAccent3),

            /* specify the new default accent in <1l> */
            new XOneL(
                NoteCommands.eCmdSetAccent3,
                "Set Accent 3: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default accent */
            new XOneL(
                NoteCommands.eCmdIncAccent3,
                "Adjust Accent 3: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new accent, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepAccentAbs3,
                "Sweep Accent 3 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = accent adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepAccentRel3,
                "Sweep Accent 3 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* restore accent value to master default */
            new XZero(
                NoteCommands.eCmdRestoreAccent4),

            /* specify the new default accent in <1l> */
            new XOneL(
                NoteCommands.eCmdSetAccent4,
                "Set Accent 4: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default accent */
            new XOneL(
                NoteCommands.eCmdIncAccent4,
                "Adjust Accent 4: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new accent, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepAccentAbs4,
                "Sweep Accent 4 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = accent adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepAccentRel4,
                "Sweep Accent 4 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* restore accent value to master default */
            new XZero(
                NoteCommands.eCmdRestoreAccent5),

            /* specify the new default accent in <1l> */
            new XOneL(
                NoteCommands.eCmdSetAccent5,
                "Set Accent 5: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default accent */
            new XOneL(
                NoteCommands.eCmdIncAccent5,
                "Adjust Accent 5: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new accent, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepAccentAbs5,
                "Sweep Accent 5 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = accent adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepAccentRel5,
                "Sweep Accent 5 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* restore accent value to master default */
            new XZero(
                NoteCommands.eCmdRestoreAccent6),

            /* specify the new default accent in <1l> */
            new XOneL(
                NoteCommands.eCmdSetAccent6,
                "Set Accent 6: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default accent */
            new XOneL(
                NoteCommands.eCmdIncAccent6,
                "Adjust Accent 6: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new accent, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepAccentAbs6,
                "Sweep Accent 6 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = accent adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepAccentRel6,
                "Sweep Accent 6 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* restore accent value to master default */
            new XZero(
                NoteCommands.eCmdRestoreAccent7),

            /* specify the new default accent in <1l> */
            new XOneL(
                NoteCommands.eCmdSetAccent7,
                "Set Accent 7: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default accent */
            new XOneL(
                NoteCommands.eCmdIncAccent7,
                "Adjust Accent 7: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new accent, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepAccentAbs7,
                "Sweep Accent 7 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = accent adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepAccentRel7,
                "Sweep Accent 7 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* restore accent value to master default */
            new XZero(
                NoteCommands.eCmdRestoreAccent8),

            /* specify the new default accent in <1l> */
            new XOneL(
                NoteCommands.eCmdSetAccent8,
                "Set Accent 8: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default accent */
            new XOneL(
                NoteCommands.eCmdIncAccent8,
                "Adjust Accent 8: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new accent, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepAccentAbs8,
                "Sweep Accent 8 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = accent adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepAccentRel8,
                "Sweep Accent 8 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* restore max pitch disp depth value to default */
            new XZero(
                NoteCommands.eCmdRestorePitchDispDepth),

            /* set new max pitch disp depth <1l> */
            new XOneL(
                NoteCommands.eCmdSetPitchDispDepth,
                "Set Pitch Displacement Depth: Enter a new maximum pitch displacement depth.",
                "Pitch Disp. Depth:"),

            /* add <1l> to the default pitch disp depth */
            new XOneL(
                NoteCommands.eCmdIncPitchDispDepth,
                "Adjust Pitch Displacement Depth: Enter an adjustment for the maximum pitch displacement depth.",
                "Pitch Disp. Depth Adjust:"),

            /* <1l> = new depth, <2xs> = # of beats */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepPitchDispDepthAbs,
                "Sweep Pitch Displacement Depth Absolute: Enter the target pitch displacement depth and the number of beats to spread the transition across.",
                "Dest. Pitch Disp. Depth:",
                LDuration),

            /* <1l> = depth adjust, <2xs> = # of beats */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepPitchDispDepthRel,
                "Sweep Pitch Displacement Depth Relative: Enter an adjustment pitch displacement depth value and the number of beats to spread the transition across.",
                "Pitch Disp. Depth Adjust:",
                LDuration),

            /* restore max pitch disp rate to the master default */
            new XZero(
                NoteCommands.eCmdRestorePitchDispRate),

            /* set new max pitch disp rate in seconds to <1l> */
            new XOneL(
                NoteCommands.eCmdSetPitchDispRate,
                "Set Pitch Displacement Rate: Enter the maximum number of oscillations per second.",
                "Pitch Displacement Rate:"),

            /* add <1l> to the default max pitch disp rate */
            new XOneL(
                NoteCommands.eCmdIncPitchDispRate,
                "Adjust Pitch Displacement Rate: Enter an adjustment pitch displacement rate value.",
                "Pitch Disp. Rate Adjust:"),

            /* <1l> = new rate, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepPitchDispRateAbs,
                "Sweep Pitch Displacement Rate Absolute: Enter a destination pitch displacement rate and the number of beats to spread the transition across.",
                "Dest. Pitch Disp. Rate:",
                LDuration),

            /* <1l> = rate adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepPitchDispRateRel,
                "Sweep Pitch Displacement Rate Relative: Enter an adjustment pitch displacement rate value and the number of beats to spread the transition across.",
                "Pitch Disp. Rate Adjust:",
                LDuration),

            /* restore pitch disp start point to default */
            new XZero(
                NoteCommands.eCmdRestorePitchDispStart),

            /* set the start point to <1l> */
            new XOneL(
                NoteCommands.eCmdSetPitchDispStart,
                "Set Pitch Displacement Start: Enter a new start point for the pitch displacement envelope (0 = note start; 1 = note end; values out of range are allowed).",
                "Pitch Disp. Start:"),

            /* add <1l> to the pitch disp start point */
            new XOneL(
                NoteCommands.eCmdIncPitchDispStart,
                "Adjust Pitch Displacement Start: Enter an adjustment for the pitch displacement start point.",
                "Pitch Disp. Start Adjust:"),

            /* specify the origin, same as for release point <1i> */
            new XOneBool(
                NoteCommands.eCmdPitchDispStartOrigin,
                "Pitch Displacement Origin: Choose where the pitch displacement start point should be measured from.",
                "From Start of Note",
                "From End of Note"),

            /* <1l> = new vib start, <2xs> = # of beats */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepPitchDispStartAbs,
                "Sweep Pitch Displacement Start Absolute: Enter a pitch displacement start point and the number of beats to spread the transition across.",
                "Dest. Pitch Disp. Start:",
                LDuration),

            /* <1l> = vib adjust, <2xs> = # of beats */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepPitchDispStartRel,
                "Sweep Pitch Displacement Start Relative: Enter an adjustment pitch displacement start point value and the number of beats to spread the transition across.",
                "Pitch Disp. Start Adjust:",
                LDuration),

            /* restore default hurryup factor */
            new XZero(
                NoteCommands.eCmdRestoreHurryUp),

            /* set the hurryup factor to <1l> */
            new XOneL(
                NoteCommands.eCmdSetHurryUp,
                "Set Hurry-Up Factor: Enter a hurry-up factor (1 = normal; less than 1 = envelopes execute faster; greater than 1 = envelopes execute more slowly).",
                "Hurry-Up Factor:"),

            /* add <1l> to the hurryup factor */
            new XOneL(
                NoteCommands.eCmdIncHurryUp,
                "Adjust Hurry-Up Factor: Enter an adjustment hurry-up value (negative values make envelopes execute faster; positive values make envelopes execute more slowly).",
                "Hurry-Up Adjustment:"),

            /* <1l> = new hurryup factor, <2xs> = # of beats */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepHurryUpAbs,
                "Sweep Hurry-Up Factor Absolute: Enter a hurry-up factor and the number of beats to spread the transition across.",
                "Destination Hurry-Up:",
                LDuration),

            /* <1l> = hurryup adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepHurryUpRel,
                "Sweep Hurry-Up Factor Relative: Enter an adjustment hurry-up value and the number of beats to spread the transition across.",
                "Hurry-Up Adjustment:",
                LDuration),

            /* restore the default detune factor */
            new XZero(
                NoteCommands.eCmdRestoreDetune),

            /* set the detune factor to <1l> */
            new XOneL(
                NoteCommands.eCmdSetDetune,
                "Set Detuning: Enter a detuning value (negative values decrease pitch; positive values increase pitch).",
                "Detuning:"),

            /* add <1l> to current detune factor */
            new XOneL(
                NoteCommands.eCmdIncDetune,
                "Adjust Detuning: Enter an adjustment detuning value (negative values decrease pitch; positive values increase pitch).",
                "Detuning Adjustment:"),

            /* <1i>: <0: Hertz, >=0: half-steps */
            new XOneBool(
                NoteCommands.eCmdDetuneMode,
                "Detuning Mode: Choose whether the detuning value is in Hertz or halfsteps.",
                "Hertz",
                "Halfsteps"),

            /* <1l> = new detune, <2xs> = # of beats */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepDetuneAbs,
                "Sweep Detuning Absolute: Enter a destination detuning value and the number of beats to spread the transition across.",
                "Destination Detuning:",
                LDuration),

            /* <1l> = detune adjust, <2xs> = # of beats */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepDetuneRel,
                "Sweep Detuning Relative: Enter an adjustment detuning value and the number of beats to spread the transition across.",
                "Detuning Adjustment:",
                LDuration),

            /* restore the default early/late adjust value */
            new XZero(
                NoteCommands.eCmdRestoreEarlyLateAdjust),

            /* set the early/late adjust value to <1l> */
            new XOneL(
                NoteCommands.eCmdSetEarlyLateAdjust,
                "Set Early/Late Hit Adjust: Enter an early/late hit time adjustment (negative values make note hit earlier; positive values make note hit later).",
                "Early/Late Adjust:"),

            /* add <1l> to the current early/late adjust value */
            new XOneL(
                NoteCommands.eCmdIncEarlyLateAdjust,
                "Adjust Early/Late Hit Adjust: Enter an adjustment early/late hit time value (negative values make note hit earlier; positive values make note hit later).",
                "Early/Late Adjust:"),

            /* <1l> = new early/late adjust, <2xs> = # of beats */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepEarlyLateAbs,
                "Sweep Early/Late Hit Adjust Absolute: Enter a destination early/late hit time adjustment and the number of beats to spread the transition across.",
                "Destination Early/Late Adjust:",
                LDuration),

            /* <1l> = early/late delta, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepEarlyLateRel,
                "Sweep Early/Late Hit Adjust Relative: Enter an adjustment early/late hit time value and the number of beats to spread the transition across",
                "Early/Late Adjust:",
                LDuration),

            /* restore the default duration adjust value */
            new XZero(
                NoteCommands.eCmdRestoreDurationAdjust),

            /* set duration adjust value to <1l> */
            new XOneL(
                NoteCommands.eCmdSetDurationAdjust,
                "Set Duration Adjust: Enter a duration adjust factor.",
                "Duration Adjust:"),

            /* add <1l> to the current duration adjust value */
            new XOneL(
                NoteCommands.eCmdIncDurationAdjust,
                "Adjust Duration Adjust: Enter an adjustment duration adjust factor (negative values make note shorter; positive values make note longer).",
                "Duration Adjust:"),

            /* <1l> = new duration adjust, <2xs> = # of beats */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepDurationAbs,
                "Sweep Duration Adjust Absolute: Enter a destination duration adjust factor and the number of beats to spread the transition across.",
                "Dest. Duration Adjust:",
                LDuration),

            /* <1l> = duration adjust delta, <2xs> = # of beats */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepDurationRel,
                "Sweep Duration Adjust Relative: Enter an adjustment duration adjust value and the number of beats to spread the transition across.",
                "Duration Adjust:",
                LDuration),

            /* <1i>: <0: Multiplicative, >=0: Additive */
            new XOneBool(
                NoteCommands.eCmdDurationAdjustMode,
                "Set Duration Adjust Mode: Choose whether the duration adjust scales the note's duration by multiplication or addition.",
                "Multiply note's duration by value",
                "Add note's duration and value"),

            /* <1i> = numerator, <2i> = denominator */
            new XTwoI(
                NoteCommands.eCmdSetMeter,
                "Set Meter: Enter the time signature for measure bar placement.",
                "Beats per Measure:",
                "Beat Reference Note:"),

            /* <1i> = new number */
            new XOneI(
                NoteCommands.eCmdSetMeasureNumber,
                "Set Measure Number: Enter the number for the next measure bar.",
                "Next Measure Number:"),

            /* <1i> = new transpose value */
            new XOneI(
                NoteCommands.eCmdSetTranspose,
                "Set Transpose: enter the number of half-steps to transpose by.",
                "Half-steps:"),

            /* <1i> = adjusting transpose value */
            new XOneI(
                NoteCommands.eCmdAdjustTranspose,
                "Adjust Transpose: enter the number of half-steps to adjust the current transpose value by.",
                "Half-steps:"),

            /* <1i> = 0..11 index, <2xs> = normal freq * 1000 */
            new XOneITwoL(
                NoteCommands.eCmdSetFrequencyValue,
                "Set Pitch Value: Enter target index (0..11) and cents.",
                "Pitch Index:",
                "Cents:"),

            /* <1i> = 0..11 index, <2xs> = normal freq * 1000 */
            new XOneITwoL(
                NoteCommands.eCmdSetFrequencyValueLegacy,
                "Set Pitch Value: Enter target pitch (0..11) and normalized pitch [1..2) X 1000.",
                "Pitch Index:",
                "Normalized:"),

            /* <1i> = 0..11 index, <2xs> = scale factor * 1000 */
            new XOneITwoL(
                NoteCommands.eCmdAdjustFrequencyValue,
                "Adjust Pitch Value: Enter target index (0..11) and cents adjustment",
                "Pitch Index:",
                "Cents Adjust:"),

            /* <1i> = 0..11 index, <2xs> = scale factor * 1000 */
            new XOneITwoL(
                NoteCommands.eCmdAdjustFrequencyValueLegacy,
                "Adjust Pitch Value: Enter target pitch (0..11) and scale factor X 1000.",
                "Pitch Index:",
                "Scale Factor:"),

            /* <1i> = 0..11 index */
            new XOneI(
                NoteCommands.eCmdResetFrequencyValue,
                "Reset Pitch Value: Enter the target pitch (0..11) to reset.",
                "Pitch Index:"),

            // <1s> = model name, <1l> = tonic offset (integer 0..11)
#if false // TODO:Remove
            new XOneStrTwoL(
                NoteCommands.eCmdLoadFrequencyModel,
                "Load Pitch Table: Enter the name of the pitch table to load.",
                "Name:",
                "Tonic Offset:"),
#else
            new XPitchTable(
                NoteCommands.eCmdLoadFrequencyModel),
#endif

            // <1l> = new frequency factor, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue0Absolute,
                "Sweep Frequency 0 Absolute: Enter a frequency factor and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            // <1l> = new frequency factor, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue1Absolute,
                "Sweep Frequency 1 Absolute: Enter a frequency factor and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            // <1l> = new frequency factor, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue2Absolute,
                "Sweep Frequency 2 Absolute: Enter a frequency factor and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            // <1l> = new frequency factor, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue3Absolute,
                "Sweep Frequency 3 Absolute: Enter a frequency factor and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            // <1l> = new frequency factor, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue4Absolute,
                "Sweep Frequency 4 Absolute: Enter a frequency factor and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            // <1l> = new frequency factor, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue5Absolute,
                "Sweep Frequency 5 Absolute: Enter a frequency factor and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            // <1l> = new frequency factor, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue6Absolute,
                "Sweep Frequency 6 Absolute: Enter a frequency factor and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            // <1l> = new frequency factor, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue7Absolute,
                "Sweep Frequency 7 Absolute: Enter a frequency factor and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            // <1l> = new frequency factor, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue8Absolute,
                "Sweep Frequency 8 Absolute: Enter a frequency factor and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            // <1l> = new frequency factor, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue9Absolute,
                "Sweep Frequency 9 Absolute: Enter a frequency factor and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            // <1l> = new frequency factor, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue10Absolute,
                "Sweep Frequency 10 Absolute: Enter a frequency factor and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            // <1l> = new frequency factor, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue11Absolute,
                "Sweep Frequency 11 Absolute: Enter a frequency factor and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            // <1l> = frequency factor adjust, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue0Relative,
                "Sweep Frequency 0 Relative: Enter a frequency factor adjustment and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            // <1l> = frequency factor adjust, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue1Relative,
                "Sweep Frequency 1 Relative: Enter a frequency factor adjustment and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            // <1l> = frequency factor adjust, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue2Relative,
                "Sweep Frequency 2 Relative: Enter a frequency factor adjustment and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            // <1l> = frequency factor adjust, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue3Relative,
                "Sweep Frequency 3 Relative: Enter a frequency factor adjustment and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            // <1l> = frequency factor adjust, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue4Relative,
                "Sweep Frequency 4 Relative: Enter a frequency factor adjustment and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            // <1l> = frequency factor adjust, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue5Relative,
                "Sweep Frequency 5 Relative: Enter a frequency factor adjustment and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            // <1l> = frequency factor adjust, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue6Relative,
                "Sweep Frequency 6 Relative: Enter a frequency factor adjustment and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            // <1l> = frequency factor adjust, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue7Relative,
                "Sweep Frequency 7 Relative: Enter a frequency factor adjustment and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            // <1l> = frequency factor adjust, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue8Relative,
                "Sweep Frequency 8 Relative: Enter a frequency factor adjustment and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            // <1l> = frequency factor adjust, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue9Relative,
                "Sweep Frequency 9 Relative: Enter a frequency factor adjustment and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            // <1l> = frequency factor adjust, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue10Relative,
                "Sweep Frequency 10 Relative: Enter a frequency factor adjustment and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            // <1l> = frequency factor adjust, <2xs> = # of beats to get there
            new XOneLTwoXS(
                NoteCommands.eCmdSweepFrequencyValue11Relative,
                "Sweep Frequency 11 Relative: Enter a frequency factor adjustment and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* specify the new default effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetEffectParam1,
                "Set Effect Accent 1: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default effect parameter */
            new XOneL(
                NoteCommands.eCmdIncEffectParam1,
                "Adjust Effect Accent 1: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepEffectParamAbs1,
                "Sweep Effect Accent 1 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepEffectParamRel1,
                "Sweep Effect Accent 1 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* specify the new default effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetEffectParam2,
                "Set Effect Accent 2: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default effect parameter */
            new XOneL(
                NoteCommands.eCmdIncEffectParam2,
                "Adjust Effect Accent 2: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepEffectParamAbs2,
                "Sweep Effect Accent 2 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepEffectParamRel2,
                "Sweep Effect Accent 2 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* specify the new default effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetEffectParam3,
                "Set Effect Accent 3: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default effect parameter */
            new XOneL(
                NoteCommands.eCmdIncEffectParam3,
                "Adjust Effect Accent 3: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepEffectParamAbs3,
                "Sweep Effect Accent 3 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepEffectParamRel3,
                "Sweep Effect Accent 3 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* specify the new default effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetEffectParam4,
                "Set Effect Accent 4: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default effect parameter */
            new XOneL(
                NoteCommands.eCmdIncEffectParam4,
                "Adjust Effect Accent 4: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepEffectParamAbs4,
                "Sweep Effect Accent 4 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepEffectParamRel4,
                "Sweep Effect Accent 4 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* specify the new default effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetEffectParam5,
                "Set Effect Accent 5: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default effect parameter */
            new XOneL(
                NoteCommands.eCmdIncEffectParam5,
                "Adjust Effect Accent 5: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepEffectParamAbs5,
                "Sweep Effect Accent 5 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepEffectParamRel5,
                "Sweep Effect Accent 5 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* specify the new default effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetEffectParam6,
                "Set Effect Accent 6: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default effect parameter */
            new XOneL(
                NoteCommands.eCmdIncEffectParam6,
                "Adjust Effect Accent 6: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepEffectParamAbs6,
                "Sweep Effect Accent 6 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepEffectParamRel6,
                "Sweep Effect Accent 6 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* specify the new default effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetEffectParam7,
                "Set Effect Accent 7: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default effect parameter */
            new XOneL(
                NoteCommands.eCmdIncEffectParam7,
                "Adjust Effect Accent 7: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepEffectParamAbs7,
                "Sweep Effect Accent 7 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepEffectParamRel7,
                "Sweep Effect Accent 7 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* specify the new default effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetEffectParam8,
                "Set Effect Accent 8: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default effect parameter */
            new XOneL(
                NoteCommands.eCmdIncEffectParam8,
                "Adjust Effect Accent 8: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepEffectParamAbs8,
                "Sweep Effect Accent 8 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepEffectParamRel8,
                "Sweep Effect Accent 8 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* <1i>: -1 = enable, 0 = disable */
            new XOneBool(
                NoteCommands.eCmdTrackEffectEnable,
                "Track Effect Switch: Choose whether track effects are enabled or disabled.",
                "Enable",
                "Disable"),

            /* specify the new default score effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetScoreEffectParam1,
                "Set Global Score Effect Accent 1: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default score effect parameter */
            new XOneL(
                NoteCommands.eCmdIncScoreEffectParam1,
                "Adjust Global Score Effect Accent 1: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new score effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepScoreEffectParamAbs1,
                "Sweep Global Score Effect Accent 1 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = score effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepScoreEffectParamRel1,
                "Sweep Global Score Effect Accent 1 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* specify the new default score effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetScoreEffectParam2,
                "Set Global Score Effect Accent 2: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default score effect parameter */
            new XOneL(
                NoteCommands.eCmdIncScoreEffectParam2,
                "Adjust Global Score Effect Accent 2: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new score effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepScoreEffectParamAbs2,
                "Sweep Global Score Effect Accent 2 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = score effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepScoreEffectParamRel2,
                "Sweep Global Score Effect Accent 2 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* specify the new default score effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetScoreEffectParam3,
                "Set Global Score Effect Accent 3: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default score effect parameter */
            new XOneL(
                NoteCommands.eCmdIncScoreEffectParam3,
                "Adjust Global Score Effect Accent 3: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new score effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepScoreEffectParamAbs3,
                "Sweep Global Score Effect Accent 3 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = score effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepScoreEffectParamRel3,
                "Sweep Global Score Effect Accent 3 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* specify the new default score effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetScoreEffectParam4,
                "Set Global Score Effect Accent 4: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default score effect parameter */
            new XOneL(
                NoteCommands.eCmdIncScoreEffectParam4,
                "Adjust Global Score Effect Accent 4: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new score effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepScoreEffectParamAbs4,
                "Sweep Global Score Effect Accent 4 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = score effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepScoreEffectParamRel4,
                "Sweep Global Score Effect Accent 4 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* specify the new default score effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetScoreEffectParam5,
                "Set Global Score Effect Accent 5: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default score effect parameter */
            new XOneL(
                NoteCommands.eCmdIncScoreEffectParam5,
                "Adjust Global Score Effect Accent 5: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new score effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepScoreEffectParamAbs5,
                "Sweep Global Score Effect Accent 5 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = score effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepScoreEffectParamRel5,
                "Sweep Global Score Effect Accent 5 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* specify the new default score effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetScoreEffectParam6,
                "Set Global Score Effect Accent 6: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default score effect parameter */
            new XOneL(
                NoteCommands.eCmdIncScoreEffectParam6,
                "Adjust Global Score Effect Accent 6: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new score effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepScoreEffectParamAbs6,
                "Sweep Global Score Effect Accent 6 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = score effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepScoreEffectParamRel6,
                "Sweep Global Score Effect Accent 6 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* specify the new default score effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetScoreEffectParam7,
                "Set Global Score Effect Accent 7: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default score effect parameter */
            new XOneL(
                NoteCommands.eCmdIncScoreEffectParam7,
                "Adjust Global Score Effect Accent 7: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new score effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepScoreEffectParamAbs7,
                "Sweep Global Score Effect Accent 4 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = score effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepScoreEffectParamRel7,
                "Sweep Global Score Effect Accent 7 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* specify the new default score effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetScoreEffectParam8,
                "Set Global Score Effect Accent 8: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default score effect parameter */
            new XOneL(
                NoteCommands.eCmdIncScoreEffectParam8,
                "Adjust Global Score Effect Accent 8: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new score effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepScoreEffectParamAbs8,
                "Sweep Global Score Effect Accent 8 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = score effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepScoreEffectParamRel8,
                "Sweep Global Score Effect Accent 8 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* <string> holds the text */
            new XOneStrLF(
                NoteCommands.eCmdMarker,
                "Comment: Enter a new comment.",
                "Comment:"),

            /* <1i>: -1 = enable, 0 = disable */
            new XOneBool(
                NoteCommands.eCmdSectionEffectEnable,
                "Section Effect Switch: Choose whether section effects are enabled or disabled.",
                "Enable",
                "Disable"),

            /* <1i>: -1 = enable, 0 = disable */
            new XOneBool(
                NoteCommands.eCmdScoreEffectEnable,
                "Score Effect Switch: Choose whether score effects are enabled or disabled.",
                "Enable",
                "Disable"),

            /* specify the new default section effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetSectionEffectParam1,
                "Set Global Section Effect Accent 1: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default section effect parameter */
            new XOneL(
                NoteCommands.eCmdIncSectionEffectParam1,
                "Adjust Global Section Effect Accent 1: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new section effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepSectionEffectParamAbs1,
                "Sweep Global Section Effect Accent 1 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = section effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepSectionEffectParamRel1,
                "Sweep Global Section Effect Accent 1 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* specify the new default section effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetSectionEffectParam2,
                "Set Global Section Effect Accent 2: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default section effect parameter */
            new XOneL(
                NoteCommands.eCmdIncSectionEffectParam2,
                "Adjust Global Section Effect Accent 2: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new section effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepSectionEffectParamAbs2,
                "Sweep Global Section Effect Accent 2 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = section effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepSectionEffectParamRel2,
                "Sweep Global Section Effect Accent 2 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* specify the new default section effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetSectionEffectParam3,
                "Set Global Section Effect Accent 3: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default section effect parameter */
            new XOneL(
                NoteCommands.eCmdIncSectionEffectParam3,
                "Adjust Global Section Effect Accent 3: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new section effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepSectionEffectParamAbs3,
                "Sweep Global Section Effect Accent 3 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = section effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepSectionEffectParamRel3,
                "Sweep Global Section Effect Accent 3 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* specify the new default section effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetSectionEffectParam4,
                "Set Global Section Effect Accent 4: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default section effect parameter */
            new XOneL(
                NoteCommands.eCmdIncSectionEffectParam4,
                "Adjust Global Section Effect Accent 4: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new section effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepSectionEffectParamAbs4,
                "Sweep Global Section Effect Accent 4 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = section effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepSectionEffectParamRel4,
                "Sweep Global Section Effect Accent 4 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* specify the new default section effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetSectionEffectParam5,
                "Set Global Section Effect Accent 5: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default section effect parameter */
            new XOneL(
                NoteCommands.eCmdIncSectionEffectParam5,
                "Adjust Global Section Effect Accent 5: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new section effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepSectionEffectParamAbs5,
                "Sweep Global Section Effect Accent 5 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = section effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepSectionEffectParamRel5,
                "Sweep Global Section Effect Accent 5 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* specify the new default section effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetSectionEffectParam6,
                "Set Global Section Effect Accent 6: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default section effect parameter */
            new XOneL(
                NoteCommands.eCmdIncSectionEffectParam6,
                "Adjust Global Section Effect Accent 6: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new section effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepSectionEffectParamAbs6,
                "Sweep Global Section Effect Accent 6 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = section effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepSectionEffectParamRel6,
                "Sweep Global Section Effect Accent 6 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* specify the new default section effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetSectionEffectParam7,
                "Set Global Section Effect Accent 7: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default section effect parameter */
            new XOneL(
                NoteCommands.eCmdIncSectionEffectParam7,
                "Adjust Global Section Effect Accent 7: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new section effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepSectionEffectParamAbs7,
                "Sweep Global Section Effect Accent 4 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = section effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepSectionEffectParamRel7,
                "Sweep Global Section Effect Accent 7 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* specify the new default section effect parameter in <1l> */
            new XOneL(
                NoteCommands.eCmdSetSectionEffectParam8,
                "Set Global Section Effect Accent 8: Enter a new accent value.",
                LAccentValue),

            /* add <1l> to the default section effect parameter */
            new XOneL(
                NoteCommands.eCmdIncSectionEffectParam8,
                "Adjust Global Section Effect Accent 8: Enter an adjustment value.",
                LAccentAdjustment),

            /* <1l> = new section effect parameter, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepSectionEffectParamAbs8,
                "Sweep Global Section Effect Accent 8 Absolute: Enter an accent value and the number of beats to spread the transition across.",
                LFinalValue,
                LDuration),

            /* <1l> = section effect parameter adjust, <2xs> = # of beats to get there */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepSectionEffectParamRel8,
                "Sweep Global Section Effect Accent 8 Relative: Enter an accent adjust value and the number of beats to spread the transition across.",
                LFinalAdjustment,
                LDuration),

            /* <string> holds sequence name */
            new XOneStr(
                NoteCommands.eCmdSequenceBegin,
                "Sequence Begin: Enter a new sequence name.",
                "Name:"),

            new XZero(
                NoteCommands.eCmdSequenceEnd),

            /* <string1> holds track/group name, <string2> hold sequence name */
            new XTwoStr(
                NoteCommands.eCmdSetSequence,
                "Set Sequence: Enter name of track or group to control and name of sequence to run.",
                "Track/Group Name:",
                "Sequence Name:"),

            /* <string1> holds track/group name, <string2> hold sequence name */
            new XTwoStr(
                NoteCommands.eCmdSetSequenceDeferred,
                "Set Sequence On Next Loop: Enter name of track or group to control and name of sequence to run.",
                "Track/Group Name:",
                "Sequence Name:"),

            /* <string1> holds track/group name */
            new XOneStr(
                NoteCommands.eCmdEndSequencing,
                "End Sequencing: Enter name of track or group to halt.",
                "Name:"),

            /* <string1> holds track/group, <1l> holds beats */
            new XOneL(
                NoteCommands.eCmdSkip,
                "Skip: Enter number of beats to skip.",
                "Beats to Skip:"),

            /* <1l> = probability of ignoring next command */
            new XOneL(
                NoteCommands.eCmdIgnoreNextCmd,
                "Skip Next Command: Enter probability of skipping next command.",
                "Probability:"),

            /* <string> holds target track/group name */
            new XOneStr(
                NoteCommands.eCmdRedirect,
                "Redirect Begin: Enter name of track or group to control.",
                "Name:"),

            new XZero(
                NoteCommands.eCmdRedirectEnd),

            new XZero(
                NoteCommands.eCmdReleaseAll1),

            new XZero(
                NoteCommands.eCmdReleaseAll2),

            new XZero(
                NoteCommands.eCmdReleaseAll3),

            /* restore the portamento to the default for the channel */
            new XZero(
                NoteCommands.eCmdRestorePortamento),

            /* set the portamento to the specified level in <1l> */
            new XOneL(
                NoteCommands.eCmdSetPortamento,
                "Set Portamento: Enter an overall portamento level value.",
                "Portamento:"),

            /* add <1l> to the portamento control */
            new XOneL(
                NoteCommands.eCmdIncPortamento,
                "Adjust Portamento: Enter a portamento adjustment value.",
                "Portamento Adjustment:"),

            /* <1l> = new portamento, <2xs> = # of beats to reach it */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepPortamentoAbs,
                "Sweep Portamento Absolute: Enter a new portamento value and the number of beats to spread the transition across.",
                "Final Portamento:",
                LDuration),

            /* <1l> = portamento adjust, <2xs> = # of beats to reach it */
            new XOneLTwoXS(
                NoteCommands.eCmdSweepPortamentoRel,
                "Sweep Portamento Relative: Enter a portamento adjustment value and the number of beats to spread the transition across.",
                "Final Portamento Adjustment:",
                LDuration),
        };
    }
}
