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
using System.Globalization;
using System.Collections.Generic;
using System.Threading;

using SoapBox.FluentDwelling;
using SoapBox.FluentDwelling.Devices;

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
        private Plm insteonPlm;
        private Thread readerTask;

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

        /// <summary>
        /// get modules and module properties
        /// </summary>
        public List<InterfaceModule> Modules { get; set; }

        public List<InterfaceModule> GetModules()
        {
            // TODO: make 'modules' data persistent in order to store status for various X10 operations (eg. like Control.Level)
            List<InterfaceModule> modules = new List<InterfaceModule>();
            if (insteonPlm != null)
            {
                //
                // X10 modules
                //
                var x10HouseCodes = this.GetOption("HouseCodes");
                if (x10HouseCodes != null && !String.IsNullOrEmpty(x10HouseCodes.Value))
                {
                    string[] hc = x10HouseCodes.Value.Split(',');
                    for (int i = 0; i < hc.Length; i++)
                    {
                        for (int x = 1; x <= 16; x++)
                        {
                            modules.Add(new InterfaceModule() {
                                Domain = this.Domain,
                                Address = (hc[i] + x.ToString()),
                                ModuleType = ModuleTypes.Generic,
                                Description = "X10 Module"
                            });
                        }
                    }
                }
                //
                // Insteon devices discovery
                //
                var database = insteonPlm.GetAllLinkDatabase();
                foreach (var record in database.Records)
                {
                    // Connect to each device to figure out what it is
                    DeviceBase device;
                    if (insteonPlm.Network.TryConnectToDevice(record.DeviceId, out device))
                    {
                        // It responded. Get identification info
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
                        case "PoolAndSpaControl":
                            type = ModuleTypes.Thermostat;
                            break;
                        case "IrrigationControl":
                            type = ModuleTypes.Switch;
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
                        // couldn't connect - device removed?
                    }
                }
            }
            return modules;
        }

        public bool Connect()
        {
            Disconnect();
            insteonPlm = new Plm(this.GetOption("Port").Value);
            insteonPlm.OnError += insteonPlm_HandleOnError;
            /* 

            //TODO: implement incoming events handling as well:

            insteonPlm.Network.StandardMessageReceived
                += new StandardMessageReceivedHandler((s, e) =>
            {
                Console.WriteLine("Message received: " + e.Description
                    + ", from " + e.PeerId.ToString());
            });

            insteonPlm.Network.X10.CommandReceived
            += new X10CommandReceivedHandler((s, e) =>
            {
                Console.WriteLine("X10 Command Received: House Code " + e.HouseCode
                    + ", Command: " + e.Command.ToString());
            });

            // TODO: also see:

            insteonPlm.SetButton.PressedAndHeld
            insteonPlm.SetButton.ReleasedAfterHolding
            insteonPlm.SetButton.UserReset (SET Button Held During Power-up)
            insteonPlm.Network.SendStandardCommandToAddress ...

            */
            if (insteonPlm.Error)
            {
                Disconnect();
                return false;
            }
            //
            readerTask = new Thread(() =>
            {
                while (insteonPlm != null)
                {
                    insteonPlm.Receive();
                    System.Threading.Thread.Sleep(100); // wait 100 ms
                }
            });
            readerTask.Start();
            //
            if (InterfaceModulesChangedAction != null)
                InterfaceModulesChangedAction(new InterfaceModulesChangedAction() { Domain = this.Domain });
            return true;
        }

        public void Disconnect()
        {
            if (insteonPlm != null)
            {
                insteonPlm.OnError -= insteonPlm_HandleOnError;
                try
                {
                    insteonPlm.Dispose();
                }
                catch
                {
                }
                insteonPlm = null;
            }
            if (readerTask != null)
            {
                try
                {
                    readerTask.Abort();
                }
                catch
                {
                }
                readerTask = null;
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
            bool raisePropertyChanged = false;
            string parameterPath = "Status.Level";
            string raiseParameter = "";
            //
            string nodeId = request.NodeId;
            var command = (Command)request.Command;
            string option = request.GetOption(0);

            bool isDottedHexId = (nodeId.IndexOf(".") > 0);

            if (isDottedHexId)
            {
                // Standard Insteon device

                DeviceBase device = null;
                insteonPlm.Network.TryConnectToDevice(nodeId, out device);

                //TODO: handle types IrrigationControl and PoolAndSpaControl

                if (command == Command.CONTROL_ON)
                {
                    raisePropertyChanged = true;
                    raiseParameter = "1";
                    //
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
                        case "PoolAndSpaControl":
                            break;
                        case "IrrigationControl":
                            (device as IrrigationControl).TurnOnSprinklerValve(byte.Parse(option));
                            break;
                        }
                }
                else if (command == Command.CONTROL_OFF)
                {
                    raisePropertyChanged = true;
                    raiseParameter = "0";
                    //
                    if (device != null)
                        switch (device.GetType().Name)
                        {
                        case "LightingControl":
                            (device as LightingControl).TurnOff();
                            break;
                        case "DimmableLightingControl":
                            (device as DimmableLightingControl).TurnOff();
                            break;
                        case "SwitchedLightingControl":
                            (device as SwitchedLightingControl).TurnOff();
                            break;
                        case "SensorsActuators":
                            (device as SensorsActuators).TurnOffOutput(byte.Parse(option));
                            break;
                        case "WindowCoveringControl":
                            (device as WindowCoveringControl).Close();
                            break;
                        case "PoolAndSpaControl":
                            break;
                        case "IrrigationControl":
                            (device as IrrigationControl).TurnOffSprinklerValve(byte.Parse(option));
                            break;
                        }
                }
                else if (command == Command.CONTROL_BRIGHT)
                {
                    // TODO: raise parameter change event
                    if (device != null && device is DimmableLightingControl)
                    {
                        (device as DimmableLightingControl).BrightenOneStep();
                    }
                }
                else if (command == Command.CONTROL_DIM)
                {
                    // TODO: raise parameter change event
                    if (device != null && device is DimmableLightingControl)
                    {
                        (device as DimmableLightingControl).DimOneStep();
                    }
                }
                else if (command == Command.CONTROL_LEVEL)
                {
                    double adjustedLevel = (double.Parse(option) / 100D);
                    raisePropertyChanged = true;
                    raiseParameter = adjustedLevel.ToString(CultureInfo.InvariantCulture);
                    //
                    byte level = (byte)((double.Parse(option) / 100D) * 255);
                    if (device != null)
                        switch (device.GetType().Name)
                        {
                        case "DimmableLightingControl":
                            (device as DimmableLightingControl).TurnOn(level);
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
            }
            else
            {
                // It is not a dotted hex addres, so fallback to X10 device control
                var x10plm = insteonPlm.Network.X10;

                // Parse house/unit
                string houseCode = nodeId.Substring(0, 1);
                byte unitCode = byte.Parse(nodeId.Substring(1));

                // Modules control
                if (command == Command.PARAMETER_STATUS)
                {
                    x10plm
                        .House(houseCode.ToString())
                        .Unit(unitCode)
                        .Command(X10Command.StatusRequest);
                }
                else if (command == Command.CONTROL_ON)
                {
                    raisePropertyChanged = true;
                    raiseParameter = "1";
                    //
                    x10plm
                        .House(houseCode)
                        .Unit(unitCode)
                        .Command(X10Command.On);
                }
                else if (command == Command.CONTROL_OFF)
                {
                    raisePropertyChanged = true;
                    raiseParameter = "0";
                    //
                    x10plm
                        .House(houseCode)
                        .Unit(unitCode)
                        .Command(X10Command.Off);
                }
                else if (command == Command.CONTROL_BRIGHT)
                {
                    // TODO: raise parameter change event
                    int amount = int.Parse(option);
                    // TODO: how to specify bright amount parameter???
                    x10plm
                        .House(houseCode)
                        .Unit(unitCode)
                        .Command(X10Command.Bright);
                }
                else if (command == Command.CONTROL_DIM)
                {
                    // TODO: raise parameter change event
                    int amount = int.Parse(option);
                    // TODO: how to specify dim amount parameter???
                    x10plm
                        .House(houseCode)
                        .Unit(unitCode)
                        .Command(X10Command.Dim);
                }
                else if (command == Command.CONTROL_LEVEL)
                {
                    double adjustedLevel = (double.Parse(option) / 100D);
                    raisePropertyChanged = true;
                    raiseParameter = adjustedLevel.ToString(CultureInfo.InvariantCulture);
                    //
                    /*int dimvalue = int.Parse(option) - (int)(x10lib.ModulesStatus[ nodeId ].Level * 100.0);
                    if (dimvalue > 0)
                    {
                        x10lib.Bright(houseCode, unitCode, dimvalue);
                    }
                    else if (dimvalue < 0)
                    {
                        x10lib.Dim(houseCode, unitCode, -dimvalue);
                    }*/
                }
                else if (command == Command.CONTROL_TOGGLE)
                {
                    /*
                    string huc = XTenLib.Utility.HouseUnitCodeFromEnum(houseCode, unitCode);
                    if (x10lib.ModulesStatus[ huc ].Level == 0)
                    {
                        x10lib.LightOn(houseCode, unitCode);
                    }
                    else
                    {
                        x10lib.LightOff(houseCode, unitCode);
                    }
                    */
                }
                else if (command == Command.CONTROL_ALLLIGHTSON)
                {
                    // TODO: ...
                    x10plm
                        .House(houseCode)
                        .Command(X10Command.AllLightsOn);
                }
                else if (command == Command.CONTROL_ALLLIGHTSOFF)
                {
                    // TODO: ...
                    x10plm
                        .House(houseCode)
                        .Command(X10Command.AllLightsOff);
                }

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
                        SourceType = "Insteon Device",
                        Path = parameterPath,
                        Value = raiseParameter
                    });
                }
                catch
                {
                }
            }
            //
            return "";
        }
        #endregion

        #region Insteon Interface events
        private void insteonPlm_HandleOnError(object sender, EventArgs e)
        {
            Console.WriteLine("\nPLM ERROR: " + insteonPlm.Exception.Message + "\n");
        }
        #endregion
    }
}
    