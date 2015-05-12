using System;
using HomeGenie.Service;
using HomeGenie.Data;

namespace HomeGenie
{
    [Serializable()]
    public class Store
    {
        public string Name;
        public string Description;
        public TsList<ModuleParameter> Data;
        public Store()
        {
            this.Name = "";
            this.Description = "";
            this.Data = new TsList<ModuleParameter>();
        }
        public Store(string name, string description = "")
        {
            this.Name = name;
            this.Description = description;
            this.Data = new TsList<ModuleParameter>();
        }
    }
}

