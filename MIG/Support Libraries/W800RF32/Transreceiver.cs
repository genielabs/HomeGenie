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


//
// references: X10 protocol documentation from http://www.linuxha.com/USB/cm15a.html
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;

namespace W800RF32
{
    public class RfDataReceivedAction
    {
        public byte[] RawData;
    }

    public class Transreceiver
    {
		public event PropertyChangedEventHandler PropertyChanged;

        public event Action<RfDataReceivedAction> RfDataReceived;

		private Queue<byte[]> _sendqueue = new Queue<byte[]>();

        private Thread treader;
        private Thread twriter;

        private object _comlock = new object();
		private object _accesslock = new object();

		private bool gotReadWriteError = true;
		private bool keepconnectionalive = false;
        private Thread _connectionwatcher;

		private string _portname = "COM1"; 
		private RfDirect _rawinterface;

        private int _zerochecksumcount = 0;

		public Transreceiver()
        {
			_rawinterface = new RfDirect(_portname);
        }



		public string PortName
		{
			get { return _portname; }
			set 
			{
				if (_portname != value)
				{
					_close();
					//
					_rawinterface = new RfDirect(value);
					gotReadWriteError = true;
				}
				_portname = value; 
			}
		}

		public bool Connect ()
		{
			bool returnvalue = _open ();
			//
			if (_connectionwatcher != null) {
				try
				{
					keepconnectionalive = false;
					_connectionwatcher.Abort();
				} catch { }
			}
			//
			keepconnectionalive = true;
			//
            _connectionwatcher = new Thread(new ThreadStart(delegate()
            {
                gotReadWriteError = !returnvalue;
                //
				lock (_accesslock)
				{
					while (keepconnectionalive)
	                {
						if (gotReadWriteError)
	                    {
							try
							{
		                        _sendqueue.Clear();
								//
								_close();
								//
								Thread.Sleep(5000);
		                        if (keepconnectionalive)
		                        {
									try
									{
		                            	gotReadWriteError = !_open();
									} catch { }
		                        }
							} catch (Exception unex) {
								//Console.WriteLine(unex.Message + "\n" + unex.StackTrace);
							}
						}
						//
						Monitor.Wait(_accesslock, 5000);
					}
                }
            }));
            _connectionwatcher.Start();
            //    
			return returnvalue;
		}

        public void Disconnect()
        {
            keepconnectionalive = false;
			//
			_close();
		}

        public bool IsConnected
        {
            get { return !gotReadWriteError; }
        }







		public static string Reverse( string s )
		{
			char[] charArray = s.ToCharArray();
			Array.Reverse( charArray );
			return new string( charArray );
		}


        private void _readerThreadLoop()
        {
			lock (_accesslock)
			{
				while (true)
				{
					//
					try
					{
						byte[] readdata = _rawinterface.ReadData();
						//                        Console.WriteLine("<<<<< " + Utility.ByteArrayToString(readdata));
						//
						if (readdata.Length > 0)
						{
							_zerochecksumcount = 0; // Linux/Pi disconnection detection
							//
							string sb1 = Convert.ToString( readdata[0], 2 ).PadLeft( 8, '0' );
							string sb2 = Convert.ToString( readdata[1], 2 ).PadLeft( 8, '0' );
							string sb3 = Convert.ToString( readdata[2], 2 ).PadLeft( 8, '0' );
							string sb4 = Convert.ToString( readdata[3], 2 ).PadLeft( 8, '0' );
							sb1 = Reverse(sb1);
							sb2 = Reverse(sb2);
							sb3 = Reverse(sb3);
							sb4 = Reverse(sb4);

							readdata[3] = Convert.ToByte(sb3, 2);
							readdata[2] = Convert.ToByte(sb4, 2);
							readdata[1] = Convert.ToByte(sb1, 2);
							readdata[0] = Convert.ToByte(sb2, 2);

//Console.WriteLine("RF      ==> " + Transreceiver.ByteArrayToString(readdata));
							if (RfDataReceived != null)
							{
								RfDataReceived(new RfDataReceivedAction() { RawData = readdata });
							}
						}
						else
						{
							// BEGIN: This is an hack for detecting disconnection status in Linux/Raspi
							_zerochecksumcount++;
							//
							if (_zerochecksumcount > 10)
							{
								_zerochecksumcount = 0;
								gotReadWriteError = true;
								_close();
							}
							// END: Linux/Raspi hack
							else if (_waitingchecksum)
							{
//Console.WriteLine("Expected [" + Transreceiver.ByteArrayToString(new byte[] { _expectedchecksum }) + "] Checksum ==> " + Transreceiver.ByteArrayToString(readdata));
								// checksum verification not handled, we just reply 0x00 (OK)
								_sendMessage(new byte[] { 0x00 });
							}
						}
						
					}
					catch (Exception e) {
						if (!e.GetType().Equals(typeof(TimeoutException)))
						{
                            // TODO: add error logging 
                            gotReadWriteError = true;
						}                
					}
					Monitor.Wait(_accesslock, 10);
				}
			}
        }


		public void WaitComplete ()
		{
			int hitcount = 0;
			while (true) {
				if (_sendqueue.Count == 0 && ++hitcount == 2)
				{
					break;
				}
				Thread.Sleep(50); 
			}
		}


		public static String ByteArrayToString(byte[] message)
		{
			String ret = String.Empty;
			foreach (byte b in message)
			{
				ret += b.ToString("X2") + " ";
			}
			return ret.Trim();
		}
        

		private void _modulePropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			// route event
			if (PropertyChanged != null)
			{
				PropertyChanged(sender, args);
			}
		}

        bool _waitingchecksum = false;
        byte _expectedchecksum = 0x00;
		private void _sendMessage (byte[] message)
		{
            _rawinterface.WriteData(message);
		}

        private void _writerThreadLoop()
        {
            while (true)
            {
				try
				{
					if (_sendqueue.Count > 0)
		            {
                        byte[] msg = _sendqueue.Dequeue();
//Console.WriteLine(">>>>> " + Transreceiver.ByteArrayToString(msg));

                        Monitor.Enter(_comlock);

                        if (_waitingchecksum && msg.Length > 1)
                        {
                            Monitor.Wait(_comlock, 3000);
                            _waitingchecksum = false;
                        }

                        _sendMessage(msg);

                        if (msg.Length > 1)
                        {
                            _expectedchecksum = (byte)((msg[0] + msg[1]) & 0xff);
                            _waitingchecksum = true;
                        }

                        Monitor.Exit(_comlock);                        
                        

					}
	                else
					{
						Thread.Sleep(250);
					}
				}
				catch
				{
					gotReadWriteError = true;
				}
				finally
				{
				}
			}
        }

		private bool _open ()
		{
			bool success = false;
			lock (_accesslock) {
				success = (_rawinterface != null && _rawinterface.Open ());
				if (success) {
					treader = new Thread (new ThreadStart (_readerThreadLoop));
					twriter = new Thread (new ThreadStart (_writerThreadLoop));
					//
					treader.Start ();
					twriter.Start ();
				}
			}
			return success;
		}

		private void _close ()
		{
			lock (_accesslock) {
				try
				{
					if (_rawinterface != null)
						_rawinterface.Close ();
				}
				catch
				{ }
				//
				try
				{
					if (twriter != null)
						twriter.Abort();
				}
				catch
				{ }
				try
				{
					if (treader != null)
						treader.Abort();
				}
				catch
				{ }
				twriter = null;
				treader = null;
			}
		}


    }

}

