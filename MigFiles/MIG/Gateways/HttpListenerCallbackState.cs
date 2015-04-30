/*
    This file is part of HomeGenie Project source code.

    HomeGenie is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    HomeGenie is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with HomeGenie.  If not, see <http://www.gnu.org/licenses/>.  
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: http://homegenie.it
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace MIG.Gateways
{
	public class HttpListenerCallbackState
	{
		private readonly HttpListener listener;
		private readonly AutoResetEvent listenForNextRequest;

		public HttpListenerCallbackState(HttpListener listener)
		{
			if (listener == null) throw new ArgumentNullException("listener");
			this.listener = listener;
			listenForNextRequest = new AutoResetEvent(false);
		}

		public HttpListener Listener { get { return listener; } }

		public AutoResetEvent ListenForNextRequest { get { return listenForNextRequest; } }
	}
}