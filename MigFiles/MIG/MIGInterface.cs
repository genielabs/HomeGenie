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

namespace MIG
{
    public static class Extensions
    {
        public static MIGServiceConfiguration.Interface.Option GetOption(this MIGInterface iface, string option)
        {
            if (iface.Options != null)
            {
                return iface.Options.Find(o => o.Name == option);
            }
            return null;
        }
        public static void SetOption(this MIGInterface iface, string option, string value)
        {
            var opt = iface.GetOption(option);
            if (opt == null)
            {
                opt = new MIGServiceConfiguration.Interface.Option() { Name =  option };
                iface.Options.Add(opt);
            }
            // TODO: instead of calling Disconnect/Connect here, rather 
            // TODO: add an OnOptionUpdate method to MIGInterface,
            // TODO: so to let the class do its bussiness logic
            if (iface.IsEnabled)
            {
                try { iface.Disconnect(); } catch { }
            }
            opt.Value = value;
            if (iface.IsEnabled)
            {
                try { iface.Connect(); } catch { }
            }
        }
    }

    public interface MIGInterface
    {
        /// <summary>
        /// interface identifier domain (eg. HomeAutomation.ZWave, Controllers.Kinect)
        /// that should be usually automatically calculated from namespace
        /// </summary>
        string Domain { get; }

        bool IsEnabled { get; set; }

        List<InterfaceModule> GetModules();

        /// <summary>
        /// sets the interface options.
        /// </summary>
        /// <param name="options">Options.</param>
        List<MIGServiceConfiguration.Interface.Option> Options { get; set; }

        /// <summary>
        /// all input data coming from connected device
        /// is routed via InterfacePropertyChangedAction event
        /// </summary>
        event Action<InterfacePropertyChangedAction> InterfacePropertyChangedAction;

        event Action<InterfaceModulesChangedAction> InterfaceModulesChangedAction;

        /// <summary>
        /// entry point for sending commands (control/configuration)
        /// to the connected device. 
        /// </summary>
        /// <param name="command">MIG interface command</param>
        /// <returns></returns>
        object InterfaceControl(MIGInterfaceCommand command);

        /// <summary>
        /// this value can be actively polled to detect
        /// current interface connection state
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// connect to the device interface / perform all setup
        /// </summary>
        /// <returns>a boolean indicating if the connection was succesful</returns>
        bool Connect();

        /// <summary>
        /// disconnect the device interface / perform everything needed for shutdown/cleanup
        /// </summary>
        void Disconnect();

        /// <summary>
        /// this return true if the device has been found in the system (probing)
        /// </summary>
        /// <returns></returns>
        bool IsDevicePresent();

    }

    public class InterfaceModule
    {
        public string Domain { get; set; }
        public string Address { get; set; }
        public ModuleTypes ModuleType { get; set; }
        public string Description { get; set; }
        public dynamic CustomData { get; set; }
    }

    public class InterfaceModulesChangedAction
    {
        public string Domain;
    }

    public class InterfacePropertyChangedAction
    {
        public string Domain { get; set; }
        public string SourceId { get; set; }
        public string SourceType { get; set; }
        public string Path { get; set; }
        public object Value { get; set; }
    }

    public class InterfaceConnectedStateChangedAction
    {
        public bool Connected { get; internal set; }
    }

    public enum ModuleTypes
    {
        Generic = -1,
        Program,
        Switch,
        Light,
        Dimmer,
        Sensor,
        Temperature,
        Siren,
        Fan,
        Thermostat,
        Shutter,
        DoorWindow,
        DoorLock,
        MediaTransmitter,
        MediaReceiver
        //siren, alarm, motion sensor, door sensor, thermal sensor, etc.
    }
}

