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

using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using MIG;
using HomeGenie.Service;

namespace HomeGenie.Data
{
    [Serializable()]
    public class SystemConfiguration : ICloneable
    {
        public event Action<bool> OnUpdate;

        public HomeGenieConfiguration HomeGenie { get; set; }

        public MIGServiceConfiguration MIGService { get; set; }

        public SystemConfiguration()
        {
            OperatingSystem os = Environment.OSVersion;
            PlatformID platform = os.Platform;
            //
            HomeGenie = new HomeGenieConfiguration();
            MIGService = new MIGServiceConfiguration();
            //
            HomeGenie.SystemName = "HAL";
            HomeGenie.Location = "";
            HomeGenie.ServicePort = 80;
            HomeGenie.UserLogin = "admin";
            HomeGenie.UserPassword = ""; // password auth disabled by default
            HomeGenie.EnableLogFile = "false";
        }

        public object Clone()
        {
            var stream = new MemoryStream();
            var formatter = new BinaryFormatter();

            formatter.Serialize(stream, this);

            stream.Position = 0;
            object obj = formatter.Deserialize(stream);
            stream.Close();

            return obj;
        }

        /*
        public MIGServiceConfiguration.Interface GetInterface(string domain)
        {
            MIGServiceConfiguration.Interface res = MIGService.Interfaces.Find(i => i.Domain == domain);
            return res;
        }

        public MIGServiceConfiguration.Interface.Option GetInterfaceOption(string domain, string option)
        {
            return GetInterfaceOptions(domain).Find(o => o.Name == option);
        }

        public List<MIGServiceConfiguration.Interface.Option> GetInterfaceOptions(string domain)
        {
            MIGServiceConfiguration.Interface mi = MIGService.Interfaces.Find(i => i.Domain == domain);
            return mi.Options;
        }
        */

        public bool Update()
        {
            bool success = false;
            try
            {
                SystemConfiguration syscopy = (SystemConfiguration)this.Clone();
                foreach (ModuleParameter p in syscopy.HomeGenie.Settings)
                {
                    try
                    {
                        if (!String.IsNullOrEmpty(p.Value)) p.Value = StringCipher.Encrypt(p.Value, GetPassPhrase());
                    }
                    catch
                    {
                    }
                }


                string fname = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "systemconfig.xml");
                if (File.Exists(fname))
                {
                    File.Delete(fname);
                }
                System.Xml.XmlWriterSettings ws = new System.Xml.XmlWriterSettings();
                ws.Indent = true;
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(syscopy.GetType());
                System.Xml.XmlWriter wri = System.Xml.XmlWriter.Create(fname, ws);
                x.Serialize(wri, syscopy);
                wri.Close();
                success = true;
            }
            catch (Exception)
            {
            }
            //
            if (OnUpdate != null)
            {
                OnUpdate(success);
            }
            //
            return success;
        }

        public string GetPassPhrase()
        {
            return (this.HomeGenie.UserPassword + "homegenie");
        }
    }

    [Serializable()]
    public class HomeGenieConfiguration
    {
        public string SystemName { get; set; }

        public string Location { get; set; }

        public int ServicePort { get; set; }

        public string UserLogin { get; set; }

        public string UserPassword { get; set; }

        public List<ModuleParameter> Settings = new List<ModuleParameter>();

        public string GUID { get; set; }

        public string EnableLogFile { get; set; }

        public StatisticsConfiguration Statistics = new StatisticsConfiguration();

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

                MaxDatabaseSizeMBytes = 10; // 10MB default.
                StatisticsTimeResolutionSeconds = 5 * 60; // 5 minute default.
                StatisticsUIRefreshSeconds = 2 * 60; // 2 minute default.
            }

            /// <summary>
            /// Set constraints to protect the system. These are absolute constraints to protect the user experience (locked browser/server), but are not 
            /// RECOMMENDED constraints. For example, StatisticsTimeResolutionSeconds less than 5*60 starts to make the graph 
            /// look messy, but we still allow anything above 30 seconds in case advanced user wants it. Might want to keep 
            /// recommended values reference later.
            /// 
            /// Should later throw error so UI can notify user?
            /// </summary>
            public void Validate()
            {
                // 
                if (MaxDatabaseSizeMBytes < 1)
                {
                    MaxDatabaseSizeMBytes = 1;
                }
                // Current design would make < 30 seconds a poor setting. In full day view, if this is anything less than a few minutes, day detail line is smashed.
                if (StatisticsTimeResolutionSeconds < 30)
                {
                    StatisticsTimeResolutionSeconds = 30;
                }
                if (StatisticsUIRefreshSeconds < 5)
                {
                    StatisticsUIRefreshSeconds = 5;
                }

            }

        }
    }

}

