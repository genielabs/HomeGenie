// ReSharper disable InconsistentNaming

using System;

namespace HomeGenie.Data.UI
{
    [Serializable()]
    public class OptionField
    {
        public string pid;
        public ModuleField field;
        public OptionFieldType type;
        public string name;
        public string description;
    }
}
