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

/* 
 * 2014-02-24:
 *      Weecoboard-4M module     
 *      Author: Luciano Neri <l.neri@nerinformatica.it>
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
using HomeGenie.Automation;
using HomeGenie.Data;
using MIG;
using MIG.Interfaces.HomeAutomation.Commons;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XTenLib;
using ZWaveLib.Devices;
using System.Diagnostics;
using System.Net.Sockets;
using OpenSource.UPnP;
using HomeGenie.Service.Constants;
using MIG.Interfaces.HomeAutomation;
using HomeGenie.Service.Logging;
using HomeGenie.Automation.Scheduler;
using System.Threading;

namespace HomeGenie.Service
{
    public class HomeGenieService
    {
        #region Private Fields declaration

        private MIGService migService;
        private ProgramEngine masterControlProgram;
        private VirtualMeter virtualMeter;
        private UpdateChecker updateChecker;
        private StatisticsLogger statisticsLogger;
        // Internal data structures
        private TsList<Module> systemModules = new HomeGenie.Service.TsList<Module>();
        private TsList<VirtualModule> virtualModules = new TsList<VirtualModule>();
        private List<Group> automationGroups = new List<Group>();
        private List<Group> controlGroups = new List<Group>();
        //
        private TsList<LogEntry> recentEventsLog;
        //
        private SystemConfiguration systemConfiguration;
        //
        // Reference to Z-Wave and X10 drivers (obtained from MIGService)
        private ZWaveLib.Devices.Controller zwaveController;
        private XTenLib.XTenManager x10Controller;
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
            //
            // initialize recent log list
            recentEventsLog = new TsList<LogEntry>();

            #region MIG Service initialization and startup

            //
            // initialize MIGService, interfaces (hw controllers drivers), webservice
            migService = new MIG.MIGService();
            migService.InterfacePropertyChanged += new Action<InterfacePropertyChangedAction>(migService_InterfacePropertyChanged);
            migService.ServiceRequestPreProcess += new MIGService.WebServiceRequestPreProcessEventHandler(migService_ServiceRequestPreProcess);
            migService.ServiceRequestPostProcess += new MIGService.WebServiceRequestPostProcessEventHandler(migService_ServiceRequestPostProcess);
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
            // Try to start WebGateway, at  0 < (port - ServicePort) < 10
            bool serviceStarted = false;
            int port = systemConfiguration.HomeGenie.ServicePort;
            while (!serviceStarted && port <= systemConfiguration.HomeGenie.ServicePort + 10)
            {
                // TODO: this should be done like this _services.Gateways["WebService"].Configure(....)
                migService.ConfigureWebGateway(port, 443, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "html"), "/hg/html", systemConfiguration.HomeGenie.UserPassword);
                if (migService.StartService())
                {
                    systemConfiguration.HomeGenie.ServicePort = port;
                    serviceStarted = true;
                }
                else
                {
                    port++;
                }
            }

            #endregion MIG Service initialization and startup

            //
            // If we successfully bound to port, then initialize the database.
            if (serviceStarted)
            {
                LogBroadcastEvent(Domains.HomeAutomation_HomeGenie, "SystemInfo", "HomeGenie service ready", "HTTP.PORT", port.ToString());
                systemConfiguration.HomeGenie.ServicePort = port;
                InitializeSystem();
            }
            else
            {
                LogBroadcastEvent(Domains.HomeAutomation_HomeGenie, "SystemInfo", "Http port bind failed.", "HTTP.PORT", port.ToString());
            }

            updateChecker = new UpdateChecker(this);
            updateChecker.ArchiveDownloadUpdate += (object sender, ArchiveDownloadEventArgs args) =>
            {
                LogBroadcastEvent(Domains.HomeGenie_UpdateChecker, "0", "HomeGenie Update Checker", "InstallProgress.Message", "= " + args.Status + ": " + args.ReleaseInfo.DownloadUrl);
            };
            updateChecker.UpdateProgress += (object sender, UpdateProgressEventArgs args) =>
            {
                LogBroadcastEvent(Domains.HomeGenie_UpdateChecker, "0", "HomeGenie Update Checker", "Update Check", args.Status.ToString());
            };
            updateChecker.InstallProgressMessage += (object sender, string message) =>
            {
                LogBroadcastEvent(Domains.HomeGenie_UpdateChecker, "0", "HomeGenie Update Checker", "InstallProgress.Message", message);
            };
            // it will check every 24 hours
            updateChecker.Start();
            //
            statisticsLogger = new StatisticsLogger(this);
            statisticsLogger.Start();
            //
            LogBroadcastEvent(Domains.HomeGenie_System, "0", "HomeGenie System", "HomeGenie", "STARTED");
            //
            // Setup local UPnP device
            SetupUpnp();
        }

        public void Start()
        {
        }

        public void Stop()
        {
            LogBroadcastEvent(Domains.HomeGenie_System, "0", "HomeGenie System", "HomeGenie", "STOPPING");
            //
            // update last received parameters before quitting
            UpdateModulesDatabase();
            systemConfiguration.Update();
            //
            if (virtualMeter != null) virtualMeter.Stop();
            if (masterControlProgram != null) masterControlProgram.StopEngine();
            if (migService != null) migService.StopService();
            //
            SystemLogger.Instance.Dispose();
        }

        #endregion

        #region Data Wrappers - Public Members

        // Control groups (i.e. rooms, Outside, Housewide)
        public List<Group> Groups { get { return controlGroups; } }
        // Automation groups
        public List<Group> AutomationGroups { get { return automationGroups; } }
        // MIG interfaces
        public Dictionary<string, MIGInterface> Interfaces { get { return migService.Interfaces; } }
        // Modules
        public TsList<Module> Modules { get { return systemModules; } }
        // Virtual modules
        public TsList<VirtualModule> VirtualModules { get { return virtualModules; } }
        // HomeGenie system parameters
        public List<ModuleParameter> Parameters { get { return systemConfiguration.HomeGenie.Settings; } }
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
            if (Interfaces.ContainsKey(domain))
                return (Interfaces[domain]);
            else
                return null;
        }

        public void InterfaceControl(MIGInterfaceCommand cmd)
        {
            var target = systemModules.Find(m => m.Domain == cmd.Domain && m.Address == cmd.NodeId);
            bool isRemoteModule = (target != null && target.RoutingNode.Trim() != "");
            if (isRemoteModule)
            {
                // ...
                try
                {
                    string serviceUrl = "http://" + target.RoutingNode + "/api/" + cmd.Domain + "/" + cmd.NodeId + "/" + cmd.Command + "/" + cmd.OptionsString;
                    Automation.Scripting.NetHelper netHelper = new Automation.Scripting.NetHelper(this).WebService(serviceUrl);
                    if (systemConfiguration.HomeGenie.UserLogin != "" && systemConfiguration.HomeGenie.UserPassword != "")
                    {
                        netHelper.WithCredentials(systemConfiguration.HomeGenie.UserLogin, systemConfiguration.HomeGenie.UserPassword);
                    }
                    netHelper.Call();
                }
                catch (Exception ex)
                {
                    HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "Interconnection:" + target.RoutingNode, ex.Message, "Exception.StackTrace", ex.StackTrace);
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
                    HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "InterfaceControl", ex.Message, "Exception.StackTrace", ex.StackTrace);
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


        public void InterfaceEnable(string domain)
        {
            try
            {
                switch (domain)
                {
                    case Domains.HomeAutomation_ZWave:
                        var zwaveInterface = (GetInterface(Domains.HomeAutomation_ZWave) as MIG.Interfaces.HomeAutomation.ZWave);
                        if (zwaveController != null)
                        {
                            zwaveController.DiscoveryEvent -= zwaveController_DiscoveryEvent;
                        }
                        zwaveInterface.SetPortName(systemConfiguration.GetInterfaceOption(Domains.HomeAutomation_ZWave, "Port").Value.Replace("|", "/"));
                        zwaveInterface.Connect();
                        zwaveController = zwaveInterface.ZWaveController;
                        if (zwaveController != null)
                        {
                            zwaveController.DiscoveryEvent += zwaveController_DiscoveryEvent;
                        }
//                    LoadModules();
//                        RefreshModules(domain, true);
                        break;
                    case Domains.HomeAutomation_X10:
                        var x10Interface = (GetInterface(Domains.HomeAutomation_X10) as MIG.Interfaces.HomeAutomation.X10);
                        x10Interface.SetPortName(systemConfiguration.GetInterfaceOption(Domains.HomeAutomation_X10, "Port").Value.Replace("|", "/"));
                        x10Interface.SetHouseCodes(systemConfiguration.GetInterfaceOption(Domains.HomeAutomation_X10, "HouseCodes").Value);
                        x10Interface.Connect();
                        x10Controller = x10Interface.X10Controller;
//                    LoadModules();
                        RefreshModules(domain, true);
                        break;
                    case Domains.HomeAutomation_W800RF:
                        var w800rfInterface = (GetInterface(Domains.HomeAutomation_W800RF) as MIG.Interfaces.HomeAutomation.W800RF);
                        w800rfInterface.SetPortName(systemConfiguration.GetInterfaceOption(Domains.HomeAutomation_W800RF, "Port").Value);
                        w800rfInterface.Connect();
//                    LoadModules();
                        RefreshModules(domain, true);
                        break;
                    case Domains.EmbeddedSystems_Weeco4mGPIO:
                        var weeco4mInterface = (GetInterface(Domains.EmbeddedSystems_Weeco4mGPIO) as MIG.Interfaces.EmbeddedSystems.Weeco4mGPIO);
                        weeco4mInterface.SetInputPin(uint.Parse(systemConfiguration.GetInterfaceOption(Domains.EmbeddedSystems_Weeco4mGPIO, "InputPin").Value));
                        weeco4mInterface.SetPulsePerWatt(double.Parse(systemConfiguration.GetInterfaceOption(Domains.EmbeddedSystems_Weeco4mGPIO, "PulsePerWatt").Value));
                        weeco4mInterface.Connect();
                        RefreshModules(domain, true);
                        break;
                    default:
                        GetInterface(domain).Connect();
                        RefreshModules(domain, true);
                        break;
                }
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "InterfaceEnable()", ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
        }

        public void InterfaceDisable(string domain)
        {
            try
            {
                switch (domain)
                {
                    case Domains.HomeAutomation_ZWave:
                        GetInterface(domain).Disconnect();
                        if (zwaveController != null)
                        {
                            zwaveController.DiscoveryEvent -= zwaveController_DiscoveryEvent;
                        }
                        zwaveController = null;
                        break;
                    case Domains.HomeAutomation_X10:
                        GetInterface(domain).Disconnect();
                        x10Controller = null;
                        break;
                    default:
                        GetInterface(domain).Disconnect();
                        break;
                }
                RefreshModules(domain, true);
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "InterfaceDisable()", ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
        }

        public void RegisterDynamicApi(string apiCall, Func<object, object> handler)
        {
            MIG.Interfaces.DynamicInterfaceAPI.Register(apiCall, handler);
        }

        public void UnRegisterDynamicApi(string apiCall)
        {
            MIG.Interfaces.DynamicInterfaceAPI.UnRegister(apiCall);
        }
        // called after ProgramCommand executed, should pause thread. Mostly unimplemented
        public void WaitOnPending(string domain)
        {
            MIGInterface migInterface = GetInterface(domain);
            if (migInterface != null)
            {
                migInterface.WaitOnPending();
            }
            //Thread.Sleep(50);
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
                HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "GetJsonSerializedModules()", ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
            //
            return jsonModules;
        }

        public void RefreshModules(string domain, bool sort = false)
        {
            switch (domain)
            {
                case Domains.HomeAutomation_ZWave:
                    modules_RefreshZwave();
                    break;
                case Domains.HomeAutomation_X10:
                    modules_RefreshX10();
                    break;
                default:
                    modules_RefreshMisc();
                    break;
            }
            //
            if (sort)
            {
                modules_Sort();
            }
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
                    if (/*module.Type == Module.Types.MultiLevelSwitch ||*/
                        module.DeviceType == Module.DeviceTypes.Light || module.DeviceType == Module.DeviceTypes.Dimmer)
                    {
                        Service.Utility.ModuleParameterGet(module, ModuleParameters.MODPAR_STATUS_LEVEL).Value = levelValue;
                        try
                        {
                            MIGInterfaceCommand icmd = new MIGInterfaceCommand(module.Domain + "/" + module.Address + "/" + commandValue);
                            InterfaceControl(icmd);
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
                if (module.RoutingNode != "")
                {
                    module.RoutingNode = "";
                }
                // we found associated module in HomeGenie.Modules

                #region z-wave specific stuff

                if (propertyChangedAction.SourceType == "ZWave Node")
                {
                    if (propertyChangedAction.Path == Properties.ZWAVENODE_MANUFACTURERSPECIFIC)
                    {
                        ManufacturerSpecific zwavemanufacturerspecs = (ManufacturerSpecific)propertyChangedAction.Value;
                        propertyChangedAction.Value = zwavemanufacturerspecs.ManufacturerId + ":" + zwavemanufacturerspecs.TypeId + ":" + zwavemanufacturerspecs.ProductId;
                        //TODO: deprecate the following line
                        UpdateZWaveNodeDeviceHandler(byte.Parse(propertyChangedAction.SourceId), module);
                    }
                }

                #endregion z-wave specific stuff

                SignalModulePropertyChange(migService, module, propertyChangedAction);

            }
            else
            {
                // There is no source module in Modules for this event.
                modules_RefreshMisc();
            }
        }
        // Check if command was Control.*, update the ModuleParameter. This should happen in a HWInt->HomeGenie pathway
        private void migService_ServiceRequestPostProcess(MIGClientRequest request, MIGInterfaceCommand command)
        {
            if (command.Domain == Domains.HomeAutomation_X10 || command.Domain == Domains.HomeAutomation_ZWave)
            {
                Module module = null;
                try
                {
                    module = Modules.Find(o => o.Domain == command.Domain && o.Address == command.NodeId);
                } catch { }
                //
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
                            command.Response = Utility.WaitModuleParameterChange(module, Properties.ZWAVENODE_WAKEUPINTERVAL);
                            command.Response = JsonHelper.GetSimpleResponse(command.Response);
                        }
                        else if (command.Command == ZWave.Command.BATTERY_GET)
                        {
                            command.Response = Utility.WaitModuleParameterChange(module, Properties.ZWAVENODE_BATTERY);
                            command.Response = JsonHelper.GetSimpleResponse(command.Response);
                        }
                        else if (command.Command == ZWave.Command.MULTIINSTANCE_GET)
                        {
                            command.Response = Utility.WaitModuleParameterChange(module, Properties.ZWAVENODE_MULTIINSTANCE + "." + command.GetOption(0).Replace(".", "") + "." + command.GetOption(1));
                            command.Response = JsonHelper.GetSimpleResponse(command.Response);
                        }
                        else if (command.Command == ZWave.Command.ASSOCIATION_GET)
                        {
                            command.Response = Utility.WaitModuleParameterChange(module, Properties.ZWAVENODE_ASSOCIATIONS + "." + command.GetOption(0));
                            command.Response = JsonHelper.GetSimpleResponse(command.Response);
                        }
                        else if (command.Command == ZWave.Command.CONFIG_PARAMETERGET)
                        {
                            command.Response = Utility.WaitModuleParameterChange(module, Properties.ZWAVENODE_CONFIGVARIABLES + "." + command.GetOption(0));
                            command.Response = JsonHelper.GetSimpleResponse(command.Response);
                        }
                        else if (command.Command == ZWave.Command.NODEINFO_GET)
                        {
                            command.Response = Utility.WaitModuleParameterChange(module, Properties.ZWAVENODE_NODEINFO);
                            command.Response = JsonHelper.GetSimpleResponse(command.Response);
                        }
                        else if (command.Command == ZWave.Command.MANUFACTURERSPECIFIC_GET)
                        {
                            command.Response = Utility.WaitModuleParameterChange(module, Properties.ZWAVENODE_MANUFACTURERSPECIFIC);
                            command.Response = JsonHelper.GetSimpleResponse(command.Response);
                        }
                    }
                }
            }
            //
            // Macro Recording
            //
            if (masterControlProgram != null && masterControlProgram.MacroRecorder.IsRecordingEnabled && command != null && command.Command != null && command.Command.StartsWith("Control."))
            {
                masterControlProgram.MacroRecorder.AddCommand(command);
            }
        }
        // execute the requested command (from web service)
        private void migService_ServiceRequestPreProcess(MIGClientRequest request, MIGInterfaceCommand migCommand)
        {
            LogBroadcastEvent("MIG.Gateways.WebServiceGateway", request.RequestOrigin, request.RequestMessage, request.SubjectName, request.SubjectValue);

            #region Interconnection (Remote Node Command Routing)

            Module target = systemModules.Find(m => m.Domain == migCommand.Domain && m.Address == migCommand.NodeId);
            bool isremotemodule = (target != null && target.RoutingNode.Trim() != "");
            if (isremotemodule)
            {
                // ...
                try
                {
                    string serviceurl = "http://" + target.RoutingNode + "/api/" + migCommand.Domain + "/" + migCommand.NodeId + "/" + migCommand.Command + "/" + migCommand.OptionsString;
                    Automation.Scripting.NetHelper neth = new Automation.Scripting.NetHelper(this).WebService(serviceurl);
                    if (systemConfiguration.HomeGenie.UserLogin != "" && systemConfiguration.HomeGenie.UserPassword != "")
                    {
                        neth.WithCredentials(systemConfiguration.HomeGenie.UserLogin, systemConfiguration.HomeGenie.UserPassword);
                    }
                    neth.Call();
                }
                catch (Exception ex)
                {
                    HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "Interconnection:" + target.RoutingNode, ex.Message, "Exception.StackTrace", ex.StackTrace);
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

        }

        #endregion

        #region Module/Interface Events handling and propagation

        public void SignalModulePropertyChange(object sender, Module module, InterfacePropertyChangedAction propertyChangedAction)
        {

            // update module parameter value
            ModuleParameter parameter = null;
            try
            {
                parameter = Utility.ModuleParameterGet(module, propertyChangedAction.Path);
                if (parameter == null)
                {
                    module.Properties.Add(new ModuleParameter()
                    {
                        Name = propertyChangedAction.Path,
                        Value = propertyChangedAction.Value.ToString()
                    });
                }
                else
                {
                    parameter.Value = propertyChangedAction.Value.ToString();
                }
            }
            catch (Exception ex)
            {
                //                HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "SignalModulePropertyChange(...)", ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
            //
            LogBroadcastEvent(propertyChangedAction.Domain, propertyChangedAction.SourceId, propertyChangedAction.SourceType, propertyChangedAction.Path, JsonConvert.SerializeObject(propertyChangedAction.Value));
            //
            ///// ROUTE EVENT TO LISTENING AutomationPrograms
            if (masterControlProgram != null)
            {
                RoutedEvent eventData = new RoutedEvent()
                {
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
                                if (!program.ModuleIsChangingHandler(new Automation.Scripting.ModuleHelper(this, moduleEvent.Module), moduleEvent.Parameter))
                                {
                                    proceed = false;
                                    break;
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            HomeGenieService.LogEvent(program.Domain, program.Address.ToString(), ex.Message, "Exception.StackTrace", ex.StackTrace);
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
                                    if (!program.ModuleChangedHandler(new Automation.Scripting.ModuleHelper(this, moduleEvent.Module), moduleEvent.Parameter))
                                    {
                                        break;
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {
                                HomeGenieService.LogEvent(program.Domain, program.Address.ToString(), ex.Message, "Exception.StackTrace", ex.StackTrace);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "RouteParameterChangedEvent()", e.Message, "Exception.StackTrace", e.StackTrace); 
            }
        }

        #endregion

        #region Logging

        internal void LogBroadcastEvent(string domain, string source, string description, string property, string value)
        {
            // these events are also routed to the UI
            var logEntry = new LogEntry()
            {
                Domain = domain,
                Source = source,
                Description = description,
                Property = property,
                Value = value.Replace("\"", "")
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
            var logEntry = new LogEntry()
            {
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
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, namePrefix.ToLower() + "groups.xml");
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
            lock (systemModules.LockObject)
                try
                {
                    // Due to encrypted values, we must clone modules before encrypting and saving
                    var clonedModules = (List<Module>)systemModules.Clone();
                    foreach (var module in clonedModules)
                    {
                        foreach (var parameter in module.Properties)
                        {
                            // these two properties have to be kept in clear text
                            if (parameter.Name != Properties.WIDGET_DISPLAYMODULE && parameter.Name != Properties.VIRTUALMODULE_PARENTID)
                            {
                                if (!String.IsNullOrEmpty(parameter.Value))
                                    parameter.Value = StringCipher.Encrypt(parameter.Value, systemConfiguration.GetPassPhrase());
                                if (!String.IsNullOrEmpty(parameter.LastValue))
                                    parameter.LastValue = StringCipher.Encrypt(parameter.LastValue, systemConfiguration.GetPassPhrase());
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
                    var serializer = new System.Xml.Serialization.XmlSerializer(systemModules.GetType());
                    var writer = System.Xml.XmlWriter.Create(filePath, settings);
                    serializer.Serialize(writer, systemModules);
                    writer.Close();
                    success = true;
                }
                catch (Exception ex)
                {
                    HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "UpdateModulesDatabase()", ex.Message, "Exception.StackTrace", ex.StackTrace);
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
            // load last saved modules data into modules list
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
                var reader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "automationgroups.xml"));
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
                    //program.IsEvaluatingConditionBlock = false;
                    //program.ScriptErrors = "";
                    // backward compatibility with hg < 0.91
                    if (program.Address == 0)
                    {
                        // assign an id to program if unassigned
                        program.Address = masterControlProgram.GeneratePid();
                    }
                    // in case of c# script preload assembly from generated .dll
                    if (program.Type.ToLower() == "csharp" && !program.AssemblyLoad())
                    {
                        program.ScriptErrors = "Program update is required.";
                    }
                    //else
                    //{
                    //    program.ScriptErrors = "";
                    //}
                    masterControlProgram.ProgramAdd(program);
                }
                reader.Close();
            }
            catch
            {
                //TODO: log error
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
            // force re-generation of Modules list
            //_jsonSerializedModules(false);
            modules_RefreshAll();
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

                    if (File.Exists(target))
                        File.Delete(target);

                    using (var source = part.GetStream(FileMode.Open, FileAccess.Read))
                    using (var destination = File.OpenWrite(target))
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

        #endregion Initialization and Data Storage

        #region Misc events handlers

        // fired after configuration is written to systemconfiguration.xml
        private void systemConfiguration_OnUpdate(bool success)
        {
            modules_RefreshAll();
        }
        // fired either at startup time and after a new z-wave node has been added to the controller
        private void zwaveController_DiscoveryEvent(object source, DiscoveryEventArgs e)
        {
            switch(e.Status)
            {
                case DISCOVERY_STATUS.DISCOVERY_START:
                    LogBroadcastEvent(Domains.HomeGenie_System, "0", "HomeGenie System", Domains.HomeAutomation_ZWave, "Discovery Started");
                    break;
                case DISCOVERY_STATUS.DISCOVERY_END:
                    LogBroadcastEvent(Domains.HomeGenie_System, "0", "HomeGenie System", Domains.HomeAutomation_ZWave, "Discovery Complete");
                    modules_RefreshZwave();
                    modules_Sort();
                    break;
                case DISCOVERY_STATUS.NODE_ADDED:
                    LogBroadcastEvent(Domains.HomeGenie_System, "0", "HomeGenie System", Domains.HomeAutomation_ZWave, "Added node " + e.NodeId);
                    break;
                case DISCOVERY_STATUS.NODE_UPDATED:
                    LogBroadcastEvent(Domains.HomeGenie_System, "0", "HomeGenie System", Domains.HomeAutomation_ZWave, "Updated node " + e.NodeId);
                    break;
                case DISCOVERY_STATUS.NODE_REMOVED:
                    LogBroadcastEvent(Domains.HomeGenie_System, "0", "HomeGenie System", Domains.HomeAutomation_ZWave, "Removed node " + e.NodeId);
                    break;
            }
        }

        #endregion

        #region Internals for modules' structure update and sorting

        internal void modules_RefreshZwave()
        {
            lock (systemModules.LockObject)
                try
                {
                    //
                    // Z-Wave nodes
                    //
                    if (systemConfiguration.GetInterface(Domains.HomeAutomation_ZWave).IsEnabled && zwaveController != null)
                    {
                        foreach (var node in zwaveController.Devices)
                        {
                            if (node.NodeId == 0x01) // zwave controller id
                                continue;
                            //
                            Module module = null;
                            try
                            {
                                module = Modules.Find(delegate(Module o)
                                {
                                    return o.Domain == Domains.HomeAutomation_ZWave && o.Address == node.NodeId.ToString();
                                });
                            }
                            catch
                            {
                            }
                            // add new module
                            if (module == null)
                            {
                                module = new Module();
                                module.Domain = Domains.HomeAutomation_ZWave;
                                module.DeviceType = Module.DeviceTypes.Generic;
                                systemModules.Add(module);
                            }
                            //
                            if (module.DeviceType == Module.DeviceTypes.Generic && node.GenericClass != 0x00)
                            {
                                switch (node.GenericClass)
                                {
                                    case 0x10: // BINARY SWITCH
                                        module.Description = "ZWave Switch";
                                        module.DeviceType = Module.DeviceTypes.Switch;
                                        break;

                                    case 0x11: // MULTILEVEL SWITCH (DIMMER)
                                        module.Description = "ZWave Multilevel Switch";
                                        module.DeviceType = Module.DeviceTypes.Dimmer;
                                        break;

                                    case 0x08: // THERMOSTAT
                                        module.Description = "ZWave Thermostat";
                                        module.DeviceType = Module.DeviceTypes.Thermostat;
                                        break;

                                    case 0x20: // BINARY SENSOR
                                        module.Description = "ZWave Sensor";
                                        module.DeviceType = Module.DeviceTypes.Sensor;
                                        break;

                                    case 0x21: // MULTI-LEVEL SENSOR
                                        module.Description = "ZWave Multilevel Sensor";
                                        module.DeviceType = Module.DeviceTypes.Sensor;
                                        break;

                                    case 0x31: // METER
                                        module.Description = "ZWave Meter";
                                        module.DeviceType = Module.DeviceTypes.Sensor;
                                        break;
                                }
                            }
                            //
                            module.Address = node.NodeId.ToString();
                            if (module.Description == null || module.Description == "")
                            {
                                module.Description = "ZWave Node";
                            }
                            // 
                            UpdateZWaveNodeDeviceHandler(node.NodeId, module);
                        }
                        // remove modules if not present in the controller ad if not virtual modules ( Address containing dot '.' instance separator )
                        if (zwaveController.Devices.Count > 0)
                        {
                            systemModules.RemoveAll(m => (m.Domain == Domains.HomeAutomation_ZWave && zwaveController.Devices.FindIndex(n => n.NodeId.ToString() == m.Address) < 0 && m.Address.IndexOf('.') < 0));
                        }
                    }
                    else if (!systemConfiguration.GetInterface(Domains.HomeAutomation_ZWave).IsEnabled)
                    {
                        systemModules.RemoveAll(m => m.Domain == Domains.HomeAutomation_ZWave && m.RoutingNode == "");
                    }
                }
                catch (Exception ex)
                {
                    HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "modules_RefreshZwave()", ex.Message, "Exception.StackTrace", ex.StackTrace);
                }
        }

        internal void modules_RefreshX10()
        {
            lock (systemModules.LockObject)
                try
                {
                    //
                    // X10 Units
                    //
                    if (systemConfiguration.GetInterface(Domains.HomeAutomation_X10).IsEnabled && x10Controller != null)
                    {
                        // CM-15 RF receiver
                        // TODO: this shouldn't be created for CM-11
                        var module = systemModules.Find(delegate(Module o)
                        {
                            return o.Domain == Domains.HomeAutomation_X10 && o.Address == "RF";
                        });
                        if (module == null)
                        {
                            module = new Module()
                            {
                                Domain = Domains.HomeAutomation_X10,
                                Address = "RF",
                                DeviceType = Module.DeviceTypes.Sensor
                            };
                            systemModules.Add(module);
                        }
                        //
                        foreach (var kv in x10Controller.ModulesStatus)
                        {
                            module = new Module();
                            try
                            {
                                module = Modules.Find(delegate(Module o)
                                {
                                    return o.Domain == Domains.HomeAutomation_X10 && o.Address == kv.Value.Code;
                                });
                            }
                            catch
                            {
                            }
                            // add new module
                            if (module == null)
                            {
                                module = new Module();
                                module.Domain = Domains.HomeAutomation_X10;
                                module.DeviceType = Module.DeviceTypes.Generic;
                                systemModules.Add(module);
                            }
                            //
                            var parameter = module.Properties.Find(mpar => mpar.Name == ModuleParameters.MODPAR_STATUS_LEVEL);
                            if (parameter == null)
                            {
                                module.Properties.Add(new ModuleParameter()
                                {
                                    Name = ModuleParameters.MODPAR_STATUS_LEVEL,
                                    Value = ((double)kv.Value.Level).ToString()
                                });
                            }
                            else if (parameter.Value != ((double)kv.Value.Level).ToString())
                            {
                                parameter.Value = ((double)kv.Value.Level).ToString();
                            }
                            module.Address = kv.Value.Code;
                            if (module.Description == null || module.Description == "")
                            {
                                module.Description = "X10 Module";
                            }
                        }
                    }
                    else if (!systemConfiguration.GetInterface(Domains.HomeAutomation_X10).IsEnabled)
                    {
                        systemModules.RemoveAll(m => m.Domain == Domains.HomeAutomation_X10 && m.RoutingNode == "");
                    }
                }
                catch (Exception ex)
                {
                    HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "modules_RefreshX10()", ex.Message, "Exception.StackTrace", ex.StackTrace);
                }
        }

        internal void modules_RefreshMisc()
        {
            // TODO: create method MIGInterface.GetModules() and abstract this draft code
            string currentDomain = "";
            lock (systemModules.LockObject)
                try
                {
                    //
                    // UPnP devices
                    //
                    currentDomain = Domains.Protocols_UPnP;
                    if (systemConfiguration.GetInterface(currentDomain) != null && systemConfiguration.GetInterface(currentDomain).IsEnabled && ((MIG.Interfaces.Protocols.UPnP)migService.Interfaces[currentDomain]).IsConnected)
                    {
                        var upnpInterface = ((MIG.Interfaces.Protocols.UPnP)migService.Interfaces[currentDomain]);
                        for (int d = 0; d < upnpInterface.UpnpControlPoint.DeviceTable.Count; d++)
                        {
                            var device = (UPnPDevice)(upnpInterface.UpnpControlPoint.DeviceTable[d]);
                            Module module = null;
                            try
                            {
                                module = Modules.Find(delegate(Module o)
                                {
                                    return o.Domain == currentDomain && o.Address == device.UniqueDeviceName;
                                });
                            }
                            catch { }
                            // add new module
                            if (module == null)
                            {
                                module = new Module();
                                module.Domain = currentDomain;
                                module.DeviceType = Module.DeviceTypes.Sensor;
                                if (device.StandardDeviceType == "MediaRenderer")
                                {
                                    Utility.ModuleParameterSet(module, Properties.WIDGET_DISPLAYMODULE, "homegenie/generic/mediareceiver");
                                }
                                else if (device.StandardDeviceType == "MediaServer")
                                {
                                    Utility.ModuleParameterSet(module, Properties.WIDGET_DISPLAYMODULE, "homegenie/generic/mediaserver");
                                }
                                else if (device.StandardDeviceType == "SwitchPower")
                                {
                                    module.DeviceType = Module.DeviceTypes.Switch;
                                }
                                else if (device.StandardDeviceType == "BinaryLight")
                                {
                                    module.DeviceType = Module.DeviceTypes.Light;
                                }
                                else if (device.StandardDeviceType == "DimmableLight")
                                {
                                    module.DeviceType = Module.DeviceTypes.Dimmer;
                                }
                                else if (device.HasPresentation)
                                {
                                    Utility.ModuleParameterSet(module, Properties.WIDGET_DISPLAYMODULE, "homegenie/generic/link");
                                    Utility.ModuleParameterSet(module, "FavouritesLink.Url", device.PresentationURL);
                                }
                                Utility.ModuleParameterSet(module, "UPnP.DeviceURN", device.DeviceURN);
                                Utility.ModuleParameterSet(module, "UPnP.DeviceURN_Prefix", device.DeviceURN_Prefix);
                                Utility.ModuleParameterSet(module, "UPnP.FriendlyName", device.FriendlyName);
                                Utility.ModuleParameterSet(module, "UPnP.LocationURL", device.LocationURL);
                                Utility.ModuleParameterSet(module, "UPnP.Version", device.Major + "." + device.Minor);
                                Utility.ModuleParameterSet(module, "UPnP.ModelName", device.ModelName);
                                Utility.ModuleParameterSet(module, "UPnP.ModelNumber", device.ModelNumber);
                                Utility.ModuleParameterSet(module, "UPnP.ModelDescription", device.ModelDescription);
                                Utility.ModuleParameterSet(module, "UPnP.ModelURL", device.ModelURL.ToString());
                                Utility.ModuleParameterSet(module, "UPnP.Manufacturer", device.Manufacturer);
                                Utility.ModuleParameterSet(module, "UPnP.ManufacturerURL", device.ManufacturerURL);
                                Utility.ModuleParameterSet(module, "UPnP.PresentationURL", device.PresentationURL);
                                Utility.ModuleParameterSet(module, "UPnP.UniqueDeviceName", device.UniqueDeviceName);
                                Utility.ModuleParameterSet(module, "UPnP.SerialNumber", device.SerialNumber);
                                Utility.ModuleParameterSet(module, "UPnP.StandardDeviceType", device.StandardDeviceType);
                                systemModules.Add(module);
                            }
                            //
                            module.Address = device.UniqueDeviceName;
                            module.Description = device.FriendlyName + " (" + device.ModelName + ")";
                        }
                    }
                    else
                    {
                        systemModules.RemoveAll(m => m.Domain == currentDomain && m.RoutingNode == "");
                    }
                    //
                    // Raspberry Pi GPIO
                    //
                    currentDomain = Domains.EmbeddedSystems_RaspiGPIO;
                    if (systemConfiguration.GetInterface(currentDomain) != null && systemConfiguration.GetInterface(currentDomain).IsEnabled && ((MIG.Interfaces.EmbeddedSystems.RaspiGPIO)migService.Interfaces[currentDomain]).IsConnected)
                    {
                        foreach (var rv in ((MIG.Interfaces.EmbeddedSystems.RaspiGPIO)migService.Interfaces[currentDomain]).GpioPins)
                        {
                            Module module = null;
                            try
                            {
                                module = Modules.Find(delegate(Module o)
                                {
                                    return o.Domain == currentDomain && o.Address == rv.Key;
                                });
                            }
                            catch
                            {
                            }
                            // add new module
                            if (module == null)
                            {
                                module = new Module();
                                module.Domain = currentDomain;
                                module.DeviceType = Module.DeviceTypes.Switch;
                                systemModules.Add(module);
                            }
                            //
                            module.Address = rv.Key;
                            module.Description = "Raspberry Pi GPIO";
                            //
                            Utility.ModuleParameterSet(module, ModuleParameters.MODPAR_STATUS_LEVEL, (((bool)rv.Value) ? "1" : "0"));
                        }
                    }
                    else
                    {
                        systemModules.RemoveAll(m => m.Domain == currentDomain && m.RoutingNode == "");
                    }
                    //
                    // Weecoboard-4m GPIO
                    //
                    currentDomain = Domains.EmbeddedSystems_Weeco4mGPIO;
                    if (systemConfiguration.GetInterface(currentDomain) != null && systemConfiguration.GetInterface(currentDomain).IsEnabled && ((MIG.Interfaces.EmbeddedSystems.Weeco4mGPIO)migService.Interfaces[currentDomain]).IsConnected)
                    {
                        foreach (var rv in ((MIG.Interfaces.EmbeddedSystems.Weeco4mGPIO)migService.Interfaces[currentDomain]).RegPins)
                        {
                            Module module = null;
                            try
                            {
                                module = Modules.Find(delegate(Module o)
                                {
                                    return o.Domain == currentDomain && o.Address == rv.Key;
                                });
                            }
                            catch
                            {
                            }
                            // add new module
                            if (module == null)
                            {
                                module = new Module();
                                module.Domain = currentDomain;
                                module.Description = "Weecoboard Register";
                                module.DeviceType = Module.DeviceTypes.Sensor;
                                systemModules.Add(module);
                            }
                            //
                            module.Address = rv.Key;
                            module.Description = "Weeco-4M Register";
                            //
                            Utility.ModuleParameterSet(module, ModuleParameters.MODPAR_STATUS_LEVEL, rv.Value);
                        }

                        // digital in/out
                        foreach (var rv in ((MIG.Interfaces.EmbeddedSystems.Weeco4mGPIO)migService.Interfaces[currentDomain]).GpioPins)
                        {
                            Module module = null;
                            try
                            {
                                module = Modules.Find(delegate(Module o)
                                {
                                    return o.Domain == currentDomain && o.Address == rv.Key;
                                });
                            }
                            catch
                            {
                            }
                            // add new module
                            if (module == null)
                            {
                                module = new Module();
                                module.Domain = currentDomain;
                                module.Description = "Weecoboard GPIO";
                                module.DeviceType = Module.DeviceTypes.Switch;
                                systemModules.Add(module);
                            }
                            //
                            module.Address = rv.Key;
                            module.Description = "Weeco-4M GPIO";
                            //
                            Utility.ModuleParameterSet(module, ModuleParameters.MODPAR_STATUS_LEVEL, (((bool)rv.Value) ? "1" : "0"));
                        }
                    }
                    else
                    {
                        systemModules.RemoveAll(m => m.Domain == currentDomain && m.RoutingNode == "");
                    }
                    //
                    //
                    currentDomain = Domains.Media_CameraInput;
                    if (systemConfiguration.GetInterface(currentDomain) != null && systemConfiguration.GetInterface(currentDomain).IsEnabled && ((MIG.Interfaces.Media.CameraInput)migService.Interfaces[currentDomain]).IsConnected)
                    {
                        Module module = null;
                        try
                        {
                            module = Modules.Find(delegate(Module o)
                            {
                                return o.Domain == currentDomain && o.Address == "AV0";
                            });
                        }
                        catch
                        {
                        }
                        // add new module
                        if (module == null)
                        {
                            module = new Module();
                            module.Domain = currentDomain;
                            module.DeviceType = Module.DeviceTypes.Sensor;
                            Utility.ModuleParameterSet(module, Properties.WIDGET_DISPLAYMODULE, "homegenie/generic/camerainput");
                            systemModules.Add(module);
                        }
                        //
                        module.Address = "AV0";
                        module.Description = "Video 4 Linux Video Input";
                    }
                    else
                    {
                        systemModules.RemoveAll(m => m.Domain == currentDomain && m.RoutingNode == "");
                    }
                    //
                    //
                    currentDomain = Domains.HomeAutomation_W800RF;
                    if (systemConfiguration.GetInterface(currentDomain) != null && systemConfiguration.GetInterface(currentDomain).IsEnabled)
                    {
                        var w800rf = systemModules.Find(delegate(Module o)
                        {
                            return o.Domain == Domains.HomeAutomation_W800RF && o.Address == "RF";
                        });
                        if (w800rf == null)
                        {
                            w800rf = new Module()
                            {
                                Domain = Domains.HomeAutomation_W800RF,
                                Address = "RF",
                                DeviceType = Module.DeviceTypes.Sensor
                            };
                            systemModules.Add(w800rf);
                        }
                    }
                    else
                    {
                        systemModules.RemoveAll(m => m.Domain == currentDomain && m.RoutingNode == "");
                    }
                    //
                    //
                    currentDomain = Domains.Controllers_LircRemote;
                    if (systemConfiguration.GetInterface(currentDomain) != null && systemConfiguration.GetInterface(currentDomain).IsEnabled)
                    {
                        var lirc = systemModules.Find(delegate(Module o)
                        {
                            return o.Domain == Domains.Controllers_LircRemote && o.Address == "IR";
                        });
                        if (lirc == null)
                        {
                            lirc = new Module()
                            {
                                Domain = Domains.Controllers_LircRemote,
                                Address = "IR",
                                DeviceType = Module.DeviceTypes.Sensor
                            };
                            systemModules.Add(lirc);
                        }
                    }
                    else
                    {
                        systemModules.RemoveAll(m => m.Domain == currentDomain && m.RoutingNode == "");
                    }

                }
                catch (Exception ex)
                {
                    HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "modules_RefreshMisc()", ex.Message, "Exception.StackTrace", ex.StackTrace);
                }
        }

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
                            var virtualModuleWidget = Utility.ModuleParameterGet(virtualModule, Properties.WIDGET_DISPLAYMODULE);
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
                            if (module.DeviceType == Module.DeviceTypes.Generic)
                            {
                                module.DeviceType = virtualModule.DeviceType;
                            }
                            // associated module's name of an automation program cannot be changed
                            if (module.Name == "" || module.DeviceType == Module.DeviceTypes.Program)
                            {
                                module.Name = virtualModule.Name;
                            }
                            module.Description = virtualModule.Description;
                            //
                            Utility.ModuleParameterSet(module, Properties.VIRTUALMODULE_PARENTID, virtualModule.ParentId);
                            var moduleWidget = Utility.ModuleParameterGet(module, Properties.WIDGET_DISPLAYMODULE);
                            // if a widget is specified on virtual module then we force module to display using this
                            if ((virtualModuleWidget != null && (virtualModuleWidget.Value != "" || moduleWidget == null)) && (moduleWidget == null || (moduleWidget.Value != virtualModuleWidget.Value)))
                            {
                                Utility.ModuleParameterSet(module, Properties.WIDGET_DISPLAYMODULE, virtualModuleWidget.Value);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "modules_RefreshVirtualModules()", ex.Message, "Exception.StackTrace", ex.StackTrace);
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
                                    Utility.ModuleParameterSet(module, Properties.WIDGET_DISPLAYMODULE, "homegenie/generic/program");
                                }
                                systemModules.Add(module);
                            }
                            //
                            module.Address = program.Address.ToString();
                            module.DeviceType = Module.DeviceTypes.Program;
                            module.Name = program.Name;
                            //module.Description = "Wizard Script";
                        }
                    }
                }
                catch (Exception ex)
                {
                    HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "modules_RefreshPrograms()", ex.Message, "Exception.StackTrace", ex.StackTrace);
                }
        }

        internal void modules_Sort()
        {
            lock (systemModules.LockObject)
                try
                {

                    // sort modules properties by name
                    foreach (var module in systemModules)
                    {
                        // various normalization stuff
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
                        string l1 = "";
                        int n1 = 0;
                        if (m1.Domain.EndsWith(".X10"))
                        {
                            l1 = m1.Address.Substring(0, 1);
                            int.TryParse(m1.Address.Substring(1), out n1);
                        }
                        else
                        {
                            int.TryParse(m1.Address, out n1);
                        }
                        string l2 = "";
                        int n2 = 0;
                        if (m2.Domain.EndsWith(".X10"))
                        {
                            l2 = m2.Address.Substring(0, 1);
                            int.TryParse(m2.Address.Substring(1), out n2);
                        }
                        else
                        {
                            int.TryParse(m2.Address, out n2);
                        }
                        string d1 = m1.Domain;
                        if (d1.StartsWith("EmbeddedSystems."))
                        {
                            d1 = "z|" + d1;
                        }
                        string d2 = m2.Domain;
                        if (d2.StartsWith("EmbeddedSystems."))
                        {
                            d2 = "z|" + d2;
                        }
                        return ((d1 + "|" + l1 + n1.ToString("00000")).CompareTo(d2 + "|" + l2 + n2.ToString("00000")));
                    });

                }
                catch (Exception ex)
                {
                    HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "modules_Sort()", ex.Message, "Exception.StackTrace", ex.StackTrace);
                }
        }

        internal void modules_RefreshAll()
        {
            systemModules.RemoveAll(m => m == null); // dunno why but sometimes it happen to have null entries causing exceptions
            modules_RefreshZwave();
            modules_RefreshX10();
            modules_RefreshMisc();
            modules_RefreshPrograms();
            modules_RefreshVirtualModules();
            modules_Sort();
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
                        if (!String.IsNullOrEmpty(parameter.Value))
                            parameter.Value = StringCipher.Decrypt(parameter.Value, systemConfiguration.GetPassPhrase());
                        if (!String.IsNullOrEmpty(parameter.LastValue))
                            parameter.LastValue = StringCipher.Decrypt(parameter.LastValue, systemConfiguration.GetPassPhrase());
                    }
                    catch
                    {
                    }
                }
                //
                reader.Close();
                //
                // add MIG interfaces
                //
                foreach (MIGServiceConfiguration.Interface iface in systemConfiguration.MIGService.Interfaces)
                {
                    var migInterface = GetInterface(iface.Domain);
                    if (migInterface == null)
                    {
                        migService.AddInterface(iface.Domain);
                    }
                }
                //
                // initialize MIG interfaces
                //
                foreach (var iface in systemConfiguration.MIGService.Interfaces)
                {
                    if (iface.IsEnabled)
                    {
                        InterfaceEnable(iface.Domain);
                    }
                    else
                    {
                        InterfaceDisable(iface.Domain);
                    }
                }
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "LoadSystemConfig()", ex.Message, "Exception.StackTrace", ex.StackTrace);
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
                    foreach (var parameter in module.Properties)
                    {
                        try
                        {
                            if (!String.IsNullOrEmpty(parameter.Value))
                                parameter.Value = StringCipher.Decrypt(parameter.Value, systemConfiguration.GetPassPhrase());
                            if (!String.IsNullOrEmpty(parameter.LastValue))
                                parameter.LastValue = StringCipher.Decrypt(parameter.LastValue, systemConfiguration.GetPassPhrase());
                        }
                        catch
                        {
                        }
                    }
                }
                //
                reader.Close();
                //
                systemModules.Clear();
                systemModules = modules;
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "LoadModules()", ex.Message, "Exception.StackTrace", ex.StackTrace);
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
                        //parameter.UpdateTime = DateTime.Now;
                        //parameter.LastValue = "0";
                        //parameter.LastUpdateTime = DateTime.Now;
                    }
                }
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "LoadModules()", ex.Message, "Exception.StackTrace", ex.StackTrace);
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
                if (File.Exists("programs/" + program.Address + ".dll"))
                {
                    Utility.AddFileToZip(archiveName, "programs/" + program.Address + ".dll");
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
            string friendlyName = "HomeGenie: " + Environment.MachineName;
            string manufacturer = "G-Labs";
            string manufacturerUrl = "http://generoso.info/";
            string modelName = "HomeGenie";
            string modelDescription = "HomeGenie Home Automation Server";
            string modelURL = "http://homegenie.it/";
            string modelNumber = "HG-1";
            string standardDeviceType = "HomeAutomationServer";
            string uniqueDeviceName = systemConfiguration.HomeGenie.GUID;
            if (String.IsNullOrEmpty(uniqueDeviceName))
            {
                systemConfiguration.HomeGenie.GUID = uniqueDeviceName = System.Guid.NewGuid().ToString();
                systemConfiguration.Update();
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

        private void UpdateZWaveNodeDeviceHandler(byte nodeId, Module module)
        {
            var handler = Utility.ModuleParameterGet(module, Properties.ZWAVENODE_DEVICEHANDLER);
            var node = zwaveController.Devices.Find(zn => zn.NodeId == nodeId);
            if (node != null && node.DeviceHandler != null && (handler == null || handler.Value.Contains(".Generic.")))
            {
                Utility.ModuleParameterSet(module, Properties.ZWAVENODE_DEVICEHANDLER, node.DeviceHandler.GetType().FullName);
            }
            else if (handler != null && !handler.Value.Contains(".Generic."))
            {
                // set to last known handler
                node.SetDeviceHandlerFromName(handler.Value);
            }
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
                    if (l.Trim() == "")
                        break;
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
