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
using System.Text;
using System.Xml.Serialization;

using MIG.Config;

using HomeGenie.Service;

namespace HomeGenie.Data
{
    [Serializable()]
    public class SystemConfiguration
    {
        private string passphrase = "";

        // TODO: change this to use standard event delegates model
        public event Action<bool> OnUpdate;

        public HomeGenieConfiguration HomeGenie { get; set; }

        public MigServiceConfiguration MigService { get; set; }

        public SystemConfiguration()
        {
            HomeGenie = new HomeGenieConfiguration();
            HomeGenie.SystemName = "HAL";
            HomeGenie.Location = "";
            HomeGenie.EnableLogFile = "false";
            MigService = new MigServiceConfiguration();
        }

        public bool Update()
        {
            bool success = false;
            try
            {
                var syscopy = this.DeepClone();
                foreach (ModuleParameter p in syscopy.HomeGenie.Settings)
                {
                    try
                    {
                        if (!String.IsNullOrEmpty(p.Value))
                            p.Value = StringCipher.Encrypt(p.Value, GetPassPhrase());
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
                ws.Encoding = Encoding.UTF8;
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(syscopy.GetType());
                using (var wri = System.Xml.XmlWriter.Create(fname, ws))
                {
                    x.Serialize(wri, syscopy);
                }
                success = true;
            }
            catch (Exception e)
            {
                MIG.MigService.Log.Error(e);
            }
            //
            if (OnUpdate != null)
            {
                OnUpdate(success);
            }
            //
            return success;
        }

        public void SetPassPhrase(string pass)
        {
            passphrase = pass;
        }

        public string GetPassPhrase()
        {
            return passphrase;
        }
    }

    [Serializable()]
    public class HomeGenieConfiguration
    {
        public string GUID { get; set; }
        public string SystemName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Location { get; set; }

        public List<ModuleParameter> Settings = new List<ModuleParameter>();
        public StatisticsConfiguration Statistics = new StatisticsConfiguration();

        public HomeGenieConfiguration()
        {
            // default values
            Username = "admin";
            Password = "";
        }
        
        public string EnableLogFile { get; set; }

        [Serializable()]
        public class StatisticsConfiguration
        {

            [XmlAttribute]
            public int MaxDatabaseSizeMBytes { get; set; }

            [XmlAttribute]
            public int StatisticsTimeResolutionSeconds { get; set; }

            [XmlAttribute]
            public int StatisticsUiRefreshSeconds { get; set; }

            public StatisticsConfiguration()
            {
                MaxDatabaseSizeMBytes = 5; // 5MB default.
                StatisticsTimeResolutionSeconds = 180; // 3 minutes default.
                StatisticsUiRefreshSeconds = 120; // 2 minutes default.
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
                if (MaxDatabaseSizeMBytes < 1)
                {
                    MaxDatabaseSizeMBytes = 1;
                }
                // Current design would make < 30 seconds a poor setting. In full day view, if this is anything less than a few minutes, day detail line is smashed.
                if (StatisticsTimeResolutionSeconds < 30)
                {
                    StatisticsTimeResolutionSeconds = 30;
                }
                if (StatisticsUiRefreshSeconds < 30)
                {
                    StatisticsUiRefreshSeconds = 30;
                }
            }

        }
    }

}
