using System;
using System.Collections.Generic;
using HomeGenie.Automation.Scripting;

namespace HomeGenie.Automation.Engines
{
    public interface IProgramEngine
    {
        void Unload();
        bool Load();

        List<ProgramError> Compile();
        MethodRunResult EvaluateCondition();
        MethodRunResult Run(string options);

        void Reset();

        ProgramError GetFormattedError(Exception e, bool isTriggerBlock);
    }
}

