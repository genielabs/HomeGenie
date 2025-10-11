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
