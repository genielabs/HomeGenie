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
using ZWaveLib.Handlers;

namespace MIG.Interfaces.HomeAutomation
{

    public class ZWave : MIGInterface
    {
        #region Private fields

        private ZWavePort zwavePort;
        private Controller controller;

        private object syncLock = new object();
        private byte lastRemovedNode = 0;
        private byte lastAddedNode = 0;

        #endregion
        
        #region Implemented MIG Commands

        // typesafe enum
        public sealed class Command : GatewayCommand
        {

            public static Dictionary<int, string> CommandsList = new Dictionary<int, string>() {
                { 101, "Controller.Discovery" },
                { 111, "Controller.NodeAdd" },
                { 113, "Controller.NodeRemove" },
                { 131, "Controller.SoftReset" },
                { 132, "Controller.HardReset" },

                { 201, "Basic.Get" },
                { 202, "Basic.Set" },

                { 211, "MultiInstance.Get" },
                { 212, "MultiInstance.Set" },
                { 213, "MultiInstance.GetCount" },

                { 251, "Battery.Get" },

                { 301, "Association.Get" },
                { 302, "Association.Set" },
                { 303, "Association.Remove" },

                { 401, "ManufacturerSpecific.Get" },
                { 402, "NodeInfo.Get" },

                { 451, "Config.ParameterGet" },
                { 452, "Config.ParameterSet" },

                { 501, "WakeUp.Get" },
                { 502, "WakeUp.Set" },

                { 601, "SensorBinary.Get" },
                { 602, "SensorMultiLevel.Get" },
                { 605, "Meter.Get" },
                { 606, "Meter.SupportedGet" },
                { 607, "Meter.Reset" },

                { 701, "Control.On" },
                { 702, "Control.Off" },
                { 705, "Control.Level" },
                { 706, "Control.Toggle" },

                { 801, "Thermostat.ModeGet" },
                { 802, "Thermostat.ModeSet" },
                { 803, "Thermostat.SetPointGet" },
                { 804, "Thermostat.SetPointSet" },
                { 805, "Thermostat.FanModeGet" },
                { 806, "Thermostat.FanModeSet" },
                { 807, "Thermostat.FanStateGet" },
                { 808, "Thermostat.GetAll" },
                { 809, "Thermostat.OperatingStateGet" },

                { 901, "UserCode.Set" },

                { 1000, "NodeInfo.Get" },
            };

            // <context>.<command> enum   -   eg. Control.On where <context> :== "Control" and <command> :== "On"
            public static readonly Command CONTROLLER_DISCOVERY = new Command(101);
            public static readonly Command CONTROLLER_NODEADD = new Command(111);
            public static readonly Command CONTROLLER_NODEREMOVE = new Command(113);
            public static readonly Command CONTROLLER_SOFTRESET = new Command(131);
            public static readonly Command CONTROLLER_HARDRESET = new Command(132);

            public static readonly Command BASIC_GET = new Command(201);
            public static readonly Command BASIC_SET = new Command(202);

            public static readonly Command MULTIINSTANCE_GET = new Command(211);
            public static readonly Command MULTIINSTANCE_SET = new Command(212);
            public static readonly Command MULTIINSTANCE_GETCOUNT = new Command(213);

            public static readonly Command BATTERY_GET = new Command(251);

            public static readonly Command ASSOCIATION_GET = new Command(301);
            public static readonly Command ASSOCIATION_SET = new Command(302);
            public static readonly Command ASSOCIATION_REMOVE = new Command(303);

            public static readonly Command MANUFACTURERSPECIFIC_GET = new Command(401);
            public static readonly Command NODEINFO_GET = new Command(402);

            public static readonly Command CONFIG_PARAMETERGET = new Command(451);
            public static readonly Command CONFIG_PARAMETERSET = new Command(452);

            public static readonly Command WAKEUP_GET = new Command(501);
            public static readonly Command WAKEUP_SET = new Command(502);

            public static readonly Command SENSORBINARY_GET = new Command(601);
            public static readonly Command SENSORMULTILEVEL_GET = new Command(602);
            public static readonly Command METER_GET = new Command(605);
            public static readonly Command METER_SUPPORTEDGET = new Command(606);
            public static readonly Command METER_RESET = new Command(607);

            public static readonly Command CONTROL_ON = new Command(701);
            public static readonly Command CONTROL_OFF = new Command(702);
            public static readonly Command CONTROL_LEVEL = new Command(705);
            public static readonly Command CONTROL_TOGGLE = new Command(706);

            public static readonly Command THERMOSTAT_MODEGET = new Command(801);
            public static readonly Command THERMOSTAT_MODESET = new Command(802);
            public static readonly Command THERMOSTAT_SETPOINTGET = new Command(803);
            public static readonly Command THERMOSTAT_SETPOINTSET = new Command(804);
            public static readonly Command THERMOSTAT_FANMODEGET = new Command(805);
            public static readonly Command THERMOSTAT_FANMODESET = new Command(806);
            public static readonly Command THERMOSTAT_FANSTATEGET = new Command(807);
            public static readonly Command THERMOSTAT_GETALL = new Command(808);
            public static readonly Command THERMOSTAT_OPERATINGSTATE_GET = new Command(809);

            public static readonly Command USERCODE_SET = new Command(901);

            private readonly String name;
            private readonly int value;

            private Command(int value)
            {
                this.name = CommandsList[value];
                this.value = value;
            }

            public Dictionary<int, string> ListCommands()
            {
                return Command.CommandsList;
            }

            public int Value
            {
                get { return this.value; }
            }

            public override String ToString()
            {
                return name;
            }

            public static implicit operator String(Command a)
            {
                return a.ToString();
            }

            public static explicit operator Command(int idx)
            {
                return new Command(idx);
            }

            public static explicit operator Command(string str)
            {
                if (CommandsList.ContainsValue(str))
                {
                    var cmd = from c in CommandsList
                        where c.Value == str
                            select c.Key;
                    return new Command(cmd.First());
                }
                else
                {
                    throw new InvalidCastException();
                }
            }

            public static bool operator ==(Command a, Command b)
            {
                return a.value == b.value;
            }

            public static bool operator !=(Command a, Command b)
            {
                return a.value != b.value;
            }
        }

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

        public List<MIGServiceConfiguration.Interface.Option> Options { get; set; }

        public List<InterfaceModule> GetModules()
        {
            List<InterfaceModule> modules = new List<InterfaceModule>();
            if (controller != null)
            {
                for(int d = 0; d < controller.Devices.Count; d++)
                {
                    var node = controller.Devices[d];
                    if (node.NodeId == 0x01) // main zwave controller
                        continue;
                    //
                    // add new module
                    InterfaceModule module = new InterfaceModule();
                    module.Domain = this.Domain;
                    module.Address = node.NodeId.ToString();
                    //module.Description = "ZWave Node";
                    module.ModuleType = ModuleTypes.Generic;
                    if (node.GenericClass != (byte)GenericType.None)
                    {
                        switch (node.GenericClass)
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
                if (zwavePort != null) return zwavePort.IsConnected;
                else return false;
            }
        }

        public object InterfaceControl(MIGInterfaceCommand request)
        {
            string returnValue = "";
            bool raiseEvent = false;
            string eventParameter = "Status.Level";
            string eventValue = "";
            //
            string nodeId = request.NodeId;
            Command command = (Command)request.Command;
            ////----------------------
            /// 
            lock(syncLock)
            try
            {
                if (command == Command.CONTROLLER_DISCOVERY)
                {
                    controller.Discovery();
                }
                else if (command == Command.CONTROLLER_SOFTRESET)
                {
                    controller.SoftReset();
                }
                else if (command == Command.CONTROLLER_HARDRESET)
                {
                    controller.HardReset();
                    Thread.Sleep(500);
                    controller.Discovery();
                }
                else if (command == Command.CONTROLLER_NODEADD)
                {
                    lastAddedNode = 0;
                    byte addedId = controller.BeginNodeAdd();
                    for (int i = 0; i < 20; i++)
                    {
                        if (lastAddedNode > 0)
                        {
                            break;
                        }
                        Thread.Sleep(500);
                    }
                    controller.StopNodeAdd();
                    //
                    returnValue = lastAddedNode.ToString();
                }
                else if (command == Command.CONTROLLER_NODEREMOVE)
                {
                    lastRemovedNode = 0;
                    byte remcid = controller.BeginNodeRemove();
                    for (int i = 0; i < 20; i++)
                    {
                        if (lastRemovedNode > 0)
                        {
                            break;
                        }
                        Thread.Sleep(500);
                    }
                    controller.StopNodeRemove();
                    //
                    returnValue = lastRemovedNode.ToString();
                }
                ////----------------------
                else if (command == Command.BASIC_SET)
                {
                    raiseEvent = true;
                    //raiseValue = Math.Round(double.Parse(request.GetOption(0)) / 99D, 2);
                    //if (raiseValue >= 0.99) raiseValue = 1;
                    var level = int.Parse(request.GetOption(0));
                    eventValue = level.ToString(CultureInfo.InvariantCulture);
                    //
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    Basic.Set(node, (byte)level);
                }
                else if (command == Command.BASIC_GET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    Basic.Get(node);
                }
                ////-----------------------
                else if (command == Command.MULTIINSTANCE_GETCOUNT)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    //
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
                }
                else if (command == Command.MULTIINSTANCE_GET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    byte instance = (byte)int.Parse(request.GetOption(1)); // parameter index
                    //
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
                }
                else if (command == Command.MULTIINSTANCE_SET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    byte instance = (byte)int.Parse(request.GetOption(1)); // parameter index
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
                else if (command == Command.SENSORBINARY_GET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    SensorBinary.Get(node);
                }
                else if (command == Command.SENSORMULTILEVEL_GET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    SensorMultilevel.Get(node);
                }
                else if (command == Command.METER_GET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    // see ZWaveLib Sensor.cs for EnergyMeterScale options
                    int scaleType = 0; int.TryParse(request.GetOption(0), out scaleType);
                    Meter.Get(node, (byte)(scaleType << 0x03));
                }
                else if (command == Command.METER_SUPPORTEDGET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    Meter.GetSupported(node);
                }
                else if (command == Command.METER_RESET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    Meter.Reset(node);
                }
                else if (command == Command.NODEINFO_GET)
                {
                    controller.GetNodeInformationFrame((byte)int.Parse(nodeId));
                }
                ////-----------------------
                else if (command == Command.BATTERY_GET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    Battery.Get(node);
                }
                ////-----------------------
                else if (command == Command.ASSOCIATION_SET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    Association.Set(node, (byte)int.Parse(request.GetOption(0)), (byte)int.Parse(request.GetOption(1)));
                }
                else if (command == Command.ASSOCIATION_GET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    Association.Get(node, (byte)int.Parse(request.GetOption(0))); // groupid
                }
                else if (command == Command.ASSOCIATION_REMOVE)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    Association.Remove(node, (byte)int.Parse(request.GetOption(0)), (byte)int.Parse(request.GetOption(1))); // groupid
                }
                ////-----------------------
                else if (command == Command.MANUFACTURERSPECIFIC_GET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    ManufacturerSpecific.Get(node);
                }
                ////------------------
                else if (command == Command.CONFIG_PARAMETERSET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    //byte[] value = new byte[] { (byte)int.Parse(option1) };//BitConverter.GetBytes(Int16.Parse(option1));
                    //Array.Reverse(value);
                    Configuration.Set(node, (byte)int.Parse(request.GetOption(0)), int.Parse(request.GetOption(1)));
                }
                else if (command == Command.CONFIG_PARAMETERGET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    Configuration.Get(node, (byte)int.Parse(request.GetOption(0)));
                }
                ////------------------
                else if (command == Command.WAKEUP_GET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    WakeUp.Get(node);
                }
                else if (command == Command.WAKEUP_SET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    WakeUp.Set(node, uint.Parse(request.GetOption(0)));
                }
                ////------------------
                else if (command == Command.CONTROL_ON)
                {
                    raiseEvent = true;
                    eventValue = "1";
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    Basic.Set(node, 0xFF);
                    SetNodeLevel(node, 0xFF);
                }
                else if (command == Command.CONTROL_OFF)
                {
                    raiseEvent = true;
                    eventValue = "0";
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    Basic.Set(node, 0x00);
                    SetNodeLevel(node, 0x00);
                }
                else if (command == Command.CONTROL_LEVEL)
                {
                    raiseEvent = true;
                    var level = int.Parse(request.GetOption(0));
                    eventValue = Math.Round(level / 100D, 2).ToString(CultureInfo.InvariantCulture);
                    // the max value should be obtained from node parameters specifications,
                    // here we assume that the commonly used interval is [0-99] for most multilevel switches
                    if (level >= 100) level = 99;
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    Basic.Set(node, (byte)level);
                    SetNodeLevel(node, (byte)level);
                }
                else if (command == Command.CONTROL_TOGGLE)
                {
                    raiseEvent = true;
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
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
                }
                else if (command == Command.THERMOSTAT_MODEGET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    Thermostat.GetMode(node);
                }
                else if (command == Command.THERMOSTAT_MODESET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    Mode mode = (Mode)Enum.Parse(typeof(Mode), request.GetOption(0));
                    //
                    raiseEvent = true;
                    eventParameter = "Thermostat.Mode";
                    eventValue = request.GetOption(0);
                    //
                    Thermostat.SetMode(node, mode);
                }
                else if (command == Command.THERMOSTAT_SETPOINTGET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    SetPointType mode = (SetPointType)Enum.Parse(typeof(SetPointType), request.GetOption(0));
                    Thermostat.GetSetPoint(node, mode);
                }
                else if (command == Command.THERMOSTAT_SETPOINTSET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    SetPointType mode = (SetPointType)Enum.Parse(typeof(SetPointType), request.GetOption(0));
                    double temperature = double.Parse(request.GetOption(1).Replace(',', '.'), CultureInfo.InvariantCulture);
                    //
                    raiseEvent = true;
                    eventParameter = "Thermostat.SetPoint." + request.GetOption(0);
                    eventValue = temperature.ToString(CultureInfo.InvariantCulture);
                    //
                    Thermostat.SetSetPoint(node, mode, temperature);
                }
                else if (command == Command.THERMOSTAT_FANMODEGET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    Thermostat.GetFanMode(node);
                }
                else if (command == Command.THERMOSTAT_FANMODESET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    FanMode mode = (FanMode)Enum.Parse(typeof(FanMode), request.GetOption(0));
                    //
                    raiseEvent = true;
                    eventParameter = "Thermostat.FanMode";
                    eventValue = request.GetOption(0);
                    //
                    Thermostat.SetFanMode(node, mode);
                }
                else if (command == Command.THERMOSTAT_FANSTATEGET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    Thermostat.GetFanState(node);
                }
                else if (command == Command.THERMOSTAT_OPERATINGSTATE_GET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    ThermostatOperatingState.GetOperatingState(node);
                }
                else if(command==Command.USERCODE_SET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    byte userId = byte.Parse(request.GetOption(0));
                    byte userIdStatus = byte.Parse(request.GetOption(1));
                    byte[] tagCode = ZWaveLib.Utility.HexStringToByteArray(request.GetOption(2));
                    UserCode.Set(node, new ZWaveLib.Values.UserCodeValue(userId, userIdStatus, tagCode));
                }
            }
            catch
            {
                if (eventValue != "") raiseEvent = true;
            }
            //
            if (raiseEvent && InterfacePropertyChangedAction != null)
            {
                try
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
                catch
                {
                }
            }
            //
            return returnValue;
        }

        public bool Connect()
        {
            bool success = false;
            //
            try
            {
                LoadZwavePort();
                //         
                success = zwavePort.Connect();
            }
            catch
            {
            }
            if (InterfaceModulesChangedAction != null) InterfaceModulesChangedAction(new InterfaceModulesChangedAction() { Domain = this.Domain });
            //
            return success;
        }

        public void Disconnect()
        {

            UnloadZwavePort();

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
        }
        
        // TODO: check if this is to be deprecated or relocated
        public void Dispose()
        {

            //_unloadZWavePort();
            try
            {
                controller.ControllerEvent -= DiscoveryEvent;
                controller.UpdateNodeParameter -= controller_UpdateNodeParameter;
                controller.ManufacturerSpecificResponse -= controller_ManufacturerSpecificResponse;
            }
            catch
            {
            }
            //
            try
            {
                zwavePort.Disconnect();
            }
            catch
            {
            }
            zwavePort = null;
            controller = null;

        }

        #endregion

        #region Private members

        private void LoadZwavePort()
        {
            if (zwavePort == null)
            {
                zwavePort = new ZWavePort();
                //
                controller = new Controller(zwavePort);
                //
                controller.ControllerEvent += DiscoveryEvent;
                controller.UpdateNodeParameter += controller_UpdateNodeParameter;
                controller.ManufacturerSpecificResponse += controller_ManufacturerSpecificResponse;
            }
            zwavePort.PortName = this.GetOption("Port").Value;
        }

        private void UnloadZwavePort()
        {
            try
            {
                //_controller.DiscoveryEvent -= DiscoveryEvent;
                //_controller.UpdateNodeParameter -= controller_UpdateNodeParameter;
                //_controller.ManufacturerSpecificResponse -= controller_ManufacturerSpecificResponse;
            }
            catch
            {
            }
            //
            try
            {
                zwavePort.Disconnect();
            }
            catch
            {
            }
            //_zwaveport = null;
            //_controller = null;
        }

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

        // fired either at startup time and after a new z-wave node has been added to the controller
        private void DiscoveryEvent(object sender, ControllerEventArgs e)
        {
            switch (e.Status)
            {
            case ControllerStatus.DiscoveryStart:
                RaisePropertyChanged(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = "1",
                    SourceType = "Z-Wave Controller",
                    Path = "Controller.Status",
                    Value = "Discovery Started"
                });
                break;
            case ControllerStatus.DiscoveryEnd:
                RaisePropertyChanged(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = "1",
                    SourceType = "Z-Wave Controller",
                    Path = "Controller.Status",
                    Value = "Discovery Complete"
                });
                if (InterfaceModulesChangedAction != null) InterfaceModulesChangedAction(new InterfaceModulesChangedAction(){ Domain = this.Domain });
                break;
            case ControllerStatus.NodeAdded:
                RaisePropertyChanged(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = "1",
                    SourceType = "Z-Wave Controller",
                    Path = "Controller.Status",
                    Value = "Added node " + e.NodeId
                });
                lastAddedNode = e.NodeId;
                if (InterfaceModulesChangedAction != null) InterfaceModulesChangedAction(new InterfaceModulesChangedAction(){ Domain = this.Domain });
                break;
            case ControllerStatus.NodeUpdated:
                RaisePropertyChanged(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = "1",
                    SourceType = "Z-Wave Controller",
                    Path = "Controller.Status",
                    Value = "Updated node " + e.NodeId
                });
                //if (InterfaceModulesChangedAction != null) InterfaceModulesChangedAction(new InterfaceModulesChangedAction(){ Domain = this.Domain });
                break;
            case ControllerStatus.NodeRemoved:
                lastRemovedNode = e.NodeId;
                RaisePropertyChanged(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = "1",
                    SourceType = "Z-Wave Controller",
                    Path = "Controller.Status",
                    Value = "Removed node " + e.NodeId
                });
                if (InterfaceModulesChangedAction != null) InterfaceModulesChangedAction(new InterfaceModulesChangedAction(){ Domain = this.Domain });
                break;
            case ControllerStatus.NodeError:
                RaisePropertyChanged(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = "1",
                    SourceType = "Z-Wave Controller",
                    Path = "Controller.Status",
                    Value = "Node " + e.NodeId + " response timeout!"
                });
                break;
            }
        }

        private void controller_UpdateNodeParameter(object sender, UpdateNodeParameterEventArgs upargs)
        {
            string path = "UnknwonParameter";
            object value = upargs.Value;
            //
            lock(syncLock)
            switch (upargs.ParameterName)
            {
            case EventParameter.MeterKwHour:
                path = GetIndexedParameterPath(ModuleParameters.MODPAR_METER_KW_HOUR, upargs.ParameterId);
                break;
            case EventParameter.MeterKvaHour:
                path = GetIndexedParameterPath(ModuleParameters.MODPAR_METER_KVA_HOUR, upargs.ParameterId);
                break;
            case EventParameter.MeterWatt:
                path = GetIndexedParameterPath(ModuleParameters.MODPAR_METER_WATTS, upargs.ParameterId);
                break;
            case EventParameter.MeterPulses:
                path = GetIndexedParameterPath(ModuleParameters.MODPAR_METER_PULSES, upargs.ParameterId);
                break;
            case EventParameter.MeterAcVolt:
                path = GetIndexedParameterPath(ModuleParameters.MODPAR_METER_AC_VOLT, upargs.ParameterId);
                break;
            case EventParameter.MeterAcCurrent:
                path = GetIndexedParameterPath(ModuleParameters.MODPAR_METER_AC_CURRENT, upargs.ParameterId);
                break;
            case EventParameter.MeterPower:
                path = GetIndexedParameterPath(ModuleParameters.MODPAR_SENSOR_POWER, upargs.ParameterId);
                break;
            case EventParameter.Battery:
                RaisePropertyChanged(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = upargs.NodeId.ToString(),
                    SourceType = "ZWave Node",
                    Path = "ZWaveNode.Battery",
                    Value = value
                });
                path = ModuleParameters.MODPAR_STATUS_BATTERY;
                break;
            case EventParameter.NodeInfo:
                path = "ZWaveNode.NodeInfo";
                break;
            case EventParameter.Generic:
                path = ModuleParameters.MODPAR_SENSOR_GENERIC;
                break;
            case EventParameter.AlarmGeneric:
                path = ModuleParameters.MODPAR_SENSOR_ALARM_GENERIC;
                break;
            case EventParameter.AlarmDoorWindow:
                path = ModuleParameters.MODPAR_SENSOR_DOORWINDOW;
                break;
            case EventParameter.AlarmTampered:
                path = ModuleParameters.MODPAR_SENSOR_TAMPER;
                break;
            case EventParameter.SensorTemperature:
                path = ModuleParameters.MODPAR_SENSOR_TEMPERATURE;
                break;
            case EventParameter.SensorHumidity:
                path = ModuleParameters.MODPAR_SENSOR_HUMIDITY;
                break;
            case EventParameter.SensorLuminance:
                path = ModuleParameters.MODPAR_SENSOR_LUMINANCE;
                break;
            case EventParameter.SensorMotion:
                path = ModuleParameters.MODPAR_SENSOR_MOTIONDETECT;
                break;
            case EventParameter.AlarmSmoke:
                path = ModuleParameters.MODPAR_SENSOR_ALARM_SMOKE;
                break;
            case EventParameter.AlarmCarbonMonoxide:
                path = ModuleParameters.MODPAR_SENSOR_ALARM_CARBONMONOXIDE;
                break;
            case EventParameter.AlarmCarbonDioxide:
                path = ModuleParameters.MODPAR_SENSOR_ALARM_CARBONDIOXIDE;
                break;
            case EventParameter.AlarmHeat:
                path = ModuleParameters.MODPAR_SENSOR_ALARM_HEAT;
                break;
            case EventParameter.AlarmFlood:
                path = ModuleParameters.MODPAR_SENSOR_ALARM_FLOOD;
                break;
            case EventParameter.ManufacturerSpecific:
                ManufacturerSpecificInfo mf = (ManufacturerSpecificInfo)value;
                path = "ZWaveNode.ManufacturerSpecific";
                value = mf.ManufacturerId + ":" + mf.TypeId + ":" + mf.ProductId;
                break;
            case EventParameter.Configuration:
                path = "ZWaveNode.Variables." + upargs.ParameterId;
                break;
            case EventParameter.Association:
                var associationResponse = (Association.AssociationResponse)value;
                RaisePropertyChanged(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = upargs.NodeId.ToString(),
                    SourceType = "ZWave Node",
                    Path = "ZWaveNode.Associations.Max",
                    Value = associationResponse.Max
                });
                RaisePropertyChanged(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = upargs.NodeId.ToString(),
                    SourceType = "ZWave Node",
                    Path = "ZWaveNode.Associations.Count",
                    Value = associationResponse.Count
                });
                path = "ZWaveNode.Associations." + associationResponse.GroupId; // TODO: implement generic group/node association instead of fixed one
                value = associationResponse.NodeList;
                break;
            case EventParameter.MultiinstanceSwitchBinaryCount:
                path = "ZWaveNode.MultiInstance.SwitchBinary.Count";
                break;
            case EventParameter.MultiinstanceSwitchMultilevelCount:
                path = "ZWaveNode.MultiInstance.SwitchMultiLevel.Count";
                break;
            case EventParameter.MultiinstanceSensorBinaryCount:
                path = "ZWaveNode.MultiInstance.SensorBinary.Count";
                break;
            case EventParameter.MultiinstanceSensorMultilevelCount:
                path = "ZWaveNode.MultiInstance.SensorMultiLevel.Count";
                break;
            case EventParameter.MultiinstanceSwitchBinary:
                path = "ZWaveNode.MultiInstance.SwitchBinary." + upargs.ParameterId;
                break;
            case EventParameter.MultiinstanceSwitchMultilevel:
                path = "ZWaveNode.MultiInstance.SwitchMultiLevel." + upargs.ParameterId;
                break;
            case EventParameter.MultiinstanceSensorBinary:
                path = "ZWaveNode.MultiInstance.SensorBinary." + upargs.ParameterId;
                break;
            case EventParameter.MultiinstanceSensorMultilevel:
                path = "ZWaveNode.MultiInstance.SensorMultiLevel." + upargs.ParameterId;
                break;
            case EventParameter.WakeUpInterval:
                path = "ZWaveNode.WakeUpInterval";
                break;
            case EventParameter.WakeUpNotify:
                path = "ZWaveNode.WakeUpNotify";
                break;
            case EventParameter.Level:
                RaisePropertyChanged(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = upargs.NodeId.ToString(),
                    SourceType = "ZWave Node",
                    Path = "ZWaveNode.Basic",
                    Value = value
                });
                double normalizedval = (Math.Round((double)value / 100D, 2));
                // binary switches have [0/255], while multilevel switches [0-99],
                // normalize Status.Level to [0.0 <-> 1.0]
                if (normalizedval >= 0.99) normalizedval = 1.0;
                if (upargs.ParameterId == 0)
                {
                    path = ModuleParameters.MODPAR_STATUS_LEVEL;
                }
                else
                {
                    path = ModuleParameters.MODPAR_STATUS_LEVEL + "." + upargs.ParameterId;
                }
                value = normalizedval.ToString(CultureInfo.InvariantCulture);
                break;
            case EventParameter.ThermostatMode:
                path = "Thermostat.Mode";
                value = ((Mode)value).ToString();
                break;
            case EventParameter.ThermostatOperatingState:
                path = "Thermostat.OperatingState";
                value = ((OperatingState)value).ToString();
                break;
            case EventParameter.ThermostatFanMode:
                path = "Thermostat.FanMode";
                value = ((FanMode)value).ToString();
                break;
            case EventParameter.ThermostatFanState:
                path = "Thermostat.FanState";
                value = ((FanState)value).ToString();
                break;
            case EventParameter.ThermostatHeating:
                path = "Thermostat.Heating";
                break;
            case EventParameter.ThermostatSetBack:
                path = "Thermostat.SetBack";
                break;
            case EventParameter.ThermostatSetPoint:
                path = "Thermostat.SetPoint." + ((SetPointType)((dynamic)value).Type).ToString();
                value = ((dynamic)value).Value;
                break;
            case EventParameter.UserCode:
                path = "EntryControl.UserCode";
                value = ((ZWaveLib.Values.UserCodeValue)value).TagCodeToHexString();
                break;
            default:
                Console.WriteLine(
                    "UNHANDLED PARAMETER CHANGE FROM NODE {0} ====> Param Type: {1} Param Id:{2} Value:{3}",
                    upargs.NodeId,
                    upargs.ParameterName,
                    upargs.ParameterId,
                    value
                );
                break;
            }
            //string type = upargs.ParameterType.ToString ();
            //
            RaisePropertyChanged(new InterfacePropertyChangedAction() {
                Domain = this.Domain,
                SourceId = upargs.NodeId.ToString(),
                SourceType = "ZWave Node",
                Path = path,
                Value = value
            });
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
            if (!node.Data.ContainsKey("Level"))
            {
                node.Data.Add("Level", level);
            }
            else
            {
                node.Data["Level"] = level;
            }
        }

        private int GetNodeLevel(ZWaveNode node)
        {
            int level = 0;
            if (node.Data.ContainsKey("Level"))
            {
                level = (int)node.Data["Level"];
            }
            return level;
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

        /*
        private void UpdateZWaveNodeDeviceHandler(int nodeId)
        {
            var node = controller.Devices.Find(zn => zn.NodeId == nodeId);
            InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                Domain = this.Domain,
                SourceId = nodeId.ToString(),
                SourceType = "ZWave Node",
                Path = "ZWaveNode.DeviceHandler",
                Value = node.DeviceHandler.GetType().FullName
            });
        }
        */
        // TODO: deprecate this... in the ZWaveLib.Controller class as well
        private void controller_ManufacturerSpecificResponse(object sender, ManufacturerSpecificResponseEventArg args)
        {
            //UpdateZWaveNodeDeviceHandler(args.NodeId);
        }

        #endregion

    }

}
