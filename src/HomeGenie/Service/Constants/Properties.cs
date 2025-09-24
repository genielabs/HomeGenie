/*
   Copyright 2012-2025 G-Labs (https://github.com/genielabs)

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
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
