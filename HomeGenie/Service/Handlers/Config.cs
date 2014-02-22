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
using System.Xml.Serialization;

namespace HomeGenie.Service.Handlers
{
    public class Config
    {
        private HomeGenieService _hg;
        public Config(HomeGenieService hg)
        {
            _hg = hg;
        }

        public void ProcessRequest(MIGClientRequest request, MIGInterfaceCommand migcmd)
        {

            switch (migcmd.command)
            {
                case "Interfaces.List":
                    migcmd.response = "[ ";
                    foreach (KeyValuePair<string, MIGInterface> kv in _hg.Interfaces)
                    {
                        MIGInterface mi = kv.Value;
                        MIGServiceConfiguration.Interface ifaceconfig = _hg.SystemConfiguration.GetInterface(mi.Domain);
                        if (ifaceconfig == null || !ifaceconfig.IsEnabled)
                        {
                            continue;
                        }
                        migcmd.response += "{ \"Domain\" : \"" + mi.Domain + "\", \"IsConnected\" : \"" + mi.IsConnected + "\" },";
                    }
                    if (_hg.UpdateChecker != null && _hg.UpdateChecker.IsUpdateAvailable)
                    {
                        migcmd.response += "{ \"Domain\" : \"HomeGenie.UpdateChecker\", \"IsConnected\" : \"True\" }";
                        migcmd.response += " ]";
                    }
                    else
                    {
                        migcmd.response = migcmd.response.Substring(0, migcmd.response.Length - 1) + " ]";
                    }
                    //
                    break;

                case "Interfaces.Configure":
                    switch (migcmd.GetOption(0))
                    {
                        case "Hardware.SerialPorts":
                            if (Environment.OSVersion.Platform == PlatformID.Unix)
                            {
                                string[] sysports = System.IO.Ports.SerialPort.GetPortNames();
                                List<string> lports = new List<string>();
                                for (int p = sysports.Length - 1; p >= 0; p--)
                                {
                                    if (sysports[p].Contains("/ttyS") || sysports[p].Contains("/ttyUSB"))
                                    {
                                        lports.Add(sysports[p]);
                                    }
                                }
                                if (Raspberry.Board.Current.IsRaspberryPi && !lports.Contains("/dev/ttyAMA0"))
                                {
                                    lports.Add("/dev/ttyAMA0");
                                }
                                migcmd.response = JsonHelper.GetSimpleResponse(JsonConvert.SerializeObject(lports));
                            }
                            else
                            {
                                string[] ports = System.IO.Ports.SerialPort.GetPortNames();
                                migcmd.response = JsonHelper.GetSimpleResponse(JsonConvert.SerializeObject(ports));
                            }
                            break;

                        case "LircRemote.GetIsEnabled":
                            migcmd.response = JsonHelper.GetSimpleResponse((_hg.SystemConfiguration.GetInterface(Domains.Controllers_LircRemote).IsEnabled ? "1" : "0"));
                            break;

                        case "LircRemote.SetIsEnabled":
                            _hg.SystemConfiguration.GetInterface(Domains.Controllers_LircRemote).IsEnabled = (migcmd.GetOption(1) == "1" ? true : false);
                            _hg.SystemConfiguration.Update();
                            //
                            if (_hg.SystemConfiguration.GetInterface(Domains.Controllers_LircRemote).IsEnabled)
                            {
                                _hg.GetInterface(Domains.Controllers_LircRemote).Connect();
                            }
                            else
                            {
                                _hg.GetInterface(Domains.Controllers_LircRemote).Disconnect();
                            }
                            break;

                        case "CameraInput.GetIsEnabled":
                            migcmd.response = JsonHelper.GetSimpleResponse((_hg.SystemConfiguration.GetInterface(Domains.Media_CameraInput).IsEnabled ? "1" : "0"));
                            break;

                        case "CameraInput.SetIsEnabled":
                            _hg.SystemConfiguration.GetInterface(Domains.Media_CameraInput).IsEnabled = (migcmd.GetOption(1) == "1" ? true : false);
                            _hg.SystemConfiguration.Update();
                            //
                            if (_hg.SystemConfiguration.GetInterface(Domains.Media_CameraInput).IsEnabled)
                            {
                                _hg.GetInterface(Domains.Media_CameraInput).Connect();
                            }
                            else
                            {
                                _hg.GetInterface(Domains.Media_CameraInput).Disconnect();
                            }
                            break;

                        case "ZWave.GetIsEnabled":
                            migcmd.response = JsonHelper.GetSimpleResponse((_hg.SystemConfiguration.GetInterface(Domains.HomeAutomation_ZWave).IsEnabled ? "1" : "0"));
                            break;

                        case "ZWave.SetIsEnabled":
                            _hg.SystemConfiguration.GetInterface(Domains.HomeAutomation_ZWave).IsEnabled = (migcmd.GetOption(1) == "1" ? true : false);
                            _hg.SystemConfiguration.Update();
                            if (_hg.SystemConfiguration.GetInterface(Domains.HomeAutomation_ZWave).IsEnabled)
                            {
                                _hg.InterfaceEnable(Domains.HomeAutomation_ZWave);
                            }
                            else
                            {
                                _hg.InterfaceDisable(Domains.HomeAutomation_ZWave);
                            }
                            break;

                        case "ZWave.SetPort":
                            (_hg.GetInterface(Domains.HomeAutomation_ZWave) as MIG.Interfaces.HomeAutomation.ZWave).SetPortName(migcmd.GetOption(1).Replace("|", "/"));
                            _hg.SystemConfiguration.GetInterfaceOption(Domains.HomeAutomation_ZWave, "Port").Value = migcmd.GetOption(1).Replace("|", "/");
                            _hg.SystemConfiguration.Update();
                            break;

                        case "ZWave.GetPort":
                            migcmd.response = (_hg.GetInterface(Domains.HomeAutomation_ZWave) as MIG.Interfaces.HomeAutomation.ZWave).GetPortName();
                            migcmd.response = JsonHelper.GetSimpleResponse(migcmd.response.Replace("/", "|"));
                            break;

                        case "X10.GetIsEnabled":
                            migcmd.response = JsonHelper.GetSimpleResponse((_hg.SystemConfiguration.GetInterface(Domains.HomeAutomation_X10).IsEnabled ? "1" : "0"));
                            break;

                        case "X10.SetIsEnabled":
                            _hg.SystemConfiguration.GetInterface(Domains.HomeAutomation_X10).IsEnabled = (migcmd.GetOption(1) == "1" ? true : false);
                            _hg.SystemConfiguration.Update();
                            //
                            if (_hg.SystemConfiguration.GetInterface(Domains.HomeAutomation_X10).IsEnabled)
                            {
                                _hg.InterfaceEnable(Domains.HomeAutomation_X10);
                            }
                            else
                            {
                                _hg.InterfaceDisable(Domains.HomeAutomation_X10);
                            }
                            break;

                        case "X10.SetPort":
                            (_hg.GetInterface(Domains.HomeAutomation_X10) as MIG.Interfaces.HomeAutomation.X10).SetPortName(migcmd.GetOption(1).Replace("|", "/"));
                            _hg.SystemConfiguration.GetInterfaceOption(Domains.HomeAutomation_X10, "Port").Value = migcmd.GetOption(1).Replace("|", "/");
                            _hg.SystemConfiguration.Update();
                            break;

                        case "X10.GetPort":
                            migcmd.response = (_hg.GetInterface(Domains.HomeAutomation_X10) as MIG.Interfaces.HomeAutomation.X10).GetPortName();
                            migcmd.response = JsonHelper.GetSimpleResponse(migcmd.response.Replace("/", "|"));
                            break;

                        case "X10.SetHouseCodes":
                            (_hg.GetInterface(Domains.HomeAutomation_X10) as MIG.Interfaces.HomeAutomation.X10).SetHouseCodes(migcmd.GetOption(1));
                            _hg.SystemConfiguration.GetInterfaceOption(Domains.HomeAutomation_X10, "HouseCodes").Value = migcmd.GetOption(1);
                            _hg.SystemConfiguration.Update();
                            // discard modules that don't belong to given housecodes
                            for (int m = 0; m < _hg.Modules.Count; m++)
                            {
                                if (_hg.Modules[m].Domain == Domains.HomeAutomation_X10 && !migcmd.GetOption(1).ToUpper().Contains(_hg.Modules[m].Address.Substring(0, 1).ToUpper()) && _hg.Modules[m].Address != "RF")
                                {
                                    for (int g = 0; g < _hg.Groups.Count; g++)
                                    {
                                        ModuleReference mref = null;
                                        do
                                        {
                                            mref = _hg.Groups[g].Modules.Find(mr => mr.Address == _hg.Modules[m].Address && mr.Domain == _hg.Modules[m].Domain);
                                            if (mref != null)
                                            {
                                                _hg.Groups[g].Modules.Remove(mref);
                                            }
                                        } while (mref != null);
                                    }
                                    //
                                    _hg.Modules.RemoveAt(m);
                                    m--;
                                }
                            }
                            // list interfaces as JSON
                            migcmd.response = _hg.GetJsonSerializedModules(false);
                            _hg.UpdateGroupsDatabase("Control"); //write groups
                            _hg.UpdateModulesDatabase(); //write modules
                            break;

                        case "X10.GetHouseCodes":
                            migcmd.response = JsonHelper.GetSimpleResponse((_hg.GetInterface(Domains.HomeAutomation_X10) as MIG.Interfaces.HomeAutomation.X10).GetHouseCodes());
                            break;

                        case "W800RF.GetPort":
                            migcmd.response = (_hg.GetInterface(Domains.HomeAutomation_W800RF) as MIG.Interfaces.HomeAutomation.W800RF).GetPortName();
                            migcmd.response = JsonHelper.GetSimpleResponse(migcmd.response.Replace("/", "|"));
                            break;

                        case "W800RF.SetPort":
                            (_hg.GetInterface(Domains.HomeAutomation_W800RF) as MIG.Interfaces.HomeAutomation.W800RF).SetPortName(migcmd.GetOption(1).Replace("|", "/"));
                            _hg.SystemConfiguration.GetInterfaceOption(Domains.HomeAutomation_W800RF, "Port").Value = migcmd.GetOption(1).Replace("|", "/");
                            _hg.SystemConfiguration.Update();
                            break;

                        case "W800RF.GetIsEnabled":
                            migcmd.response = JsonHelper.GetSimpleResponse((_hg.SystemConfiguration.GetInterface(Domains.HomeAutomation_W800RF).IsEnabled ? "1" : "0"));
                            break;

                        case "W800RF.SetIsEnabled":
                            _hg.SystemConfiguration.GetInterface(Domains.HomeAutomation_W800RF).IsEnabled = (migcmd.GetOption(1) == "1" ? true : false);
                            _hg.SystemConfiguration.Update();
                            //
                            if (_hg.SystemConfiguration.GetInterface(Domains.HomeAutomation_W800RF).IsEnabled)
                            {
                                _hg.GetInterface(Domains.HomeAutomation_W800RF).Connect();
                            }
                            else
                            {
                                _hg.GetInterface(Domains.HomeAutomation_W800RF).Disconnect();
                            }
                            break;

                        case "RaspiGPIO.GetIsEnabled":
                            migcmd.response = JsonHelper.GetSimpleResponse((_hg.SystemConfiguration.GetInterface(Domains.EmbeddedSystems_RaspiGPIO).IsEnabled ? "1" : "0"));
                            break;

                        case "RaspiGPIO.SetIsEnabled":
                            _hg.SystemConfiguration.GetInterface(Domains.EmbeddedSystems_RaspiGPIO).IsEnabled = (migcmd.GetOption(1) == "1" ? true : false);
                            _hg.SystemConfiguration.Update();
                            //
                            if (_hg.SystemConfiguration.GetInterface(Domains.EmbeddedSystems_RaspiGPIO).IsEnabled)
                            {
                                _hg.GetInterface(Domains.EmbeddedSystems_RaspiGPIO).Connect();
                            }
                            else
                            {
                                _hg.GetInterface(Domains.EmbeddedSystems_RaspiGPIO).Disconnect();
                            }
                            break;

                        case "UPnP.GetIsEnabled":
                            migcmd.response = JsonHelper.GetSimpleResponse((_hg.SystemConfiguration.GetInterface(Domains.Protocols_UPnP).IsEnabled ? "1" : "0"));
                            break;

                        case "UPnP.SetIsEnabled":
                            _hg.SystemConfiguration.GetInterface(Domains.Protocols_UPnP).IsEnabled = (migcmd.GetOption(1) == "1" ? true : false);
                            _hg.SystemConfiguration.Update();
                            //
                            if (_hg.SystemConfiguration.GetInterface(Domains.Protocols_UPnP).IsEnabled)
                            {
                                _hg.InterfaceEnable(Domains.Protocols_UPnP);
                            }
                            else
                            {
                                _hg.InterfaceDisable(Domains.Protocols_UPnP);
                            }
                            break;

                    }
                    break;

                case "System.Configure":
                    if (migcmd.GetOption(0) == "UpdateManager.UpdatesList")
                    {
                        migcmd.response = JsonConvert.SerializeObject(_hg.UpdateChecker.RemoteUpdates);
                    }
                    else if (migcmd.GetOption(0) == "UpdateManager.Check")
                    {
                        _hg.UpdateChecker.Check();
                        migcmd.response = JsonHelper.GetSimpleResponse("OK");
                    }
                    else if (migcmd.GetOption(0) == "UpdateManager.DownloadUpdate")
                    {
                        string res = "ERROR";
                        bool success = _hg.UpdateChecker.DownloadUpdateFiles();
                        if (success)
                        {
                            if (_hg.UpdateChecker.IsRestartRequired)
                            {
                                res = "RESTART";
                            }
                            else
                            {
                                res = "OK";
                            }
                        }
                        migcmd.response = JsonHelper.GetSimpleResponse(res);
                    }
                    else if (migcmd.GetOption(0) == "UpdateManager.InstallProgramsList")
                    {
                        List<ProgramBlock> prgs = new List<ProgramBlock>();
                        // We assume that first 999 (0<id<1000) programs are "system" programs whose id must match for all hg releases,
                        // so after upgrade, system programs contained in the update will replace current system programs (new ones added)
                        var newprgdata = Path.Combine(_hg.UpdateChecker.UpdateBaseFolder, "programs.xml");
                        if (File.Exists(newprgdata))
                        {
                            XmlSerializer pserializer = new XmlSerializer(typeof(List<ProgramBlock>));
                            StreamReader preader = new StreamReader(newprgdata);
                            List<ProgramBlock> pl = (List<ProgramBlock>)pserializer.Deserialize(preader);
                            preader.Close();
                            foreach (ProgramBlock pb in pl)
                            {
                                ProgramBlock p = new ProgramBlock();
                                p.Address = pb.Address;
                                p.Name = pb.Name;
                                p.Description = pb.Description;
                                prgs.Add(p);
                            }
                            prgs.Sort(delegate(ProgramBlock p1, ProgramBlock p2)
                            {
                                string c1 = /*p1.Name + " " +*/ p1.Address.ToString();
                                string c2 = /*p2.Name + " " +*/ p2.Address.ToString();
                                return c1.CompareTo(c2);
                            });
                        }
                        migcmd.response = JsonConvert.SerializeObject(prgs);
                    }
                    else if (migcmd.GetOption(0) == "UpdateManager.InstallUpdate") //UpdateManager.InstallProgramsCommit")
                    {
                        string res = "OK";
                        var newprgdata = Path.Combine(_hg.UpdateChecker.UpdateBaseFolder, "programs.xml");
                        if (File.Exists(newprgdata))
                        {
                            var plist = migcmd.GetOption(1); // comma separated programs' id list
                            List<ProgramBlock> pl = new List<ProgramBlock>();
                            //
                            try
                            {
                                XmlSerializer pserializer = new XmlSerializer(typeof(List<ProgramBlock>));
                                StreamReader preader = new StreamReader(newprgdata);
                                pl = (List<ProgramBlock>)pserializer.Deserialize(preader);
                                preader.Close();
                            }
                            catch { } // TODO: handle error during programs deserialization
                            //
                            try
                            {
                                if (plist != "" && pl.Count > 0)
                                {

                                    _hg.ProgramEngine.Enabled = false;
                                    _hg.ProgramEngine.StopEngine();
                                    //
                                    foreach (ProgramBlock pb in pl)
                                    {
                                        if (pb.Address < ProgramEngine.USER_SPACE_PROGRAMS_START) // && plist.Contains("," + pb.Address.ToString() + ","))
                                        {
                                            try
                                            {
                                                File.Copy(Path.Combine(_hg.UpdateChecker.UpdateBaseFolder, "programs", pb.Address + ".dll"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs", pb.Address + ".dll"), true);
                                            }
                                            catch (Exception)
                                            {

                                            }
                                            ProgramBlock oldprogram = _hg.ProgramEngine.Programs.Find(p => p.Address == pb.Address);
                                            if (oldprogram != null)
                                            {
                                                _hg.ProgramEngine.ProgramRemove(oldprogram);
                                            }
                                            _hg.ProgramEngine.ProgramAdd(pb);
                                        }
                                    }
                                    //
                                    _hg.UpdateProgramsDatabase();
                                    //
                                    // add new automation groups 
                                    // TODO: should ignore not-imported programs groups
                                    //
                                    List<Group> agroups = new List<Group>();
                                    //
                                    try
                                    {
                                        XmlSerializer zserializer = new XmlSerializer(typeof(List<Group>));
                                        StreamReader zreader = new StreamReader(Path.Combine(_hg.UpdateChecker.UpdateBaseFolder, "automationgroups.xml"));
                                        agroups = (List<Group>)zserializer.Deserialize(zreader);
                                        zreader.Close();
                                    }
                                    catch { }
                                    //
                                    foreach (Group agrp in agroups)
                                    {
                                        if (_hg.AutomationGroups.Find(g => g.Name == agrp.Name) == null)
                                        {
                                            _hg.AutomationGroups.Add(agrp);
                                        }
                                    }
                                    //
                                    _hg.UpdateGroupsDatabase("Automation");
                                    //
                                    if (!_hg.UpdateChecker.IsRestartRequired)
                                    {
                                        _hg.LoadConfiguration();
                                    }
                                }
                                //
                                File.Delete(newprgdata);
                                if (Directory.Exists(Path.Combine(_hg.UpdateChecker.UpdateBaseFolder, "programs")))
                                {
                                    Directory.Delete(Path.Combine(_hg.UpdateChecker.UpdateBaseFolder, "programs"), true);
                                }
                            }
                            catch
                            {
                                res = "ERROR";
                            }

                        }

                        //
                        //                                }
                        //                                else if (cmd.option == "UpdateManager.InstallUpdate")
                        //                                {
                        if (res == "OK")
                        {
                            if (_hg.UpdateChecker.IsRestartRequired)
                            {
                                res = "RESTART";
                                Utility.RunAsyncTask(() =>
                                {
                                    System.Threading.Thread.Sleep(2000);
                                    Program.Quit(true);
                                });
                            }
                            else
                            {
                                Process updater = Utility.StartUpdater(false);
                                // wait for HomeGenieUpdater.exe to exit
                                updater.WaitForExit();
                                _hg.UpdateChecker.Check();
                            }
                        }
                        migcmd.response = JsonHelper.GetSimpleResponse(res);
                    }
                    else if (migcmd.GetOption(0) == "HttpService.GetPort")
                    {
                        migcmd.response = JsonHelper.GetSimpleResponse(_hg.SystemConfiguration.HomeGenie.ServicePort.ToString());
                    }
                    else if (migcmd.GetOption(0) == "HttpService.SetPort")
                    {
                        try
                        {
                            _hg.SystemConfiguration.HomeGenie.ServicePort = int.Parse(migcmd.GetOption(1));
                            _hg.SystemConfiguration.Update();
                        }
                        catch
                        {
                        }
                    }
                    else if (migcmd.GetOption(0) == "SystemLogging.Enable")
                    {
                        SystemLogger.Instance.OpenLog();
                        _hg.SystemConfiguration.HomeGenie.EnableLogFile = "true";
                        _hg.SystemConfiguration.Update();
                    }
                    else if (migcmd.GetOption(0) == "SystemLogging.Disable")
                    {
                        SystemLogger.Instance.CloseLog();
                        _hg.SystemConfiguration.HomeGenie.EnableLogFile = "false";
                        _hg.SystemConfiguration.Update();
                    }
                    else if (migcmd.GetOption(0) == "SystemLogging.IsEnabled")
                    {
                        migcmd.response = JsonHelper.GetSimpleResponse((_hg.SystemConfiguration.HomeGenie.EnableLogFile.ToLower().Equals("true") ? "1" : "0"));
                    }
                    else if (migcmd.GetOption(0) == "Security.SetPassword")
                    {
                        // password only for now, with fixed user login 'admin'
                        string pass = migcmd.GetOption(1) == "" ? "" : MIG.Utility.Encryption.SHA1.GenerateHashString(migcmd.GetOption(1));
                        _hg.MigService.SetWebServicePassword(pass);
                        _hg.SystemConfiguration.HomeGenie.UserPassword = pass;
                        // regenerate encrypted files
                        _hg.SystemConfiguration.Update();
                        _hg.UpdateModulesDatabase();
                    }
                    else if (migcmd.GetOption(0) == "Security.ClearPassword")
                    {
                        _hg.MigService.SetWebServicePassword("");
                        _hg.SystemConfiguration.HomeGenie.UserPassword = "";
                        // regenerate encrypted files
                        _hg.SystemConfiguration.Update();
                        _hg.UpdateModulesDatabase();
                    }
                    else if (migcmd.GetOption(0) == "Security.HasPassword")
                    {
                        migcmd.response = JsonHelper.GetSimpleResponse((_hg.SystemConfiguration.HomeGenie.UserPassword != "" ? "1" : "0"));
                    }
                    else if (migcmd.GetOption(0) == "System.ConfigurationRestore")
                    {
                        // file uploaded by user
                        string archivename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp", "homegenie_restore_config.zip");
                        if (!Directory.Exists("tmp"))
                        {
                            Directory.CreateDirectory("tmp");
                        }
                        try
                        {
                            System.IO.DirectoryInfo downloadedMessageInfo = new DirectoryInfo("tmp");
                            foreach (FileInfo file in downloadedMessageInfo.GetFiles())
                            {
                                file.Delete();
                            }
                            foreach (DirectoryInfo dir in downloadedMessageInfo.GetDirectories())
                            {
                                dir.Delete(true);
                            }
                        }
                        catch { }
                        //
                        try
                        {
                            Encoding enc = (request.Context as HttpListenerContext).Request.ContentEncoding;
                            string boundary = MIG.Gateways.WebServiceUtility.GetBoundary((request.Context as HttpListenerContext).Request.ContentType);
                            MIG.Gateways.WebServiceUtility.SaveFile(enc, boundary, request.InputStream, archivename);
                            _hg.UnarchiveConfiguration(archivename, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp"));
                            File.Delete(archivename);
                        }
                        catch { }
                    }
                    else if (migcmd.GetOption(0) == "System.ConfigurationRestoreS1")
                    {
                        XmlSerializer pserializer = new XmlSerializer(typeof(List<ProgramBlock>));
                        StreamReader preader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp", "programs.xml"));
                        List<ProgramBlock> pl = (List<ProgramBlock>)pserializer.Deserialize(preader);
                        preader.Close();
                        List<ProgramBlock> prgs = new List<ProgramBlock>();
                        foreach (ProgramBlock pb in pl)
                        {
                            ProgramBlock p = new ProgramBlock();
                            p.Address = pb.Address;
                            p.Name = pb.Name;
                            p.Description = pb.Description;
                            prgs.Add(p);
                        }
                        prgs.Sort(delegate(ProgramBlock p1, ProgramBlock p2)
                        {
                            string c1 = /*p1.Name + " " +*/ p1.Address.ToString();
                            string c2 = /*p2.Name + " " +*/ p2.Address.ToString();
                            return c1.CompareTo(c2);
                        });
                        migcmd.response = JsonConvert.SerializeObject(prgs);
                    }
                    else if (migcmd.GetOption(0) == "System.ConfigurationRestoreS2")
                    {
                        var plist = migcmd.GetOption(1);
                        XmlSerializer pserializer = new XmlSerializer(typeof(List<ProgramBlock>));
                        StreamReader preader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp", "programs.xml"));
                        List<ProgramBlock> pl = (List<ProgramBlock>)pserializer.Deserialize(preader);
                        preader.Close();
                        foreach (ProgramBlock pb in pl)
                        {
                            if (plist.Contains("," + pb.Address.ToString() + ","))
                            {
                                ProgramBlock hgprg = _hg.ProgramEngine.Programs.Find(p => p.Address == pb.Address);
                                if (hgprg == null || pb.Address >= 1000)
                                {
                                    var newpid = _hg.ProgramEngine.GeneratePid();
                                    try
                                    {
                                        File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp", "programs", pb.Address + ".dll"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs", newpid + ".dll"), true);
                                    }
                                    catch { }
                                    pb.Address = newpid;
                                    _hg.ProgramEngine.ProgramAdd(pb);
                                }
                                else
                                {
                                    // system programs keep original pid
                                    bool wasenabled = hgprg.IsEnabled;
                                    hgprg.IsEnabled = false;
                                    _hg.ProgramEngine.ProgramRemove(hgprg);
                                    try
                                    {
                                        File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp", "programs", pb.Address + ".dll"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs", pb.Address + ".dll"), true);
                                    }
                                    catch { }
                                    _hg.ProgramEngine.ProgramAdd(pb);
                                    pb.IsEnabled = wasenabled;
                                }
                            }
                        }
                        //
                        _hg.UpdateProgramsDatabase();
                        //
                        XmlSerializer zserializer = new XmlSerializer(typeof(List<Group>));
                        StreamReader zreader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp", "automationgroups.xml"));
                        List<Group> agroups = (List<Group>)zserializer.Deserialize(zreader);
                        zreader.Close();
                        //
                        foreach (Group agrp in agroups)
                        {
                            if (_hg.AutomationGroups.Find(g => g.Name == agrp.Name) == null)
                            {
                                _hg.AutomationGroups.Add(agrp);
                            }
                        }
                        //
                        _hg.UpdateGroupsDatabase("Automation");
                        //
                        //File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp", "automationgroups.xml"), "./automationgroups.xml", true);
                        File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp", "groups.xml"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "groups.xml"), true);
                        File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp", "lircconfig.xml"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lircconfig.xml"), true);
                        File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp", "modules.xml"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "modules.xml"), true);
                        File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp", "systemconfig.xml"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "systemconfig.xml"), true);
                        //
                        _hg.LoadConfiguration();
                        //
                        // regenerate encrypted files
                        _hg.UpdateModulesDatabase();
                        _hg.SystemConfiguration.Update();
                    }
                    else if (migcmd.GetOption(0) == "System.ConfigurationReset")
                    {
                        _hg.RestoreFactorySettings();
                    }
                    else if (migcmd.GetOption(0) == "System.ConfigurationBackup")
                    {
                        _hg.BackupCurrentSettings();
                        (request.Context as HttpListenerContext).Response.Redirect("/hg/html/homegenie_backup_config.zip");
                    }
                    else if (migcmd.GetOption(0) == "System.ConfigurationLoad")
                    {
                        _hg.LoadConfiguration();
                    }
                    break;

                case "Modules.List":
                    try
                    {
                        //_modules_sort();
                        migcmd.response = _hg.GetJsonSerializedModules(migcmd.GetOption(0).ToLower() == "short");
                    }
                    catch (Exception ex)
                    {
                        migcmd.response = "ERROR: \n" + ex.Message + "\n\n" + ex.StackTrace;
                    }
                    break;

                case "Modules.RoutingReset":
                    try
                    {
                        //lock (_modules)
                        {
                            for (int m = 0; m < _hg.Modules.Count; m++)
                            {
                                _hg.Modules[m].RoutingNode = "";
                            }
                        }
                        migcmd.response = "OK";
                    }
                    catch (Exception ex)
                    {
                        migcmd.response = "ERROR: \n" + ex.Message + "\n\n" + ex.StackTrace;
                    }
                    break;

                case "Modules.Save":
                    string body = new StreamReader(request.InputStream).ReadToEnd();
                    //
                    JArray mods = JsonConvert.DeserializeObject(body) as JArray;
                    for (int i = 0; i < mods.Count; i++)
                    {
                        try
                        {
                            Module module = _hg.Modules.Find(m => m.Address == mods[i]["Address"].ToString() && m.Domain == mods[i]["Domain"].ToString());
                            module.Name = mods[i]["Name"].ToString();
                            //
                            try
                            {
                                module.DeviceType = (Module.DeviceTypes)Enum.Parse(typeof(Module.DeviceTypes), mods[i]["DeviceType"].ToString(), true);
                            }
                            catch (Exception e)
                            {
                                // TODO: check what's wrong here...
                            }
                            //
                            JArray vmvalue = mods[i]["Properties"] as JArray;
                            for (int p = 0; p < vmvalue.Count; p++)
                            {
                                string propname = vmvalue[p]["Name"].ToString();
                                string propvalue = vmvalue[p]["Value"].ToString();
                                ModuleParameter parameter = null;
                                parameter = module.Properties.Find(delegate(ModuleParameter mp)
                                {
                                    return mp.Name == propname;
                                });
                                //
                                if (propname == ModuleParameters.MODPAR_VIRTUALMETER_WATTS)
                                {
                                    try
                                    {
                                        propvalue = double.Parse(propvalue.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture).ToString();
                                    }
                                    catch { propvalue = "0"; }
                                }
                                //
                                if (parameter == null)
                                {
                                    module.Properties.Add(new ModuleParameter() { Name = propname, Value = propvalue });
                                }
                                else //if (true)
                                {
                                    if (vmvalue[p]["NeedsUpdate"] != null && vmvalue[p]["NeedsUpdate"].ToString() == "true")
                                    {
                                        parameter.Value = propvalue;
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            //TODO: notify exception?
                        }
                    }
                    _hg.UpdateModulesDatabase();//write modules
                    break;

                case "Modules.Update":
                    string streamContent = new StreamReader(request.InputStream).ReadToEnd();
                    Module mod = JsonConvert.DeserializeObject<Module>(streamContent);
                    Module cm = _hg.Modules.Find(p => p.Domain == mod.Domain && p.Address == mod.Address);
                    //
                    if (cm == null)
                    {
                        _hg.Modules.Add(mod);
                    }
                    else
                    {
                        cm.Type = mod.Type;
                        cm.Name = mod.Name;
                        cm.Description = mod.Description;
                        cm.DeviceType = mod.DeviceType;
                        //cm.Properties = mod.Properties;

                        foreach (ModuleParameter p in mod.Properties)
                        {
                            ModuleParameter cpar = cm.Properties.Find(mp => mp.Name == p.Name);
                            if (cpar == null)
                            {
                                cm.Properties.Add(p);
                            }
                            else if (p.NeedsUpdate)
                            {
                                cpar.Value = p.Value;
                            }
                        }
                        // look for deleted properties
                        List<ModuleParameter> todelete = new List<ModuleParameter>();
                        foreach (ModuleParameter p in cm.Properties)
                        {
                            ModuleParameter cpar = mod.Properties.Find(mp => mp.Name == p.Name);
                            if (cpar == null)
                            {
                                todelete.Add(p);
                            }
                        }
                        foreach (ModuleParameter p in todelete)
                        {
                            cm.Properties.Remove(p);
                        }
                        todelete.Clear();
                    }
                    //
                    _hg.UpdateModulesDatabase();
                    break;

                case "Modules.Delete":
                    Module delmod = _hg.Modules.Find(m => m.Domain == migcmd.GetOption(0) && m.Address == migcmd.GetOption(1));
                    if (delmod != null)
                    {
                        _hg.Modules.Remove(delmod);
                    }
                    migcmd.response = JsonHelper.GetSimpleResponse("OK");
                    //
                    _hg.UpdateModulesDatabase();
                    break;

                case "Groups.ModulesList":
                    Group theGroup = _hg.Groups.Find(z => z.Name.ToLower() == migcmd.GetOption(0).Trim().ToLower());
                    if (theGroup != null)
                    {
                        string jsonmodules = "[";
                        for (int m = 0; m < theGroup.Modules.Count; m++)
                        {
                            Module gmod = _hg.Modules.Find(mm => mm.Domain == theGroup.Modules[m].Domain && mm.Address == theGroup.Modules[m].Address);
                            if (gmod != null)
                            {
                                jsonmodules += Utility.Module2Json(gmod, false) + ",\n";
                            }

                        }
                        jsonmodules = jsonmodules.TrimEnd(',', '\n');
                        jsonmodules += "]";
                        migcmd.response = jsonmodules;
                    }
                    break;
                case "Groups.List":
                    try
                    {
                        migcmd.response = JsonConvert.SerializeObject(_hg.GetGroups(migcmd.GetOption(0)));
                    }
                    catch (Exception ex)
                    {
                        migcmd.response = "ERROR: \n" + ex.Message + "\n\n" + ex.StackTrace;
                    }
                    break;

                case "Groups.Rename":
                    string oldname = migcmd.GetOption(1);
                    string newname = new StreamReader(request.InputStream).ReadToEnd();
                    Group grp = _hg.GetGroups(migcmd.GetOption(0)).Find(g => g.Name == oldname);
                    Group chk = _hg.GetGroups(migcmd.GetOption(0)).Find(g => g.Name == newname);
                    if (chk == null && grp != null)
                    {
                        grp.Name = newname;
                        _hg.UpdateGroupsDatabase(migcmd.GetOption(0));
                        //cmd.response = JsonHelper.GetSimpleResponse("OK");
                    }
                    else
                    {
                        migcmd.response = JsonHelper.GetSimpleResponse("New name already in use.");
                    }
                    /*
                    try
                    {
                        cmd.response = JsonConvert.SerializeObject(cmd.option.ToLower() == "automation" ? _automationgroups : _controlgroups);
                    }
                    catch (Exception ex)
                    {
                        cmd.response = "ERROR: \n" + ex.Message + "\n\n" + ex.StackTrace;
                    }
                    */
                    break;

                case "Groups.Sort":
                    using (StreamReader sr = new StreamReader(request.InputStream))
                    {
                        List<Group> newlist = new List<Group>();
                        string[] neworder = sr.ReadToEnd().Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < neworder.Length; i++)
                        {
                            newlist.Add(_hg.GetGroups(migcmd.GetOption(0))[int.Parse(neworder[i])]);
                        }
                        _hg.GetGroups(migcmd.GetOption(0)).Clear();
                        _hg.GetGroups(migcmd.GetOption(0)).RemoveAll(g => true);
                        _hg.GetGroups(migcmd.GetOption(0)).AddRange(newlist);
                        _hg.UpdateGroupsDatabase(migcmd.GetOption(0));
                    }
                    //
                    try
                    {
                        migcmd.response = JsonConvert.SerializeObject(_hg.GetGroups(migcmd.GetOption(0)));
                    }
                    catch (Exception ex)
                    {
                        migcmd.response = "ERROR: \n" + ex.Message + "\n\n" + ex.StackTrace;
                    }
                    break;

                case "Groups.SortModules":
                    using (StreamReader sr = new StreamReader(request.InputStream))
                    {
                        string gname = migcmd.GetOption(1);
                        Group mgroupobj = null;
                        try
                        {
                            mgroupobj = _hg.GetGroups(migcmd.GetOption(0)).Find(zn => zn.Name == gname);
                        }
                        catch
                        {
                        }
                        //
                        if (mgroupobj != null)
                        {
                            List<ModuleReference> newlist = new List<ModuleReference>();
                            string[] neworder = sr.ReadToEnd().Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < neworder.Length; i++)
                            {
                                newlist.Add(mgroupobj.Modules[int.Parse(neworder[i])]);
                            }
                            mgroupobj.Modules.Clear();
                            mgroupobj.Modules = newlist;
                            _hg.UpdateGroupsDatabase(migcmd.GetOption(0));
                        }
                    }

                    try
                    {
                        migcmd.response = JsonConvert.SerializeObject(_hg.GetGroups(migcmd.GetOption(0)));
                    }
                    catch (Exception ex)
                    {
                        migcmd.response = "ERROR: \n" + ex.Message + "\n\n" + ex.StackTrace;
                    }
                    break;

                case "Groups.Add":
                    string groupname = new StreamReader(request.InputStream).ReadToEnd();
                    _hg.GetGroups(migcmd.GetOption(0)).Add(new Group() { Name = groupname });
                    _hg.UpdateGroupsDatabase(migcmd.GetOption(0));//write groups
                    break;

                case "Groups.Delete":
                    string groupName = new StreamReader(request.InputStream).ReadToEnd();
                    Group groupobj = null;
                    try
                    {
                        groupobj = _hg.GetGroups(migcmd.GetOption(0)).Find(zn => zn.Name == groupName);
                    }
                    catch
                    {
                    }
                    //
                    if (groupobj != null)
                    {
                        _hg.GetGroups(migcmd.GetOption(0)).Remove(groupobj);
                        _hg.UpdateGroupsDatabase(migcmd.GetOption(0));//write groups
                        if (migcmd.GetOption(0).ToLower() == "automation")
                        {
                            List<ProgramBlock> mp = _hg.ProgramEngine.Programs.FindAll(p => p.Group.ToLower() == groupobj.Name.ToLower());
                            if (mp != null)
                            {
                                foreach (ProgramBlock pb in mp)
                                {
                                    pb.Group = "";
                                }
                            }
                        }
                    }
                    break;

                case "Groups.Save":
                    string jsongroups = new StreamReader(request.InputStream).ReadToEnd();
                    List<Group> groups = JsonConvert.DeserializeObject<List<Group>>(jsongroups);
                    for (int i = 0; i < groups.Count; i++)
                    {
                        try
                        {
                            Group group = _hg.Groups.Find(z => z.Name == groups[i].Name);
                            group.Modules.Clear();
                            group.Modules = groups[i].Modules;
                        }
                        catch
                        {
                        }
                    }
                    _hg.UpdateGroupsDatabase(migcmd.GetOption(0));//write groups
                    break;
            }

        }


    }
}
