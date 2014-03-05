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
        private W800RF32.Transceiver w800Rf32;
        private string portName;

        public W800RF()
        {
            w800Rf32 = new Transceiver();
            w800Rf32.RfDataReceived += HandleRfDataReceived;
        }

        private Timer rfPulseTimer;
        private string rfLastStringData = "";
        void HandleRfDataReceived(RfDataReceivedAction eventdata)
        {
            string data = XTenLib.Utility.ByteArrayToString(eventdata.RawData);
            if (InterfacePropertyChangedAction != null)
            {
                // flood protection =) - discard dupes
                if (rfLastStringData != data)
                {
                    rfLastStringData = data;
                    try
                    {
                        InterfacePropertyChangedAction(new InterfacePropertyChangedAction() { Domain = this.Domain, SourceId = "RF", SourceType = "W800RF32 RF Receiver", Path = "Receiver.RawData", Value = rfLastStringData });
                    }
                    catch (Exception ex)
                    {
                        // TODO: add error logging 
                    }
                    //
                    if (rfPulseTimer == null)
                    {
                        rfPulseTimer = new Timer(delegate(object target)
                                                  {
                                                      try
                                                      {
                                                          rfLastStringData = "";
                                                          InterfacePropertyChangedAction(new InterfacePropertyChangedAction() { Domain = this.Domain, SourceId = "RF", SourceType = "W800RF32 RF Receiver", Path = "Receiver.RawData", Value = "" });
                                                      }
                                                      catch (Exception ex)
                                                      {
                                                          // TODO: add error logging 
                                                      }
                                                  });
                    }
                    rfPulseTimer.Change(1000, Timeout.Infinite);
                }
            }
        }



        #region MIG Interface members

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

        public bool Connect()
        {
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
            return portName;
        }

        public void SetPortName(string portname)
        {
            if (w800Rf32 != null)
            {
                portName = portname;
                w800Rf32.PortName = portName;
            }
        }


    }
}

