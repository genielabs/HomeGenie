using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelldusLib
{
	public class ModuleNotFoundException : TellStickException
	{
		public ModuleNotFoundException()
			: base("Could not locate module for Telldus API.")
		{
		}
		public ModuleNotFoundException(string message)
			: base(message)
		{
		}
		public ModuleNotFoundException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
