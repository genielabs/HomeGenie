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
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Text;
using System.Timers;
using System.Xml.Serialization;

using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;

using HomeGenie.Automation;
using HomeGenie.Data;
using HomeGenie.Service.Constants;
using HomeGenie.Service.Logging;
using HomeGenie.Automation.Scheduler;

using MIG;
using MIG.Interfaces.HomeAutomation.Commons;
using MIG.Interfaces.HomeAutomation;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using OpenSource.UPnP;

namespace HomeGenie.Service
{
    public class HomeGenieService
    {
        #region Private Fields declaration

        private const string HOMEGENIE_MASTERNODE = "0";
        private MIGService migService;
        private ProgramEngine masterControlProgram;
        private VirtualMeter virtualMeter;
        private UpdateChecker updateChecker;
        private StatisticsLogger statisticsLogger;
        // Internal data structures
        private TsList<Module> systemModules = new HomeGenie.Service.TsList<Module>();
        private TsList<Module> modulesGarbage = new HomeGenie.Service.TsList<Module>();
        private TsList<VirtualModule> virtualModules = new TsList<VirtualModule>();
        private List<Group> automationGroups = new List<Group>();
        private List<Group> controlGroups = new List<Group>();
        //
        private TsList<LogEntry> recentEventsLog;
        //
        private SystemConfiguration systemConfiguration;
        //
        private object interfaceControlLock = new object();
        //
        // public events
        public event Action<LogEntry> LogEventAction;

        public class RoutedEvent
        {
            public object Sender;
            public Module Module;
            public ModuleParameter Parameter;
        }

        #endregion

        #region Web Service Handlers declaration

        private Handlers.Config wshConfig;
        private Handlers.Automation wshAutomation;
        private Handlers.Interconnection wshInterconnection;
        private Handlers.Statistics wshStatistics;
        private Handlers.Logging wshLogging;

        #endregion

        #region Lifecycle

        public HomeGenieService()
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            // TODO: all the following initialization stuff should go async
            //
            // initialize recent log list
            recentEventsLog = new TsList<LogEntry>();

            #region MIG Service initialization and startup

            //
            // initialize MIGService, interfaces (hw controllers drivers), webservice
            migService = new MIG.MIGService();
            migService.InterfaceModulesChanged += migService_InterfaceModulesChanged;
            migService.InterfacePropertyChanged += migService_InterfacePropertyChanged;
            migService.ServiceRequestPreProcess += migService_ServiceRequestPreProcess;
            migService.ServiceRequestPostProcess += migService_ServiceRequestPostProcess;
            //
            // load system configuration
            systemConfiguration = new SystemConfiguration();
            systemConfiguration.HomeGenie.ServicePort = 8080;
            systemConfiguration.OnUpdate += systemConfiguration_OnUpdate;
            LoadSystemConfig();
            //
            // setup web service handlers
            wshConfig = new Handlers.Config(this);
            wshAutomation = new Handlers.Automation(this);
            wshInterconnection = new Handlers.Interconnection(this);
            wshStatistics = new Handlers.Statistics(this);
            wshLogging = new Handlers.Logging(this);
            //
            // Try to start WebGateway, if default HTTP port is busy, then it will try from 8080 to 8090
            bool serviceStarted = false;
            int bindAttempts = 0;
            int port = systemConfiguration.HomeGenie.ServicePort;
            while (!serviceStarted && bindAttempts <= 10)
            {
                // TODO: this should be done like this _services.Gateways["WebService"].Configure(....)
                migService.ConfigureWebGateway(
                    port,
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "html"),
                    "/hg/html",
                    systemConfiguration.HomeGenie.UserPassword
                );
                if (migService.StartGateways())
                {
                    serviceStarted = true;
                }
                else
                {
                    if (port < 8080) port = 8080;
                    else port++;
                    bindAttempts++;
                }
            }

            #endregion MIG Service initialization and startup

            //
            // If we successfully bound to port, then initialize the database.
            if (serviceStarted)
            {
                LogBroadcastEvent(
                    Domains.HomeAutomation_HomeGenie,
                    HOMEGENIE_MASTERNODE,
                    "HomeGenie service ready",
                    Properties.SYSTEMINFO_HTTPPORT,
                    port.ToString()
                );
                InitializeSystem();
                // Update system configuration with the HTTP port the service succeed to bind on
                systemConfiguration.HomeGenie.ServicePort = port;
            }
            else
            {
                LogBroadcastEvent(
                    Domains.HomeAutomation_HomeGenie,
                    HOMEGENIE_MASTERNODE,
                    "Http port bind failed.",
                    Properties.SYSTEMINFO_HTTPPORT,
                    systemConfiguration.HomeGenie.ServicePort.ToString()
                );
                Program.Quit(false);
            }

            updateChecker = new UpdateChecker(this);
            updateChecker.ArchiveDownloadUpdate += (object sender, ArchiveDownloadEventArgs args) =>
            {
                LogBroadcastEvent(
                    Domains.HomeGenie_UpdateChecker,
                    HOMEGENIE_MASTERNODE,
                    "HomeGenie Update Checker",
                    Properties.INSTALLPROGRESS_MESSAGE,
                    "= " + args.Status + ": " + args.ReleaseInfo.DownloadUrl
                );
            };
            updateChecker.UpdateProgress += (object sender, UpdateProgressEventArgs args) =>
            {
                LogBroadcastEvent(
                    Domains.HomeGenie_UpdateChecker,
                    HOMEGENIE_MASTERNODE,
                    "HomeGenie Update Checker",
                    Properties.INSTALLPROGRESS_UPDATE,
                    args.Status.ToString()
                );
            };
            updateChecker.InstallProgressMessage += (object sender, string message) =>
            {
                LogBroadcastEvent(
                    Domains.HomeGenie_UpdateChecker,
                    HOMEGENIE_MASTERNODE,
                    "HomeGenie Update Checker",
                    Properties.INSTALLPROGRESS_MESSAGE,
                    message
                );
            };
            //
            statisticsLogger = new StatisticsLogger(this);
            statisticsLogger.Start();
            //
            // Setup local UPnP device
            SetupUpnp();
            //
            // it will check every 24 hours
            updateChecker.Start();
            //
            Start();
        }

        public void Start()
        {
            LogBroadcastEvent(Domains.HomeGenie_System, HOMEGENIE_MASTERNODE, "HomeGenie System", Properties.HOMEGENIE_STATUS, "STARTED");
            //
            // Signal "SystemStarted" event to listeners
            for (int p = 0; p < masterControlProgram.Programs.Count; p++)
            {
                try
                {
                    var pb = masterControlProgram.Programs[p];
                    if (pb.IsEnabled)
                    {
                        if (pb.SystemStarted != null)
                        {
                            if (!pb.SystemStarted())
                            // stop routing this event to other listeners
                            break;
                        }
                    }
                }
                catch 
                {
                    // TODO: log error
                }
            }
        }

        public void Stop()
        {
            LogBroadcastEvent(Domains.HomeGenie_System, HOMEGENIE_MASTERNODE, "HomeGenie System", Properties.HOMEGENIE_STATUS, "STOPPING");
            //
            // Signal "SystemStopping" event to listeners
            for (int p = 0; p < masterControlProgram.Programs.Count; p++)
            {
                try
                {
                    var pb = masterControlProgram.Programs[p];
                    if (pb.IsEnabled)
                    {
                        if (pb.SystemStopping != null && !pb.SystemStopping())
                        {
                            // stop routing this event to other listeners
                            break;
                        }
                    }
                }
                catch 
                {
                    // TODO: log error
                }
            }
            //
            // save system data before quitting
            LogBroadcastEvent(Domains.HomeGenie_System, HOMEGENIE_MASTERNODE, "HomeGenie System", Properties.HOMEGENIE_STATUS, "SAVING DATA");
            UpdateModulesDatabase();
            systemConfiguration.Update();
            //
            LogBroadcastEvent(Domains.HomeGenie_System, HOMEGENIE_MASTERNODE, "HomeGenie System", Properties.HOMEGENIE_STATUS, "VirtualMeter STOPPING");
            if (virtualMeter != null) virtualMeter.Stop();
            LogBroadcastEvent(Domains.HomeGenie_System, HOMEGENIE_MASTERNODE, "HomeGenie System", Properties.HOMEGENIE_STATUS, "VirtualMeter STOPPED");
            LogBroadcastEvent(Domains.HomeGenie_System, HOMEGENIE_MASTERNODE, "HomeGenie System", Properties.HOMEGENIE_STATUS, "MIG Service STOPPING");
            if (migService != null) migService.StopService();
            LogBroadcastEvent(Domains.HomeGenie_System, HOMEGENIE_MASTERNODE, "HomeGenie System", Properties.HOMEGENIE_STATUS, "MIG Service STOPPED");
            LogBroadcastEvent(Domains.HomeGenie_System, HOMEGENIE_MASTERNODE, "HomeGenie System", Properties.HOMEGENIE_STATUS, "ProgramEngine STOPPING");
            if (masterControlProgram != null) masterControlProgram.StopEngine();
            LogBroadcastEvent(Domains.HomeGenie_System, HOMEGENIE_MASTERNODE, "HomeGenie System", Properties.HOMEGENIE_STATUS, "ProgramEngine STOPPED");
            //
            SystemLogger.Instance.Dispose();
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
        public Dictionary<string, MIGInterface> Interfaces
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
        public MIGService MigService
        {
            get { return migService; }
        }
        // Reference to ProgramEngine
        public ProgramEngine ProgramEngine
        {
            get { return masterControlProgram; }
        }
        // Reference to UpdateChecked
        public UpdateChecker UpdateChecker
        {
            get { return updateChecker; }
        }
        // Reference to Recent Events Log
        //TODO: deprecate this
        public TsList<LogEntry> RecentEventsLog
        {
            get { return recentEventsLog; }
        }
        // Reference to Statistics
        public StatisticsLogger Statistics
        {
            get { return statisticsLogger; }
        }
        // Public utility methods
        public int GetHttpServicePort()
        {
            return systemConfiguration.HomeGenie.ServicePort;
        }

        public MIGInterface GetInterface(string domain)
        {
            if (Interfaces.ContainsKey(domain)) return (Interfaces[domain]);
            else return null;
        }

        public void InterfaceControl(MIGInterfaceCommand cmd)
        {
            lock (interfaceControlLock)
            {
                var target = systemModules.Find(m => m.Domain == cmd.Domain && m.Address == cmd.NodeId);
                bool isRemoteModule = (target != null && !String.IsNullOrWhiteSpace(target.RoutingNode));
                if (isRemoteModule)
                {
                    // ...
                    try
                    {
                        string domain = cmd.Domain;
                        if (domain.StartsWith("HGIC:"))
                            domain = domain.Substring(domain.IndexOf(".") + 1);
                        string serviceUrl = "http://" + target.RoutingNode + "/api/" + domain + "/" + cmd.NodeId + "/" + cmd.Command + "/" + cmd.OptionsString;
                        Automation.Scripting.NetHelper netHelper = new Automation.Scripting.NetHelper(this).WebService(serviceUrl);
                        if (!String.IsNullOrWhiteSpace(systemConfiguration.HomeGenie.UserLogin) && !String.IsNullOrWhiteSpace(systemConfiguration.HomeGenie.UserPassword))
                        {
                            netHelper.WithCredentials(
                                systemConfiguration.HomeGenie.UserLogin,
                                systemConfiguration.HomeGenie.UserPassword
                            );
                        }
                        netHelper.Call();
                    }
                    catch (Exception ex)
                    {
                        HomeGenieService.LogEvent(
                            Domains.HomeAutomation_HomeGenie,
                            "Interconnection:" + target.RoutingNode,
                            ex.Message,
                            "Exception.StackTrace",
                            ex.StackTrace
                        );
                    }
                    return;
                }
                //
                object response = null;
                MIGInterface migInterface = GetInterface(cmd.Domain);
                if (migInterface != null)
                {
                    try
                    {
                        response = migInterface.InterfaceControl(cmd);
                    }
                    catch (Exception ex)
                    {
                        HomeGenieService.LogEvent(
                            Domains.HomeAutomation_HomeGenie,
                            "InterfaceControl",
                            ex.Message,
                            "Exception.StackTrace",
                            ex.StackTrace
                        );
                    }
                }
                //
                if (response == null || response.Equals(""))
                {
                    migService.WebServiceDynamicApiCall(cmd);
                }
                //
                migService_ServiceRequestPostProcess(null, cmd);
            }
        }

        //TODO: should these two moved to ProgramEngine?
        public void RegisterDynamicApi(string apiCall, Func<object, object> handler)
        {
            MIG.Interfaces.DynamicInterfaceAPI.Register(apiCall, handler);
        }

        public void UnRegisterDynamicApi(string apiCall)
        {
            MIG.Interfaces.DynamicInterfaceAPI.UnRegister(apiCall);
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
                HomeGenieService.LogEvent(
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

        public bool ExecuteAutomationRequest(MIGInterfaceCommand command)
        {
            bool handled = false; //never assigned
            string levelValue, commandValue;
            // check for certain commands
            if (command.Command == Commands.Groups.GROUPS_LIGHTSOFF)
            {
                levelValue = "0";
                commandValue = Commands.Control.CONTROL_OFF;
            }
            else if (command.Command == Commands.Groups.GROUPS_LIGHTSON)
            {
                levelValue = "1";
                commandValue = Commands.Control.CONTROL_ON;
            }
            else
            {
                return handled;
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
                            MIGInterfaceCommand icmd = new MIGInterfaceCommand(module.Domain + "/" + module.Address + "/" + commandValue);
                            InterfaceControl(icmd);
                            Service.Utility.ModuleParameterGet(module, ModuleParameters.MODPAR_STATUS_LEVEL).Value = levelValue;
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
                // TODO: handle exception here
            }
            return handled;
        }

        #endregion

        #region MIG Service events handling

        private void migService_InterfaceModulesChanged (InterfaceModulesChangedAction args)
        {
            modules_RefreshInterface(GetInterface(args.Domain));
        }
        // called by interfaces, when a device changes
        internal void migService_InterfacePropertyChanged(InterfacePropertyChangedAction propertyChangedAction)
        {
            // look for module associated to this event
            Module module = null;
            try
            {
                module = Modules.Find(delegate(Module o)
                {
                    return o.Domain == propertyChangedAction.Domain && o.Address == propertyChangedAction.SourceId;
                });
            }
            catch
            {
            }
            //
            if (module != null && propertyChangedAction.Path != "")
            {
                // clear RoutingNode property since the event was locally generated
                //if (module.RoutingNode != "")
                //{
                //    module.RoutingNode = "";
                //}
                // we found associated module in HomeGenie.Modules

                SignalModulePropertyChange(migService, module, propertyChangedAction);

            }
            else
            {
                if (propertyChangedAction.Domain == Domains.MigService_Interfaces)
                {
                    modules_RefreshInterface(GetInterface(propertyChangedAction.SourceId));
                }
                LogBroadcastEvent(
                    propertyChangedAction.Domain,
                    propertyChangedAction.SourceId,
                    propertyChangedAction.SourceType,
                    propertyChangedAction.Path,
                    propertyChangedAction.Value != null ? propertyChangedAction.Value.ToString() : ""
                );
            }
        }
        // Check if command was Control.*, update the ModuleParameter. This should happen in a HWInt->HomeGenie pathway
        private void migService_ServiceRequestPostProcess(MIGClientRequest request, MIGInterfaceCommand command)
        {
            // REMARK: No post data is available at this point since it has already beel consumed by ServiceRequestPreProcess
            switch (command.Domain)
            {
            case Domains.HomeAutomation_X10:
            case Domains.HomeAutomation_ZWave:
                Module module = null;
                try
                {
                    module = Modules.Find(o => o.Domain == command.Domain && o.Address == command.NodeId);
                }
                catch
                {
                }
                //
                // TODO: this should be placed somewhere else (this is specific code for handling interface responses, none of HG business)
                if (module != null)
                {
                    // wait for ZWaveLib asynchronous response from node and raise the proper "parameter changed" event
                    if (command.Domain == Domains.HomeAutomation_ZWave)  //  && (context != null && !context.Request.IsLocal)
                    {
                        if (command.Command == ZWave.Command.BASIC_GET)
                        {
                            command.Response = Utility.WaitModuleParameterChange(module, Properties.ZWAVENODE_BASIC);
                            command.Response = JsonHelper.GetSimpleResponse(command.Response);
                        }
                        else if (command.Command == ZWave.Command.WAKEUP_GET)
                        {
                            command.Response = Utility.WaitModuleParameterChange(
                                module,
                                Properties.ZWAVENODE_WAKEUPINTERVAL
                            );
                            command.Response = JsonHelper.GetSimpleResponse(command.Response);
                        }
                        else if (command.Command == ZWave.Command.BATTERY_GET)
                        {
                            command.Response = Utility.WaitModuleParameterChange(module, Properties.ZWAVENODE_BATTERY);
                            command.Response = JsonHelper.GetSimpleResponse(command.Response);
                        }
                        else if (command.Command == ZWave.Command.MULTIINSTANCE_GET)
                        {
                            command.Response = Utility.WaitModuleParameterChange(
                                module,
                                Properties.ZWAVENODE_MULTIINSTANCE + "." + command.GetOption(0).Replace(".", "") + "." + command.GetOption(1)
                            );
                            command.Response = JsonHelper.GetSimpleResponse(command.Response);
                        }
                        else if (command.Command == ZWave.Command.MULTIINSTANCE_GETCOUNT)
                        {
                            command.Response = Utility.WaitModuleParameterChange(
                                module, 
                                Properties.ZWAVENODE_MULTIINSTANCE + "." + command.GetOption(0).Replace(".", "") + "." + ".Count"
                            );
                            command.Response = JsonHelper.GetSimpleResponse(command.Response);
                        }
                        else if (command.Command == ZWave.Command.ASSOCIATION_GET)
                        {
                            command.Response = Utility.WaitModuleParameterChange(
                                module,
                                Properties.ZWAVENODE_ASSOCIATIONS + "." + command.GetOption(0)
                            );
                            command.Response = JsonHelper.GetSimpleResponse(command.Response);
                        }
                        else if (command.Command == ZWave.Command.CONFIG_PARAMETERGET)
                        {
                            command.Response = Utility.WaitModuleParameterChange(
                                module,
                                Properties.ZWAVENODE_CONFIGVARIABLES + "." + command.GetOption(0)
                            );
                            command.Response = JsonHelper.GetSimpleResponse(command.Response);
                        }
                        else if (command.Command == ZWave.Command.NODEINFO_GET)
                        {
                            command.Response = Utility.WaitModuleParameterChange(module, Properties.ZWAVENODE_NODEINFO);
                            command.Response = JsonHelper.GetSimpleResponse(command.Response);
                        }
                        else if (command.Command == ZWave.Command.MANUFACTURERSPECIFIC_GET)
                        {
                            command.Response = Utility.WaitModuleParameterChange(
                                module,
                                Properties.ZWAVENODE_MANUFACTURERSPECIFIC
                            );
                            command.Response = JsonHelper.GetSimpleResponse(command.Response);
                        }
                    }
                }
                break;
            case Domains.MigService_Interfaces:
                if (command.Command.EndsWith(".Set"))
                {
                    systemConfiguration.Update();
                }
                break;
            }
            //
            // Macro Recording
            //
            if (masterControlProgram != null && masterControlProgram.MacroRecorder.IsRecordingEnabled && command != null && command.Command != null && (command.Command.StartsWith("Control.") || (command.Command.StartsWith("AvMedia.") && command.Command != "AvMedia.Browse" && command.Command != "AvMedia.GetUri")))
            {
                masterControlProgram.MacroRecorder.AddCommand(command);
            }
        }
        // execute the requested command (from web service)
        private void migService_ServiceRequestPreProcess(MIGClientRequest request, MIGInterfaceCommand migCommand)
        {
            LogBroadcastEvent(
                "MIG.Gateways.WebServiceGateway",
                request.RequestOrigin,
                request.RequestMessage,
                request.SubjectName,
                request.SubjectValue
            );

            #region Interconnection (Remote Node Command Routing)

            Module target = systemModules.Find(m => m.Domain == migCommand.Domain && m.Address == migCommand.NodeId);
            bool isRemoteModule = (target != null && !String.IsNullOrWhiteSpace(target.RoutingNode));
            if (isRemoteModule)
            {
                // ...
                try
                {
                    string domain = migCommand.Domain;
                    if (domain.StartsWith("HGIC:")) domain = domain.Substring(domain.IndexOf(".") + 1);
                    string serviceurl = "http://" + target.RoutingNode + "/api/" + domain + "/" + migCommand.NodeId + "/" + migCommand.Command + "/" + migCommand.OptionsString;
                    Automation.Scripting.NetHelper neth = new Automation.Scripting.NetHelper(this).WebService(serviceurl);
                    if (systemConfiguration.HomeGenie.UserLogin != "" && systemConfiguration.HomeGenie.UserPassword != "")
                    {
                        neth.WithCredentials(
                            systemConfiguration.HomeGenie.UserLogin,
                            systemConfiguration.HomeGenie.UserPassword
                        );
                    }
                    neth.Call();
                }
                catch (Exception ex)
                {
                    HomeGenieService.LogEvent(
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
                switch (migCommand.NodeId)
                {
                case "Logging":

                    wshLogging.ProcessRequest(request, migCommand);
                    break;

                case "Config":

                    wshConfig.ProcessRequest(request, migCommand);
                    break;

                case "Automation":

                    wshAutomation.ProcessRequest(request, migCommand);
                    break;

                case "Interconnection":

                    wshInterconnection.ProcessRequest(request, migCommand);
                    break;

                case "Statistics":

                    wshStatistics.ProcessRequest(request, migCommand);
                    break;
                }
            }
            else if (migCommand.Domain == Domains.HomeAutomation_HomeGenie_Automation)
            {
                int n;
                bool nodeIdIsNumeric = int.TryParse(migCommand.NodeId, out n);
                if (nodeIdIsNumeric)
                {
                    switch (migCommand.Command)
                    {
                    case "Control.Run":
                        wshAutomation.ProgramRun(migCommand.NodeId, migCommand.GetOption(0));
                        break;
                    case "Control.Break":
                        wshAutomation.ProgramBreak(migCommand.NodeId);
                        break;
                    }
                }
            }

        }

        #endregion

        #region Module/Interface Events handling and propagation

        public void SignalModulePropertyChange(
            object sender,
            Module module,
            InterfacePropertyChangedAction propertyChangedAction
        )
        {

            // update module parameter value
            ModuleParameter parameter = null;
            try
            {
                parameter = Utility.ModuleParameterGet(module, propertyChangedAction.Path);
                if (parameter == null)
                {
                    module.Properties.Add(new ModuleParameter() {
                        Name = propertyChangedAction.Path,
                        Value = propertyChangedAction.Value.ToString()
                    });
                    parameter = Utility.ModuleParameterGet(module, propertyChangedAction.Path);
                }
                else
                {
                    parameter.Value = propertyChangedAction.Value.ToString();
                }
            }
            catch
            {
                //                HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "SignalModulePropertyChange(...)", ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
            //
            string eventValue = (propertyChangedAction.Value.GetType() == typeof(String) ? propertyChangedAction.Value.ToString() : JsonConvert.SerializeObject(propertyChangedAction.Value));
            LogBroadcastEvent(
                propertyChangedAction.Domain,
                propertyChangedAction.SourceId,
                propertyChangedAction.SourceType,
                propertyChangedAction.Path,
                eventValue
            );
            //
            ///// ROUTE EVENT TO LISTENING AutomationPrograms
            if (masterControlProgram != null)
            {
                RoutedEvent eventData = new RoutedEvent() {
                    Sender = sender,
                    Module = module,
                    Parameter = parameter
                };
                ThreadPool.QueueUserWorkItem(new WaitCallback(RouteParameterChangedEvent), eventData);
            }
        }

        public void RouteParameterChangedEvent(object eventData)
        {
            try
            {
                bool proceed = true;
                RoutedEvent moduleEvent = (RoutedEvent)eventData;
                foreach (var program in masterControlProgram.Programs)
                {
                    if ((moduleEvent.Sender == null || !moduleEvent.Sender.Equals(program)))
                    {
                        try
                        {
                            if (program.ModuleIsChangingHandler != null)
                            {
                                if (!program.ModuleIsChangingHandler(
                                        new Automation.Scripting.ModuleHelper(
                                            this,
                                            moduleEvent.Module
                                        ),
                                        moduleEvent.Parameter
                                    ))
                                {
                                    proceed = false;
                                    break;
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            HomeGenieService.LogEvent(
                                program.Domain,
                                program.Address.ToString(),
                                ex.Message,
                                "Exception.StackTrace",
                                ex.StackTrace
                            );
                        }
                    }
                }
                if (proceed)
                {
                    foreach (ProgramBlock program in masterControlProgram.Programs)
                    {
                        if ((moduleEvent.Sender == null || !moduleEvent.Sender.Equals(program)))
                        {
                            try
                            {
                                if (program.ModuleChangedHandler != null && moduleEvent.Parameter != null) // && proceed)
                                {
                                    if (!program.ModuleChangedHandler(
                                            new Automation.Scripting.ModuleHelper(
                                                this,
                                                moduleEvent.Module
                                            ),
                                            moduleEvent.Parameter
                                        ))
                                    {
                                        break;
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {
                                HomeGenieService.LogEvent(
                                    program.Domain,
                                    program.Address.ToString(),
                                    ex.Message,
                                    "Exception.StackTrace",
                                    ex.StackTrace
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                HomeGenieService.LogEvent(
                    Domains.HomeAutomation_HomeGenie,
                    "RouteParameterChangedEvent()",
                    e.Message,
                    "Exception.StackTrace",
                    e.StackTrace
                ); 
            }
        }

        #endregion

        #region Logging

        internal void LogBroadcastEvent(
            string domain,
            string source,
            string description,
            string property,
            string value
        )
        {
            // these events are also routed to the UI
            var logEntry = new LogEntry() {
                Domain = domain,
                Source = source,
                Description = description,
                Property = property,
                Value = value
            };
            try
            {
                if (recentEventsLog.Count > 100)
                {
                    recentEventsLog.RemoveRange(0, recentEventsLog.Count - 100);
                }
                recentEventsLog.Add(logEntry);
                //
                if (LogEventAction != null)
                {
                    LogEventAction(logEntry);
                }
            }
            catch
            {
                System.Diagnostics.Debugger.Break();
            }
            //
            LogEvent(logEntry);
        }

        public static void LogEvent(string domain, string source, string description, string property, string value)
        {
            var logEntry = new LogEntry() {
                Domain = domain,
                Source = source,
                Description = description,
                Property = property,
                Value = value.Replace("\"", "")
            };
            LogEvent(logEntry);
        }

        public static void LogEvent(LogEntry logentry)
        {
            if (SystemLogger.Instance.IsLogEnabled)
            {
                //Console.ResetColor ();
                Console.WriteLine(logentry);
                try
                {
                    SystemLogger.Instance.WriteToLog(logentry);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Logger: could not process event! " + ex.Message + "\n" + ex.StackTrace);
                }
            }
        }

        #endregion Logging

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
            //
            modules_RefreshAll();
            //
            lock (systemModules.LockObject) try
                {
                    // Due to encrypted values, we must clone modules before encrypting and saving
                    var clonedModules = (List<Module>)systemModules.Clone();
                    foreach (var module in clonedModules)
                    {
                        foreach (var parameter in module.Properties)
                        {
                            // these two properties have to be kept in clear text
                            if (parameter.Name != Properties.WIDGET_DISPLAYMODULE 
                                && parameter.Name != Properties.VIRTUALMODULE_PARENTID
                                && parameter.Name != Properties.PROGRAM_STATUS
                                && parameter.Name != Properties.RUNTIME_ERROR
                            )
                            {
                                if (!String.IsNullOrEmpty(parameter.Value)) parameter.Value = StringCipher.Encrypt(
                                        parameter.Value,
                                        systemConfiguration.GetPassPhrase()
                                    );
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
                    HomeGenieService.LogEvent(
                        Domains.HomeAutomation_HomeGenie,
                        "UpdateModulesDatabase()",
                        ex.Message,
                        "Exception.StackTrace",
                        ex.StackTrace
                    );
                }
            //
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
                var reader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "groups.xml"));
                controlGroups = (List<Group>)serializer.Deserialize(reader);
                reader.Close();
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
                var reader = new StreamReader(Path.Combine(
                                 AppDomain.CurrentDomain.BaseDirectory,
                                 "automationgroups.xml"
                             ));
                automationGroups = (List<Group>)serializer.Deserialize(reader);
                reader.Close();
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
                masterControlProgram.StopEngine();
                masterControlProgram = null;
            }
            masterControlProgram = new ProgramEngine(this);
            try
            {
                var serializer = new XmlSerializer(typeof(List<ProgramBlock>));
                var reader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs.xml"));
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
                reader.Close();
            }
            catch (Exception ex)
            {
                //TODO: log error
                HomeGenieService.LogEvent(
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
                var reader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scheduler.xml"));
                var schedulerItems = (List<SchedulerItem>)serializer.Deserialize(reader);
                masterControlProgram.SchedulerService.Items.AddRange(schedulerItems);
                reader.Close();
            }
            catch
            {
                //TODO: log error
            }
            //
            // start MIG Interfaces
            //
            try
            {
                migService.StartInterfaces();
            }
            catch
            {
                //TODO: log error
            }
            // force re-generation of Modules list
            //_jsonSerializedModules(false);
            modules_RefreshAll();
            //
            // enable automation programs engine
            //
            masterControlProgram.Enabled = true;
        }

        public void RestoreFactorySettings()
        {
            string archiveName = "homegenie_factory_config.zip";
            //
            try
            {
                masterControlProgram.Enabled = false;
                masterControlProgram.StopEngine();
                // delete old programs assemblies
                foreach (var program in masterControlProgram.Programs)
                {
                    program.AppAssembly = null;
                }
                masterControlProgram = null;
            }
            catch
            {
            }
            //
            UnarchiveConfiguration(archiveName, AppDomain.CurrentDomain.BaseDirectory);
            //
            LoadConfiguration();
            //
            // regenerate encrypted files
            UpdateModulesDatabase();
            SystemConfiguration.Update();
        }

        public void BackupCurrentSettings()
        {
            // regenerate encrypted files
            UpdateProgramsDatabase();
            UpdateModulesDatabase();
            SystemConfiguration.Update();
            ArchiveConfiguration("html/homegenie_backup_config.zip");
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
                    var virtualModuleWidget = Utility.ModuleParameterGet(
                                                  virtualModule,
                                                  Properties.WIDGET_DISPLAYMODULE
                                              );
                    //
                    Module module = Modules.Find(o => o.Domain == virtualModule.Domain && o.Address == virtualModule.Address);

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
                    if (module.DeviceType == MIG.ModuleTypes.Generic)
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
                    Utility.ModuleParameterSet(
                        module,
                        Properties.VIRTUALMODULE_PARENTID,
                        virtualModule.ParentId
                    );
                    var moduleWidget = Utility.ModuleParameterGet(module, Properties.WIDGET_DISPLAYMODULE);
                    // if a widget is specified on virtual module then we force module to display using this
                    if ((virtualModuleWidget != null && (virtualModuleWidget.Value != "" || moduleWidget == null)) && (moduleWidget == null || (moduleWidget.Value != virtualModuleWidget.Value)))
                    {
                        Utility.ModuleParameterSet(
                            module,
                            Properties.WIDGET_DISPLAYMODULE,
                            virtualModuleWidget.Value
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(
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
                //
                // ProgramEngine programs (modules)
                //
                if (masterControlProgram != null)
                {
                    lock (masterControlProgram.Programs.LockObject) 
                    foreach (var program in masterControlProgram.Programs)
                    {
                        Module module = null;
                        try
                        {
                            module = systemModules.Find(delegate(Module o)
                            {
                                return o.Domain == Domains.HomeAutomation_HomeGenie_Automation && o.Address == program.Address.ToString();
                            });
                        }
                        catch
                        {
                        }
                        //
                        if (module != null && program.Type.ToLower() == "wizard" && !program.IsEnabled && module.RoutingNode == "")
                        {
                            systemModules.Remove(module);
                            continue;
                        }
                        else if (/*program.Type.ToLower() != "wizard" &&*/ !program.IsEnabled)
                        {
                            continue;
                        }
                        //
                        // add new module
                        if (module == null)
                        {
                            module = new Module();
                            module.Domain = Domains.HomeAutomation_HomeGenie_Automation;
                            if (program.Type.ToLower() == "wizard")
                            {
                                Utility.ModuleParameterSet(
                                    module,
                                    Properties.WIDGET_DISPLAYMODULE,
                                    "homegenie/generic/program"
                                );
                            }
                            systemModules.Add(module);
                        }
                        //
                        module.Name = program.Name;
                        module.Address = program.Address.ToString();
                        module.DeviceType = MIG.ModuleTypes.Program;
                        //module.Description = "Wizard Script";
                    }
                }
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(
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
                HomeGenieService.LogEvent(
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
                    modules_RefreshInterface(iface.Value);
                } catch {
                    //TODO: interface not ready? handle this
                }
            }

            // Refresh other HG modules
            modules_RefreshPrograms();
            modules_RefreshVirtualModules();

            modules_Sort();
        }

        private void modules_RefreshInterface(MIGInterface iface)
        {
            // TODO: read IsEnabled instead of IsConnected
            if (migService.Configuration.GetInterface(iface.Domain).IsEnabled)
            {
                var interfaceModules = iface.GetModules();
                if (interfaceModules.Count > 0)
                {
                    // delete removed modules
                    var deleted = systemModules.FindAll(m => m.Domain == iface.Domain && (interfaceModules.Find(m1 => m1.Address == m.Address && m1.Domain == m.Domain) == null));
                    foreach (var mod in deleted)
                    {
                        var virtualParam = Utility.ModuleParameterGet(mod, "VirtualModule.ParentId");
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
                        //
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
                var deleted = systemModules.FindAll(m => m.Domain == iface.Domain);
                foreach (var mod in deleted)
                {
                    var virtualParam = Utility.ModuleParameterGet(mod, "VirtualModule.ParentId");
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
            LoadConfiguration();
            //
            // setup other objects used in HG
            //
            virtualMeter = new VirtualMeter(this);
        }

        private void LoadSystemConfig()
        {
            try
            {
                // load config
                var serializer = new XmlSerializer(typeof(SystemConfiguration));
                var reader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "systemconfig.xml"));
                systemConfiguration = (SystemConfiguration)serializer.Deserialize(reader);
                if (!String.IsNullOrEmpty(systemConfiguration.HomeGenie.EnableLogFile) && systemConfiguration.HomeGenie.EnableLogFile.ToLower().Equals("true"))
                {
                    SystemLogger.Instance.OpenLog();
                }
                else
                {
                    SystemLogger.Instance.CloseLog();
                }
                // set the system password
                migService.SetWebServicePassword(systemConfiguration.HomeGenie.UserPassword);
                //
                foreach (var parameter in systemConfiguration.HomeGenie.Settings)
                {
                    try
                    {
                        if (!String.IsNullOrEmpty(parameter.Value)) parameter.Value = StringCipher.Decrypt(
                                parameter.Value,
                                systemConfiguration.GetPassPhrase()
                            );
                    }
                    catch
                    {
                    }
                }
                //
                reader.Close();
                //
                // configure MIG
                //
                if (systemConfiguration.MIGService.GetInterface("HomeAutomation.Insteon") == null)
                {
                    var options = new List<MIGServiceConfiguration.Interface.Option>();
                    options.Add(new MIGServiceConfiguration.Interface.Option(){ Name = "Port", Value = "" });
                    systemConfiguration.MIGService.Interfaces.Add(new MIGServiceConfiguration.Interface(){ 
                        Domain = "HomeAutomation.Insteon",
                        Options = options
                    });
                }
                migService.Configuration = systemConfiguration.MIGService;
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(
                    Domains.HomeAutomation_HomeGenie,
                    "LoadSystemConfig()",
                    ex.Message,
                    "Exception.StackTrace",
                    ex.StackTrace
                );
            }

        }

        private void LoadModules()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(HomeGenie.Service.TsList<Module>));
                var reader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "modules.xml"));
                HomeGenie.Service.TsList<Module> modules = (HomeGenie.Service.TsList<Module>)serializer.Deserialize(reader);
                //
                foreach (var module in modules)
                {                    
                    if (module.Domain == "HomeAutomation.ConnAir")
                    {
                        var migInterface = systemConfiguration.MIGService.GetInterface(module.Domain);
                        if (migInterface != null)
                        {
                            migService.Modules.Add(module.Domain +"_" + module.Address, new InterfaceModule()
                            {                         
                            Domain = module.Domain,
                            Address = module.Address,
                            Description = module.Description,
                            ModuleType = module.DeviceType,
                            CustomData = module.Properties
                            });                            
                        }
                    }

                    foreach (var parameter in module.Properties)
                    {
                        try
                        {
                            if (!String.IsNullOrEmpty(parameter.Value)) parameter.Value = StringCipher.Decrypt(
                                    parameter.Value,
                                    systemConfiguration.GetPassPhrase()
                                );
                        }
                        catch
                        {
                        }
                    }
                }
                //
                reader.Close();
                //
                modulesGarbage.Clear();
                systemModules.Clear();
                systemModules = modules;
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(
                    Domains.HomeAutomation_HomeGenie,
                    "LoadModules()",
                    ex.Message,
                    "Exception.StackTrace",
                    ex.StackTrace
                );
            }
            try
            {
                //
                // reset Parameter.Watts, /*Status Level,*/ Sensor.Generic values
                //
                for (int m = 0; m < systemModules.Count; m++)
                {
                    // cleanup stuff for unwanted  xsi:nil="true" empty params
                    systemModules[m].Properties.RemoveAll(p => p == null);
                    //
                    ModuleParameter parameter = null;
                    parameter = systemModules[m].Properties.Find(delegate(ModuleParameter mp)
                    {
                        return mp.Name == ModuleParameters.MODPAR_METER_WATTS /*|| mp.Name == ModuleParameters.MODPAR_STATUS_LEVEL*/ || mp.Name == ModuleParameters.MODPAR_SENSOR_GENERIC;
                    });
                    if (parameter != null)
                    {
                        parameter.Value = "0";
                        //parameter.UpdateTime = DateTime.UtcNow;
                    }
                }
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(
                    Domains.HomeAutomation_HomeGenie,
                    "LoadModules()",
                    ex.Message,
                    "Exception.StackTrace",
                    ex.StackTrace
                );
            }
            //
            // force re-generation of Modules list
            modules_RefreshAll();
        }

        private void ArchiveConfiguration(string archiveName)
        {
            if (File.Exists(archiveName))
            {
                File.Delete(archiveName);
            }
            foreach (var program in masterControlProgram.Programs)
            {
                string relFile = Path.Combine("programs/", program.Address + ".dll");
                if (File.Exists(relFile))
                {
                    Utility.AddFileToZip(archiveName, relFile);
                }
                if (program.Type.ToLower() == "arduino")
                {
                    string arduinoFolder = Path.Combine("programs", "arduino", program.Address.ToString());
                    string[] filePaths = Directory.GetFiles(arduinoFolder);
                    foreach (string f in filePaths)
                    {
                        Utility.AddFileToZip(archiveName, Path.Combine(arduinoFolder, Path.GetFileName(f)));
                    }
                }
            }
            //
            Utility.AddFileToZip(archiveName, "systemconfig.xml");
            Utility.AddFileToZip(archiveName, "automationgroups.xml");
            Utility.AddFileToZip(archiveName, "modules.xml");
            Utility.AddFileToZip(archiveName, "programs.xml");
            Utility.AddFileToZip(archiveName, "scheduler.xml");
            Utility.AddFileToZip(archiveName, "groups.xml");
            if (File.Exists("lircconfig.xml"))
            {
                Utility.AddFileToZip(archiveName, "lircconfig.xml");
            }
        }
        
        public void UnarchiveConfiguration(string archiveName, string destinationFolder)
        {
            // Unarchive (unzip)
            using (var package = Package.Open(archiveName, FileMode.Open, FileAccess.Read))
            {
                foreach (var part in package.GetParts())
                {
                    string target = Path.Combine(destinationFolder, part.Uri.OriginalString.Substring(1));
                    if (!Directory.Exists(Path.GetDirectoryName(target)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                    }

                    if (File.Exists(target)) File.Delete(target);

                    using (var source = part.GetStream(FileMode.Open, FileAccess.Read)) using (var destination = File.OpenWrite(target))
                    {
                        byte[] buffer = new byte[4096];
                        int read;
                        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            destination.Write(buffer, 0, read);
                        }
                    }
                }
            }
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
            //
            string presentationUrl = "http://" + localIP + ":" + systemConfiguration.HomeGenie.ServicePort;
            //string friendlyName = "HomeGenie: " + Environment.MachineName;
            string manufacturer = "G-Labs";
            string manufacturerUrl = "http://generoso.info/";
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
                statisticsLogger.DatabaseReset();
            }
            //
            UPnPDevice localDevice = UPnPDevice.CreateRootDevice(900, 1, "web\\");
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
            var webclient = new WebClient();
            string response = webclient.DownloadString(url);

            string pattern = @"<(.|\n)*?>";
            response = response.Replace("</a>", " ");
            response = System.Text.RegularExpressions.Regex.Replace(response, pattern, string.Empty);

            response = response.Replace("&amp;", "&");
            response = response.Replace("&nbsp;", " ");

            string[] lines = response.Split('\n');

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
