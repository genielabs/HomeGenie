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
using Jint.Parser;
using HomeGenie.Automation.Scripting;
using MIG.Config;

namespace HomeGenie.Service.Handlers
{
    public class Config
    {
        private HomeGenieService homegenie;
        private string widgetBasePath;
        private string tempFolderPath;
        private string groupWallpapersPath;

        public Config(HomeGenieService hg)
        {
            homegenie = hg;
            tempFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Utility.GetTmpFolder());
            widgetBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "html", "pages", "control", "widgets");
            groupWallpapersPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "html", "images", "wallpapers");
        }

        public void ProcessRequest(MigClientRequest request)
        {
            var migCommand = request.Command;

            string response = "";
            switch (migCommand.Command)
            {
            case "Interfaces.List":
                response = "[ ";
                foreach (var migInterface in homegenie.Interfaces)
                {
                    var ifaceConfig = homegenie.SystemConfiguration.MigService.GetInterface(migInterface.GetDomain());
                    if (ifaceConfig == null || !ifaceConfig.IsEnabled)
                    {
                        continue;
                    }
                    response += "{ \"Domain\" : \"" + migInterface.GetDomain() + "\", \"IsConnected\" : \"" + migInterface.IsConnected + "\" },";
                }
                if (homegenie.UpdateChecker != null && homegenie.UpdateChecker.IsUpdateAvailable)
                {
                    response += "{ \"Domain\" : \"" + Domains.HomeGenie_UpdateChecker + "\", \"IsConnected\" : \"True\" }";
                    response += " ]";
                }
                else
                {
                    response = response.Substring(0, response.Length - 1) + " ]";
                }
                request.ResponseData = response;
                break;

            case "Interfaces.ListConfig":
                response = "[ ";
                foreach (var migInterface in homegenie.Interfaces)
                {
                    var ifaceConfig = homegenie.SystemConfiguration.MigService.GetInterface(migInterface.GetDomain());
                    if (ifaceConfig == null)
                        continue;
                    response += JsonConvert.SerializeObject(ifaceConfig) + ",";
                }
                response = response.Substring(0, response.Length - 1) + " ]";
                request.ResponseData = response;
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
                            if (serialPorts[p].Contains("/ttyS")
                                || serialPorts[p].Contains("/ttyUSB")
                                || serialPorts[p].Contains("/ttyAMA")// RaZberry
                                || serialPorts[p].Contains("/ttyACM"))  // ZME_UZB1
                            {
                                portList.Add(serialPorts[p]);
                            }
                        }
                        request.ResponseData = portList;
                    }
                    else
                    {
                        var portNames = System.IO.Ports.SerialPort.GetPortNames();
                        request.ResponseData = portNames;
                    }
                    break;

                }
                break;

            case "Interface.Import":
                string downloadUrl = migCommand.GetOption(0);
                response = "";
                string ifaceFileName = Path.Combine(tempFolderPath, "mig_interface_import.zip");
                string outputFolder = Path.Combine(tempFolderPath, "mig");
                Utility.FolderCleanUp(outputFolder);

                try
                {
                    if (String.IsNullOrWhiteSpace(downloadUrl))
                    {
                        // file uploaded by user
                        MIG.Gateways.WebServiceUtility.SaveFile(request.RequestData, ifaceFileName);
                    }
                    else
                    {
                        // download file from url
                        using (var client = new WebClient())
                        {
                            client.DownloadFile(downloadUrl, ifaceFileName);
                            client.Dispose();
                        }
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

                    var migInt = homegenie.PackageManager.GetInterfaceConfig(Path.Combine(outputFolder, "configuration.xml"));
                    if (migInt != null)
                    {
                        response = String.Format("{0} ({1})\n{2}\n", migInt.Domain, migInt.AssemblyName, migInt.Description);
                        // Check for README notes and append them to the response
                        var readmeFile = Path.Combine(outputFolder, "README.TXT");
                        if (File.Exists(readmeFile))
                        {
                            response += File.ReadAllText(readmeFile);
                        }
                        request.ResponseData = new ResponseText(response);
                    }
                    else
                    {
                        request.ResponseData = new ResponseText("NOT A VALID ADD-ON PACKAGE");
                    }
                }
                catch
                {
                    // TODO: report exception
                }
                break;

            case "Interface.Install":
                // install the interface package from the unpacked archive folder
                if (homegenie.PackageManager.InterfaceInstall(Path.Combine(tempFolderPath, "mig")))
                    request.ResponseData = new ResponseText("OK");
                else
                    request.ResponseData = new ResponseText("NOT A VALID ADD-ON PACKAGE");
                break;

            case "System.GetVersion":
                request.ResponseData = homegenie.UpdateChecker.GetCurrentRelease();
                break;

            case "System.Configure":
                if (migCommand.GetOption(0) == "Service.Restart")
                {
                    Program.Quit(true);
                    request.ResponseData = new ResponseText("OK");
                }
                else if (migCommand.GetOption(0) == "UpdateManager.UpdatesList")
                {
                    request.ResponseData = homegenie.UpdateChecker.RemoteUpdates;
                }
                else if (migCommand.GetOption(0) == "UpdateManager.Check")
                {
                    homegenie.UpdateChecker.Check();
                    request.ResponseData = new ResponseText("OK");
                }
                else if (migCommand.GetOption(0) == "UpdateManager.ManualUpdate")
                {
                    homegenie.RaiseEvent(
                        Domains.HomeGenie_System,
                        Domains.HomeGenie_UpdateChecker,
                        SourceModule.Master,
                        "HomeGenie Manual Update",
                        Properties.InstallProgressMessage,
                        "Receiving update file"
                    );
                    bool success = false;
                    // file uploaded by user
                    Utility.FolderCleanUp(tempFolderPath);
                    string archivename = Path.Combine(tempFolderPath, "homegenie_update_file.tgz");
                    try
                    {
                        MIG.Gateways.WebServiceUtility.SaveFile(request.RequestData, archivename);
                        var files = Utility.UncompressTgz(archivename, tempFolderPath);
                        File.Delete(archivename);
                        string relInfo = Path.Combine(tempFolderPath, "homegenie", "release_info.xml");
                        if (File.Exists(relInfo))
                        {
                            var updateRelease = UpdateChecker.GetReleaseFile(relInfo);
                            var currentRelease = homegenie.UpdateChecker.GetCurrentRelease();
                            if (updateRelease.ReleaseDate >= currentRelease.ReleaseDate)
                            {
                                string installPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_update", "files");
                                Utility.FolderCleanUp(installPath);
                                Directory.Move(Path.Combine(tempFolderPath, "homegenie"), Path.Combine(installPath, "HomeGenie"));
                                var installStatus = homegenie.UpdateChecker.InstallFiles();
                                if (installStatus != UpdateChecker.InstallStatus.Error)
                                {
                                    success = true;
                                    if (installStatus == UpdateChecker.InstallStatus.RestartRequired)
                                    {
                                        homegenie.RaiseEvent(
                                            Domains.HomeGenie_System,
                                            Domains.HomeGenie_UpdateChecker,
                                            SourceModule.Master,
                                            "HomeGenie Manual Update",
                                            Properties.InstallProgressMessage,
                                            "HomeGenie will now restart."
                                        );
                                        Program.Quit(true);
                                    }
                                    else
                                    {
                                        homegenie.RaiseEvent(Domains.HomeGenie_System, Domains.HomeGenie_System, SourceModule.Master, "HomeGenie System", Properties.HomeGenieStatus, "UPDATED");
                                        Thread.Sleep(3000);
                                    }
                                }
                            }
                            else
                            {
                                homegenie.RaiseEvent(
                                    Domains.HomeGenie_System,
                                    Domains.HomeGenie_UpdateChecker,
                                    SourceModule.Master,
                                    "HomeGenie Manual Update",
                                    Properties.InstallProgressMessage,
                                    "ERROR: Installed release is newer than update file"
                                );
                                Thread.Sleep(3000);
                            }
                        }
                        else 
                        {
                            homegenie.RaiseEvent(
                                Domains.HomeGenie_System,
                                Domains.HomeGenie_UpdateChecker,
                                SourceModule.Master,
                                "HomeGenie Manual Update",
                                Properties.InstallProgressMessage,
                                "ERROR: Invalid update file"
                            );
                            Thread.Sleep(3000);
                        }
                    }
                    catch (Exception e)
                    {
                        homegenie.RaiseEvent(
                            Domains.HomeGenie_System,
                            Domains.HomeGenie_UpdateChecker,
                            SourceModule.Master,
                            "HomeGenie Update Checker",
                            Properties.InstallProgressMessage,
                            "ERROR: Exception occurred ("+e.Message+")"
                        );
                        Thread.Sleep(3000);
                    }
                    request.ResponseData = new ResponseStatus(success ? Status.Ok : Status.Error);
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
                    request.ResponseData = new ResponseText(resultMessage);
                }
                else if (migCommand.GetOption(0) == "UpdateManager.InstallUpdate") //UpdateManager.InstallProgramsCommit")
                {
                    string resultMessage = "OK";
                    homegenie.SaveData();
                    if (homegenie.UpdateChecker.InstallFiles() == UpdateChecker.InstallStatus.Error)
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
                            homegenie.UpdateChecker.Check();
                        }
                    }
                    request.ResponseData = new ResponseText(resultMessage);
                }
                else if (migCommand.GetOption(0) == "Statistics.GetStatisticsDatabaseMaximumSize")
                {
                    request.ResponseData = new ResponseText(homegenie.SystemConfiguration.HomeGenie.Statistics.MaxDatabaseSizeMBytes.ToString());
                }
                else if (migCommand.GetOption(0) == "Statistics.SetStatisticsDatabaseMaximumSize")
                {
                    try
                    {
                        int sizeLimit = int.Parse(migCommand.GetOption(1));
                        homegenie.SystemConfiguration.HomeGenie.Statistics.MaxDatabaseSizeMBytes = sizeLimit;
                        homegenie.SystemConfiguration.Update();
                        homegenie.Statistics.SizeLimit = sizeLimit * 1024 * 1024;
                    }
                    catch
                    {
                    }
                }
                else if (migCommand.GetOption(0) == "SystemLogging.DownloadCsv")
                {
                    string csvlog = "";
                    string logpath = Path.Combine("log", "homegenie.log");
                    if (migCommand.GetOption(1) == "1")
                    {
                        logpath = Path.Combine("log", "homegenie.log.bak");
                    }
                    else if (SystemLogger.Instance != null)
                    {                        
                        SystemLogger.Instance.FlushLog();
                    }
                    if (File.Exists(logpath))
                    {
                        using (var fs = new FileStream(logpath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var sr = new StreamReader(fs, Encoding.Default))
                        {
                            csvlog = sr.ReadToEnd();
                        }
                    }
                    (request.Context.Data as HttpListenerContext).Response.AddHeader("Content-Disposition", "attachment;filename=homegenie_log_" + migCommand.GetOption(1) + ".csv");
                    request.ResponseData = csvlog;
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
                    request.ResponseData = new ResponseText((homegenie.SystemConfiguration.HomeGenie.EnableLogFile.ToLower().Equals("true") ? "1" : "0"));
                }
                else if (migCommand.GetOption(0) == "Security.SetPassword")
                {
                    // password only for now, with fixed user login 'admin'
                    string pass = migCommand.GetOption(1) == "" ? "" : MIG.Utility.Encryption.SHA1.GenerateHashString(migCommand.GetOption(1));
                    homegenie.MigService.GetGateway("WebServiceGateway").SetOption("Password", pass);
                    homegenie.SaveData();
                }
                else if (migCommand.GetOption(0) == "Security.ClearPassword")
                {
                    homegenie.MigService.GetGateway("WebServiceGateway").SetOption("Password", "");
                    homegenie.SaveData();
                }
                else if (migCommand.GetOption(0) == "Security.HasPassword")
                {
                    var webGateway = homegenie.MigService.GetGateway("WebServiceGateway");
                    var password = webGateway.GetOption("Password");
                    request.ResponseData = new ResponseText((password == null || String.IsNullOrEmpty(password.Value) ? "0" : "1"));
                }
                else if (migCommand.GetOption(0) == "HttpService.SetWebCacheEnabled")
                {
                    if (migCommand.GetOption(1) == "1")
                    {
                        homegenie.MigService.GetGateway("WebServiceGateway").SetOption("EnableFileCaching", "true");
                    }
                    else
                    {
                        homegenie.MigService.GetGateway("WebServiceGateway").SetOption("EnableFileCaching", "false");
                    }
                    homegenie.SystemConfiguration.Update();
                    request.ResponseData = new ResponseText("OK");
                }
                else if (migCommand.GetOption(0) == "HttpService.GetWebCacheEnabled")
                {
                    var fileCaching = homegenie.MigService.GetGateway("WebServiceGateway").GetOption("EnableFileCaching");
                    request.ResponseData = new ResponseText(fileCaching != null ? fileCaching.Value : "false");  
                }
                else if (migCommand.GetOption(0) == "HttpService.GetPort")
                {
                    var port = homegenie.MigService.GetGateway("WebServiceGateway").GetOption("Port");
                    request.ResponseData = new ResponseText(port != null ? port.Value : "8080");
                }
                else if (migCommand.GetOption(0) == "HttpService.SetPort")
                {
                    homegenie.MigService.GetGateway("WebServiceGateway").SetOption("Port", migCommand.GetOption(1));
                    homegenie.SystemConfiguration.Update();
                }
                else if (migCommand.GetOption(0) == "HttpService.GetHostHeader")
                {
                    var host = homegenie.MigService.GetGateway("WebServiceGateway").GetOption("Host");
                    request.ResponseData = new ResponseText(host != null ? host.Value : "*");
                }
                else if (migCommand.GetOption(0) == "HttpService.SetHostHeader")
                {
                    homegenie.MigService.GetGateway("WebServiceGateway").SetOption("Host", migCommand.GetOption(1));
                    homegenie.SystemConfiguration.Update();
                }
                else if (migCommand.GetOption(0) == "System.ConfigurationRestore")
                {
                    // file uploaded by user
                    Utility.FolderCleanUp(tempFolderPath);
                    string archivename = Path.Combine(tempFolderPath, "homegenie_restore_config.zip");
                    try
                    {
                        MIG.Gateways.WebServiceUtility.SaveFile(request.RequestData, archivename);
                        Utility.UncompressZip(archivename, tempFolderPath);
                        File.Delete(archivename);
                        request.ResponseData = new ResponseStatus(Status.Ok);
                    }
                    catch
                    {
                        request.ResponseData = new ResponseStatus(Status.Error);
                    }
                }
                else if (migCommand.GetOption(0) == "System.ConfigurationRestoreS1")
                {
                    var serializer = new XmlSerializer(typeof(List<ProgramBlock>));
                    var reader = new StreamReader(Path.Combine(tempFolderPath, "programs.xml"));
                    var newProgramsData = (List<ProgramBlock>)serializer.Deserialize(reader);
                    reader.Close();
                    var newProgramList = new List<ProgramBlock>();
                    foreach (ProgramBlock program in newProgramsData)
                    {
                        if (program.Address >= ProgramManager.USERSPACE_PROGRAMS_START && program.Address < ProgramManager.PACKAGE_PROGRAMS_START)
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
                    request.ResponseData = newProgramList;
                }
                else if (migCommand.GetOption(0) == "System.ConfigurationRestoreS2")
                {
                    var success = homegenie.BackupManager.RestoreConfiguration(tempFolderPath, migCommand.GetOption(1));
                    request.ResponseData = new ResponseText(success ? "OK" : "ERROR");
                }
                else if (migCommand.GetOption(0) == "System.ConfigurationReset")
                {
                    homegenie.RestoreFactorySettings();
                }
                else if (migCommand.GetOption(0) == "System.ConfigurationBackup")
                {
                    homegenie.BackupManager.BackupConfiguration("html/homegenie_backup_config.zip");
                    (request.Context.Data as HttpListenerContext).Response.Redirect("/hg/html/homegenie_backup_config.zip");
                }
                else if (migCommand.GetOption(0) == "System.ConfigurationLoad")
                {
                    homegenie.SoftReload();
                }
                break;

            case "Modules.Get":
                try
                {
                    var module = homegenie.Modules.Find(m => m.Domain == migCommand.GetOption(0) && m.Address == migCommand.GetOption(1));
                    request.ResponseData = Utility.Module2Json(module, false);
                }
                catch (Exception ex)
                {
                    request.ResponseData = new ResponseText("ERROR: \n" + ex.Message + "\n\n" + ex.StackTrace);
                }
                break;

            case "Modules.ParameterGet":
                try
                {
                    var module = homegenie.Modules.Find(m => m.Domain == migCommand.GetOption(0) && m.Address == migCommand.GetOption(1));
                    var parameter = Utility.ModuleParameterGet(module, migCommand.GetOption(2));
                    if (parameter != null)
                        request.ResponseData = JsonConvert.SerializeObject(parameter, Formatting.Indented);
                    else
                        request.ResponseData = new ResponseText("ERROR: Unknown parameter '" + migCommand.GetOption(2) + "'");
                }
                catch (Exception ex)
                {
                    request.ResponseData = new ResponseText("ERROR: \n" + ex.Message + "\n\n" + ex.StackTrace);
                }
                break;

            case "Modules.ParameterSet":
                try
                {
                    var module = homegenie.Modules.Find(m => m.Domain == migCommand.GetOption(0) && m.Address == migCommand.GetOption(1));
                    homegenie.RaiseEvent(Domains.HomeGenie_System, module.Domain, module.Address, module.Description, migCommand.GetOption(2), migCommand.GetOption(3));
                    request.ResponseData = new ResponseText("OK");
                }
                catch (Exception ex)
                {
                    request.ResponseData = new ResponseText("ERROR: \n" + ex.Message + "\n\n" + ex.StackTrace);
                }
                break;

            case "Modules.StatisticsGet":
                try
                {
                    var module = homegenie.Modules.Find(m => m.Domain == migCommand.GetOption(0) && m.Address == migCommand.GetOption(1));
                    var parameter = Utility.ModuleParameterGet(module, migCommand.GetOption(2));
                    if (parameter != null)
                        request.ResponseData = JsonConvert.SerializeObject(parameter.Statistics, Formatting.Indented);
                    else
                        request.ResponseData = new ResponseText("ERROR: Unknown parameter '" + migCommand.GetOption(2) + "'");
                }
                catch (Exception ex)
                {
                    request.ResponseData = new ResponseText("ERROR: \n" + ex.Message + "\n\n" + ex.StackTrace);
                }
                break;

            case "Modules.List":
                try
                {
                    homegenie.modules_Sort();
                    request.ResponseData = homegenie.GetJsonSerializedModules(migCommand.GetOption(0).ToLower() == "short");
                }
                catch (Exception ex)
                {
                    request.ResponseData = new ResponseText("ERROR: \n" + ex.Message + "\n\n" + ex.StackTrace);
                }
                break;

            case "Modules.RoutingReset":
                try
                {
                    for (int m = 0; m < homegenie.Modules.Count; m++)
                    {
                        homegenie.Modules[m].RoutingNode = "";
                    }
                    request.ResponseData = new ResponseText("OK");
                }
                catch (Exception ex)
                {
                    request.ResponseData = new ResponseText("ERROR: \n" + ex.Message + "\n\n" + ex.StackTrace);
                }
                break;

            case "Modules.Save":
                string body = request.RequestText;
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
                            if (propertyName == Properties.VirtualMeterWatts)
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
                string streamContent = request.RequestText;
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
                            homegenie.RaiseEvent(Domains.HomeGenie_System, currentModule.Domain, currentModule.Address, currentModule.Description, newParameter.Name, newParameter.Value);
                        }
                        else if (newParameter.NeedsUpdate)
                        {
                            // reset current reporting Watts if VMWatts field is set to 0
                            if (newParameter.Name == Properties.VirtualMeterWatts && newParameter.DecimalValue == 0 && currentParameter.DecimalValue != 0)
                            {
                                homegenie.RaiseEvent(Domains.HomeGenie_System, currentModule.Domain, currentModule.Address, currentModule.Description, Properties.MeterWatts, "0.0");
                            }
                            else if (newParameter.Value != currentParameter.Value)
                            {
                                homegenie.RaiseEvent(Domains.HomeGenie_System, currentModule.Domain, currentModule.Address, currentModule.Description, newParameter.Name, newParameter.Value);
                            }
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
                request.ResponseData = new ResponseText("OK");
                //
                homegenie.UpdateModulesDatabase();
                break;

            case "Stores.List":
                {
                    var module = homegenie.Modules.Find(m => m.Domain == migCommand.GetOption(0) && m.Address == migCommand.GetOption(1));
                    if (module != null)
                    {
                        //module.Stores
                        response = "[";
                        for (int s = 0; s < module.Stores.Count; s++)
                        {
                            response += "{ \"Name\": \"" + Utility.XmlEncode(module.Stores[s].Name) + "\", \"Description\": \"" + Utility.XmlEncode(module.Stores[s].Description) + "\" },";
                        }
                        response = response.TrimEnd(',') + "]";
                        request.ResponseData = response;
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
                        response = "[";
                        var store = new StoreHelper(module.Stores, migCommand.GetOption(2));
                        for (int p = 0; p < store.List.Count; p++)
                        {
                            response += "{ \"Name\": \"" + Utility.XmlEncode(store.List[p].Name) + "\", \"Description\": \"" + Utility.XmlEncode(store.List[p].Description) + "\" },";
                        }
                        response = response.TrimEnd(',') + "]";
                        request.ResponseData = response;
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
                        request.ResponseData = store.Get(migCommand.GetOption(3));
                    }
                }
                break;

            case "Stores.ItemSet":
                {
                    // value is the POST body
                    string itemData = request.RequestText;
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
                    request.ResponseData = jsonmodules;
                }
                break;
            case "Groups.List":
                try
                {
                    request.ResponseData = homegenie.GetGroups(migCommand.GetOption(0));
                }
                catch (Exception ex)
                {
                    request.ResponseData = new ResponseText("ERROR: \n" + ex.Message + "\n\n" + ex.StackTrace);
                }
                break;

            case "Groups.Rename":
                string oldName = migCommand.GetOption(1);
                string newName = request.RequestText;
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
                    request.ResponseData = new ResponseText("New name already in use.");
                }
                break;

            case "Groups.Sort":
                {
                    var newGroupList = new List<Group>();
                    string[] newPositionOrder = request.RequestText.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < newPositionOrder.Length; i++)
                    {
                        newGroupList.Add(homegenie.GetGroups(migCommand.GetOption(0))[int.Parse(newPositionOrder[i])]);
                    }
                    homegenie.GetGroups(migCommand.GetOption(0)).Clear();
                    homegenie.GetGroups(migCommand.GetOption(0)).RemoveAll(g => true);
                    homegenie.GetGroups(migCommand.GetOption(0)).AddRange(newGroupList);
                    homegenie.UpdateGroupsDatabase(migCommand.GetOption(0));
                }
                try
                {
                    request.ResponseData = homegenie.GetGroups(migCommand.GetOption(0));
                }
                catch (Exception ex)
                {
                    request.ResponseData = new ResponseText("ERROR: \n" + ex.Message + "\n\n" + ex.StackTrace);
                }
                break;

            case "Groups.SortModules":
                {
                    string groupName = migCommand.GetOption(1);
                    Group sortGroup = null;
                    sortGroup = homegenie.GetGroups(migCommand.GetOption(0)).Find(zn => zn.Name == groupName);
                    if (sortGroup != null)
                    {
                        var newModulesReference = new List<ModuleReference>();
                        string[] newPositionOrder = request.RequestText.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
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
                    request.ResponseData = homegenie.GetGroups(migCommand.GetOption(0));
                }
                catch (Exception ex)
                {
                    request.ResponseData = new ResponseText("ERROR: \n" + ex.Message + "\n\n" + ex.StackTrace);
                }
                break;

            case "Groups.Add":
                string newGroupName = request.RequestText;
                homegenie.GetGroups(migCommand.GetOption(0)).Add(new Group() { Name = newGroupName });
                homegenie.UpdateGroupsDatabase(migCommand.GetOption(0));//write groups
                break;

            case "Groups.Delete":
                string deletedGroupName = request.RequestText;
                Group deletedGroup = null;
                try
                {
                    deletedGroup = homegenie.GetGroups(migCommand.GetOption(0)).Find(zn => zn.Name == deletedGroupName);
                }
                catch
                {
                }
                //
                if (deletedGroup != null)
                {
                    homegenie.GetGroups(migCommand.GetOption(0)).Remove(deletedGroup);
                    homegenie.UpdateGroupsDatabase(migCommand.GetOption(0));//write groups
                    if (migCommand.GetOption(0).ToLower() == "automation")
                    {
                        var groupPrograms = homegenie.ProgramManager.Programs.FindAll(p => p.Group.ToLower() == deletedGroup.Name.ToLower());
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
                string jsonGroups = request.RequestText;
                var newGroups = JsonConvert.DeserializeObject<List<Group>>(jsonGroups);
                for (int i = 0; i < newGroups.Count; i++)
                {
                    try
                    {
                        var group = homegenie.Groups.Find(z => z.Name == newGroups[i].Name);
                        group.Modules.Clear();
                        group.Modules = newGroups[i].Modules;
                    }
                    catch
                    {
                    }
                }
                homegenie.UpdateGroupsDatabase(migCommand.GetOption(0));//write groups
                break;

            case "Groups.WallpaperList":
                List<string> wallpaperList = new List<string>();
                var images = Directory.GetFiles(groupWallpapersPath);
                for (int i = 0; i < images.Length; i++)
                {
                    wallpaperList.Add(Path.GetFileName(images[i]));
                }
                request.ResponseData = wallpaperList;

                break;

            case "Groups.WallpaperAdd":
                {
                    string wallpaperFile = "";
                    try
                    {
                        wallpaperFile = MIG.Gateways.WebServiceUtility.SaveFile(request.RequestData, groupWallpapersPath);
                    }
                    catch
                    {
                    }
                    request.ResponseData = new ResponseText(Path.GetFileName(wallpaperFile));
                }
                break;

            case "Groups.WallpaperSet":
                {
                    string wpGroupName = migCommand.GetOption(0);
                    var wpGroup = homegenie.GetGroups(migCommand.GetOption(0)).Find(g => g.Name == wpGroupName);
                    if (wpGroup != null)
                    {
                        wpGroup.Wallpaper = migCommand.GetOption(1);
                        homegenie.UpdateGroupsDatabase("Control");
                    }
                }
                break;

            case "Groups.WallpaperDelete":
                {
                    string wallpaperFile = migCommand.GetOption(0);
                    wallpaperFile = Path.Combine(groupWallpapersPath, Path.GetFileName(wallpaperFile));
                    if (File.Exists(wallpaperFile))
                    {
                        File.Delete(wallpaperFile);
                    }
                    request.ResponseData = new ResponseText("OK");
                }
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
                request.ResponseData = widgetsList;
                break;

            case "Widgets.Add":
                {
                    var status = Status.Error;
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
                            status = Status.Ok;
                        }
                    }
                    request.ResponseData = new ResponseStatus(status);
                }
                break;

            case "Widgets.Save":
                {
                    var status = Status.Error;
                    string widgetData = request.RequestText;
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
                        status = Status.Ok;
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
                    request.ResponseData = new ResponseStatus(status);
                }
                break;

            case "Widgets.Delete":
                {
                    var status = Status.Error;
                    string widgetPath = migCommand.GetOption(0); // eg. homegenie/generic/dimmer
                    string[] widgetParts = widgetPath.Split('/');
                    string filePath = Path.Combine(widgetBasePath, widgetParts[0], widgetParts[1], widgetParts[2] + ".");
                    if (File.Exists(filePath + "html"))
                    {
                        File.Delete(filePath + "html");
                        status = Status.Ok;
                    }
                    if (File.Exists(filePath + "js"))
                    {
                        File.Delete(filePath + "js");
                        status = Status.Ok;
                    }
                    request.ResponseData = new ResponseStatus(status);
                }
                break;

            case "Widgets.Export":
                {
                    string widgetPath = migCommand.GetOption(0); // eg. homegenie/generic/dimmer
                    string[] widgetParts = widgetPath.Split('/');
                    string widgetBundle = Path.Combine(tempFolderPath, "export", widgetPath.Replace('/', '_') + ".zip");
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
                    (request.Context.Data as HttpListenerContext).Response.AddHeader("Content-Disposition", "attachment; filename=\"" + widgetPath.Replace('/', '_') + ".zip\"");
                    (request.Context.Data as HttpListenerContext).Response.OutputStream.Write(bundleData, 0, bundleData.Length);
                }
                break;
                
            case "Widgets.Import":
                {
                    string archiveFile = Path.Combine(tempFolderPath, "import_widget.zip");
                    string importPath = Path.Combine(tempFolderPath, "import");
                    if (Directory.Exists(importPath))
                        Directory.Delete(importPath, true);
                    MIG.Gateways.WebServiceUtility.SaveFile(request.RequestData, archiveFile);
                    if (homegenie.PackageManager.WidgetImport(archiveFile, importPath))
                    {
                        request.ResponseData = new ResponseText("OK");
                    }
                    else
                    {
                        request.ResponseData = new ResponseText("ERROR");
                    }
                }
                break;

            case "Widgets.Parse":
                {
                    string widgetData = request.RequestText;
                    var parser = new JavaScriptParser();
                    try
                    {
                        request.ResponseData = new ResponseText("OK");
                        parser.Parse(widgetData);
                    }
                    catch (Jint.Parser.ParserException e)
                    {
                        request.ResponseData = new ResponseText("ERROR (" + e.LineNumber + "," + e.Column + "): " + e.Description);
                    }
                }
                break;

            case "Package.Get":
                {
                    string pkgFolderUrl = migCommand.GetOption(0);
                    var pkg = homegenie.PackageManager.GetInstalledPackage(pkgFolderUrl);
                    request.ResponseData = pkg;
                }
                break;

            case "Package.List":
                // TODO: get the list of installed packages...
                break;
                
            case "Package.Install":
                {
                    string pkgFolderUrl = migCommand.GetOption(0);
                    string installFolder = Path.Combine(tempFolderPath, "pkg");
                    bool success = homegenie.PackageManager.InstallPackage(pkgFolderUrl, installFolder);
                    if (success)
                    {
                        homegenie.UpdateProgramsDatabase();
                        homegenie.SaveData();
                    }
                    request.ResponseData = new ResponseText(success ? "OK" : "ERROR");
                }
                break;

            case "Package.Uninstall":
                // TODO: uninstall a package....
                break;

            }
        }

    }
}
