using System;
using System.Collections.Generic;
using System.Linq;

namespace HomeGenie
{
    public static class ProgramDynamiApi
    {
        private static Dictionary<string, Func<object, object>> dynamicApi = new Dictionary<string, Func<object, object>>();

        public static Func<object, object> Find(string request)
        {
            Func<object, object> handler = null;
            if (dynamicApi.ContainsKey(request))
            {
                handler = dynamicApi[request];
            }
            return handler;
        }
        public static Func<object, object> FindMatching(string request)
        {
            Func<object, object> handler = null;
            for (int i = 0; i < dynamicApi.Keys.Count; i++)
            {
                if (request.StartsWith(dynamicApi.Keys.ElementAt(i)))
                {
                    handler = dynamicApi[dynamicApi.Keys.ElementAt(i)];
                    break;
                }
            }
            return handler;
        }
        public static void Register(string request, Func<object, object> handlerfn)
        {
            if (dynamicApi.ContainsKey(request))
            {
                dynamicApi[request] = handlerfn;
            }
            else
            {
                dynamicApi.Add(request, handlerfn);
            }
        }
        public static void UnRegister(string request)
        {
            if (dynamicApi.ContainsKey(request))
            {
                dynamicApi.Remove(request);
            }
        }

    }
}

