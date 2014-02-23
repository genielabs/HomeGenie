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

namespace HomeGenie.Service
{

    public class HomeGenieService
    {


        #region Private Fields declaration

        private MIGService _migservice;
        private ProgramEngine _mastercontrolprogram;
        private VirtualMeter _virtualmeter;
        private UpdateChecker _updatechecker;
        private StatisticsLogger _statisticslogger;

        // Internal data structures
        private TsList<Module> _modules = new HomeGenie.Service.TsList<Module>();
        private TsList<VirtualModule> _virtualmodules = new TsList<VirtualModule>();
        private List<Group> _automationgroups = new List<Group>();
        private List<Group> _controlgroups = new List<Group>();
        //
        private TsList<LogEntry> _recenteventslog;
        //
        private SystemConfiguration _systemconfiguration;
        //
        // Reference to Z-Wave and X10 drivers (obtained from MIGService)
        private ZWaveLib.Devices.Controller _zwavecontroller;
        private XTenLib.XTenManager _x10controller;

        // public events
        public event Action<LogEntry> LogEventAction;

        #endregion


        #region Web Service Handlers declaration

        private Handlers.Config _wsh_config;
        private Handlers.Automation _wsh_automation;
        private Handlers.Interconnection _wsh_interconnection;
        private Handlers.Statistics _wsh_statistics;
        private Handlers.Logging _wsh_logging;

        #endregion


        #region Lifecycle

        public HomeGenieService()
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
			//
            // initialize recent log list
            _recenteventslog = new TsList<LogEntry>();

            #region MIG Service initialization and startup

            //
            // initialize MIGService, interfaces (hw controllers drivers), webservice
            _migservice = new MIG.MIGService();
            _migservice.InterfacePropertyChanged += new Action<InterfacePropertyChangedAction>(migservice_InterfacePropertyChanged);
            _migservice.ServiceRequestPreProcess += new MIGService.WebServiceRequestPreProcessEventHandler(migservice_ServiceRequestPreProcess);
            _migservice.ServiceRequestPostProcess += new MIGService.WebServiceRequestPostProcessEventHandler(migservice_ServiceRequestPostProcess);
            //
            //
            // load system configuration
            _systemconfiguration = new SystemConfiguration();
            _systemconfiguration.HomeGenie.ServicePort = 8080;
            _systemconfiguration.OnUpdate += _systemconfiguration_OnUpdate;
            _loadsystemconfig();
            //
            // setup web service handlers
            //
            _wsh_config = new Handlers.Config(this);
            _wsh_automation = new Handlers.Automation(this);
            _wsh_interconnection = new Handlers.Interconnection(this);
            _wsh_statistics = new Handlers.Statistics(this);
            _wsh_logging = new Handlers.Logging(this);
            //
            // Try to start WebGateway, at  0 < (port - ServicePort) < 10
            bool servicestarted = false;
            int port = _systemconfiguration.HomeGenie.ServicePort;
            while (!servicestarted && port <= _systemconfiguration.HomeGenie.ServicePort + 10)
            {
                // TODO: this should be done like this _services.Gateways["WebService"].Configure(....)
                _migservice.ConfigureWebGateway(port, 443, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "html"), "/hg/html", _systemconfiguration.HomeGenie.UserPassword);
                if (_migservice.StartService())
                {
                    _systemconfiguration.HomeGenie.ServicePort = port;
                    servicestarted = true;
                }
                else
                {
                    port++;
                }
            }

            #endregion MIG Service initialization and startup

            //
            // If we successfully bound to port, then initialize the database.
            if (servicestarted)
            {
                LogBroadcastEvent(Domains.HomeAutomation_HomeGenie, "SystemInfo", "HomeGenie service ready", "HTTP.PORT", port.ToString());
                _systemconfiguration.HomeGenie.ServicePort = port;
                _initializesystem();
            }
            else
            {
                LogBroadcastEvent(Domains.HomeAutomation_HomeGenie, "SystemInfo", "Http port bind failed.", "HTTP.PORT", port.ToString());
            }
            
            _updatechecker = new UpdateChecker();
            _updatechecker.ArchiveDownloadUpdate += delegate(object sender, ArchiveDownloadEventArgs args)
            {
                LogBroadcastEvent(Domains.HomeGenie_UpdateChecker, "0", "HomeGenie Update Checker", "Download File", args.ReleaseInfo.DownloadUrl + " " + args.Status);
            };
            _updatechecker.UpdateProgress += delegate(object sender, UpdateProgressEventArgs args)
            {
                LogBroadcastEvent(Domains.HomeGenie_UpdateChecker, "0", "HomeGenie Update Checker", "Update Check", args.Status.ToString());
            };
            _updatechecker.Start(); // it will check every 24 hours
            //
            _statisticslogger = new StatisticsLogger(this);
            _statisticslogger.Start();
            //
            LogBroadcastEvent(Domains.HomeGenie_System, "0", "HomeGenie System", "HomeGenie", "STARTED");
        }

        public void Start()
        {
        }

        public void Stop()
        {
            LogBroadcastEvent(Domains.HomeGenie_System, "0", "HomeGenie System", "HomeGenie", "STOPPING");
            //
            UpdateModulesDatabase(); // update last received parameters before quitting
            _systemconfiguration.Update();
            //
            _mastercontrolprogram.StopEngine();
            _virtualmeter.Stop();
            _migservice.StopService();
            //
            SystemLogger.Instance.Dispose();
        }

        #endregion


        #region Data Wrappers - Public Members

        // Control groups (i.e. rooms, Outside, Housewide)
        public List<Group> Groups { get { return _controlgroups; } }

        // Automation groups 
        public List<Group> AutomationGroups { get { return _automationgroups; } }

        // MIG interfaces
        public Dictionary<string, MIGInterface> Interfaces { get { return _migservice.Interfaces; } }

        // Modules
        public TsList<Module> Modules { get { return _modules; } }

        // Virtual modules
        public TsList<VirtualModule> VirtualModules { get { return _virtualmodules; } }

        // HomeGenie system parameters
        public List<ModuleParameter> Parameters { get { return _systemconfiguration.HomeGenie.Settings; } }

        // Reference to SystemConfiguration
        public SystemConfiguration SystemConfiguration
        {
            get { return _systemconfiguration; }
        }

        // Reference to MigService
        public MIGService MigService
        {
            get { return _migservice; }
        }

        // Reference to ProgramEngine
        public ProgramEngine ProgramEngine
        {
            get { return _mastercontrolprogram; }
        }

        // Reference to UpdateChecked
        public UpdateChecker UpdateChecker
        {
            get { return _updatechecker;  }
        }

        // Reference to Recent Events Log
        public TsList<LogEntry> RecentEventsLog
        {
            get { return _recenteventslog; }
        }

        // Reference to Statistics
        public StatisticsLogger Statistics
        {
            get { return _statisticslogger; }
        }

        // Public utility methods

        public int GetHttpServicePort()
        {
            return _systemconfiguration.HomeGenie.ServicePort;
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
            Module target = _modules.Find(m => m.Domain == cmd.domain && m.Address == cmd.nodeid);
            bool isremotemodule = (target != null && target.RoutingNode.Trim() != "");
            if (isremotemodule)
            {
                // ...
                try
                {
                    string serviceurl = "http://" + target.RoutingNode + "/api/" + cmd.domain + "/" + cmd.nodeid + "/" + cmd.command + "/" + cmd.OptionsString;
                    Automation.Scripting.NetHelper neth = new Automation.Scripting.NetHelper(this).WebService(serviceurl);
                    if (_systemconfiguration.HomeGenie.UserLogin != "" && _systemconfiguration.HomeGenie.UserPassword != "")
                    {
                        neth.WithCredentials(_systemconfiguration.HomeGenie.UserLogin, _systemconfiguration.HomeGenie.UserPassword);
                    }
                    neth.Call();
                }
                catch (Exception ex)
                {
                    HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "Interconnection:" + target.RoutingNode, ex.Message, "Exception.StackTrace", ex.StackTrace);
                }
                return;
            }
            //
            object response = null;
            MIGInterface mif = GetInterface(cmd.domain);
            if (mif != null)
            {
                try
                {
                    response = mif.InterfaceControl(cmd);
                }
                catch (Exception ex)
                {
                    HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "InterfaceControl", ex.Message, "Exception.StackTrace", ex.StackTrace);
                }
            }
            //
            if (response == null || response.Equals(""))
            {
                _migservice.WebServiceDynamicApiCall(cmd);
            }
            //
            migservice_ServiceRequestPostProcess(null, cmd);
        }

        public void InterfaceEnable(string domain)
        {
            switch (domain)
            {
                case Domains.HomeAutomation_ZWave:
                    if (_zwavecontroller != null) _zwavecontroller.DiscoveryEvent -= _zwavecontroller_DiscoveryEvent;
                    _zwavecontroller = (GetInterface(domain) as MIG.Interfaces.HomeAutomation.ZWave).ZWaveController;
                    if (_zwavecontroller != null) _zwavecontroller.DiscoveryEvent += _zwavecontroller_DiscoveryEvent;
                    GetInterface(domain).Connect();
                    _loadmodules();
                    RefreshModules(domain, true);
                    break;
                case Domains.HomeAutomation_X10:
                    GetInterface(domain).Connect();
                    _x10controller = (GetInterface(domain) as MIG.Interfaces.HomeAutomation.X10).X10Controller;
                    _loadmodules();
                    RefreshModules(domain, true);
                    break;
                case Domains.Protocols_UPnP:
                    GetInterface(domain).Connect();
                    _setupUPnP();
                    // rebuild UPnP modules list
                    _modules_refresh_misc();
                    _modules_sort();
                    break;
            }
        }

        public void InterfaceDisable(string domain)
        {
            switch (domain)
            {
                case Domains.HomeAutomation_ZWave:
                    GetInterface(domain).Disconnect();
                    _zwavecontroller = null;
                    break;
                case Domains.HomeAutomation_X10:
                    GetInterface(domain).Disconnect();
                    _x10controller = null;
                    break;
                case Domains.Protocols_UPnP:
                    GetInterface(domain).Disconnect();
                    break;
            }
        }

        public void RegisterDynamicApi(string apicall, Func<object, object> handler)
        {
            MIG.Interfaces.DynamicInterfaceAPI.Register(apicall, handler);
        }

        public void UnRegisterDynamicApi(string apicall)
        {
            MIG.Interfaces.DynamicInterfaceAPI.UnRegister(apicall);
        }

        // called after ProgramCommand executed, should pause thread. Mostly unimplemented
        public void WaitOnPending(string domain)
        {
            MIGInterface mif = GetInterface(domain);
            if (mif != null)
            {
                mif.WaitOnPending();
            }
            //Thread.Sleep(50);
        }

        public List<Group> GetGroups(string nameprefix)
        {
            List<Group> grp = null;
            if (nameprefix.ToLower() == "automation")
            {
                grp = _automationgroups;
            }
            else
            {
                grp = _controlgroups;
            }
            return grp;
        }
        
        public string GetJsonSerializedModules(bool hideprops)
        {
            string jsonmodules = "";
            try
            {
                jsonmodules = "[";
                for (int m = 0; m < _modules.Count; m++)// Module m in Modules)
                {
                    jsonmodules += Utility.Module2Json(_modules[m], hideprops) + ",\n";
                    //System.Threading.Thread.Sleep(1);
                }
                jsonmodules = jsonmodules.TrimEnd(',', '\n');
                jsonmodules += "]";
                // old code for generate json, it was too much cpu time consuming on ARM
                //jsonmodules = JsonConvert.SerializeObject(Modules, Formatting.Indented);
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "_jsonSerializedModules()", ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
            //
            return jsonmodules;
        }

        public void RefreshModules(string domain, bool sort = false)
        {
            switch (domain)
            {
                case Domains.HomeAutomation_ZWave:
                    _modules_refresh_zwave();
                    break;
                case Domains.HomeAutomation_X10:
                    _modules_refresh_x10();
                    break;
            }
            //
            if (sort)
            {
                _modules_sort();
            }
        }

        public bool ExecuteAutomationRequest(MIGInterfaceCommand cmd)
        {
            bool handled = false; //never assigned
            string levelValue, cmdValue;
            // check for certain commands
            if (cmd.command == Commands.Groups.GROUPS_LIGHTSOFF)
            {
                levelValue = "0";
                cmdValue = Commands.Control.CONTROL_OFF;
            }
            else if (cmd.command == Commands.Groups.GROUPS_LIGHTSON)
            {
                levelValue = "1";
                cmdValue = Commands.Control.CONTROL_ON;
            }
            else
            {
                return handled;
            }
            //loop, turning off lights
            try
            {
                Group theGroup = Groups.Find(z => z.Name == cmd.GetOption(0));
                for (int m = 0; m < theGroup.Modules.Count; m++)
                {
                    Module module = Modules.Find(mod => mod.Domain == theGroup.Modules[m].Domain && mod.Address == theGroup.Modules[m].Address);
                    if (/*module.Type == Module.Types.MultiLevelSwitch ||*/
                        module.DeviceType == Module.DeviceTypes.Light || module.DeviceType == Module.DeviceTypes.Dimmer)
                    {
                        Service.Utility.ModuleParameterGet(module, ModuleParameters.MODPAR_STATUS_LEVEL).Value = levelValue;
                        try
                        {
                            MIGInterfaceCommand icmd = new MIGInterfaceCommand(module.Domain + "/" + module.Address + "/" + cmdValue);
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
        internal void migservice_InterfacePropertyChanged(InterfacePropertyChangedAction propertychangedaction)
        {
            // look for module associated to this event
            Module mod = null;
            try
            {
                mod = Modules.Find(delegate(Module o)
                {
                    return o.Domain == propertychangedaction.Domain && o.Address == propertychangedaction.SourceId;
                });
            }
            catch
            {
            }
            //
            if (mod != null && propertychangedaction.Path != "")
            {
                // clear RoutingNode property since the event was locally generated
                if (mod.RoutingNode != "")
                {
                    mod.RoutingNode = "";
                }
                // we found associated module in HomeGenie.Modules

                #region z-wave specific stuff

                if (propertychangedaction.SourceType == "ZWave Node")
                {
                    if (propertychangedaction.Path == Properties.ZWAVENODE_MANUFACTURERSPECIFIC)
                    {
                        ManufacturerSpecific zwavemanufacturerspecs = (ManufacturerSpecific)propertychangedaction.Value;
                        propertychangedaction.Value = zwavemanufacturerspecs.ManufacturerId + ":" + zwavemanufacturerspecs.TypeId + ":" + zwavemanufacturerspecs.ProductId;
                        //TODO: deprecate the following line
                        _updateZWaveNodeDeviceHandler(byte.Parse(propertychangedaction.SourceId), mod);
                    }
                }

                #endregion z-wave specific stuff

                SignalModulePropertyChange(_migservice, mod, propertychangedaction);

            }
            else
            {
                // There is no source module in Modules for this event.
                _modules_refresh_misc();
            }
        }

        // Check if command was Control.*, update the ModuleParameter. This should happen in a HWInt->HomeGenie pathway
        private void migservice_ServiceRequestPostProcess(MIGClientRequest request, MIGInterfaceCommand cmd)
        {
            if (cmd.domain == Domains.HomeAutomation_X10 || cmd.domain == Domains.HomeAutomation_ZWave)
            {
                Module mod = null;
                try
                {
                    mod = Modules.Find(delegate(Module o)
                    {
                        return o.Domain == cmd.domain && o.Address == cmd.nodeid;
                    });
                }
                catch
                {
                }

                if (mod != null)
                {
                    ModuleParameter p = Service.Utility.ModuleParameterGet(mod, ModuleParameters.MODPAR_STATUS_LEVEL);
                    if (p != null)
                    {
                        if (cmd.command == Commands.Control.CONTROL_ON)
                        {
                            p.Value = "1";
                        }
                        else if (cmd.command == Commands.Control.CONTROL_OFF)
                        {
                            p.Value = "0";
                        }
                        else if (cmd.command == Commands.Control.CONTROL_LEVEL)
                        {
                            p.Value = (double.Parse(cmd.GetOption(0)) / 100D).ToString();
                        }
                        else if (cmd.command == Commands.Control.CONTROL_TOGGLE)
                        {
                            double cv = 0; double.TryParse(p.Value, out cv);
                            p.Value = (cv == 0 ? "1" : "0");
                        }
                    }
                    //
                    // wait for ZWaveLib asynchronous response from node and raise the proper "parameter changed" event
                    if (cmd.domain == Domains.HomeAutomation_ZWave)  //  && (context != null && !context.Request.IsLocal)
                    {
                        if (cmd.command == ZWave.Command.CONTROLLER_NODEADD || cmd.command == ZWave.Command.CONTROLLER_NODEREMOVE)
                        {
                            _modules_refresh_zwave();
                            _modules_sort();
                        }
                        else if (cmd.command == ZWave.Command.BASIC_GET)
                        {
                            cmd.response = Utility.WaitModuleParameterChange(mod, Properties.ZWAVENODE_BASIC);
                            cmd.response = JsonHelper.GetSimpleResponse(cmd.response);
                        }
                        else if (cmd.command == ZWave.Command.WAKEUP_GET)
                        {
                            cmd.response = Utility.WaitModuleParameterChange(mod, Properties.ZWAVENODE_WAKEUPINTERVAL);
                            cmd.response = JsonHelper.GetSimpleResponse(cmd.response);
                        }
                        else if (cmd.command == ZWave.Command.BATTERY_GET)
                        {
                            cmd.response = Utility.WaitModuleParameterChange(mod, Properties.ZWAVENODE_BATTERY);
                            cmd.response = JsonHelper.GetSimpleResponse(cmd.response);
                        }
                        else if (cmd.command == ZWave.Command.MULTIINSTANCE_GET)
                        {
                            cmd.response = Utility.WaitModuleParameterChange(mod, Properties.ZWAVENODE_MULTIINSTANCE + "." + cmd.GetOption(0).Replace(".", "") + "." + cmd.GetOption(1));
                            cmd.response = JsonHelper.GetSimpleResponse(cmd.response);
                        }
                        else if (cmd.command == ZWave.Command.ASSOCIATION_GET)
                        {
                            cmd.response = Utility.WaitModuleParameterChange(mod, Properties.ZWAVENODE_ASSOCIATIONS + "." + cmd.GetOption(0));
                            cmd.response = JsonHelper.GetSimpleResponse(cmd.response);
                        }
                        else if (cmd.command == ZWave.Command.CONFIG_PARAMETERGET)
                        {
                            cmd.response = Utility.WaitModuleParameterChange(mod, Properties.ZWAVENODE_CONFIGVARIABLES + "." + cmd.GetOption(0));
                            cmd.response = JsonHelper.GetSimpleResponse(cmd.response);
                        }
                        else if (cmd.command == ZWave.Command.NODEINFO_GET)
                        {
                            cmd.response = Utility.WaitModuleParameterChange(mod, Properties.ZWAVENODE_NODEINFO);
                            cmd.response = JsonHelper.GetSimpleResponse(cmd.response);
                        }
                        else if (cmd.command == ZWave.Command.MANUFACTURERSPECIFIC_GET)
                        {
                            cmd.response = Utility.WaitModuleParameterChange(mod, Properties.ZWAVENODE_MANUFACTURERSPECIFIC);
                            cmd.response = JsonHelper.GetSimpleResponse(cmd.response);
                        }
                    }
                }
            }
            //
            // Macro Recording
            //
            if (_mastercontrolprogram != null && _mastercontrolprogram.MacroRecorder.IsRecordingEnabled && cmd != null && cmd.command != null && cmd.command.StartsWith("Control."))
            {
                _mastercontrolprogram.MacroRecorder.AddCommand(cmd);
            }
        }

        // execute the requested command (from web service)
        private void migservice_ServiceRequestPreProcess(MIGClientRequest request, MIGInterfaceCommand migcmd)
        {
            LogBroadcastEvent("MIG.Gateways.WebServiceGateway", request.RequestOrigin, request.RequestMessage, request.SubjectName, request.SubjectValue);

            #region Interconnection (Remote Node Command Routing)

            Module target = _modules.Find(m => m.Domain == migcmd.domain && m.Address == migcmd.nodeid);
            bool isremotemodule = (target != null && target.RoutingNode.Trim() != "");
            if (isremotemodule)
            {
                // ...
                try
                {
                    string serviceurl = "http://" + target.RoutingNode + "/api/" + migcmd.domain + "/" + migcmd.nodeid + "/" + migcmd.command + "/" + migcmd.OptionsString;
                    Automation.Scripting.NetHelper neth = new Automation.Scripting.NetHelper(this).WebService(serviceurl);
                    if (_systemconfiguration.HomeGenie.UserLogin != "" && _systemconfiguration.HomeGenie.UserPassword != "")
                    {
                        neth.WithCredentials(_systemconfiguration.HomeGenie.UserLogin, _systemconfiguration.HomeGenie.UserPassword);
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
            if (migcmd.domain == Domains.HomeAutomation_HomeGenie)
            {
                // domain == HomeAutomation.HomeGenie
                switch (migcmd.nodeid)
                {
                    case "Logging":

                        _wsh_logging.ProcessRequest(request, migcmd);
                        break;

                    case "Config":

                        _wsh_config.ProcessRequest(request, migcmd);
                        break;

                    case "Automation":

                        _wsh_automation.ProcessRequest(request, migcmd);
                        break;

                    case "Interconnection":

                        _wsh_interconnection.ProcessRequest(request, migcmd);
                        break;

                    case "Statistics":

                        _wsh_statistics.ProcessRequest(request, migcmd);
                        break;
                }
            }

        }

        #endregion


        #region Module/Interface Events handling and propagation

        public void SignalModulePropertyChange(object sender, Module mod, InterfacePropertyChangedAction propertychangedaction)
        {
            
            // update module parameter value
            ModuleParameter parameter = null;
            try
            {
                parameter = Utility.ModuleParameterGet(mod, propertychangedaction.Path);
                if (parameter == null)
                {
                    mod.Properties.Add(new ModuleParameter() { Name = propertychangedaction.Path, Value = propertychangedaction.Value.ToString() });
                }
                else
                {
                    parameter.Value = propertychangedaction.Value.ToString();
                }
            }
            catch (Exception ex)
            {
//                HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "SignalModulePropertyChange(...)", ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
            //
            LogBroadcastEvent(propertychangedaction.Domain, propertychangedaction.SourceId, propertychangedaction.SourceType, propertychangedaction.Path, JsonConvert.SerializeObject(propertychangedaction.Value));
            //
            RouteParameterChangedEvent(sender, mod, parameter);

        }

        public void RouteParameterChangedEvent(object sender, Module sm, ModuleParameter smp)
        {
            ///// ROUTE EVENT TO LISTENING AutomationPrograms
            if (_mastercontrolprogram != null)
            {
                Utility.RunAsyncTask(() =>
                {
                    bool proceed = true;
                    foreach (ProgramBlock pb in _mastercontrolprogram.Programs)
                    {
                        if ((sender == null || !sender.Equals(pb)))
                        {
                            try
                            {
                                if (pb.ModuleIsChangingHandler != null)
                                {
                                    if (!pb.ModuleIsChangingHandler(new Automation.Scripting.ModuleHelper(this, sm), smp))
                                    {
                                        proceed = false;
                                        break;
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {
                                HomeGenieService.LogEvent(pb.Domain, pb.Address.ToString(), ex.Message, "Exception.StackTrace", ex.StackTrace);
                            }
                        }
                    }
                    if (proceed)
                        foreach (ProgramBlock pb in _mastercontrolprogram.Programs)
                        {
                            if ((sender == null || !sender.Equals(pb)))
                            {
                                try
                                {
                                    if (pb.ModuleChangedHandler != null && smp != null) // && proceed)
                                    {
                                        if (!pb.ModuleChangedHandler(new Automation.Scripting.ModuleHelper(this, sm), smp))
                                        {
                                            break;
                                        }
                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    HomeGenieService.LogEvent(pb.Domain, pb.Address.ToString(), ex.Message, "Exception.StackTrace", ex.StackTrace);
                                }
                            }
                        }
                });
            }
        }


        #endregion


        #region Logging

        internal void LogBroadcastEvent(string domain, string source, string description, string property, string value)
        {
            // these events are also routed to the UI
            LogEntry logentry = new LogEntry() { Domain = domain, Source = source, Description = description, Property = property, Value = value.Replace("\"", "") };
            try
            {
                if (_recenteventslog.Count > 100)
                {
                    _recenteventslog.RemoveRange(0, _recenteventslog.Count - 100);
                }
                _recenteventslog.Add(logentry);
                //
                if (LogEventAction != null)
                {
                    LogEventAction(logentry);
                }
            }
            catch {
                System.Diagnostics.Debugger.Break();
            }
            //
            LogEvent(logentry);
        }

        public static void LogEvent(string domain, string source, string description, string property, string value)
        {
            LogEntry logentry = new LogEntry() { Domain = domain, Source = source, Description = description, Property = property, Value = value.Replace("\"", "") };
            LogEvent(logentry);
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

        public bool UpdateGroupsDatabase(string nameprefix)
        {
            List<Group> grp = _controlgroups;
            if (nameprefix.ToLower() == "automation")
            {
                grp = _automationgroups;
            }
            else
            {
                nameprefix = ""; // default fallback to Control Groups groups.xml - no prefix
            }
            //
            bool success = false;
            try
            {
                //lock (_dblock)
                {
                    string fname = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, nameprefix.ToLower() + "groups.xml");
                    if (File.Exists(fname))
                    {
                        File.Delete(fname);
                    }
                    System.Xml.XmlWriterSettings ws = new System.Xml.XmlWriterSettings();
                    ws.Indent = true;
                    System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(grp.GetType());
                    System.Xml.XmlWriter wri = System.Xml.XmlWriter.Create(fname, ws);
                    x.Serialize(wri, grp);
                    wri.Close();
                }
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
            try
            {
                //lock (_dblock)
                {
                    // Due to encrypted values, we must clone modules before encrypting and saving
                    List<Module> modcopy = null;
                    modcopy = (List<Module>)_modules.Clone();
                    lock (_modules.LockObject)
                    {

                        foreach (Module m in modcopy)
                        {
                            foreach (ModuleParameter p in m.Properties)
                            {
                                // these two properties have to be kept in clear text
                                if (p.Name != Properties.WIDGET_DISPLAYMODULE && p.Name != Properties.VIRTUALMODULE_PARENTID)
                                {
                                    if (!String.IsNullOrEmpty(p.Value)) p.Value = StringCipher.Encrypt(p.Value, _systemconfiguration.GetPassPhrase());
                                    if (!String.IsNullOrEmpty(p.LastValue)) p.LastValue = StringCipher.Encrypt(p.LastValue, _systemconfiguration.GetPassPhrase());
                                }
                            }
                        }


                        string fname = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "modules.xml");
                        if (File.Exists(fname))
                        {
                            File.Delete(fname);
                        }
                        System.Xml.XmlWriterSettings ws = new System.Xml.XmlWriterSettings();
                        ws.Indent = true;
                        System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(_modules.GetType());
                        System.Xml.XmlWriter wri = System.Xml.XmlWriter.Create(fname, ws);
                        x.Serialize(wri, _modules);
                        wri.Close();

                    }

                }
                success = true;
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "_updateModulesDatabase()", ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
            //
            return success;
        }

        public bool UpdateProgramsDatabase()
        {
            bool success = false;
            try
            {
                //lock (_dblock)
                {
                    string fname = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs.xml");
                    if (File.Exists(fname))
                    {
                        File.Delete(fname);
                    }
                    System.Xml.XmlWriterSettings ws = new System.Xml.XmlWriterSettings();
                    ws.Indent = true;
                    System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(_mastercontrolprogram.Programs.GetType());
                    System.Xml.XmlWriter wri = System.Xml.XmlWriter.Create(fname, ws);
                    x.Serialize(wri, _mastercontrolprogram.Programs);
                    wri.Close();
                }
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
                //lock (_dblock)
                {
                    string fname = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scheduler.xml");
                    if (File.Exists(fname))
                    {
                        File.Delete(fname);
                    }
                    System.Xml.XmlWriterSettings ws = new System.Xml.XmlWriterSettings();
                    ws.Indent = true;
                    System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(_mastercontrolprogram.SchedulerService.Items.GetType());
                    System.Xml.XmlWriter wri = System.Xml.XmlWriter.Create(fname, ws);
                    x.Serialize(wri, _mastercontrolprogram.SchedulerService.Items);
                    wri.Close();
                }
                success = true;
            }
            catch
            {
            }
            return success;
        }


        public void LoadConfiguration()
        {
            _loadsystemconfig();
            //
            // load last saved modules data into _modules list
            //
            _loadmodules();
            //
            // load last saved groups data into _controlgroups list
            try
            {
                XmlSerializer zserializer = new XmlSerializer(typeof(List<Group>));
                StreamReader zreader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "groups.xml"));
                _controlgroups = (List<Group>)zserializer.Deserialize(zreader);
                zreader.Close();
            }
            catch
            {
                //TODO: log error
            }
            //
            // load last saved automation groups data into _automationgroups list
            try
            {
                XmlSerializer zserializer = new XmlSerializer(typeof(List<Group>));
                StreamReader zreader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "automationgroups.xml"));
                _automationgroups = (List<Group>)zserializer.Deserialize(zreader);
                zreader.Close();
            }
            catch
            {
                //TODO: log error
            }
            //
            // load last saved programs data into _mastercontrolprogram.Programs list
            //
            if (_mastercontrolprogram != null)
            {
                _mastercontrolprogram.Enabled = false;
                _mastercontrolprogram.StopEngine();
                _mastercontrolprogram = null;
            }
            _mastercontrolprogram = new ProgramEngine(this);
            try
            {
                XmlSerializer pserializer = new XmlSerializer(typeof(List<ProgramBlock>));
                StreamReader preader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs.xml"));
                List<ProgramBlock> pl = (List<ProgramBlock>)pserializer.Deserialize(preader);
                foreach (ProgramBlock pb in pl)
                {
                    pb.IsRunning = false;
                    pb.IsEvaluatingConditionBlock = false;
                    pb.ScriptErrors = "";
                    // backward compatibility with hg < 0.91
                    if (pb.Address == 0)
                    {
                        // assign an id to program if unassigned
                        pb.Address = _mastercontrolprogram.GeneratePid();
                    }
                    // in case of c# script preload assembly from generated .dll
                    if (pb.Type.ToLower() == "csharp" && !pb.AssemblyLoad(this))
                    {
                        pb.ScriptErrors = "Programm not compiled (save it to compile)";
                    }
                    else
                    {
                        pb.ScriptErrors = "";
                    }
                    _mastercontrolprogram.ProgramAdd(pb);
                }
                preader.Close();
            }
            catch
            {
                //TODO: log error
            }
            //
            // load last saved scheduler items data into _mastercontrolprogram.SchedulerService.Items list
            //
            try
            {
                XmlSerializer pserializer = new XmlSerializer(typeof(List<SchedulerItem>));
                StreamReader preader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scheduler.xml"));
                List<SchedulerItem> si = (List<SchedulerItem>)pserializer.Deserialize(preader);
                _mastercontrolprogram.SchedulerService.Items.AddRange(si);
                preader.Close();
            }
            catch
            {
                //TODO: log error
            }
            // force re-generation of Modules list
            //_jsonSerializedModules(false);
            _modules_refresh_all();
            //
            _mastercontrolprogram.Enabled = true;
        }

        public void RestoreFactorySettings()
        {
            string archivename = "homegenie_factory_config.zip";
            //
            try
            {
                _mastercontrolprogram.Enabled = false;
                _mastercontrolprogram.StopEngine();
                // delete old programs assemblies
                foreach (ProgramBlock pb in _mastercontrolprogram.Programs)
                {
                    pb.ScriptAssembly = null;
                }
                _mastercontrolprogram = null;
            }
            catch { }
            //
            UnarchiveConfiguration(archivename, AppDomain.CurrentDomain.BaseDirectory);
            //
            LoadConfiguration();
            //
            // regenerate encrypted files
            UpdateModulesDatabase();
            this.SystemConfiguration.Update();
        }

        public void BackupCurrentSettings()
        {
            // regenerate encrypted files
            UpdateModulesDatabase();
            SystemConfiguration.Update();
            _archiveConfiguration("html/homegenie_backup_config.zip");
        }

        public void UnarchiveConfiguration(string archivename, string destfolder)
        {
            // Unarchive (unzip)
            using (Package package = Package.Open(archivename, FileMode.Open, FileAccess.Read))
            {
                foreach (PackagePart part in package.GetParts())
                {
                    string target = Path.Combine(destfolder, part.Uri.OriginalString.Substring(1));
                    if (!Directory.Exists(Path.GetDirectoryName(target)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                    }

                    if (File.Exists(target)) File.Delete(target);

                    using (Stream source = part.GetStream(
                        FileMode.Open, FileAccess.Read))
                    using (Stream destination = File.OpenWrite(target))
                    {
                        byte[] buffer = new byte[4096];
                        int read;
                        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            destination.Write(buffer, 0, read);
                        }
                    }
                    //                    Console.WriteLine("Deflated {0}", target);
                }
            }
        }

        #endregion Initialization and Data Storage


        #region Misc events handlers

        // fired after configuration is written to systemconfiguration.xml
        private void _systemconfiguration_OnUpdate(bool success)
        {
            _modules_refresh_all();
        }

        // fired either at startup time and after a new z-wave node has been added to the controller
        private void _zwavecontroller_DiscoveryEvent(object source, DiscoveryEventArgs e)
        {
            _modules_refresh_zwave();
            _modules_sort();
        }

        #endregion


        #region Internals for modules' structure update and sorting

        internal void _modules_refresh_zwave()
        {
            try
            {
                //
                // Z-Wave nodes
                //
                if (_systemconfiguration.GetInterface(Domains.HomeAutomation_ZWave).IsEnabled && _zwavecontroller != null)
                {
                    foreach (ZWaveNode zn in _zwavecontroller.Devices)
                    {
                        if (zn.NodeId == 0x01) // zwave controller id
                            continue;
                        //
                        Module m = null;
                        try
                        {
                            m = Modules.Find(delegate(Module o)
                            {
                                return o.Domain == Domains.HomeAutomation_ZWave && o.Address == zn.NodeId.ToString();
                            });
                        }
                        catch
                        {
                        }
                        // add new module
                        if (m == null)
                        {
                            m = new Module();
                            m.Domain = Domains.HomeAutomation_ZWave;
                            m.DeviceType = Module.DeviceTypes.Generic;
                            Modules.Add(m);
                            //
                            if (zn.DeviceHandler != null)
                            {
                                if (zn.DeviceHandler.GetType().Equals(typeof(ZWaveLib.Devices.ProductHandlers.Generic.Switch)))
                                {
                                    m.Type = Module.Types.BinarySwitch;
                                }
                                else if (zn.DeviceHandler.GetType().Equals(typeof(ZWaveLib.Devices.ProductHandlers.Generic.Dimmer)))
                                {
                                    m.Type = Module.Types.MultiLevelSwitch;
                                }
                            }
                        }
                        //
                        if (m.DeviceType == Module.DeviceTypes.Generic && zn.GenericClass != 0x00)
                        {
                            switch (zn.GenericClass)
                            {
                                case 0x10: // BINARY SWITCH
                                    m.Description = "ZWave Switch";
                                    m.DeviceType = Module.DeviceTypes.Switch;
                                    break;

                                case 0x11: // MULTILEVEL SWITCH (DIMMER)
                                    m.Description = "ZWave Multilevel Switch";
                                    m.DeviceType = Module.DeviceTypes.Dimmer;
                                    break;

                                case 0x08: // THERMOSTAT
                                    m.Description = "ZWave Thermostat";
                                    m.DeviceType = Module.DeviceTypes.Thermostat;
                                    break;

                                case 0x20: // BINARY SENSOR
                                    m.Description = "ZWave Sensor";
                                    m.DeviceType = Module.DeviceTypes.Sensor;
                                    break;

                                case 0x21: // MULTI-LEVEL SENSOR
                                    m.Description = "ZWave Multilevel Sensor";
                                    m.DeviceType = Module.DeviceTypes.Sensor;
                                    break;

                                case 0x31: // METER
                                    m.Description = "ZWave Meter";
                                    m.DeviceType = Module.DeviceTypes.Sensor;
                                    break;
                            }
                        }
                        //
                        m.Address = zn.NodeId.ToString();
                        if (m.Description == null || m.Description == "")
                        {
                            m.Description = "ZWave Node"; //_zwavecontroller.GetDevice(zn.Value.NodeId).Description;
                        }
                        // 
                        _updateZWaveNodeDeviceHandler(zn.NodeId, m);
                    }
                    // remove modules if not present in the controller ad if not virtual modules ( Address containing dot '.' instance separator )
                    if (_zwavecontroller.Devices.Count > 0)
                    {
                        Modules.RemoveAll(m => (m.Domain == Domains.HomeAutomation_ZWave && _zwavecontroller.Devices.FindIndex(n => n.NodeId.ToString() == m.Address) < 0 && m.Address.IndexOf('.') < 0));
                    }
                }
                else if (!_systemconfiguration.GetInterface(Domains.HomeAutomation_ZWave).IsEnabled)
                {
                    Modules.RemoveAll(m => m.Domain == Domains.HomeAutomation_ZWave && m.RoutingNode == "");
                }
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "_modules_refresh_zwave()", ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
        }

        internal void _modules_refresh_x10()
        {
            try
            {
                //
                // X10 Units
                //
                if (_systemconfiguration.GetInterface(Domains.HomeAutomation_X10).IsEnabled && _x10controller != null)
                {
                    foreach (KeyValuePair<string, X10Module> kv in _x10controller.ModulesStatus)
                    {
                        Module m = new Module();
                        try
                        {
                            m = Modules.Find(delegate(Module o)
                            {
                                return o.Domain == Domains.HomeAutomation_X10 && o.Address == kv.Value.Code;
                            });
                        }
                        catch
                        {
                        }
                        // add new module
                        if (m == null)
                        {
                            m = new Module();
                            m.Domain = Domains.HomeAutomation_X10;
                            m.DeviceType = Module.DeviceTypes.Generic;
                            Modules.Add(m);
                        }
                        //
                        ModuleParameter mp = m.Properties.Find(mpar => mpar.Name == ModuleParameters.MODPAR_STATUS_LEVEL);
                        if (mp == null)
                        {
                            m.Properties.Add(new ModuleParameter() { Name = ModuleParameters.MODPAR_STATUS_LEVEL, Value = ((double)kv.Value.Level).ToString() });
                        }
                        else
                        {
                            if (mp.Value != ((double)kv.Value.Level).ToString())
                            {
                                mp.Value = ((double)kv.Value.Level).ToString();
                            }
                        }
                        m.Address = kv.Value.Code;
                        if (m.Description == null || m.Description == "")
                            m.Description = "X10 Module";
                    }
                }
                else if (!_systemconfiguration.GetInterface(Domains.HomeAutomation_X10).IsEnabled)
                {
                    Modules.RemoveAll(m => m.Domain == Domains.HomeAutomation_X10 && m.RoutingNode == "");
                }
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "_modules_refresh_x10()", ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
        }

        internal void _modules_refresh_misc()
        {
            // TODO: create method MIGInterface.GetModules() and abstract this draft code
            string domain = "";
            try
            {
                //
                // UPnP devices
                //
                domain = Domains.Protocols_UPnP;
                if (_systemconfiguration.GetInterface(domain) != null && _systemconfiguration.GetInterface(domain).IsEnabled && ((MIG.Interfaces.Protocols.UPnP)_migservice.Interfaces[domain]).IsConnected)
                {
                    var upnpiface = ((MIG.Interfaces.Protocols.UPnP)_migservice.Interfaces[domain]);
                    for (int d = 0; d < upnpiface.UpnpControlPoint.DeviceTable.Count; d++)
                    {
                        UPnPDevice dev = (UPnPDevice)(upnpiface.UpnpControlPoint.DeviceTable[d]);
                        Module m = null;
                        try
                        {
                            m = Modules.Find(delegate(Module o)
                            {
                                return o.Domain == domain && o.Address == dev.UniqueDeviceName;
                            });
                        }
                        catch
                        {
                        }
                        // add new module
                        if (m == null)
                        {
                            m = new Module();
                            m.Domain = domain;
                            m.DeviceType = Module.DeviceTypes.Sensor;
                            if (dev.StandardDeviceType == "MediaRenderer")
                            {
                                Utility.ModuleParameterSet(m, Properties.WIDGET_DISPLAYMODULE, "homegenie/generic/mediareceiver");
                            }
                            else if (dev.StandardDeviceType == "MediaServer")
                            {
                                Utility.ModuleParameterSet(m, Properties.WIDGET_DISPLAYMODULE, "homegenie/generic/mediaserver");
                            }
                            else if (dev.StandardDeviceType == "SwitchPower")
                            {
                                m.DeviceType = Module.DeviceTypes.Switch;
                            }
                            else if (dev.StandardDeviceType == "BinaryLight")
                            {
                                m.DeviceType = Module.DeviceTypes.Light;
                            }
                            else if (dev.StandardDeviceType == "DimmableLight")
                            {
                                m.DeviceType = Module.DeviceTypes.Dimmer;
                            }
                            else if (dev.HasPresentation)
                            {
                                Utility.ModuleParameterSet(m, Properties.WIDGET_DISPLAYMODULE, "homegenie/generic/link");
                                Utility.ModuleParameterSet(m, "FavouritesLink.Url", dev.PresentationURL);
                            }
                            Utility.ModuleParameterSet(m, "UPnP.DeviceURN", dev.DeviceURN);
                            Utility.ModuleParameterSet(m, "UPnP.DeviceURN_Prefix", dev.DeviceURN_Prefix);
                            Utility.ModuleParameterSet(m, "UPnP.FriendlyName", dev.FriendlyName);
                            Utility.ModuleParameterSet(m, "UPnP.LocationURL", dev.LocationURL);
                            Utility.ModuleParameterSet(m, "UPnP.Version", dev.Major + "." + dev.Minor);
                            Utility.ModuleParameterSet(m, "UPnP.ModelName", dev.ModelName);
                            Utility.ModuleParameterSet(m, "UPnP.ModelNumber", dev.ModelNumber);
                            Utility.ModuleParameterSet(m, "UPnP.ModelDescription", dev.ModelDescription);
                            Utility.ModuleParameterSet(m, "UPnP.ModelURL", dev.ModelURL.ToString());
                            Utility.ModuleParameterSet(m, "UPnP.Manufacturer", dev.Manufacturer);
                            Utility.ModuleParameterSet(m, "UPnP.ManufacturerURL", dev.ManufacturerURL);
                            Utility.ModuleParameterSet(m, "UPnP.PresentationURL", dev.PresentationURL);
                            Utility.ModuleParameterSet(m, "UPnP.UniqueDeviceName", dev.UniqueDeviceName);
                            Utility.ModuleParameterSet(m, "UPnP.SerialNumber", dev.SerialNumber);
                            Utility.ModuleParameterSet(m, "UPnP.StandardDeviceType", dev.StandardDeviceType);
                            Modules.Add(m);
                        }
                        //
                        m.Address = dev.UniqueDeviceName;
                        m.Description = dev.FriendlyName + " (" + dev.ModelName + ")";
                    }
                }
                else
                {
                    Modules.RemoveAll(m => m.Domain == domain && m.RoutingNode == "");
                }
                //
                // Raspberry Pi GPIO
                //
                if (_systemconfiguration.GetInterface(Domains.EmbeddedSystems_RaspiGPIO).IsEnabled && ((MIG.Interfaces.EmbeddedSystems.RaspiGPIO)_migservice.Interfaces[Domains.EmbeddedSystems_RaspiGPIO]).IsConnected)
                {
                    foreach (KeyValuePair<string, bool> rv in ((MIG.Interfaces.EmbeddedSystems.RaspiGPIO)_migservice.Interfaces[Domains.EmbeddedSystems_RaspiGPIO]).GPIOPins)
                    {
                        Module m = null;
                        try
                        {
                            m = Modules.Find(delegate(Module o)
                            {
                                return o.Domain == Domains.EmbeddedSystems_RaspiGPIO && o.Address == rv.Key;
                            });
                        }
                        catch
                        {
                        }
                        // add new module
                        if (m == null)
                        {
                            m = new Module();
                            m.Domain = Domains.EmbeddedSystems_RaspiGPIO;
                            m.DeviceType = Module.DeviceTypes.Switch;
                            Modules.Add(m);
                        }
                        //
                        m.Address = rv.Key;
                        m.Description = "Raspberry Pi GPIO";
                        //
                        Utility.ModuleParameterSet(m, ModuleParameters.MODPAR_STATUS_LEVEL, (((bool)rv.Value) ? "1" : "0"));
                    }
                }
                else
                {
                    Modules.RemoveAll(m => m.Domain == Domains.EmbeddedSystems_RaspiGPIO && m.RoutingNode == "");
                }

                //
                if (_systemconfiguration.GetInterface(Domains.Media_CameraInput) != null && _systemconfiguration.GetInterface(Domains.Media_CameraInput).IsEnabled && ((MIG.Interfaces.Media.CameraInput)_migservice.Interfaces[Domains.Media_CameraInput]).IsConnected)
                {
                    Module m = null;
                    try
                    {
                        m = Modules.Find(delegate(Module o)
                        {
                            return o.Domain == Domains.Media_CameraInput && o.Address == "AV0";
                        });
                    }
                    catch
                    {
                    }
                    // add new module
                    if (m == null)
                    {
                        m = new Module();
                        m.Domain = Domains.Media_CameraInput;
                        m.DeviceType = Module.DeviceTypes.Sensor;
                        Utility.ModuleParameterSet(m, Properties.WIDGET_DISPLAYMODULE, "homegenie/generic/camerainput");
                        Modules.Add(m);
                    }
                    //
                    m.Address = "AV0";
                    m.Description = "Video 4 Linux Video Input";
                }
                else
                {
                    Modules.RemoveAll(m => m.Domain == Domains.Media_CameraInput && m.RoutingNode == "");
                }
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "_modules_refresh_misc()", ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
        }

        internal void _modules_sort()
        {

            // sort modules properties by name
            foreach (Module m in Modules)
            {
                // various normalization stuff
                m.Properties.Sort(delegate(ModuleParameter p1, ModuleParameter p2)
                {
                    return p1.Name.CompareTo(p2.Name);
                });
            }
            //
            // sort modules
            //
            Modules.Sort(delegate(Module m1, Module m2)
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

        internal void _modules_refresh_all()
        {
            _modules.RemoveAll(m => m == null); // dunno why but sometimes it happen to have null entries causing exceptions
            _modules_refresh_zwave();
            _modules_refresh_x10();
            _modules_refresh_misc();
            _modules_refresh_programs();
            _modules_refresh_virtualmods();
            _modules_sort();
        }

        internal void _modules_refresh_virtualmods()
        {
            try
            {
                //
                // Virtual Modules
                //
                lock (_virtualmodules.LockObject)
                    foreach (VirtualModule vm in _virtualmodules)
                    {
                        ProgramBlock pb = null;
                        try
                        {
                            pb = _mastercontrolprogram.Programs.Find(p => p.Address.ToString() == vm.ParentId);
                        }
                        catch { }
                        if (pb == null) continue;
                        //
                        ModuleParameter parwidget = Utility.ModuleParameterGet(vm, Properties.WIDGET_DISPLAYMODULE);
                        //
                        Module m = null;
                        try
                        {
                            m = Modules.Find(delegate(Module o)
                            {
                                return o.Domain == vm.Domain && o.Address == vm.Address;
                            });
                        }
                        catch
                        {
                        }

                        // TODO: improve modules dispose mechanism
                        if ((pb == null || !pb.IsEnabled) && m.RoutingNode == "")
                        {
                            if (m != null && m.Domain != Domains.HomeAutomation_HomeGenie_Automation)
                            {
                                // copy properties to virtualmodules before removing
                                vm.Properties.Clear();
                                foreach (ModuleParameter p in m.Properties)
                                {
                                    vm.Properties.Add(p);
                                }
                                Modules.Remove(m);
                            }
                            continue;
                        }

                        /*
                        if ((parwidget == null || parwidget.Value == null || parwidget.Value.Trim() == "") && m.RoutingNode == "")
                        {
                            //if (m != null) Modules.Remove(m);
                            continue;
                        }
                        else*/
                        if (m == null)
                        {
                            // add new module
                            m = new Module();
                            Modules.Add(m);
                            // copy properties from virtualmodules
                            foreach (ModuleParameter p in vm.Properties)
                            {
                                m.Properties.Add(p);
                            }
                        }
                        //
                        // inherited props from virtual module
                        m.Domain = vm.Domain;
                        m.Address = vm.Address;
                        m.DeviceType = vm.DeviceType;
                        if (m.Name == "" || m.DeviceType == Module.DeviceTypes.Program) m.Name = vm.Name;
                        m.Description = vm.Description;
                        //
                        Utility.ModuleParameterSet(m, Properties.VIRTUALMODULE_PARENTID, vm.ParentId);
                        var modwidget = Utility.ModuleParameterGet(m, Properties.WIDGET_DISPLAYMODULE);
                        if (parwidget != null && parwidget.Value != "" && (modwidget == null || modwidget.Value != parwidget.Value)) // && parwidget.Value != "")
                        {
                            Utility.ModuleParameterSet(m, Properties.WIDGET_DISPLAYMODULE, parwidget.Value);
                        }
                    }
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "_modules_refresh_virtualmods()", ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
        }

        internal void _modules_refresh_programs()
        {
            try
            {
                //
                // ProgramEngine programs (modules)
                //
                if (_mastercontrolprogram != null)
                {
                    foreach (ProgramBlock pb in _mastercontrolprogram.Programs)
                    {
                        Module m = null;
                        try
                        {
                            m = Modules.Find(delegate(Module o)
                            {
                                return o.Domain == Domains.HomeAutomation_HomeGenie_Automation && o.Address == pb.Address.ToString();
                            });
                        }
                        catch
                        {
                        }
                        //
                        if (m != null && pb.Type.ToLower() == "wizard" && !pb.IsEnabled && m.RoutingNode == "")
                        {
                            Modules.Remove(m);
                            continue;
                        }
                        else if (!pb.IsEnabled)
                        {
                            continue;
                        }
                        //
                        // add new module
                        if (m == null)
                        {
                            m = new Module();
                            m.Domain = Domains.HomeAutomation_HomeGenie_Automation;
                            if (pb.Type.ToLower() == "wizard")
                            {
                                Utility.ModuleParameterSet(m, Properties.WIDGET_DISPLAYMODULE, "homegenie/generic/program");
                            }
                            Modules.Add(m);
                        }
                        //
                        m.Address = pb.Address.ToString();
                        m.DeviceType = Module.DeviceTypes.Program;
                        m.Name = pb.Name;
                        m.Description = "Wizard Script";
                    }
                }
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "_modules_refresh_programs()", ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
        }

        #endregion


        #region Private utility methods

        private void _initializesystem()
        {
            LoadConfiguration();
            //
            // setup other objects used in HG
            //
            _virtualmeter = new VirtualMeter(this);
        }

        private void _loadsystemconfig()
        {
            try
            {
                // load config
                XmlSerializer mserializer = new XmlSerializer(typeof(SystemConfiguration));
                StreamReader mreader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "systemconfig.xml"));
                _systemconfiguration = (SystemConfiguration)mserializer.Deserialize(mreader);
                if (!String.IsNullOrEmpty(_systemconfiguration.HomeGenie.EnableLogFile) && _systemconfiguration.HomeGenie.EnableLogFile.ToLower().Equals("true"))
                {
                    SystemLogger.Instance.OpenLog();
                }
                else
                {
                    SystemLogger.Instance.CloseLog();
                }
                // set the system password
                _migservice.SetWebServicePassword(_systemconfiguration.HomeGenie.UserPassword);
                //
                foreach (ModuleParameter p in _systemconfiguration.HomeGenie.Settings)
                {
                    try
                    {
                        if (!String.IsNullOrEmpty(p.Value)) p.Value = StringCipher.Decrypt(p.Value, _systemconfiguration.GetPassPhrase());
                        if (!String.IsNullOrEmpty(p.LastValue)) p.LastValue = StringCipher.Decrypt(p.LastValue, _systemconfiguration.GetPassPhrase());
                    }
                    catch { }
                }
                //
                mreader.Close();
                //
                //
                if (_zwavecontroller != null)
                {
                    _zwavecontroller.DiscoveryEvent -= _zwavecontroller_DiscoveryEvent;
                }
                //
                // connect enabled interfaces only
                //
                foreach (MIGServiceConfiguration.Interface iface in _systemconfiguration.MIGService.Interfaces)
                {
                    MIGInterface mif = GetInterface(iface.Domain);
                    if (mif == null)
                    {
                        _migservice.AddInterface(iface.Domain);
                    }
                }
                //
                // initialize interfaces
                //
                (GetInterface(Domains.HomeAutomation_ZWave) as MIG.Interfaces.HomeAutomation.ZWave).SetPortName(_systemconfiguration.GetInterfaceOption(Domains.HomeAutomation_ZWave, "Port").Value.Replace("|", "/"));
                (GetInterface(Domains.HomeAutomation_X10) as MIG.Interfaces.HomeAutomation.X10).SetPortName(_systemconfiguration.GetInterfaceOption(Domains.HomeAutomation_X10, "Port").Value.Replace("|", "/"));
                (GetInterface(Domains.HomeAutomation_X10) as MIG.Interfaces.HomeAutomation.X10).SetHouseCodes(_systemconfiguration.GetInterfaceOption(Domains.HomeAutomation_X10, "HouseCodes").Value);
                (GetInterface(Domains.HomeAutomation_W800RF) as MIG.Interfaces.HomeAutomation.W800RF).SetPortName(_systemconfiguration.GetInterfaceOption(Domains.HomeAutomation_W800RF, "Port").Value);
                //
                // get direct reference to XTenLib and ZWaveLib interface drivers
                //
                _x10controller = (GetInterface(Domains.HomeAutomation_X10) as MIG.Interfaces.HomeAutomation.X10).X10Controller;
                _zwavecontroller = (GetInterface(Domains.HomeAutomation_ZWave) as MIG.Interfaces.HomeAutomation.ZWave).ZWaveController;
                //
                if (_zwavecontroller != null)
                {
                    _zwavecontroller.DiscoveryEvent += _zwavecontroller_DiscoveryEvent;
                }
                //
                // setup UPnP local service
                //
                if (_systemconfiguration.GetInterface(Domains.Protocols_UPnP) == null)
                {
                    // following code is needed for backward compatibility
                    MIGInterface mif = GetInterface(Domains.Protocols_UPnP);
                    if (mif == null)
                    {
                        _migservice.AddInterface(Domains.Protocols_UPnP);
                    }
                    _systemconfiguration.MIGService.Interfaces.Add(new MIGServiceConfiguration.Interface() { Domain = Domains.Protocols_UPnP });
                    _systemconfiguration.GetInterface(Domains.Protocols_UPnP).IsEnabled = true;
                }
                if (_systemconfiguration.GetInterface(Domains.Protocols_UPnP).IsEnabled)
                {
                    _setupUPnP();
                }
                //
                foreach (MIGServiceConfiguration.Interface iface in _systemconfiguration.MIGService.Interfaces)
                {
                    MIGInterface mif = GetInterface(iface.Domain);
                    //mif.Disconnect();
                    bool isconnected = false;
                    try { isconnected = mif.IsConnected; }
                    catch { }
                    if (iface.IsEnabled && !isconnected)
                    {
                        //Thread.Sleep(1000);
                        mif.Connect();
                    }
                }
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "_loadsystemconfig()", ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
        }

        private void _loadmodules()
        {
            try
            {
                XmlSerializer mserializer = new XmlSerializer(typeof(HomeGenie.Service.TsList<Module>));
                StreamReader mreader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "modules.xml"));
                HomeGenie.Service.TsList<Module> mods = (HomeGenie.Service.TsList<Module>)mserializer.Deserialize(mreader);
                //
                foreach (Module m in mods)
                {
                    foreach (ModuleParameter p in m.Properties)
                    {
                        try
                        {
                            if (!String.IsNullOrEmpty(p.Value)) p.Value = StringCipher.Decrypt(p.Value, _systemconfiguration.GetPassPhrase());
                            if (!String.IsNullOrEmpty(p.LastValue)) p.LastValue = StringCipher.Decrypt(p.LastValue, _systemconfiguration.GetPassPhrase());
                        }
                        catch { }
                    }
                }
                //
                mreader.Close();
                //
                _modules.Clear();
                _modules = mods;
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "_loadmodules()", ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
            try
            {
                //
                // reset Parameter.Watts, Status Level, Sensor.Generic values
                //
                for (int m = 0; m < _modules.Count; m++)
                {
                    // cleanup stuff for unwanted  xsi:nil="true" empty params
                    _modules[m].Properties.RemoveAll(p => p == null);
                    //
                    ModuleParameter parameter = null;
                    parameter = _modules[m].Properties.Find(delegate(ModuleParameter mp)
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
                //
                // these are virtual special modules for exposing CM15 RF, W800RF and LIRC client for IR learn/control
                //
                Module mod = _modules.Find(delegate(Module o)
                {
                    return o.Domain == Domains.HomeAutomation_X10 && o.Address == "RF";
                });
                if (mod == null)
                {
                    mod = new Module() { Domain = Domains.HomeAutomation_X10, Address = "RF", Type = Module.Types.InputSensor, DeviceType = Module.DeviceTypes.Sensor };
                    _modules.Add(mod);
                }
                Module w800rf = _modules.Find(delegate(Module o)
                {
                    return o.Domain == Domains.HomeAutomation_W800RF && o.Address == "RF";
                });
                if (w800rf == null)
                {
                    w800rf = new Module() { Domain = Domains.HomeAutomation_W800RF, Address = "RF", Type = Module.Types.InputSensor, DeviceType = Module.DeviceTypes.Sensor };
                    _modules.Add(w800rf);
                }
                Module lircir = _modules.Find(delegate(Module o)
                {
                    return o.Domain == Domains.Controllers_LircRemote && o.Address == "IR";
                });
                if (lircir == null)
                {
                    lircir = new Module() { Domain = Domains.Controllers_LircRemote, Address = "IR", Type = Module.Types.InputSensor, DeviceType = Module.DeviceTypes.Sensor };
                    _modules.Add(lircir);
                }
            }
            catch (Exception ex)
            {
                HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "_loadmodules()", ex.Message, "Exception.StackTrace", ex.StackTrace);
            }
            //
            // force re-generation of Modules list
            _modules_refresh_all();
        }

        private void _archiveConfiguration(string archivename)
        {
            if (File.Exists(archivename))
            {
                File.Delete(archivename);
            }
            foreach (ProgramBlock pb in _mastercontrolprogram.Programs)
            {
                if (File.Exists("programs/" + pb.Address + ".dll"))
                {
                    Utility.AddFileToZip(archivename, "programs/" + pb.Address + ".dll");
                }
            }
            //
            Utility.AddFileToZip(archivename, "systemconfig.xml");
            Utility.AddFileToZip(archivename, "automationgroups.xml");
            Utility.AddFileToZip(archivename, "modules.xml");
            Utility.AddFileToZip(archivename, "programs.xml");
            Utility.AddFileToZip(archivename, "scheduler.xml");
            Utility.AddFileToZip(archivename, "groups.xml");
            if (File.Exists("lircconfig.xml"))
            {
                Utility.AddFileToZip(archivename, "lircconfig.xml");
            }
        }

        private void _setupUPnP()
        {
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            //
            string presentationURL = "http://" + localIP + ":" + _systemconfiguration.HomeGenie.ServicePort;
            string friendlyName = "HomeGenie: " + Environment.MachineName;
            string manufacturer = "G-Labs";
            string manufacturerURL = "http://generoso.info/";
            string modelName = "HomeGenie";
            string modelDescription = "HomeGenie Home Automation Server";
            string modelURL = "http://homegenie.it/";
            string modelNumber = "HG-1";
            string standardDeviceType = "HomeAutomationServer";
            string uniqueDeviceName = _systemconfiguration.HomeGenie.GUID;
            if (String.IsNullOrEmpty(uniqueDeviceName))
            {
                _systemconfiguration.HomeGenie.GUID = uniqueDeviceName = System.Guid.NewGuid().ToString();
                _systemconfiguration.Update();
            }
            //
            (GetInterface(Domains.Protocols_UPnP) as MIG.Interfaces.Protocols.UPnP)
                .CreateLocalDevice(uniqueDeviceName, standardDeviceType,
                        presentationURL,
                        "web\\",
                        modelName,
                        modelDescription,
                        modelURL,
                        modelNumber,
                        manufacturer,
                        manufacturerURL);
        }

        private void _updateZWaveNodeDeviceHandler(byte nodeid, Module mod)
        {
            ModuleParameter handler = Utility.ModuleParameterGet(mod, Properties.ZWAVENODE_DEVICEHANDLER);
            ZWaveNode node = _zwavecontroller.Devices.Find(zn => zn.NodeId == nodeid);
            if (node != null && node.DeviceHandler != null && (handler == null || handler.Value.Contains(".Generic.")))
            {
                Utility.ModuleParameterSet(mod, Properties.ZWAVENODE_DEVICEHANDLER, node.DeviceHandler.GetType().FullName);
            }
            else if (handler != null && !handler.Value.Contains(".Generic."))
            {
                // set to last known handler
                node.SetDeviceHandlerFromName(handler.Value);
            }
        }

        // this is used to generate Lirc supported remotes from http://lirc.sourceforge.net/remotes/
        private List<string> _getlircitems(string url)
        {
            WebClient wb = new WebClient();
            string s = wb.DownloadString(url);

            string pattern = @"<(.|\n)*?>";
            s = s.Replace("</a>", " ");
            s = System.Text.RegularExpressions.Regex.Replace(s, pattern, string.Empty);

            s = s.Replace("&amp;", "&");
            s = s.Replace("&nbsp;", " ");

            string[] lines = s.Split('\n');

            bool readitems = false;
            List<string> manufacturers = new List<string>();
            foreach (string l in lines)
            {
                if (readitems)
                {
                    if (l.Trim() == "") break;
                    Console.WriteLine(l);
                    string brand = l.Split('/')[0];
                    brand = brand.Split(' ')[0];
                    manufacturers.Add(brand.Trim());
                }
                else if (l.ToLower().StartsWith("parent directory"))
                {
                    readitems = true;
                }
            }
            return manufacturers;
        }

        #endregion


    }

}