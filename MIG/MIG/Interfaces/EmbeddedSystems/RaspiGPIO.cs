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

/// <summary>
/// 
/// Based on code from 
/// http://www.raspberrypi.org/phpBB3/viewtopic.php?p=88063#p88063
/// 
/// </summary>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using MIG.Interfaces.HomeAutomation.Commons;

namespace MIG.Interfaces.EmbeddedSystems
{
	public class RaspiGPIO : MIGInterface
	{

        #region Implemented MIG Commands

        // typesafe enum
        public sealed class Command : GatewayCommand
        {

            public static Dictionary<int, string> CommandsList = new Dictionary<int, string>()
            {
                {203, "Parameter.Status"},
                {701, "Control.On"},
                {702, "Control.Off"},
                {731, "Control.Reset"},
            };

            // <context>.<command> enum   -   eg. Control.On where <context> :== "Control" and <command> :== "On"
            public static readonly Command PARAMETER_STATUS = new Command(203);
            public static readonly Command CONTROL_ON = new Command(701);
            public static readonly Command CONTROL_OFF = new Command(702);
            public static readonly Command CONTROL_RESET = new Command(731);

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
                    var cmd = from c in CommandsList where c.Value == str select c.Key;
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

        public Dictionary<string, bool> GPIOPins = new Dictionary<string, bool>()
        {
            { "4", false },
            { "17", false },
            { "18", false },
            { "21", false },
            { "22", false },
            { "23", false },
            { "24", false },
            { "25", false }
        };

        bool isconnected = false;

		public RaspiGPIO ()
		{
            isconnected = Directory.Exists(GPIO_PATH);
            CleanUpAllPins();
            //foreach (KeyValuePair<string, bool> kv in GPIOPins)
            //{
            //    GPIOPins[kv.Key] = this.InputPin((enumPIN)int.Parse(kv.Key));
            //}
		}

        #region MIG Interface members

        public event Action<InterfacePropertyChangedAction> InterfacePropertyChangedAction;

		public string Domain {
			get {
				string ifacedomain = this.GetType ().Namespace.ToString ();
				ifacedomain = ifacedomain.Substring (ifacedomain.LastIndexOf (".") + 1) + "." + this.GetType ().Name.ToString ();
				return ifacedomain;
			}
		}
		
		public bool Connect ()
		{
			return isconnected;
		}
		public void Disconnect ()
		{
            CleanUpAllPins();
		}
        public bool IsDevicePresent()
        {
            return isconnected;
        }
        public bool IsConnected
        {
			get { return isconnected; }
		}

		public void WaitOnPending ()
		{
		}

		public object InterfaceControl (MIGInterfaceCommand request)
		{
            Command command = (Command)request.command;
            string returnvalue = "";
			//
			if (command == Command.CONTROL_ON) {
                this.OutputPin((enumPIN)int.Parse(request.nodeid), true);
                GPIOPins[request.nodeid] = true;
                if (InterfacePropertyChangedAction != null)
                {
                    try
                    {
                        InterfacePropertyChangedAction(new InterfacePropertyChangedAction() { Domain = this.Domain, SourceId = request.nodeid, SourceType = "Raspberry Pi GPIO", Path = ModuleParameters.MODPAR_STATUS_LEVEL, Value = 1 });
                    }
                    catch { }
                }
			}
			//
			if (command == Command.CONTROL_OFF) {
                this.OutputPin((enumPIN)int.Parse(request.nodeid), false);
                GPIOPins[request.nodeid] = false;
                if (InterfacePropertyChangedAction != null)
                {
                    try
                    {
                        InterfacePropertyChangedAction(new InterfacePropertyChangedAction() { Domain = this.Domain, SourceId = request.nodeid, SourceType = "Raspberry Pi GPIO", Path = ModuleParameters.MODPAR_STATUS_LEVEL, Value = 0 });
                    }
                    catch { }
                }
            }
			//
			if (command == Command.PARAMETER_STATUS) {
                GPIOPins[request.nodeid] = this.InputPin((enumPIN)int.Parse(request.nodeid));
			}
			//
			if (command == Command.CONTROL_RESET) {
				this.CleanUpAllPins();
                foreach (KeyValuePair<string, bool> kv in GPIOPins)
                {
                    GPIOPins[kv.Key] = false;
                }
            }
			//
			return returnvalue;
		}

        #endregion


        //GPIO connector on the Pi (P1) (as found next to the yellow RCA video socket on the Rpi circuit board)
		//P1-01 = top left,    P1-02 = top right
		//P1-25 = bottom left, P1-26 = bottom right
		//pi connector P1 pin     = GPIOnum = slice of pi v1.0 board label
		//                  P1-07 = GPIO4   = GP7
		//                  P1-11 = GPIO17  = GP0
		//                  P1-12 = GPIO18  = GP1
		//                  P1-13 = GPIO21  = GP2
		//                  P1-15 = GPIO22  = GP3
		//                  P1-16 = GPIO23  = GP4
		//                  P1-18 = GPIO24  = GP5
		//                  P1-22 = GPIO25  = GP6
		//So to turn on Pin7 on the GPIO connector, pass in enumGPIOPIN.gpio4 as the pin parameter
		public enum enumPIN { gpio0 = 0, gpio1 = 1, gpio4 = 4, gpio7 = 7, gpio8 = 8, gpio9 = 9, gpio10 = 10, gpio11 = 11, 
			gpio14 = 14, gpio15 = 15, gpio17 = 17, gpio18 = 18, gpio21 = 21, gpio22 = 22, gpio23 = 23, gpio24 = 24, gpio25 = 25 };
		
		public enum enumDirection { IN, OUT };
		
		private const string GPIO_PATH ="/sys/class/gpio/";
		
		//contains list of pins exported with an OUT direction
		List<enumPIN> _OutExported = new List<enumPIN>();
		
		//contains list of pins exported with an IN direction
		List<enumPIN> _InExported = new List<enumPIN>(); 
		
		//set to true to write whats happening to the screen
		private const bool DEBUG = false; 
		
		//this gets called automatically when we try and output to, or input from, a pin
		private void SetupPin(enumPIN pin, enumDirection direction)
		{
			//unexport if it we're using it already
			if (_OutExported.Contains(pin) || _InExported.Contains(pin)) UnexportPin(pin);
			
			//export
			File.WriteAllText(GPIO_PATH + "export", GetPinNumber(pin));
			
			if (DEBUG) Console.WriteLine("exporting pin " + pin + " as " + direction);
			
			// set i/o direction
			File.WriteAllText(GPIO_PATH + pin.ToString() + "/direction", direction.ToString().ToLower());
			
			//record the fact that we've setup that pin
			if (direction == enumDirection.OUT)
				_OutExported.Add(pin);
			else
				_InExported.Add(pin);
		}
		
		//no need to setup pin this is done for you
		public void OutputPin(enumPIN pin, bool value)
		{
			//if we havent used the pin before,  or if we used it as an input before, set it up
			if (!_OutExported.Contains(pin) || _InExported.Contains(pin)) SetupPin(pin, enumDirection.OUT);
			
			string writeValue = "0";
			if (value) writeValue = "1";
			File.WriteAllText(GPIO_PATH + pin.ToString() + "/value", writeValue);
			
			if (DEBUG) Console.WriteLine("output to pin " + pin + ", value was " + value);
		}
		
		//no need to setup pin this is done for you
		public bool InputPin(enumPIN pin)
		{
			bool returnValue = false;
			
			//if we havent used the pin before, or if we used it as an output before, set it up
			if (!_InExported.Contains(pin) || _OutExported.Contains(pin) ) SetupPin(pin, enumDirection.IN);
			
			string filename = GPIO_PATH + pin.ToString() + "/value";
			if (File.Exists(filename))
			{
				string readValue = File.ReadAllText(filename);
				if (readValue != null && readValue.Length > 0 && readValue[0] == '1') returnValue = true;
			}
			else
				throw new Exception(string.Format("Cannot read from {0}. File does not exist", pin));
			
			if (DEBUG) Console.WriteLine("input from pin " + pin + ", value was " + returnValue);
			
			return returnValue;
		}
		
		//if for any reason you want to unexport a particular pin use this, otherwise just call CleanUpAllPins when you're done
		public void UnexportPin(enumPIN pin)
		{
			bool found = false;
			if (_OutExported.Contains(pin))
			{
				found = true;
				_OutExported.Remove(pin);
			}
			if (_InExported.Contains(pin))
			{
				found = true;
				_InExported.Remove(pin);
			}
			
			if (found)
			{
				File.WriteAllText(GPIO_PATH + "unexport", GetPinNumber(pin));
				if (DEBUG) Console.WriteLine("unexporting  pin " + pin);
			}
		}
		
		public void CleanUpAllPins()
		{
			for (int p = _OutExported.Count - 1; p >= 0; p--) UnexportPin(_OutExported[p]); //unexport in reverse order
			for (int p = _InExported.Count - 1; p >= 0; p--) UnexportPin(_InExported[p]);
		}
		
		private string GetPinNumber(enumPIN pin)
		{
			return ((int) pin).ToString() ; //e.g. returns 17 for enum value of gpio17
		}

	}
}

