/*
	TUIO C# backend - part of the reacTIVision project
	http://mtg.upf.es/reactable?software

	Copyright (c) 2006 Martin Kaltenbrunner <mkalten@iua.upf.es>

	Permission is hereby granted, free of charge, to any person obtaining
	a copy of this software and associated documentation files
	(the "Software"), to deal in the Software without restriction,
	including without limitation the rights to use, copy, modify, merge,
	publish, distribute, sublicense, and/or sell copies of the Software,
	and to permit persons to whom the Software is furnished to do so,
	subject to the following conditions:

	The above copyright notice and this permission notice shall be
	included in all copies or substantial portions of the Software.

	Any person wishing to distribute modifications to the Software is
	requested to send the modifications to the original developer so that
	they can be incorporated into the canonical version.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
	EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
	MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
	IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR
	ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
	CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
	WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Threading;
using System.Collections;

using OSC.NET;

namespace OSC.NET.Implementations.TUIO
{
  	
	public class TUIOClient 
	{
		private bool listening = false;
		private int port = 3333;
		private OSCReceiver receiver;
		private Thread thread;
		
		private ArrayList listenerList = new ArrayList();
		
		public TUIOClient() {}

		public TUIOClient(int port)
		{
			this.port = port;
            this.connect();
        }
		
		public int getPort() {
			return port;
		}

        public void connect(int port)
        {
            this.port = port;
            connect();
        }
		
		public void connect() {
			try {
				receiver = new OSCReceiver(port);
				startListening();
				
			} catch (Exception e) {
				Console.WriteLine("failed to connect to port "+port);
				Console.WriteLine(e.Message);
			}
		}
	
		public void disconnect() {
			stopListening();
		}

		private void startListening() {
			listening = true;
			thread = new Thread(new ThreadStart(listen));
			thread.Start();
		}

		private void stopListening() {
            if (receiver != null)
    			receiver.Close();
            thread.Abort();
		    listening = false;
		    receiver = null;
        }


		private void listen() {
			while(listening) {
				try {
					OSCPacket packet = receiver.Receive();
					if (packet!=null) {
						if (packet.IsBundle()) {
							ArrayList messages = packet.Values;
							for (int i=0; i<messages.Count; i++) {
								processMessage((OSCMessage)messages[i]);
							}
						} else processMessage((OSCMessage)packet);						
					} else Console.WriteLine("null packet");
				} catch (Exception e) { Console.WriteLine(e.Message); }
			}
		}




		private void processMessage(OSCMessage message) {			
            for (int i = 0; i < listenerList.Count; i++)
            {
                TUIOListener listener = (TUIOListener)listenerList[i];
                if (listener != null)
                {
                    if (listener.processMessage(message)) break;
                }
            }
        }

		
		public void addListener(TUIOListener listener) {
			listenerList.Add(listener);
		}
		
		public void removeListener(TUIOListener listener) {	
			listenerList.Remove(listener);
		}

	}
}
