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

            public static Dictionary<int, string> CommandsList = new Dictionary<int, string>() {
                { 101, "Remotes.Search" },
                { 102, "Remotes.Add" },
                { 103, "Remotes.Remove" },
                { 104, "Remotes.List" },
                { 711, "Control.IrSend" },
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

        #region Managed to Unmanaged Interop

        [DllImport("lirc_client")]
        private extern static int lirc_init(string prog, int verbose);

        [DllImport("lirc_client")]
        private extern static int LircDeinit();

        [DllImport("lirc_client")]
        private extern static int lirc_nextcode(out string code);

        [DllImport("lirc_client")]
        private extern static int lirc_readconfig(IntPtr file, out IntPtr config, IntPtr check);

        [DllImport("lirc_client")]
        private extern static int LircFreeConfig(IntPtr config);

        [DllImport("lirc_client")]
        private extern static int lirc_code2char(IntPtr config, string code, out string str);

        #endregion

        private string programName = "homegenie";
        private bool isConnected;
        private Thread lircListener;
        private IntPtr lircConfig;
        List<LircRemoteData> remotesData = null;
        List<LircRemoteData> remotesConfig = null;
        private Timer rfPulseTimer;

        public LircRemote()
        {
            remotesConfig = new List<LircRemoteData>();
            string configfile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lircconfig.xml");
            if (File.Exists(configfile))
            {
                var serializer = new XmlSerializer(typeof(List<LircRemoteData>));
                var reader = new StreamReader(configfile);
                remotesConfig = (List<LircRemoteData>)serializer.Deserialize(reader);
                reader.Close();
            }
            //
            string remotesdb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lircremotes.xml");
            if (File.Exists(remotesdb))
            {
                var serializer = new XmlSerializer(typeof(List<LircRemoteData>));
                var reader = new StreamReader(remotesdb);
                remotesData = (List<LircRemoteData>)serializer.Deserialize(reader);
                reader.Close();
            }

        }


        public List<LircRemoteData> SearchRemotes(string searchString)
        {
            var filtered = new List<LircRemoteData>();
            searchString = searchString.ToLower();
            foreach (var remote in remotesData)
            {
                if (remote.Manufacturer.ToLower().StartsWith(searchString) || remote.Model.ToLower().StartsWith(searchString))
                {
                    filtered.Add(remote);
                }
            }
            return filtered;
        }


        #region MIG Interface members

        public event Action<InterfaceModulesChangedAction> InterfaceModulesChangedAction;
        public event Action<InterfacePropertyChangedAction> InterfacePropertyChangedAction;

        /// <summary>
        /// Gets the domain.
        /// ** Do not modify this function. **
        /// </summary>
        /// <value>
        /// The domain.
        /// </value>
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
            InterfaceModule module = new InterfaceModule();
            module.Domain = this.Domain;
            module.Address = "IR";
            module.ModuleType = ModuleTypes.Sensor;
            modules.Add(module);
            return modules;
        }

        /// <summary>
        /// Gets a value indicating whether the interface/controller device is connected or not.
        /// </summary>
        /// <value>
        /// <c>true</c> if it is connected; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnected
        {
            get { return isConnected; }
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

        public bool Connect()
        {
            if (!isConnected)
            {
                try
                {
                    if (lirc_init(programName, 1) == -1)
                    {
                        return false;
                    }
                    if (lirc_readconfig(IntPtr.Zero, out lircConfig, IntPtr.Zero) != 0)
                    {
                        return false;
                    }
                    //
                    isConnected = true;
                    //
                    lircListener = new Thread(new ThreadStart(() =>
                    {
                        while (isConnected)
                        {
                            string code = null;
                            try
                            {
                                lirc_nextcode(out code);
                            }
                            catch
                            {
                            } // TODO: handle exception
                            //
                            if (code == null)
                            {
                                // TODO: reconnect??
                                isConnected = false;
                                break;
                            }
                            //
                            if (code != "" && InterfacePropertyChangedAction != null)
                            {
                                string[] codeparts = code.Split(' ');
                                try
                                {
                                    if (codeparts[ 1 ] == "00") // we signal only the first pulse
                                    {
                                        InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                                            Domain = this.Domain,
                                            SourceId = "IR",
                                            SourceType = "LIRC Remote",
                                            Path = "Receiver.RawData",
                                            Value = codeparts[ 3 ].TrimEnd(new char[] { '\n', '\r' }) + "/" + codeparts[ 2 ]
                                        });
                                        //
                                        if (rfPulseTimer == null)
                                        {
                                            rfPulseTimer = new Timer(delegate(object target)
                                            {
                                                try
                                                {
                                                    //_rfprevstringdata = "";
                                                    InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                                                        Domain = this.Domain,
                                                        SourceId = "IR",
                                                        SourceType = "LIRC Remote",
                                                        Path = "Receiver.RawData",
                                                        Value = ""
                                                    });
                                                }
                                                catch
                                                {
                                                    // TODO: add error logging 
                                                }
                                            });
                                        }
                                        rfPulseTimer.Change(1000, Timeout.Infinite);
                                    }
                                }
                                catch
                                {
                                } // TODO: handle exception
                            }
                            Thread.Sleep(100);
                        }
                    }));
                    lircListener.Start();
                }
                catch
                {
                    return false;
                }
            }
            if (InterfaceModulesChangedAction != null) InterfaceModulesChangedAction(new InterfaceModulesChangedAction(){ Domain = this.Domain });
            return true;
        }

        public void Disconnect()
        {
            if (isConnected)
            {
                try
                {
                    lircListener.Abort();
                }
                catch
                {
                }
                lircListener = null;
                //
                try
                {
                    LircFreeConfig(lircConfig);
                    LircDeinit();
                }
                catch
                {

                }
                //
                isConnected = false;
            }
        }

        public object InterfaceControl(MIGInterfaceCommand request)
        {
            request.Response = ""; //default success value
            //
            if (request.Command == Command.REMOTES_SEARCH)
            {
                request.Response = JsonConvert.SerializeObject(
                    SearchRemotes(request.GetOption(0)),
                    Formatting.Indented
                );
            }
            else if (request.Command == Command.REMOTES_ADD)
            {
                var remote = remotesData.Find(r => r.Manufacturer.ToLower() == request.GetOption(0).ToLower() && r.Model.ToLower() == request.GetOption(1).ToLower());
                if (remote != null && remotesConfig.Find(r => r.Model.ToLower() == remote.Model.ToLower() && r.Manufacturer.ToLower() == remote.Manufacturer.ToLower()) == null)
                {
                    var webClient = new WebClient();
                    string config = webClient.DownloadString("http://lirc.sourceforge.net/remotes/" + remote.Manufacturer + "/" + remote.Model);
                    remote.Configuration = GetBytes(config);
                    remotesConfig.Add(remote);
                    SaveConfig();
                }
            }
            else if (request.Command == Command.REMOTES_REMOVE)
            {
                var remote = remotesConfig.Find(r => r.Manufacturer.ToLower() == request.GetOption(0).ToLower() && r.Model.ToLower() == request.GetOption(1).ToLower());
                if (remote != null)
                {
                    remotesConfig.Remove(remote);
                    SaveConfig();
                }
            }
            else if (request.Command == Command.REMOTES_LIST)
            {
                request.Response = JsonConvert.SerializeObject(remotesConfig, Formatting.Indented);
            }
            else if (request.Command == Command.CONTROL_IRSEND)
            {
                string commands = "";
                int c = 0;
                while (request.GetOption(c) != "")
                {
                    var options = request.GetOption(c).Split('/');
                    foreach (string o in options)
                    {
                        commands += "\"" + o + "\" ";
                    }
                    c++;
                }
                ShellCommand("irsend", "SEND_ONCE " + commands);
            }
            //
            return request.Response;
        }

        #endregion



        public void Dispose()
        {
            Disconnect();
        }



        private void SaveConfig()
        {
            string fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lircconfig.xml");
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            var settings = new System.Xml.XmlWriterSettings();
            settings.Indent = true;
            var serializer = new System.Xml.Serialization.XmlSerializer(remotesConfig.GetType());
            var writer = System.Xml.XmlWriter.Create(fileName, settings);
            serializer.Serialize(writer, remotesConfig);
            writer.Close();
            //
            try
            {
                string lircConfiguration = "";
                foreach (var remote in remotesConfig)
                {
                    lircConfiguration += GetString(remote.Configuration) + "\n";
                }
                File.WriteAllText("/etc/lirc/lircd.conf", lircConfiguration);
                ShellCommand("/etc/init.d/lirc", " force-reload");
            }
            catch
            {
            }
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
            var processInfo = new System.Diagnostics.ProcessStartInfo(command, args);
            processInfo.RedirectStandardOutput = false;
            processInfo.UseShellExecute = false;
            processInfo.CreateNoWindow = true;
            var process = new System.Diagnostics.Process();
            process.StartInfo = processInfo;
            process.Start();
        }


    }


}

