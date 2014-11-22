using System;
using System.Collections;
using System.Text;

namespace OSC.NET
{
	/// <summary>
	/// OSCMessage
	/// </summary>
	public class OSCMessage : OSCPacket
	{
		protected const char INTEGER = 'i';
		protected const char FLOAT	  = 'f';
		protected const char LONG	  = 'h';
		protected const char DOUBLE  = 'd';
		protected const char STRING  = 's';
		protected const char SYMBOL  = 'S';
		//protected const char BLOB	  = 'b';
		//protected const char ALL     = '*';

		public OSCMessage(string address)
		{
			this.typeTag = ",";
			this.Address = address;
		}
		public OSCMessage(string address, object value)
		{
			this.typeTag = ",";
			this.Address = address;
			Append(value);
		}

		override protected void pack()
		{
			ArrayList data = new ArrayList();

			addBytes(data, packString(this.address));
			padNull(data);
			addBytes(data, packString(this.typeTag));
			padNull(data);
			
			foreach(object value in this.Values)
			{
				if(value is int) addBytes(data, packInt((int)value));
				else if(value is long) addBytes(data, packLong((long)value));
				else if(value is float) addBytes(data, packFloat((float)value));
				else if(value is double) addBytes(data, packDouble((double)value));
				else if(value is string)
				{
					addBytes(data, packString((string)value));
					padNull(data);
				}
				else 
				{
					// TODO
				}
			}
			
			this.binaryData = (byte[])data.ToArray(typeof(byte));
		}


		public static OSCMessage Unpack(byte[] bytes, ref int start)
		{
			string address = unpackString(bytes, ref start);
			//Console.WriteLine("address: " + address);
			OSCMessage msg = new OSCMessage(address);

			char[] tags = unpackString(bytes, ref start).ToCharArray();
			//Console.WriteLine("tags: " + new string(tags));
			foreach(char tag in tags)
			{
				//Console.WriteLine("tag: " + tag + " @ "+start);
				if(tag == ',') continue;
				else if(tag == INTEGER) msg.Append(unpackInt(bytes, ref start));
				else if(tag == LONG) msg.Append(unpackLong(bytes, ref start));
				else if(tag == DOUBLE) msg.Append(unpackDouble(bytes, ref start));
				else if(tag == FLOAT) msg.Append(unpackFloat(bytes, ref start));
				else if(tag == STRING || tag == SYMBOL) msg.Append(unpackString(bytes, ref start));
				else Console.WriteLine("unknown tag: "+tag);
			}

			return msg;
		}

		override public void Append(object value)
		{
			if(value is int)
			{
				AppendTag(INTEGER);
			}
			else if(value is long)
			{
				AppendTag(LONG);
			}
			else if(value is float)
			{
				AppendTag(FLOAT);
			}
			else if(value is double)
			{
				AppendTag(DOUBLE);
			}
			else if(value is string)
			{
				AppendTag(STRING);
			}
			else 
			{
				// TODO: exception
			}
			values.Add(value);
		}

		protected string typeTag;
		protected void AppendTag(char type)
		{
			typeTag += type;
		}


		override public bool IsBundle() { return false; }
	}
}
