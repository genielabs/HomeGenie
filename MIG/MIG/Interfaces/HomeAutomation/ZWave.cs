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
using System.Linq;
using System.Text;
using System.Threading;

using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;

using ZWaveLib;
using ZWaveLib.Devices;

//using System.Management;

using MIG.Interfaces.HomeAutomation.Commons;

namespace MIG.Interfaces.HomeAutomation
{

	public class ZWave : MIGInterface
    {
        #region Implemented MIG Commands
        // typesafe enum
		public sealed class Command : GatewayCommand
		{

			public static Dictionary<int, string> CommandsList = new Dictionary<int, string> ()
            {
                {101, "Controller.Discovery"},
                {111, "Controller.NodeAdd"},
                {113, "Controller.NodeRemove"},
                {151, "Controller.AutoReportSet"},

                {201, "Basic.Get"},
                {202, "Basic.Set"},
                {203, "Basic.Report"},

                {211, "MultiInstance.Get"},
                {212, "MultiInstance.Set"},
                {213, "MultiInstance.GetCount"},

                {251, "Battery.Get"},

                {301, "Association.Get"},
                {302, "Association.Set"},
                {303, "Association.Remove"},

                {401, "ManufacturerSpecific.Get"},
                {402, "NodeInfo.Get"},

                {451, "Config.ParameterGet"},
                {452, "Config.ParameterSet"},

                {501, "WakeUp.Get"},
                {502, "WakeUp.Set"},

                {601, "MultiLevel.Report"},

                {701, "Control.On"},
                {702, "Control.Off"},
                {705, "Control.Level"},
                {706, "Control.Toggle"},

                {1000, "NodeInfo.Get"},
            };

			// <context>.<command> enum   -   eg. Control.On where <context> :== "Control" and <command> :== "On"
			public static readonly Command CONTROLLER_DISCOVERY = new Command (101);
			public static readonly Command CONTROLLER_NODEADD = new Command (111);
			public static readonly Command CONTROLLER_NODEREMOVE = new Command (113);
			public static readonly Command CONTROLLER_AUTOREPORTSET = new Command (151);

			public static readonly Command BASIC_GET = new Command (201);
			public static readonly Command BASIC_SET = new Command (202);

            public static readonly Command BASIC_REPORT = new Command (203); // TODO deprecate this
            
            public static readonly Command MULTIINSTANCE_GET = new Command(211);
            public static readonly Command MULTIINSTANCE_SET = new Command(212);
            public static readonly Command MULTIINSTANCE_GETCOUNT = new Command(213);

			public static readonly Command BATTERY_GET = new Command (251);

			public static readonly Command ASSOCIATION_GET = new Command (301);
			public static readonly Command ASSOCIATION_SET = new Command (302);
			public static readonly Command ASSOCIATION_REMOVE = new Command (303);

			public static readonly Command MANUFACTURERSPECIFIC_GET = new Command (401);
            public static readonly Command NODEINFO_GET = new Command(402);

			public static readonly Command CONFIG_PARAMETERGET = new Command (451);
			public static readonly Command CONFIG_PARAMETERSET = new Command (452);

			public static readonly Command WAKEUP_GET = new Command (501);
			public static readonly Command WAKEUP_SET = new Command (502);


            public static readonly Command MULTILEVEL_REPORT = new Command(601); // TODO deprecate this

			public static readonly Command CONTROL_ON = new Command (701);
			public static readonly Command CONTROL_OFF = new Command (702);
			public static readonly Command CONTROL_LEVEL = new Command (705);
            public static readonly Command CONTROL_TOGGLE = new Command(706);

            private readonly String name;
            private readonly int value;
            
            private Command(int value)
			{
				this.name = CommandsList [value];
				this.value = value;
			}

			public Dictionary<int, string> ListCommands ()
			{
				return Command.CommandsList;
			}

			public int Value {
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

			public static explicit operator Command (int idx)
			{
				return new Command (idx);
			}

			public static explicit operator Command (string str)
			{
				if (CommandsList.ContainsValue (str)) {
					var cmd = from c in CommandsList where c.Value == str select c.Key;
					return new Command (cmd.First ());
				} else {
					throw new InvalidCastException ();
				}
			}
			public static bool operator == (Command a, Command b)
			{
				return a.value == b.value;
			}
			public static bool operator != (Command a, Command b)
			{
				return a.value != b.value;
			}
		}

        #endregion

		private ZWavePort _zwaveport;
		private Controller _controller;
		private string _portname = "";

		private byte _lastnoderemoved = 0;
		private byte _lastnodeadded = 0;

		public ZWave()
		{
			OperatingSystem os = Environment.OSVersion;
			PlatformID pid = os.Platform;
			//
			switch (pid) {
			case PlatformID.Win32NT:
			case PlatformID.Win32S:
			case PlatformID.Win32Windows:
			case PlatformID.WinCE:
				_portname = "COM7";
				break;
			case PlatformID.Unix:
			case PlatformID.MacOSX:
			default:
				_portname = "/dev/ttyUSB0";
				break;
			}
		}

        #region MIG Interface members

        public event Action<InterfacePropertyChangedAction> InterfacePropertyChangedAction;

        public string Domain
        {
            get
            {
                string ifacedomain = this.GetType().Namespace.ToString();
                ifacedomain = ifacedomain.Substring(ifacedomain.LastIndexOf(".") + 1) + "." + this.GetType().Name.ToString();
                return ifacedomain;
            }
        }

        public bool IsConnected
        {
            get
            {
                if (_zwaveport != null)
                    return _zwaveport.IsConnected;
                else return false;
            }
        }

        public void WaitOnPending()
        {

        }

        public object InterfaceControl(MIGInterfaceCommand request)
        {
            string returnvalue = "";
            bool raisepropchanged = false;
            string parampath = "Status.Level";
            string raiseparam = "";
            //
            string nodeid = request.nodeid;
            Command command = (Command)request.command;
            ////----------------------
            try
            {
                if (command == Command.CONTROLLER_DISCOVERY)
                {
                    _controller.Discovery();
                }


                //--------------////---------------------- DEPRECATE THESE  ----------------------------
                //else if (command == Command.CONTROLLER_AUTOREPORTSET)
                //{
                //    _controller.AutoReportSet((byte)int.Parse(request.GetOption(0)), (byte)int.Parse(request.GetOption(1)));
                //}
                //else 
                if (command == Command.BASIC_REPORT || command.ToString() == "Meter.Get") // TODO .. FIX: this is not basic report, it's a meter get
                {
                    ZWaveNode node = _controller.GetDevice((byte)int.Parse(nodeid));
                    node.RequestMeterReport();
                }
                else if (command == Command.MULTILEVEL_REPORT) // TODO this one call for having SwitchMultiLevel and SensorMultiLevel reports
                {
                    ZWaveNode node = _controller.GetDevice((byte)int.Parse(nodeid));
                    node.RequestMultiLevelReport();
                }
                //--------------////--------------------------------------------------------------------


                else if (command == Command.CONTROLLER_NODEADD)
                {
                    _lastnodeadded = 0;
                    byte addcid = _controller.BeginNodeAdd();
                    for (int i = 0; i < 20; i++)
                    {
                        if (_lastnodeadded > 0)
                        {
                            break;
                        }
                        Thread.Sleep(500);
                    }
                    _controller.StopNodeAdd();
                    //
                    returnvalue = _lastnodeadded.ToString();
                }
                else if (command == Command.CONTROLLER_NODEREMOVE)
                {
                    _lastnoderemoved = 0;
                    byte remcid = _controller.BeginNodeRemove();
                    for (int i = 0; i < 20; i++)
                    {
                        if (_lastnoderemoved > 0)
                        {
                            break;
                        }
                        Thread.Sleep(500);
                    }
                    _controller.StopNodeRemove();
                    //
                    returnvalue = _lastnoderemoved.ToString();
                }
                ////----------------------
                else if (command == Command.BASIC_SET)
                {
                    raisepropchanged = true;
                    double raiseval = double.Parse(request.GetOption(0)) / 100;
                    if (raiseval > 1) raiseval = 1;
                    raiseparam = raiseval.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    //
                    ZWaveNode node = _controller.GetDevice((byte)int.Parse(nodeid));
                    node.Basic_Set((byte)int.Parse(request.GetOption(0)));
                }
                else if (command == Command.BASIC_GET)
                {
                    ZWaveNode node = _controller.GetDevice((byte)int.Parse(nodeid));
                    node.Basic_Get();
                }
                ////-----------------------
                else if (command == Command.MULTIINSTANCE_GETCOUNT)
                {
                    ZWaveNode node = _controller.GetDevice((byte)int.Parse(nodeid));
                    //
                    switch (request.GetOption(0))
                    {
                        case "Switch.Binary":
                            node.MultiInstance_GetCount((byte)ZWaveLib.CommandClass.COMMAND_CLASS_SWITCH_BINARY);
                            break;
                        case "Switch.MultiLevel":
                            node.MultiInstance_GetCount((byte)ZWaveLib.CommandClass.COMMAND_CLASS_SWITCH_MULTILEVEL);
                            break;
                        case "Sensor.Binary":
                            node.MultiInstance_GetCount((byte)ZWaveLib.CommandClass.COMMAND_CLASS_SENSOR_BINARY);
                            break;
                        case "Sensor.MultiLevel":
                            node.MultiInstance_GetCount((byte)ZWaveLib.CommandClass.COMMAND_CLASS_SENSOR_MULTILEVEL);
                            break;
                    }
                }
                else if (command == Command.MULTIINSTANCE_GET)
                {
                    ZWaveNode node = _controller.GetDevice((byte)int.Parse(nodeid));
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
                    ZWaveNode node = _controller.GetDevice((byte)int.Parse(nodeid));
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
                else if (command == Command.NODEINFO_GET)
                {
                    ZWaveController.GetNodeInformationFrame((byte)int.Parse(nodeid));
                }
                ////-----------------------
                else if (command == Command.BATTERY_GET)
                {
                    ZWaveNode node = _controller.GetDevice((byte)int.Parse(nodeid));
                    node.Battery_Get();
                }
                ////-----------------------
                else if (command == Command.ASSOCIATION_SET)
                {
                    ZWaveNode node = _controller.GetDevice((byte)int.Parse(nodeid));
                    node.Association_Set((byte)int.Parse(request.GetOption(0)), (byte)int.Parse(request.GetOption(1)));
                }
                else if (command == Command.ASSOCIATION_GET)
                {
                    ZWaveNode node = _controller.GetDevice((byte)int.Parse(nodeid));
                    node.Association_Get((byte)int.Parse(request.GetOption(0))); // groupid
                }
                else if (command == Command.ASSOCIATION_REMOVE)
                {
                    ZWaveNode node = _controller.GetDevice((byte)int.Parse(nodeid));
                    node.Association_Remove((byte)int.Parse(request.GetOption(0)), (byte)int.Parse(request.GetOption(1))); // groupid
                }
                ////-----------------------
                else if (command == Command.MANUFACTURERSPECIFIC_GET)
                {
                    ZWaveNode node = _controller.GetDevice((byte)int.Parse(nodeid));
                    node.ManufacturerSpecific_Get();
                }
                ////------------------
                else if (command == Command.CONFIG_PARAMETERSET)
                {
                    ZWaveNode node = _controller.GetDevice((byte)int.Parse(nodeid));
                    //byte[] value = new byte[] { (byte)int.Parse(option1) };//BitConverter.GetBytes(Int16.Parse(option1));
                    //Array.Reverse(value);
                    node.ConfigParameterSet((byte)int.Parse(request.GetOption(0)), int.Parse(request.GetOption(1)));
                }
                else if (command == Command.CONFIG_PARAMETERGET)
                {
                    ZWaveNode node = _controller.GetDevice((byte)int.Parse(nodeid));
                    node.ConfigParameterGet((byte)int.Parse(request.GetOption(0)));
                }
                ////------------------
                else if (command == Command.WAKEUP_GET)
                {
                    ZWaveNode node = _controller.GetDevice((byte)int.Parse(nodeid));
                    node.WakeUpGetInterval();
                }
                else if (command == Command.WAKEUP_SET)
                {
                    ZWaveNode node = _controller.GetDevice((byte)int.Parse(nodeid));
                    node.WakeUpSetInterval(uint.Parse(request.GetOption(0)));
                }
                ////------------------
                else if (command == Command.CONTROL_ON)
                {
                    raisepropchanged = true;
                    raiseparam = "1";
                    //
                    // Basic.Set 0xFF
                    ZWaveNode node = _controller.GetDevice((byte)int.Parse(nodeid));
                    ((ZWaveLib.Devices.ProductHandlers.Generic.Switch)node.DeviceHandler).On();
                }
                else if (command == Command.CONTROL_OFF)
                {
                    raisepropchanged = true;
                    raiseparam = "0";
                    //
                    // Basic.Set 0x00
                    ZWaveNode node = _controller.GetDevice((byte)int.Parse(nodeid));
                    ((ZWaveLib.Devices.ProductHandlers.Generic.Switch)node.DeviceHandler).Off();
                }
                else if (command == Command.CONTROL_LEVEL)
                {
                    raisepropchanged = true;
                    raiseparam = (double.Parse(request.GetOption(0)) / 100).ToString();
                    //
                    // Basic.Set <level>
                    ZWaveNode node = _controller.GetDevice((byte)int.Parse(nodeid));
                    ((ZWaveLib.Devices.ProductHandlers.Generic.Dimmer)node.DeviceHandler).Level = int.Parse(request.GetOption(0));
                }
                else if (command == Command.CONTROL_TOGGLE)
                {
                    raisepropchanged = true;
                    //
                    ZWaveNode node = _controller.GetDevice((byte)int.Parse(nodeid));
                    if (((ZWaveLib.Devices.ProductHandlers.Generic.Switch)node.DeviceHandler).Level == 0)
                    {
                        raiseparam = "1";
                        // Basic.Set 0xFF
                        ((ZWaveLib.Devices.ProductHandlers.Generic.Switch)node.DeviceHandler).On();
                    }
                    else
                    {
                        raiseparam = "0";
                        // Basic.Set 0x00
                        ((ZWaveLib.Devices.ProductHandlers.Generic.Switch)node.DeviceHandler).Off();
                    }
                }
            }
            catch
            {
                if (raiseparam != "") raisepropchanged = true;
            }
            //
            if (raisepropchanged && InterfacePropertyChangedAction != null)
            {
                try
                {
                    //ZWaveNode node = _controller.GetDevice ((byte)int.Parse (nodeid));
                    InterfacePropertyChangedAction(new InterfacePropertyChangedAction() { Domain = this.Domain, SourceId = nodeid, SourceType = "ZWave Node", Path = parampath, Value = raiseparam });
                }
                catch
                {
                }
            }
            //
            return returnvalue;
        }


		public bool Connect ()
		{
			bool success = false;
			//
			try {
                _loadZWavePort();
				//         
				success = _zwaveport.Connect ();			
			} catch {
			}
			//
			return success;
		}

        public void Disconnect()
        {

            _unloadZWavePort();

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


        public Controller ZWaveController
        {
            get { return _controller; }
        }


        public string GetPortName()
        {
            return _portname;
        }

        public void SetPortName(string portname)
        {
            _portname = portname;
            Disconnect();
            Connect();
        }

        
        public void Dispose()
        {

            //_unloadZWavePort();
            try
            {
                _controller.DiscoveryEvent -= DiscoveryEvent;
                _controller.UpdateNodeParameter -= controller_UpdateNodeParameter;
                _controller.ManufacturerSpecificResponse -= controller_ManufacturerSpecificResponse;
            }
            catch
            {
            }
            //
            try
            {
                _zwaveport.Disconnect();
            }
            catch
            {
            }
            _zwaveport = null;
            _controller = null;

        }


        private void _loadZWavePort()
        {
            if (_zwaveport == null)
            {
                _zwaveport = new ZWavePort();
                //
                _controller = new Controller(_zwaveport);
                //
                _controller.DiscoveryEvent += DiscoveryEvent;
                _controller.UpdateNodeParameter += controller_UpdateNodeParameter;
                _controller.ManufacturerSpecificResponse += controller_ManufacturerSpecificResponse;
            }
            _zwaveport.PortName = _portname;
        }
        
        private void _unloadZWavePort()
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
                _zwaveport.Disconnect();
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
        
		private void DiscoveryEvent (object sender, DiscoveryEventArgs e)
		{
			//ZWaveNode node = _controller.GetDevice(e.NodeId);
			//
			if (e.Status == DISCOVERY_STATUS.NODE_REMOVED) {
				_lastnoderemoved = e.NodeId;
			} else if (e.Status == DISCOVERY_STATUS.NODE_ADDED) {
				_lastnodeadded = e.NodeId;
			}
		}

		private void controller_UpdateNodeParameter (object sender, UpdateNodeParameterEventArgs upargs)
		{
			string path = "UnknwonParameter";
			object value = upargs.Value;
            //
			switch (upargs.ParameterType) {
			case ParameterType.PARAMETER_WATTS:
				path = ModuleParameters.MODPAR_METER_WATTS;
				break;
            case ParameterType.PARAMETER_BATTERY:
				//
                _raisePropertyChanged(new InterfacePropertyChangedAction() { Domain = this.Domain, SourceId = upargs.NodeId.ToString(), SourceType = "ZWave Node", Path = "ZWaveNode.Battery", Value = value });
				//
                path = ModuleParameters.MODPAR_STATUS_BATTERY;
                break;
            case ParameterType.PARAMETER_NODE_INFO:
                path = "ZWaveNode.NodeInfo";
                break;
            case ParameterType.PARAMETER_GENERIC:
                path = ModuleParameters.MODPAR_SENSOR_GENERIC;
                break;
            case ParameterType.PARAMETER_ALARM_GENERIC:
                path = ModuleParameters.MODPAR_SENSOR_ALARM_GENERIC;
                break;
            case ParameterType.PARAMETER_ALARM_DOORWINDOW:
                path = ModuleParameters.MODPAR_SENSOR_DOORWINDOW;
                break;
            case ParameterType.PARAMETER_ALARM_TAMPERED:
                path = ModuleParameters.MODPAR_SENSOR_TAMPER;
                break;
            case ParameterType.PARAMETER_TEMPERATURE:
				path = ModuleParameters.MODPAR_SENSOR_TEMPERATURE;
				break;
			case ParameterType.PARAMETER_HUMIDITY:
				path = ModuleParameters.MODPAR_SENSOR_HUMIDITY;
				break;
			case ParameterType.PARAMETER_LUMINANCE:
				path = ModuleParameters.MODPAR_SENSOR_LUMINANCE;
				break;
			case ParameterType.PARAMETER_MOTION:
				path = ModuleParameters.MODPAR_SENSOR_MOTIONDETECT;
				break;
            case ParameterType.PARAMETER_ALARM_SMOKE:
                path = ModuleParameters.MODPAR_SENSOR_ALARM_SMOKE;
                break;
            case ParameterType.PARAMETER_ALARM_CARBONMONOXIDE:
                path = ModuleParameters.MODPAR_SENSOR_ALARM_CARBONMONOXIDE;
                break;
            case ParameterType.PARAMETER_ALARM_CARBONDIOXIDE:
                path = ModuleParameters.MODPAR_SENSOR_ALARM_CARBONDIOXIDE;
                break;
            case ParameterType.PARAMETER_ALARM_HEAT:
                path = ModuleParameters.MODPAR_SENSOR_ALARM_HEAT;
                break;
            case ParameterType.PARAMETER_ALARM_FLOOD:
                path = ModuleParameters.MODPAR_SENSOR_ALARM_FLOOD;
                break;
            case ParameterType.PARAMETER_ZWAVE_MANUFACTURER_SPECIFIC:
                ManufacturerSpecific mf = (ManufacturerSpecific)value;
				path = "ZWaveNode.ManufacturerSpecific";
                //value = mf.ManufacturerId + ":" + mf.TypeId + ":" + mf.ProductId;
				break;
            case ParameterType.PARAMETER_CONFIG:
                path = "ZWaveNode.Variables." + upargs.ParameterId;
                break;
            case ParameterType.PARAMETER_ASSOC:
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
            case ParameterType.PARAMETER_MULTIINSTANCE_SWITCH_BINARY_COUNT:
                path = "ZWaveNode.MultiInstance.SwitchBinary.Count";
                break;
            case ParameterType.PARAMETER_MULTIINSTANCE_SWITCH_MULTILEVEL_COUNT:
                path = "ZWaveNode.MultiInstance.SwitchMultiLevel.Count";
                break;
            case ParameterType.PARAMETER_MULTIINSTANCE_SENSOR_BINARY_COUNT:
                path = "ZWaveNode.MultiInstance.SensorBinary.Count";
                break;
            case ParameterType.PARAMETER_MULTIINSTANCE_SENSOR_MULTILEVEL_COUNT:
                path = "ZWaveNode.MultiInstance.SensorMultiLevel.Count";
                break;
            case ParameterType.PARAMETER_MULTIINSTANCE_SWITCH_BINARY:
                path = "ZWaveNode.MultiInstance.SwitchBinary." + upargs.ParameterId;
                break;
            case ParameterType.PARAMETER_MULTIINSTANCE_SWITCH_MULTILEVEL:
                path = "ZWaveNode.MultiInstance.SwitchMultiLevel." + upargs.ParameterId;
                break;
            case ParameterType.PARAMETER_MULTIINSTANCE_SENSOR_BINARY:
                path = "ZWaveNode.MultiInstance.SensorBinary." + upargs.ParameterId;
                break;
            case ParameterType.PARAMETER_MULTIINSTANCE_SENSOR_MULTILEVEL:
                path = "ZWaveNode.MultiInstance.SensorMultiLevel." + upargs.ParameterId;
                break;
            case ParameterType.PARAMETER_WAKEUP_INTERVAL:
                path = "ZWaveNode.WakeUpInterval";
                break;
            case ParameterType.PARAMETER_WAKEUP_NOTIFY:
                path = "ZWaveNode.WakeUpNotify";
                break;
            case ParameterType.PARAMETER_BASIC:
                //
                _raisePropertyChanged(new InterfacePropertyChangedAction() { Domain = this.Domain, SourceId = upargs.NodeId.ToString(), SourceType = "ZWave Node", Path = "ZWaveNode.Basic", Value = value });
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
                value = normalizedval.ToString(System.Globalization.CultureInfo.InvariantCulture);
                
                break;
            default:
                Console.WriteLine("UNHANDLED PARAMETER CHANGE FROM NODE {0} ====> Param Type: {1} Param Id:{2} Value:{3}", upargs.NodeId, upargs.ParameterType, upargs.ParameterId, value);
				break;
			}
			//string type = upargs.ParameterType.ToString ();
			//
            _raisePropertyChanged(new InterfacePropertyChangedAction() { Domain = this.Domain, SourceId = upargs.NodeId.ToString(), SourceType = "ZWave Node", Path = path, Value = value });
		}

        private void _raisePropertyChanged(InterfacePropertyChangedAction ifaceaction)
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

		private void controller_ManufacturerSpecificResponse (object sender, ManufacturerSpecificResponseEventArg args)
		{
			if (InterfacePropertyChangedAction != null) {
				try {
					InterfacePropertyChangedAction (new InterfacePropertyChangedAction () { Domain = this.Domain, SourceId = args.NodeId.ToString(), SourceType = "ZWave Node", Path = "ZWaveNode.ManufacturerSpecific", Value = new ManufacturerSpecific() { TypeId = args.ManufacturerSpecific.TypeId, ProductId = args.ManufacturerSpecific.ProductId, ManufacturerId = args.ManufacturerSpecific.ManufacturerId } });
				} catch {
				}
			}
		}


	}

}
