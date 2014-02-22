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

using System.Net;

using System.IO;
using System.Xml.Serialization;

using System.Threading;
using System.Runtime.InteropServices;

using Newtonsoft;
using Newtonsoft.Json;

namespace MIG.Interfaces.Controllers
{
    [Serializable]
    public class LircRemoteData
    {
        public string Manufacturer = "";
        public string Model = "";
        //[NonSerialized]
        public byte[] Configuration;
    }


    public class LircRemote : MIGInterface
    {

        #region Implemented MIG Commands
        // typesafe enum
        public sealed class Command : GatewayCommand
        {

            public static Dictionary<int, string> CommandsList = new Dictionary<int, string>()
            {
                {101, "Remotes.Search"},
                {102, "Remotes.Add"},
                {103, "Remotes.Remove"},
                {104, "Remotes.List"},
                {711, "Control.IrSend"},
            };

            // <context>.<command> enum   -   eg. Control.On where <context> :== "Control" and <command> :== "On"
            public static readonly Command REMOTES_SEARCH = new Command(101);
            public static readonly Command REMOTES_ADD = new Command(102);
            public static readonly Command REMOTES_REMOVE = new Command(103);
            public static readonly Command REMOTES_LIST = new Command(104);
            public static readonly Command CONTROL_IRSEND = new Command(711);

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

        #region Managed to Unmanaged Interop
        [DllImport("lirc_client")]
        private extern static int lirc_init(string prog, int verbose);

        [DllImport("lirc_client")]
        private extern static int lirc_deinit();

        [DllImport("lirc_client")]
        private extern static int lirc_nextcode(out string code);

        [DllImport("lirc_client")]
        private extern static int lirc_readconfig(IntPtr file, out IntPtr config, IntPtr check);

        [DllImport("lirc_client")]
        private extern static int lirc_freeconfig(IntPtr config);

        [DllImport("lirc_client")]
        private extern static int lirc_code2char(IntPtr config, string code, out string str);
        #endregion

        private string programname = "homegenie";
        private bool isconnected;
        private Thread lirclistener;
        private IntPtr lircconfig;
        List<LircRemoteData> _remotesdata = null;
        List<LircRemoteData> _remotesconfig = null;
        private Timer _rfpulsetimer;

        public LircRemote()
        {
            _remotesconfig = new List<LircRemoteData>();
            string configfile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lircconfig.xml");
            if (File.Exists(configfile))
            {
                XmlSerializer mserializer = new XmlSerializer(typeof(List<LircRemoteData>));
                StreamReader mreader = new StreamReader(configfile);
                _remotesconfig = (List<LircRemoteData>)mserializer.Deserialize(mreader);
                mreader.Close();
            }
            //
            string remotesdb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lircremotes.xml");
            if (File.Exists(remotesdb))
            {
                XmlSerializer mserializer = new XmlSerializer(typeof(List<LircRemoteData>));
                StreamReader mreader = new StreamReader(remotesdb);
                _remotesdata = (List<LircRemoteData>)mserializer.Deserialize(mreader);
                mreader.Close();
            }

        }


        public List<LircRemoteData> SearchRemotes(string searchstring)
        {
            List<LircRemoteData> filtered = new List<LircRemoteData>();
            searchstring = searchstring.ToLower();
            foreach (LircRemoteData lrd in _remotesdata)
            {
                if (lrd.Manufacturer.ToLower().StartsWith(searchstring) || lrd.Model.ToLower().StartsWith(searchstring))
                {
                    filtered.Add(lrd);
                }
            }
            return filtered;
        }


        #region MIG Interface members

        public event Action<InterfacePropertyChangedAction> InterfacePropertyChangedAction;

        /// <summary>
		/// Gets the domain.
		/// ** Do not modify this function. **
		/// </summary>
		/// <value>
		/// The domain.
		/// </value>
		public string Domain {
			get {
				string ifacedomain = this.GetType ().Namespace.ToString ();
				ifacedomain = ifacedomain.Substring (ifacedomain.LastIndexOf (".") + 1) + "." + this.GetType ().Name.ToString ();
				return ifacedomain;
			}
		}


		/// <summary>
		/// Gets a value indicating whether the interface/controller device is connected or not.
		/// </summary>
		/// <value>
		/// <c>true</c> if it is connected; otherwise, <c>false</c>.
		/// </value>
		public bool IsConnected {
            get { return isconnected; }
		}
        /// <summary>
        /// Returns true if the device has been found in the system
        /// </summary>
        /// <returns></returns>
        public bool IsDevicePresent()
        {
            // eg. check against libusb for device presence by vendorId and productId
            return true;
        }

		/// <summary>
		/// This method is used by ProgramEngine to synchronize with
		/// asyncronously executed commands.
		/// You can ignore this if commands to interface device are already executed synchronously
		/// </summary>
		public void WaitOnPending ()
		{
			// Pause the thread until all issued interface commands are effectively completed. 
		}

        public bool Connect()
        {
            if (!isconnected)
            {
                try
                {
                    if (lirc_init(programname, 1) == -1)
                    {
                        return false;
                    }
                    if (lirc_readconfig(IntPtr.Zero, out lircconfig, IntPtr.Zero) != 0)
                    {
                        return false;
                    }
                    //
                    isconnected = true;
                    //
                    lirclistener = new Thread(new ThreadStart(() =>
                    {
                        while (isconnected)
                        {
                            string code = null;
                            try
                            {
                                lirc_nextcode(out code);
                            }
                            catch { } // TODO: handle exception
                            //
                            if (code == null)
                            {
                                // TODO: reconnect??
                                isconnected = false;
                                break;
                            }
                            //
                            if (code != "" && InterfacePropertyChangedAction != null)
                            {
                                string[] codeparts = code.Split(' ');
                                try
                                {
                                    if (codeparts[1] == "00") // we signal only the first pulse
                                    {
                                        InterfacePropertyChangedAction(new InterfacePropertyChangedAction()
                                        {
                                            Domain = this.Domain,
                                            SourceId = "IR",
                                            SourceType = "LIRC Remote",
                                            Path = "Receiver.RawData",
                                            Value = codeparts[3].TrimEnd(new char[] { '\n', '\r' }) + "/" + codeparts[2]
                                        });
                                        //
                                        if (_rfpulsetimer == null)
                                        {
                                            _rfpulsetimer = new Timer(delegate(object target)
                                            {
                                                try
                                                {
                                                    //_rfprevstringdata = "";
                                                    InterfacePropertyChangedAction(new InterfacePropertyChangedAction()
                                                    {
                                                        Domain = this.Domain,
                                                        SourceId = "IR",
                                                        SourceType = "LIRC Remote",
                                                        Path = "Receiver.RawData",
                                                        Value = ""
                                                    });
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
                                catch { } // TODO: handle exception
                            }
                            Thread.Sleep(100);
                        }
                    }));
                    lirclistener.Start();
                }
                catch { }
            }
            return true;
        }

        public void Disconnect()
        {
            if (isconnected)
            {
                lirclistener.Abort();
                lirclistener = null;
                //
                try
                {
                    lirc_freeconfig(lircconfig);
                    lirc_deinit();
                }
                catch (System.Exception ex)
                {

                }
                //
                isconnected = false;
            }
        }

		public object InterfaceControl (MIGInterfaceCommand request)
		{
			request.response = ""; //default success value
            //
            if (request.command == Command.REMOTES_SEARCH)
            {
                request.response = JsonConvert.SerializeObject(SearchRemotes(request.GetOption(0)), Formatting.Indented);
            }
            else if (request.command == Command.REMOTES_ADD)
            {
                LircRemoteData lrd = _remotesdata.Find(r => r.Manufacturer.ToLower() == request.GetOption(0).ToLower() && r.Model.ToLower() == request.GetOption(1).ToLower());
                if (lrd != null && _remotesconfig.Find(r => r.Model.ToLower() == lrd.Model.ToLower() && r.Manufacturer.ToLower() == lrd.Manufacturer.ToLower()) == null)
                {
                    WebClient wb = new WebClient();
                    string config = wb.DownloadString("http://lirc.sourceforge.net/remotes/" + lrd.Manufacturer + "/" + lrd.Model);
                    lrd.Configuration = GetBytes(config);
                    _remotesconfig.Add(lrd);
                    _configsave();
                }
            }
            else if (request.command == Command.REMOTES_REMOVE)
            {
                LircRemoteData lrd1 = _remotesconfig.Find(r => r.Manufacturer.ToLower() == request.GetOption(0).ToLower() && r.Model.ToLower() == request.GetOption(1).ToLower());
                if (lrd1 != null)
                {
                    _remotesconfig.Remove(lrd1);
                    _configsave();
                }
            }
            else if (request.command == Command.REMOTES_LIST)
            {
                request.response = JsonConvert.SerializeObject(_remotesconfig, Formatting.Indented);
            }
            else if (request.command == Command.CONTROL_IRSEND)
            {
                string commands = "";
                int c = 0;
                while (request.GetOption(c) != "")
                {
                    commands += "\"" + request.GetOption(c) + "\" ";
                    c++;
                }
                ShellCommand("irsend", "SEND_ONCE " + commands);
            }
			//
			return request.response;
		}
		
        #endregion



        public void Dispose() 
        {
            Disconnect();
        }



        private void _configsave()
        {
            string fname = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lircconfig.xml");
            if (File.Exists(fname))
            {
                File.Delete(fname);
            }
            System.Xml.XmlWriterSettings ws = new System.Xml.XmlWriterSettings();
            ws.Indent = true;
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(_remotesconfig.GetType());
            System.Xml.XmlWriter wri = System.Xml.XmlWriter.Create(fname, ws);
            x.Serialize(wri, _remotesconfig);
            wri.Close();
            //
            try
            {
                string lircconfig = "";
                foreach (LircRemoteData r in _remotesconfig)
                {
                    lircconfig += GetString(r.Configuration) + "\n";
                }
                File.WriteAllText("/etc/lirc/lircd.conf", lircconfig);
                ShellCommand("/etc/init.d/lirc", " force-reload");
            }
            catch { }
        }



        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        static void ShellCommand(string command, string args)
        {
            System.Diagnostics.ProcessStartInfo psinfo = new System.Diagnostics.ProcessStartInfo(command, args);
            psinfo.RedirectStandardOutput = false;
            psinfo.UseShellExecute = false;
            psinfo.CreateNoWindow = true;
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo = psinfo;
            process.Start();
        }


    }


}

