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
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting;

namespace HomeGenie.Service.Handlers
{
    public class Automation
    {
        private HomeGenieService homegenie;

        public Automation(HomeGenieService hg)
        {
            homegenie = hg;
        }

        public void ProcessRequest(MIGClientRequest request, MIGInterfaceCommand migCommand)
        {
            string streamContent = "";
            ProgramBlock currentProgram;
            ProgramBlock newProgram;
            string sketchFile = "", sketchFolder = "";
            //
            homegenie.ExecuteAutomationRequest(migCommand);
            if (migCommand.Command.StartsWith("Macro."))
            {
                switch (migCommand.Command)
                {
                case "Macro.Record":
                    homegenie.ProgramEngine.MacroRecorder.RecordingEnable();
                    break;
                case "Macro.Save":
                    newProgram = homegenie.ProgramEngine.MacroRecorder.SaveMacro(migCommand.GetOption(1));
                    migCommand.Response = newProgram.Address.ToString();
                    break;
                case "Macro.Discard":
                    homegenie.ProgramEngine.MacroRecorder.RecordingDisable();
                    break;
                case "Macro.SetDelay":
                    switch (migCommand.GetOption(0).ToLower())
                    {
                    case "none":
                        homegenie.ProgramEngine.MacroRecorder.DelayType = MacroDelayType.None;
                        break;

                    case "mimic":
                        homegenie.ProgramEngine.MacroRecorder.DelayType = MacroDelayType.Mimic;
                        break;

                    case "fixed":
                        double secs = double.Parse(
                            migCommand.GetOption(1),
                            System.Globalization.CultureInfo.InvariantCulture
                        );
                        homegenie.ProgramEngine.MacroRecorder.DelayType = MacroDelayType.Fixed;
                        homegenie.ProgramEngine.MacroRecorder.DelaySeconds = secs;
                        break;
                    }
                    break;
                case "Macro.GetDelay":
                    migCommand.Response = "[{ DelayType : '" + homegenie.ProgramEngine.MacroRecorder.DelayType + "', DelayOptions : '" + homegenie.ProgramEngine.MacroRecorder.DelaySeconds + "' }]";
                    break;
                }
            }
            else if (migCommand.Command.StartsWith("Scheduling."))
            {
                switch (migCommand.Command)
                {
                case "Scheduling.Add":
                case "Scheduling.Update":
                    var item = homegenie.ProgramEngine.SchedulerService.AddOrUpdate(
                        migCommand.GetOption(0),
                        migCommand.GetOption(1).Replace(
                            "|",
                            "/"
                        )
                    );
                    item.ProgramId = migCommand.GetOption(2);
                    homegenie.UpdateSchedulerDatabase();
                    break;
                case "Scheduling.Delete":
                    homegenie.ProgramEngine.SchedulerService.Remove(migCommand.GetOption(0));
                    homegenie.UpdateSchedulerDatabase();
                    break;
                case "Scheduling.Enable":
                    homegenie.ProgramEngine.SchedulerService.Enable(migCommand.GetOption(0));
                    homegenie.UpdateSchedulerDatabase();
                    break;
                case "Scheduling.Disable":
                    homegenie.ProgramEngine.SchedulerService.Disable(migCommand.GetOption(0));
                    homegenie.UpdateSchedulerDatabase();
                    break;
                case "Scheduling.Get":
                    migCommand.Response = JsonConvert.SerializeObject(homegenie.ProgramEngine.SchedulerService.Get(migCommand.GetOption(0)));
                    break;
                case "Scheduling.List":
                    homegenie.ProgramEngine.SchedulerService.Items.Sort((SchedulerItem s1, SchedulerItem s2) =>
                    {
                        return s1.Name.CompareTo(s2.Name);
                    });
                    migCommand.Response = JsonConvert.SerializeObject(homegenie.ProgramEngine.SchedulerService.Items);
                    break;
                }
            }
            else if (migCommand.Command.StartsWith("Programs."))
            {
                if (migCommand.Command != "Programs.Import")
                {
                    streamContent = new StreamReader(request.InputStream).ReadToEnd();
                }
                //
                switch (migCommand.Command)
                {
                case "Programs.Import":
                    string archiveName = "homegenie_program_import.hgx";
                    if (File.Exists(archiveName))
                        File.Delete(archiveName);
                    //
                    var encoding = (request.Context as HttpListenerContext).Request.ContentEncoding;
                    string boundary = MIG.Gateways.WebServiceUtility.GetBoundary((request.Context as HttpListenerContext).Request.ContentType);
                    MIG.Gateways.WebServiceUtility.SaveFile(encoding, boundary, request.InputStream, archiveName);
                    //
                    int newPid = homegenie.ProgramEngine.GeneratePid();
                    var reader = new StreamReader(archiveName);
                    char[] signature = new char[2];
                    reader.Read(signature, 0, 2);
                    reader.Close();
                    if (signature[0] == 'P' && signature[1] == 'K')
                    {
                        // Read and uncompress zip file content (arduino program bundle)
                        string zipFileName = archiveName.Replace(".hgx", ".zip");
                        if (File.Exists(zipFileName))
                            File.Delete(zipFileName);
                        File.Move(archiveName, zipFileName);
                        string destFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp", "import");
                        if (Directory.Exists(destFolder))
                            Directory.Delete(destFolder, true);
                        homegenie.UnarchiveConfiguration(zipFileName, destFolder);
                        string bundleFolder = Path.Combine("programs", "arduino", newPid.ToString());
                        if (Directory.Exists(bundleFolder))
                            Directory.Delete(bundleFolder, true);
                        if (!Directory.Exists(Path.Combine("programs", "arduino")))
                            Directory.CreateDirectory(Path.Combine("programs", "arduino"));
                        Directory.Move(Path.Combine(destFolder, "src"), bundleFolder);
                        reader = new StreamReader(Path.Combine(destFolder, "program.hgx"));
                    }
                    else
                    {
                        reader = new StreamReader(archiveName);
                    }
                    var serializer = new XmlSerializer(typeof(ProgramBlock));
                    newProgram = (ProgramBlock)serializer.Deserialize(reader);
                    reader.Close();
                    //
                    newProgram.Address = newPid;
                    newProgram.Group = migCommand.GetOption(0);
                    homegenie.ProgramEngine.ProgramAdd(newProgram);
                    //
                    newProgram.IsEnabled = false;
                    newProgram.ScriptErrors = "";
                    newProgram.AppAssembly = null;
                    //
                    if (newProgram.Type.ToLower() != "arduino")
                    {
                        homegenie.ProgramEngine.CompileScript(newProgram);
                    }
                    //
                    homegenie.UpdateProgramsDatabase();
                    //migCommand.response = JsonHelper.GetSimpleResponse(programblock.Address);
                    migCommand.Response = newProgram.Address.ToString();
                    break;

                case "Programs.Export":
                    currentProgram = homegenie.ProgramEngine.Programs.Find(p => p.Address == int.Parse(migCommand.GetOption(0)));
                    string filename = currentProgram.Address + "-" + currentProgram.Name.Replace(" ", "_");
                    //
                    var writerSettings = new System.Xml.XmlWriterSettings();
                    writerSettings.Indent = true;
                    var programSerializer = new System.Xml.Serialization.XmlSerializer(typeof(ProgramBlock));
                    var builder = new StringBuilder();
                    var writer = System.Xml.XmlWriter.Create(builder, writerSettings);
                    programSerializer.Serialize(writer, currentProgram);
                    writer.Close();
                    //
                    if (currentProgram.Type.ToLower() == "arduino")
                    {
                        string arduinoBundle = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                                           "tmp",
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
                        (request.Context as HttpListenerContext).Response.AddHeader(
                            "Content-Disposition",
                            "attachment; filename=\"" + filename + ".zip\""
                            );
                        (request.Context as HttpListenerContext).Response.OutputStream.Write(bundleData, 0, bundleData.Length);
                    }
                    else
                    {
                        (request.Context as HttpListenerContext).Response.AddHeader(
                            "Content-Disposition",
                            "attachment; filename=\"" + filename + ".hgx\""
                        );
                        migCommand.Response = builder.ToString();
                    }
                    break;

                case "Programs.List":
                    var programList = new List<ProgramBlock>(homegenie.ProgramEngine.Programs);
                    programList.Sort(delegate(ProgramBlock p1, ProgramBlock p2)
                    {
                        string c1 = p1.Name + " " + p1.Address;
                        string c2 = p2.Name + " " + p2.Address;
                        return c1.CompareTo(c2);
                    });
                    migCommand.Response = JsonConvert.SerializeObject(programList);
                    break;

                case "Programs.Add":
                    newProgram = new ProgramBlock() {
                        Group = migCommand.GetOption(0),
                        Name = streamContent,
                        Type = "Wizard"
                    };
                    newProgram.Address = homegenie.ProgramEngine.GeneratePid();
                    homegenie.ProgramEngine.ProgramAdd(newProgram);
                    homegenie.UpdateProgramsDatabase();
                    migCommand.Response = JsonHelper.GetSimpleResponse(newProgram.Address.ToString());
                    break;

                case "Programs.Delete":
                    currentProgram = homegenie.ProgramEngine.Programs.Find(p => p.Address == int.Parse(migCommand.GetOption(0)));
                    if (currentProgram != null)
                    {
                        //TODO: remove groups associations as well
                        currentProgram.IsEnabled = false;
                        homegenie.ProgramEngine.ProgramRemove(currentProgram);
                        homegenie.UpdateProgramsDatabase();
                        // remove associated module entry
                        homegenie.Modules.RemoveAll(m => m.Domain == Domains.HomeAutomation_HomeGenie_Automation && m.Address == currentProgram.Address.ToString());
                        homegenie.UpdateModulesDatabase();
                    }
                    break;

                case "Programs.Compile":
                case "Programs.Update":
                    newProgram = JsonConvert.DeserializeObject<ProgramBlock>(streamContent);
                    currentProgram = homegenie.ProgramEngine.Programs.Find(p => p.Address == newProgram.Address);
                        //
                    if (currentProgram == null)
                    {
                        newProgram.Address = homegenie.ProgramEngine.GeneratePid();
                        homegenie.ProgramEngine.ProgramAdd(newProgram);
                    }
                    else
                    {
                        if (currentProgram.Type.ToLower() != newProgram.Type.ToLower())
                        {
                            currentProgram.AppAssembly = null; // dispose assembly and interrupt current task
                        }
                        currentProgram.Type = newProgram.Type;
                        currentProgram.Group = newProgram.Group;
                        currentProgram.Name = newProgram.Name;
                        currentProgram.Description = newProgram.Description;
                        currentProgram.IsEnabled = newProgram.IsEnabled;
                        currentProgram.ScriptCondition = newProgram.ScriptCondition;
                        currentProgram.ScriptSource = newProgram.ScriptSource;
                        currentProgram.Commands = newProgram.Commands;
                        currentProgram.Conditions = newProgram.Conditions;
                        currentProgram.ConditionType = newProgram.ConditionType;
                        // reset last condition evaluation status
                        currentProgram.LastConditionEvaluationResult = false;
                    }
                        //
                    if (migCommand.Command == "Programs.Compile")
                    {
                        // reset previous error status
                        currentProgram.IsEnabled = false;
                        currentProgram.Stop();
                        currentProgram.ScriptErrors = "";
                        //
                        List<ProgramError> errors = homegenie.ProgramEngine.CompileScript(currentProgram);
                        //
                        currentProgram.IsEnabled = newProgram.IsEnabled;
                        currentProgram.ScriptErrors = JsonConvert.SerializeObject(errors);
                        migCommand.Response = currentProgram.ScriptErrors;
                    }
                        //
                    homegenie.UpdateProgramsDatabase();
                        //
                    homegenie.modules_RefreshPrograms();
                    homegenie.modules_RefreshVirtualModules();
                    homegenie.modules_Sort();
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
                    migCommand.Response = JsonHelper.GetSimpleResponse(File.ReadAllText(sketchFile));
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
                        migCommand.Response = JsonHelper.GetSimpleResponse("EXISTS");
                    }
                    else if (!ArduinoAppFactory.IsValidProjectFile(sketchFile))
                    {
                        migCommand.Response = JsonHelper.GetSimpleResponse("INVALID_NAME");
                    }
                    else
                    {
                        StreamWriter sw = File.CreateText(sketchFile);
                        sw.Close();
                        sw.Dispose();
                        sw = null;
                        migCommand.Response = JsonHelper.GetSimpleResponse("OK");
                    }
                    break;
                    
                case "Programs.Arduino.FileDelete":
                    sketchFolder = Path.GetDirectoryName(ArduinoAppFactory.GetSketchFile(migCommand.GetOption(0)));
                    sketchFile = Path.Combine(sketchFolder, Path.GetFileName(migCommand.GetOption(1)));
                    if (!File.Exists(sketchFile))
                    {
                        migCommand.Response = JsonHelper.GetSimpleResponse("NOT_FOUND");
                    }
                    else
                    {
                        File.Delete(sketchFile);
                        migCommand.Response = JsonHelper.GetSimpleResponse("OK");
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
                    migCommand.Response = JsonConvert.SerializeObject(files);
                    break;

                case "Programs.Run":
                    currentProgram = homegenie.ProgramEngine.Programs.Find(p => p.Address == int.Parse(migCommand.GetOption(0)));
                    if (currentProgram != null)
                    {
                        // clear any runtime errors before running
                        currentProgram.ScriptErrors = "";
                        homegenie.ProgramEngine.RaiseProgramModuleEvent(
                            currentProgram,
                            "Runtime.Error",
                            ""
                        );
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
                    currentProgram = homegenie.ProgramEngine.Programs.Find(p => p.Address == int.Parse(migCommand.GetOption(0)));
                    if (currentProgram != null)
                    {
                        currentProgram.IsEnabled = false;
                        try
                        {
                            currentProgram.Stop();
                        }
                        catch
                        {
                        }
                        currentProgram.IsEnabled = true;
                        homegenie.UpdateProgramsDatabase();
                    }
                    break;

                case "Programs.Enable":
                    currentProgram = homegenie.ProgramEngine.Programs.Find(p => p.Address == int.Parse(migCommand.GetOption(0)));
                    if (currentProgram != null)
                    {
                        currentProgram.IsEnabled = true;
                        homegenie.UpdateProgramsDatabase();
                    }
                    break;

                case "Programs.Disable":
                    currentProgram = homegenie.ProgramEngine.Programs.Find(p => p.Address == int.Parse(migCommand.GetOption(0)));
                    if (currentProgram != null)
                    {
                        currentProgram.IsEnabled = false;
                        try
                        {
                            currentProgram.Stop();
                        }
                        catch
                        {
                        }
                        homegenie.UpdateProgramsDatabase();
                    }
                    break;
                }

            }
        }

        internal ProgramBlock ProgramRun(string address, string options)
        {
            int pid = 0;
            int.TryParse(address, out pid);
            ProgramBlock program = homegenie.ProgramEngine.Programs.Find(p => p.Address == pid);
            if (program != null)
            {
                if (!program.IsEnabled)
                    program.IsEnabled = true;
                try
                {
                    homegenie.ProgramEngine.Run(program, options);
                }
                catch
                {
                }
                ;
            }
            return program;
        }

        internal ProgramBlock ProgramToggle(string address, string options)
        {
            int pid = 0;
            int.TryParse(address, out pid);
            ProgramBlock program = homegenie.ProgramEngine.Programs.Find(p => p.Address == pid);
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
                        homegenie.ProgramEngine.Run(program, options);
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
            ProgramBlock program = homegenie.ProgramEngine.Programs.Find(p => p.Address == pid);
            if (program != null)
            {
                program.IsEnabled = false;
                program.Stop();
                homegenie.UpdateProgramsDatabase();
            }
            return program;
        }
    }
}
