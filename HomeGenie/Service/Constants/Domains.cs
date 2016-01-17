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
        //
        public const string MigService_Interfaces
            = "MIGService.Interfaces";
        //
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
        //
        public const string Controllers_LircRemote
            = "Controllers.LircRemote";
        //
        public const string EmbeddedSystems_Weeco4mGPIO
            = "EmbeddedSystems.Weeco4mGPIO";
        //
        public const string Media_CameraInput
            = "Media.CameraInput";
        //
        public const string Protocols_UPnP
            = "Protocols.UPnP";
    }
}
