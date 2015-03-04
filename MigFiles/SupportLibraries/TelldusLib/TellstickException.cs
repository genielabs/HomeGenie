using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelldusLib
{
	public abstract class TellStickException : Exception
	{
		protected TellStickException()
		{
		}
		protected TellStickException(string message)
			: base(message)
		{
		}
		protected TellStickException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
