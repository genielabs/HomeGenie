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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;

using XTenLib;
using MIG.Interfaces.HomeAutomation.Commons;

namespace MIG.Interfaces.HomeAutomation
{
    public class X10 : MIGInterface
    {

        #region Implemented MIG Commands

        // typesafe enum
        public sealed class Command : GatewayCommand
        {

            public static Dictionary<int, string> CommandsList = new Dictionary<int, string>() {
                { 203, "Parameter.Status" },
                { 701, "Control.On" },
                { 702, "Control.Off" },
                { 703, "Control.Bright" },
                { 704, "Control.Dim" },
                { 705, "Control.Level" },
                { 706, "Control.Level.Adjust" },
                { 707, "Control.Toggle" },
                { 721, "Control.AllLightsOn" },
                { 722, "Control.AllLightsOff" }
            };

            // <context>.<command> enum   -   eg. Control.On where <context> :== "Control" and <command> :== "On"
            public static readonly Command PARAMETER_STATUS = new Command(203);
            public static readonly Command CONTROL_ON = new Command(701);
            public static readonly Command CONTROL_OFF = new Command(702);
            public static readonly Command CONTROL_BRIGHT = new Command(703);
            public static readonly Command CONTROL_DIM = new Command(704);
            public static readonly Command CONTROL_LEVEL = new Command(705);
            public static readonly Command CONTROL_LEVEL_ADJUST = new Command(706);
            public static readonly Command CONTROL_TOGGLE = new Command(707);
            public static readonly Command CONTROL_ALLLIGHTSON = new Command(721);
            public static readonly Command CONTROL_ALLLIGHTSOFF = new Command(722);

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

        private XTenManager x10lib;
        private Timer rfPulseTimer;
        private List<InterfaceModule> securityModules;

        List<MIGServiceConfiguration.Interface.Option> options;

        public X10()
        {
            x10lib = new XTenManager();
            x10lib.ModuleChanged += X10lib_ModuleChanged;
            x10lib.RfDataReceived += X10lib_RfDataReceived;
            x10lib.RfSecurityReceived += X10lib_RfSecurityReceived;
            securityModules = new List<InterfaceModule>();
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

        public bool IsEnabled { get; set; }

        public List<MIGServiceConfiguration.Interface.Option> Options
        { 
            get
            {
                return options;
            }
            set
            {
                options = value;
                x10lib.PortName = this.GetOption("Port").Value.Replace("|", "/");
                x10lib.HouseCode = this.GetOption("HouseCodes").Value;
            }
        }

        public List<InterfaceModule> GetModules()
        {
            List<InterfaceModule> modules = new List<InterfaceModule>();
            if (x10lib != null)
            {

                InterfaceModule module = new InterfaceModule();

                // CM-15 RF receiver
                if (this.GetOption("Port").Value.Equals("USB"))
                {
                    module.Domain = this.Domain;
                    module.Address = "RF";
                    module.ModuleType = ModuleTypes.Sensor;
                    modules.Add(module);
                }

                // Standard X10 modules
                foreach (var kv in x10lib.Modules)
                {

                    module = new InterfaceModule();
                    module.Domain = this.Domain;
                    module.Address = kv.Value.Code;
                    module.ModuleType = ModuleTypes.Switch;
                    module.Description = "X10 Module";
                    modules.Add(module);

                }

                // CM-15 RF Security modules
                modules.AddRange(securityModules);

            }
            return modules;
        }

        public bool Connect()
        {
            x10lib.PortName = this.GetOption("Port").Value.Replace("|", "/");
            x10lib.HouseCode = this.GetOption("HouseCodes").Value;
            if (InterfaceModulesChangedAction != null)
                InterfaceModulesChangedAction(new InterfaceModulesChangedAction(){ Domain = this.Domain });
            return x10lib.Connect();
        }

        public void Disconnect()
        {
            x10lib.Disconnect();
        }

        public bool IsConnected
        {
            get { return x10lib.IsConnected; }
        }

        public bool IsDevicePresent()
        {
            //bool present = false;
            ////
            ////TODO: implement serial port scanning for CM11 as well
            //foreach (UsbRegistry usbdev in LibUsbDevice.AllDevices)
            //{
            //    //Console.WriteLine(o.Vid + " " + o.SymbolicName + " " + o.Pid + " " + o.Rev + " " + o.FullName + " " + o.Name + " ");
            //    if ((usbdev.Vid == 0x0BC7 && usbdev.Pid == 0x0001) || usbdev.FullName.ToUpper().Contains("X10"))
            //    {
            //        present = true;
            //        break;
            //    }
            //}
            //return present;
            return true;
        }

        public object InterfaceControl(MIGInterfaceCommand request)
        {
            string response = "[{ ResponseValue : 'OK' }]";

            string nodeId = request.NodeId;
            var command = (Command)request.Command;
            string option = request.GetOption(0);

            // Parse house/unit
            var houseCode = XTenLib.Utility.HouseCodeFromString(nodeId);
            var unitCode = XTenLib.Utility.UnitCodeFromString(nodeId);

            // Modules control
            if (command == Command.PARAMETER_STATUS)
            {
                x10lib.StatusRequest(houseCode, unitCode);
            }
            else if (command == Command.CONTROL_ON)
            {
                x10lib.UnitOn(houseCode, unitCode);
            }
            else if (command == Command.CONTROL_OFF)
            {
                x10lib.UnitOff(houseCode, unitCode);
            }
            else if (command == Command.CONTROL_BRIGHT)
            {
                x10lib.Bright(houseCode, unitCode, int.Parse(option));
            }
            else if (command == Command.CONTROL_DIM)
            {
                x10lib.Dim(houseCode, unitCode, int.Parse(option));
            }
            else if (command == Command.CONTROL_LEVEL_ADJUST)
            {
                int dimvalue = int.Parse(option);
                //x10lib.Modules[nodeId].Level = ((double)dimvalue/100D);
                RaisePropertyChanged(this.Domain, nodeId, "X10 Module", ModuleParameters.MODPAR_STATUS_LEVEL, x10lib.Modules[nodeId].Level);
                throw(new NotImplementedException("X10 CONTROL_LEVEL_ADJUST Not Implemented"));
            }
            else if (command == Command.CONTROL_LEVEL)
            {
                int dimvalue = int.Parse(option) - (int)(x10lib.Modules[nodeId].Level * 100.0);
                if (dimvalue > 0)
                {
                    x10lib.Bright(houseCode, unitCode, dimvalue);
                }
                else if (dimvalue < 0)
                {
                    x10lib.Dim(houseCode, unitCode, -dimvalue);
                }
            }
            else if (command == Command.CONTROL_TOGGLE)
            {
                string huc = XTenLib.Utility.HouseUnitCodeFromEnum(houseCode, unitCode);
                if (x10lib.Modules[huc].Level == 0)
                {
                    x10lib.UnitOn(houseCode, unitCode);
                }
                else
                {
                    x10lib.UnitOff(houseCode, unitCode);
                }
            }
            else if (command == Command.CONTROL_ALLLIGHTSON)
            {
                x10lib.AllLightsOn(houseCode);
            }
            else if (command == Command.CONTROL_ALLLIGHTSOFF)
            {
                x10lib.AllUnitsOff(houseCode);
            }
            //
            return response;
        }

        #endregion

        private void RaisePropertyChanged(string domain, string address, string source, string property, object val)
        {
            if (InterfacePropertyChangedAction != null)
            {
                var evt = new InterfacePropertyChangedAction() {
                    Domain = domain,
                    SourceId = address,
                    SourceType = source,
                    Path = property,
                    Value = val
                };
                InterfacePropertyChangedAction(evt);
            }
        }

        private void X10lib_RfSecurityReceived(object sender, RfSecurityReceivedEventArgs args)
        {
            string address = "S-" + args.Address.ToString("X6");
            var moduleType = ModuleTypes.Sensor;
            if (args.Event.ToString().StartsWith("DoorSensor1_"))
            {
                address += "01";
                moduleType = ModuleTypes.DoorWindow;
            }
            else if (args.Event.ToString().StartsWith("DoorSensor2_"))
            {
                address += "02";
                moduleType = ModuleTypes.DoorWindow;
            }
            else if (args.Event.ToString().StartsWith("Motion_"))
            {
                moduleType = ModuleTypes.Sensor;
            }
            else if (args.Event.ToString().StartsWith("Remote_"))
            {
                address = "S-REMOTE";
                moduleType = ModuleTypes.Sensor;
            }
            var module = securityModules.Find(m => m.Address == address);
            if (module == null)
            {
                module = new InterfaceModule();
                module.Domain = this.Domain;
                module.Address = address;
                module.Description = "X10 Security";
                module.ModuleType = moduleType;
                module.CustomData = 0.0D;
                securityModules.Add(module);
                RaisePropertyChanged(this.Domain, "RF", "X10 RF Receiver", "Receiver.Status", "Added security module " + address);
                if (InterfaceModulesChangedAction != null)
                    InterfaceModulesChangedAction(new InterfaceModulesChangedAction(){ Domain = this.Domain });
            }
            switch (args.Event)
            {
            case X10RfSecurityEvent.DoorSensor1_Alert:
            case X10RfSecurityEvent.DoorSensor2_Alert:
                RaisePropertyChanged(module.Domain, module.Address, "X10 Module", ModuleParameters.MODPAR_STATUS_LEVEL, 1);
                break;
            case X10RfSecurityEvent.DoorSensor1_Normal:
            case X10RfSecurityEvent.DoorSensor2_Normal:
                RaisePropertyChanged(module.Domain, module.Address, "X10 Module", ModuleParameters.MODPAR_STATUS_LEVEL, 0);
                break;
            case X10RfSecurityEvent.DoorSensor1_BatteryLow:
            case X10RfSecurityEvent.DoorSensor2_BatteryLow:
                RaisePropertyChanged(module.Domain, module.Address, "X10 Module", ModuleParameters.MODPAR_STATUS_BATTERY, 10);
                break;
            case X10RfSecurityEvent.DoorSensor1_BatteryOk:
            case X10RfSecurityEvent.DoorSensor2_BatteryOk:
                RaisePropertyChanged(module.Domain, module.Address, "X10 Module", ModuleParameters.MODPAR_STATUS_BATTERY, 100);
                break;
            case X10RfSecurityEvent.Motion_Alert:
                RaisePropertyChanged(module.Domain, module.Address, "X10 Module", ModuleParameters.MODPAR_STATUS_LEVEL, 1);
                break;
            case X10RfSecurityEvent.Motion_Normal:
                RaisePropertyChanged(module.Domain, module.Address, "X10 Module", ModuleParameters.MODPAR_STATUS_LEVEL, 0);
                break;
            case X10RfSecurityEvent.Remote_Arm:
            case X10RfSecurityEvent.Remote_Disarm:
            case X10RfSecurityEvent.Remote_Panic:
            case X10RfSecurityEvent.Remote_LightOn:
            case X10RfSecurityEvent.Remote_LightOff:
                var evt = args.Event.ToString();
                evt = evt.Substring(evt.IndexOf('_') + 1);
                RaisePropertyChanged(module.Domain, module.Address, "X10 Module", "Sensor.Key", evt);
                break;
            }
        }

        private void X10lib_RfDataReceived(object sender, RfDataReceivedEventArgs args)
        {
            var code = BitConverter.ToString(args.Data).Replace("-", " ");
            RaisePropertyChanged(this.Domain, "RF", "X10 RF Receiver", "Receiver.RawData", code);
            if (rfPulseTimer == null)
            {
                rfPulseTimer = new Timer(delegate(object target)
                {
                    RaisePropertyChanged(this.Domain, "RF", "X10 RF Receiver", "Receiver.RawData", "");
                });
            }
            rfPulseTimer.Change(1000, Timeout.Infinite);
        }

        private void X10lib_ModuleChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Level")
                RaisePropertyChanged(this.Domain, (sender as X10Module).Code, (sender as X10Module).Description, ModuleParameters.MODPAR_STATUS_LEVEL, (sender as X10Module).Level.ToString());
        }

    }
}
