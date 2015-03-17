using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ZWaveLib.Handlers
{
    class CommandClassFactory
    {
        static readonly object lck = new object();

        public static ICommandClass GetCommandClass(byte ccId)
        {
            if (_commandClasses == null)
            {
                lock (lck)
                {
                    if (_commandClasses == null)
                    {
                        _commandClasses = CollectCommandClasses();
                    }
                }
            }

            Type type;
            if (!_commandClasses.TryGetValue(ccId, out type)) 
                return null;

            var cc = (ICommandClass)Activator.CreateInstance(type);
            return cc;
        }

        private static Dictionary<byte, Type> _commandClasses;

        private static Dictionary<byte, Type> CollectCommandClasses()
        {
            var iCommandClassTypes = new Dictionary<byte, Type>();
            var iType = typeof (ICommandClass);

            var assemblyTypes = Assembly.GetExecutingAssembly().GetTypes();
            var types = assemblyTypes.Where(t => iType.IsAssignableFrom(t)).ToList();

            foreach (var type in types)
            {
                if (type == iType)
                    continue; // we are not going to use interface itself
                var cc = (ICommandClass) Activator.CreateInstance(type);
                var id = cc.GetCommandClassId();
                iCommandClassTypes.Add(id, type);
            }

            return iCommandClassTypes;
        }
    }
}
