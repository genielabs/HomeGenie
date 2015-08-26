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
using System.Threading;

using W800Rf32Lib;
using System.Collections.Generic;
using MIG.Interfaces.HomeAutomation.Commons;

namespace MIG.Interfaces.HomeAutomation
{
    public class W800RF : MIGInterface
    {
        private const string X10_DOMAIN = "HomeAutomation.X10";
        private RfReceiver w800Rf32;
        private Timer rfPulseTimer;
        private List<InterfaceModule> modules;

        // TODO: Add option "Disable Virtual Modules"
        // TODO: Add option "Discard unrecognized RF messages"

        public W800RF()
        {
            w800Rf32 = new RfReceiver();
            w800Rf32.RfCommandReceived += W800Rf32_RfCommandReceived;
            w800Rf32.RfDataReceived += W800Rf32_RfDataReceived;
            w800Rf32.RfSecurityReceived += W800Rf32_RfSecurityReceived;
            modules = new List<InterfaceModule>();
            // Add RF receiver module
            InterfaceModule module = new InterfaceModule();
            module.Domain = this.Domain;
            module.Address = "RF";
            module.ModuleType = ModuleTypes.Sensor;
            modules.Add(module);
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

        public List<MIGServiceConfiguration.Interface.Option> Options { get; set; }

        public List<InterfaceModule> GetModules()
        {
            return modules;
        }

        public bool Connect()
        {
            w800Rf32.PortName = this.GetOption("Port").Value;
            if (InterfaceModulesChangedAction != null)
                InterfaceModulesChangedAction(new InterfaceModulesChangedAction(){ Domain = this.Domain });
            return w800Rf32.Connect();
        }

        public void Disconnect()
        {
            w800Rf32.Disconnect();
        }

        public bool IsConnected
        {
            get { return w800Rf32.IsConnected; }
        }

        public bool IsDevicePresent()
        {
            bool present = true;
            return present;
        }

        public object InterfaceControl(MIGInterfaceCommand request)
        {
            return "";
        }

        #endregion

        private void W800Rf32_RfSecurityReceived(object sender, RfSecurityReceivedEventArgs args)
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
            var module = modules.Find(m => m.Address == address);
            if (module == null)
            {
                module = new InterfaceModule();
                module.Domain = X10_DOMAIN;
                module.Address = address;
                module.Description = "W800RF32 security module";
                module.ModuleType = moduleType;
                module.CustomData = 0.0D;
                modules.Add(module);
                RaisePropertyChanged(this.Domain, "1", "W800RF32 Receiver", "Receiver.Status", "Added security module " + address);
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

        private void W800Rf32_RfCommandReceived(object sender, RfCommandReceivedEventArgs args)
        {
            string address = args.HouseCode.ToString() + args.UnitCode.ToString().Split('_')[1];
            if (args.UnitCode == X10UnitCode.Unit_NotSet)
                return;
            var module = modules.Find(m => m.Address == address);
            if (module == null)
            {
                module = new InterfaceModule();
                module.Domain = X10_DOMAIN;
                module.Address = address;
                module.Description = "W800RF32 module";
                module.ModuleType = ModuleTypes.Switch;
                module.CustomData = 0.0D;
                modules.Add(module);
                RaisePropertyChanged(this.Domain, "1", "W800RF32 Receiver", "Receiver.Status", "Added module " + address);
                if (InterfaceModulesChangedAction != null)
                    InterfaceModulesChangedAction(new InterfaceModulesChangedAction(){ Domain = this.Domain });
            }
            switch (args.Command)
            {
            case X10RfFunction.On:
                module.CustomData = 1.0D;
                break;
            case X10RfFunction.Off:
                module.CustomData = 0.0D;
                break;
            case X10RfFunction.Bright:
                double lbri = module.CustomData;
                lbri += 0.1;
                if (lbri > 1)
                    lbri = 1;
                module.CustomData = lbri;
                break;
            case X10RfFunction.Dim:
                double ldim = module.CustomData;
                ldim -= 0.1;
                if (ldim < 0)
                    ldim = 0;
                module.CustomData = ldim;
                break;
            case X10RfFunction.AllLightsOn:
                break;
            case X10RfFunction.AllLightsOff:
                break;
            }
            RaisePropertyChanged(module.Domain, module.Address, "X10 Module", ModuleParameters.MODPAR_STATUS_LEVEL, module.CustomData);
        }

        private void W800Rf32_RfDataReceived(object sender, RfDataReceivedEventArgs args)
        {
            var code = BitConverter.ToString(args.Data).Replace("-", " ");
            RaisePropertyChanged(this.Domain, "RF", "W800RF32 RF Receiver", "Receiver.RawData", code);
            if (rfPulseTimer == null)
            {
                rfPulseTimer = new Timer(delegate(object target)
                {
                    RaisePropertyChanged(this.Domain, "RF", "W800RF32 RF Receiver", "Receiver.RawData", "");
                });
            }
            rfPulseTimer.Change(1000, Timeout.Infinite);
        }

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

    }
}

