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
using System.Text;
using System.Threading;
using System.Linq;
using System.Globalization;

using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;

using ZWaveLib;

using MIG.Interfaces.HomeAutomation.Commons;
using ZWaveLib.CommandClasses;

namespace MIG.Interfaces.HomeAutomation
{

    public class ZWave : MIGInterface
    {
        #region Private fields

        private ZWaveController controller;

        private ManualResetEvent responseAck = new ManualResetEvent(false);
        private string waitEventPath = "";
        private object waitEventValue = null;
        private object eventLock = new object();

        private byte lastRemovedNode = 0;
        private byte lastAddedNode = 0;

        #endregion

        #region Implemented MIG Commands

        public enum Commands
        {
            Controller_Discovery,
            Controller_NodeAdd,
            Controller_NodeRemove,
            Controller_SoftReset,
            Controller_HardReset,
            Controller_NodeNeighborUpdate,

            Basic_Get,
            Basic_Set,

            MultiInstance_Get,
            MultiInstance_Set,
            MultiInstance_GetCount,

            Battery_Get,

            Association_Get,
            Association_Set,
            Association_Remove,

            ManufacturerSpecific_Get,
            NodeInfo_Get,

            Config_ParameterGet,
            Config_ParameterSet,

            WakeUp_Get,
            WakeUp_Set,

            SensorBinary_Get,
            SensorMultiLevel_Get,

            Meter_Get,
            Meter_SupportedGet,
            Meter_Reset,

            Control_On,
            Control_Off,
            Control_Level,
            Control_Toggle,

            Thermostat_ModeGet,
            Thermostat_ModeSet,
            Thermostat_SetPointGet,
            Thermostat_SetPointSet,
            Thermostat_FanModeGet,
            Thermostat_FanModeSet,
            Thermostat_FanStateGet,
            Thermostat_OperatingStateGet,

            UserCode_Set,

            DoorLock_Set,
            DoorLock_Get
        }

        // z-wave events
        const string EventPath_Basic
           = "ZWaveNode.Basic";
        const string EventPath_WakeUpInterval
            = "ZWaveNode.WakeUpInterval";
        const string EventPath_Battery
            = "ZWaveNode.Battery";
        const string EventPath_MultiInstance
            = "ZWaveNode.MultiInstance";
        const string EventPath_Associations
            = "ZWaveNode.Associations";
        const string EventPath_ConfigVariables
            = "ZWaveNode.Variables";
        const string EventPath_NodeInfo
            = "ZWaveNode.NodeInfo";
        const string EventPath_RoutingInfo
            = "ZWaveNode.RoutingInfo";
        const string EventPath_ManufacturerSpecific
            = "ZWaveNode.ManufacturerSpecific";
        const string EventPath_DoorLock
            = "Status.DoorLock";

        #endregion

        #region MIG Interface members

        public event Action<InterfaceModulesChangedAction> InterfaceModulesChangedAction;
        public event Action<InterfacePropertyChangedAction> InterfacePropertyChangedAction;

        public string Domain
        {
            get
            {
                string domain = this.GetType().Namespace.ToString();
                domain = domain.Substring(domain.LastIndexOf(".") + 1) + "." + this.GetType().Name.ToString();
                return domain;
            }
        }

        public bool IsEnabled { get; set; }

        public List<MIGServiceConfiguration.Interface.Option> Options { get; set; }

        public List<InterfaceModule> GetModules()
        {
            List<InterfaceModule> modules = new List<InterfaceModule>();
            if (controller != null)
            {
                for (int d = 0; d < controller.Nodes.Count; d++)
                {
                    var node = controller.Nodes[d];
                    // add new module
                    InterfaceModule module = new InterfaceModule();
                    module.Domain = this.Domain;
                    module.Address = node.Id.ToString();
                    //module.Description = "ZWave Node";
                    module.ModuleType = ModuleTypes.Generic;
                    if (node.ProtocolInfo.GenericType != (byte)GenericType.None)
                    {
                        switch (node.ProtocolInfo.GenericType)
                        {
                        case (byte)GenericType.StaticController:
                            module.Description = "Static Controller";
                            module.ModuleType = ModuleTypes.Generic;
                            break;

                        case (byte)GenericType.SwitchBinary:
                            module.Description = "Binary Switch";
                            module.ModuleType = ModuleTypes.Switch;
                            break;

                        case (byte)GenericType.SwitchMultilevel:
                            module.Description = "Multilevel Switch";
                            module.ModuleType = ModuleTypes.Dimmer;
                            break;

                        case (byte)GenericType.Thermostat:
                            module.Description = "Thermostat";
                            module.ModuleType = ModuleTypes.Thermostat;
                            break;
                            
                        case (byte)GenericType.SensorAlarm:
                            module.Description = "Alarm Sensor";
                            module.ModuleType = ModuleTypes.Sensor;
                            break;

                        case (byte)GenericType.SensorBinary:
                            module.Description = "Binary Sensor";
                            module.ModuleType = ModuleTypes.Sensor;
                            break;

                        case (byte)GenericType.SensorMultilevel:
                            module.Description = "Multilevel Sensor";
                            module.ModuleType = ModuleTypes.Sensor;
                            break;

                        case (byte)GenericType.Meter:
                            module.Description = "ZWave Meter";
                            module.ModuleType = ModuleTypes.Sensor;
                            break;

                        case (byte)GenericType.EntryControl:
                            module.Description = "ZWave Door Lock";
                            module.ModuleType = ModuleTypes.DoorLock;
                            break;

                        }
                    }
                    modules.Add(module);
                }
            }
            return modules;
        }

        public bool IsConnected
        {
            get
            {
                return (controller.Status == ControllerStatus.Ready);
            }
        }

        public object InterfaceControl(MIGInterfaceCommand request)
        {
            string returnValue = "";
            bool raiseEvent = false;
            string eventParameter = "Status.Level";
            string eventValue = "";

            string nodeId = request.NodeId;
            Commands command;
            Enum.TryParse<Commands>(request.Command.Replace(".", "_"), out command);
            ZWaveNode node = null;

            byte nodeNumber = 0;
            if (byte.TryParse(nodeId, out nodeNumber))
            {
                if (nodeNumber > 0)
                    node = controller.GetNode(nodeNumber);
                lock (eventLock)
                {
                    switch (command)
                    {

                    case Commands.Controller_Discovery:
                        controller.Discovery();
                        break;

                    case Commands.Controller_SoftReset:
                        controller.SoftReset();
                        break;

                    case Commands.Controller_HardReset:
                        controller.HardReset();
                        controller.Discovery();
                        break;

                    case Commands.Controller_NodeNeighborUpdate:
                        controller.RequestNeighborsUpdateOptions(nodeNumber).Wait();
                        controller.RequestNeighborsUpdate(nodeNumber).Wait();
                        controller.GetNeighborsRoutingInfo(nodeNumber).Wait();
                        returnValue = GetResponseValue(EventPath_RoutingInfo);
                        break;

                    case Commands.Controller_NodeAdd:
                        lastAddedNode = 0;
                        controller.BeginNodeAdd();
                        for (int i = 0; i < 20; i++)
                        {
                            if (lastAddedNode > 0)
                            {
                                break;
                            }
                            Thread.Sleep(500);
                        }
                        controller.StopNodeAdd();
                        returnValue = lastAddedNode.ToString();
                        break;

                    case Commands.Controller_NodeRemove:
                        lastRemovedNode = 0;
                        controller.BeginNodeRemove();
                        for (int i = 0; i < 20; i++)
                        {
                            if (lastRemovedNode > 0)
                            {
                                break;
                            }
                            Thread.Sleep(500);
                        }
                        controller.StopNodeRemove();
                        returnValue = lastRemovedNode.ToString();
                        break;

                    case Commands.Basic_Set:
                        {
                            raiseEvent = true;
                            var level = int.Parse(request.GetOption(0));
                            eventValue = level.ToString(CultureInfo.InvariantCulture);
                            Basic.Set(node, (byte)level);
                        }
                        break;

                    case Commands.Basic_Get:
                        Basic.Get(node);
                        returnValue = GetResponseValue(EventPath_Basic);
                        break;

                    case Commands.MultiInstance_GetCount:
                        switch (request.GetOption(0))
                        {
                        case "Switch.Binary":
                            MultiInstance.GetCount(node, (byte)ZWaveLib.CommandClass.SwitchBinary);
                            break;
                        case "Switch.MultiLevel":
                            MultiInstance.GetCount(node, (byte)ZWaveLib.CommandClass.SwitchMultilevel);
                            break;
                        case "Sensor.Binary":
                            MultiInstance.GetCount(node, (byte)ZWaveLib.CommandClass.SensorBinary);
                            break;
                        case "Sensor.MultiLevel":
                            MultiInstance.GetCount(node, (byte)ZWaveLib.CommandClass.SensorMultilevel);
                            break;
                        }
                        returnValue = GetResponseValue(EventPath_MultiInstance + "." + request.GetOption(0) + ".Count");
                        break;

                    case Commands.MultiInstance_Get:
                        {
                            byte instance = (byte)int.Parse(request.GetOption(1));
                            switch (request.GetOption(0))
                            {
                            case "Switch.Binary":
                                MultiInstance.SwitchBinaryGet(node, instance);
                                break;
                            case "Switch.MultiLevel":
                                MultiInstance.SwitchMultiLevelGet(node, instance);
                                break;
                            case "Sensor.Binary":
                                MultiInstance.SensorBinaryGet(node, instance);
                                break;
                            case "Sensor.MultiLevel":
                                MultiInstance.SensorMultiLevelGet(node, instance);
                                break;
                            }
                            returnValue = GetResponseValue(EventPath_MultiInstance + "." + request.GetOption(0) + "." + instance);
                        }
                        break;

                    case Commands.MultiInstance_Set:
                        {
                            byte instance = (byte)int.Parse(request.GetOption(1));
                            int value = int.Parse(request.GetOption(2));
                            //
                            //raisepropchanged = true;
                            //parampath += "." + instance; // Status.Level.<instance>
                            //
                            switch (request.GetOption(0))
                            {
                            case "Switch.Binary":
                                MultiInstance.SwitchBinarySet(node, instance, value);
                            //raiseparam = (double.Parse(request.GetOption(2)) / 255).ToString();
                                break;
                            case "Switch.MultiLevel":
                                MultiInstance.SwitchMultiLevelSet(node, instance, value);
                            //raiseparam = (double.Parse(request.GetOption(2)) / 100).ToString(); // TODO: should it be 99 ?
                                break;
                            }
                        }
                        break;

                    case Commands.SensorBinary_Get:
                        SensorBinary.Get(node);
                        break;

                    case Commands.SensorMultiLevel_Get:
                        SensorMultilevel.Get(node);
                        break;

                    case Commands.Meter_Get:
                    // see ZWaveLib Sensor.cs for EnergyMeterScale options
                        int scaleType = 0;
                        int.TryParse(request.GetOption(0), out scaleType);
                        Meter.Get(node, (byte)(scaleType << 0x03));
                        break;

                    case Commands.Meter_SupportedGet:
                        Meter.GetSupported(node);
                        break;

                    case Commands.Meter_Reset:
                        Meter.Reset(node);
                        break;

                    case Commands.NodeInfo_Get:
                        controller.GetNodeInformationFrame(nodeNumber);
                        break;

                    case Commands.Battery_Get:
                        Battery.Get(node);
                        returnValue = GetResponseValue(EventPath_Battery);
                        break;

                    case Commands.Association_Set:
                        Association.Set(node, (byte)int.Parse(request.GetOption(0)), (byte)int.Parse(request.GetOption(1)));
                        break;

                    case Commands.Association_Get:
                        byte group = (byte)int.Parse(request.GetOption(0));
                        Association.Get(node, group);
                        returnValue = GetResponseValue(EventPath_Associations + "." + group);
                        break;

                    case Commands.Association_Remove:
                        Association.Remove(node, (byte)int.Parse(request.GetOption(0)), (byte)int.Parse(request.GetOption(1)));
                        break;

                    case Commands.ManufacturerSpecific_Get:
                        ManufacturerSpecific.Get(node);
                        returnValue = GetResponseValue(EventPath_ManufacturerSpecific);
                        break;

                    case Commands.Config_ParameterSet:
                        Configuration.Set(node, (byte)int.Parse(request.GetOption(0)), int.Parse(request.GetOption(1)));
                        break;

                    case Commands.Config_ParameterGet:
                        byte position = (byte)int.Parse(request.GetOption(0));
                        Configuration.Get(node, position);
                        returnValue = GetResponseValue(EventPath_ConfigVariables + "." + position);
                        break;

                    case Commands.WakeUp_Get:
                        WakeUp.Get(node);
                        returnValue = GetResponseValue(EventPath_WakeUpInterval);
                        break;

                    case Commands.WakeUp_Set:
                        WakeUp.Set(node, uint.Parse(request.GetOption(0)));
                        break;

                    case Commands.Control_On:
                        raiseEvent = true;
                        eventValue = "1";
                        Basic.Set(node, 0xFF);
                        SetNodeLevel(node, 0xFF);
                        break;

                    case Commands.Control_Off:
                        raiseEvent = true;
                        eventValue = "0";
                        Basic.Set(node, 0x00);
                        SetNodeLevel(node, 0x00);
                        break;

                    case Commands.Control_Level:
                        {
                            raiseEvent = true;
                            var level = int.Parse(request.GetOption(0));
                            eventValue = Math.Round(level / 100D, 2).ToString(CultureInfo.InvariantCulture);
                            // the max value should be obtained from node parameters specifications,
                            // here we assume that the commonly used interval is [0-99] for most multilevel switches
                            if (level >= 100)
                                level = 99;
                            if (node.SupportCommandClass(CommandClass.SwitchMultilevel))
                                SwitchMultilevel.Set(node, (byte)level);
                            else
                                Basic.Set(node, (byte)level);
                            SetNodeLevel(node, (byte)level);
                        }
                        break;

                    case Commands.Control_Toggle:
                        raiseEvent = true;
                        if (GetNodeLevel(node) == 0)
                        {
                            eventValue = "1";
                            Basic.Set(node, 0xFF);
                            SetNodeLevel(node, 0xFF);
                        }
                        else
                        {
                            eventValue = "0";
                            Basic.Set(node, 0x00);
                            SetNodeLevel(node, 0x00);
                        }
                        break;

                    case Commands.Thermostat_ModeGet:
                        ThermostatMode.Get(node);
                        break;

                    case Commands.Thermostat_ModeSet:
                        {
                            ThermostatMode.Value mode = (ThermostatMode.Value)Enum.Parse(typeof(ThermostatMode.Value), request.GetOption(0));
                            //
                            raiseEvent = true;
                            eventParameter = "Thermostat.Mode";
                            eventValue = request.GetOption(0);
                            //
                            ThermostatMode.Set(node, mode);
                        }
                        break;

                    case Commands.Thermostat_SetPointGet:
                        {
                            ThermostatSetPoint.Value mode = (ThermostatSetPoint.Value)Enum.Parse(typeof(ThermostatSetPoint.Value), request.GetOption(0));
                            ThermostatSetPoint.Get(node, mode);
                        }
                        break;

                    case Commands.Thermostat_SetPointSet:
                        {
                            ThermostatSetPoint.Value mode = (ThermostatSetPoint.Value)Enum.Parse(typeof(ThermostatSetPoint.Value), request.GetOption(0));
                            double temperature = double.Parse(request.GetOption(1).Replace(',', '.'), CultureInfo.InvariantCulture);
                            //
                            raiseEvent = true;
                            eventParameter = "Thermostat.SetPoint." + request.GetOption(0);
                            eventValue = temperature.ToString(CultureInfo.InvariantCulture);
                            //
                            ThermostatSetPoint.Set(node, mode, temperature);
                        }
                        break;

                    case Commands.Thermostat_FanModeGet:
                        ThermostatFanMode.Get(node);
                        break;

                    case Commands.Thermostat_FanModeSet:
                        {
                            ThermostatFanMode.Value mode = (ThermostatFanMode.Value)Enum.Parse(typeof(ThermostatFanMode.Value), request.GetOption(0));
                            //
                            raiseEvent = true;
                            eventParameter = "Thermostat.FanMode";
                            eventValue = request.GetOption(0);
                            //
                            ThermostatFanMode.Set(node, mode);
                        }
                        break;

                    case Commands.Thermostat_FanStateGet:
                        ThermostatFanState.Get(node);
                        break;

                    case Commands.Thermostat_OperatingStateGet:
                        ThermostatOperatingState.GetOperatingState(node);
                        break;

                    case Commands.UserCode_Set:
                        byte userId = byte.Parse(request.GetOption(0));
                        byte userIdStatus = byte.Parse(request.GetOption(1));
                        byte[] tagCode = ZWaveLib.Utility.HexStringToByteArray(request.GetOption(2));
                        UserCode.Set(node, new ZWaveLib.Values.UserCodeValue(userId, userIdStatus, tagCode));
                        break;

                    case Commands.DoorLock_Get:
                        DoorLock.Get(node);
                        returnValue = GetResponseValue(EventPath_DoorLock);
                        break;

                    case Commands.DoorLock_Set:
                        {
                            DoorLock.Value mode = (DoorLock.Value)Enum.Parse(typeof(DoorLock.Value), request.GetOption(0));
                            DoorLock.Set(node, mode);
                        }
                        break;
                    }
                }
            }

            if (raiseEvent && InterfacePropertyChangedAction != null)
            {
                //ZWaveNode node = _controller.GetDevice ((byte)int.Parse (nodeid));
                InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = nodeId,
                    SourceType = "ZWave Node",
                    Path = eventParameter,
                    Value = eventValue
                });
            }
            //
            return returnValue;
        }

        public bool Connect()
        {
            controller.PortName = this.GetOption("Port").Value;
            controller.Connect();
            return true;
        }

        public void Disconnect()
        {
            controller.Disconnect();
        }

        public bool IsDevicePresent()
        {
            /*
            List<USBDeviceInfo> devices = new List<USBDeviceInfo>();

            ManagementObjectCollection collection;
            using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_USBHub"))
            collection = searcher.Get();      

            foreach (var device in collection)
            {
            devices.Add(new USBDeviceInfo(
                (string)device.GetPropertyValue("DeviceID"),
                (string)device.GetPropertyValue("PNPDeviceID"),
                (string)device.GetPropertyValue("Description")
            ));
            }

            collection.Dispose();
            */
            //bool present = false;
            ////
            //Console.WriteLine(LibUsbDevice.LegacyLibUsbDeviceList.Count);
            //foreach (UsbRegistry usbdev in LibUsbDevice.AllDevices)
            //{
            //    if ((usbdev.Vid == 0x10C4 && usbdev.Pid == 0xEA60) || usbdev.FullName.ToUpper().Contains("CP2102"))
            //    {
            //        present = true;
            //        break;
            //    }
            //}
            //return present;
            return true;
        }

        #endregion

        #region Public members

        public ZWave()
        {
            controller = new ZWaveController();
            controller.ControllerStatusChanged += Controller_ControllerStatusChanged;
            ;
            controller.DiscoveryProgress += Controller_DiscoveryProgress;
            controller.NodeOperationProgress += Controller_NodeOperationProgress;
            controller.NodeUpdated += Controller_NodeUpdated;            
        }


        #endregion

        #region Private members

        /*
        class USBDeviceInfo
        {
            public USBDeviceInfo(string deviceID, string pnpDeviceID, string description)
            {
                this.DeviceID = deviceID;
                this.PnpDeviceID = pnpDeviceID;
                this.Description = description;
            }
            public string DeviceID { get; private set; }
            public string PnpDeviceID { get; private set; }
            public string Description { get; private set; }
        }
        */


        private void Controller_ControllerStatusChanged(object sender, ControllerStatusEventArgs args)
        {
            var controller = (sender as ZWaveController);
            switch (args.Status)
            {
            case ControllerStatus.Connected:
                // Initialize the controller and get the node list
                controller.Initialize();
                break;
            case ControllerStatus.Disconnected:
                break;
            case ControllerStatus.Initializing:
                break;
            case ControllerStatus.Ready:
                // Query all nodes (Basic Classes, Node Information Frame, Manufacturer Specific, Command Class version)
                controller.Discovery();
                break;
            case ControllerStatus.Error:
                controller.Connect();
                break;
            }
        }

        private void Controller_NodeOperationProgress(object sender, NodeOperationProgressEventArgs args)
        {
            // this will fire on a node operation such as Add, Remove, Updating Routing, etc..
            switch (args.Status)
            {
            case NodeQueryStatus.NodeAdded:
                RaisePropertyChanged(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = "1",
                    SourceType = "Z-Wave Controller",
                    Path = "Controller.Status",
                    Value = "Added node " + args.NodeId
                });
                lastAddedNode = args.NodeId;
                if (InterfaceModulesChangedAction != null)
                    InterfaceModulesChangedAction(new InterfaceModulesChangedAction(){ Domain = this.Domain });
                break;
            case NodeQueryStatus.NodeUpdated:
                RaisePropertyChanged(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = "1",
                    SourceType = "Z-Wave Controller",
                    Path = "Controller.Status",
                    Value = "Updated node " + args.NodeId
                });
                //if (InterfaceModulesChangedAction != null) InterfaceModulesChangedAction(new InterfaceModulesChangedAction(){ Domain = this.Domain });
                break;
            case NodeQueryStatus.NodeRemoved:
                lastRemovedNode = args.NodeId;
                RaisePropertyChanged(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = "1",
                    SourceType = "Z-Wave Controller",
                    Path = "Controller.Status",
                    Value = "Removed node " + args.NodeId
                });
                if (InterfaceModulesChangedAction != null)
                    InterfaceModulesChangedAction(new InterfaceModulesChangedAction(){ Domain = this.Domain });
                break;
            case NodeQueryStatus.Timeout:
                RaisePropertyChanged(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = "1",
                    SourceType = "Z-Wave Controller",
                    Path = "Controller.Status",
                    Value = "Node " + args.NodeId + " response timeout!"
                });
                break;
            default:
                RaisePropertyChanged(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = "1",
                    SourceType = "Z-Wave Controller",
                    Path = "Controller.Status",
                    Value = args.Status.ToString()
                });
                break;
            }
        }

        private void Controller_DiscoveryProgress(object sender, DiscoveryProgressEventArgs args)
        {
            //var controller = (sender as ZWaveController);
            switch (args.Status)
            {
            case DiscoveryStatus.DiscoveryStart:
                RaisePropertyChanged(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = "1",
                    SourceType = "Z-Wave Controller",
                    Path = "Controller.Status",
                    Value = "Discovery Started"
                });
                break;
            case DiscoveryStatus.DiscoveryEnd:
                RaisePropertyChanged(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = "1",
                    SourceType = "Z-Wave Controller",
                    Path = "Controller.Status",
                    Value = "Discovery Complete"
                });
                if (InterfaceModulesChangedAction != null)
                    InterfaceModulesChangedAction(new InterfaceModulesChangedAction(){ Domain = this.Domain });
                break;
            }
        }


        private void Controller_NodeUpdated(object sender, NodeUpdatedEventArgs args)
        {
            var eventData = args.Event;
            while (eventData != null)
            {
                string eventPath = "UnknwonParameter";
                object eventValue = eventData.Value;
                switch (eventData.Parameter)
                {
                case EventParameter.MeterKwHour:
                    eventPath = GetIndexedParameterPath(ModuleParameters.MODPAR_METER_KW_HOUR, eventData.Instance);
                    break;
                case EventParameter.MeterKvaHour:
                    eventPath = GetIndexedParameterPath(ModuleParameters.MODPAR_METER_KVA_HOUR, eventData.Instance);
                    break;
                case EventParameter.MeterWatt:
                    eventPath = GetIndexedParameterPath(ModuleParameters.MODPAR_METER_WATTS, eventData.Instance);
                    break;
                case EventParameter.MeterPulses:
                    eventPath = GetIndexedParameterPath(ModuleParameters.MODPAR_METER_PULSES, eventData.Instance);
                    break;
                case EventParameter.MeterAcVolt:
                    eventPath = GetIndexedParameterPath(ModuleParameters.MODPAR_METER_AC_VOLT, eventData.Instance);
                    break;
                case EventParameter.MeterAcCurrent:
                    eventPath = GetIndexedParameterPath(ModuleParameters.MODPAR_METER_AC_CURRENT, eventData.Instance);
                    break;
                case EventParameter.MeterPower:
                    eventPath = GetIndexedParameterPath(ModuleParameters.MODPAR_SENSOR_POWER, eventData.Instance);
                    break;
                case EventParameter.Battery:
                    RaisePropertyChanged(new InterfacePropertyChangedAction() {
                        Domain = this.Domain,
                        SourceId = eventData.Node.Id.ToString(),
                        SourceType = "ZWave Node",
                        Path = EventPath_Battery,
                        Value = eventValue
                    });
                    eventPath = ModuleParameters.MODPAR_STATUS_BATTERY;
                    break;
                case EventParameter.NodeInfo:
                    eventPath = EventPath_NodeInfo;
                    break;
                case EventParameter.RoutingInfo:
                    eventPath = EventPath_RoutingInfo;
                    break;
                case EventParameter.SensorGeneric:
                    eventPath = ModuleParameters.MODPAR_SENSOR_GENERIC;
                    break;
                case EventParameter.SensorTemperature:
                    eventPath = ModuleParameters.MODPAR_SENSOR_TEMPERATURE;
                    break;
                case EventParameter.SensorHumidity:
                    eventPath = ModuleParameters.MODPAR_SENSOR_HUMIDITY;
                    break;
                case EventParameter.SensorLuminance:
                    eventPath = ModuleParameters.MODPAR_SENSOR_LUMINANCE;
                    break;
                case EventParameter.SensorMotion:
                    eventPath = ModuleParameters.MODPAR_SENSOR_MOTIONDETECT;
                    break;
                case EventParameter.AlarmGeneric:
                    eventPath = ModuleParameters.MODPAR_SENSOR_ALARM_GENERIC;
                    // Translate generic alarm into specific Door Lock event values if node is an entry control type device
                    //at this level the sender is the controller so get the node from eventData
                    if (eventData.Node.ProtocolInfo.GenericType == (byte)GenericType.EntryControl)
                    {
                        eventPath = EventPath_DoorLock;
                        //! do not convert to string since Alarms accept ONLY numbers a string would be outputed as NaN
                        //! for now let it as is.
                        //eventValue = ((DoorLock.Alarm)(byte)value).ToString();
                    }
                    break;
                case EventParameter.AlarmDoorWindow:
                    eventPath = ModuleParameters.MODPAR_SENSOR_DOORWINDOW;
                    break;
                case EventParameter.AlarmTampered:
                    eventPath = ModuleParameters.MODPAR_SENSOR_TAMPER;
                    break;
                case EventParameter.AlarmSmoke:
                    eventPath = ModuleParameters.MODPAR_SENSOR_ALARM_SMOKE;
                    break;
                case EventParameter.AlarmCarbonMonoxide:
                    eventPath = ModuleParameters.MODPAR_SENSOR_ALARM_CARBONMONOXIDE;
                    break;
                case EventParameter.AlarmCarbonDioxide:
                    eventPath = ModuleParameters.MODPAR_SENSOR_ALARM_CARBONDIOXIDE;
                    break;
                case EventParameter.AlarmHeat:
                    eventPath = ModuleParameters.MODPAR_SENSOR_ALARM_HEAT;
                    break;
                case EventParameter.AlarmFlood:
                    eventPath = ModuleParameters.MODPAR_SENSOR_ALARM_FLOOD;
                    break;
                case EventParameter.DoorLockStatus:
                    eventPath = ModuleParameters.MODPAR_STATUS_DOORLOCK;
                    eventValue = ((DoorLock.Value)(byte)eventValue).ToString();
                    break;
                case EventParameter.ManufacturerSpecific:
                    ManufacturerSpecificInfo mf = (ManufacturerSpecificInfo)eventValue;
                    eventPath = EventPath_ManufacturerSpecific;
                    eventValue = mf.ManufacturerId + ":" + mf.TypeId + ":" + mf.ProductId;
                    break;
                case EventParameter.Configuration:
                    eventPath = EventPath_ConfigVariables + "." + eventData.Instance;
                    break;
                case EventParameter.Association:
                    var associationResponse = (Association.AssociationResponse)eventValue;
                    RaisePropertyChanged(new InterfacePropertyChangedAction() {
                        Domain = this.Domain,
                        SourceId = eventData.Node.Id.ToString(),
                        SourceType = "ZWave Node",
                        Path = EventPath_Associations + ".Max",
                        Value = associationResponse.Max
                    });
                    RaisePropertyChanged(new InterfacePropertyChangedAction() {
                        Domain = this.Domain,
                        SourceId = eventData.Node.Id.ToString(),
                        SourceType = "ZWave Node",
                        Path = EventPath_Associations + ".Count",
                        Value = associationResponse.Count
                    });
                    eventPath = EventPath_Associations + "." + associationResponse.GroupId; // TODO: implement generic group/node association instead of fixed one
                    eventValue = associationResponse.NodeList;
                    break;
                case EventParameter.MultiinstanceSwitchBinaryCount:
                    eventPath = EventPath_MultiInstance + ".SwitchBinary.Count";
                    break;
                case EventParameter.MultiinstanceSwitchMultilevelCount:
                    eventPath = EventPath_MultiInstance + ".SwitchMultiLevel.Count";
                    break;
                case EventParameter.MultiinstanceSensorBinaryCount:
                    eventPath = EventPath_MultiInstance + ".SensorBinary.Count";
                    break;
                case EventParameter.MultiinstanceSensorMultilevelCount:
                    eventPath = EventPath_MultiInstance + ".SensorMultiLevel.Count";
                    break;
                case EventParameter.MultiinstanceSwitchBinary:
                    eventPath = EventPath_MultiInstance + ".SwitchBinary." + eventData.Instance;
                    break;
                case EventParameter.MultiinstanceSwitchMultilevel:
                    eventPath = EventPath_MultiInstance + ".SwitchMultiLevel." + eventData.Instance;
                    break;
                case EventParameter.MultiinstanceSensorBinary:
                    eventPath = EventPath_MultiInstance + ".SensorBinary." + eventData.Instance;
                    break;
                case EventParameter.MultiinstanceSensorMultilevel:
                    eventPath = EventPath_MultiInstance + ".SensorMultiLevel." + eventData.Instance;
                    break;
                case EventParameter.WakeUpInterval:
                    eventPath = EventPath_WakeUpInterval;
                    break;
                case EventParameter.WakeUpNotify:
                    eventPath = "ZWaveNode.WakeUpNotify";
                    break;
                case EventParameter.Level:
                    eventPath = EventPath_Basic;
                    // binary switches have [0/255], while multilevel switches [0-99],
                    // normalize Status.Level to [0.0 <-> 1.0]
                    double normalizedval = (Math.Round((double)eventValue / 100D, 2));
                    if (normalizedval >= 0.99)
                        normalizedval = 1.0;
                    RaisePropertyChanged(new InterfacePropertyChangedAction() {
                        Domain = this.Domain,
                        SourceId = eventData.Node.Id.ToString(),
                        SourceType = "ZWave Node",
                        Path = ModuleParameters.MODPAR_STATUS_LEVEL + (eventData.Instance == 0 ? "" : "." + eventData.Instance),
                        Value = normalizedval.ToString(CultureInfo.InvariantCulture)
                    });
                    break;
                case EventParameter.ThermostatMode:
                    eventPath = "Thermostat.Mode";
                    eventValue = ((ThermostatMode.Value)eventValue).ToString();
                    break;
                case EventParameter.ThermostatOperatingState:
                    eventPath = "Thermostat.OperatingState";
                    eventValue = ((ThermostatOperatingState.Value)eventValue).ToString();
                    break;
                case EventParameter.ThermostatFanMode:
                    eventPath = "Thermostat.FanMode";
                    eventValue = ((ThermostatFanMode.Value)eventValue).ToString();
                    break;
                case EventParameter.ThermostatFanState:
                    eventPath = "Thermostat.FanState";
                    eventValue = ((ThermostatFanState.Value)eventValue).ToString();
                    break;
                case EventParameter.ThermostatHeating:
                    eventPath = "Thermostat.Heating";
                    break;
                case EventParameter.ThermostatSetBack:
                    eventPath = "Thermostat.SetBack";
                    break;
                case EventParameter.ThermostatSetPoint:
                    // value stores a dynamic object with Type and Value fields: value = { Type = ..., Value = ... }
                    eventPath = "Thermostat.SetPoint." + ((ThermostatSetPoint.Value)((dynamic)eventValue).Type).ToString();
                    eventValue = ((dynamic)eventValue).Value;
                    break;
                case EventParameter.UserCode:
                    eventPath = "EntryControl.UserCode";
                    eventValue = ((ZWaveLib.Values.UserCodeValue)eventValue).TagCodeToHexString();
                    break;
                case EventParameter.SecurityNodeInformationFrame:
                    eventPath = "ZWaveNode.SecuredNodeInfo";
                    break;
                default:
                    Console.WriteLine("UNHANDLED PARAMETER CHANGE FROM NODE {0} ====> Param Type: {1} Param Id:{2} Value:{3}", eventData.Node.Id, eventData.Parameter, eventData.Instance, eventValue);
                    break;
                }

                if (waitEventPath == eventPath)
                {
                    waitEventValue = eventValue;
                    responseAck.Set();
                }

                RaisePropertyChanged(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = eventData.Node.Id.ToString(),
                    SourceType = "ZWave Node",
                    Path = eventPath,
                    Value = eventValue
                });

                eventData = eventData.NestedEvent;
            }
        }

        private string GetResponseValue(string eventPath)
        {
            waitEventPath = eventPath;
            responseAck.Reset();
            responseAck.WaitOne(ZWaveMessage.SendMessageTimeoutMs);
            string returnValue = "[{ \"ResponseValue\" : \"ERR_TIMEOUT\" }]";
            if (waitEventValue != null)
            {
                returnValue = "[{ \"ResponseValue\" : \"" + waitEventValue + "\" }]";
            }
            waitEventPath = "";
            waitEventValue = null;
            return returnValue;
        }

        private string GetIndexedParameterPath(string basePath, int parameterId)
        {
            if (parameterId > 0)
            {
                basePath += "." + parameterId;
            }
            return basePath;
        }

        private void SetNodeLevel(ZWaveNode node, int level)
        {
            node.UpdateData("Level", level);
        }

        private int GetNodeLevel(ZWaveNode node)
        {
            return (int)node.GetData("Level", 0).Value;
        }

        private void RaisePropertyChanged(InterfacePropertyChangedAction ifaceaction)
        {
            if (InterfacePropertyChangedAction != null)
            {
                try
                {
                    InterfacePropertyChangedAction(ifaceaction);
                }
                catch
                {
                }
            }
        }

        #endregion

    }

}
