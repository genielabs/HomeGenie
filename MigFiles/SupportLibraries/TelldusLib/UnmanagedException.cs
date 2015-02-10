using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelldusLib
{
	public class UnmanagedException : TellStickException
	{
		public UnmanagedException()
		{
		}

		public UnmanagedException(string message)
			: base(message)
		{
		}

		public UnmanagedException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
