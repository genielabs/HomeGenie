/*
    This file is part of HomeGenie Project source code.

    HomeGenie is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    HomeGenie is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with HomeGenie.  If not, see <http://www.gnu.org/licenses/>.  
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: http://homegenie.it
 */

using MIG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HomeGenie.Automation
{
    public enum MacroDelayType
    {
        None = -1,
        Fixed,
        Mimic
    }

    public class MacroRecorder
    {
        // vars for macro record fn
        private bool _ismacrorecordingenabled = false;
        private List<MIGInterfaceCommand> _macrocommands = new List<MIGInterfaceCommand>();
        private DateTime _macrorecordcurrentts = DateTime.Now;
        private double _macrorecorddelayseconds = 1;
        private MacroDelayType _macrorecorddelaytype = MacroDelayType.Fixed;
        private DateTime _macrorecordstartts = DateTime.Now;

        private ProgramEngine _mastercontrolprogram;

        public MacroRecorder(ProgramEngine mcp)
        {
            _mastercontrolprogram = mcp;
        }

        public void RecordingDisable()
        {
            // stop recording
            _ismacrorecordingenabled = false;
        }

        public void RecordingEnable()
        {
            // start recording
            _macrocommands.Clear();
            _macrorecordstartts = _macrorecordcurrentts = DateTime.Now;
            _ismacrorecordingenabled = true;
        }

        public ProgramBlock SaveMacro(string options)
        {
            RecordingDisable();
            //
            ProgramBlock pb = new ProgramBlock();
            pb.Name = "New Macro";
            pb.Address = _mastercontrolprogram.GeneratePid();
            pb.Type = "Wizard";
            foreach (MIGInterfaceCommand mc in _macrocommands)
            {
                ProgramCommand pc = new ProgramCommand();
                pc.Domain = mc.domain;
                pc.Target = mc.nodeid;
                pc.CommandString = mc.command;
                pc.CommandArguments = "";
                if (!string.IsNullOrEmpty(mc.GetOption(0)) && mc.GetOption(0) != "null")
                {
                    //TODO: should we pass entire command option string? migCmd.OptionsString
                    pc.CommandArguments = mc.GetOption(0) + (options != "" && options != "null" ? "/" + options : "");
                }
                pb.Commands.Add(pc);
            }
            _mastercontrolprogram.ProgramAdd(pb);
            //
            return pb;
        }

        public void AddCommand(MIGInterfaceCommand cmd)
        {
            double delaypause = 0;
            switch (_macrorecorddelaytype)
            {
                case MacroDelayType.Mimic:
                    // calculate pause between current and previous command
                    delaypause = new TimeSpan(DateTime.Now.Ticks - _macrorecordcurrentts.Ticks).TotalSeconds;
                    break;

                case MacroDelayType.Fixed:
                    // put a fixed pause
                    delaypause = _macrorecorddelayseconds;
                    break;
            }
            //
            try
            {
                if (delaypause > 0 && _macrocommands.Count > 0)
                {
                    // add a pause command to the macro
                    _macrocommands.Add(new MIGInterfaceCommand("HomeAutomation.HomeGenie/Automation/Program.Pause/" + delaypause.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                }
                _macrocommands.Add(cmd);
            }
            catch (Exception ex)
            {
                //HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "migservice_ServiceRequestPostProcess(...)", ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
            //
            _macrorecordcurrentts = DateTime.Now;

        }

        public bool IsRecordingEnabled
        {
            get { return _ismacrorecordingenabled; }
        }

        public MacroDelayType DelayType
        {
            get { return _macrorecorddelaytype; }
            set { _macrorecorddelaytype = value; }
        }

        public double DelaySeconds
        {
            get { return _macrorecorddelayseconds; }
            set { _macrorecorddelayseconds = value; }
        }

    }
}
