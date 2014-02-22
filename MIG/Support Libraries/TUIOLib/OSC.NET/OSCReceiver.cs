using System;
using System.Net;
using System.Net.Sockets;

namespace OSC.NET
{
	/// <summary>
	/// OSCReceiver
	/// </summary>
	public class OSCReceiver
	{
		protected UdpClient udpClient;
		protected int localPort;

		public OSCReceiver(int localPort)
		{
			this.localPort = localPort;
			Connect();
		}

		public void Connect()
		{
			if(this.udpClient != null) Close();
			this.udpClient = new UdpClient(this.localPort);
		}

		public void Close()
		{
			this.udpClient.Close();
			this.udpClient = null;
		}

		public OSCPacket Receive()
		{
			IPEndPoint ip = null;
			byte[] bytes = this.udpClient.Receive(ref ip);
			
			if(bytes != null && bytes.Length > 0)
			{
				return OSCPacket.Unpack(bytes);
			}
			return null; // TODO exception
		}
	}
}
