// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;

namespace HomeGenie.Data.UI
{
    [Serializable()]
    public class ModuleOptions
    {
        public string id;
        public string name;
        public string description;
        public List<OptionField> items;
    }
}
