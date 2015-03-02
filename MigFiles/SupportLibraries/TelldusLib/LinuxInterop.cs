using System;
using System.Runtime.InteropServices;

namespace TelldusLib
{
	internal sealed class LinuxInterop
	{
		[DllImport("libtelldus-core.so.2")]
		public static extern int tdGetNumberOfDevices();
		[DllImport("libtelldus-core.so.2")]
		public static extern int tdGetDeviceId(int value);
		[DllImport("libtelldus-core.so.2")]
		public unsafe static extern char* tdGetName(int deviceId);
		[DllImport("libtelldus-core.so.2")]
		public unsafe static extern char* tdGetProtocol(int deviceId);
		[DllImport("libtelldus-core.so.2")]
		public unsafe static extern char* tdGetModel(int deviceId);
		[DllImport("libtelldus-core.so.2")]
		public unsafe static extern char* tdGetDeviceParameter(int deviceId, char* name, char* defaultValue);
		[DllImport("libtelldus-core.so.2")]
		public unsafe static extern bool tdSetName(int deviceId, char* name);
		[DllImport("libtelldus-core.so.2")]
		public unsafe static extern bool tdSetProtocol(int deviceId, char* protocol);
		[DllImport("libtelldus-core.so.2")]
		public unsafe static extern bool tdSetModel(int deviceId, char* model);
		[DllImport("libtelldus-core.so.2")]
		public unsafe static extern bool tdSetDeviceParameter(int deviceId, char* name, char* value);
		[DllImport("libtelldus-core.so.2")]
		public static extern int tdAddDevice();
		[DllImport("libtelldus-core.so.2")]
		public static extern bool tdRemoveDevice(int deviceId);
		[DllImport("libtelldus-core.so.2")]
		public static extern int tdMethods(int deviceId, int methodsSupported);
		[DllImport("libtelldus-core.so.2")]
		public static extern int tdTurnOn(int deviceId);
		[DllImport("libtelldus-core.so.2")]
		public static extern int tdTurnOff(int deviceId);
		[DllImport("libtelldus-core.so.2")]
		public static extern int tdBell(int deviceId);
		[DllImport("libtelldus-core.so.2")]
		public static extern int tdDim(int deviceId, byte level);
		[DllImport("libtelldus-core.so.2")]
		public unsafe static extern char* tdGetErrorString(int errorNo);
		[DllImport("libtelldus-core.so.2")]
		public static extern void tdClose();
		[DllImport("libtelldus-core.so.2")]
		public static extern void tdInit();
		[DllImport("libtelldus-core.so.2")]
		public unsafe static extern int tdRegisterDeviceEvent(Delegate eventFunction, void* context);
		[DllImport("libtelldus-core.so.2")]
		public static extern int tdLastSentCommand(int deviceId, int methods);
		[DllImport("libtelldus-core.so.2")]
		public static extern int tdGetDeviceType(int deviceId);
		[DllImport("libtelldus-core.so.2")]
		public unsafe static extern int tdSendRawCommand(char* command, int reserved);
		[DllImport("libtelldus-core.so.2")]
		public unsafe static extern int tdRegisterRawDeviceEvent(Delegate rawListeningFunction, void* context);
		[DllImport("libtelldus-core.so.2")]
		public static extern int tdLearn(int deviceId);
		[DllImport("libtelldus-core.so.2")]
		public unsafe static extern char* tdLastSentValue(int deviceId);
		[DllImport("libtelldus-core.so.2")]
		public unsafe static extern void tdReleaseString(char* value);
		[DllImport("libtelldus-core.so.2")]
		public static extern int tdUnregisterCallback(int eventId);
		[DllImport("libtelldus-core.so.2")]
		public unsafe static extern int tdRegisterDeviceChangeEvent(Delegate deviceEventFunction, void* context);
		[DllImport("libtelldus-core.so.2")]
		public unsafe static extern int tdRegisterSensorEvent(Delegate sensorEventFunction, void* context);
	}
}
