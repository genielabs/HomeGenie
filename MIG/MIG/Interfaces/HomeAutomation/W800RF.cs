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

using W800RF32;

namespace MIG.Interfaces.HomeAutomation
{
    public class W800RF : MIGInterface
    {
        private W800RF32.Transreceiver _w800rf32;
        private string _portname;

        public W800RF()
        {
            _w800rf32 = new Transreceiver();
            _w800rf32.RfDataReceived += HandleRfDataReceived;
        }

        private Timer _rfpulsetimer;
        private string _rfprevstringdata = "";
        void HandleRfDataReceived(RfDataReceivedAction eventdata)
        {
            string data = XTenLib.Utility.ByteArrayToString(eventdata.RawData);
            if (InterfacePropertyChangedAction != null)
            {
                // flood protection =) - discard dupes
                if (_rfprevstringdata != data)
                {
                    _rfprevstringdata = data;
                    try
                    {
                        InterfacePropertyChangedAction(new InterfacePropertyChangedAction() { Domain = this.Domain, SourceId = "RF", SourceType = "W800RF32 RF Receiver", Path = "Receiver.RawData", Value = _rfprevstringdata });
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
                                                          InterfacePropertyChangedAction(new InterfacePropertyChangedAction() { Domain = this.Domain, SourceId = "RF", SourceType = "W800RF32 RF Receiver", Path = "Receiver.RawData", Value = "" });
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



        #region MIG Interface members

        public event Action<InterfacePropertyChangedAction> InterfacePropertyChangedAction;

        public string Domain
        {
            get
            {
                string ifacedomain = this.GetType().Namespace.ToString();
                ifacedomain = ifacedomain.Substring(ifacedomain.LastIndexOf(".") + 1) + "." + this.GetType().Name.ToString();
                return ifacedomain;
            }
        }

        public bool Connect()
        {
            return _w800rf32.Connect();
        }
        public void Disconnect()
        {
            _w800rf32.Disconnect();
        }
        public bool IsConnected
        {
            get { return _w800rf32.IsConnected; }
        }
        public bool IsDevicePresent()
        {
            bool present = true;
            return present;
        }

        public void WaitOnPending()
        {

        }

        public object InterfaceControl(MIGInterfaceCommand request)
        {
            return "";
        }

        #endregion


        public string GetPortName()
        {
            return _portname;
        }

        public void SetPortName(string portname)
        {
            if (_w800rf32 != null)
            {
                _portname = portname;
                _w800rf32.PortName = _portname;
            }
        }


    }
}

