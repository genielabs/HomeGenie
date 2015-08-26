using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace MIG
{

    [Serializable()]
    public class MIGServiceConfiguration
    {
        public string EnableWebCache { get; set; }

        public List<Interface> Interfaces = new List<Interface>();

        public MIGServiceConfiguration.Interface GetInterface(string domain)
        {
            var iface = this.Interfaces.Find(i => i.Domain == domain);
            return iface;
        }

        [Serializable()]
        public class Interface
        {

            [XmlAttribute]
            public string Domain { get; set; }
            
            public string Description { get; set; }

            [XmlAttribute]
            public bool IsEnabled { get; set; }

            public List<Option> Options = new List<Option>();

            [Serializable()]
            public class Option
            {
                [XmlAttribute]
                public string Name { get; set; }

                [XmlAttribute]
                public string Value { get; set; }
            }

            [XmlAttribute]
            public string AssemblyName { get; set; }

            // TODO: add SupportedPlatform field (Windows, Unix, All)
        }
    }

}

