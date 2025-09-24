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
    public static class Domains
    {
        public const string HomeGenie_UpdateChecker
            = "HomeGenie.UpdateChecker";
        public const string HomeGenie_System
            = "HomeGenie.System";
        public const string HomeGenie_PackageInstaller
            = "HomeGenie.PackageInstaller";
        public const string HomeGenie_BackupRestore
            = "HomeGenie.BackupRestore";
        public const string HomeGenie_BootProgress
            = "HomeGenie.BootProgress";

        public const string MigService_Interfaces
            = "MIGService.Interfaces";

        public const string HomeAutomation_HomeGenie
            = "HomeAutomation.HomeGenie";
        public const string HomeAutomation_HomeGenie_Automation
            = HomeAutomation_HomeGenie + ".Automation";
        public const string HomeAutomation_ZWave
            = "HomeAutomation.ZWave";
        public const string HomeAutomation_X10
            = "HomeAutomation.X10";
        public const string HomeAutomation_Insteon
            = "HomeAutomation.Insteon";
        public const string HomeAutomation_W800RF
            = "HomeAutomation.W800RF";

        public const string Controllers_LircRemote
            = "Controllers.LircRemote";

        public const string EmbeddedSystems_Weeco4mGPIO
            = "EmbeddedSystems.Weeco4mGPIO";

        public const string Media_CameraInput
            = "Media.CameraInput";

        public const string Protocols_UPnP
            = "Protocols.UPnP";
    }
}
