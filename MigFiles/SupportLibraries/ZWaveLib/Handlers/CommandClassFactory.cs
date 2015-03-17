using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ZWaveLib.Handlers
{
    class CommandClassFactory
    {
        private static readonly object syncLock = new object();
        private static Dictionary<byte, Type> commandClasses;

        public static ICommandClass GetCommandClass(byte id)
        {
            if (commandClasses == null)
            {
                lock (syncLock)
                {
                    if (commandClasses == null)
                    {
                        commandClasses = CollectCommandClasses();
                    }
                }
            }

            Type type;
            if (!commandClasses.TryGetValue(id, out type)) 
                return null;

            return (ICommandClass)Activator.CreateInstance(type);
        }

        private static Dictionary<byte, Type> CollectCommandClasses()
        {
            var commandClassTypes = new Dictionary<byte, Type>();
            var assemblyTypes = Assembly.GetExecutingAssembly().GetTypes();
            var types = assemblyTypes.Where(t => typeof(ICommandClass).IsAssignableFrom(t)).ToList();

            foreach (var type in types)
            {
                if (type == typeof(ICommandClass))
                    continue; // we are not going to use interface itself
                var cc = (ICommandClass)Activator.CreateInstance(type);
                var id = (byte)cc.GetTypeId();
                commandClassTypes.Add(id, type);
            }

            return commandClassTypes;
        }
    }
}
