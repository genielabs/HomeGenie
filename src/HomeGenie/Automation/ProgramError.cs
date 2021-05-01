using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using HomeGenie.Automation.Scripting;

namespace HomeGenie.Automation
{
    public class ProgramError
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public int EndLine { get; set; }
        public int EndColumn { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorNumber { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public CodeBlockEnum CodeBlock { get; set; }
    }
}