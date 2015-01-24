﻿/*
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
using ZWaveLib.Devices;

//using System.Management;
using ZWaveLib.Devices.ProductHandlers.Generic;

using MIG.Interfaces.HomeAutomation.Commons;

namespace MIG.Interfaces.HomeAutomation
{

    public class ZWave : MIGInterface
    {
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
                { 810, "Thermostat.OperatingStateReport" },

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
            public static readonly Command THERMOSTAT_OPERATINGSTATE_REPORT = new Command(810);

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

        private ZWavePort zwavePort;
        private Controller controller;

        private byte lastRemovedNode = 0;
        private byte lastAddedNode = 0;

        public ZWave()
        {
        }

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
            bool raisePropertyChanged = false;
            string parameterPath = "Status.Level";
            string raiseParameter = "";
            //
            string nodeId = request.NodeId;
            Command command = (Command)request.Command;
            ////----------------------
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
                    raisePropertyChanged = true;
                    double raiseValue = double.Parse(request.GetOption(0)) / 100;
                    if (raiseValue > 1) raiseValue = 1;
                    raiseParameter = raiseValue.ToString(CultureInfo.InvariantCulture);
                    //
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    node.Basic_Set((byte)int.Parse(request.GetOption(0)));
                }
                else if (command == Command.BASIC_GET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    node.Basic_Get();
                }
                ////-----------------------
                else if (command == Command.MULTIINSTANCE_GETCOUNT)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    //
                    switch (request.GetOption(0))
                    {
                    case "Switch.Binary":
                        node.MultiInstance_GetCount((byte)ZWaveLib.CommandClass.SwitchBinary);
                        break;
                    case "Switch.MultiLevel":
                        node.MultiInstance_GetCount((byte)ZWaveLib.CommandClass.SwitchMultilevel);
                        break;
                    case "Sensor.Binary":
                        node.MultiInstance_GetCount((byte)ZWaveLib.CommandClass.SensorBinary);
                        break;
                    case "Sensor.MultiLevel":
                        node.MultiInstance_GetCount((byte)ZWaveLib.CommandClass.SensorMultilevel);
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
                        node.MultiInstance_SwitchBinaryGet(instance);
                        break;
                    case "Switch.MultiLevel":
                        node.MultiInstance_SwitchMultiLevelGet(instance);
                        break;
                    case "Sensor.Binary":
                        node.MultiInstance_SensorBinaryGet(instance);
                        break;
                    case "Sensor.MultiLevel":
                        node.MultiInstance_SensorMultiLevelGet(instance);
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
                        node.MultiInstance_SwitchBinarySet(instance, value);
                            //raiseparam = (double.Parse(request.GetOption(2)) / 255).ToString();
                        break;
                    case "Switch.MultiLevel":
                        node.MultiInstance_SwitchMultiLevelSet(instance, value);
                            //raiseparam = (double.Parse(request.GetOption(2)) / 100).ToString(); // TODO: should it be 99 ?
                        break;
                    }
                }
                else if (command == Command.SENSORBINARY_GET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    node.SensorBinary_Get();
                }
                else if (command == Command.SENSORMULTILEVEL_GET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    node.SensorMultiLevel_Get();
                }
                else if (command == Command.METER_GET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    // see ZWaveLib Sensor.cs for EnergyMeterScale options
                    int scaleType = 0; int.TryParse(request.GetOption(0), out scaleType);
                    node.Meter_Get((byte)(scaleType << 0x03));
                }
                else if (command == Command.METER_SUPPORTEDGET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    node.Meter_SupportedGet();
                }
                else if (command == Command.METER_RESET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    node.Meter_Reset();
                }
                else if (command == Command.NODEINFO_GET)
                {
                    controller.GetNodeInformationFrame((byte)int.Parse(nodeId));
                }
                ////-----------------------
                else if (command == Command.BATTERY_GET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    node.Battery_Get();
                }
                ////-----------------------
                else if (command == Command.ASSOCIATION_SET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    node.Association_Set((byte)int.Parse(request.GetOption(0)), (byte)int.Parse(request.GetOption(1)));
                }
                else if (command == Command.ASSOCIATION_GET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    node.Association_Get((byte)int.Parse(request.GetOption(0))); // groupid
                }
                else if (command == Command.ASSOCIATION_REMOVE)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    node.Association_Remove(
                        (byte)int.Parse(request.GetOption(0)),
                        (byte)int.Parse(request.GetOption(1))
                    ); // groupid
                }
                ////-----------------------
                else if (command == Command.MANUFACTURERSPECIFIC_GET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    node.ManufacturerSpecific_Get();
                }
                ////------------------
                else if (command == Command.CONFIG_PARAMETERSET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    //byte[] value = new byte[] { (byte)int.Parse(option1) };//BitConverter.GetBytes(Int16.Parse(option1));
                    //Array.Reverse(value);
                    node.Configuration_ParameterSet((byte)int.Parse(request.GetOption(0)), int.Parse(request.GetOption(1)));
                }
                else if (command == Command.CONFIG_PARAMETERGET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    node.Configuration_ParameterGet((byte)int.Parse(request.GetOption(0)));
                }
                ////------------------
                else if (command == Command.WAKEUP_GET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    node.WakeUp_Get();
                }
                else if (command == Command.WAKEUP_SET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    node.WakeUp_Set(uint.Parse(request.GetOption(0)));
                }
                ////------------------
                else if (command == Command.CONTROL_ON)
                {
                    raisePropertyChanged = true;
                    raiseParameter = "1";
                    //
                    // Basic.Set 0xFF
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    ((Switch)node.DeviceHandler).On();
                }
                else if (command == Command.CONTROL_OFF)
                {
                    raisePropertyChanged = true;
                    raiseParameter = "0";
                    //
                    // Basic.Set 0x00
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    ((Switch)node.DeviceHandler).Off();
                }
                else if (command == Command.CONTROL_LEVEL)
                {
                    raisePropertyChanged = true;
                    raiseParameter = (double.Parse(request.GetOption(0)) / 100).ToString();
                    //
                    // Basic.Set <level>
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    ((Dimmer)node.DeviceHandler).Level = int.Parse(request.GetOption(0));
                }
                else if (command == Command.CONTROL_TOGGLE)
                {
                    raisePropertyChanged = true;
                    //
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    if (((Switch)node.DeviceHandler).Level == 0)
                    {
                        raiseParameter = "1";
                        // Basic.Set 0xFF
                        ((Switch)node.DeviceHandler).On();
                    }
                    else
                    {
                        raiseParameter = "0";
                        // Basic.Set 0x00
                        ((Switch)node.DeviceHandler).Off();
                    }
                }
                else if (command == Command.THERMOSTAT_MODEGET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    ((Thermostat)node.DeviceHandler).Thermostat_ModeGet();
                }
                else if (command == Command.THERMOSTAT_MODESET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    Thermostat.Mode mode = (Thermostat.Mode)Enum.Parse(typeof(Thermostat.Mode), request.GetOption(0));
                    //
                    raisePropertyChanged = true;
                    parameterPath = "Thermostat.Mode";
                    raiseParameter = request.GetOption(0);
                    //
                    ((Thermostat)node.DeviceHandler).Thermostat_ModeSet(mode);
                }
                else if (command == Command.THERMOSTAT_SETPOINTGET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    ((Thermostat)node.DeviceHandler).Thermostat_SetPointGet();
                }
                else if (command == Command.THERMOSTAT_SETPOINTSET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    Thermostat.SetPointType mode = (Thermostat.SetPointType)Enum.Parse(typeof(Thermostat.SetPointType), request.GetOption(0));
                    int temperature = int.Parse(request.GetOption(1));
                    //
                    raisePropertyChanged = true;
                    parameterPath = "Thermostat.SetPoint." + request.GetOption(0);
                    raiseParameter = temperature.ToString(CultureInfo.InvariantCulture);
                    //
                    ((Thermostat)node.DeviceHandler).Thermostat_SetPointSet(mode, temperature);
                }
                else if (command == Command.THERMOSTAT_FANMODEGET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    ((Thermostat)node.DeviceHandler).Thermostat_FanModeGet();
                }
                else if (command == Command.THERMOSTAT_FANMODESET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    Thermostat.FanMode mode = (Thermostat.FanMode)Enum.Parse(typeof(Thermostat.FanMode), request.GetOption(0));
                    //
                    raisePropertyChanged = true;
                    parameterPath = "Thermostat.FanMode";
                    raiseParameter = request.GetOption(0);
                    //
                    ((Thermostat)node.DeviceHandler).Thermostat_FanModeSet(mode);
                }
                else if (command == Command.THERMOSTAT_FANSTATEGET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    ((Thermostat)node.DeviceHandler).Thermostat_FanStateGet();
                }
                else if (command == Command.THERMOSTAT_GETALL)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    ((Thermostat)node.DeviceHandler).Thermostat_SetPointGet();
                    Thread.Sleep(200);
                    ((Thermostat)node.DeviceHandler).Thermostat_FanStateGet();
                    Thread.Sleep(200);
                    ((Thermostat)node.DeviceHandler).Thermostat_FanModeGet();
                    Thread.Sleep(200);
                    ((Thermostat)node.DeviceHandler).Thermostat_ModeGet();
                    // TODO: find an alternative to the deprecated method below
                    //Thread.Sleep(200);
                    //node.RequestMultiLevelReport();
                }
                else if (command == Command.THERMOSTAT_OPERATINGSTATE_GET)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    ((Thermostat)node.DeviceHandler).Thermostat_OperatingStateGet();
                }
                else if (command == Command.THERMOSTAT_OPERATINGSTATE_REPORT)
                {
                    var node = controller.GetDevice((byte)int.Parse(nodeId));
                    ((Thermostat)node.DeviceHandler).Thermostat_OperatingStateReport();
                }
            }
            catch
            {
                if (raiseParameter != "") raisePropertyChanged = true;
            }
            //
            if (raisePropertyChanged && InterfacePropertyChangedAction != null)
            {
                try
                {
                    //ZWaveNode node = _controller.GetDevice ((byte)int.Parse (nodeid));
                    InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                        Domain = this.Domain,
                        SourceId = nodeId,
                        SourceType = "ZWave Node",
                        Path = parameterPath,
                        Value = raiseParameter
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
            switch (upargs.ParameterType)
            {
            case ParameterType.METER_KW_HOUR:
                path = ModuleParameters.MODPAR_METER_KW_HOUR;
                break;
            case ParameterType.METER_KVA_HOUR:
                path = ModuleParameters.MODPAR_METER_KVA_HOUR;
                break;
            case ParameterType.METER_WATT:
                path = ModuleParameters.MODPAR_METER_WATTS;
                break;
            case ParameterType.METER_PULSES:
                path = ModuleParameters.MODPAR_METER_PULSES;
                break;
            case ParameterType.METER_AC_VOLT:
                path = ModuleParameters.MODPAR_METER_AC_VOLT;
                break;
            case ParameterType.METER_AC_CURRENT:
                path = ModuleParameters.MODPAR_METER_AC_CURRENT;
                break;
            case ParameterType.METER_POWER:
                path = ModuleParameters.MODPAR_SENSOR_POWER;
                break;
            case ParameterType.BATTERY:
                RaisePropertyChanged(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = upargs.NodeId.ToString(),
                    SourceType = "ZWave Node",
                    Path = "ZWaveNode.Battery",
                    Value = value
                });
                path = ModuleParameters.MODPAR_STATUS_BATTERY;
                break;
            case ParameterType.NODE_INFO:
                path = "ZWaveNode.NodeInfo";
                break;
            case ParameterType.GENERIC:
                path = ModuleParameters.MODPAR_SENSOR_GENERIC;
                break;
            case ParameterType.ALARM_GENERIC:
                path = ModuleParameters.MODPAR_SENSOR_ALARM_GENERIC;
                break;
            case ParameterType.ALARM_DOORWINDOW:
                path = ModuleParameters.MODPAR_SENSOR_DOORWINDOW;
                break;
            case ParameterType.ALARM_TAMPERED:
                path = ModuleParameters.MODPAR_SENSOR_TAMPER;
                break;
            case ParameterType.SENSOR_TEMPERATURE:
                path = ModuleParameters.MODPAR_SENSOR_TEMPERATURE;
                break;
            case ParameterType.SENSOR_HUMIDITY:
                path = ModuleParameters.MODPAR_SENSOR_HUMIDITY;
                break;
            case ParameterType.SENSOR_LUMINANCE:
                path = ModuleParameters.MODPAR_SENSOR_LUMINANCE;
                break;
            case ParameterType.SENSOR_MOTION:
                path = ModuleParameters.MODPAR_SENSOR_MOTIONDETECT;
                break;
            case ParameterType.ALARM_SMOKE:
                path = ModuleParameters.MODPAR_SENSOR_ALARM_SMOKE;
                break;
            case ParameterType.ALARM_CARBONMONOXIDE:
                path = ModuleParameters.MODPAR_SENSOR_ALARM_CARBONMONOXIDE;
                break;
            case ParameterType.ALARM_CARBONDIOXIDE:
                path = ModuleParameters.MODPAR_SENSOR_ALARM_CARBONDIOXIDE;
                break;
            case ParameterType.ALARM_HEAT:
                path = ModuleParameters.MODPAR_SENSOR_ALARM_HEAT;
                break;
            case ParameterType.ALARM_FLOOD:
                path = ModuleParameters.MODPAR_SENSOR_ALARM_FLOOD;
                break;
            case ParameterType.MANUFACTURER_SPECIFIC:
                ManufacturerSpecific mf = (ManufacturerSpecific)value;
                path = "ZWaveNode.ManufacturerSpecific";
                value = mf.ManufacturerId + ":" + mf.TypeId + ":" + mf.ProductId;
                break;
            case ParameterType.CONFIGURATION:
                path = "ZWaveNode.Variables." + upargs.ParameterId;
                break;
            case ParameterType.ASSOCIATION:
                switch (upargs.ParameterId)
                {
                //                    case 0:
                //                        path = "ZWaveNode.Associations.Group";
                //                        break;
                case 1:
                    path = "ZWaveNode.Associations.Max";
                    break;
                case 2:
                    path = "ZWaveNode.Associations.Count";
                    break;
                case 3:
                    string gid = value.ToString().Split(':')[0];
                    value = value.ToString().Split(':')[1];
                    path = "ZWaveNode.Associations." + gid; // TODO: implement generic group/node association instead of fixed one
                    break;
                }
                break;
            case ParameterType.MULTIINSTANCE_SWITCH_BINARY_COUNT:
                path = "ZWaveNode.MultiInstance.SwitchBinary.Count";
                break;
            case ParameterType.MULTIINSTANCE_SWITCH_MULTILEVEL_COUNT:
                path = "ZWaveNode.MultiInstance.SwitchMultiLevel.Count";
                break;
            case ParameterType.MULTIINSTANCE_SENSOR_BINARY_COUNT:
                path = "ZWaveNode.MultiInstance.SensorBinary.Count";
                break;
            case ParameterType.MULTIINSTANCE_SENSOR_MULTILEVEL_COUNT:
                path = "ZWaveNode.MultiInstance.SensorMultiLevel.Count";
                break;
            case ParameterType.MULTIINSTANCE_SWITCH_BINARY:
                path = "ZWaveNode.MultiInstance.SwitchBinary." + upargs.ParameterId;
                break;
            case ParameterType.MULTIINSTANCE_SWITCH_MULTILEVEL:
                path = "ZWaveNode.MultiInstance.SwitchMultiLevel." + upargs.ParameterId;
                break;
            case ParameterType.MULTIINSTANCE_SENSOR_BINARY:
                path = "ZWaveNode.MultiInstance.SensorBinary." + upargs.ParameterId;
                break;
            case ParameterType.MULTIINSTANCE_SENSOR_MULTILEVEL:
                path = "ZWaveNode.MultiInstance.SensorMultiLevel." + upargs.ParameterId;
                break;
            case ParameterType.WAKEUP_INTERVAL:
                path = "ZWaveNode.WakeUpInterval";
                break;
            case ParameterType.WAKEUP_NOTIFY:
                path = "ZWaveNode.WakeUpNotify";
                break;
            case ParameterType.LEVEL:
                    //
                RaisePropertyChanged(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = upargs.NodeId.ToString(),
                    SourceType = "ZWave Node",
                    Path = "ZWaveNode.Basic",
                    Value = value
                });
                    //
                double normalizedval = (Math.Round((double)value / 99D, 2));
                if (normalizedval > 1.0) normalizedval = 1.0; // binary switches have [0/255], while multilevel switches [0-99]
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
            case ParameterType.THERMOSTAT_MODE:
                path = "Thermostat.Mode";
                value = ((Thermostat.Mode)value).ToString();
                break;
            case ParameterType.THERMOSTAT_OPERATING_STATE:
                path = "Thermostat.OperatingState";
                value = ((Thermostat.OperatingState)value).ToString();
                break;
            case ParameterType.THERMOSTAT_FAN_MODE:
                path = "Thermostat.FanMode";
                value = ((Thermostat.FanMode)value).ToString();
                break;
            case ParameterType.THERMOSTAT_FAN_STATE:
                path = "Thermostat.FanState";
                value = ((Thermostat.FanState)value).ToString();
                break;
            case ParameterType.THERMOSTAT_HEATING:
                path = "Thermostat.Heating";
                break;
            case ParameterType.THERMOSTAT_SETBACK:
                path = "Thermostat.SetBack";
                break;
            case ParameterType.THERMOSTAT_SETPOINT:
                path = "Thermostat.SetPoint." + ((Thermostat.SetPointType)((dynamic)value).Type).ToString();
                value = ((dynamic)value).Value;
                break;

            default:
                Console.WriteLine(
                    "UNHANDLED PARAMETER CHANGE FROM NODE {0} ====> Param Type: {1} Param Id:{2} Value:{3}",
                    upargs.NodeId,
                    upargs.ParameterType,
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

        private void controller_ManufacturerSpecificResponse(object sender, ManufacturerSpecificResponseEventArg args)
        {
            UpdateZWaveNodeDeviceHandler(args.NodeId);
        }


    }

}
