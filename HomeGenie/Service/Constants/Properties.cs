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

namespace HomeGenie.Service.Constants
{
    // Modules's properties are to be considered as metadata fields
    // these are untyped, thouhg the consumer know how to handle these
    public static class Properties
    {
        // internal HomeGenie parameters
        public const string HOMEGENIE_STATUS
            = "HomeGenie.Status";
        public const string SYSTEMINFO_HTTPPORT
            = "SystemInfo.HttpPort";
        public const string SCHEDULER_ERROR
            = "Scheduler.Error";
        public const string PROGRAM_STATUS
            = "Program.Status";
        public const string RUNTIME_ERROR
            = "Runtime.Error";
        public const string PROGRAM_NOTIFICATION
            = "Program.Notification";
        public const string INSTALLPROGRESS_MESSAGE
            = "InstallProgress.Message";
        public const string INSTALLPROGRESS_UPDATE
            = "InstallProgress.Update";

        // commonly used parameters
        public const string STATUS_LEVEL
            = "Status.Level";
        public const string STATUS_DOORLOCK
            = "Status.DoorLock";
        public const string METER_WATTS
            = "Meter.Watts";
        public const string METER_ANY
            = "Meter.";
        public const string WIDGET_DISPLAYMODULE
            = "Widget.DisplayModule";
        public const string VIRTUALMODULE_PARENTID
            = "VirtualModule.ParentId";

        // z-wave specific parameters
        public const string ZWAVENODE_BASIC
            = "ZWaveNode.Basic";
        public const string ZWAVENODE_WAKEUPINTERVAL
            = "ZWaveNode.WakeUpInterval";
        public const string ZWAVENODE_BATTERY
            = "ZWaveNode.Battery";
        public const string ZWAVENODE_MULTIINSTANCE
            = "ZWaveNode.MultiInstance";
        public const string ZWAVENODE_ASSOCIATIONS
            = "ZWaveNode.Associations";
        public const string ZWAVENODE_CONFIGVARIABLES
            = "ZWaveNode.Variables";
        public const string ZWAVENODE_NODEINFO
            = "ZWaveNode.NodeInfo";
        public const string ZWAVENODE_MANUFACTURERSPECIFIC
            = "ZWaveNode.ManufacturerSpecific";
        public const string ZWAVENODE_DEVICEHANDLER
            = "ZWaveNode.DeviceHandler";
    }
}
