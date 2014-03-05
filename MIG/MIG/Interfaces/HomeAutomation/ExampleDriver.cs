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

using MIG.Interfaces.HomeAutomation.Commons;

namespace MIG.Interfaces.HomeAutomation
{
    /// <summary>
    /// Example driver.
    /// HomeAutomation interface driver class must implement <see cref="MIG.MIGInterface"/> interface members
    /// </summary>/
    public class ExampleDriver : MIGInterface   // <------
    {

        /// <summary>
        // used if your interface driver requires port selection
        /// </summary>
        private string portName;

        /// <summary>
        /// Initializes a new instance of the <see cref="MIG.Interfaces.HomeAutomation.ExampleDriver"/> class.
        /// </summary>
        public ExampleDriver()
        {

        }

        #region MIG Interface members

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
                string domain = this.GetType().Namespace.ToString();
                domain = domain.Substring(domain.LastIndexOf(".") + 1) + "." + this.GetType().Name.ToString();
                return domain;
            }
        }



        /// <summary>
        /// Connect to the automation interface/controller device.
        /// </summary>
        public bool Connect()
        {
            return true;
        }
        /// <summary>
        /// Disconnect the automation interface/controller device.
        /// </summary>
        public void Disconnect()
        {

        }
        /// <summary>
        /// Gets a value indicating whether the interface/controller device is connected or not.
        /// </summary>
        /// <value>
        /// <c>true</c> if it is connected; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnected
        {
            get { return true; }
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
            request.Response = ""; //default success value
            // this is an example set of commands:
            // for each received command
            // add the needed code to perform 
            // the desired action on the <nodeid> device
            switch (request.Command)
            {
                case "Control.On":

                    break;
                case "Control.Off":

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
            //
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
            return portName;
        }

        /// <summary>
        /// Sets the name of the port.
        /// </summary>
        /// <param name='name'>
        /// Portname.
        /// </param>
        /// INFO: You can ignore/delete this method if the device doesn't require port selection.
        public void SetPortName(string name)
        {
            portName = name;
        }


    }
}

