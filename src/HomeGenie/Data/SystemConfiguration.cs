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
        private object configWriteLock = new object();
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
                    if (p.GetData() is string)
                    {
                        string stringValue = p.Value;
                        try
                        {
                            if (!String.IsNullOrEmpty(stringValue))
                            {
                                p.Value = StringCipher.Encrypt(stringValue, GetPassPhrase());
                            }
                        }
                        catch (Exception ex)
                        {
                            MIG.MigService.Log.Error(ex);
                        }
                    }
                }
                string fname = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "systemconfig.xml");
                if (File.Exists(fname))
                {
                    File.Delete(fname);
                }
                var ws = new System.Xml.XmlWriterSettings();
                ws.Indent = true;
                ws.Encoding = Encoding.UTF8;
                XmlSerializer x = new XmlSerializer(syscopy.GetType());
                lock (configWriteLock)
                {
                    using (var wri = System.Xml.XmlWriter.Create(fname, ws))
                    {
                        x.Serialize(wri, syscopy);
                    }
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

        public HomeGenieConfiguration()
        {
            // default values
            Username = "admin";
            Password = "";
        }

        public string EnableLogFile { get; set; }
    }

}
