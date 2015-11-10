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
using System.Xml.Serialization;

using MIG.Config;

namespace HomeGenie.Data
{
    [Serializable()]
    public class SystemConfiguration_1_0 
    {
        public HomeGenieConfiguration_1_0 HomeGenie { get; set; }

        public MigServiceConfiguration MIGService { get; set; }

        public SystemConfiguration_1_0()
        {
            HomeGenie = new HomeGenieConfiguration_1_0();
            MIGService = new MigServiceConfiguration();
            //
            HomeGenie.SystemName = "HAL";
            HomeGenie.Location = "";
            HomeGenie.ServiceHost = "+";
            HomeGenie.ServicePort = 80;
            HomeGenie.UserLogin = "admin";
            HomeGenie.UserPassword = "";
            HomeGenie.EnableLogFile = "false";
        }
    }

    [Serializable()]
    public class HomeGenieConfiguration_1_0
    {
        public string GUID { get; set; }
        public string SystemName { get; set; }
        public string Location { get; set; }

        public string ServiceHost { get; set; }
        public int ServicePort { get; set; }
        public string UserLogin { get; set; }
        public string UserPassword { get; set; }

        public List<ModuleParameter> Settings = new List<ModuleParameter>();
        public StatisticsConfiguration Statistics = new StatisticsConfiguration();

        public string EnableLogFile { get; set; }

        public HomeGenieConfiguration_1_0()
        {
            ServiceHost = "+";
            ServicePort = 80;
        }

        [Serializable()]
        public class StatisticsConfiguration
        {

            [XmlAttribute]
            public int MaxDatabaseSizeMBytes { get; set; }

            [XmlAttribute]
            public int StatisticsTimeResolutionSeconds { get; set; }

            [XmlAttribute]
            public int StatisticsUIRefreshSeconds { get; set; }

            public StatisticsConfiguration()
            {

                MaxDatabaseSizeMBytes = 5; // 5MB default.
                StatisticsTimeResolutionSeconds = 5 * 60; // 5 minute default.
                StatisticsUIRefreshSeconds = 2 * 60; // 2 minute default.
            }

        }
    }

}
