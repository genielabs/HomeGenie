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

using HomeGenie.Automation;
using MIG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using HomeGenie.Service;
using System.IO;
using System.Net;
using System.Xml.Serialization;
using Newtonsoft.Json;
using HomeGenie.Service.Constants;
using HomeGenie.Automation.Scheduler;

namespace HomeGenie.Service.Handlers
{
    public class Automation
    {
        private HomeGenieService _hg;
        public Automation(HomeGenieService hg)
        {
            _hg = hg;
        }

        public void ProcessRequest(MIGClientRequest request, MIGInterfaceCommand migcmd)
        {
            string streamcontent = "";
            //
            _hg.ExecuteAutomationRequest(migcmd);
            //
            if (migcmd.command.StartsWith("Macro."))
            {
                switch (migcmd.command)
                {
                    case "Macro.Record":
                        _hg.ProgramEngine.MacroRecorder.RecordingEnable();
                        break;
                    case "Macro.Save":
                        ProgramBlock pb = _hg.ProgramEngine.MacroRecorder.SaveMacro(migcmd.GetOption(1));
                        migcmd.response = pb.Address.ToString();
                        break;
                    case "Macro.Discard":
                        _hg.ProgramEngine.MacroRecorder.RecordingDisable();
                        break;
                    case "Macro.SetDelay":
                        switch (migcmd.GetOption(0).ToLower())
                        {
                            case "none":
                                _hg.ProgramEngine.MacroRecorder.DelayType = MacroDelayType.None;
                                break;

                            case "mimic":
                                _hg.ProgramEngine.MacroRecorder.DelayType = MacroDelayType.Mimic;
                                break;

                            case "fixed":
                                double secs = double.Parse(migcmd.GetOption(1), System.Globalization.CultureInfo.InvariantCulture);
                                _hg.ProgramEngine.MacroRecorder.DelayType = MacroDelayType.Fixed;
                                _hg.ProgramEngine.MacroRecorder.DelaySeconds = secs;
                                break;
                        }
                        break;
                    case "Macro.GetDelay":
                        migcmd.response = "[{ DelayType : '" + _hg.ProgramEngine.MacroRecorder.DelayType + "', DelayOptions : '" + _hg.ProgramEngine.MacroRecorder.DelaySeconds + "' }]";
                        break;
                }
            }
            else if (migcmd.command.StartsWith("Scheduling."))
            {
                switch (migcmd.command)
                {
                    case "Scheduling.Add":
                    case "Scheduling.Update":
                        SchedulerItem item = _hg.ProgramEngine.SchedulerService.AddOrUpdate(migcmd.GetOption(0), migcmd.GetOption(1).Replace("|", "/"));
                        item.ProgramId = migcmd.GetOption(2);
                        _hg.UpdateSchedulerDatabase();
                        break;
                    case "Scheduling.Delete":
                        _hg.ProgramEngine.SchedulerService.Remove(migcmd.GetOption(0));
                        _hg.UpdateSchedulerDatabase();
                        break;
                    case "Scheduling.Enable":
                        _hg.ProgramEngine.SchedulerService.Enable(migcmd.GetOption(0));
                        _hg.UpdateSchedulerDatabase();
                        break;
                    case "Scheduling.Disable":
                        _hg.ProgramEngine.SchedulerService.Disable(migcmd.GetOption(0));
                        _hg.UpdateSchedulerDatabase();
                        break;
                    case "Scheduling.Get":
                        migcmd.response = JsonConvert.SerializeObject(_hg.ProgramEngine.SchedulerService.Get(migcmd.GetOption(0)));
                        break;
                    case "Scheduling.List":
                        migcmd.response = JsonConvert.SerializeObject(_hg.ProgramEngine.SchedulerService.Items);
                        break;
                }
            }
            else if (migcmd.command.StartsWith("Programs."))
            {
                if (migcmd.command != "Programs.Import")
                {
                    streamcontent = new StreamReader(request.InputStream).ReadToEnd();
                }
                //
                ProgramBlock cp = null;
                //
                switch (migcmd.command)
                {
                    case "Programs.Import":
                        string archivename = "homegenie_program_import.hgx";
                        if (File.Exists(archivename)) File.Delete(archivename);
                        //
                        Encoding enc = (request.Context as HttpListenerContext).Request.ContentEncoding;
                        string boundary = MIG.Gateways.WebServiceUtility.GetBoundary((request.Context as HttpListenerContext).Request.ContentType);
                        MIG.Gateways.WebServiceUtility.SaveFile(enc, boundary, request.InputStream, archivename);
                        //
                        XmlSerializer mserializer = new XmlSerializer(typeof(ProgramBlock));
                        StreamReader mreader = new StreamReader(archivename);
                        ProgramBlock programblock = (ProgramBlock)mserializer.Deserialize(mreader);
                        mreader.Close();
                        //
                        programblock.Address = _hg.ProgramEngine.GeneratePid();
                        programblock.Group = migcmd.GetOption(0);
                        _hg.ProgramEngine.ProgramAdd(programblock);
                        //
                        //TODO: move program compilation into an method of ProgramEngine and also apply to Programs.Update
                        programblock.IsEnabled = false;
                        programblock.ScriptErrors = "";
                        programblock.ScriptAssembly = null;
                        //
                        // DISABLED IN FLAVOUR OF USER ASSISTED ENABLING, to prevent malicious scripts to start automatically
                        // in case of c# script type, we have to recompile it
                        /*
                        if (programblock.Type.ToLower() == "csharp" && programblock.IsEnabled)
                        {
                            System.CodeDom.Compiler.CompilerResults res = _mastercontrolprogram.CompileScript(programblock);
                            //
                            if (res.Errors.Count == 0)
                            {
                                programblock.ScriptAssembly = res.CompiledAssembly;
                            }
                            else
                            {
                                int sourcelines = programblock.ScriptSource.Split('\n').Length;
                                foreach (System.CodeDom.Compiler.CompilerError ce in res.Errors)
                                {
                                    //if (!ce.IsWarning)
                                    {
                                        int errline = (ce.Line - 16);
                                        string blocktype = "Code";
                                        if (errline >= sourcelines + 7)
                                        {
                                            errline -= (sourcelines + 7);
                                            blocktype = "Condition";
                                        }
                                        string errmsg = "Line " + errline + ", Column " + ce.Column + " " + ce.ErrorText + " (" + ce.ErrorNumber + ")";
                                        programblock.ScriptErrors += errmsg + " (" + blocktype + ")" + "\n";
                                    }
                                }
                            }
                        }
                        //
                        cmd.response = programblock.ScriptErrors;
                        */
                        //
                        _hg.UpdateProgramsDatabase();
                        //cmd.response = JsonHelper.GetSimpleResponse(programblock.Address);
                        migcmd.response = programblock.Address.ToString();
                        break;

                    case "Programs.Export":
                        cp = _hg.ProgramEngine.Programs.Find(p => p.Address == int.Parse(migcmd.GetOption(0)));
                        System.Xml.XmlWriterSettings ws = new System.Xml.XmlWriterSettings();
                        ws.Indent = true;
                        System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(typeof(ProgramBlock));
                        StringBuilder sb = new StringBuilder();
                        System.Xml.XmlWriter wri = System.Xml.XmlWriter.Create(sb, ws);
                        x.Serialize(wri, cp);
                        wri.Close();
                        migcmd.response = sb.ToString();
                        //
                        (request.Context as HttpListenerContext).Response.AddHeader("Content-Disposition", "attachment; filename=\"" + cp.Address + "-" + cp.Name.Replace(" ", "_") + ".hgx\"");
                        break;

                    case "Programs.List":
                        List<ProgramBlock> prgs = new List<ProgramBlock>(_hg.ProgramEngine.Programs);
                        prgs.Sort(delegate(ProgramBlock p1, ProgramBlock p2)
                        {
                            string c1 = p1.Name + " " + p1.Address;
                            string c2 = p2.Name + " " + p2.Address;
                            return c1.CompareTo(c2);
                        });
                        migcmd.response = JsonConvert.SerializeObject(prgs);
                        break;

                    case "Programs.Add":
                        ProgramBlock pb = new ProgramBlock() { Group = migcmd.GetOption(0), Name = streamcontent, Type = "Wizard", ScriptCondition = "// A \"return true;\" statement at any point of this code block, will cause the program to run.\n// For manually activated program, just leave a \"return false\" statement here.\n\nreturn false;\n" };
                        pb.Address = _hg.ProgramEngine.GeneratePid();
                        _hg.ProgramEngine.ProgramAdd(pb);
                        _hg.UpdateProgramsDatabase();
                        migcmd.response = JsonHelper.GetSimpleResponse(pb.Address.ToString());
                        break;

                    case "Programs.Delete":
                        cp = _hg.ProgramEngine.Programs.Find(p => p.Address == int.Parse(migcmd.GetOption(0)));
                        if (cp != null)
                        {
                            //TODO: remove groups associations as well
                            cp.IsEnabled = false;
                            _hg.ProgramEngine.ProgramRemove(cp);
                            _hg.UpdateProgramsDatabase();
                            // remove associated module entry
                            _hg.Modules.RemoveAll(m => m.Domain == Domains.HomeAutomation_HomeGenie_Automation && m.Address == cp.Address.ToString());
                            _hg.UpdateModulesDatabase();
                        }
                        break;

                    case "Programs.Compile":
                    case "Programs.Update":
                        programblock = JsonConvert.DeserializeObject<ProgramBlock>(streamcontent);
                        cp = _hg.ProgramEngine.Programs.Find(p => p.Address == programblock.Address);
                        //
                        if (cp == null)
                        {
                            programblock.Address = _hg.ProgramEngine.GeneratePid();
                            _hg.ProgramEngine.ProgramAdd(programblock);
                        }
                        else
                        {
                            if (cp.Type.ToLower() != programblock.Type.ToLower())
                            {
                                cp.ScriptAssembly = null; // dispose assembly and interrupt current task
                            }
                            cp.Type = programblock.Type;
                            cp.Group = programblock.Group;
                            cp.Name = programblock.Name;
                            cp.Description = programblock.Description;
                            cp.IsEnabled = programblock.IsEnabled;
                            cp.ScriptCondition = programblock.ScriptCondition;
                            cp.ScriptSource = programblock.ScriptSource;
                            cp.Commands = programblock.Commands;
                            cp.Conditions = programblock.Conditions;
                            cp.ConditionType = programblock.ConditionType;
                            // reset last condition evaluation status
                            cp.LastConditionEvaluationResult = false;
                        }

                        if (migcmd.command == "Programs.Compile" && cp.Type.ToLower() == "csharp") // && programblock.IsEnabled)
                        {
                            cp.ScriptAssembly = null; // dispose assembly and interrupt current task
                            cp.IsEnabled = false;
                            cp.ScriptErrors = "";
                            //
                            System.CodeDom.Compiler.CompilerResults res = _hg.ProgramEngine.CompileScript(cp);
                            //
                            if (res.Errors.Count == 0)
                            {
                                cp.ScriptAssembly = res.CompiledAssembly;
                            }
                            else
                            {
                                int sourcelines = cp.ScriptSource.Split('\n').Length;
                                foreach (System.CodeDom.Compiler.CompilerError ce in res.Errors)
                                {
                                    //if (!ce.IsWarning)
                                    {
                                        int errline = (ce.Line - 16);
                                        string blocktype = "Code";
                                        if (errline >= sourcelines + 7)
                                        {
                                            errline -= (sourcelines + 7);
                                            blocktype = "Condition";
                                        }
                                        string errmsg = "Line " + errline + ", Column " + ce.Column + " " + ce.ErrorText + " (" + ce.ErrorNumber + ")";
                                        cp.ScriptErrors += errmsg + " (" + blocktype + ")" + "\n";
                                    }
                                }
                            }
                            //
                            cp.IsEnabled = programblock.IsEnabled;
                            //
                            migcmd.response = cp.ScriptErrors;
                        }

                        _hg.UpdateProgramsDatabase();
                        //
                        _hg._modules_refresh_virtualmods();
                        _hg._modules_sort();
                        break;

                    case "Programs.Run":
                        cp = _hg.ProgramEngine.Programs.Find(p => p.Address == int.Parse(migcmd.GetOption(0)));
                        if (cp != null)
                        {
                            _hg.ProgramEngine.Run(cp, migcmd.GetOption(1));
                        }
                        break;

                    case "Programs.Break":
                        cp = _hg.ProgramEngine.Programs.Find(p => p.Address == int.Parse(migcmd.GetOption(0)));
                        if (cp != null)
                        {
                            cp.Stop();
                            _hg.UpdateProgramsDatabase();
                        }
                        break;

                    case "Programs.Enable":
                        cp = _hg.ProgramEngine.Programs.Find(p => p.Address == int.Parse(migcmd.GetOption(0)));
                        if (cp != null)
                        {
                            cp.IsEnabled = true;
                            _hg.UpdateProgramsDatabase();
                        }
                        break;

                    case "Programs.Disable":
                        cp = _hg.ProgramEngine.Programs.Find(p => p.Address == int.Parse(migcmd.GetOption(0)));
                        if (cp != null)
                        {
                            cp.IsEnabled = false;
                            try
                            {
                                cp.Stop();
                            }
                            catch { }
                            _hg.UpdateProgramsDatabase();
                        }
                        break;
                }

            }
        }

    }
}
