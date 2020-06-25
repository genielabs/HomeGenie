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

#if !NETCOREAPP
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using HomeGenie.Automation.Engines;
using HomeGenie.Automation.Engines.WizardScript;
using HomeGenie.Service;
using Newtonsoft.Json;

namespace HomeGenie
{
    public static class Compat_526
    {
        public static bool FixProgramsDatabase(string programsFile)
        {
            if (programsFile.EndsWith(".hgx"))
            {
                ProgramBlock program;
                using (var reader = new StreamReader(programsFile))
                {
                    var serializer = new XmlSerializer(typeof(ProgramBlock));
                    program = (ProgramBlock) serializer.Deserialize(reader);
                }

                HomeGenie.Automation.ProgramBlock programNew;
                var programSerializer = new XmlSerializer(typeof(HomeGenie.Automation.ProgramBlock));
                using (var reader = new StreamReader(programsFile))
                {
                    programNew = (HomeGenie.Automation.ProgramBlock) programSerializer.Deserialize(reader);
                }
                bool updated = false;
                var wizardJson = GetWizardScript(program);
                if (!string.IsNullOrEmpty(wizardJson))
                {
                    // Covert old wizard script to new format
                    programNew.ScriptSource = wizardJson;
                    updated = true;
                }
                else if (!string.IsNullOrEmpty(program.ScriptCondition))
                {
                    // Rename old 'ScriptCondition' field to 'ScriptSetup'
                    programNew.ScriptSetup = program.ScriptCondition;
                    updated = true;
                }

                if (!updated) return true;
                // TODO: should log something...
                var writerSettings = new XmlWriterSettings();
                writerSettings.Indent = true;
                writerSettings.Encoding = Encoding.UTF8;
                var builder = new XmlTextWriter(programsFile, Encoding.UTF8);
                using (var writer = XmlWriter.Create(builder, writerSettings))
                {
                    programSerializer.Serialize(writer, programNew);
                }
            }
            else
            {
                List<HomeGenie.Automation.ProgramBlock> programs;
                using (var reader = new StreamReader(programsFile))
                {
                    var serializer = new XmlSerializer(typeof(List<HomeGenie.Automation.ProgramBlock>));
                    programs = (List<HomeGenie.Automation.ProgramBlock>)serializer.Deserialize(reader);
                }
                List<ProgramBlock> programsNew;
                using (var reader = new StreamReader(programsFile))
                {
                    bool updated = false;
                    var serializer = new XmlSerializer(typeof(List<ProgramBlock>));
                    programsNew = (List<ProgramBlock>)serializer.Deserialize(reader);
                    for (int p = 0; p < programsNew.Count; p++)
                    {
                        string wizardJson = GetWizardScript(programsNew[p]);
                        if (!string.IsNullOrEmpty(wizardJson))
                        {
                            // Covert old wizard script to new format
                            programs[p].ScriptSource = wizardJson;
                            updated = true;
                        }
                        else if (!String.IsNullOrEmpty(programsNew[p].ScriptCondition))
                        {
                            // Rename old 'ScriptCondition' field to 'ScriptSetup'
                            programs[p].ScriptSetup = programsNew[p].ScriptCondition;
                            updated = true;
                        }
                    }
                    if (updated)
                    {
                        // Converted old wizard scripts to new format
                        // TODO: should log something...
                        Utility.UpdateXmlDatabase(programs, programsFile, null);
                    }
                }
            }
            return true;
        }

        private static string GetWizardScript(ProgramBlock program)
        {
            if (program.Type.ToLower() != "wizard" ||
                (program.Conditions.Count <= 0 && program.Commands.Count <= 0))
                return null;
            WizardEngine.WizardScript script = new WizardEngine.WizardScript(null)
            {
                Commands = program.Commands.ToList<ScriptCommand>(),
                Conditions = program.Conditions.ToList<ScriptCondition>(),
                ConditionType = program.ConditionType
            };
            return JsonConvert.SerializeObject(script);
        }

        public class ProgramBlock
        {
            public string Type;
            public List<ProgramCommand> Commands = new List<ProgramCommand>();
            public List<ProgramCondition> Conditions = new List<ProgramCondition>();
            public ConditionType ConditionType = ConditionType.None;
            public string ScriptCondition;
            public bool LastConditionEvaluationResult { get; set; }
        }

        public class ProgramCommand : ScriptCommand {}

        public class ProgramCondition : ScriptCondition {}
    }
}
#endif
