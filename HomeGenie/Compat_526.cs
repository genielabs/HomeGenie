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
                var reader = new StreamReader(programsFile);
                var serializer = new XmlSerializer(typeof(ProgramBlock));
                var program = (ProgramBlock) serializer.Deserialize(reader);
                reader.Close();
                var reader2 = new StreamReader(programsFile);
                var serializer2 = new XmlSerializer(typeof(HomeGenie.Automation.ProgramBlock));
                var program2 = (HomeGenie.Automation.ProgramBlock) serializer2.Deserialize(reader2);
                reader2.Close();
                bool updated = false;
                var wiz = ConvertScriptSource(program);
                if (!string.IsNullOrEmpty(wiz))
                {
                    // Covert old wizard script to new format
                    program2.ScriptSource = ConvertScriptSource(program);
                    updated = true;
                }
                else if (!String.IsNullOrEmpty(program.ScriptCondition))
                {
                    // Rename old 'ScriptCondition' field to 'ScriptSetup'
                    program2.ScriptSetup = program.ScriptCondition;
                    updated = true;
                }

                if (updated)
                {
                    // TODO: should log something...
                    var writerSettings = new XmlWriterSettings();
                    writerSettings.Indent = true;
                    writerSettings.Encoding = Encoding.UTF8;
                    var programSerializer = new XmlSerializer(typeof(HomeGenie.Automation.ProgramBlock));
                    var builder = new XmlTextWriter(programsFile, Encoding.UTF8);
                    var writer = XmlWriter.Create(builder, writerSettings);
                    programSerializer.Serialize(writer, program2);
                    writer.Close();                    
                }
            }
            else
            {
                List<HomeGenie.Automation.ProgramBlock> programs;
                var serializer = new XmlSerializer(typeof(List<HomeGenie.Automation.ProgramBlock>));
                using (var reader = new StreamReader(programsFile))
                {
                    programs = (List<HomeGenie.Automation.ProgramBlock>)serializer.Deserialize(reader);
                }
                List<ProgramBlock> programs2;
                var serializer2 = new XmlSerializer(typeof(List<ProgramBlock>));
                using (var reader2 = new StreamReader(programsFile))
                {
                    bool updated = false;
                    programs2 = (List<ProgramBlock>)serializer2.Deserialize(reader2);
                    for (int p = 0; p < programs2.Count; p++)
                    {
                        string wiz = ConvertScriptSource(programs2[p]);
                        if (!string.IsNullOrEmpty(wiz))
                        {
                            programs[p].ScriptSource = wiz;
                            updated = true;
                        }
                    }

                    if (updated)
                    {
                        // Converted old wizard scripts to new format
                        // TODO: should log something...
                        Utility.UpdateXmlDatabase(programs, "programs.xml", null);
                    }
                }

            }

            return true;
        }

        private static string ConvertScriptSource(ProgramBlock program)
        {
            if (program.Type.ToLower() == "wizard" && program.Conditions.Count > 0 || program.Commands.Count > 0)
            {
                WizardEngine.WizardScript script = new WizardEngine.WizardScript(null)
                {
                    Commands = program.Commands.ToList<ScriptCommand>(),
                    Conditions = program.Conditions.ToList<ScriptCondition>(),
                    ConditionType = program.ConditionType
                };
                return JsonConvert.SerializeObject(script);
            }

            return null;
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