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
using System.IO;
using System.Net;
using System.Xml.Serialization;
using Newtonsoft.Json;
using HomeGenie.Service.Constants;
using HomeGenie.Automation.Scheduler;
using CronExpressionDescriptor;
using HomeGenie.Automation.Engines;
using HomeGenie.Data;
using HomeGenie.Data.UI;
using Innovative.SolarCalculator;

namespace HomeGenie.Service.Handlers
{
    public class Automation
    {
        private HomeGenieService homegenie;

        public Automation(HomeGenieService hg)
        {
            homegenie = hg;
        }

        public void ProcessRequest(MigClientRequest request)
        {
            var migCommand = request.Command;
            string streamContent = "";
            ProgramBlock currentProgram;
            ProgramBlock newProgram;
            string sketchFile = "", sketchFolder = "";
            //
            request.ResponseData = new ResponseStatus(Status.Ok);
            if (homegenie.ExecuteAutomationRequest(migCommand)) {
                // TODO: should it just return if the request has been already processed?
            }
            if (migCommand.Command.StartsWith("Macro."))
            {
                switch (migCommand.Command)
                {
                case "Macro.Record":
                    homegenie.ProgramManager.MacroRecorder.RecordingEnable();
                    break;
                case "Macro.Save":
                    newProgram = homegenie.ProgramManager.MacroRecorder.SaveMacro(migCommand.GetOption(1));
                    request.ResponseData = newProgram.Address.ToString();
                    break;
                case "Macro.Discard":
                    homegenie.ProgramManager.MacroRecorder.RecordingDisable();
                    break;
                case "Macro.SetDelay":
                    switch (migCommand.GetOption(0).ToLower())
                    {
                    case "none":
                        homegenie.ProgramManager.MacroRecorder.DelayType = MacroDelayType.None;
                        break;

                    case "mimic":
                        homegenie.ProgramManager.MacroRecorder.DelayType = MacroDelayType.Mimic;
                        break;

                    case "fixed":
                        double secs = double.Parse(
                            migCommand.GetOption(1),
                            System.Globalization.CultureInfo.InvariantCulture
                        );
                        homegenie.ProgramManager.MacroRecorder.DelayType = MacroDelayType.Fixed;
                        homegenie.ProgramManager.MacroRecorder.DelaySeconds = secs;
                        break;
                    default:
                        request.ResponseData = new ResponseStatus(Status.Error);
                        break;
                    }
                    break;
                case "Macro.GetDelay":
                    request.ResponseData = "{ \"DelayType\" : \"" + homegenie.ProgramManager.MacroRecorder.DelayType + "\", \"DelayOptions\" : \"" + homegenie.ProgramManager.MacroRecorder.DelaySeconds + "\" }";
                    break;
                default:
                    request.ResponseData = new ResponseStatus(Status.Error);
                    break;
                }
            }
            else if (migCommand.Command.StartsWith("Scheduling."))
            {
                switch (migCommand.Command)
                {
                case "Scheduling.Add":
                case "Scheduling.Update":
                    var newSchedule = JsonConvert.DeserializeObject<SchedulerItem>(request.RequestText);
                    var item = homegenie.ProgramManager.SchedulerService.AddOrUpdate(
                        newSchedule.Name,
                        newSchedule.CronExpression,
                        newSchedule.Data,
                        newSchedule.Description,
                        newSchedule.Script
                    );
                    if (newSchedule.BoundDevices != null)
                    {
                        item.BoundDevices = newSchedule.BoundDevices;
                    }
                    if (newSchedule.BoundModules != null)
                    {
                        item.BoundModules = newSchedule.BoundModules;
                    }
                    homegenie.UpdateSchedulerDatabase();
                    break;
                case "Scheduling.ModuleUpdate":
                    var mod = homegenie.Modules.Find((m) =>
                        m.Domain == migCommand.GetOption(0) && m.Address == migCommand.GetOption(1));
                    if (mod == null) break;
                    var scheduling = JsonConvert.DeserializeObject<dynamic>(request.RequestText);
                    for (int i = 0; i < scheduling.include.Count; i++)
                    {
                        string name = scheduling.include[i].Value.ToString();
                        var schedulerItem = homegenie.ProgramManager.SchedulerService.Get(name);
                        if (schedulerItem != null)
                        {
                            schedulerItem.BoundModules.RemoveAll((mr) =>
                                mr.Domain == mod.Domain && mr.Address == mod.Address);
                            schedulerItem.BoundModules.Add(new ModuleReference()
                            {
                                Domain = mod.Domain,
                                Address = mod.Address
                            });
                        }
                    }
                    for (int i = 0; i < scheduling.exclude.Count; i++)
                    {
                        string name = scheduling.exclude[i].Value.ToString();
                        var schedulerItem = homegenie.ProgramManager.SchedulerService.Get(name);
                        if (schedulerItem != null)
                        {
                            schedulerItem.BoundModules.RemoveAll((mr) =>
                                mr.Domain == mod.Domain && mr.Address == mod.Address);
                        }
                    }
                    homegenie.UpdateSchedulerDatabase();
                    break;
                case "Scheduling.Get":
                    request.ResponseData = homegenie.ProgramManager.SchedulerService.Get(migCommand.GetOption(0));
                    break;
                case "Scheduling.Enable":
                    homegenie.ProgramManager.SchedulerService.Enable(migCommand.GetOption(0));
                    homegenie.UpdateSchedulerDatabase();
                    break;
                case "Scheduling.Disable":
                    homegenie.ProgramManager.SchedulerService.Disable(migCommand.GetOption(0));
                    homegenie.UpdateSchedulerDatabase();
                    break;
                case "Scheduling.Delete":
                    homegenie.ProgramManager.SchedulerService.Remove(migCommand.GetOption(0));
                    homegenie.UpdateSchedulerDatabase();
                    break;
                case "Scheduling.ListOccurrences":
                    int hours = 24;
                    int.TryParse(migCommand.GetOption(0), out hours);
                    DateTime dateStart = DateTime.Today.ToUniversalTime();
                    string startFrom = migCommand.GetOption(1);
                    if (!String.IsNullOrWhiteSpace(startFrom))
                        dateStart = Utility.JavascriptToDate(long.Parse(startFrom));
                    string cronExpression = migCommand.GetOption(2);
                    List<dynamic> nextList = new List<dynamic>();
                    if (!String.IsNullOrEmpty(cronExpression))
                    {
                        var evt = new
                        {
                            CronExpression = cronExpression,
                            Occurrences = new List<double>()
                        };
                        var d = dateStart;
                        var dateEnd = dateStart.AddHours(hours);
                        var occurs = homegenie.ProgramManager.SchedulerService.GetScheduling(dateStart, dateEnd, cronExpression);
                        occurs.Sort();
                        foreach (var dt in occurs)
                        {
                            evt.Occurrences.Add(Utility.DateToJavascript(dt.ToUniversalTime()));
                        }
                        if (evt.Occurrences.Count > 0)
                        {
                            nextList.Add(evt);
                        }
                        
                    }
                    else
                    {
                        for (int s = 0; s < homegenie.ProgramManager.SchedulerService.Items.Count; s++)
                        {
                            var ce = homegenie.ProgramManager.SchedulerService.Items[s];
                            if (!ce.IsEnabled)
                                continue;
                            var evt = new { ce.Name, ce.Description, RunScript = !String.IsNullOrWhiteSpace(ce.Script), Occurrences = new List<double>() };
                            var d = dateStart;
                            var dateEnd = dateStart.AddHours(hours);
                            var occurs = homegenie.ProgramManager.SchedulerService.GetScheduling(dateStart, dateEnd, ce.CronExpression);
                            occurs.Sort();
                            foreach (var dt in occurs)
                            {
                                evt.Occurrences.Add(Utility.DateToJavascript(dt.ToUniversalTime()));
                            }
                            if (evt.Occurrences.Count > 0)
                            {
                                nextList.Add(evt);
                            }
                        }
                    }
                    request.ResponseData = nextList;
                    break;
                case "Scheduling.List":
                    homegenie.ProgramManager.SchedulerService
                        .Items.Sort((s1, s2) => String.Compare(s1.Name, s2.Name, StringComparison.Ordinal));
                    request.ResponseData = homegenie.ProgramManager.SchedulerService.Items;
                    break;
                case "Scheduling.Describe":
                    var cronDescription = "";
                    try {
                        cronDescription = ExpressionDescriptor.GetDescription(migCommand.GetOption(0).Trim());
                        cronDescription = Char.ToLowerInvariant(cronDescription[0]) + cronDescription.Substring(1);
                    } catch { }
                    request.ResponseData = new ResponseText(cronDescription);
                    break;
                case "Scheduling.SolarTimes":
                    var solarTimes = new SolarTimes(DateTime.Now, homegenie.ProgramManager.SchedulerService.Location["latitude"].Value, homegenie.ProgramManager.SchedulerService.Location["longitude"].Value);
                    request.ResponseData = solarTimes;
                    break;
                default:
                    request.ResponseData = new ResponseStatus(Status.Error);
                    break;
                }
            }
            else if (migCommand.Command.StartsWith("Programs."))
            {
                if (migCommand.Command != "Programs.Import")
                {
                    streamContent = request.RequestText;
                }
                //
                switch (migCommand.Command)
                {
                case "Programs.Import":
                    string archiveName = "homegenie_program_import.hgx";
                    if (File.Exists(archiveName))
                        File.Delete(archiveName);
                    //
                    MIG.Gateways.WebServiceUtility.SaveFile(request.RequestData, archiveName);
                    int newPid = homegenie.ProgramManager.GeneratePid();
                    newProgram = homegenie.PackageManager.ProgramImport(newPid, archiveName, migCommand.GetOption(0));
                    homegenie.UpdateProgramsDatabase();
                    //migCommand.response = JsonHelper.GetSimpleResponse(programblock.Address);
                    request.ResponseData = newProgram.Address.ToString();
                    break;

                case "Programs.Export":
                    currentProgram = homegenie.ProgramManager.Programs.Find(p => p.Address == int.Parse(migCommand.GetOption(0)));
                    string filename = currentProgram.Address + "-" + currentProgram.Name.Replace(" ", "_");
                    //
                    var writerSettings = new System.Xml.XmlWriterSettings();
                    writerSettings.Indent = true;
                    writerSettings.Encoding = Encoding.UTF8;
                    var programSerializer = new XmlSerializer(typeof(ProgramBlock));
                    var builder = new StringBuilder();
                    var writer = System.Xml.XmlWriter.Create(builder, writerSettings);
                    programSerializer.Serialize(writer, currentProgram);
                    writer.Close();
                    //
                    if (currentProgram.Type.ToLower() == "arduino")
                    {
                        string arduinoBundle = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                                            Utility.GetTmpFolder(),
                                                           "export",
                                                            filename + ".zip");
                        if (File.Exists(arduinoBundle))
                        {
                            File.Delete(arduinoBundle);
                        }
                        else if (!Directory.Exists(Path.GetDirectoryName(arduinoBundle)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(arduinoBundle));
                        }
                        string mainProgramFile = Path.Combine(Path.GetDirectoryName(arduinoBundle), "program.hgx");
                        File.WriteAllText(
                            mainProgramFile,
                            builder.ToString()
                        );
                        Utility.AddFileToZip(arduinoBundle, mainProgramFile, "program.hgx");
                        sketchFolder = Path.Combine("programs", "arduino", currentProgram.Address.ToString());
                        foreach (string f in Directory.GetFiles(sketchFolder))
                        {
                            if (!Path.GetFileName(f).StartsWith("sketch_"))
                            {
                                Utility.AddFileToZip(
                                    arduinoBundle,
                                    Path.Combine(sketchFolder, Path.GetFileName(f)),
                                    Path.Combine(
                                        "src",
                                        Path.GetFileName(f)
                                    )
                                );
                            }
                        }
                        //
                        byte[] bundleData = File.ReadAllBytes(arduinoBundle);
                        (request.Context.Data as HttpListenerContext).Response.AddHeader(
                            "Content-Disposition",
                            "attachment; filename=\"" + filename + ".zip\""
                            );
                        (request.Context.Data as HttpListenerContext).Response.OutputStream.Write(bundleData, 0, bundleData.Length);
                    }
                    else
                    {
                        (request.Context.Data as HttpListenerContext).Response.AddHeader(
                            "Content-Disposition",
                            "attachment; filename=\"" + filename + ".hgx\""
                        );
                        request.ResponseData = builder.ToString();
                    }
                    break;

                case "Programs.List":
                    var programList = new List<ProgramBlock>(homegenie.ProgramManager.Programs);
                    programList.Sort(delegate(ProgramBlock p1, ProgramBlock p2)
                    {
                        string c1 = p1.Name + " " + p1.Address;
                        string c2 = p2.Name + " " + p2.Address;
                        return c1.CompareTo(c2);
                    });
                    request.ResponseData = programList;
                    break;

                case "Programs.Get":
                    try
                    {
                        var prg = homegenie.ProgramManager.Programs.Find(p => p.Address == int.Parse(migCommand.GetOption(0)));
                        var settings = new JsonSerializerSettings{ Formatting = Formatting.Indented };
                        request.ResponseData = JsonConvert.SerializeObject(prg, settings);
                    }
                    catch (Exception ex)
                    {
                        request.ResponseData = new ResponseText("ERROR: \n" + ex.Message + "\n\n" + ex.StackTrace);
                    }
                    break;

                case "Programs.Add":
                    newProgram = new ProgramBlock() {
                        Group = migCommand.GetOption(0),
                        Name = streamContent,
                        Type = "Wizard"
                    };
                    newProgram.Address = homegenie.ProgramManager.GeneratePid();
                    homegenie.ProgramManager.ProgramAdd(newProgram);
                    homegenie.UpdateProgramsDatabase();
                    request.ResponseData = new ResponseText(newProgram.Address.ToString());
                    break;

                case "Programs.Clone":
                    var copy = homegenie.ProgramManager.ProgramClone(int.Parse(migCommand.GetOption(0)));
                    homegenie.UpdateProgramsDatabase();
                    request.ResponseData = new ResponseText(copy.Address.ToString());
                    break;

                case "Programs.Delete":
                    currentProgram = homegenie.ProgramManager.Programs.Find(p => p.Address == int.Parse(migCommand.GetOption(0)));
                    if (currentProgram != null)
                    {
                        // TODO: remove groups associations as well
                        homegenie.ProgramManager.ProgramRemove(currentProgram);
                        homegenie.UpdateProgramsDatabase();
                        // remove associated module entry
                        homegenie.Modules.RemoveAll(m => m.Domain == Domains.HomeAutomation_HomeGenie_Automation && m.Address == currentProgram.Address.ToString());
                        homegenie.UpdateModulesDatabase();
                    }
                    break;

                case "Programs.Compile":
                case "Programs.Update":
                    newProgram = JsonConvert.DeserializeObject<ProgramBlock>(streamContent);
                    currentProgram = homegenie.ProgramManager.Programs.Find(p => p.Address == newProgram.Address);
                    //
                    if (currentProgram == null)
                    {
                        newProgram.Address = homegenie.ProgramManager.GeneratePid();
                        homegenie.ProgramManager.ProgramAdd(newProgram);
                    }
                    else
                    {
                        var typeChanged = !string.Equals(currentProgram.Type, newProgram.Type, StringComparison.CurrentCultureIgnoreCase);
                        currentProgram.Type = newProgram.Type;
                        currentProgram.Group = newProgram.Group;
                        currentProgram.Name = newProgram.Name;
                        currentProgram.Description = newProgram.Description;
                        currentProgram.AutoRestartEnabled = newProgram.AutoRestartEnabled;
                        if (typeChanged)
                            currentProgram.Engine.SetHost(homegenie);
                        currentProgram.IsEnabled = newProgram.IsEnabled;
                        currentProgram.ScriptSetup = newProgram.ScriptSetup;
                        currentProgram.ScriptSource = newProgram.ScriptSource;
                    }
                    //
                    if (migCommand.Command == "Programs.Compile")
                    {
                        // reset previous error status
                        currentProgram.IsEnabled = false;
                        currentProgram.Engine.StopProgram();
                        currentProgram.ScriptErrors = "";
                        //
                        List<ProgramError> errors = homegenie.ProgramManager.ProgramCompile(currentProgram);
                        //
                        currentProgram.IsEnabled = newProgram.IsEnabled && errors.Count == 0;
                        currentProgram.ScriptErrors = JsonConvert.SerializeObject(errors);
                        request.ResponseData = currentProgram.ScriptErrors;
                    }
                    //
                    homegenie.UpdateProgramsDatabase();
                    //
                    homegenie.modules_RefreshPrograms();
                    homegenie.modules_RefreshVirtualModules();
                    //homegenie.modules_Sort();
                    break;

                case "Programs.Arduino.FileLoad":
                    sketchFolder = Path.GetDirectoryName(ArduinoAppFactory.GetSketchFile(migCommand.GetOption(0)));
                    sketchFile = migCommand.GetOption(1);
                    if (sketchFile == "main")
                    {
                        // "main" is a special keyword to indicate the main program sketch file
                        sketchFile = ArduinoAppFactory.GetSketchFile(migCommand.GetOption(0));
                    }
                    sketchFile = Path.Combine(sketchFolder, Path.GetFileName(sketchFile));
                    request.ResponseData = new ResponseText(File.ReadAllText(sketchFile));
                    break;

                case "Programs.Arduino.FileSave":
                    sketchFolder = Path.GetDirectoryName(ArduinoAppFactory.GetSketchFile(migCommand.GetOption(0)));
                    sketchFile = Path.Combine(sketchFolder, Path.GetFileName(migCommand.GetOption(1)));
                    File.WriteAllText(sketchFile, streamContent);
                    break;

                case "Programs.Arduino.FileAdd":
                    sketchFolder = Path.GetDirectoryName(ArduinoAppFactory.GetSketchFile(migCommand.GetOption(0)));
                    if (!Directory.Exists(sketchFolder))
                        Directory.CreateDirectory(sketchFolder);
                    sketchFile = Path.Combine(sketchFolder, Path.GetFileName(migCommand.GetOption(1)));
                    if (File.Exists(sketchFile))
                    {
                        request.ResponseData = new ResponseText("EXISTS");
                    }
                    else if (!ArduinoAppFactory.IsValidProjectFile(sketchFile))
                    {
                        request.ResponseData = new ResponseText("INVALID_NAME");
                    }
                    else
                    {
                        StreamWriter sw = File.CreateText(sketchFile);
                        sw.Close();
                        sw.Dispose();
                        sw = null;
                        request.ResponseData = new ResponseText("OK");
                    }
                    break;

                case "Programs.Arduino.FileDelete":
                    sketchFolder = Path.GetDirectoryName(ArduinoAppFactory.GetSketchFile(migCommand.GetOption(0)));
                    sketchFile = Path.Combine(sketchFolder, Path.GetFileName(migCommand.GetOption(1)));
                    if (!File.Exists(sketchFile))
                    {
                        request.ResponseData = new ResponseText("NOT_FOUND");
                    }
                    else
                    {
                        File.Delete(sketchFile);
                        request.ResponseData = new ResponseText("OK");
                    }
                    break;

                case "Programs.Arduino.FileList":
                    sketchFolder = Path.GetDirectoryName(ArduinoAppFactory.GetSketchFile(migCommand.GetOption(0)));
                    List<string> files = new List<string>();
                    foreach (string f in Directory.GetFiles(sketchFolder))
                    {
                        if (ArduinoAppFactory.IsValidProjectFile(f))
                        {
                            files.Add(Path.GetFileName(f));
                        }
                    }
                    request.ResponseData = files;
                    break;

                case "Programs.Run":
                    currentProgram = homegenie.ProgramManager.Programs.Find(p => p.Address == int.Parse(migCommand.GetOption(0)));
                    if (currentProgram != null)
                    {
                        // clear any runtime errors before running
                        currentProgram.ScriptErrors = "";
                        homegenie.ProgramManager.RaiseProgramModuleEvent(
                            currentProgram,
                            Properties.RuntimeError,
                            ""
                        );
                        currentProgram.IsEnabled = true;
                        System.Threading.Thread.Sleep(500);
                        ProgramRun(migCommand.GetOption(0), migCommand.GetOption(1));
                    }
                    break;

                case "Programs.Toggle":
                    currentProgram = ProgramToggle(migCommand.GetOption(0), migCommand.GetOption(1));
                    break;

                case "Programs.Break":
                    currentProgram = ProgramBreak(migCommand.GetOption(0));
                    break;

                case "Programs.Restart":
                    currentProgram = homegenie.ProgramManager.Programs.Find(p => p.Address == int.Parse(migCommand.GetOption(0)));
                    if (currentProgram != null)
                    {
                        currentProgram.IsEnabled = false;
                        try
                        {
                            currentProgram.Engine.StopProgram();
                        }
                        catch
                        {
                        }
                        currentProgram.IsEnabled = true;
                        homegenie.UpdateProgramsDatabase();
                    }
                    break;

                case "Programs.Enable":
                    currentProgram = homegenie.ProgramManager.Programs.Find(p => p.Address == int.Parse(migCommand.GetOption(0)));
                    if (currentProgram != null)
                    {
                        currentProgram.IsEnabled = true;
                        homegenie.UpdateProgramsDatabase();
                    }
                    break;

                case "Programs.Disable":
                    currentProgram = homegenie.ProgramManager.Programs.Find(p => p.Address == int.Parse(migCommand.GetOption(0)));
                    if (currentProgram != null)
                    {
                        currentProgram.IsEnabled = false;
                        try
                        {
                            currentProgram.Engine.StopProgram();
                        }
                        catch
                        {
                        }
                        homegenie.UpdateProgramsDatabase();
                    }
                    break;
                case "Programs.OptionsGet":
                    var programModule = homegenie.Modules.Find(m =>
                        m.Domain == migCommand.GetOption(0) && m.Address == migCommand.GetOption(1));
                    if (programModule != null)
                    {
                        var options = new List<OptionField>();
                        var programOptions = new ModuleOptions()
                        {
                            id = programModule.Address,
                            name = programModule.Name,
                            description = programModule.Description,
                            items = options
                        };
                        programModule.Properties.ForEach((o) =>
                        {
                            if (o.Name.StartsWith("ConfigureOptions."))
                            {
                                var fieldType = o.FieldType.Split(':');
                                options.Add(new OptionField()
                                {
                                    pid = programModule.Address,
                                    type = new OptionFieldType()
                                    {
                                        id = fieldType[0],
                                        options = fieldType.Skip(1).ToList<object>()
                                    },
                                    name = o.Name,
                                    description = o.Description,
                                    field = new ModuleField()
                                    {
                                        key = o.Name,
                                        value = o.Value,
                                        timestamp = o.UpdateTime.ToString("o") 
                                    }
                                });
                            }
                        });
                        options.Sort((o1, o2) => (o1.description).CompareTo(o2.description));
                        request.ResponseData = JsonConvert.SerializeObject(programOptions);
                    }
                    break;
                default:
                    request.ResponseData = new ResponseStatus(Status.Error);
                    break;
                }
            }
            else
            {
                request.ResponseData = new ResponseStatus(Status.Error);
            }
        }

        internal ProgramBlock ProgramRun(string address, string options)
        {
            int pid = 0;
            int.TryParse(address, out pid);
            ProgramBlock program = homegenie.ProgramManager.Programs.Find(p => p.Address == pid);
            if (program != null)
            {
                if (program.IsEnabled)
                {
                    try
                    {
                        program.Engine.StartProgram(options);
                    }
                    catch (Exception e)
                    {
                        HomeGenieService.LogError(e);
                    }
                }
                else
                {
                    homegenie.RaiseEvent(
                        Domains.HomeGenie_System,
                        Domains.HomeAutomation_HomeGenie_Automation,
                        program.Address.ToString(),
                        "Program Error",
                        Properties.RuntimeError,
                        "Program is disabled, cannot run."
                    );
                }
            }
            return program;
        }

        internal ProgramBlock ProgramToggle(string address, string options)
        {
            int pid = 0;
            int.TryParse(address, out pid);
            ProgramBlock program = homegenie.ProgramManager.Programs.Find(p => p.Address == pid);
            if (program != null)
            {
                if (program.IsRunning)
                {
                    ProgramBreak(address);
                    program.IsEnabled = true;
                }
                else
                {
                    if (!program.IsEnabled)
                        program.IsEnabled = true;
                    try
                    {
                        program.Engine.StartProgram(options);
                    }
                    catch
                    {
                    }
                    ;
                }
            }
            return program;
        }

        internal ProgramBlock ProgramBreak(string address)
        {
            int pid = 0;
            int.TryParse(address, out pid);
            ProgramBlock program = homegenie.ProgramManager.Programs.Find(p => p.Address == pid);
            if (program != null)
            {
                program.IsEnabled = false;
                program.Engine.StopProgram();
                homegenie.UpdateProgramsDatabase();
            }
            return program;
        }
    }
}
