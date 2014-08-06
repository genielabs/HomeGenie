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


// MIG Insteon Interface handler it's
// based on SoapBox.FluentDwelling Insteon library.
// Documentation: http://soapboxautomation.com/support-2/fluentdwelling-support/


using System;
using System.Linq;

using SoapBox.FluentDwelling;
using SoapBox.FluentDwelling.Devices;
using System.Collections.Generic;

namespace MIG.Interfaces.HomeAutomation
{
    public class Insteon: MIGInterface
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
                { 706, "Control.Toggle" },
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
            public static readonly Command CONTROL_TOGGLE = new Command(706);
            public static readonly Command CONTROL_ALLLIGHTSON = new Command(721);
            public static readonly Command CONTROL_ALLLIGHTSOFF = new Command(722);

            private readonly String name;
            private readonly int value;

            private Command(int value)
            {
                this.name = CommandsList[ value ];
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

        private Plm insteonPlm;

        public Insteon()
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
            if (insteonPlm != null)
            {
                //
                // discovery
                //
                var database = insteonPlm.GetAllLinkDatabase();
                foreach (var record in database.Records)
                {
                    // You can attempt to connect to each device
                    // to figure out what it is:
                    DeviceBase device;
                    if (insteonPlm.Network.TryConnectToDevice(record.DeviceId, out device))
                    {
                        // It responded.  You can get identification info like this:
                        string address = device.DeviceId.ToString();
                        string category = device.DeviceCategoryCode.ToString();
                        string subcategory = device.DeviceSubcategoryCode.ToString();

                        ModuleTypes type = ModuleTypes.Generic;
                        switch (device.GetType().Name)
                        {
                        case "LightingControl":
                            type = ModuleTypes.Light;
                            break;
                        case "DimmableLightingControl":
                            type = ModuleTypes.Dimmer;
                            break;
                        case "SwitchedLightingControl":
                            type = ModuleTypes.Light;
                            break;
                        case "SensorsActuators":
                            type = ModuleTypes.Switch;
                            break;
                        case "WindowCoveringControl":
                            type = ModuleTypes.DoorWindow;
                            break;
                        }

                        modules.Add(new InterfaceModule() {
                            Domain = this.Domain,
                            Address = address,
                            ModuleType = type,
                            CustomData = category + "/" + subcategory
                        });
                    }
                    else
                    {
                        // couldn't connect - device may have been removed?
                    }
                }
            }
            return modules;
        }

        public bool Connect()
        {
            insteonPlm = new Plm(this.GetOption("Port").Value);
            if (insteonPlm.Error)
            {
                Disconnect();
                return false;
            }
            insteonPlm.OnError += insteonPlm_HandleOnError;
            if (InterfaceModulesChangedAction != null) InterfaceModulesChangedAction(new InterfaceModulesChangedAction(){ Domain = this.Domain });
            return true;
        }

        public void Disconnect()
        {
            if (insteonPlm != null)
            {
                insteonPlm.Dispose();
                insteonPlm = null;
            }
        }

        public bool IsConnected
        {
            get { return (insteonPlm != null && !insteonPlm.Error); }
        }

        public bool IsDevicePresent()
        {
            bool present = true;
            return present;
        }

        public object InterfaceControl(MIGInterfaceCommand request)
        {
            string nodeId = request.NodeId;
            var command = (Command)request.Command;
            string option = request.GetOption(0);

            DeviceBase device = null;
            insteonPlm.Network.TryConnectToDevice(nodeId, out device);

            //TODO: handle types IrrigationControl and PoolAndSpaControl

            if (command == Command.CONTROL_ON)
            {
                if (device != null)
                switch (device.GetType().Name)
                {
                case "LightingControl":
                    (device as LightingControl).TurnOn();
                    break;
                case "DimmableLightingControl":
                    (device as DimmableLightingControl).TurnOn();
                    break;
                case "SwitchedLightingControl":
                    (device as SwitchedLightingControl).TurnOn();
                    break;
                case "SensorsActuators":
                    (device as SensorsActuators).TurnOnOutput(byte.Parse(option));
                    break;
                case "WindowCoveringControl":
                    (device as WindowCoveringControl).Open();
                    break;
                }
            }
            else if (command == Command.CONTROL_OFF)
            {
                if (device != null)
                switch (device.GetType().Name)
                {
                case "LightingControl":
                    (device as LightingControl).TurnOn();
                    break;
                case "DimmableLightingControl":
                    (device as DimmableLightingControl).TurnOn();
                    break;
                case "SwitchedLightingControl":
                    (device as SwitchedLightingControl).TurnOn();
                    break;
                case "SensorsActuators":
                    (device as SensorsActuators).TurnOnOutput(byte.Parse(option));
                    break;
                case "WindowCoveringControl":
                    (device as WindowCoveringControl).Close();
                    break;
                }
            }
            else if (command == Command.CONTROL_BRIGHT)
            {
                if (device != null && device is DimmableLightingControl)
                {
                    (device as DimmableLightingControl).BrightenOneStep();
                }
            }
            else if (command == Command.CONTROL_DIM)
            {
                if (device != null && device is DimmableLightingControl)
                {
                    (device as DimmableLightingControl).DimOneStep();
                }
            }
            else if (command == Command.CONTROL_LEVEL)
            {
                byte level = byte.Parse(option);
                switch (device.GetType().Name)
                {
                case "DimmableLightingControl":
                    (device as DimmableLightingControl).RampOn(level);
                    break;
                case "WindowCoveringControl":
                    (device as WindowCoveringControl).MoveToPosition(level);
                    break;
                }
            }
            else if (command == Command.CONTROL_TOGGLE)
            {
            }
            else if (command == Command.CONTROL_ALLLIGHTSON)
            {
            }
            else if (command == Command.CONTROL_ALLLIGHTSOFF)
            {
            }
            //
            return "";
        }

        #endregion

        #region Insteon Interface events

        private void insteonPlm_HandleOnError (object sender, EventArgs e)
        {
            Console.WriteLine("\nPLM ERROR: " + insteonPlm.Exception.Message +"\n");
        }

        #endregion


    }
}
    