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
*     Project Homepage: https://homegenie.it
*/

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

using Newtonsoft.Json;

using HomeGenie.Service.Constants;
using HomeGenie.Automation.Engines;

namespace HomeGenie.Automation
{
    public class MethodRunResult
    {
        public Exception Exception = null;
        public object ReturnValue = null;
    }

    public class ImplementedInterface
    {
        public string Identifier;
        public string ApiUrl;
        public object Options;
    }

    [Serializable()]
    public class ProgramPackageInfo
    {
        [JsonProperty("repository")]
        public string Repository { get; set; }
        [JsonProperty("packageId")]
        public string PackageId { get; set; }
        [JsonProperty("packageVersion")]
        public string PackageVersion { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("required")]
        public bool Required { get; set; }
        [JsonProperty("checksum")]
        public string Checksum { get; set; }
    }

    [Serializable()]
    public class ProgramBlock
    {
        private IProgramEngine programEngine;
        private bool isProgramEnabled;
        private string codeType = "";

        internal List<ImplementedInterface> ImplementedInterfaces = new List<ImplementedInterface>();

        // event delegates
        public delegate void EnabledStateChangedEventHandler(object sender, bool isEnabled);
        public event EnabledStateChangedEventHandler EnabledStateChanged;

        // c# program public members
        public string ScriptSetup { get; set; }
        public string ScriptSource { get; set; }
        public string ScriptContext { get; set; }
        public string ScriptErrors { get; set; }
        public string Data { get; set; }

        // common public members
        public ProgramPackageInfo PackageInfo { get; set; }
        public string Domain  { get; set; }
        public int Address  { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Group { get; set; }
        public List<ProgramFeature> Features  { get; set; }
        public bool AutoRestartEnabled { get; set; }
        public bool Cloneable { get; set; }

        [XmlIgnore, JsonIgnore]
        public bool WillRun { get; set; }
        [XmlIgnore]
        public bool IsRunning { get; set; }
        [XmlIgnore, JsonIgnore]
        public object OperationLock = new object();

        public ProgramBlock()
        {
            // init stuff
            PackageInfo = new ProgramPackageInfo();
            Domain = Domains.HomeAutomation_HomeGenie_Automation;
            Address = 0;
            Features = new List<ProgramFeature>();

            Type = "csharp";
            ScriptSetup = "";
            ScriptSource = "";
            ScriptContext = "";
            Data = "";
            ScriptErrors = "";
            //
            isProgramEnabled = false;
            IsRunning = false;
        }

        public string Type
        {
            get { return codeType; }
            set
            {
                bool changed = codeType != value;
                codeType = value;
                if (changed || programEngine == null)
                {
                    if (programEngine != null)
                    {
                        programEngine.Unload();
                        programEngine = null;
                    }
                    switch (codeType.ToLower())
                    {
                        case "csharp":
                        case "visual":
                            programEngine = new CSharpEngine(this);
                            break;
                        case "python":
                            programEngine = new PythonEngine(this);
                            break;
                        case "javascript":
                            programEngine = new JavascriptEngine(this);
                            break;
                        case "wizard":
                            // TODO: deprecate "wizard" type and WizardEngine
                            programEngine = new WizardEngine(this);
                            break;
                        case "arduino":
                            programEngine = new ArduinoEngine(this);
                            break;
                    }
                }
            }
        }

        public bool IsEnabled
        {
            get { return isProgramEnabled; }
            set
            {
                if (isProgramEnabled != value)
                {
                    isProgramEnabled = value;
                    if (isProgramEnabled)
                    {
                        if (programEngine != null) programEngine.Load();
                    }
                    else
                    {
                        if (programEngine != null) programEngine.Unload();
                    }

                    if (EnabledStateChanged != null) EnabledStateChanged.Invoke(this, value);
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public ProgramEngineBase Engine
        {
            get { return (ProgramEngineBase) programEngine; }
        }

        [XmlIgnore, JsonIgnore]
        internal List<string> BackupFiles = new List<string>();
    }
}
