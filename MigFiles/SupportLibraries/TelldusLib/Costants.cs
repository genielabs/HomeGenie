namespace TelldusLib
{
	public enum Command
	{
		TURNON = 1,
		TURNOFF = 2,
		BELL = 4,
		TOGGLE = 8,
		DIM = 16,
		LEARN = 32,
		EXECUTE = 64,
		UP = 128,
		DOWN = 256,
		STOP = 512,
	}

	public enum DataType
	{
		TEMPERATURE = 1,
		HUMIDITY = 2
	}

	public enum Error
	{
		SUCCESS = 0,
		NOT_FOUND = -1,
		PERMISSION_DENIED = -2,
		DEVICE_NOT_FOUND = -3,
		METHOD_NOT_SUPPORTED = -4,
		COMMUNICATION = -5,
		CONNECTING_SERVICE = -6,
		UNKNOWN_RESPONSE = -7,
		UNKNOWN = -99
	}

	internal enum TellsticType
	{
		DEVICE = 1,
		GROUP = 2
	}

	public enum DeviceEvent
	{
		ADDED = 1,
		CHANGED = 2,
		REMOVED = 3,
		STATE_CHANGED = 4
	}

	public enum ChangeType
	{
		NAME = 1,
		PROTOCOL = 2,
		MODEL = 3
	}
}
