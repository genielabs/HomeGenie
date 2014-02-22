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

			public static Dictionary<int, string> CommandsList = new Dictionary<int, string> ()
            {
                {203, "Parameter.Status"},
                {701, "Control.On"},
                {702, "Control.Off"},
                {703, "Control.Bright"},
                {704, "Control.Dim"},
                {705, "Control.Level"},
                {706, "Control.Toggle"},
                {721, "Control.AllLightsOn"},
                {722, "Control.AllLightsOff"}
            };

			// <context>.<command> enum   -   eg. Control.On where <context> :== "Control" and <command> :== "On"
			public static readonly Command PARAMETER_STATUS = new Command (203);
			public static readonly Command CONTROL_ON = new Command (701);
			public static readonly Command CONTROL_OFF = new Command (702);
			public static readonly Command CONTROL_BRIGHT = new Command (703);
			public static readonly Command CONTROL_DIM = new Command (704);
			public static readonly Command CONTROL_LEVEL = new Command (705);
            public static readonly Command CONTROL_TOGGLE = new Command(706);
            public static readonly Command CONTROL_ALLLIGHTSON = new Command(721);
			public static readonly Command CONTROL_ALLLIGHTSOFF = new Command (722);

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

			public override String ToString ()
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


        private XTenManager x10lib;
		private string _portname;

		public X10 ()
		{
			x10lib = new XTenManager ();
			x10lib.PropertyChanged += HandlePropertyChanged;
            x10lib.RfDataReceived += new Action<RfDataReceivedAction>(x10lib_RfDataReceived);
		}

        private Timer _rfpulsetimer;
        private string _rfprevstringdata = "";
        void x10lib_RfDataReceived(RfDataReceivedAction eventdata)
        {
            if (InterfacePropertyChangedAction != null)
            {
                string data = XTenLib.Utility.ByteArrayToString(eventdata.RawData);
                // flood protection =) - discard dupes
                if (_rfprevstringdata != data)
                {
                    _rfprevstringdata = data;
                    try
                    {
                        InterfacePropertyChangedAction(new InterfacePropertyChangedAction() { Domain = this.Domain, SourceId = "RF", SourceType = "X10 RF Receiver", Path = "Receiver.RawData", Value = _rfprevstringdata });
                    }
                    catch (Exception ex)
                    {
                        // TODO: add error logging 
                    }
                    //
                    if (_rfpulsetimer == null)
                    {
                        _rfpulsetimer = new Timer(delegate(object target)
                        {
                            try
                            {
                                _rfprevstringdata = "";
                                InterfacePropertyChangedAction(new InterfacePropertyChangedAction() { Domain = this.Domain, SourceId = "RF", SourceType = "X10 RF Receiver", Path = "Receiver.RawData", Value = "" });
                            }
                            catch (Exception ex)
                            {
                                // TODO: add error logging 
                            }
                        });
                    }
                    _rfpulsetimer.Change(1000, Timeout.Infinite);
                }
            }
        }

		void HandlePropertyChanged (object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Level") {
				if (InterfacePropertyChangedAction != null) {
					try {
						InterfacePropertyChangedAction (new InterfacePropertyChangedAction () { Domain = this.Domain, SourceId = (sender as X10Module).Code, SourceType = (sender as X10Module).Description, Path = ModuleParameters.MODPAR_STATUS_LEVEL, Value = (sender as X10Module).Level.ToString() });
					} catch {
					}
				}
			}
		}


        #region MIG Interface members

        public event Action<InterfacePropertyChangedAction> InterfacePropertyChangedAction;

        public string Domain
        {
			get {
				string ifacedomain = this.GetType ().Namespace.ToString ();
				ifacedomain = ifacedomain.Substring (ifacedomain.LastIndexOf (".") + 1) + "." + this.GetType ().Name.ToString ();
				return ifacedomain;
			}
		}

		public bool Connect ()
		{
			return x10lib.Connect ();
		}
		public void Disconnect ()
		{
			x10lib.Disconnect ();
		}
		public bool IsConnected {
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

        public void WaitOnPending()
        {
            x10lib.WaitComplete();
        }

        public object InterfaceControl(MIGInterfaceCommand request)
        {
            string nodeid = request.nodeid;
            Command command = (Command)request.command;
            string option = request.GetOption(0);

            //process command
            #region X10HAL-commands compatibility !!! <-- DEPRECATE THIS
            if (nodeid.ToUpper() == "STATUS")
            {
                List<X10Module> tempdataitems = new List<X10Module>(x10lib.ModulesStatus.Count);
                foreach (string key in x10lib.ModulesStatus.Keys)
                {
                    tempdataitems.Add(x10lib.ModulesStatus[key]);
                }

                XmlSerializer serializer = new XmlSerializer(typeof(List<X10Module>));
                StringWriter sw = new StringWriter();
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                serializer.Serialize(sw, tempdataitems, ns);

                StringBuilder sb = new StringBuilder();
                sb.Append(sw.ToString());
                //byte[] b = Encoding.UTF8.GetBytes(sb.ToString());
                //response.ContentLength64 = b.Length;
                //response.OutputStream.Write(b, 0, b.Length);
                return sb.ToString();
            }
            else if (nodeid.ToUpper() == "CONFIG")
            {
                string configpath = @"C:\Program Files\ActiveHome Pro\HAL.ahx";
                string config = "";
                //
                try
                {
                    config = System.IO.File.ReadAllText(configpath);
                    //
                    if (config.IndexOf("&amp;") <= 0)
                    {
                        config = config.Replace("&", "&amp;");
                    }
                    //
                    StringBuilder sb = new StringBuilder();
                    sb.Append(config);
                    return sb.ToString();
                }
                catch (Exception e)
                {
                }
            }
            #endregion
            X10HouseCodes housecode = XTenLib.Utility.HouseCodeFromString(nodeid);
            X10UnitCodes unitcode = XTenLib.Utility.UnitCodeFromString(nodeid);
            if (command == Command.PARAMETER_STATUS)
            {
                x10lib.StatusRequest(housecode, unitcode);
            }
            else if (command == Command.CONTROL_ON)
            {
                x10lib.LightOn(housecode, unitcode);
            }
            else if (command == Command.CONTROL_OFF)
            {
                x10lib.LightOff(housecode, unitcode);
            }
            else if (command == Command.CONTROL_BRIGHT)
            {
                x10lib.Bright(housecode, unitcode, int.Parse(option));
            }
            else if (command == Command.CONTROL_DIM)
            {
                x10lib.Dim(housecode, unitcode, int.Parse(option));
            }
            else if (command == Command.CONTROL_LEVEL)
            {
                int dimvalue = int.Parse(option) - (int)(x10lib.ModulesStatus[nodeid].Level * 100.0);
                if (dimvalue > 0)
                {
                    x10lib.Bright(housecode, unitcode, dimvalue);
                }
                else if (dimvalue < 0)
                {
                    x10lib.Dim(housecode, unitcode, -dimvalue);
                }
            }
            else if (command == Command.CONTROL_TOGGLE)
            {
                string huc = XTenLib.Utility.HouseUnitCodeFromEnum(housecode, unitcode);
                if (x10lib.ModulesStatus[huc].Level == 0)
                {
                    x10lib.LightOn(housecode, unitcode);
                }
                else
                {
                    x10lib.LightOff(housecode, unitcode);
                }
            }
            else if (command == Command.CONTROL_ALLLIGHTSON)
            {
                x10lib.AllLightsOn(housecode);
            }
            else if (command == Command.CONTROL_ALLLIGHTSOFF)
            {
                x10lib.AllUnitsOff(housecode);
            }//
            return "";
        }

        #endregion


		public XTenManager X10Controller {
			get { return x10lib; }
		}


		public string GetPortName ()
		{
			return _portname;
		}
		
		public void SetPortName (string portname)
		{
            if (x10lib != null)
            {
                x10lib.PortName = portname;
            }
            _portname = portname;
        }


		public string GetHouseCodes ()
		{
			return x10lib.HouseCode;
		}

		public void SetHouseCodes (string hcodes)
		{
			x10lib.HouseCode = hcodes;
		}



	}
}
