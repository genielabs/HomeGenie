using System;
using System.Collections.Generic;

namespace HomeGenie.Data.UI
{
    [Serializable()]
    public class OptionFieldType
    {
        public string id; // OptionFieldTypeId
        public List<object> options;
    }
}
