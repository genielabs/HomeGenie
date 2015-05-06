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
using HomeGenie.Data;
using HomeGenie.Service.Constants;
using HomeGenie.Service.Logging;
using MIG;
using MIG.Interfaces.HomeAutomation.Commons;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using MIG.Interfaces.Media;
using Jint.Parser;
using HomeGenie.Automation.Scripting;

namespace HomeGenie.Service.Handlers
{
    public class Config
    {
        private HomeGenieService homegenie;
        private string widgetBasePath;

        public Config(HomeGenieService hg)
        {
            homegenie = hg;
            widgetBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "html", "pages", "control", "widgets");
        }

        public void ProcessRequest(MIGClientRequest request, MIGInterfaceCommand migCommand)
        {
            string response = "";
            switch (migCommand.Command)
            {
            case "Interfaces.List":
                migCommand.Response = "[ ";
                foreach (var kv in homegenie.Interfaces)
                {
                    var migInterface = kv.Value;
                    var ifaceConfig = homegenie.SystemConfiguration.MIGService.GetInterface(migInterface.Domain);
                    if (ifaceConfig == null || !ifaceConfig.IsEnabled)
                    {
                        continue;
                    }
                    migCommand.Response += "{ \"Domain\" : \"" + migInterface.Domain + "\", \"IsConnected\" : \"" + migInterface.IsConnected + "\" },";
                }
                if (homegenie.UpdateChecker != null && homegenie.UpdateChecker.IsUpdateAvailable)
                {
                    migCommand.Response += "{ \"Domain\" : \"" + Domains.HomeGenie_UpdateChecker + "\", \"IsConnected\" : \"True\" }";
                    migCommand.Response += " ]";
                }
                else
                {
                    migCommand.Response = migCommand.Response.Substring(0, migCommand.Response.Length - 1) + " ]";
                }
                break;

            case "Interfaces.ListConfig":
                migCommand.Response = "[ ";
                foreach (var kv in homegenie.Interfaces)
                {
                    var migInterface = kv.Value;
                    var ifaceConfig = homegenie.SystemConfiguration.MIGService.GetInterface(migInterface.Domain);
                    if (ifaceConfig == null) continue;
                    migCommand.Response += JsonConvert.SerializeObject(ifaceConfig)+",";
                }
                migCommand.Response = migCommand.Response.Substring(0, migCommand.Response.Length - 1) + " ]";
                break;

            //TODO: should this be moved somewhere to MIG?
            case "Interfaces.Configure":
                switch (migCommand.GetOption(0))
                {
                case "Hardware.SerialPorts":
                    if (Environment.OSVersion.Platform == PlatformID.Unix)
                    {
                        var serialPorts = System.IO.Ports.SerialPort.GetPortNames();
                        var portList = new List<string>();
                        for (int p = serialPorts.Length - 1; p >= 0; p--)
                        {
                            if (serialPorts[p].Contains("/ttyS") || serialPorts[p].Contains("/ttyUSB"))
                            {
                                portList.Add(serialPorts[p]);
                            }
                        }
                        if (Raspberry.Board.Current.IsRaspberryPi)
                        {
                            if (!portList.Contains("/dev/ttyAMA0"))
                                portList.Add("/dev/ttyAMA0"); // RaZberry
                            if (!portList.Contains("/dev/ttyACM0"))
                                portList.Add("/dev/ttyACM0"); // ZME_UZB1
                        }
                        migCommand.Response = JsonHelper.GetSimpleResponse(JsonConvert.SerializeObject(portList));
                    }
                    else
                    {
                        var portNames = System.IO.Ports.SerialPort.GetPortNames();
                        migCommand.Response = JsonHelper.GetSimpleResponse(JsonConvert.SerializeObject(portNames));
                    }
                    break;

                }
                break;

            case "Interface.Import":
                string downloadUrl = migCommand.GetOption(0);
                response = "";
                string ifaceFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Utility.GetTmpFolder(), "mig_interface_import.zip");
                string outputFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Utility.GetTmpFolder(), "mig");
                Utility.FolderCleanUp(outputFolder);

                try
                {
                    if (String.IsNullOrWhiteSpace(downloadUrl))
                    {
                        // file uploaded by user
                        var encoding = (request.Context as HttpListenerContext).Request.ContentEncoding;
                        string boundary = MIG.Gateways.WebServiceUtility.GetBoundary((request.Context as HttpListenerContext).Request.ContentType);
                        MIG.Gateways.WebServiceUtility.SaveFile(encoding, boundary, request.InputStream, ifaceFileName);
                    }
                    else
                    {
                        // download file from url
                        var client = new WebClient();
                        client.DownloadFile(downloadUrl, ifaceFileName);
                        client.Dispose();
                    }
                }
                catch
                { 
                    // TODO: report exception
                }

                try
                {
                    if (!Directory.Exists(outputFolder))
                    {
                        Directory.CreateDirectory(outputFolder);
                    }
                    Utility.UncompressZip(ifaceFileName, outputFolder);
                    File.Delete(ifaceFileName);

                    var migInt = GetInterfaceConfig(Path.Combine(outputFolder, "configuration.xml"));
                    if (migInt != null)
                    {
                        response = String.Format("{0} ({1})\n{2}\n", migInt.Domain, migInt.AssemblyName, migInt.Description);
                        // Check for README notes and append them to the response
                        var readmeFile = Path.Combine(outputFolder, "README.TXT");
                        if (File.Exists(readmeFile))
                        {
                            response += File.ReadAllText(readmeFile);
                        }
                        migCommand.Response = JsonHelper.GetSimpleResponse(response);
                    }
                    else
                    {
                        migCommand.Response = JsonHelper.GetSimpleResponse("NOT A VALID ADD-ON PACKAGE");
                    }
                } catch { }
                break;

            case "Interface.Install":

                // install the interface package
                string outFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Utility.GetTmpFolder(), "mig");
                string configFile = Path.Combine(outFolder, "configuration.xml");
                var iface = GetInterfaceConfig(configFile);
                if (iface != null)
                {
                    File.Delete(configFile);
                    //
                    homegenie.MigService.RemoveInterface(iface.Domain);
                    //
                    string configletName = iface.Domain.Substring(iface.Domain.LastIndexOf(".") + 1).ToLower();
                    string configletPath = Path.Combine("html", "pages", "configure", "interfaces", "configlet", configletName + ".html");
                    if (File.Exists(configletPath))
                    {
                        File.Delete(configletPath);
                    }
                    File.Move(Path.Combine(outFolder, "configlet.html"), configletPath);
                    //
                    string logoPath = Path.Combine("html", "images", "interfaces", configletName + ".png");
                    if (File.Exists(logoPath))
                    {
                        File.Delete(logoPath);
                    }
                    File.Move(Path.Combine(outFolder, "logo.png"), logoPath);
                    // copy other interface files to mig folder (dll and dependencies)
                    string migFolder = "mig";
                    Utility.FolderCleanUp(migFolder);
                    DirectoryInfo dir = new DirectoryInfo(outFolder);
                    foreach (var f in dir.GetFiles())
                    {
                        string destFile = Path.Combine(migFolder, Path.GetFileName(f.FullName));
                        if (File.Exists(destFile))
                        {
                            File.Move(destFile, Path.Combine(destFile, ".old"));
                            File.Delete(Path.Combine(destFile, ".old"));
                        }
                        File.Move(f.FullName, destFile);
                    }
                    //
                    homegenie.SystemConfiguration.MIGService.Interfaces.RemoveAll(i => i.Domain == iface.Domain);
                    homegenie.SystemConfiguration.MIGService.Interfaces.Add(iface);
                    homegenie.SystemConfiguration.Update();
                    homegenie.MigService.AddInterface(iface.Domain, iface.AssemblyName);

                    migCommand.Response = JsonHelper.GetSimpleResponse("OK");
                }
                else
                {
                    migCommand.Response = JsonHelper.GetSimpleResponse("NOT A VALID ADD-ON PACKAGE");
                }
                break;

            case "System.Configure":
                if (migCommand.GetOption(0) == "Service.Restart")
                {
                    Program.Quit(true);
                    migCommand.Response = JsonHelper.GetSimpleResponse("OK");
                }
                else if (migCommand.GetOption(0) == "UpdateManager.UpdatesList")
                {
                    migCommand.Response = JsonConvert.SerializeObject(homegenie.UpdateChecker.RemoteUpdates);
                }
                else if (migCommand.GetOption(0) == "UpdateManager.Check")
                {
                    homegenie.UpdateChecker.Check();
                    migCommand.Response = JsonHelper.GetSimpleResponse("OK");
                }
                else if (migCommand.GetOption(0) == "UpdateManager.DownloadUpdate")
                {
                    var resultMessage = "ERROR";
                    bool success = homegenie.UpdateChecker.DownloadUpdateFiles();
                    if (success)
                    {
                        if (homegenie.UpdateChecker.IsRestartRequired)
                        {
                            resultMessage = "RESTART";
                        }
                        else
                        {
                            resultMessage = "OK";
                        }
                    }
                    migCommand.Response = JsonHelper.GetSimpleResponse(resultMessage);
                }
                else if (migCommand.GetOption(0) == "UpdateManager.InstallUpdate") //UpdateManager.InstallProgramsCommit")
                {
                    string resultMessage = "OK";
                    if (!homegenie.UpdateChecker.InstallFiles())
                    {
                        resultMessage = "ERROR";
                    }
                    else
                    {
                        if (homegenie.UpdateChecker.IsRestartRequired)
                        {
                            resultMessage = "RESTART";
                            Utility.RunAsyncTask(() =>
                            {
                                Thread.Sleep(2000);
                                Program.Quit(true);
                            });
                        }
                        else
                        {
                            homegenie.LoadConfiguration();
                            homegenie.MigService.ClearWebCache();
                            homegenie.UpdateChecker.Check();
                        }
                    }
                    migCommand.Response = JsonHelper.GetSimpleResponse(resultMessage);
                }
                else if (migCommand.GetOption(0) == "HttpService.SetWebCacheEnabled")
                {
                    if (migCommand.GetOption(1) == "1")
                    {
                        homegenie.MigService.IsWebCacheEnabled = true;
                        homegenie.SystemConfiguration.MIGService.EnableWebCache = "true";
                    }
                    else
                    {
                        homegenie.MigService.IsWebCacheEnabled = false;
                        homegenie.SystemConfiguration.MIGService.EnableWebCache = "false";
                    }
                    homegenie.SystemConfiguration.Update();
                    migCommand.Response = JsonHelper.GetSimpleResponse("OK");
                }
                else if (migCommand.GetOption(0) == "HttpService.GetWebCacheEnabled")
                {
                    migCommand.Response = JsonHelper.GetSimpleResponse(homegenie.MigService.IsWebCacheEnabled ? "1" : "0");
                }
                else if (migCommand.GetOption(0) == "HttpService.GetPort")
                {
                    migCommand.Response = JsonHelper.GetSimpleResponse(homegenie.SystemConfiguration.HomeGenie.ServicePort.ToString());
                }
                else if (migCommand.GetOption(0) == "HttpService.SetPort")
                {
                    try
                    {
                        homegenie.SystemConfiguration.HomeGenie.ServicePort = int.Parse(migCommand.GetOption(1));
                        homegenie.SystemConfiguration.Update();
                    } catch { }
                }
                else if (migCommand.GetOption(0) == "Statistics.GetStatisticsDatabaseMaximumSize")
                {
                    migCommand.Response = JsonHelper.GetSimpleResponse(homegenie.SystemConfiguration.HomeGenie.Statistics.MaxDatabaseSizeMBytes.ToString());
                }
                else if (migCommand.GetOption(0) == "Statistics.SetStatisticsDatabaseMaximumSize")
                {
                    try
                    {
                        homegenie.SystemConfiguration.HomeGenie.Statistics.MaxDatabaseSizeMBytes = int.Parse(migCommand.GetOption(1));
                        homegenie.SystemConfiguration.Update();
                    } catch { }
                }
                else if (migCommand.GetOption(0) == "SystemLogging.DownloadCsv")
                {
                    string csvlog = "";
                    string logpath = Path.Combine("log", "homegenie.log");
                    if (migCommand.GetOption(1) == "1")
                    {
                        logpath = Path.Combine("log", "homegenie.log.bak");
                    }
                    if (File.Exists(logpath))
                    {
                        using (var fs = new FileStream(logpath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var sr = new StreamReader(fs, Encoding.Default))
                        {
                            csvlog = sr.ReadToEnd();
                        }
                    }
                    (request.Context as HttpListenerContext).Response.AddHeader("Content-Disposition", "attachment;filename=homegenie_log_" + migCommand.GetOption(1) + ".csv");
                    migCommand.Response = csvlog;
                }
                else if (migCommand.GetOption(0) == "SystemLogging.Enable")
                {
                    SystemLogger.Instance.OpenLog();
                    homegenie.SystemConfiguration.HomeGenie.EnableLogFile = "true";
                    homegenie.SystemConfiguration.Update();
                }
                else if (migCommand.GetOption(0) == "SystemLogging.Disable")
                {
                    SystemLogger.Instance.CloseLog();
                    homegenie.SystemConfiguration.HomeGenie.EnableLogFile = "false";
                    homegenie.SystemConfiguration.Update();
                }
                else if (migCommand.GetOption(0) == "SystemLogging.IsEnabled")
                {
                    migCommand.Response = JsonHelper.GetSimpleResponse((homegenie.SystemConfiguration.HomeGenie.EnableLogFile.ToLower().Equals("true") ? "1" : "0"));
                }
                else if (migCommand.GetOption(0) == "Security.SetPassword")
                {
                    // password only for now, with fixed user login 'admin'
                    string pass = migCommand.GetOption(1) == "" ? "" : MIG.Utility.Encryption.SHA1.GenerateHashString(migCommand.GetOption(1));
                    homegenie.MigService.SetWebServicePassword(pass);
                    homegenie.SystemConfiguration.HomeGenie.UserPassword = pass;
                    // regenerate encrypted files
                    homegenie.SystemConfiguration.Update();
                    homegenie.UpdateModulesDatabase();
                }
                else if (migCommand.GetOption(0) == "Security.ClearPassword")
                {
                    homegenie.MigService.SetWebServicePassword("");
                    homegenie.SystemConfiguration.HomeGenie.UserPassword = "";
                    // regenerate encrypted files
                    homegenie.SystemConfiguration.Update();
                    homegenie.UpdateModulesDatabase();
                }
                else if (migCommand.GetOption(0) == "Security.HasPassword")
                {
                    migCommand.Response = JsonHelper.GetSimpleResponse((homegenie.SystemConfiguration.HomeGenie.UserPassword != "" ? "1" : "0"));
                }
                else if (migCommand.GetOption(0) == "System.ConfigurationRestore")
                {
                    // file uploaded by user
                    string archivename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Utility.GetTmpFolder(), "homegenie_restore_config.zip");
                    Utility.FolderCleanUp(Utility.GetTmpFolder());
                    try
                    {
                        var encoding = (request.Context as HttpListenerContext).Request.ContentEncoding;
                        string boundary = MIG.Gateways.WebServiceUtility.GetBoundary((request.Context as HttpListenerContext).Request.ContentType);
                        MIG.Gateways.WebServiceUtility.SaveFile(encoding, boundary, request.InputStream, archivename);
                        Utility.UncompressZip(archivename, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Utility.GetTmpFolder()));
                        File.Delete(archivename);
                    } catch { }
                }
                else if (migCommand.GetOption(0) == "System.ConfigurationRestoreS1")
                {
                    var serializer = new XmlSerializer(typeof(List<ProgramBlock>));
                    var reader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Utility.GetTmpFolder(), "programs.xml"));
                    var newProgramsData = (List<ProgramBlock>)serializer.Deserialize(reader);
                    reader.Close();
                    var newProgramList = new List<ProgramBlock>();
                    foreach (ProgramBlock program in newProgramsData)
                    {
                        if (program.Address >= ProgramEngine.USER_SPACE_PROGRAMS_START)
                        {
                            ProgramBlock p = new ProgramBlock();
                            p.Address = program.Address;
                            p.Name = program.Name;
                            p.Description = program.Description;
                            newProgramList.Add(p);
                        }
                    }
                    newProgramList.Sort(delegate(ProgramBlock p1, ProgramBlock p2)
                    {
                        string c1 = p1.Address.ToString();
                        string c2 = p2.Address.ToString();
                        return c1.CompareTo(c2);
                    });
                    migCommand.Response = JsonConvert.SerializeObject(newProgramList);
                }
                else if (migCommand.GetOption(0) == "System.ConfigurationRestoreS2")
                {
                    var serializer = new XmlSerializer(typeof(List<Group>));
                    var reader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Utility.GetTmpFolder(), "automationgroups.xml"));
                    var automationGroups = (List<Group>)serializer.Deserialize(reader);
                    reader.Close();
                    //
                    foreach (var automationGroup in automationGroups)
                    {
                        if (homegenie.AutomationGroups.Find(g => g.Name == automationGroup.Name) == null)
                        {
                            homegenie.AutomationGroups.Add(automationGroup);
                        }
                    }
                    //
                    homegenie.UpdateGroupsDatabase("Automation");
                    //
                    //File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp", "automationgroups.xml"), "./automationgroups.xml", true);
                    File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Utility.GetTmpFolder(), "groups.xml"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "groups.xml"), true);
                    File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Utility.GetTmpFolder(), "lircconfig.xml"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lircconfig.xml"), true);
                    File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Utility.GetTmpFolder(), "modules.xml"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "modules.xml"), true);
                    File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Utility.GetTmpFolder(), "scheduler.xml"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scheduler.xml"), true);
                    File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Utility.GetTmpFolder(), "systemconfig.xml"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "systemconfig.xml"), true);
                    //
                    homegenie.LoadConfiguration();
                    //
                    // Restore automation programs
                    string selectedPrograms = migCommand.GetOption(1);
                    serializer = new XmlSerializer(typeof(List<ProgramBlock>));
                    reader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Utility.GetTmpFolder(), "programs.xml"));
                    var newProgramsData = (List<ProgramBlock>)serializer.Deserialize(reader);
                    reader.Close();
                    foreach (var program in newProgramsData)
                    {
                        var currentProgram = homegenie.ProgramEngine.Programs.Find(p => p.Address == program.Address);
                        program.IsRunning = false;
                        // Only restore user space programs
                        if (selectedPrograms.Contains("," + program.Address.ToString() + ",") && program.Address >= ProgramEngine.USER_SPACE_PROGRAMS_START)
                        {
                            int oldPid = program.Address;
                            if (currentProgram == null)
                            {
                                var newPid = ((currentProgram != null && currentProgram.Address == program.Address) ? homegenie.ProgramEngine.GeneratePid() : program.Address);
                                try
                                {
                                    File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Utility.GetTmpFolder(), "programs", program.Address + ".dll"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs", newPid + ".dll"), true);
                                } catch { }
                                program.Address = newPid;
                                homegenie.ProgramEngine.ProgramAdd(program);
                            }
                            else if (currentProgram != null)
                            {
                                homegenie.ProgramEngine.ProgramRemove(currentProgram);
                                try
                                {
                                    File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Utility.GetTmpFolder(), "programs", program.Address + ".dll"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs", program.Address + ".dll"), true);
                                } catch { }
                                homegenie.ProgramEngine.ProgramAdd(program);
                            }
                            // Restore Arduino program folder ...
                            // TODO: this is untested yet...
                            if (program.Type.ToLower() == "arduino")
                            {
                                string sourceFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Utility.GetTmpFolder(), "programs", "arduino", oldPid.ToString());
                                string arduinoFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs", "arduino", program.Address.ToString());
                                if (Directory.Exists(arduinoFolder))
                                    Directory.Delete(arduinoFolder, true);
                                Directory.CreateDirectory(arduinoFolder);
                                foreach (string newPath in Directory.GetFiles(sourceFolder))
                                {
                                    File.Copy(newPath, newPath.Replace(sourceFolder, arduinoFolder), true);
                                }
                            }
                        }
                        else if (currentProgram != null && program.Address < ProgramEngine.USER_SPACE_PROGRAMS_START)
                        {
                            // Only restore Enabled/Disabled status of system programs
                            currentProgram.IsEnabled = program.IsEnabled;
                        }
                    }
                    //
                    homegenie.UpdateProgramsDatabase();
                    //
                    // regenerate encrypted files
                    homegenie.UpdateModulesDatabase();
                    homegenie.SystemConfiguration.Update();
                }
                else if (migCommand.GetOption(0) == "System.ConfigurationReset")
                {
                    homegenie.RestoreFactorySettings();
                }
                else if (migCommand.GetOption(0) == "System.ConfigurationBackup")
                {
                    homegenie.BackupCurrentSettings();
                    (request.Context as HttpListenerContext).Response.Redirect("/hg/html/homegenie_backup_config.zip");
                }
                else if (migCommand.GetOption(0) == "System.ConfigurationLoad")
                {
                    homegenie.LoadConfiguration();
                }
                break;

            case "Modules.Get":
                try
                {
                    var module = homegenie.Modules.Find(m => m.Domain == migCommand.GetOption(0) && m.Address == migCommand.GetOption(1));
                    migCommand.Response = Utility.Module2Json(module, false);
                }
                catch (Exception ex)
                {
                    migCommand.Response = JsonHelper.GetSimpleResponse("ERROR: \n" + ex.Message + "\n\n" + ex.StackTrace);
                }
                break;

            case "Modules.List":
                try
                {
                    homegenie.modules_Sort();
                    migCommand.Response = homegenie.GetJsonSerializedModules(migCommand.GetOption(0).ToLower() == "short");
                }
                catch (Exception ex)
                {
                    migCommand.Response = JsonHelper.GetSimpleResponse("ERROR: \n" + ex.Message + "\n\n" + ex.StackTrace);
                }
                break;

            case "Modules.RoutingReset":
                try
                {
                    for (int m = 0; m < homegenie.Modules.Count; m++)
                    {
                        homegenie.Modules[m].RoutingNode = "";
                    }
                    migCommand.Response = JsonHelper.GetSimpleResponse("OK");
                }
                catch (Exception ex)
                {
                    migCommand.Response = JsonHelper.GetSimpleResponse("ERROR: \n" + ex.Message + "\n\n" + ex.StackTrace);
                }
                break;

            case "Modules.Save":
                string body = new StreamReader(request.InputStream).ReadToEnd();
                var newModules = JsonConvert.DeserializeObject(body) as JArray;
                for (int i = 0; i < newModules.Count; i++)
                {
                    try
                    {
                        var module = homegenie.Modules.Find(m => m.Address == newModules[i]["Address"].ToString() && m.Domain == newModules[i]["Domain"].ToString());
                        module.Name = newModules[i]["Name"].ToString();
                        //
                        try
                        {
                            module.DeviceType = (MIG.ModuleTypes)Enum.Parse(typeof(MIG.ModuleTypes), newModules[i]["DeviceType"].ToString(), true);
                        }
                        catch
                        {
                            // TODO: check what's wrong here...
                        }
                        //
                        var moduleProperties = newModules[i]["Properties"] as JArray;
                        for (int p = 0; p < moduleProperties.Count; p++)
                        {
                            string propertyName = moduleProperties[p]["Name"].ToString();
                            string propertyValue = moduleProperties[p]["Value"].ToString();
                            ModuleParameter parameter = null;
                            parameter = module.Properties.Find(delegate(ModuleParameter mp)
                            {
                                return mp.Name == propertyName;
                            });
                            //
                            if (propertyName == ModuleParameters.MODPAR_VIRTUALMETER_WATTS)
                            {
                                try
                                {
                                    propertyValue = double.Parse(propertyValue.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture).ToString();
                                }
                                catch
                                {
                                    propertyValue = "0";
                                }
                            }
                            //
                            if (parameter == null)
                            {
                                module.Properties.Add(new ModuleParameter() {
                                    Name = propertyName,
                                    Value = propertyValue
                                });
                            }
                            else
                            {
                                if (moduleProperties[p]["NeedsUpdate"] != null && moduleProperties[p]["NeedsUpdate"].ToString() == "true")
                                {
                                    parameter.Value = propertyValue;
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        //TODO: notify exception?
                    }
                }
                homegenie.UpdateModulesDatabase();//write modules
                break;

            case "Modules.Update":
                string streamContent = new StreamReader(request.InputStream).ReadToEnd();
                var newModule = JsonConvert.DeserializeObject<Module>(streamContent);
                var currentModule = homegenie.Modules.Find(p => p.Domain == newModule.Domain && p.Address == newModule.Address);
                //
                if (currentModule == null)
                {
                    homegenie.Modules.Add(newModule);
                }
                else
                {
                    currentModule.Name = newModule.Name;
                    currentModule.Description = newModule.Description;
                    currentModule.DeviceType = newModule.DeviceType;
                    foreach (var newParameter in newModule.Properties)
                    {
                        var currentParameter = currentModule.Properties.Find(mp => mp.Name == newParameter.Name);
                        if (currentParameter == null)
                        {
                            currentModule.Properties.Add(newParameter);
                        }
                        else if (newParameter.NeedsUpdate)
                        {
                            currentParameter.Value = newParameter.Value;
                        }
                    }
                    // look for deleted properties
                    var deletedParameters = new List<ModuleParameter>();
                    foreach (var parameter in currentModule.Properties)
                    {
                        var currentParameter = newModule.Properties.Find(mp => mp.Name == parameter.Name);
                        if (currentParameter == null)
                        {
                            deletedParameters.Add(parameter);
                        }
                    }
                    foreach (var parameter in deletedParameters)
                    {
                        currentModule.Properties.Remove(parameter);
                    }
                    deletedParameters.Clear();
                }
                //
                homegenie.UpdateModulesDatabase();
                break;

            case "Modules.Delete":
                var deletedModule = homegenie.Modules.Find(m => m.Domain == migCommand.GetOption(0) && m.Address == migCommand.GetOption(1));
                if (deletedModule != null)
                {
                    homegenie.Modules.Remove(deletedModule);
                }
                migCommand.Response = JsonHelper.GetSimpleResponse("OK");
                //
                homegenie.UpdateModulesDatabase();
                break;

            case "Stores.List":
                {
                    var module = homegenie.Modules.Find(m => m.Domain == migCommand.GetOption(0) && m.Address == migCommand.GetOption(1));
                    if (module != null)
                    {
                        //module.Stores
                        migCommand.Response = "[";
                        for (int s = 0; s < module.Stores.Count; s++)
                        {
                            migCommand.Response += "{ \"Name\": \"" + Utility.XmlEncode(module.Stores[s].Name) + "\", \"Description\": \"" + Utility.XmlEncode(module.Stores[s].Description) + "\" },";
                        }
                        migCommand.Response = migCommand.Response.TrimEnd(',') + "]";
                    }

                }
                break;

            case "Stores.Delete":
                break;

            case "Stores.ItemList":
                {
                    var module = homegenie.Modules.Find(m => m.Domain == migCommand.GetOption(0) && m.Address == migCommand.GetOption(1));
                    if (module != null)
                    {
                        migCommand.Response = "[";
                        var store = new StoreHelper(module.Stores, migCommand.GetOption(2));
                        for (int p = 0; p < store.List.Count; p++)
                        {
                            migCommand.Response += "{ \"Name\": \"" + Utility.XmlEncode(store.List[p].Name) + "\", \"Description\": \"" + Utility.XmlEncode(store.List[p].Description) + "\" },";
                        }
                        migCommand.Response = migCommand.Response.TrimEnd(',') + "]";
                    }
                }
                break;

            case "Stores.ItemDelete":
                {
                    var module = homegenie.Modules.Find(m => m.Domain == migCommand.GetOption(0) && m.Address == migCommand.GetOption(1));
                    if (module != null)
                    {
                        var name = migCommand.GetOption(3);
                        var store = new StoreHelper(module.Stores, migCommand.GetOption(2));
                        store.List.RemoveAll(i => i.Name == name);
                    }
                }
                break;

            case "Stores.ItemGet":
                {
                    var module = homegenie.Modules.Find(m => m.Domain == migCommand.GetOption(0) && m.Address == migCommand.GetOption(1));
                    if (module != null)
                    {
                        var store = new StoreHelper(module.Stores, migCommand.GetOption(2));
                        migCommand.Response += JsonConvert.SerializeObject(store.Get(migCommand.GetOption(3)));
                    }
                }
                break;

            case "Stores.ItemSet":
                {
                    // value is the POST body
                    string itemData = new StreamReader(request.InputStream).ReadToEnd();
                    var module = homegenie.Modules.Find(m => m.Domain == migCommand.GetOption(0) && m.Address == migCommand.GetOption(1));
                    if (module != null)
                    {
                        var store = new StoreHelper(module.Stores, migCommand.GetOption(2));
                        store.Get(migCommand.GetOption(3)).Value = itemData;
                    }
                }
                break;

            case "Groups.ModulesList":
                var theGroup = homegenie.Groups.Find(z => z.Name.ToLower() == migCommand.GetOption(0).Trim().ToLower());
                if (theGroup != null)
                {
                    string jsonmodules = "[";
                    for (int m = 0; m < theGroup.Modules.Count; m++)
                    {
                        var groupModule = homegenie.Modules.Find(mm => mm.Domain == theGroup.Modules[m].Domain && mm.Address == theGroup.Modules[m].Address);
                        if (groupModule != null)
                        {
                            jsonmodules += Utility.Module2Json(groupModule, false) + ",\n";
                        }
                    }
                    jsonmodules = jsonmodules.TrimEnd(',', '\n');
                    jsonmodules += "]";
                    migCommand.Response = jsonmodules;
                }
                break;
            case "Groups.List":
                try
                {
                    migCommand.Response = JsonConvert.SerializeObject(homegenie.GetGroups(migCommand.GetOption(0)));
                }
                catch (Exception ex)
                {
                    migCommand.Response = JsonHelper.GetSimpleResponse("ERROR: \n" + ex.Message + "\n\n" + ex.StackTrace);
                }
                break;

            case "Groups.Rename":
                string oldName = migCommand.GetOption(1);
                string newName = new StreamReader(request.InputStream).ReadToEnd();
                var currentGroup = homegenie.GetGroups(migCommand.GetOption(0)).Find(g => g.Name == oldName);
                var newGroup = homegenie.GetGroups(migCommand.GetOption(0)).Find(g => g.Name == newName);
                // ensure that the new group name is not already defined
                if (newGroup == null && currentGroup != null)
                {
                    currentGroup.Name = newName;
                    homegenie.UpdateGroupsDatabase(migCommand.GetOption(0));
                    //cmd.response = JsonHelper.GetSimpleResponse("OK");
                }
                else
                {
                    migCommand.Response = JsonHelper.GetSimpleResponse("New name already in use.");
                }
                break;

            case "Groups.Sort":
                using (var reader = new StreamReader(request.InputStream))
                {
                    var newGroupList = new List<Group>();
                    string[] newPositionOrder = reader.ReadToEnd().Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < newPositionOrder.Length; i++)
                    {
                        newGroupList.Add(homegenie.GetGroups(migCommand.GetOption(0))[int.Parse(newPositionOrder[i])]);
                    }
                    homegenie.GetGroups(migCommand.GetOption(0)).Clear();
                    homegenie.GetGroups(migCommand.GetOption(0)).RemoveAll(g => true);
                    homegenie.GetGroups(migCommand.GetOption(0)).AddRange(newGroupList);
                    homegenie.UpdateGroupsDatabase(migCommand.GetOption(0));
                }
                //
                try
                {
                    migCommand.Response = JsonConvert.SerializeObject(homegenie.GetGroups(migCommand.GetOption(0)));
                }
                catch (Exception ex)
                {
                    migCommand.Response = JsonHelper.GetSimpleResponse("ERROR: \n" + ex.Message + "\n\n" + ex.StackTrace);
                }
                break;

            case "Groups.SortModules":
                using (var reader = new StreamReader(request.InputStream))
                {
                    string groupName = migCommand.GetOption(1);
                    Group sortGroup = null;
                    try
                    {
                        sortGroup = homegenie.GetGroups(migCommand.GetOption(0)).Find(zn => zn.Name == groupName);
                    } catch { }
                    //
                    if (sortGroup != null)
                    {
                        var newModulesReference = new List<ModuleReference>();
                        string[] newPositionOrder = reader.ReadToEnd().Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < newPositionOrder.Length; i++)
                        {
                            newModulesReference.Add(sortGroup.Modules[int.Parse(newPositionOrder[i])]);
                        }
                        sortGroup.Modules.Clear();
                        sortGroup.Modules = newModulesReference;
                        homegenie.UpdateGroupsDatabase(migCommand.GetOption(0));
                    }
                }

                try
                {
                    migCommand.Response = JsonConvert.SerializeObject(homegenie.GetGroups(migCommand.GetOption(0)));
                }
                catch (Exception ex)
                {
                    migCommand.Response = JsonHelper.GetSimpleResponse("ERROR: \n" + ex.Message + "\n\n" + ex.StackTrace);
                }
                break;

            case "Groups.Add":
                string newGroupName = new StreamReader(request.InputStream).ReadToEnd();
                homegenie.GetGroups(migCommand.GetOption(0)).Add(new Group() { Name = newGroupName });
                homegenie.UpdateGroupsDatabase(migCommand.GetOption(0));//write groups
                break;

            case "Groups.Delete":
                string deletedGroupName = new StreamReader(request.InputStream).ReadToEnd();
                Group deletedGroup = null;
                try
                {
                    deletedGroup = homegenie.GetGroups(migCommand.GetOption(0)).Find(zn => zn.Name == deletedGroupName);
                } catch { }
                //
                if (deletedGroup != null)
                {
                    homegenie.GetGroups(migCommand.GetOption(0)).Remove(deletedGroup);
                    homegenie.UpdateGroupsDatabase(migCommand.GetOption(0));//write groups
                    if (migCommand.GetOption(0).ToLower() == "automation")
                    {
                        var groupPrograms = homegenie.ProgramEngine.Programs.FindAll(p => p.Group.ToLower() == deletedGroup.Name.ToLower());
                        if (groupPrograms != null)
                        {
                            // delete group association from programs
                            foreach (ProgramBlock program in groupPrograms)
                            {
                                program.Group = "";
                            }
                        }
                    }
                }
                break;

            case "Groups.Save":
                string jsonGroups = new StreamReader(request.InputStream).ReadToEnd();
                var newGroups = JsonConvert.DeserializeObject<List<Group>>(jsonGroups);
                for (int i = 0; i < newGroups.Count; i++)
                {
                    try
                    {
                        var group = homegenie.Groups.Find(z => z.Name == newGroups[i].Name);
                        group.Modules.Clear();
                        group.Modules = newGroups[i].Modules;
                    } catch { }
                }
                homegenie.UpdateGroupsDatabase(migCommand.GetOption(0));//write groups
                break;

            case "Widgets.List":
                List<string> widgetsList = new List<string>();
                var groups = Directory.GetDirectories(widgetBasePath);
                for (int d = 0; d < groups.Length; d++)
                {
                    var categories = Directory.GetDirectories(groups[d]);
                    for (int c = 0; c < categories.Length; c++)
                    {
                        var widgets = Directory.GetFiles(categories[c], "*.js");
                        var group = groups[d].Replace(widgetBasePath, "").Substring(1);
                        var category = categories[c].Replace(groups[d], "").Substring(1);
                        for (int w = 0; w < widgets.Length; w++)
                        {
                            widgetsList.Add(group + "/" + category + "/" + Path.GetFileNameWithoutExtension(widgets[w]));
                        }
                    }
                }
                migCommand.Response = JsonConvert.SerializeObject(widgetsList);
                break;

            case "Widgets.Add":
                {
                    response = "ERROR";
                    string widgetPath = migCommand.GetOption(0); // eg. homegenie/generic/dimmer
                    string[] widgetParts = widgetPath.Split('/');
                    widgetParts[0] = new String(widgetParts[0].Where(Char.IsLetter).ToArray()).ToLower();
                    widgetParts[1] = new String(widgetParts[1].Where(Char.IsLetter).ToArray()).ToLower();
                    widgetParts[2] = new String(widgetParts[2].Where(Char.IsLetter).ToArray()).ToLower();
                    if (!String.IsNullOrWhiteSpace(widgetParts[0]) && !String.IsNullOrWhiteSpace(widgetParts[1]) && !String.IsNullOrWhiteSpace(widgetParts[2]))
                    {
                        string filePath = Path.Combine(widgetBasePath, widgetParts[0], widgetParts[1]);
                        if (!Directory.Exists(filePath))
                        {
                            Directory.CreateDirectory(filePath);
                        }
                        // copy widget template into the new widget
                        var htmlFile = Path.Combine(filePath, widgetParts[2] + ".html");
                        var jsFile = Path.Combine(filePath, widgetParts[2] + ".js");
                        if (!File.Exists(htmlFile) && !File.Exists(jsFile))
                        {
                            File.Copy(Path.Combine(widgetBasePath, "template.html"), htmlFile);
                            File.Copy(Path.Combine(widgetBasePath, "template.js"), jsFile);
                            response = "OK";
                        }
                    }
                    migCommand.Response = JsonHelper.GetSimpleResponse(response);
                }
                break;

            case "Widgets.Save":
                {
                    response = "ERROR";
                    string widgetData = new StreamReader(request.InputStream).ReadToEnd();
                    string fileType = migCommand.GetOption(0);
                    string widgetPath = migCommand.GetOption(1); // eg. homegenie/generic/dimmer
                    string[] widgetParts = widgetPath.Split('/');
                    string filePath = Path.Combine(widgetBasePath, widgetParts[0], widgetParts[1]);
                    if (!Directory.Exists(filePath))
                    {
                        Directory.CreateDirectory(filePath);
                    }
                    switch (fileType)
                    {
                    // html/javascript source
                    case "html":
                    case "js":
                        using (TextWriter widgetWriter = new StreamWriter(Path.Combine(filePath, widgetParts[2] + "." + fileType)))
                        {
                            widgetWriter.Write(widgetData);
                        }
                        response = "OK";
                        break;
                    // style sheet file
                    case "css":
                        break;
                    // locale file
                    case "json":
                        break;
                    // image file
                    case "jpg":
                    case "png":
                    case "gif":
                        break;
                    }
                    migCommand.Response = JsonHelper.GetSimpleResponse(response);
                }
                break;

            case "Widgets.Delete":
                {
                    response = "ERROR";
                    string widgetPath = migCommand.GetOption(0); // eg. homegenie/generic/dimmer
                    string[] widgetParts = widgetPath.Split('/');
                    string filePath = Path.Combine(widgetBasePath, widgetParts[0], widgetParts[1], widgetParts[2] + ".");
                    if (File.Exists(filePath + "html"))
                    {
                        File.Delete(filePath + "html");
                        response = "OK";
                    }
                    if (File.Exists(filePath + "js"))
                    {
                        File.Delete(filePath + "js");
                        response = "OK";
                    }
                    migCommand.Response = JsonHelper.GetSimpleResponse(response);
                }
                break;

            case "Widgets.Export":
                {
                    string widgetPath = migCommand.GetOption(0); // eg. homegenie/generic/dimmer
                    string[] widgetParts = widgetPath.Split('/');
                    string widgetBundle = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Utility.GetTmpFolder(), "export", widgetPath.Replace('/', '_') + ".zip");
                    if (File.Exists(widgetBundle))
                    {
                        File.Delete(widgetBundle);
                    }
                    else if (!Directory.Exists(Path.GetDirectoryName(widgetBundle)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(widgetBundle));
                    }
                    string inputPath = Path.Combine(widgetBasePath, widgetParts[0], widgetParts[1]);
                    string outputPath = Path.Combine(widgetParts[0], widgetParts[1]);
                    string infoFilePath = Path.Combine(inputPath, "widget.info");
                    File.WriteAllText(infoFilePath, "HomeGenie exported widget.");
                    Utility.AddFileToZip(widgetBundle, infoFilePath, "widget.info");
                    Utility.AddFileToZip(widgetBundle, Path.Combine(inputPath, widgetParts[2] + ".html"), Path.Combine(outputPath, widgetParts[2] + ".html"));
                    Utility.AddFileToZip(widgetBundle, Path.Combine(inputPath, widgetParts[2] + ".js"), Path.Combine(outputPath, widgetParts[2] + ".js"));
                    //
                    byte[] bundleData = File.ReadAllBytes(widgetBundle);
                    (request.Context as HttpListenerContext).Response.AddHeader("Content-Disposition", "attachment; filename=\"" + widgetPath.Replace('/', '_') + ".zip\"");
                    (request.Context as HttpListenerContext).Response.OutputStream.Write(bundleData, 0, bundleData.Length);
                }
                break;
                
            case "Widgets.Import":
                {
                    var encoding = (request.Context as HttpListenerContext).Request.ContentEncoding;
                    string boundary = MIG.Gateways.WebServiceUtility.GetBoundary((request.Context as HttpListenerContext).Request.ContentType);
                    string archiveFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Utility.GetTmpFolder(), "import_widget.zip");
                    string importPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Utility.GetTmpFolder(), "import");
                    if (Directory.Exists(importPath))
                        Directory.Delete(importPath, true);
                    MIG.Gateways.WebServiceUtility.SaveFile(encoding, boundary, request.InputStream, archiveFile);
                    if (WidgetImport(archiveFile, importPath))
                    {
                        migCommand.Response = JsonHelper.GetSimpleResponse("OK");
                    }
                    else
                    {
                        migCommand.Response = JsonHelper.GetSimpleResponse("ERROR");
                    }
                }
                break;

            case "Widgets.Parse":
                {
                    string widgetData = new StreamReader(request.InputStream).ReadToEnd();
                    var parser = new JavaScriptParser();
                    try
                    {
                        migCommand.Response = JsonHelper.GetSimpleResponse("OK");
                        parser.Parse(widgetData);
                    } 
                    catch (Jint.Parser.ParserException e) 
                    {
                        migCommand.Response = JsonHelper.GetSimpleResponse("ERROR (" + e.LineNumber + "," + e.Column + "): " + e.Description);
                    }
                }
                break;
            }
        }

        private MIGServiceConfiguration.Interface GetInterfaceConfig(string configFile)
        {
            MIGServiceConfiguration.Interface iface = null;
            using (StreamReader ifaceReader = new StreamReader(configFile))
            {
                XmlSerializer ifaceSerializer = new XmlSerializer(typeof(MIGServiceConfiguration.Interface));
                iface = (MIGServiceConfiguration.Interface)ifaceSerializer.Deserialize(ifaceReader);
                ifaceReader.Close();
            }
            return iface;
        }

        private bool WidgetImport(string archiveFile, string importPath)
        {
            bool success = false;
            List<string> extractedFiles = Utility.UncompressZip(archiveFile, importPath);
            if (File.Exists(Path.Combine(importPath, "widget.info")))
            {
                foreach (string f in extractedFiles)
                {
                    if (f.EndsWith(".html") || f.EndsWith(".js"))
                    {
                        string destFolder = Path.Combine(widgetBasePath, Path.GetDirectoryName(f));
                        if (!Directory.Exists(destFolder))
                            Directory.CreateDirectory(destFolder);
                        File.Copy(Path.Combine(importPath, f), Path.Combine(widgetBasePath, f), true);
                    }
                }
                success = true;
            }
            return success;
        }
    }
}
