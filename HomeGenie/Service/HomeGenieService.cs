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


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Timers;
using System.Xml.Serialization;

using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using OpenSource.UPnP;

using HomeGenie.Automation;
using HomeGenie.Data;
using HomeGenie.Service.Constants;
using HomeGenie.Service.Logging;
using HomeGenie.Automation.Scheduler;

using MIG;
using MIG.Gateways;

namespace HomeGenie.Service
{
    [Serializable]
    public class HomeGenieService
    {
        #region Private Fields declaration

        private MigService migService;
        private WebServiceGateway webGateway;
        private ProgramManager masterControlProgram;
        private VirtualMeter virtualMeter;
        private UpdateChecker updateChecker;
        private BackupManager backupManager;
        private PackageManager packageManager;
        private StatisticsLogger statisticsLogger;
        // Internal data structures
        private TsList<Module> systemModules = new HomeGenie.Service.TsList<Module>();
        private TsList<Module> modulesGarbage = new HomeGenie.Service.TsList<Module>();
        private TsList<VirtualModule> virtualModules = new TsList<VirtualModule>();
        private List<Group> automationGroups = new List<Group>();
        private List<Group> controlGroups = new List<Group>();
        //
        private SystemConfiguration systemConfiguration;
        //
        // public events
        //public event Action<LogEntry> LogEventAction;

        #endregion

        #region Web Service Handlers declaration

        private Handlers.Config wshConfig;
        private Handlers.Automation wshAutomation;
        private Handlers.Interconnection wshInterconnection;
        private Handlers.Statistics wshStatistics;

        #endregion

        #region Lifecycle

        public HomeGenieService()
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            EnableOutputRedirect();

            InitializeSystem();
            Reload();

            backupManager = new BackupManager(this);
            packageManager = new PackageManager(this);
            updateChecker = new UpdateChecker(this);
            updateChecker.ArchiveDownloadUpdate += (object sender, ArchiveDownloadEventArgs args) =>
            {
                RaiseEvent(
                    Domains.HomeGenie_System,
                    Domains.HomeGenie_UpdateChecker,
                    SourceModule.Master,
                    "HomeGenie Update Checker",
                    Properties.InstallProgressMessage,
                    "= " + args.Status + ": " + args.ReleaseInfo.DownloadUrl
                );
            };
            updateChecker.UpdateProgress += (object sender, UpdateProgressEventArgs args) =>
            {
                RaiseEvent(
                    Domains.HomeGenie_System,
                    Domains.HomeGenie_UpdateChecker,
                    SourceModule.Master,
                    "HomeGenie Update Checker",
                    Properties.InstallProgressUpdate,
                    args.Status.ToString()
                );
            };
            updateChecker.InstallProgressMessage += (object sender, string message) =>
            {
                RaiseEvent(
                    Domains.HomeGenie_System,
                    Domains.HomeGenie_UpdateChecker,
                    SourceModule.Master,
                    "HomeGenie Update Checker",
                    Properties.InstallProgressMessage,
                    message
                );
            };

            statisticsLogger = new StatisticsLogger(this);
            statisticsLogger.Start();

            // Setup local UPnP device
            SetupUpnp();

            // it will check every 24 hours
            updateChecker.Start();

            Thread.Sleep(5000);
            Start();
        }

        public void Start()
        {
            RaiseEvent(Domains.HomeGenie_System, Domains.HomeGenie_System, SourceModule.Master, "HomeGenie System", Properties.HomeGenieStatus, "STARTED");
            // Signal "SystemStarted" event to automation programs
            for (int p = 0; p < masterControlProgram.Programs.Count; p++)
            {
                try
                {
                    var pb = masterControlProgram.Programs[p];
                    if (pb.IsEnabled)
                    {
                        if (pb.Engine.SystemStarted != null)
                        {
                            if (!pb.Engine.SystemStarted())
                            // stop routing this event to other listeners
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    LogError(e);
                }
            }
        }

        public void Stop()
        {
            RaiseEvent(Domains.HomeGenie_System, Domains.HomeGenie_System, SourceModule.Master, "HomeGenie System", Properties.HomeGenieStatus, "STOPPING");
            // Signal "SystemStopping" event to automation programs
            for (int p = 0; p < masterControlProgram.Programs.Count; p++)
            {
                try
                {
                    var pb = masterControlProgram.Programs[p];
                    if (pb.IsEnabled)
                    {
                        if (pb.Engine.SystemStopping != null && !pb.Engine.SystemStopping())
                        {
                            // stop routing this event to other listeners
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    LogError(e);
                }
            }

            // Save system data before quitting
            SaveData();

            // Stop HG helper services
            updateChecker.Stop();
            statisticsLogger.Stop();

            RaiseEvent(Domains.HomeGenie_System, Domains.HomeGenie_System, SourceModule.Master, "HomeGenie System", Properties.HomeGenieStatus, "VirtualMeter STOPPING");
            if (virtualMeter != null) virtualMeter.Stop();
            RaiseEvent(Domains.HomeGenie_System, Domains.HomeGenie_System, SourceModule.Master, "HomeGenie System", Properties.HomeGenieStatus, "VirtualMeter STOPPED");
            RaiseEvent(Domains.HomeGenie_System, Domains.HomeGenie_System, SourceModule.Master, "HomeGenie System", Properties.HomeGenieStatus, "MIG Service STOPPING");
            if (migService != null) migService.StopService();
            RaiseEvent(Domains.HomeGenie_System, Domains.HomeGenie_System, SourceModule.Master, "HomeGenie System", Properties.HomeGenieStatus, "MIG Service STOPPED");
            RaiseEvent(Domains.HomeGenie_System, Domains.HomeGenie_System, SourceModule.Master, "HomeGenie System", Properties.HomeGenieStatus, "ProgramEngine STOPPING");
            if (masterControlProgram != null)
                masterControlProgram.Enabled = false;
            RaiseEvent(Domains.HomeGenie_System, Domains.HomeGenie_System, SourceModule.Master, "HomeGenie System", Properties.HomeGenieStatus, "ProgramEngine STOPPED");
            RaiseEvent(Domains.HomeGenie_System, Domains.HomeGenie_System, SourceModule.Master, "HomeGenie System", Properties.HomeGenieStatus, "STOPPED");

            SystemLogger.Instance.Dispose();
        }

        public void SaveData()
        {
            RaiseEvent(Domains.HomeGenie_System, Domains.HomeGenie_System, SourceModule.Master, "HomeGenie System", Properties.HomeGenieStatus, "SAVING DATA");
            systemConfiguration.Update();
            UpdateModulesDatabase();
            UpdateSchedulerDatabase();
        }

        ~HomeGenieService()
        {
            Stop();
        }

        #endregion

        #region Data Wrappers - Public Members

        // Control groups (i.e. rooms, Outside, Housewide)
        public List<Group> Groups
        {
            get { return controlGroups; }
        }
        // Automation groups
        public List<Group> AutomationGroups
        {
            get { return automationGroups; }
        }
        // MIG interfaces
        public List<MigInterface> Interfaces
        {
            get { return migService.Interfaces; }
        }
        // Modules
        public TsList<Module> Modules
        {
            get { return systemModules; }
        }
        // Virtual modules
        public TsList<VirtualModule> VirtualModules
        {
            get { return virtualModules; }
        }
        // HomeGenie system parameters
        public List<ModuleParameter> Parameters
        {
            get { return systemConfiguration.HomeGenie.Settings; }
        }
        // Reference to SystemConfiguration
        public SystemConfiguration SystemConfiguration
        {
            get { return systemConfiguration; }
        }
        // Reference to MigService
        public MigService MigService
        {
            get { return migService; }
        }
        // Reference to ProgramEngine
        public ProgramManager ProgramManager
        {
            get { return masterControlProgram; }
        }
        // Reference to UpdateChecked
        public UpdateChecker UpdateChecker
        {
            get { return updateChecker; }
        }
        // Reference to BackupManager
        public BackupManager BackupManager
        {
            get { return backupManager; }
        }
        // Reference to PackageManager
        public PackageManager PackageManager
        {
            get { return packageManager; }
        }
        // Reference to Statistics
        public StatisticsLogger Statistics
        {
            get { return statisticsLogger; }
        }
        // Public utility methods
        public string GetHttpServicePort()
        {
            return webGateway.GetOption("Port").Value;
        }

        public object InterfaceControl(MigInterfaceCommand cmd)
        {
            object response = null;
            var target = systemModules.Find(m => m.Domain == cmd.Domain && m.Address == cmd.Address);
            bool isRemoteModule = (target != null && !String.IsNullOrWhiteSpace(target.RoutingNode));
            if (isRemoteModule)
            {
                try
                {
                    string domain = cmd.Domain;
                    if (domain.StartsWith("HGIC:"))
                        domain = domain.Substring(domain.IndexOf(".") + 1);
                    string serviceUrl = "http://" + target.RoutingNode + "/api/" + domain + "/" + cmd.Address + "/" + cmd.Command + "/" + cmd.OptionsString;
                    Automation.Scripting.NetHelper netHelper = new Automation.Scripting.NetHelper(this).WebService(serviceUrl);
                    string username = webGateway.GetOption("Username").Value;
                    string password = webGateway.GetOption("Password").Value;
                    if (!String.IsNullOrWhiteSpace(username) && !String.IsNullOrWhiteSpace(password))
                    {
                        netHelper.WithCredentials(username, password);
                    }
                    response = netHelper.GetData();
                }
                catch (Exception ex)
                {
                    LogError(Domains.HomeAutomation_HomeGenie, "Interconnection:" + target.RoutingNode, ex.Message, "Exception.StackTrace", ex.StackTrace);
                }
            }
            else
            {
                var migInterface = migService.GetInterface(cmd.Domain);
                if (migInterface != null)
                {
                    try
                    {
                        response = migInterface.InterfaceControl(cmd);
                    }
                    catch (Exception ex)
                    {
                        LogError(Domains.HomeAutomation_HomeGenie, "InterfaceControl", ex.Message, "Exception.StackTrace", ex.StackTrace);
                    }
                }
                //
                // If the command was not already handled, let automation programs process it
                if (response == null || String.IsNullOrWhiteSpace(response.ToString()))
                {
                    response = ProgramDynamicApi.TryApiCall(cmd);
                }
                //
                // Macro Recording
                //
                // TODO: find a better solution for this.... 
                // TODO: it was: migService_ServiceRequestPostProcess(this, new ProcessRequestEventArgs(cmd));
                // TODO: !IMPORTANT!
                if (masterControlProgram != null && masterControlProgram.MacroRecorder.IsRecordingEnabled && cmd != null && cmd.Command != null && (cmd.Command.StartsWith("Control.") || (cmd.Command.StartsWith("AvMedia.") && cmd.Command != "AvMedia.Browse" && cmd.Command != "AvMedia.GetUri")))
                {
                    masterControlProgram.MacroRecorder.AddCommand(cmd);
                }
            }
            return response;
        }

        public List<Group> GetGroups(string namePrefix)
        {
            List<Group> group = null;
            if (namePrefix.ToLower() == "automation")
            {
                group = automationGroups;
            }
            else
            {
                group = controlGroups;
            }
            return group;
        }

        public string GetJsonSerializedModules(bool hideProperties)
        {
            string jsonModules = "";
            try
            {
                jsonModules = "[";
                for (int m = 0; m < systemModules.Count; m++)// Module m in Modules)
                {
                    jsonModules += Utility.Module2Json(systemModules[m], hideProperties) + ",\n";
                    //System.Threading.Thread.Sleep(1);
                }
                jsonModules = jsonModules.TrimEnd(',', '\n');
                jsonModules += "]";
                // old code for generate json, it was too much cpu time consuming on ARM
                //jsonmodules = JsonConvert.SerializeObject(Modules, Formatting.Indented);
            }
            catch (Exception ex)
            {
                LogError(
                    Domains.HomeAutomation_HomeGenie,
                    "GetJsonSerializedModules()",
                    ex.Message,
                    "Exception.StackTrace",
                    ex.StackTrace
                );
            }
            //
            return jsonModules;
        }

        // TODO: move this to a better location
        public bool ExecuteAutomationRequest(MigInterfaceCommand command)
        {
            string levelValue, commandValue;
            // check for certain commands
            if (command.Command == Commands.Groups.GroupsLightsOff)
            {
                levelValue = "0";
                commandValue = Commands.Control.ControlOff;
            }
            else if (command.Command == Commands.Groups.GroupsLightsOn)
            {
                levelValue = "1";
                commandValue = Commands.Control.ControlOn;
            }
            else
            {
                return false;
            }
            //loop, turning off lights
            try
            {
                var group = Groups.Find(z => z.Name == command.GetOption(0));
                for (int m = 0; m < group.Modules.Count; m++)
                {
                    var module = Modules.Find(mod => mod.Domain == group.Modules[m].Domain && mod.Address == group.Modules[m].Address);
                    if (module != null && (module.DeviceType == MIG.ModuleTypes.Light || module.DeviceType == MIG.ModuleTypes.Dimmer))
                    {
                        try
                        {
                            var icmd = new MigInterfaceCommand(module.Domain + "/" + module.Address + "/" + commandValue);
                            InterfaceControl(icmd);
                            Service.Utility.ModuleParameterGet(module, Properties.StatusLevel).Value = levelValue;
                        }
                        catch (Exception e)
                        {
                            LogError(e);
                        }
                    }
                }
            }
            catch
            {
                // TODO: handle exception here
            }
            return true;
        }

        #endregion

        #region MIG Events Propagation / Logging

        internal void RaiseEvent(object sender, MigEvent evt)
        {
            migService.RaiseEvent(sender, evt);
        }

        internal void RaiseEvent(
            object sender,
            string domain,
            string source,
            string description,
            string property,
            string value
        )
        {
            var evt = migService.GetEvent(domain, source, description, property, value);
            migService.RaiseEvent(sender, evt);
        }

        internal static void LogDebug(
            string domain,
            string source,
            string description,
            string property,
            string value)
        {
            var debugEvent = new MigEvent(domain, source, description, property, value);
            MigService.Log.Debug(debugEvent);
        }

        internal static void LogError(
            string domain,
            string source,
            string description,
            string property,
            string value)
        {
            var errorEvent = new MigEvent(domain, source, description, property, value);
            LogError(errorEvent);
        }

        internal static void LogError(object err)
        {
            MigService.Log.Error(err);
        }

        private void EnableOutputRedirect()
        {
            Console.OutputEncoding = Encoding.UTF8;
            var outputRedirect = new ConsoleRedirect();
            outputRedirect.ProcessOutput = (outputLine) => {
                if (SystemLogger.Instance.IsLogEnabled)
                    SystemLogger.Instance.WriteToLog(outputLine);
            };
            Console.SetOut(outputRedirect);
            Console.SetError(outputRedirect);
        }

        #endregion

        #region MIG Service events handling

        private void migService_InterfaceModulesChanged(object sender, InterfaceModulesChangedEventArgs args)
        {
            modules_RefreshInterface(migService.GetInterface(args.Domain));
        }

        private void migService_InterfacePropertyChanged(object sender, InterfacePropertyChangedEventArgs args)
        {
            
            // look for module associated to this event
            Module module = Modules.Find(o => o.Domain == args.EventData.Domain && o.Address == args.EventData.Source);
            if (module != null && args.EventData.Property != "")
            {
                // clear RoutingNode property since the event was locally generated
                //if (module.RoutingNode != "")
                //{
                //    module.RoutingNode = "";
                //}
                // we found associated module in HomeGenie.Modules

                // Update/Add the module parameter as needed
                ModuleParameter parameter = null;
                try
                {
                    // Lookup for the existing module parameter
                    parameter = Utility.ModuleParameterGet(module, args.EventData.Property);
                    if (parameter == null)
                    {
                        parameter = new ModuleParameter() {
                            Name = args.EventData.Property,
                            Value = args.EventData.Value.ToString()
                        };
                        module.Properties.Add(parameter);
                        //parameter = Utility.ModuleParameterGet(module, args.EventData.Property);
                    }
                    else
                    {
                        parameter.Value = args.EventData.Value.ToString();
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                }
                // Prevent event pump from blocking on other worker tasks
                if (masterControlProgram != null)
                Utility.RunAsyncTask(() =>
                {
                    masterControlProgram.SignalPropertyChange(sender, module, args.EventData);
                });
            }
            else
            {
                if (args.EventData.Domain == Domains.MigService_Interfaces)
                {
                    modules_RefreshInterface(migService.GetInterface(args.EventData.Source));
                }
                /*
                LogBroadcastEvent(
                    args.EventData.Domain,
                    args.EventData.Source,
                    args.EventData.Description,
                    args.EventData.Property,
                    args.EventData.Value != null ? args.EventData.Value.ToString() : ""
                );
                */
            }
        }

        private void migService_ServiceRequestPreProcess(object sender, ProcessRequestEventArgs args)
        {
            // Currently we only support requests coming from WebServiceGateway
            // TODO: in the future, add support for any MigGateway channel (eg. WebSocketGateway as well)
            if (args.Request.Context.Source != ContextSource.WebServiceGateway)
                return;

            var migCommand = args.Request.Command;

            #region Interconnection (Remote Node Command Routing)

            Module target = systemModules.Find(m => m.Domain == migCommand.Domain && m.Address == migCommand.Address);
            bool isRemoteModule = (target != null && !String.IsNullOrWhiteSpace(target.RoutingNode));
            if (isRemoteModule)
            {
                try
                {
                    string domain = migCommand.Domain;
                    if (domain.StartsWith("HGIC:")) domain = domain.Substring(domain.IndexOf(".") + 1);
                    string serviceurl = "http://" + target.RoutingNode + "/api/" + domain + "/" + migCommand.Address + "/" + migCommand.Command + "/" + migCommand.OptionsString;
                    Automation.Scripting.NetHelper neth = new Automation.Scripting.NetHelper(this).WebService(serviceurl);
                    string username = webGateway.GetOption("Username").Value;
                    string password = webGateway.GetOption("Password").Value;
                    if (!String.IsNullOrWhiteSpace(username) && !String.IsNullOrWhiteSpace(password))
                    {
                        neth.WithCredentials(
                            username,
                            password
                        );
                    }
                    neth.Call();
                }
                catch (Exception ex)
                {
                    LogError(
                        Domains.HomeAutomation_HomeGenie,
                        "Interconnection:" + target.RoutingNode,
                        ex.Message,
                        "Exception.StackTrace",
                        ex.StackTrace
                    );
                }
                return;
            }

            #endregion

            // HomeGenie Web Service domain API
            if (migCommand.Domain == Domains.HomeAutomation_HomeGenie)
            {
                // domain == HomeAutomation.HomeGenie
                switch (migCommand.Address)
                {

                case "Config":
                    wshConfig.ProcessRequest(args.Request);
                    break;

                case "Automation":
                    wshAutomation.ProcessRequest(args.Request);
                    break;

                case "Interconnection":
                    wshInterconnection.ProcessRequest(args.Request);
                    break;

                case "Statistics":
                    wshStatistics.ProcessRequest(args.Request);
                    break;

                }
            }
            else if (migCommand.Domain == Domains.HomeAutomation_HomeGenie_Automation)
            {
                int n;
                bool nodeIdIsNumeric = int.TryParse(migCommand.Address, out n);
                if (nodeIdIsNumeric)
                {
                    switch (migCommand.Command)
                    {

                    case "Control.Run":
                        wshAutomation.ProgramRun(migCommand.Address, migCommand.GetOption(0));
                        break;

                    case "Control.Break":
                        wshAutomation.ProgramBreak(migCommand.Address);
                        break;

                    }
                }
            }

        }

        private void migService_ServiceRequestPostProcess(object sender, ProcessRequestEventArgs args)
        {
            var command = args.Request.Command;
            if (command.Domain ==  Domains.MigService_Interfaces && command.Command.EndsWith(".Set"))
            {
                systemConfiguration.Update();
            }

            // Let automation programs process the request; we append eventual POST data (RequestText) to the MigInterfaceCommand
            if (!String.IsNullOrWhiteSpace(args.Request.RequestText))
                command = new MigInterfaceCommand(command.OriginalRequest + "/" + args.Request.RequestText);
            args.Request.ResponseData = ProgramDynamicApi.TryApiCall(command);

            // Macro Recording
            if (masterControlProgram != null && masterControlProgram.MacroRecorder.IsRecordingEnabled && command != null && command.Command != null && (command.Command.StartsWith("Control.") || (command.Command.StartsWith("AvMedia.") && command.Command != "AvMedia.Browse" && command.Command != "AvMedia.GetUri")))
            {
                masterControlProgram.MacroRecorder.AddCommand(command);
            }
        }

        #endregion

        #region Initialization and Data Persistence

        public bool UpdateGroupsDatabase(string namePrefix)
        {
            var groups = controlGroups;
            if (namePrefix.ToLower() == "automation")
            {
                groups = automationGroups;
            }
            else
            {
                namePrefix = ""; // default fallback to Control Groups groups.xml - no prefix
            }
            //
            bool success = false;
            try
            {
                string filePath = Path.Combine(
                                      AppDomain.CurrentDomain.BaseDirectory,
                                      namePrefix.ToLower() + "groups.xml"
                                  );
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                var settings = new System.Xml.XmlWriterSettings();
                settings.Indent = true;
                var serializer = new System.Xml.Serialization.XmlSerializer(groups.GetType());
                var writer = System.Xml.XmlWriter.Create(filePath, settings);
                serializer.Serialize(writer, groups);
                writer.Close();
                //
                success = true;
            }
            catch
            {
            }
            return success;
        }

        public bool UpdateModulesDatabase()
        {
            bool success = false;
            modules_RefreshAll();
            lock (systemModules.LockObject)
            {
                try
                {
                    // Due to encrypted values, we must clone modules before encrypting and saving
                    var clonedModules = systemModules.DeepClone();
                    foreach (var module in clonedModules)
                    {
                        foreach (var parameter in module.Properties)
                        {
                            // these four properties have to be kept in clear text
                            if (parameter.Name != Properties.WidgetDisplayModule
                                && parameter.Name != Properties.VirtualModuleParentId
                                && parameter.Name != Properties.ProgramStatus
                                && parameter.Name != Properties.RuntimeError)
                            {
                                if (!String.IsNullOrEmpty(parameter.Value))
                                    parameter.Value = StringCipher.Encrypt(parameter.Value, GetPassPhrase());
                            }
                        }
                    }
                    string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "modules.xml");
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    var settings = new System.Xml.XmlWriterSettings();
                    settings.Indent = true;
                    var serializer = new System.Xml.Serialization.XmlSerializer(clonedModules.GetType());
                    var writer = System.Xml.XmlWriter.Create(filePath, settings);
                    serializer.Serialize(writer, clonedModules);
                    writer.Close();
                    success = true;
                }
                catch (Exception ex)
                {
                    LogError(Domains.HomeAutomation_HomeGenie, "UpdateModulesDatabase()", ex.Message, "Exception.StackTrace", ex.StackTrace);
                }
            }
            return success;
        }

        public bool UpdateProgramsDatabase()
        {
            bool success = false;
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs.xml");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                var settings = new System.Xml.XmlWriterSettings();
                settings.Indent = true;
                var serializer = new System.Xml.Serialization.XmlSerializer(masterControlProgram.Programs.GetType());
                var writer = System.Xml.XmlWriter.Create(filePath, settings);
                serializer.Serialize(writer, masterControlProgram.Programs);
                writer.Close();

                success = true;
            }
            catch
            {
            }
            return success;
        }

        public bool UpdateSchedulerDatabase()
        {
            bool success = false;
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scheduler.xml");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                var settings = new System.Xml.XmlWriterSettings();
                settings.Indent = true;
                var serializer = new System.Xml.Serialization.XmlSerializer(masterControlProgram.SchedulerService.Items.GetType());
                var writer = System.Xml.XmlWriter.Create(filePath, settings);
                serializer.Serialize(writer, masterControlProgram.SchedulerService.Items);
                writer.Close();

                success = true;
            }
            catch
            {
            }
            return success;
        }

        /// <summary>
        /// Reload system configuration and restart services and interfaces.
        /// </summary>
        public void Reload()
        {
            migService.StopService();

            LoadConfiguration();

            webGateway = (WebServiceGateway)migService.GetGateway("WebServiceGateway");
            if (webGateway == null)
            {
                RaiseEvent(
                    Domains.HomeGenie_System,
                    Domains.HomeAutomation_HomeGenie,
                    SourceModule.Master,
                    "Configuration entry not found",
                    "Gateways",
                    "WebServiceGateway"
                );
                Program.Quit(false);
            }
            int webPort = int.Parse(webGateway.GetOption("Port").Value);

            bool started = migService.StartService();
            while (!started)
            {
                RaiseEvent(
                    Domains.HomeGenie_System,
                    Domains.HomeAutomation_HomeGenie,
                    SourceModule.Master,
                    "HTTP binding failed.",
                    Properties.SystemInfoHttpAddress,
                    webGateway.GetOption("Host").Value + ":" + webGateway.GetOption("Port").Value
                );
                // Try auto-binding to another port >= 8080 (up to 8090)
                if (webPort < 8080)
                    webPort = 8080;
                else
                    webPort++;
                if (webPort <= 8090)
                {
                    webGateway.SetOption("Port", webPort.ToString());
                    started = webGateway.Start();
                }
            }

            if (started)
            {
                RaiseEvent(
                    Domains.HomeGenie_System,
                    Domains.HomeAutomation_HomeGenie,
                    SourceModule.Master,
                    "HomeGenie service ready",
                    Properties.SystemInfoHttpAddress,
                    webGateway.GetOption("Host").Value + ":" + webGateway.GetOption("Port").Value
                );
            }
            else
            {
                Program.Quit(false);
            }
        }

        /// <summary>
        /// Reload the system without stopping the web service.
        /// </summary>
        /// <returns><c>true</c> on success, <c>false</c> otherwise.</returns>
        public bool SoftReload()
        {
            bool success = true;
            foreach (var migInterface in migService.Interfaces)
            {
                MigService.Log.Debug("Disabling Interface {0}", migInterface.GetDomain());
                migInterface.IsEnabled = false;
                migInterface.Disconnect();
            }
            LoadConfiguration();
            try
            {
                // Initialize MIG Interfaces
                foreach (MIG.Config.Interface iface in migService.Configuration.Interfaces)
                {
                    if (iface.IsEnabled)
                    {
                        migService.EnableInterface(iface.Domain);
                    }
                    else
                    {
                        migService.DisableInterface(iface.Domain);
                    }
                }
            }
            catch (Exception e)
            {
                MigService.Log.Error(e);
                success = false;
            }
            return success;
        }

        public void LoadConfiguration()
        {
            LoadSystemConfig();
            //
            // load modules data
            //
            LoadModules();
            //
            // load last saved groups data into controlGroups list
            try
            {
                var serializer = new XmlSerializer(typeof(List<Group>));
                using (var reader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "groups.xml")))
                    controlGroups = (List<Group>)serializer.Deserialize(reader);
            }
            catch
            {
                //TODO: log error
            }
            //
            // load last saved automation groups data into automationGroups list
            try
            {
                var serializer = new XmlSerializer(typeof(List<Group>));
                using (var reader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "automationgroups.xml")))
                    automationGroups = (List<Group>)serializer.Deserialize(reader);
            }
            catch
            {
                //TODO: log error
            }
            //
            // load last saved programs data into masterControlProgram.Programs list
            //
            if (masterControlProgram != null)
            {
                masterControlProgram.Enabled = false;
                masterControlProgram = null;
            }
            masterControlProgram = new ProgramManager(this);
            try
            {
                var serializer = new XmlSerializer(typeof(List<ProgramBlock>));
                using (var reader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs.xml")))
                {
                    var programs = (List<ProgramBlock>)serializer.Deserialize(reader);
                    foreach (var program in programs)
                    {
                        program.IsRunning = false;
                        // backward compatibility with hg < 0.91
                        if (program.Address == 0)
                        {
                            // assign an id to program if unassigned
                            program.Address = masterControlProgram.GeneratePid();
                        }
                        masterControlProgram.ProgramAdd(program);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(
                    Domains.HomeAutomation_HomeGenie,
                    "LoadConfiguration()",
                    ex.Message,
                    "Exception.StackTrace",
                    ex.StackTrace
                );
            }
            //
            // load last saved scheduler items data into masterControlProgram.SchedulerService.Items list
            //
            try
            {
                var serializer = new XmlSerializer(typeof(List<SchedulerItem>));
                using (var reader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scheduler.xml")))
                {
                    var schedulerItems = (List<SchedulerItem>)serializer.Deserialize(reader);
                    masterControlProgram.SchedulerService.Items.AddRange(schedulerItems);
                }
            }
            catch
            {
                //TODO: log error
            }
            // force re-generation of Modules list
            modules_RefreshAll();
            //
            // enable automation programs engine
            //
            masterControlProgram.Enabled = true;
        }

        public void RestoreFactorySettings()
        {
            // Stop program engine
            try
            {
                masterControlProgram.Enabled = false;
                masterControlProgram = null;
            } catch { }
            // Uncompress factory settings and restart HG service
            Utility.UncompressZip("homegenie_factory_config.zip", AppDomain.CurrentDomain.BaseDirectory);
            Reload();
            SaveData();
        }

        #endregion Initialization and Data Storage

        #region Misc events handlers

        // fired after configuration is written to systemconfiguration.xml
        private void systemConfiguration_OnUpdate(bool success)
        {
            modules_RefreshAll();
        }
 
        #endregion

        #region Internals for modules' structure update and sorting

        internal void modules_RefreshVirtualModules()
        {
            lock (systemModules.LockObject) 
            lock (virtualModules.LockObject) 
            try
            {
                //
                // Virtual Modules
                //
                foreach (var virtualModule in virtualModules)
                {
                    ProgramBlock program = masterControlProgram.Programs.Find(p => p.Address.ToString() == virtualModule.ParentId);
                    if (program == null) continue;
                    //
                    var virtualModuleWidget = Utility.ModuleParameterGet(virtualModule, Properties.WidgetDisplayModule);
                    //
                    Module module = Modules.Find(o => {
                        // main program module...
                        bool found = (o.Domain == virtualModule.Domain && o.Address == virtualModule.Address && o.Address == virtualModule.ParentId);
                        // ...or virtual module
                        if (!found && o.Domain == virtualModule.Domain && o.Address == virtualModule.Address && o.Address != virtualModule.ParentId)
                        {
                            var prop = Utility.ModuleParameterGet(o, Properties.VirtualModuleParentId);
                            if (prop != null && prop.Value == virtualModule.ParentId) found = true;
                        }
                        return found;
                    });

                    if (!program.IsEnabled)
                    {
                        if (module != null && module.RoutingNode == "" && virtualModule.ParentId != module.Address)
                        {
                            // copy instance module properties to virtualmodules before removing
                            virtualModule.Name = module.Name;
                            virtualModule.DeviceType = module.DeviceType;
                            virtualModule.Properties.Clear();
                            foreach (var p in module.Properties)
                            {
                                virtualModule.Properties.Add(p);
                            }
                            systemModules.Remove(module);
                        }
                        continue;
                    }
                    //else if (virtualModule.ParentId == virtualModule.Address)
                    //{
                    //    continue;
                    //}

                    if (module == null)
                    {
                        // add new module
                        module = new Module();
                        systemModules.Add(module);
                        // copy properties from virtualmodules
                        foreach (var p in virtualModule.Properties)
                        {
                            module.Properties.Add(p);
                        }
                    }

                    // module inherits props from associated virtual module
                    module.Domain = virtualModule.Domain;
                    module.Address = virtualModule.Address;
                    if (module.DeviceType == MIG.ModuleTypes.Generic && virtualModule.DeviceType != ModuleTypes.Generic)
                    {
                        module.DeviceType = virtualModule.DeviceType;
                    }
                    // associated module's name of an automation program cannot be changed
                    if (module.Name == "" || (module.DeviceType == MIG.ModuleTypes.Program && virtualModule.Name != ""))
                    {
                        module.Name = virtualModule.Name;
                    }
                    module.Description = virtualModule.Description;
                    //
                    if (virtualModule.ParentId != virtualModule.Address)
                    {
                        Utility.ModuleParameterSet(
                            module,
                            Properties.VirtualModuleParentId,
                            virtualModule.ParentId
                        );
                    }
                    var moduleWidget = Utility.ModuleParameterGet(module, Properties.WidgetDisplayModule);
                    // if a widget is specified on virtual module then we force module to display using this
                    if ((virtualModuleWidget != null && (virtualModuleWidget.Value != "" || moduleWidget == null)) && (moduleWidget == null || (moduleWidget.Value != virtualModuleWidget.Value)))
                    {
                        Utility.ModuleParameterSet(
                            module,
                            Properties.WidgetDisplayModule,
                            virtualModuleWidget.Value
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(
                    Domains.HomeAutomation_HomeGenie,
                    "modules_RefreshVirtualModules()",
                    ex.Message,
                    "Exception.StackTrace",
                    ex.StackTrace
                );
            }
        }

        internal void modules_RefreshPrograms()
        {
            lock (systemModules.LockObject) 
            try
            {
                // Refresh ProgramEngine program modules
                if (masterControlProgram != null)
                {
                    lock (masterControlProgram.Programs.LockObject) 
                    foreach (var program in masterControlProgram.Programs)
                    {
                        Module module = systemModules.Find(o => o.Domain == Domains.HomeAutomation_HomeGenie_Automation && o.Address == program.Address.ToString());
                        if (module != null && program.Type.ToLower() == "wizard" && !program.IsEnabled && module.RoutingNode == "")
                        {
                            // we don't remove non-wizard programs to keep configuration options
                            // TODO: ?? should use modulesGarbage in order to allow correct removing/restoring of all program types ??
                            // TODO: ?? (but it will loose config options when hg is restarted because modulesGarbage it's not saved) ??
                            systemModules.Remove(module);
                            continue;
                        }
                        else if (module == null && !program.IsEnabled)
                        {
                            continue;
                        }
                        else if (module == null)
                        {
                            // add module for the program
                            module = new Module();
                            module.Domain = Domains.HomeAutomation_HomeGenie_Automation;
                            if (program.Type.ToLower() == "wizard")
                            {
                                Utility.ModuleParameterSet(
                                    module,
                                    Properties.WidgetDisplayModule,
                                    "homegenie/generic/program"
                                );
                            }
                            systemModules.Add(module);
                        }
                        module.Name = program.Name;
                        module.Address = program.Address.ToString();
                        module.DeviceType = MIG.ModuleTypes.Program;
                        //module.Description = "Wizard Script";
                    }
                    // Add "Scheduler" virtual module
                    //Module scheduler = systemModules.Find(o=> o.Domain == Domains.HomeAutomation_HomeGenie && o.Address == SourceModule.Scheduler);
                    //if (scheduler == null) {
                    //    scheduler = new Module(){ Domain = Domains.HomeAutomation_HomeGenie, Address = SourceModule.Scheduler };
                    //    systemModules.Add(scheduler);
                    //}
                }
            }
            catch (Exception ex)
            {
                LogError(
                    Domains.HomeAutomation_HomeGenie,
                    "modules_RefreshPrograms()",
                    ex.Message,
                    "Exception.StackTrace",
                    ex.StackTrace
                );
            }
        }

        internal void modules_Sort()
        {
            lock (systemModules.LockObject) try
            {
                // sort modules properties by name
                foreach (var module in systemModules)
                {
                    module.Properties.Sort((ModuleParameter p1, ModuleParameter p2) =>
                    {
                        return p1.Name.CompareTo(p2.Name);
                    });
                }
                //
                // sort modules
                //
                systemModules.Sort((Module m1, Module m2) =>
                {
                    System.Text.RegularExpressions.Regex re = new System.Text.RegularExpressions.Regex(@"([a-zA-Z]+)(\d+)");
                    System.Text.RegularExpressions.Match result1 = re.Match(m1.Address);
                    System.Text.RegularExpressions.Match result2 = re.Match(m2.Address);

                    string alphaPart1 = result1.Groups[1].Value.PadRight(8, '0');
                    string numberPart1 = (String.IsNullOrWhiteSpace(result1.Groups[2].Value) ? m1.Address.PadLeft(8, '0') : result1.Groups[2].Value.PadLeft(8, '0'));
                    string alphaPart2 = result2.Groups[1].Value.PadRight(8, '0');
                    string numberPart2 = (String.IsNullOrWhiteSpace(result2.Groups[2].Value) ? m2.Address.PadLeft(8, '0') : result2.Groups[2].Value.PadLeft(8, '0'));

                    string d1 = m1.Domain + "|" + alphaPart1 + numberPart1;
                    string d2 = m2.Domain + "|" + alphaPart2 + numberPart2;
                    return d1.CompareTo(d2);
                });

            }
            catch (Exception ex)
            {
                LogError(
                    Domains.HomeAutomation_HomeGenie,
                    "modules_Sort()",
                    ex.Message,
                    "Exception.StackTrace",
                    ex.StackTrace
                );
            }
        }

        internal void modules_RefreshAll()
        {
            systemModules.RemoveAll(m => m == null); // <-- dunno why but sometimes it happen to have null entries causing exceptions

            // Refresh all MIG modules
            foreach (var iface in migService.Interfaces)
            {
                try
                {
                    modules_RefreshInterface(iface);
                } catch {
                    //TODO: interface not ready? handle this
                }
            }

            // Refresh other HG modules
            modules_RefreshPrograms();
            modules_RefreshVirtualModules();

            modules_Sort();
        }

        private void modules_RefreshInterface(MigInterface iface)
        {
            if (migService.Configuration.GetInterface(iface.GetDomain()).IsEnabled)
            {
                var interfaceModules = iface.GetModules();
                if (interfaceModules.Count > 0)
                {
                    // delete removed modules
                    var deleted = systemModules.FindAll(m => m.Domain == iface.GetDomain() && (interfaceModules.Find(m1 => m1.Address == m.Address && m1.Domain == m.Domain) == null));
                    foreach (var mod in deleted)
                    {
                        // only "real" modules defined by mig interfaces are considered
                        var virtualParam = Utility.ModuleParameterGet(mod, Properties.VirtualModuleParentId);
                        if (virtualParam == null || virtualParam.DecimalValue == 0)
                        {
                            Module garbaged = modulesGarbage.Find(m => m.Domain == mod.Domain && m.Address == mod.Address);
                            if (garbaged != null) modulesGarbage.Remove(garbaged);
                            modulesGarbage.Add(mod);
                            systemModules.Remove(mod);
                        }
                    }
                    //
                    foreach (var migModule in interfaceModules)
                    {
                        Module module = systemModules.Find(o => o.Domain == migModule.Domain && o.Address == migModule.Address);
                        if (module == null)
                        {
                            // try restoring from garbage
                            module = modulesGarbage.Find(o => o.Domain == migModule.Domain && o.Address == migModule.Address);
                            if (module != null)
                            {
                                systemModules.Add(module);
                            }
                            else
                            {
                                module = new Module();
                                module.Domain = migModule.Domain;
                                module.Address = migModule.Address;
                                systemModules.Add(module);
                            }
                        }
                        if (String.IsNullOrEmpty(module.Description))
                        {
                            module.Description = migModule.Description;
                        }
                        if (module.DeviceType == ModuleTypes.Generic)
                        {
                            module.DeviceType = migModule.ModuleType;
                        }
                    }
                }
            }
            else
            {
                var deleted = systemModules.FindAll(m => m.Domain == iface.GetDomain());
                foreach (var mod in deleted)
                {
                    var virtualParam = Utility.ModuleParameterGet(mod, Properties.VirtualModuleParentId);
                    if (virtualParam == null || virtualParam.DecimalValue == 0)
                    {
                        Module garbaged = modulesGarbage.Find(m => m.Domain == mod.Domain && m.Address == mod.Address);
                        if (garbaged != null) modulesGarbage.Remove(garbaged);
                        modulesGarbage.Add(mod);
                        systemModules.Remove(mod);
                    }
                }
            }
        }

        #endregion

        #region Private utility methods

        private void InitializeSystem()
        {
            // Setup web service handlers
            wshConfig = new Handlers.Config(this);
            wshAutomation = new Handlers.Automation(this);
            wshInterconnection = new Handlers.Interconnection(this);
            wshStatistics = new Handlers.Statistics(this);

            // Initialize MigService, gateways and interfaces
            migService = new MIG.MigService();
            migService.InterfaceModulesChanged += migService_InterfaceModulesChanged;
            migService.InterfacePropertyChanged += migService_InterfacePropertyChanged;
            migService.GatewayRequestPreProcess += migService_ServiceRequestPreProcess;
            migService.GatewayRequestPostProcess += migService_ServiceRequestPostProcess;

            // Setup other objects used in HG
            virtualMeter = new VirtualMeter(this);
        }

        private string GetPassPhrase()
        {
            // Get username/password from web serivce and use as encryption key
            var webGw = migService.GetGateway("WebServiceGateway");
            if (webGw != null)
            {
                var username = webGw.GetOption("Username").Value;
                var password = webGw.GetOption("Password").Value;
                //return String.Format("{0}{1}homegenie", username, password);
                return String.Format("{0}homegenie", password);
            }
            else
                return "";
        }

        private void LoadSystemConfig()
        {
            if (systemConfiguration != null)
                systemConfiguration.OnUpdate -= systemConfiguration_OnUpdate;
            try
            {
                // load config
                var serializer = new XmlSerializer(typeof(SystemConfiguration));
                using (var reader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "systemconfig.xml")))
                {
                    systemConfiguration = (SystemConfiguration)serializer.Deserialize(reader);
                    // setup logging
                    if (!String.IsNullOrEmpty(systemConfiguration.HomeGenie.EnableLogFile) && systemConfiguration.HomeGenie.EnableLogFile.ToLower().Equals("true"))
                    {
                        SystemLogger.Instance.OpenLog();
                    }
                    else
                    {
                        SystemLogger.Instance.CloseLog();
                    }
                    // configure MIG
                    migService.Configuration = systemConfiguration.MigService;
                    // Set the password for decrypting settings values and later module parameters
                    systemConfiguration.SetPassPhrase(GetPassPhrase());
                    // decrypt config data
                    foreach (var parameter in systemConfiguration.HomeGenie.Settings)
                    {
                        try
                        {
                            if (!String.IsNullOrEmpty(parameter.Value)) parameter.Value = StringCipher.Decrypt(
                                    parameter.Value,
                                    GetPassPhrase()
                                );
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(
                    Domains.HomeAutomation_HomeGenie,
                    "LoadSystemConfig()",
                    ex.Message,
                    "Exception.StackTrace",
                    ex.StackTrace
                );
            }
            if (systemConfiguration != null)
                systemConfiguration.OnUpdate += systemConfiguration_OnUpdate;
        }

        private void LoadModules()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(HomeGenie.Service.TsList<Module>));
                using (var reader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "modules.xml")))
                {
                    var modules = (HomeGenie.Service.TsList<Module>)serializer.Deserialize(reader);
                    foreach (var module in modules)
                    {
                        foreach (var parameter in module.Properties)
                        {
                            try
                            {
                                if (!String.IsNullOrEmpty(parameter.Value)) parameter.Value = StringCipher.Decrypt(
                                        parameter.Value,
                                        GetPassPhrase()
                                    );
                            }
                            catch
                            {
                            }
                        }
                    }
                    modulesGarbage.Clear();
                    systemModules.Clear();
                    systemModules = modules;
                }
            }
            catch (Exception ex)
            {
                LogError(
                    Domains.HomeAutomation_HomeGenie,
                    "LoadModules()",
                    ex.Message,
                    "Exception.StackTrace",
                    ex.StackTrace
                );
            }
            try
            {
                // Reset Parameter.Watts, /*Status Level,*/ Sensor.Generic values
                for (int m = 0; m < systemModules.Count; m++)
                {
                    // cleanup stuff for unwanted  xsi:nil="true" empty params
                    systemModules[m].Properties.RemoveAll(p => p == null);
                    ModuleParameter parameter = systemModules[m].Properties.Find(mp => mp.Name == Properties.MeterWatts /*|| mp.Name == Properties.STATUS_LEVEL || mp.Name == Properties.SENSOR_GENERIC */);
                    if (parameter != null)
                        parameter.Value = "0";
                }
            }
            catch (Exception ex)
            {
                LogError(
                    Domains.HomeAutomation_HomeGenie,
                    "LoadModules()",
                    ex.Message,
                    "Exception.StackTrace",
                    ex.StackTrace
                );
            }
            // Force re-generation of Modules list
            modules_RefreshAll();
        }

        private void SetupUpnp()
        {
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            string address = localIP;
            string bindhost = webGateway.GetOption("Host").Value;
            string bindport = webGateway.GetOption("Port").Value;
            if (bindhost.Length > 1)
            {
                address = bindhost;
            }
            //
            string presentationUrl = "http://" + address + ":" + bindport;
            //string friendlyName = "HomeGenie: " + Environment.MachineName;
            string manufacturer = "G-Labs";
            string manufacturerUrl = "http://genielabs.github.io/HomeGenie/";
            string modelName = "HomeGenie";
            string modelDescription = "HomeGenie Home Automation Server";
            //string modelURL = "http://homegenie.it/";
            string modelNumber = "HG-1";
            string standardDeviceType = "HomeAutomationServer";
            string uniqueDeviceName = systemConfiguration.HomeGenie.GUID;
            if (String.IsNullOrEmpty(uniqueDeviceName))
            {
                systemConfiguration.HomeGenie.GUID = uniqueDeviceName = System.Guid.NewGuid().ToString();
                systemConfiguration.Update();
                // initialize database for first use
                statisticsLogger.ResetDatabase();
            }
            //
            var localDevice = UPnPDevice.CreateRootDevice(900, 1, "web\\");
            //hgdevice.Icon = null;
            if (presentationUrl != "")
            {
                localDevice.HasPresentation = true;
                localDevice.PresentationURL = presentationUrl;
            }
            localDevice.FriendlyName = modelName + ": " + Environment.MachineName;
            localDevice.Manufacturer = manufacturer;
            localDevice.ManufacturerURL = manufacturerUrl;
            localDevice.ModelName = modelName;
            localDevice.ModelDescription = modelDescription;
            if (Uri.IsWellFormedUriString(manufacturerUrl, UriKind.Absolute))
            {
                localDevice.ModelURL = new Uri(manufacturerUrl);
            }
            localDevice.ModelNumber = modelNumber;
            localDevice.StandardDeviceType = standardDeviceType;
            localDevice.UniqueDeviceName = uniqueDeviceName;
            localDevice.StartDevice();
        }

        // this is used to generate Lirc supported remotes from http://lirc.sourceforge.net/remotes/
        private List<string> GetLircItems(string url)
        {
            string[] lines = new string[0];
            using (var webclient = new WebClient())
            {
                string response = webclient.DownloadString(url);

                string pattern = @"<(.|\n)*?>";
                response = response.Replace("</a>", " ");
                response = System.Text.RegularExpressions.Regex.Replace(response, pattern, string.Empty);

                response = response.Replace("&amp;", "&");
                response = response.Replace("&nbsp;", " ");

                lines = response.Split('\n');

                webclient.Dispose();
            }

            bool readItems = false;
            var manufacturers = new List<string>();
            foreach (string l in lines)
            {
                if (readItems)
                {
                    if (l.Trim() == "") break;
                    string brand = l.Split('/')[0];
                    brand = brand.Split(' ')[0];
                    manufacturers.Add(brand.Trim());
                }
                else if (l.ToLower().StartsWith("parent directory"))
                {
                    readItems = true;
                }
            }
            return manufacturers;
        }

        #endregion
    }
}
