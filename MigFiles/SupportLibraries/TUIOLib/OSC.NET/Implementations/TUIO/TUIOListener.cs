/*
	MIG Input Plugin TUIO Listener Interface
    author: Generoso Martello <gene@homegenie.it>
    date  : 11-2008
*/

using System;

using OSC.NET;

namespace OSC.NET.Implementations.TUIO
{

	public interface TUIOListener
	{
        bool processMessage(OSCMessage om);
	}
}
