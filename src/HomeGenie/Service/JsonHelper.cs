using Newtonsoft.Json;

namespace HomeGenie.Service
{
    public static class JsonHelper
    {
        public static string ToPrettyJson(this object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
    }
}