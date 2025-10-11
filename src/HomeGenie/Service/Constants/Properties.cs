/*
   Copyright 2012-2025 G-Labs (https://github.com/genielabs)

   This program is free software: you can redistribute it and/or modify
   it under the terms of the GNU Affero General Public License as
   published by the Free Software Foundation, either version 3 of the
   License, or (at your option) any later version.

   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU Affero General Public License for more details.

   You should have received a copy of the GNU Affero General Public License
   along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: https://homegenie.it
 */

namespace HomeGenie.Service.Constants
{
    // Modules's properties are to be considered as metadata fields
    // these are untyped, thouhg the consumer know how to handle these
    public static class Properties
    {
        // internal HomeGenie parameters
        public const string HomeGenieStatus
            = "HomeGenie.Status";
        public const string SystemInfoBootProgress
            = "SystemInfo.BootProgress";
        public const string SystemInfoHttpAddress
            = "SystemInfo.HttpAddress";
        public const string SchedulerModuleUpdateStart
            = "Scheduler.OnModuleUpdateStart";
        public const string SchedulerModuleUpdateEnd
            = "Scheduler.OnModuleUpdateEnd";
        public const string SchedulerError
            = "Scheduler.Error";
        public const string SchedulerScriptStatus
            = "Scheduler.ScriptStatus";
        public const string SchedulerTriggeredEvent
            = "Scheduler.TriggeredEvent";
        public const string ProgramEvent
            = "Program.Event";
        public const string ProgramStatus
            = "Program.Status";
        public const string ProgramNotification
            = "Program.Notification";
        public const string RuntimeError
            = "Runtime.Error";
        public const string CompilerWarning
            = "Compiler.Warning";
        public const string InstallProgressMessage
            = "InstallProgress.Message";
        public const string InstallProgressUpdate
            = "InstallProgress.Update";
        public const string EventsDisable
            = "Events.Disable";

        // commonly used parameters
        public const string StatusLevel
            = "Status.Level";
        public const string StatusColorHsb
            = "Status.Color";
        public const string MeterAny
            = "Meter.";
        public const string MeterWatts
            = "Meter.Watts";
        public const string SensorTemperature
            = "Sensor.Temperature";
        public const string SensorLuminance
            = "Sensor.Luminance";
        public const string SensorHumidity
            = "Sensor.Humidity";
        public const string SensorAlarm
            = "Sensor.Alarm";
        public const string SensorTamper
            = "Sensor.Tamper";
        public const string SensorMotionDetect
            = "Sensor.MotionDetect";
        public const string VirtualMeterWatts
            = "VirtualMeter.Watts";
        public const string WidgetDisplayModule
            = "Widget.DisplayModule";
        public const string VirtualModuleParentId
            = "VirtualModule.ParentId";
    }
}
