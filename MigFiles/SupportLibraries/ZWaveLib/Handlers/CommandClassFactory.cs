using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ZWaveLib.Handlers
{
    class CommandClassFactory
    {
        public static ICommandClass GetCommandClass(byte ccId)
        {
            if(_commandClasses == null)
                _commandClasses = CollectCommandClasses();

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
            var iType = typeof(ICommandClass);
            var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => iType.IsAssignableFrom(t));

            foreach (var type in types)
            {
                var cc = (ICommandClass)Activator.CreateInstance(type);
                var id = cc.GetCommandClassId();
                _commandClasses.Add(id, type);
            }

            return iCommandClassTypes;
        }
    }
}
