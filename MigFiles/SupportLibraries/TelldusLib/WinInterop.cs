using System;
using System.Runtime.InteropServices;

namespace TelldusLib
{
	internal sealed class WinInterop
	{
		[DllImport("TelldusCore.dll")]
		public static extern int tdGetNumberOfDevices();
		[DllImport("TelldusCore.dll")]
		public static extern int tdGetDeviceId(int value);
		[DllImport("TelldusCore.dll")]
		public unsafe static extern char* tdGetName(int deviceId);
		[DllImport("TelldusCore.dll")]
		public unsafe static extern char* tdGetProtocol(int deviceId);
		[DllImport("TelldusCore.dll")]
		public unsafe static extern char* tdGetModel(int deviceId);
		[DllImport("TelldusCore.dll")]
		public unsafe static extern char* tdGetDeviceParameter(int deviceId, char* name, char* defaultValue);
		[DllImport("TelldusCore.dll")]
		public unsafe static extern bool tdSetName(int deviceId, char* name);
		[DllImport("TelldusCore.dll")]
		public unsafe static extern bool tdSetProtocol(int deviceId, char* protocol);
		[DllImport("TelldusCore.dll")]
		public unsafe static extern bool tdSetModel(int deviceId, char* model);
		[DllImport("TelldusCore.dll")]
		public unsafe static extern bool tdSetDeviceParameter(int deviceId, char* name, char* value);
		[DllImport("TelldusCore.dll")]
		public static extern int tdAddDevice();
		[DllImport("TelldusCore.dll")]
		public static extern bool tdRemoveDevice(int deviceId);
		[DllImport("TelldusCore.dll")]
		public static extern int tdMethods(int deviceId, int methodsSupported);
		[DllImport("TelldusCore.dll")]
		public static extern int tdTurnOn(int deviceId);
		[DllImport("TelldusCore.dll")]
		public static extern int tdTurnOff(int deviceId);
		[DllImport("TelldusCore.dll")]
		public static extern int tdBell(int deviceId);
		[DllImport("TelldusCore.dll")]
		public static extern int tdDim(int deviceId, byte level);
		[DllImport("TelldusCore.dll")]
		public unsafe static extern char* tdGetErrorString(int errorNo);
		[DllImport("TelldusCore.dll")]
		public static extern void tdClose();
		[DllImport("TelldusCore.dll")]
		public static extern void tdInit();
		[DllImport("TelldusCore.dll")]
		public unsafe static extern int tdRegisterDeviceEvent(Delegate eventFunction, void* context);
		[DllImport("TelldusCore.dll")]
		public static extern int tdLastSentCommand(int deviceId, int methods);
		[DllImport("TelldusCore.dll")]
		public static extern int tdGetDeviceType(int deviceId);
		[DllImport("TelldusCore.dll")]
		public unsafe static extern int tdSendRawCommand(char* command, int reserved);
		[DllImport("TelldusCore.dll")]
		public unsafe static extern int tdRegisterRawDeviceEvent(Delegate rawListeningFunction, void* context);
		[DllImport("TelldusCore.dll")]
		public static extern int tdLearn(int deviceId);
		[DllImport("TelldusCore.dll")]
		public unsafe static extern char* tdLastSentValue(int deviceId);
		[DllImport("TelldusCore.dll")]
		public unsafe static extern void tdReleaseString(char* value);
		[DllImport("TelldusCore.dll")]
		public static extern int tdUnregisterCallback(int eventId);
		[DllImport("TelldusCore.dll")]
		public unsafe static extern int tdRegisterDeviceChangeEvent(Delegate deviceEventFunction, void* context);
		[DllImport("TelldusCore.dll")]
		public unsafe static extern int tdRegisterSensorEvent(Delegate sensorEventFunction, void* context);
	}
}
