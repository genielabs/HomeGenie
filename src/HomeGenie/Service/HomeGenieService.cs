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
 *     Project Homepage: https://homegenie.it
 */


using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Serialization;

using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using OpenSource.UPnP;

using HomeGenie.Automation;
using HomeGenie.Data;
using HomeGenie.Service.Constants;
using HomeGenie.Service.Logging;
using HomeGenie.Automation.Scheduler;

using MIG;
using MIG.Gateways;
using MIG.Gateways.Authentication;
using MIG.Interfaces.HomeAutomation.Commons;
using NLog;

using Module = HomeGenie.Data.Module;

namespace HomeGenie.Service
{
    [Serializable]
    public class HomeGenieService
    {
        #region Constants

        public const string authenticationRealm = "HomeGenie Secure Zone";

        #endregion

        #region Private Fields declaration

        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private MigService migService;
        private WebServiceGateway webGateway;
        private ProgramManager masterControlProgram;
        private UpdateChecker updateChecker;
        private BackupManager backupManager;
        private PackageManager packageManager;

        // Internal data structures
        private TsList<Module> systemModules = new TsList<Module>();
        private TsList<Module> modulesGarbage = new TsList<Module>();
        private TsList<VirtualModule> virtualModules = new TsList<VirtualModule>();
        private List<Group> automationGroups = new List<Group>();
        private List<Group> controlGroups = new List<Group>();

        private SystemConfiguration systemConfiguration;

        #endregion

        #region Web Service Handlers declaration

        private Handlers.Config wshConfig;
        private Handlers.Automation wshAutomation;

        #endregion

        #region Lifecycle

        private bool systemStarted;
        public HomeGenieService(bool rebuildPrograms = false)
        {
            RebuildPrograms = rebuildPrograms;
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            try
            {
                EnableOutputRedirect();
            }
            catch (Exception e)
            {
                // this happens when running as a Windows Service,
                // output can't be redirected due to:
                // IOException: "The handle is invalid."
            }

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

            // Setup local UPnP device
            SetupUpnp();

            // it will check every 24 hours
            updateChecker.Start();
        }

        public bool RebuildPrograms { get; set; }

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

            RaiseEvent(Domains.HomeGenie_System, Domains.HomeGenie_System, SourceModule.Master, "HomeGenie System", Properties.HomeGenieStatus, "MIG Service STOPPING");
            if (migService != null)
            {
                try
                {
                    migService.StopService();
                }
                catch (Exception e)
                {
                    LogError(e);
                }
            }
            RaiseEvent(Domains.HomeGenie_System, Domains.HomeGenie_System, SourceModule.Master, "HomeGenie System", Properties.HomeGenieStatus, "MIG Service STOPPED");
            RaiseEvent(Domains.HomeGenie_System, Domains.HomeGenie_System, SourceModule.Master, "HomeGenie System", Properties.HomeGenieStatus, "ProgramEngine STOPPING");
            if (masterControlProgram != null)
            {
                masterControlProgram.Enabled = false;
            }
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

        // Public utility methods
        public string GetHttpServicePort()
        {
            return webGateway.GetOption(WebServiceGatewayOptions.Port).Value;
        }

        public object InterfaceControl(MigInterfaceCommand cmd)
        {
            object response = null;
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
            // Route command to Automation Programs' Dynamic API
            var r = ProgramDynamicApi.TryApiCall(cmd);
            if (r != null)
            {
                // Automation Programs can eventually override MIG response
                response = r;
            }
            return response;
        }

        public List<Group> GetGroups(string namePrefix = "")
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
                    var module = systemModules[m];
                    if (module.DeviceType == ModuleTypes.Program)
                    {
                        int.TryParse(module.Address, out var pid);
                        var program = masterControlProgram.ProgramGet(pid);
                        if (program == null || !program.IsEnabled)
                        {
                            // hide program modules if program does not exists or is disabled
                            continue;
                        }
                    }
                    jsonModules += Utility.Module2Json(module, hideProperties) + ",\n";
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

        #endregion

        #region MIG Events Propagation / Logging

        // TODO: refactor to RouteEvent
        internal void RaiseEvent(object sender, MigEvent evt)
        {
            // TODO: refactor to RouteEvent  (even in MIG)
            migService.RaiseEvent(sender, evt);
        }

        internal void RaiseEvent(
            object sender,
            string domain,
            string source,
            string description,
            string property,
            object value
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
                ModuleParameter parameter;
                try
                {
                    // Lookup for the existing module parameter
                    parameter = Utility.ModuleParameterGet(module, args.EventData.Property);
                    if (parameter == null)
                    {
                        parameter = new ModuleParameter() { Name = args.EventData.Property };
                        module.Properties.Add(parameter);
                    }
                    parameter.SetData(args.EventData.Value);
                }
                catch (Exception ex)
                {
                    LogError(ex);
                }
                // Route event to Master Control Program =)
                if (masterControlProgram != null)
                {
                    // Prevent event pump from blocking on other worker tasks
                    Task.Run(() =>
                    {
                        masterControlProgram.SignalPropertyChange(sender, module, args.EventData);
                    }); 
                }
                // check if event routing has been disabled for this module
                var eventsDisable = Utility.ModuleParameterGet(module, Properties.EventsDisable);
                if (eventsDisable != null && eventsDisable.Value == "1")
                {
                    args.EventData.Disabled = true;
                }
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
            if (args.Request.Context.Source != ContextSource.WebServiceGateway && args.Request.Context.Source != ContextSource.WebSocketGateway)
                return;
            
            HandleSystemApiRequest(args.Request);
        }

        private void migService_ServiceRequestPostProcess(object sender, ProcessRequestEventArgs args)
        {
            var command = args.Request.Command;
            if (command.Domain ==  Domains.MigService_Interfaces && command.Command.EndsWith(".Set"))
            {
                systemConfiguration.Update();
            }

            // Let automation programs process this request too
            var response = ProgramDynamicApi.TryApiCall(command);
            if (response != null)
            {
                args.Request.ResponseData = response;
            }
        }

        #endregion

        #region Initialization and Data Persistence

        private Func<Type,Exception,bool> UpdateDatabaseErrorHandler = (t, e) =>
        {
            LogError(Domains.HomeAutomation_HomeGenie,
                string.Format("UpdateXmlDatabase<{0}>()", t.FullName), e.Message, "StackTrace",
                e.StackTrace);
            return false;
        };

        public bool UpdateGroupsDatabase(string namePrefix = "")
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
            string filename = namePrefix.ToLower() + "groups.xml";
            return Utility.UpdateXmlDatabase(groups, filename, UpdateDatabaseErrorHandler);
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
                    // add virtual modules
                    for (int i = 0; i < virtualModules.Count; i++)
                    {
                        var virtualModule = virtualModules[i];
                        var module = clonedModules
                            .Find(m => m.Domain == virtualModule.Domain && m.Address == virtualModule.Address);
                        
                        if (module != null)
                        {
                            continue;
                        }

                        // add new module
                        module = virtualModule.DeepClone();
                        clonedModules.Add(module);
                        Utility.ModuleParameterSet(
                            module,
                            Properties.VirtualModuleParentId,
                            virtualModule.ParentId
                        );
                    }
                    
                    foreach (var module in clonedModules)
                    {
                        foreach (var parameter in module.Properties)
                        {
                            // these four properties have to be kept in clear text
                            if (parameter.Name != Properties.WidgetDisplayModule
                                && parameter.Name != Properties.VirtualModuleParentId
                                && parameter.Name != Properties.ProgramStatus
                                && parameter.Name != Properties.RuntimeError
                                && parameter.GetData() is string)
                            {
                                string stringValue = parameter.Value;
                                if (!string.IsNullOrEmpty(stringValue))
                                {
                                    parameter.Value = StringCipher.Encrypt(stringValue, GetPassPhrase());
                                }
                            }
                        }
                    }
                    success = Utility.UpdateXmlDatabase(clonedModules, "modules.xml", UpdateDatabaseErrorHandler);
                }
                catch (Exception ex)
                {
                    LogError(Domains.HomeAutomation_HomeGenie, "UpdateModulesDatabase()", ex.Message,
                        "Exception.StackTrace", ex.StackTrace);
                }
            }
            return success;
        }

        public bool UpdateProgramsDatabase()
        {
            lock (masterControlProgram.Programs.LockObject)
            {
                return Utility.UpdateXmlDatabase(masterControlProgram.Programs, "programs.xml",
                    UpdateDatabaseErrorHandler);
            }
        }

        public bool UpdateSchedulerDatabase()
        {
            return Utility.UpdateXmlDatabase(masterControlProgram.SchedulerService.Items, "scheduler.xml", UpdateDatabaseErrorHandler);
        }

        /// <summary>
        /// Reload system configuration and restart services and interfaces.
        /// </summary>
        public void Reload()
        {
            // delete service address log file
            File.Delete("serviceaddress.txt");

            migService.StopService();

            LoadConfiguration();

            webGateway = (WebServiceGateway)migService.GetGateway(Gateways.WebServiceGateway);
            if (webGateway == null)
            {
                RaiseEvent(
                    Domains.HomeGenie_System,
                    Domains.HomeAutomation_HomeGenie,
                    SourceModule.Master,
                    "Configuration entry not found",
                    "Gateways",
                    Gateways.WebServiceGateway
                );
                Program.Quit(false);
            }
            // handle user authentication when Basic or Digest authentication schema is set
            webGateway.SetOption(WebServiceGatewayOptions.AuthenticationRealm, authenticationRealm);
            webGateway.UserAuthenticationHandler += (sender, eventArgs) =>
            {
                if (eventArgs.Username == systemConfiguration.HomeGenie.Username)
                {
                    return new User(eventArgs.Username, authenticationRealm, systemConfiguration.HomeGenie.Password);
                }
                return null;
            };
            int webPort = int.Parse(webGateway.GetOption(WebServiceGatewayOptions.Port).Value);

            bool started = migService.StartService();
            while (!started)
            {
                RaiseEvent(
                    Domains.HomeGenie_System,
                    Domains.HomeAutomation_HomeGenie,
                    SourceModule.Master,
                    "HTTP binding failed.",
                    Properties.SystemInfoHttpAddress,
                    webGateway.GetOption(WebServiceGatewayOptions.Host).Value + ":" + webGateway.GetOption(WebServiceGatewayOptions.Port).Value
                );
                // Try auto-binding to another port >= 8080 (up to 8090)
                if (webPort < 8080)
                    webPort = 8080;
                else
                    webPort++;
                if (webPort <= 8180) // binding range [8080-8180]
                {
                    webGateway.SetOption(WebServiceGatewayOptions.Port, webPort.ToString());
                    started = webGateway.Start();
                }
                else break;
            }

            if (started)
            {
                string serviceHost = webGateway.GetOption(WebServiceGatewayOptions.Host).Value;
                string servicePort = webGateway.GetOption(WebServiceGatewayOptions.Port).Value;
                RaiseEvent(
                    Domains.HomeGenie_System,
                    Domains.HomeAutomation_HomeGenie,
                    SourceModule.Master,
                    "HomeGenie service ready",
                    Properties.SystemInfoHttpAddress,
                    serviceHost + ":" + servicePort
                );
                // log service address to file
                File.WriteAllText("serviceaddress.txt", String.Format("HG_SERVICE_HOST={0}\nHG_SERVICE_PORT={1}\n", serviceHost, servicePort));
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
                string programsDatabase = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs.xml");
                var serializer = new XmlSerializer(typeof(List<ProgramBlock>));
                using (var reader = new StreamReader(programsDatabase))
                {
                    var programs = (List<ProgramBlock>)serializer.Deserialize(reader);
                    foreach (var program in programs)
                    {
                        program.IsRunning = false;
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
            // restore virtual modules list 
            systemModules.ForEach((m) =>
            {
                var parentId = Utility.ModuleParameterGet(m, Properties.VirtualModuleParentId);
                if (parentId == null)
                {
                    return;
                }
                var program = masterControlProgram.Programs.Find((p) => p.Address.ToString() == parentId.Value);
                if (program != null)
                {
                    var virtualModule = new VirtualModule
                    {
                        ParentId = program.Address.ToString(),
                        Domain = m.Domain,
                        Address = m.Address,
                        Name = m.Name,
                        Description = m.Description,
                        DeviceType = m.DeviceType,
                        Properties = m.Properties.DeepClone(),
                        Stores = m.Stores.DeepClone()
                    };
                    virtualModules.Add(virtualModule);
                }
            });
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
            // run remaining operations in the background
            Task.Run(() =>
            {
                BootProgress = 0;
                // Rebuild automation programs if required
                if (RebuildPrograms)
                {
                    double bootStep = 100D / masterControlProgram.Programs.Count;
                    for (int p = 0; p < masterControlProgram.Programs.Count; p++)
                    {
                        var program = masterControlProgram.Programs[p];
                        masterControlProgram.ProgramCompile(program);
                        BootProgress += bootStep;
                    }
                    RebuildPrograms = false;
                }
                // enable automation programs engine
                masterControlProgram.Enabled = true;
                BootProgress = 100;

                if (!systemStarted)
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
                    systemStarted = true;
                }
            });
        }

        private double bootProgress;
        public double BootProgress
        {
            get { return bootProgress; }
            private set
            {
                bootProgress = value;
                RaiseEvent(
                    Domains.HomeGenie_System,
                    Domains.HomeGenie_System,
                    SourceModule.Master,
                    "HomeGenie Boot Progress",
                    Properties.SystemInfoBootProgress,
                    bootProgress.ToString()
                );
            }
        }

        public void RestoreFactorySettings()
        {
            // Stop program engine
            try
            {
                masterControlProgram.Enabled = false;
                masterControlProgram = null;
            } catch { }
            RebuildPrograms = true;
            // Unzip factory settings and restart HG service
            Utility.UncompressZip("homegenie_factory_config.zip", AppDomain.CurrentDomain.BaseDirectory);
            // Keep current MIG gateways settings
            var gatewaysConfig = SystemConfiguration.MigService.Gateways;
            backupManager.UpdateSystemConfig(AppDomain.CurrentDomain.BaseDirectory, gatewaysConfig);
            // delete programs' data
            string programsDataFolder = Path.Combine(Utility.GetDataFolder(), "programs");
            if (Directory.Exists(programsDataFolder))
            {
                try
                {
                    Directory.Delete(programsDataFolder, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            // Reload and save new config
            Reload();
            virtualModules.Clear();
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
                    if (!virtualModule.IsActive || masterControlProgram == null) continue;
                    ProgramBlock program = masterControlProgram.Programs.Find(p => p.Address.ToString() == virtualModule.ParentId);
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
                    if (module != null && (program == null || 
                                           (!program.IsEnabled
                                            && (module.Domain != program.Domain
                                             || module.Address != program.Address.ToString()))))
                    {
                        // copy instance module properties to virtual modules before removing
                        virtualModule.Name = module.Name;
                        virtualModule.Description = module.Description;
                        virtualModule.DeviceType = module.DeviceType;
                        //virtualModule.Properties.Clear();
                        virtualModule.Properties = module.Properties.DeepClone();
                        virtualModule.Stores = module.Stores.DeepClone();
                        systemModules.Remove(module);
                        continue;
                    }

                    if (module == null)
                    {
                        // add new module
                        module = new Module();
                        module.Properties = virtualModule.Properties.DeepClone();
                        module.Stores = virtualModule.Stores.DeepClone();
                        systemModules.Add(module);
                    }

                    // module inherits props from associated virtual module
                    module.Domain = virtualModule.Domain;
                    module.Address = virtualModule.Address;
                    if (module.DeviceType == ModuleTypes.Generic && virtualModule.DeviceType != ModuleTypes.Generic)
                    {
                        module.DeviceType = virtualModule.DeviceType;
                    }
                    if (module.Name == "")
                    {
                        module.Name = virtualModule.Name;
                    }
                    if (module.Description == "")
                    {
                        module.Description = virtualModule.Description;
                    }
                    Utility.ModuleParameterSet(
                        module,
                        Properties.VirtualModuleParentId,
                        virtualModule.ParentId
                    );
                    var virtualModuleWidget = Utility.ModuleParameterGet(virtualModule, Properties.WidgetDisplayModule);
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
                // Refresh ProgramEngine program modules
                lock (systemModules.LockObject)
                if (masterControlProgram != null)
                {
                    var modules = new List<Module>(systemModules);
                    lock (masterControlProgram.Programs.LockObject)
                    foreach (var program in masterControlProgram.Programs)
                    {
                        try
                        {
                            Module module = modules.Find(o => o.Domain == Domains.HomeAutomation_HomeGenie_Automation && o.Address == program.Address.ToString());
                            if (module != null && (program.Type.ToLower() == "visual" || program.Type.ToLower() == "wizard") && !program.IsEnabled)
                            {
                                // TODO: deprecate "wizard" programs
                                
                                // only removes program's module if the program kind is "scenario",
                                // while keep it for other types in order to hold program's configuration
                                // that is stored as module's parameters
                                systemModules.Remove(module);
                                continue;
                            }
                            if (module == null && !program.IsEnabled)
                            {
                                continue;
                            }
                            if (module == null)
                            {
                                // add module for the program
                                module = new Module
                                {
                                    Domain = Domains.HomeAutomation_HomeGenie_Automation,
                                    Name = program.Name,
                                    Description = program.Description
                                };
                                if (program.Type.ToLower() == "visual" || program.Type.ToLower() == "wizard")
                                {
                                    // set the widget used for programs of type "scenario"
                                    Utility.ModuleParameterSet(
                                        module,
                                        Properties.WidgetDisplayModule,
                                        "homegenie/generic/program"
                                    );
                                }
                                systemModules.Add(module);
                            }
                            module.Address = program.Address.ToString();
                            module.DeviceType = ModuleTypes.Program;
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
                    // Add "Scheduler" virtual module
                    //Module scheduler = systemModules.Find(o=> o.Domain == Domains.HomeAutomation_HomeGenie && o.Address == SourceModule.Scheduler);
                    //if (scheduler == null) {
                    //    scheduler = new Module(){ Domain = Domains.HomeAutomation_HomeGenie, Address = SourceModule.Scheduler };
                    //    systemModules.Add(scheduler);
                    //}
                }
        }

        internal void modules_Sort()
        {
            try
            {
                var modules = new List<Module>(systemModules);
                // sort modules properties by name
                foreach (var module in modules)
                {
                    module?.Properties.Sort((ModuleParameter p1, ModuleParameter p2) =>
                    {
                        int c = -1;
                        try
                        {
                            c = p1.Name.CompareTo(p2.Name);
                        }
                        catch (Exception e)
                        {
                        }
                        return c;
                    });
                }
                //
                // sort modules
                //
                lock (systemModules.LockObject)
                systemModules.Sort((Module m1, Module m2) =>
                {
                    int c = -1;
                    try
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

                        c = d1.CompareTo(d2);
                    } catch(Exception e) {}
                    return c;
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
            lock (systemModules.LockObject)
            {
                systemModules.RemoveAll(m =>
                    m == null); // <-- dunno why but sometimes it happen to have null entries causing exceptions
            }

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
        }

        private void modules_RefreshInterface(MigInterface iface)
        {
            lock (systemModules.LockObject)
            {
                if (migService.Configuration.GetInterface(iface.GetDomain()).IsEnabled)
                {
                    var interfaceModules = iface.GetModules();
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
                    if (interfaceModules.Count > 0)
                    {
                        bool modulesGroupAdded = false;
                        for (var i = 0; i < interfaceModules.Count; i++)
                        {
                            var migModule = interfaceModules[i];
                            Module module = systemModules.Find(o =>
                                o.Domain == migModule.Domain && o.Address == migModule.Address);
                            if (module == null)
                            {
                                // try restoring from garbage
                                module = modulesGarbage.Find(o =>
                                    o.Domain == migModule.Domain && o.Address == migModule.Address);
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

                                // Add UPnP devices automatically to UPnP group
                                if (iface.GetDomain() == Domains.Protocols_UPnP)
                                {
                                    var upnpGroup = Groups.Find((group => group.Name == "UPnP"));
                                    if (upnpGroup == null)
                                    {
                                        upnpGroup = new Group();
                                        upnpGroup.Name = "UPnP";
                                        Groups.Add(upnpGroup);
                                        modulesGroupAdded = true;
                                    }
                                    bool exists = upnpGroup.Modules.Find((mr => mr.Domain == module.Domain && mr.Address == module.Address)) != null;
                                    if (!exists)
                                    {
                                        upnpGroup.Modules.Add(new ModuleReference(){Domain = module.Domain, Address = module.Address});
                                        modulesGroupAdded = true;
                                    }
                                }

                            }

                            if (String.IsNullOrEmpty(module.Description))
                            {
                                module.Description = migModule.Description;
                            }

                            if (module.DeviceType == ModuleTypes.Generic && migModule.CustomData != null &&
                                migModule.CustomData.Type != null)
                            {
                                module.DeviceType = migModule.CustomData.Type;
                            }
                        }
                        if (modulesGroupAdded)
                        {
                            UpdateGroupsDatabase();
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
        }

        internal void modules_SetParameter(Module module, string parameter, string value)
        {
            var data = "{" + JsonSerializer.Serialize(parameter) + ": " + JsonSerializer.Serialize(value) + "}";
            var command =
                new MigInterfaceCommand(
                    $"HomeAutomation.HomeGenie/Config/Modules.ParameterSet/{module.Domain}/{module.Address}"); 
            var request = new MigClientRequest(null, command);
            request.RequestData = Encoding.UTF8.GetBytes(data);
            wshConfig.ProcessRequest(request);
        }

        #endregion

        #region Private utility methods

        private void HandleSystemApiRequest(MigClientRequest request)
        {
            var migCommand = request.Command;
            // HomeGenie Web Service domain API
            if (migCommand.Domain == Domains.HomeAutomation_HomeGenie)
            {
                // domain == HomeAutomation.HomeGenie
                switch (migCommand.Address)
                {
                    case "Config":
                        wshConfig.ProcessRequest(request);
                        break;
                    case "Automation":
                        wshAutomation.ProcessRequest(request);
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

        private void InitializeSystem()
        {
            // Setup web service handlers
            wshConfig = new Handlers.Config(this);
            wshAutomation = new Handlers.Automation(this);
            // Initialize MigService, gateways and interfaces
            migService = new MIG.MigService();
            migService.InterfaceModulesChanged += migService_InterfaceModulesChanged;
            migService.InterfacePropertyChanged += migService_InterfacePropertyChanged;
            migService.GatewayRequestPreProcess += migService_ServiceRequestPreProcess;
            migService.GatewayRequestPostProcess += migService_ServiceRequestPostProcess;
        }

        private string GetPassPhrase()
        {
            if (systemConfiguration != null && !String.IsNullOrEmpty(systemConfiguration.HomeGenie.Password))
                return systemConfiguration.HomeGenie.Password;
            return "homegenie";
        }

        private void LoadSystemConfig()
        {
            if (systemConfiguration != null)
                systemConfiguration.OnUpdate -= systemConfiguration_OnUpdate;
            try
            {
                // load config - restore backup if config file is zero-length
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "systemconfig.xml");
                string configBakPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "systemconfig.bak.xml");
                try
                {
                    if (!File.Exists(configPath))
                    {
                        File.Copy(configBakPath, configPath,true);
                    }
                    else
                    {
                        long length = new FileInfo(configPath).Length;
                        if (length == 0 && File.Exists(configBakPath))
                        {
                            File.Copy(configBakPath, configPath,true);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                var serializer = new XmlSerializer(typeof(SystemConfiguration));
                using (var reader = new StreamReader(configPath))
                {
                    systemConfiguration = (SystemConfiguration)serializer.Deserialize(reader);
                    // setup logging
                    if (!string.IsNullOrEmpty(systemConfiguration.HomeGenie.EnableLogFile) && systemConfiguration.HomeGenie.EnableLogFile.ToLower().Equals("true"))
                    {
                        SystemLogger.Instance.OpenLog();
                    }
                    else
                    {
                        SystemLogger.Instance.CloseLog();
                    }
                    // Generate new System UID if required
                    if (String.IsNullOrEmpty(systemConfiguration.HomeGenie.GUID))
                    {
                        systemConfiguration.HomeGenie.GUID = Guid.NewGuid().ToString();
                        systemConfiguration.Update();
                    }
                    // configure MIG
                    migService.Configuration = systemConfiguration.MigService;
                    // Set the password for decrypting settings values and later module parameters
                    systemConfiguration.SetPassPhrase(GetPassPhrase());
                    // decrypt config data
                    foreach (var parameter in systemConfiguration.HomeGenie.Settings)
                    {
                        // TODO: this is duplicate code
                        string stringValue = parameter.Value;
                        try
                        {
                            if (!String.IsNullOrEmpty(stringValue)) {
                                parameter.Value = StringCipher.Decrypt(
                                    stringValue,
                                    GetPassPhrase()
                                );
                            }
                        }
                        catch(Exception ex)
                        {
                            _log.Error(ex);
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
                var serializer = new XmlSerializer(typeof(TsList<Module>));
                using (var reader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "modules.xml")))
                {
                    var modules = (TsList<Module>)serializer.Deserialize(reader);
                    foreach (var module in modules)
                    {
                        foreach (var parameter in module.Properties)
                        {
                            // TODO: this is duplicate code
                            string stringValue = parameter.Value;
                            try
                            {
                                if (!String.IsNullOrEmpty(stringValue)) {
                                    parameter.Value = StringCipher.Decrypt(
                                    stringValue,
                                        GetPassPhrase()
                                    );
                                }
                            }
                            catch(Exception ex)
                            {
                                // ignored
                            }
                        }
                    }
                    modulesGarbage.Clear();
                    // prevent duplicates
                    lock (systemModules.LockObject)
                    {
                        systemModules.Clear();
                        foreach (var m in modules)
                        {
                            Module module = systemModules.Find(o => o.Domain == m.Domain && o.Address == m.Address);
                            if (module == null)
                            {
                                systemModules.Add(m);
                            }
                            else
                            {
                                var warningEvent = new MigEvent(
                                    Domains.HomeAutomation_HomeGenie,
                                    "LoadModules()",
                                    "Ignoring module duplicate",
                                    "SystemMessage.Warning",
                                    m.Domain + ":" + m.Address
                                );
                                MigService.Log.Warn(warningEvent);
                            }
                        }
                    }
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
                if (ip.AddressFamily == AddressFamily.InterNetwork && !ip.ToString().StartsWith("127.") && !ip.ToString().StartsWith("::1"))
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            string address = localIP;
            string bindhost = webGateway.GetOption(WebServiceGatewayOptions.Host).Value;
            string bindport = webGateway.GetOption(WebServiceGatewayOptions.Port).Value;
            if (bindhost.Length > 1)
            {
                address = bindhost;
            }
            //
            string presentationUrl = "http://" + address + ":" + bindport;
            string manufacturer = "G-Labs";
            string manufacturerUrl = "https://github.com/genielabs";
            string modelName = "HomeGenie";
            string modelDescription = "HomeGenie Automation Server";
            string friendlyName = modelName + ": " + Environment.MachineName;
            string modelUrl = "https://homegenie.it/";
            string version = "1";
            if (updateChecker.GetCurrentRelease() != null)
            {
                version = updateChecker.GetCurrentRelease().Version;
            }
            string modelNumber = version;
            string standardDeviceType = "HomeAutomationServer";  // "urn:schemas-glabs-it:device:HomeAutomationServer:1"
            string uniqueDeviceName = systemConfiguration.HomeGenie.GUID;
            //
            var localDevice = UPnPDevice.CreateRootDevice(900, 1, "web\\");
            //hgdevice.Icon = null;
            if (presentationUrl != "")
            {
                localDevice.HasPresentation = true;
                localDevice.PresentationURL = presentationUrl;
            }
            localDevice.FriendlyName = friendlyName;
            localDevice.Manufacturer = manufacturer;
            localDevice.ManufacturerURL = manufacturerUrl;
            localDevice.ModelName = modelName;
            localDevice.ModelDescription = modelDescription;
            if (Uri.IsWellFormedUriString(modelUrl, UriKind.Absolute))
            {
                localDevice.ModelURL = new Uri(modelUrl);
            }
            localDevice.ModelNumber = modelNumber;
            localDevice.StandardDeviceType = standardDeviceType;
            localDevice.UniqueDeviceName = uniqueDeviceName;
            localDevice.StartDevice();
        }

        #endregion
    }
}
