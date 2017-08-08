using System.Collections.Generic;
using HomeGenie.Automation.Scripting;
using Microsoft.Scripting.Hosting;

namespace HomeGenie.Automation
{
    public class ScriptEngineErrors : ErrorListener
    {
        private readonly CodeBlockEnum blockType;
        public List<ProgramError> Errors = new List<ProgramError>();

        public ScriptEngineErrors(CodeBlockEnum type)
        {
            blockType = type;
        }

        public override void ErrorReported(ScriptSource source, string message, Microsoft.Scripting.SourceSpan span, int errorCode, Microsoft.Scripting.Severity severity)
        {
            Errors.Add(new ProgramError {
                Line = span.Start.Line,
                Column = span.Start.Column,
                ErrorMessage = message,
                ErrorNumber = errorCode.ToString(),
                CodeBlock = blockType
            });
        }
    }
}