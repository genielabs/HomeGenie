using System;
using System.Collections;

namespace OSC.NET
{
	/// <summary>
	/// OSCBundle
	/// </summary>
	public class OSCBundle : OSCPacket
	{
		protected const string BUNDLE = "#bundle";
		private long timestamp = 0;
		
		public OSCBundle(long ts)
		{
			this.address = BUNDLE;
			this.timestamp = ts;
		}

		public OSCBundle()
		{
			this.address = BUNDLE;
			this.timestamp = 0;
		}

		override protected void pack()
		{
			ArrayList data = new ArrayList();

			addBytes(data, packString(this.Address));
			padNull(data);
			addBytes(data, packLong(0)); // TODO
			
			foreach(object value in this.Values)
			{
				if(value is OSCPacket)
				{
					byte[] bs = ((OSCPacket)value).BinaryData;
					addBytes(data, packInt(bs.Length));
					addBytes(data, bs);
				}
				else 
				{
					// TODO
				}
			}
			
			this.binaryData = (byte[])data.ToArray(typeof(byte));
		}

		public static new OSCBundle Unpack(byte[] bytes, ref int start, int end)
		{

			string address = unpackString(bytes, ref start);
			//Console.WriteLine("bundle: " + address);
			if(!address.Equals(BUNDLE)) return null; // TODO

			long timestamp = unpackLong(bytes, ref start);
			OSCBundle bundle = new OSCBundle(timestamp);
			
			while(start < end)
			{
				int length = unpackInt(bytes, ref start);
				int sub_end = start + length;
				//Console.WriteLine(bytes.Length +" "+ start+" "+length+" "+sub_end);
				bundle.Append(OSCPacket.Unpack(bytes, ref start, sub_end));

			}

			return bundle;
		}

		public long getTimeStamp() {
			return timestamp;
		}

		override public void Append(object value)
		{
			if( value is OSCPacket) 
			{
				values.Add(value);
			}
			else 
			{
				// TODO: exception
			}
		}

		override public bool IsBundle() { return true; }
	}
}

