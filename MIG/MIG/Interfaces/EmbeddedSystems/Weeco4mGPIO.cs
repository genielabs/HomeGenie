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
 * 2014-02-24
 *      Weecoboard-4M module
 *      Author: Luciano Neri <l.neri@nerinformatica.it>
*/


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using MIG.Interfaces.HomeAutomation.Commons;

namespace MIG.Interfaces.EmbeddedSystems
{
    public class Weeco4mGPIO : MIGInterface
    {

        // echo 10000 > /sys/kernel/lgw4m-8di/counter_limit_value
        // echo gpio > /sys/class/leds/led_green/trigger


			
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
				if (DEBUG_API) Console.WriteLine("Weeco4mGPIO Command " + str);

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

		private const string GPIO_LED_PATH ="/sys/class/leds/";
        private const string GPIO_INOUT_PATH = "/sys/kernel/lgw4m-8di/";

        private Dictionary<string, string> CrossRefPins = new Dictionary<string, string>()		
        {
        	{ "0", GPIO_INOUT_PATH + "in0_value" },
        	{ "1", GPIO_INOUT_PATH + "in1_value" },
        	{ "2", GPIO_INOUT_PATH + "in2_value" },
        	{ "3", GPIO_INOUT_PATH + "in3_value" },
        	{ "4", GPIO_INOUT_PATH + "in4_value" },
        	{ "5", GPIO_INOUT_PATH + "in5_value" },
        	{ "6", GPIO_INOUT_PATH + "in6_value" },
        	{ "7", GPIO_INOUT_PATH + "in7_value" },
			
        	{ "8", GPIO_LED_PATH + "led_orange/brightness" },
        	{ "9", GPIO_LED_PATH + "led_green/brightness" },
        	{ "10", GPIO_LED_PATH + "output1/brightness" },
        	{ "11", GPIO_LED_PATH + "output2/brightness" },
			
        	{ "16", GPIO_INOUT_PATH + "in0_counter" },
        	{ "17", GPIO_INOUT_PATH + "in1_counter" },			
        	{ "18", GPIO_INOUT_PATH + "in2_counter" },			
        	{ "19", GPIO_INOUT_PATH + "in3_counter" },			
        	{ "20", GPIO_INOUT_PATH + "in4_counter" },
        	{ "21", GPIO_INOUT_PATH + "in5_counter" },
        	{ "22", GPIO_INOUT_PATH + "in6_counter" },			
        	{ "23", GPIO_INOUT_PATH + "in7_counter" },
			
        	{ "32", GPIO_INOUT_PATH + "in0_periode" },			
        	{ "33", GPIO_INOUT_PATH + "in1_periode" },	
        	{ "34", GPIO_INOUT_PATH + "in2_periode" },			
        	{ "35", GPIO_INOUT_PATH + "in3_periode" },				
        	{ "36", GPIO_INOUT_PATH + "in4_periode" },			
        	{ "37", GPIO_INOUT_PATH + "in5_periode" },				
        	{ "38", GPIO_INOUT_PATH + "in6_periode" },			
        	{ "39", GPIO_INOUT_PATH + "in7_periode" }			
			
		};

		
		
        public Dictionary<string, bool> GPIOPins = new Dictionary<string, bool>()
		{
            { "0", false },
            { "1", false },
            { "2", false },
            { "3", false },
            { "4", false },
            { "5", false },
            { "6", false },
            { "7", false },
			
            { "8", false },
            { "9", false },
			
            { "10", false },
            { "11", false }
		};

        public Dictionary<string, string> REGPins = new Dictionary<string, string>()
		{			
            { "16", "0" },
            { "17", "0" },
            { "18", "0" },
            { "19", "0" },
            { "20", "0" },
            { "21", "0" },
            { "22", "0" },
            { "23", "0" },
		
            { "32", "0" },
            { "33", "0" },
            { "34", "0" },
            { "35", "0" },
            { "36", "0" },
            { "37", "0" },
            { "38", "0" },
            { "39", "0" }			
        };

		
		private const int REGSTORAGE_IDX_STATUS = 0;
		private const int REGSTORAGE_IDX_COUNTER = 1;
		private const int REGSTORAGE_IDX_PERIODE = 2;
		private const int REGSTORAGE_IDX_LAST_COUNTER = 3;
		private const int REGSTORAGE_IDX_LAST_PERIODE = 4;
		private const int REGSTORAGE_IDX_LAST_PERIODE_TIME = 5;
		private const int REGSTORAGE_NUMOF_INDEX = 6;
		private const int REGSTORAGE_NUMOF_REGS = 64;	

		
        private bool isconnected = false;
		private bool isDesktopEmulation = false;
		private Thread myThread = null;
        private UInt32[,] regsStorage;
        private uint NumOfInputPin = 6;						// which pin, from 0, will be used as input, the remaining will be used as power counter
        private Double Pulse4Watt = 1.0;					// pulse for watt
		private const int ThreadPeriodeMs = 1000;
		private const int PeriodeToSendCountersMs = 60000;

        public Weeco4mGPIO()
        {
			regsStorage = new UInt32[REGSTORAGE_NUMOF_INDEX,REGSTORAGE_NUMOF_REGS];
            isconnected = Directory.Exists(GPIO_INOUT_PATH) && Directory.Exists(GPIO_LED_PATH);
			if (DEBUG_API)
            {
                isconnected = true;
                isDesktopEmulation = true;
            }
            if (isconnected)
            {
				myThread = new System.Threading.Thread(new System.Threading.ThreadStart(ThreadSendData));
                myThread.Start();
            }
        }

        #region MIG Interface members

        public event Action<InterfacePropertyChangedAction> InterfacePropertyChangedAction;


		public string Domain {
			get {
				string ifacedomain = this.GetType().Namespace.ToString ();
				ifacedomain = ifacedomain.Substring(ifacedomain.LastIndexOf (".") + 1) + "." + this.GetType ().Name.ToString ();
                return ifacedomain;
            }
        }

        public bool Connect()
        {
            return isconnected;
        }
        public void Disconnect()
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

        public void WaitOnPending()
        {
        }

		public void SetInputPin(uint pin)
		{
			if (pin >=0 && pin <=7)
				NumOfInputPin = pin;
		}
		
		public uint GetInputPin()
		{
			return NumOfInputPin;
		}
		
		public double GetPulsePerWatt()
		{
			return Pulse4Watt;
		}
		
		public void SetPulsePerWatt(double pulseXwatt)
        {
			if (pulseXwatt >= 0.0)
				Pulse4Watt=pulseXwatt;
		}
		

		public object InterfaceControl(MIGInterfaceCommand request)
		{
			if (DEBUG_API) Console.WriteLine("Weeco4mGPIO Command : " + request.command);

            Command command = (Command)request.command;
            string returnvalue = "";
            //
			if (command == Command.CONTROL_ON) {
				if (DEBUG_API) Console.WriteLine("Weeco4mGPIO Command.CONTROL_ON ");

                this.OutputPin(request.nodeid, "1");
                if (IsRegister(request.nodeid))
                    REGPins[request.nodeid] = "1";
                else
                    GPIOPins[request.nodeid] = true;
                if (InterfacePropertyChangedAction != null)
                {
                    try
                    {
                        InterfacePropertyChangedAction(new InterfacePropertyChangedAction() { Domain = this.Domain, SourceId = request.nodeid, SourceType = "Weeco-4M GPIO", Path = ModuleParameters.MODPAR_STATUS_LEVEL, Value = 1 });
                    }
                    catch { }
                }
			}else if (command == Command.CONTROL_OFF) {
				if (DEBUG_API) Console.WriteLine("Weeco4mGPIO Command.CONTROL_OFF ");

                this.OutputPin(request.nodeid, "0");
                if (IsRegister(request.nodeid))
                    REGPins[request.nodeid] = "0";
                else
                    GPIOPins[request.nodeid] = false;

                if (InterfacePropertyChangedAction != null)
                {
                    try
                    {
                        InterfacePropertyChangedAction(new InterfacePropertyChangedAction() { Domain = this.Domain, SourceId = request.nodeid, SourceType = "Weeco-4M GPIO", Path = ModuleParameters.MODPAR_STATUS_LEVEL, Value = 0 });
                    }
                    catch { }
                }
            }else if (command == Command.PARAMETER_STATUS) {
				if (DEBUG_API) Console.WriteLine("Weeco4mGPIO Command.PARAMETER_STATUS ");				
                if (!IsRegister(request.nodeid))
                    GPIOPins[request.nodeid] = this.InputPin(request.nodeid).CompareTo("0") == 0 ? false : true;
                else
                    REGPins[request.nodeid] = this.InputPin(request.nodeid);
			}else if (command == Command.CONTROL_RESET) {
                this.CleanUpAllPins();
                foreach (KeyValuePair<string, bool> kv in GPIOPins)
                {
                    GPIOPins[kv.Key] = false;
                }
                foreach (KeyValuePair<string, string> kv in REGPins)
                {
                    REGPins[kv.Key] = "0";
                }
            }

            return returnvalue;
        }

        #endregion


		public enum enumPIN { gpio0 = 0, gpio1 = 1, gpio2 = 2, gpio3 = 3, gpio4 = 4, gpio5 = 5, gpio6 = 6, gpio7= 7,	// input
            gpio16 = 16, gpio17 = 17, gpio18 = 18, gpio19 = 19, gpio20 = 20, gpio21 = 21, gpio22 = 22, gpio23 = 23,	// counters
            gpio24 = 32, gpio25 = 33, gpio26 = 34, gpio27 = 35, gpio28 = 36, gpio29 = 37, gpio30 = 38, gpio31 = 39,	// periode
							  gpio8 = 8, gpio9 = 9, gpio10 = 10, gpio11=11};	// output (leds and out)

        public enum enumDirection { IN, OUT };


		
        //set to true to write whats happening to the screen
		private const bool DEBUG_MEAS = false; 
		private const bool DEBUG_API = false; 
		private const bool DESKTOP = false; 
		

        private bool IsRegister(string nodeid)
        {
            return Int32.Parse(nodeid) > 15;
        }

        //no need to setup pin this is done for you
        public void OutputPin(string pin, string value)
        {
			if (!isDesktopEmulation)
                File.WriteAllText(CrossRefPins[pin], value);

			if (DEBUG_API) Console.WriteLine("Weeco4mGPIO  output to pin " + CrossRefPins[pin] + ", value was " + value);
        }

        //no need to setup pin this is done for you
        public string InputPin(string pin)
        {
            string returnValue = "0";

            string filename = CrossRefPins[pin];

			if (!isDesktopEmulation)
            {
                if (File.Exists(filename))
                {
                    returnValue = File.ReadAllText(filename);
                }
                else
                    throw new Exception(string.Format("Cannot read from {0}. File does not exist", pin));
            }
            else
				returnValue="-1";

			if (DEBUG_API) Console.WriteLine("Weeco4mGPIO  input from pin " + CrossRefPins[pin] + ", value was " + returnValue);

            return returnValue;
        }

        public void CleanUpAllPins()
        {
            OutputPin("8", "0");
            OutputPin("9", "0");
            OutputPin("10", "0");
            OutputPin("11", "0");

			OutputPin("16", "0");
            OutputPin("17", "0");
            OutputPin("18", "0");
            OutputPin("19", "0");
            OutputPin("20", "0");
            OutputPin("21", "0");
            OutputPin("22", "0");
            OutputPin("23", "0");
		
            OutputPin("32", "0");
            OutputPin("33", "0");
            OutputPin("34", "0");
            OutputPin("35", "0");
            OutputPin("36", "0");
            OutputPin("37", "0");
            OutputPin("38", "0");
            OutputPin("39", "0");
		}
		
		private void ReadAndSendValues(bool sendCounters)
		{
			Double currWatt, diffMeas, deltaMeasPerc;
			if(Pulse4Watt == 0)
				Pulse4Watt = 1.0;
			
			for (int currIdx=0;currIdx<8;currIdx++)
			{
				if (DESKTOP)
				{
					regsStorage[REGSTORAGE_IDX_STATUS,currIdx] = (regsStorage[REGSTORAGE_IDX_STATUS,currIdx] == 0) ? (uint)1 : (uint)0;
					regsStorage[REGSTORAGE_IDX_COUNTER,currIdx] +=1;		// read counter
					regsStorage[REGSTORAGE_IDX_PERIODE,currIdx] +=4;
                }
                else
                {
					regsStorage[REGSTORAGE_IDX_STATUS,currIdx] = UInt32.Parse(File.ReadAllText(CrossRefPins[currIdx.ToString()]));			// read input
					regsStorage[REGSTORAGE_IDX_COUNTER,currIdx] = UInt32.Parse(File.ReadAllText(CrossRefPins[(currIdx+16).ToString()]));		// read counter
					regsStorage[REGSTORAGE_IDX_PERIODE,currIdx] = UInt32.Parse(File.ReadAllText(CrossRefPins[(currIdx+32).ToString()]))/1000;	// read pulse
                }
                if (currIdx < NumOfInputPin)
                {
                    // analyse only input pin	
					if (regsStorage[REGSTORAGE_IDX_COUNTER,currIdx] != regsStorage[REGSTORAGE_IDX_LAST_COUNTER,currIdx])
                    {
                        // is different, send event
						InterfacePropertyChangedAction(new InterfacePropertyChangedAction() { Domain = this.Domain, SourceId = currIdx.ToString(), SourceType = "Weeco-4M GPIO", Path = ModuleParameters.MODPAR_STATUS_LEVEL, Value = regsStorage[REGSTORAGE_IDX_STATUS,currIdx].ToString() });
						regsStorage[REGSTORAGE_IDX_LAST_COUNTER,currIdx] = regsStorage[REGSTORAGE_IDX_COUNTER,currIdx];
                    }
                }
                else
                {
                    // analyse energy meter counters and periode
					if (regsStorage[REGSTORAGE_IDX_COUNTER,currIdx] != regsStorage[REGSTORAGE_IDX_LAST_COUNTER,currIdx])
                    {
						if (sendCounters)
							InterfacePropertyChangedAction(new InterfacePropertyChangedAction() { Domain = this.Domain, SourceId = (currIdx+16).ToString(), SourceType = "Weeco-4M Register", Path = ModuleParameters.MODPAR_STATUS_LEVEL, Value = regsStorage[REGSTORAGE_IDX_COUNTER,currIdx].ToString() });
						if ((regsStorage[REGSTORAGE_IDX_PERIODE,currIdx] * Pulse4Watt) != 0)
							currWatt = (3600 * 1000) / (regsStorage[REGSTORAGE_IDX_PERIODE,currIdx] * Pulse4Watt);
                        else
                            currWatt = 0.0;
						// check if measure changed enought to be sent
						diffMeas = Math.Abs((int)regsStorage[REGSTORAGE_IDX_LAST_PERIODE,currIdx] - (int)regsStorage[REGSTORAGE_IDX_PERIODE,currIdx]);
						deltaMeasPerc =  ((double)diffMeas / (double)regsStorage[REGSTORAGE_IDX_LAST_PERIODE,currIdx]) * 100;
						if (deltaMeasPerc > 7.5)
						{
							InterfacePropertyChangedAction(new InterfacePropertyChangedAction() { Domain = this.Domain, SourceId = (currIdx+32).ToString(), SourceType = "Weeco-4M Register", Path = ModuleParameters.MODPAR_STATUS_LEVEL, Value = currWatt.ToString() });
			            	InterfacePropertyChangedAction(new InterfacePropertyChangedAction() { Domain = this.Domain, SourceId = (currIdx+32).ToString(), SourceType = "Weeco-4M Register", Path = ModuleParameters.MODPAR_METER_WATTS, Value = currWatt });
							regsStorage[REGSTORAGE_IDX_LAST_PERIODE,currIdx] = regsStorage[REGSTORAGE_IDX_PERIODE,currIdx];
						}
						regsStorage[REGSTORAGE_IDX_LAST_COUNTER,currIdx] = regsStorage[REGSTORAGE_IDX_COUNTER,currIdx];
						regsStorage[REGSTORAGE_IDX_LAST_PERIODE_TIME,currIdx]=0;
						if (DEBUG_MEAS) 
						{
							Console.WriteLine("Weeco4mGPIO reg:" + currIdx + " watt:" + currWatt + " diff:" + diffMeas + " diff%:" + deltaMeasPerc);
						}
					}
					else
					{						
						// estimate the power meter value when no pulse is coming
						regsStorage[REGSTORAGE_IDX_LAST_PERIODE_TIME,currIdx]+=ThreadPeriodeMs;
						if ((regsStorage[REGSTORAGE_IDX_LAST_PERIODE_TIME,currIdx] %  (ThreadPeriodeMs*5)) == 0 &&
						    regsStorage[REGSTORAGE_IDX_LAST_PERIODE_TIME,currIdx] > regsStorage[REGSTORAGE_IDX_PERIODE,currIdx] && 
						    regsStorage[REGSTORAGE_IDX_LAST_PERIODE,currIdx] > 0)
						{
							// ok, I need to estimate the new value because no new pulse arrived .. may be the power consumption will be decreased from 2KW to 100W ??
							currWatt = (3600 * 1000) / (regsStorage[REGSTORAGE_IDX_LAST_PERIODE_TIME,currIdx] * Pulse4Watt);

							InterfacePropertyChangedAction(new InterfacePropertyChangedAction() { Domain = this.Domain, SourceId = (currIdx+32).ToString(), SourceType = "Weeco-4M Register", Path = ModuleParameters.MODPAR_STATUS_LEVEL, Value = currWatt.ToString() });
			            	InterfacePropertyChangedAction(new InterfacePropertyChangedAction() { Domain = this.Domain, SourceId = (currIdx+32).ToString(), SourceType = "Weeco-4M Register", Path = ModuleParameters.MODPAR_METER_WATTS, Value = currWatt });

							if (DEBUG_MEAS) 
							{
								Console.WriteLine("Weeco4mGPIO reg:" + currIdx + " est.watt:" + currWatt);
							}
						}	
                    }
                }
            }
        }

		public void ThreadSendData()
        {
			int cntCounters=PeriodeToSendCountersMs;
			while(IsConnected)
            {
				ReadAndSendValues(PeriodeToSendCountersMs <= 0);
				Thread.Sleep(ThreadPeriodeMs);
				if (cntCounters > 0)
					cntCounters-=ThreadPeriodeMs;
				else
					cntCounters=PeriodeToSendCountersMs;
            }
        }


    }
}

