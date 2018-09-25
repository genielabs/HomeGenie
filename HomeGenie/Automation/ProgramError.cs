using HomeGenie.Automation.Scripting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HomeGenie.Automation
{
    public class ProgramError
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorNumber { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public CodeBlockEnum CodeBlock { get; set; }
    }
}