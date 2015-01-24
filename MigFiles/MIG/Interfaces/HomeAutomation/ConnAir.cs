using System;

using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading;

using XTenLib;

using MIG.Interfaces.HomeAutomation.Commons;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace MIG.Interfaces.HomeAutomation
{
    /// <summary>
    /// ConnAir Simple Solutions driver.
    /// HomeAutomation interface driver class must implement <see cref="MIG.MIGInterface"/> interface members
    /// </summary>/
    public class ConnAir : MIGInterface   // <------
    {
        /// <summary>
        /// Own derived class for ConnAir modules similar X10modules
        /// </summary>
        public class ConnAirDevice
        {
            #region "Fields"

            public byte GenericClass { get; internal set; }
            public string IP;
            public string HouseCode;
            public string PortName;
            public int Port;
            #endregion
        }

        /// <summary>
        /// Own derived class for InterTechno/Brennerstuhl modules similar X10modules
        /// </summary>
        public class InterTechModule : XTenLib.X10Module
        {
            public _moduletype type;
            public ModuleTypes devicetype;
            public string path;
            public string value
            {
                get { return value; }
                set
                {
                    _moduletype moduleTypeValue = new _moduletype();
                    try
                    {
                        moduleTypeValue = (_moduletype)Enum.Parse(typeof(_moduletype), value);
                        if (Enum.IsDefined(typeof(_moduletype), moduleTypeValue) | moduleTypeValue.ToString().Contains(","))
                            Console.WriteLine("Converted '{0}' to {1}.", value, moduleTypeValue.ToString());
                        else
                            Debug.WriteLine("{0} is not an underlying value of the moduletype enumeration.", value);
                    }
                    catch (ArgumentException)
                    {
                        Debug.WriteLine("'{0}' is not a member of the moduletype enumeration.", value);
                    }
                    type = moduleTypeValue;
                    OnPropertyChanged("DeviceType");
                }
            }

            public string deviceType
            {
                get { return type.ToString(); }
                set
                {
                    ModuleTypes DevTypeValue = new ModuleTypes();
                    try
                    {
                        DevTypeValue = (ModuleTypes)Enum.Parse(typeof(ModuleTypes), value);
                        if (Enum.IsDefined(typeof(ModuleTypes), DevTypeValue) | DevTypeValue.ToString().Contains(","))
                            Console.WriteLine("Converted '{0}' to {1}.", value, DevTypeValue.ToString());
                        else
                            Debug.WriteLine("{0} is not an underlying value of the moduletype enumeration.", value);
                    }
                    catch (ArgumentException)
                    {
                        Debug.WriteLine("'{0}' is not a member of the moduletype enumeration.", value);
                    }
                    devicetype = DevTypeValue;
                    OnPropertyChanged("DeviceType");
                }
            }
        }

        /// <summary>
        // used if your interface driver requires port selection
        /// </summary>
        private Socket _UDP;
        private IPEndPoint _hostendp;

        private bool gotReadWriteError = true;
        private bool keepconnectionalive = false;
        private Thread _connectionwatcher;

        private Thread _receiverthread;
        private Thread _senderthread;

        private string _monitoredhousecode = "";
        public event PropertyChangedEventHandler PropertyChanged;

        private Dictionary<string, InterTechModule> _modstatus = new Dictionary<string, InterTechModule>();
        private bool _connected = false;

        private ConnAirDevice _connAir;

        /// <summary>
        /// Initializes a new instance of the <see cref="MIG.Interfaces.HomeAutomation.ExampleDriver"/> class.
        /// </summary>
        public ConnAir()
        {
            //instance of ConnAir TCP/IP Adapter device
            _connAir = new ConnAirDevice();
            _connAir.GenericClass = 0x010;

            if (_UDP != null)
            {
                _UDP = null;
            }

            _UDP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
            ProtocolType.Udp);            

            OperatingSystem os = Environment.OSVersion;

            PlatformID pid = os.Platform;
            //
            switch (pid)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                case PlatformID.Unix:
                    break;
                case PlatformID.MacOSX:
                    break;
                default:
                    break;
            }
        }

        #region MIG Interface members

        List<MIGServiceConfiguration.Interface.Option> options;
        List<InterfaceModule> modules;

        public event Action<InterfaceModulesChangedAction> InterfaceModulesChangedAction;

        private void _UDP_ChannelConnected(object sender, EventArgs e)
        {
            // Handle the event
            _connected = true;
        }
        private void _UDP_ChannelDisconnected(object sender, EventArgs e)
        {
            // Handle the event
            _connected = false;
        }


        /// <summary>
        /// This event must be arisen whenever an interface property changed.
        /// (eg. light A57 changed its dimmer level to 75%)
        /// 
        /// This example code would be placed, for instance, inside the event handler
        /// callback of the interface device driver.
        /// 
        /// if (InterfacePropertyChangedAction != null)
        ///	{
        /// 	var nodeid = mydevice_event.SourceNodeId; 		// "A57"
        /// 	var type = mydevice_event.SourceDescription;	// "My dimmer type"
        /// 	var event = mydevice_event.PropertyName;		// "Level"
        /// 	var value = mydevice_event.PropertyValue;		// "75"
        /// 
        /// 	if (event == "Level") // we want to signal this type of event
        /// 	{
        /// 		try
        ///			{
        ///				InterfacePropertyChangedAction(new InterfacePropertyChangedAction() 
        /// 			{ 
        /// 				Domain = this.Domain,
        /// 				SourceId = nodeid,
        /// 				SourceType = type,
        ///	 				Path = Parameters.MODPAR_STATUS_LEVEL,
        /// 				Value = value 
        /// 			});
        /// 		} catch {  }
        /// 	}
        /// }
        /// </summary>
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
                string ifacedomain = this.GetType().Namespace.ToString();
                ifacedomain = ifacedomain.Substring(ifacedomain.LastIndexOf(".") + 1) + "." + this.GetType().Name.ToString();
                return ifacedomain;
            }
        }
        public List<MIGServiceConfiguration.Interface.Option> Options
        {
            get
            {
                return options;
            }
            set
            {
                options = value;
                _connAir.Port = Convert.ToInt32(this.GetOption("Port").Value.Replace("|", "/"));
                _connAir.PortName = this.GetOption("PortName").Value.Replace("|", "/");
                _connAir.IP = this.GetOption("IP").Value.Replace("|", "/");
                _connAir.HouseCode = this.GetOption("HouseCodes").Value;
            }
        }

        /// <summary>
        /// get modules and module properties
        /// </summary>
        public List<InterfaceModule> Modules
        {
            get
            {
                return modules;
            }
            set
            {
                modules = value;
            }
        }

        public List<InterfaceModule> GetModules()
        {
            //this interface is only used to adjusted the configuration
            //like change settings by interface communication like receive new module etc.
            //List<InterfaceModule> modules = new List<InterfaceModule>();
            if (_connAir != null)
            {
                //don't know exactly to set this value
                foreach (var _mod in modules)
                {
                    InterTechModule im = new InterTechModule();
                    //addd eventhandler for popertychange
                    im.PropertyChanged += HandlePropertyChanged;
                    im.Code = _mod.Address;
                    im.Description = _mod.Description;
                    im.deviceType = _mod.ModuleType.ToString();
                    var modparameter = _mod.CustomData;
                    foreach (var param in modparameter)
                    {
                        if (param.Name == @"ConnAir.DeviceType")
                        {
                            // in case of passive device, device type is needed
                            // no active response from device implemented
                            im.value = param.Value;                            
                        }
                    }
                    //only add valid devices to list
                    if (im.type != _moduletype.NONE)
                    {
                        _modstatus.Add(_mod.Address, im);
                    }                    
                }
            }
            return modules;
        }
        /// <summary>
        /// Connect to the automation interface/controller device.
        /// </summary>
        public bool Connect()
        {
            try
            {
                IPAddress broadcast = IPAddress.Parse(_connAir.IP);
                try
                {
                    _hostendp = new IPEndPoint(broadcast, _connAir.Port);
                }
                catch (Exception ex)
                {
                    if (ex.Message == "Normalerweise darf jede Socketadresse (Protokoll, Netzwerkadresse oder Anschluss) nur jeweils einmal verwendet werden")
                    {
                        //MessageBox.Show(text: "Fehler beim Verbinden an Port " + _port + "!\nMöglicherweise wird der Port bereits von einem anderen Programm benutzt.", icon: MessageBoxIcon.Error, buttons: MessageBoxButtons.OK, caption: "Fehler beim Verbinden!");
                    }
                }
                _UDP.Connect(_connAir.IP, _connAir.Port);
            }
            catch (Exception ex)
            {
                ex.ToString();
                // would be nice to have an error handling here Sk 20131219
            }
            if (_UDP.Connected)
            {
                if (InterfaceModulesChangedAction != null) InterfaceModulesChangedAction(new InterfaceModulesChangedAction() { Domain = this.Domain });
                return true;
            }
            else
            {
                return false;
            }

        }

        //private bool _open()
        //{
        //    bool success = false;
        //    try
        //    {
        //        bool tryopen = (_UDP == null);
        //        if (Environment.OSVersion.Platform.ToString().StartsWith("Win") == false)
        //        {
        //            tryopen = (tryopen && System.IO.File.Exists(_portname));
        //        }
        //        if (tryopen)
        //        {
        //            _UDP = new Socket;
        //            _serialport.PortName = _portname;
        //            _serialport.BaudRate = _baudrate;
        //            //
        //            _serialport.ErrorReceived += HanldeErrorReceived;
        //        }
        //        if (_serialport.IsOpen == false)
        //        {
        //            _serialport.Open();
        //        }
        //        success = true;
        //    }
        //    catch (Exception ex)
        //    {
        //    }
        //    //
        //    if (ConnectedStateChanged != null)
        //    {
        //        ConnectedStateChanged(this, new ConnectedStateChangedEventArgs(success));
        //    }
        //    _isconnected = success;
        //    //
        //    if (success && _receiverthread == null)
        //    {
        //        _receiverthread = new Thread(_receiverloop);
        //        _receiverthread.Priority = ThreadPriority.Highest;
        //        _receiverthread.Start();
        //        //
        //        _senderthread = new Thread(_senderLoop);
        //        _senderthread.Priority = ThreadPriority.Highest;
        //        _senderthread.Start();
        //    }
        //    return success;
        //}

        //public bool Connect()
        //{
        //    bool success = _open();
        //    //
        //    //
        //    // we use reader loop for Linux/Mono compatibility
        //    //
        //    if (_connectionwatcher != null)
        //    {
        //        try
        //        {
        //            keepconnectionalive = false;
        //            _connectionwatcher.Abort();
        //        }
        //        catch { }
        //    }
        //    //
        //    keepconnectionalive = true;
        //    //
        //    _connectionwatcher = new Thread(new ThreadStart(delegate()
        //    {
        //        gotReadWriteError = !success;
        //        //
        //        while (keepconnectionalive)
        //        {
        //            if (gotReadWriteError)
        //            {
        //                try
        //                {
        //                    _close();
        //                    //
        //                }
        //                catch (Exception unex)
        //                {
        //                    //							Console.WriteLine(unex.Message + "\n" + unex.StackTrace);
        //                }
        //                Thread.Sleep(5000);
        //                if (keepconnectionalive)
        //                {
        //                    try
        //                    {
        //                        gotReadWriteError = !_open();
        //                    }
        //                    catch (Exception unex)
        //                    {
        //                        //                                Console.WriteLine(unex.Message + "\n" + unex.StackTrace);
        //                    }
        //                }
        //            }
        //            //
        //            Thread.Sleep(1000);
        //        }
        //    }));
        //    _connectionwatcher.Start();
        //    //
        //    return success;
        //}

        //public void Disconnect()
        //{
        //    keepconnectionalive = false;
        //    //
        //    try { _senderthread.Abort(); }
        //    catch { }
        //    _senderthread = null;
        //    try { _receiverthread.Abort(); }
        //    catch { }
        //    _receiverthread = null;
        //    //
        //    _UDP.Close();
        //}
        /// <summary>
        /// Disconnect the automation interface/controller device.
        /// </summary>
        public void Disconnect()
        {
            _UDP.Close();
        }
        /// <summary>
        /// Gets a value indicating whether the interface/controller device is connected or not.
        /// </summary>
        /// <value>
        /// <c>true</c> if it is connected; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnected
        {
            get { return _UDP.Connected; }
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


        /* I deprecate this
        /// <summary>
        /// Handles the webservice gateway client requests.
        /// This function is automatically invoked by
        /// the MIG WebService Gateway.
        /// Don't modify it. It is just an entry point. 
        /// You'll be modifying the other function called "InterfaceControl"
        /// instead, described later.
        /// </summary>
        /// <returns>
        /// optional string reply to the client (usually xml encoded).
        /// </returns>
        /// <param name='request'>
        /// Request.
        /// </param>
        public string HandleGatewayClientRequest (string request)
        {
            string returnvalue = InterfaceControl (request);
            return returnvalue;
        }*/

        /// <summary>
        /// This method is used by ProgramEngine to synchronize with
        /// asyncronously executed commands.
        /// You can ignore this if commands to interface device are already executed synchronously
        /// </summary>
        public void WaitOnPending()
        {
            // Pause the thread until all issued interface commands are effectively completed. 
        }

        /// <summary>
        ///
        /// A MIG interface class automatically receives a call to this function
        /// for each request made to the webservice and that is addressed to its
        /// driver namespace (domain). A driver request has the form:
        /// http://<server_addr>/<notimplauthkey>/<interface_domain>/<nodeid>/<command>[<other_slash_separated_parameters>]
        /// eg. http://192.168.1.8/api/HomeAutomation.ExampleDriver/G73/Control.On
        /// (tells ExampleDriver to turn on the module with address G73)
        ///
        /// the parameter "request" of this function will contain only the relevant
        /// part of the whole http request:
        /// <nodeid>/<command>[<other_slash_separated_parameters>]
        /// eg.: "G73/Control.Level/50"
        ///
        /// </summary>
        /// <returns>
        /// optional string reply to request (usually xml encoded reply)
        /// </returns>
        /// <param name='request'>
        /// a formatted string containging a valid request for this domain
        /// eg: "A8/Control.Level/75"
        /// </param>
        /// 
        public object InterfaceControl(MIGInterfaceCommand request)
        {
            string nodeid = request.NodeId;
            InterTechModule _mod = null;

            if (_modstatus.ContainsKey(nodeid))
            {
                _modstatus.TryGetValue(nodeid, out _mod);
            };

            string Master = nodeid.Substring(0, 1).ToUpper();
            string Slave = nodeid.Substring(1);
            string _msg;
            byte[] _bmsg;
            _header myhead = null;
            request.Response = ""; //default success value
            switch (_mod.type)
            {
                default:
                    //TODO: how to handle this
                    // visualize in Debugger
                    Debugger.Break();
                    return false;
                case _moduletype.CMR_1000:
                    myhead = CMR_1000;
                    break;
                case _moduletype.CMR_1224:
                    myhead = CMR_1224;
                    break;
                case _moduletype.CMR_300:
                    myhead = CMR_300;
                    break;
                case _moduletype.CMR_500:
                    myhead = CMR_500;
                    break;
                case _moduletype.IT_1500:
                    myhead = IT_1500;
                    break;
                case _moduletype.IT_2300:
                    myhead = IT_2300;
                    break;
                case _moduletype.ITL_150:
                    myhead = ITL_150;
                    break;
                case _moduletype.ITL_210:
                    myhead = ITL_210;
                    break;
                case _moduletype.ITL_300:
                    myhead = ITL_300;
                    break;
                case _moduletype.ITL_500:
                    myhead = ITL_500;
                    break;
                case _moduletype.ITL_1000:
                    myhead = ITL_1000;
                    break;
                case _moduletype.ITL_3000:
                    myhead = ITL_3000;
                    break;
                case _moduletype.ITLR_3500:
                    myhead = ITLR_3500;
                    break;
                case _moduletype.ITLR_3500T:
                    myhead = ITLR_3500T;
                    break;
                case _moduletype.ITR_300:
                    myhead = ITR_300;
                    break;
                case _moduletype.ITR_3500:
                    myhead = ITR_3500;
                    break;
                case _moduletype.ITR_7000:
                    myhead = ITR_7000;
                    break;
                case _moduletype.LBUR_100:
                    myhead = LBUR_100;
                    break;
                case _moduletype.PA3_1000:
                    myhead = PA3_1000;
                    break;
            }
            // this is an example set of commands:Z
            // the desired action on the <nodeid> device
            switch (request.Command)
            {
                case "Control.On":
                    _msg = tx433_intertechno(myhead, Master, Slave, 1, 0);
                    _bmsg = System.Text.Encoding.ASCII.GetBytes(_msg);
                    _UDP.SendTo(_bmsg, _hostendp);
                    
                    if (_mod.Level == 0.0)
                    {
                        _mod.Level = 1.0;
                    }
                    break;
                case "Control.Off":
                    _msg = tx433_intertechno(myhead, Master, Slave, 0, 0);
                    _bmsg = System.Text.Encoding.ASCII.GetBytes(_msg);
                    _UDP.SendTo(_bmsg, _hostendp);
                    if (_mod.Level == 1.0)
                    {
                        _mod.Level = 0.0;
                    }
                    break;
                case "Control.Level":

                    break;
                case "Control.Bright":

                    break;
                case "Control.Dim":

                    break;
                case "Controll.AllLightsOn":

                    break;
                case "Control.AllUnitsOff":

                    break;
            }

            return request.Response;
        }

        #endregion



        /// <summary>
        /// Gets the name of the port.
        /// </summary>
        /// <returns>
        /// The port name.
        /// </returns>
        /// INFO: You can ignore/delete this method if the device doesn't require port selection.
        public string GetPortName()
        {
            return _connAir.PortName;
        }

        /// <summary>
        /// Sets the name of the port.
        /// </summary>
        /// <param name='portname'>
        /// Portname.
        /// </param>
        /// INFO: You can ignore/delete this method if the device doesn't require port selection.
        public void SetPortName(string portname)
        {
            _connAir.PortName = portname;
        }

        public int GetPort()
        {
            return _connAir.Port;
        }

        public void SetPort(int port)
        {
            _connAir.Port = port;
        }
        /// <summary>
        /// Gets the IP of the ConnAir device
        /// </summary>
        /// <returns></returns>
        public string GetIP() { return _connAir.IP; }
        /// <summary>
        /// Sets the IP of the ConnAir device
        /// </summary>
        /// <param name="IP"></param>
        public void SetIP(string IP) { _connAir.IP = IP; }
        /// <summary>
        /// Get valid 1st Adress of modules for ConnAir device
        /// </summary>
        /// <returns></returns>
        public string GetHouseCodes() { return HouseCode; }
        /// <summary>
        /// Set valid 1st Adress of modules for ConnAir device
        /// </summary>
        /// <param name="hcodes"></param>
        public void SetHouseCodes(string hcodes) { HouseCode = hcodes; }

        public string HouseCode
        {
            get { return _monitoredhousecode; }
            set
            {
                _monitoredhousecode = value;
                for (int i = 0; i < _modstatus.Keys.Count; i++)
                {
                    _modstatus[_modstatus.Keys.ElementAt(i)].PropertyChanged -= _modulePropertyChanged;
                }
                _modstatus.Clear();
                string[] hc = _monitoredhousecode.Split(',');
                for (int i = 0; i < hc.Length; i++)
                {
                    for (int x = 1; x <= 16; x++)
                    {
                        InterTechModule module = new InterTechModule() { Code = hc[i] + x.ToString(), /*Status = "OFF",*/ Level = 0.0 };
                        //
                        module.PropertyChanged += _modulePropertyChanged;
                        //
                        _modstatus.Add(hc[i] + x.ToString(), module);
                    }
                }
            }
        }
        /// <summary>
        /// public property to get ConnAir modules
        /// </summary>
        public Dictionary<string, InterTechModule> ModulesStatus
        {
            get { return _modstatus; }
        }

        //Name der Steckdose                           Kopf                                      Fuß
        //Brennenstuhl RCS 1000 N Comfort     TXP:0,0,10,5600,350,25   ,16;
        //Brennenstuhl RCS 1044 N Comfort     TXP:0,0,10,5600,350,25   ,16;
        //Brennenstuhl RC 3600                TXP:0,0,10,3825,85,25      ,45;
        //Intertechno CMR-1000                TXP:0,0,6,11125,89,25     ,125;
        //Intertechno CMR-1224                TXP:0,0,6,11125,89,25     ,125;
        //Intertechno CMR-300                 TXP:0,0,6,11125,89,25      ,125;
        //Intertechno CMR-500                 TXP:0,0,6,11125,89,25      ,125;
        //Intertechno GRR-300                 TXP:0,0,6,11125,89,25      ,125;
        //Intertechno GRR-3500                TXP:0,0,5,10976,98,66      ,112;
        //Intertechno IT-1500                 TXP:0,0,5,10976,98,66      ,112;
        //Intertechno IT-2300                 TXP:0,0,5,10976,98,66      ,112;
        //Intertechno ITDL-1000               TXP:0,0,6,11125,89,25      ,125;
        //Intertechno ITL-1000                TXP:0,0,6,11125,89,25      ,125;
        //Intertechno ITL-150                 TXP:0,0,5,10976,98,66      ,112;
        //Intertechno ITL-210                 TXP:0,0,5,10976,98,66      ,112;
        //Intertechno ITL-230                 TXP:0,0,5,10976,98,66      ,112;
        //Intertechno ITL-300                 TXP:0,0,5,10976,98,66      ,112;
        //Intertechno ITL-3500                TXP:0,0,5,10976,98,66      ,112;
        //Intertechno ITL-500                 TXP:0,0,5,10976,98,66      ,112;
        //Intertechno ITLR-3500               TXP:0,0,5,10976,98,66      ,112;
        //Intertechno ITLR-3500T              TXP:0,0,5,10976,98,66      ,112;
        //Intertechno ITR-300                 TXP:0,0,6,11125,89,25      ,125;
        //Intertechno ITR-3500                TXP:0,0,6,11125,89,25      ,125;
        //Intertechno ITR-7000                TXP:0,0,5,10976,98,66      ,112;
        //Intertechno LBUR-100                TXP:0,0,5,10976,98,66      ,112;
        //Intertechno PA3-1000                TXP:0,0,6,11125,89,25      ,125;
        //Elro AB440D 200W                    TXP:0,0,10,5600,350,25   ,16:
        //Elro AB440D 300W                    TXP:0,0,10,5600,350,25   ,16:
        //Elro AB440ID                        TXP:0,0,10,5600,350,25   ,16:
        //Elro AB440IS                        TXP:0,0,10,5600,350,25   ,16:
        //Elro AB440L                         TXP:0,0,10,5600,350,25   ,16:
        //Elro AB440WD                        TXP:0,0,10,5600,350,25   ,16:

        private class _header
        {
            public int _sA;
            public int _sG;
            public int _sRepeat;
            public int _sPause;
            public int _sTune;
            public int _sBaud;
            public int _sSpeed;

            public _header(_header me)
            {
                _sA = me._sA;
                _sG = me._sG;
                _sRepeat = me._sRepeat;
                _sPause = me._sPause;
                _sTune = me._sTune;
                _sBaud = me._sBaud;
                _sSpeed = me._sSpeed;
            }

            public _header(int sA, int sG, int sRepeat, int sPause, int sTune, int sBaud, int sSpeed)
            {
                _sA = sA;
                _sG = sG;
                _sRepeat = sRepeat;
                _sPause = sPause;
                _sTune = sTune;
                _sBaud = sBaud;
                _sSpeed = sSpeed;
            }
        }

        //public static class ModuleParameters : MIG.Interfaces.HomeAutomation.Commons.ModuleParameters
        //{

        //}        

        [DefaultValue(NONE)]
        public enum _moduletype : int
        {
            NONE = -1,
            CMR_1000 = 1,
            CMR_1224 = 2,
            CMR_300 = 3,
            CMR_500 = 4,
            GRR_300 = 5,
            GRR_3500 = 6,
            IT_1500 = 7,
            IT_2300 = 8,
            ITDL_1000 = 9,
            ITL_1000 = 10,
            ITL_150 = 11,
            ITL_210 = 12,
            ITL_230 = 13,
            ITL_300 = 14,
            ITL_3000 = 15,
            ITL_500 = 16,
            ITLR_3500 = 17,
            ITLR_3500T = 18,
            ITR_300 = 19,
            ITR_3500 = 20,
            ITR_7000 = 21,
            LBUR_100 = 22,
            PA3_1000 = 23,
        }

        private _header CMR_1000 = new _header(0, 0, 6, 11125, 89, 25, 125);
        private _header CMR_1224 = new _header(0, 0, 6, 11125, 89, 25, 125);
        private _header CMR_300 = new _header(0, 0, 6, 11125, 89, 25, 125);
        private _header CMR_500 = new _header(0, 0, 6, 11125, 89, 25, 125);
        private _header GRR_300 = new _header(0, 0, 6, 11125, 89, 25, 125);
        private _header GRR_3500 = new _header(0, 0, 5, 10976, 98, 66, 112);
        private _header IT_1500 = new _header(0, 0, 5, 10976, 98, 66, 112);
        private _header IT_2300 = new _header(0, 0, 5, 10976, 98, 66, 112);
        private _header ITDL_1000 = new _header(0, 0, 6, 11125, 89, 25, 125);
        private _header ITL_1000 = new _header(0, 0, 6, 11125, 89, 25, 125);
        private _header ITL_150 = new _header(0, 0, 5, 10976, 98, 66, 112);
        private _header ITL_210 = new _header(0, 0, 5, 10976, 98, 66, 112);
        private _header ITL_230 = new _header(0, 0, 5, 10976, 98, 66, 112);
        private _header ITL_300 = new _header(0, 0, 5, 10976, 98, 66, 112);
        private _header ITL_3000 = new _header(0, 0, 5, 10976, 98, 66, 112);
        private _header ITL_500 = new _header(0, 0, 5, 10976, 98, 66, 112);
        private _header ITLR_3500 = new _header(0, 0, 5, 10976, 98, 66, 112);
        private _header ITLR_3500T = new _header(0, 0, 5, 10976, 98, 66, 112);
        private _header ITR_300 = new _header(0, 0, 6, 11125, 89, 25, 125);
        private _header ITR_3500 = new _header(0, 0, 6, 11125, 89, 25, 125);
        private _header ITR_7000 = new _header(0, 0, 5, 10976, 98, 66, 112);
        private _header LBUR_100 = new _header(0, 0, 5, 10976, 98, 66, 112);
        private _header PA3_1000 = new _header(0, 0, 6, 11125, 89, 25, 125);

        private string tx433_intertechno(_header _in, string Master, string Slave, int onoff, int tx433version)
        {
            //int _sA = 0;
            //int _sG = 0;
            //int _sRepeat = 6;
            //int _sPause = 11125;
            //int _sTune = 89;
            //int _sBaud = 25;
            //int _sSpeed = 125;

            //int _uSleep = 800000;

            string _HEAD = "TXP:" + _in._sA + "," + _in._sG + "," + _in._sRepeat + "," + _in._sPause + "," + _in._sTune + "," + _in._sBaud + ",";
            string _TAIL = ",1" + "," + _in._sSpeed + ";";// +"\r\n";
            string _AN = "12,4,4,12,12,4";
            string _AUS = "12,4,4,12,4,12";

            string _bitLow = "4";
            string _bitHgh = "12";

            string _seqLow = _bitHgh + "," + _bitHgh + "," + _bitLow + "," + _bitLow + ",";
            string _seqHgh = _bitHgh + "," + _bitLow + "," + _bitHgh + "," + _bitLow + ",";

            string _msgM = "";

            switch (Master)
            {
                case "A":
                    _msgM = _seqHgh + _seqHgh + _seqHgh + _seqHgh;
                    break;
                case "B":
                    _msgM = _seqLow + _seqHgh + _seqHgh + _seqHgh;
                    break;
                case "C":
                    _msgM = _seqHgh + _seqLow + _seqHgh + _seqHgh;
                    break;
                case "D":
                    _msgM = _seqLow + _seqLow + _seqHgh + _seqHgh;
                    break;
                case "E":
                    _msgM = _seqHgh + _seqHgh + _seqLow + _seqHgh;
                    break;
                case "F":
                    _msgM = _seqLow + _seqHgh + _seqLow + _seqHgh;
                    break;
                case "G":
                    _msgM = _seqHgh + _seqLow + _seqLow + _seqHgh;
                    break;
                case "H":
                    _msgM = _seqLow + _seqLow + _seqLow + _seqHgh;
                    break;
                case "I":
                    _msgM = _seqHgh + _seqHgh + _seqHgh + _seqLow;
                    break;
                case "J":
                    _msgM = _seqLow + _seqHgh + _seqHgh + _seqLow;
                    break;
                case "K":
                    _msgM = _seqHgh + _seqLow + _seqHgh + _seqLow;
                    break;
                case "L":
                    _msgM = _seqLow + _seqLow + _seqHgh + _seqLow;
                    break;
                case "M":
                    _msgM = _seqHgh + _seqHgh + _seqLow + _seqLow;
                    break;
                case "N":
                    _msgM = _seqLow + _seqHgh + _seqLow + _seqLow;
                    break;
                case "O":
                    _msgM = _seqHgh + _seqLow + _seqLow + _seqLow;
                    break;
                case "P":
                    _msgM = _seqLow + _seqLow + _seqLow + _seqLow;
                    break;
            }

            string _msgS = "";
            switch (Slave)
            {
                case "1":
                    _msgS = _seqHgh + _seqHgh + _seqHgh + _seqHgh;
                    break;
                case "2":
                    _msgS = _seqLow + _seqHgh + _seqHgh + _seqHgh;
                    break;
                case "3":
                    _msgS = _seqHgh + _seqLow + _seqHgh + _seqHgh;
                    break;
                case "4":
                    _msgS = _seqLow + _seqLow + _seqHgh + _seqHgh;
                    break;
                case "5":
                    _msgS = _seqHgh + _seqHgh + _seqLow + _seqHgh;
                    break;
                case "6":
                    _msgS = _seqLow + _seqHgh + _seqLow + _seqHgh;
                    break;
                case "7":
                    _msgS = _seqHgh + _seqLow + _seqLow + _seqHgh;
                    break;
                case "8":
                    _msgS = _seqLow + _seqLow + _seqLow + _seqHgh;
                    break;
                case "9":
                    _msgS = _seqHgh + _seqHgh + _seqHgh + _seqLow;
                    break;
                case "10":
                    _msgS = _seqLow + _seqHgh + _seqHgh + _seqLow;
                    break;
                case "11":
                    _msgS = _seqHgh + _seqLow + _seqHgh + _seqLow;
                    break;
                case "12":
                    _msgS = _seqLow + _seqLow + _seqHgh + _seqLow;
                    break;
                case "13":
                    _msgS = _seqHgh + _seqHgh + _seqLow + _seqLow;
                    break;
                case "14":
                    _msgS = _seqLow + _seqHgh + _seqLow + _seqLow;
                    break;
                case "15":
                    _msgS = _seqHgh + _seqLow + _seqLow + _seqLow;
                    break;
                case "16":
                    _msgS = _seqLow + _seqLow + _seqLow + _seqLow;
                    break;
            }

            string _msg_ON = _HEAD + _bitLow + "," + _msgM + _msgS + _seqHgh + _seqLow + _bitHgh + "," + _AN + _TAIL;
            string _msg_OFF = _HEAD + _bitLow + "," + _msgM + _msgS + _seqHgh + _seqLow + _bitHgh + "," + _AUS + _TAIL;
            string _msg = "";

            if (onoff == 1) { _msg = _msg_ON; }
            else _msg = _msg_OFF;

            return _msg;
        }

        //        function tx433_brennstuhl($Master,$Slave,$onoff,$tx433version)
        //{   
        //   $sA=0;
        //   $sG=0;
        //   $sRepeat=10;
        //   $sPause=5600;
        //   $sTune=350;
        //   $sBaud=25;
        //   $sSpeed=16;

        //   $uSleep=800000;
        //   if ($tx433version==1) $txversion=3;
        //   else $txversion=1;

        //   $HEAD="TXP:$sA,$sG,$sRepeat,$sPause,$sTune,$sBaud,";
        //   $TAIL=",$txversion,1,$sSpeed,;";
        //   $AN="1,3,1,3,3";
        //   $AUS="3,1,1,3,1";

        //   $bitLow=1;
        //   $bitHgh=3;

        //   $seqLow=$bitHgh.",".$bitHgh.",".$bitLow.",".$bitLow.",";
        //   $seqHgh=$bitHgh.",".$bitLow.",".$bitHgh.",".$bitLow.",";

        //   $bits=$Master;
        //   $msg="";
        //   for($i=0;$i<strlen($bits);$i++) {   
        //      $bit=substr($bits,$i,1);
        //      if($bit=="0")
        //         $msg=$msg.$seqLow;
        //      else
        //         $msg=$msg.$seqHgh;
        //   }
        //   $msgM=$msg;
        //   $bits=$Slave;
        //   $msg="";
        //   for($i=0;$i<strlen($bits);$i++) {
        //      $bit=substr($bits,$i,1);
        //      if($bit=="0")
        //         $msg=$msg.$seqLow;
        //      else
        //         $msg=$msg.$seqHgh;
        //   }

        //   $msgS=$msg;
        //   $msg_ON=$HEAD.$bitLow.",".$msgM.$msgS.$bitHgh.",".$AN.$TAIL;
        //   $msg_OFF=$HEAD.$bitLow.",".$msgM.$msgS.$bitHgh.",".$AUS.$TAIL;

        //   if($onoff==1){ $msg=$msg_ON;}
        //   else $msg=$msg_OFF;
        //   connair_send($msg);
        //}   
        void HandlePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Level")
            {
                if (InterfacePropertyChangedAction != null)
                {
                    try
                    {
                        InterfacePropertyChangedAction(new InterfacePropertyChangedAction()
                        {
                            Domain = this.Domain,
                            SourceId = (sender as InterTechModule).Code,
                            SourceType = (sender as InterTechModule).Description,
                            Path = ModuleParameters.MODPAR_STATUS_LEVEL,
                            Value = (sender as InterTechModule).Level.ToString()
                        });
                    }
                    catch
                    {
                    }
                }
            }
            if (e.PropertyName == "ConnAir.DeviceType")
            {
                if (InterfacePropertyChangedAction != null)
                {
                    try
                    {
                        InterfacePropertyChangedAction(new InterfacePropertyChangedAction()
                        {
                            Domain = this.Domain,
                            SourceId = (sender as InterTechModule).deviceType,
                            SourceType = (sender as InterTechModule).Description,
                            Path = (sender as InterTechModule).path,
                            Value = (sender as InterTechModule).value
                        });
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void _modulePropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            // route event
            if (PropertyChanged != null)
            {
                PropertyChanged(sender, args);
            }
        }
    }
}

