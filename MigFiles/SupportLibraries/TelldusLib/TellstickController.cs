using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace TelldusLib
{
	public class TellstickController : IDisposable
	{
		public delegate int DeviceChangeEventCallbackFunction(
			int deviceId, int changeEvent, int changeType, int callbackId, object obj, UnmanagedException ex);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public unsafe delegate int DeviceChangeEventFunctionDelegate(
			int deviceId, int changeEvent, int changeType, int callbackId, void* context);

		public delegate int EventCallbackFunction(
			int deviceId, int method, string data, int callbackId, object obj, UnmanagedException ex);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public unsafe delegate int EventFunctionDelegate(int deviceId, int method, char* data, int callbackId, void* context);

		public delegate int RawListeningCallbackFunction(
			string data, int controllerId, int callbackId, object obj, UnmanagedException ex);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public unsafe delegate int RawListeningDelegate(char* data, int controllerId, int callbackId, void* context);

		public delegate int SensorCallbackFunction(
			string protocol, string model, int id, int dataType, string val, int timestamp, int callbackId, object obj,
			UnmanagedException ex);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public unsafe delegate int SensorListeningDelegate(
			char* protocol, char* model, int id, int dataType, char* value, int timestamp, int callbackId, void* context);

		private readonly bool _isWin = true;
		private bool _isInitialized = false;
		private readonly Dictionary<int, object> callbackFunctionReferenceList = new Dictionary<int, object>();

		private readonly Dictionary<int, DeviceChangeEventCallbackFunction> deviceChangeEventList =
			new Dictionary<int, DeviceChangeEventCallbackFunction>();

		private readonly Dictionary<int, EventCallbackFunction> eventList = new Dictionary<int, EventCallbackFunction>();

		private readonly Dictionary<int, RawListeningCallbackFunction> rawListenerList =
			new Dictionary<int, RawListeningCallbackFunction>();

		private readonly Dictionary<int, SensorCallbackFunction> sensorListenerList =
			new Dictionary<int, SensorCallbackFunction>();

		private GCHandle deviceChangeEventContextHandle;
		private GCHandle eventContextHandle;
		private int lastEventID;
		private GCHandle rawListenerContextHandle;
		private int registeredDeviceChangeEventFunctionId = -1;
		private int registeredEventFunctionId = -1;
		private int registeredRawListenerFunctionId = -1;
		private int registeredSensorListenerFunctionId = -1;
		private GCHandle sensorListenerContextHandle;

		public bool IsConnected { get { return _isInitialized; } }

		public TellstickController() : this(false)
		{
		}

        public TellstickController(bool init)
		{
			_isWin = !Environment.OSVersion.Platform.ToString().Contains("Unix");
			if (init)
				Init();
		}

        ~TellstickController()
		{
			Dispose(false);
		}

		//Interops

		public void Init()
		{
			if(_isWin)
				WinInterop.tdInit();
			else
				LinuxInterop.tdInit();
			_isInitialized = true;
		}

		public int AddDevice()
		{
			return (!_isWin)
				? LinuxInterop.tdAddDevice()
				: WinInterop.tdAddDevice();
		}

		public int Bell(int deviceId)
		{
			return (!_isWin)
				? LinuxInterop.tdBell(deviceId)
				: WinInterop.tdBell(deviceId);
		}

		public void Close()
		{
			if (!_isInitialized)
				return;

			if (_isWin)
			{
				WinInterop.tdClose();
			}
			else
			{
				LinuxInterop.tdClose();
			}
			_isInitialized = false;
		}

		public int Dim(int deviceId, int percent)
		{
			byte level = Convert.ToByte(255m*(Convert.ToDecimal(percent)/100m));
			return (!_isWin) ? LinuxInterop.tdDim(deviceId, level) : WinInterop.tdDim(deviceId, level);
		}

		public int GetDeviceId(int order)
		{
			return (!_isWin) ? LinuxInterop.tdGetDeviceId(order) : WinInterop.tdGetDeviceId(order);
		}

		public string GetDeviceParameter(int deviceId, string name, string defaultValue)
		{
			unsafe
			{
				return (!_isWin)
					? AsString(LinuxInterop.tdGetDeviceParameter(deviceId, name.ToCharPointer(), defaultValue.ToCharPointer()))
					: AsString(WinInterop.tdGetDeviceParameter(deviceId, name.ToCharPointer(), defaultValue.ToCharPointer()));
			}
		}

		public int GetDeviceType(int deviceId)
		{
			return (!_isWin) ? LinuxInterop.tdGetDeviceType(deviceId) : WinInterop.tdGetDeviceType(deviceId);
		}

		public string GetErrorString(int errorNo)
		{
			unsafe
			{
				return (!_isWin)
					? AsString(LinuxInterop.tdGetErrorString(errorNo))
					: AsString(WinInterop.tdGetErrorString(errorNo));
			}
		}

		public string GetName(int deviceId)
		{
			unsafe
			{
				return (!_isWin)
					? AsString(LinuxInterop.tdGetName(deviceId))
					: AsString(WinInterop.tdGetName(deviceId));
			}
		}

		public int Methods(int deviceId, int methodsSupported)
		{
			return (!_isWin)
				? LinuxInterop.tdMethods(deviceId, methodsSupported)
				: WinInterop.tdMethods(deviceId, methodsSupported);
		}

		public string GetModel(int deviceId)
		{
			unsafe
			{
				return (!_isWin)
					? AsString(LinuxInterop.tdGetModel(deviceId))
					: AsString(WinInterop.tdGetModel(deviceId));
			}
		}

		public int GetNumberOfDevices()
		{
			return (!_isWin)
				? LinuxInterop.tdGetNumberOfDevices()
				: WinInterop.tdGetNumberOfDevices();
		}

		public string GetProtocol(int deviceId)
		{
			unsafe
			{
				return (!_isWin)
					? AsString(LinuxInterop.tdGetProtocol(deviceId))
					: AsString(WinInterop.tdGetProtocol(deviceId));
			}
		}

		public int LastSentCommand(int deviceId, int methods)
		{
			return (!_isWin)
				? LinuxInterop.tdLastSentCommand(deviceId, methods)
				: WinInterop.tdLastSentCommand(deviceId, methods);
		}

		public string LastSentValue(int deviceId)
		{
			unsafe
			{
				return (!_isWin)
					? AsString(LinuxInterop.tdLastSentValue(deviceId))
					: AsString(WinInterop.tdLastSentValue(deviceId));
			}
		}

		public int Learn(int deviceId)
		{
			return (!_isWin)
				? LinuxInterop.tdLearn(deviceId)
				: WinInterop.tdLearn(deviceId);
		}

		public int RegisterDeviceEvent(EventCallbackFunction eventFunc, object obj)
		{
			unsafe
			{
				if (eventList.Count == 0)
				{
					var eventFunctionDelegate = new EventFunctionDelegate(eventFunction);
					if (obj != null)
					{
						eventContextHandle = GCHandle.Alloc(obj);
						registeredEventFunctionId = ((!_isWin)
							? LinuxInterop.tdRegisterDeviceEvent(eventFunctionDelegate, (void*) GCHandle.ToIntPtr(eventContextHandle))
							: WinInterop.tdRegisterDeviceEvent(eventFunctionDelegate, (void*) GCHandle.ToIntPtr(eventContextHandle)));
					}
					else
					{
						registeredEventFunctionId = ((!_isWin)
							? LinuxInterop.tdRegisterDeviceEvent(eventFunctionDelegate, null)
							: WinInterop.tdRegisterDeviceEvent(eventFunctionDelegate, null));
					}
					if (callbackFunctionReferenceList.ContainsKey(registeredEventFunctionId))
					{
						callbackFunctionReferenceList.Remove(registeredEventFunctionId);
					}
					callbackFunctionReferenceList.Add(registeredEventFunctionId, eventFunctionDelegate);
				}
				this.lastEventID++;
				int lastEventID = this.lastEventID;
				eventList.Add(lastEventID, eventFunc);
				return lastEventID;
			}
		}

		public int RegisterDeviceChangeEvent(DeviceChangeEventCallbackFunction deviceEventFunc, object obj)
		{
			unsafe
			{
				if (deviceChangeEventList.Count == 0)
				{
					var deviceChangeEventFunctionDelegate = new DeviceChangeEventFunctionDelegate(deviceEventFunction);
					if (obj != null)
					{
						deviceChangeEventContextHandle = GCHandle.Alloc(obj);
						registeredDeviceChangeEventFunctionId = ((!_isWin)
							? LinuxInterop.tdRegisterDeviceChangeEvent(deviceChangeEventFunctionDelegate,
								(void*) GCHandle.ToIntPtr(deviceChangeEventContextHandle))
							: WinInterop.tdRegisterDeviceChangeEvent(deviceChangeEventFunctionDelegate,
								(void*) GCHandle.ToIntPtr(deviceChangeEventContextHandle)));
					}
					else
					{
						registeredDeviceChangeEventFunctionId = ((!_isWin)
							? LinuxInterop.tdRegisterDeviceChangeEvent(deviceChangeEventFunctionDelegate, null)
							: WinInterop.tdRegisterDeviceChangeEvent(deviceChangeEventFunctionDelegate, null));
					}
					if (!callbackFunctionReferenceList.ContainsKey(registeredDeviceChangeEventFunctionId))
					{
						callbackFunctionReferenceList.Remove(registeredDeviceChangeEventFunctionId);
					}
					callbackFunctionReferenceList.Add(registeredDeviceChangeEventFunctionId,
						deviceChangeEventFunctionDelegate);
				}
				this.lastEventID++;
				int lastEventID = this.lastEventID;
				deviceChangeEventList.Add(lastEventID, deviceEventFunc);
				return lastEventID;
			}
		}

		public int RegisterRawDeviceEvent(RawListeningCallbackFunction listeningFunc, object obj)
		{
			unsafe
			{
				if (rawListenerList.Count == 0)
				{
					var rawListeningDelegate = new RawListeningDelegate(rawListeningFunction);
					if (obj != null)
					{
						rawListenerContextHandle = GCHandle.Alloc(obj);
						registeredRawListenerFunctionId = ((!_isWin)
							? LinuxInterop.tdRegisterRawDeviceEvent(rawListeningDelegate,
								(void*) GCHandle.ToIntPtr(rawListenerContextHandle))
							: WinInterop.tdRegisterRawDeviceEvent(rawListeningDelegate,
								(void*) GCHandle.ToIntPtr(rawListenerContextHandle)));
					}
					else
					{
						registeredRawListenerFunctionId = ((!_isWin)
							? LinuxInterop.tdRegisterRawDeviceEvent(rawListeningDelegate, null)
							: WinInterop.tdRegisterRawDeviceEvent(rawListeningDelegate, null));
					}
					if (!callbackFunctionReferenceList.ContainsKey(registeredRawListenerFunctionId))
					{
						callbackFunctionReferenceList.Remove(registeredRawListenerFunctionId);
					}
					callbackFunctionReferenceList.Add(registeredRawListenerFunctionId, rawListeningDelegate);
				}
				this.lastEventID++;
				int lastEventID = this.lastEventID;
				rawListenerList.Add(lastEventID, listeningFunc);
				return lastEventID;
			}
		}

		public int RegisterSensorEvent(SensorCallbackFunction listeningFunc, object obj)
		{
			unsafe
			{
				if (sensorListenerList.Count == 0)
				{
					var sensorListeningDelegate = new SensorListeningDelegate(sensorListeningFunction);
					if (obj != null)
					{
						sensorListenerContextHandle = GCHandle.Alloc(obj);
						registeredSensorListenerFunctionId = ((!_isWin)
							? LinuxInterop.tdRegisterSensorEvent(sensorListeningDelegate,
								(void*) GCHandle.ToIntPtr(sensorListenerContextHandle))
							: WinInterop.tdRegisterSensorEvent(sensorListeningDelegate,
								(void*) GCHandle.ToIntPtr(sensorListenerContextHandle)));
					}
					else
					{
						registeredSensorListenerFunctionId = ((!_isWin)
							? LinuxInterop.tdRegisterSensorEvent(sensorListeningDelegate, null)
							: WinInterop.tdRegisterSensorEvent(sensorListeningDelegate, null));
					}
					if (!callbackFunctionReferenceList.ContainsKey(registeredSensorListenerFunctionId))
					{
						callbackFunctionReferenceList.Remove(registeredSensorListenerFunctionId);
					}
					callbackFunctionReferenceList.Add(registeredSensorListenerFunctionId, sensorListeningDelegate);
				}
				this.lastEventID++;
				int lastEventID = this.lastEventID;
				sensorListenerList.Add(lastEventID, listeningFunc);
				return lastEventID;
			}
		}

		public bool RemoveDevice(int deviceId)
		{
			return (!_isWin)
				? LinuxInterop.tdRemoveDevice(deviceId)
				: WinInterop.tdRemoveDevice(deviceId);
		}

		public int SendRawCommand(string command, int reserved)
		{
			unsafe
			{
				char* ptr = command.ToCharPointer();
				int result = (!_isWin)
					? LinuxInterop.tdSendRawCommand(ptr, reserved)
					: WinInterop.tdSendRawCommand(ptr, reserved);
				Marshal.FreeHGlobal((IntPtr) ptr);
				return result;
			}
		}

		public bool SetDeviceParameter(int deviceId, string name, string value)
		{
			unsafe
			{
				char* ptr = name.ToCharPointer();
				char* value2 = value.ToCharPointer();
				bool result = (!_isWin)
					? LinuxInterop.tdSetDeviceParameter(deviceId, ptr, value2)
					: WinInterop.tdSetDeviceParameter(deviceId, ptr, value2);
				Marshal.FreeHGlobal((IntPtr) ptr);
				Marshal.FreeHGlobal((IntPtr) value2);
				Thread.Sleep(0); //??
				return result;
			}
		}

		public bool SetModel(int deviceId, string model)
		{
			unsafe
			{
				char* ptr = model.ToCharPointer();
				bool result = (!_isWin) ? LinuxInterop.tdSetModel(deviceId, ptr) : WinInterop.tdSetModel(deviceId, ptr);
				Marshal.FreeHGlobal((IntPtr) ptr);
				Thread.Sleep(0);
				return result;
			}
		}

		public bool SetName(int deviceId, string name)
		{
			unsafe
			{
				char* ptr = name.ToCharPointer();
				bool result = (!_isWin) ? LinuxInterop.tdSetName(deviceId, ptr) : WinInterop.tdSetName(deviceId, ptr);
				Marshal.FreeHGlobal((IntPtr) ptr);
				Thread.Sleep(0);
				return result;
			}
		}

		public bool SetProtocol(int deviceId, string protocol)
		{
			unsafe
			{
				char* ptr = protocol.ToCharPointer();
				bool result = (!_isWin) ? LinuxInterop.tdSetProtocol(deviceId, ptr) : WinInterop.tdSetProtocol(deviceId, ptr);
				Marshal.FreeHGlobal((IntPtr) ptr);
				Thread.Sleep(0);
				return result;
			}
		}

		public int TurnOn(int deviceId)
		{
			return (!_isWin)
				? LinuxInterop.tdTurnOn(deviceId)
				: WinInterop.tdTurnOn(deviceId);
		}

		public int TurnOff(int deviceId)
		{
			return (!_isWin)
				? LinuxInterop.tdTurnOff(deviceId)
				: WinInterop.tdTurnOff(deviceId);
		}


		//Helpers
		private unsafe int eventFunction(int deviceId, int method, char* data, int callbackId, void* context)
		{
			string data2 = "Invalid data from Telldus API.";
			UnmanagedException ex = null;
			try
			{
				data2 = AsString(data, false);
			}
			catch (Exception innerException)
			{
				ex = new UnmanagedException("GetString failed during callback from Telldus API.", innerException);
			}
			foreach (EventCallbackFunction current in eventList.Values)
			{
				if (context != null)
				{
					current(deviceId, method, data2, callbackId, GCHandle.FromIntPtr((IntPtr) context).Target, ex);
				}
				else
				{
					current(deviceId, method, data2, callbackId, null, ex);
				}
			}
			return 0;
		}

		private unsafe int deviceEventFunction(int deviceId, int changeEvent, int changeType, int callbackId, void* context)
		{
			foreach (DeviceChangeEventCallbackFunction current in deviceChangeEventList.Values)
			{
				if (context != null)
				{
					current(deviceId, changeEvent, changeType, callbackId, GCHandle.FromIntPtr((IntPtr) context).Target, null);
				}
				else
				{
					current(deviceId, changeEvent, changeType, callbackId, null, null);
				}
			}
			return 0;
		}

		private unsafe int rawListeningFunction(char* data, int controllerId, int callbackId, void* context)
		{
			UnmanagedException ex = null;
			string data2;
			try
			{
				data2 = AsString(data, false);
			}
			catch (Exception innerException)
			{
				data2 = "error;TelldusAPIError:";
				ex = new UnmanagedException("GetString failed during callback from Telldus API.", innerException);
			}
			foreach (RawListeningCallbackFunction current in rawListenerList.Values)
			{
				if (context != null)
				{
					current(data2, controllerId, callbackId, GCHandle.FromIntPtr((IntPtr) context).Target, ex);
				}
				else
				{
					current(data2, controllerId, callbackId, null, ex);
				}
			}
			return 0;
		}

		private unsafe int sensorListeningFunction(char* protocol, char* model, int id, int dataType, char* value,
			int timestamp, int callbackId, void* context)
		{
			string protocol2 = "Invalid data from Telldus API.";
			string model2 = "Invalid data from Telldus API.";
			string val = "Invalid data from Telldus API.";
			UnmanagedException ex = null;
			try
			{
				protocol2 = AsString(protocol, false);
				model2 = AsString(model, false);
				val = AsString(value, false);
			}
			catch (Exception innerException)
			{
				ex = new UnmanagedException("GetString failed during callback from Telldus API.", innerException);
			}
			foreach (SensorCallbackFunction current in sensorListenerList.Values)
			{
				if (context != null)
				{
					current(protocol2, model2, id, dataType, val, timestamp, callbackId, GCHandle.FromIntPtr((IntPtr) context).Target,
						ex);
				}
				else
				{
					current(protocol2, model2, id, dataType, val, timestamp, callbackId, null, ex);
				}
			}
			return 0;
		}

		public void CleanUp(bool closeAll)
		{
            try {
    			if (registeredEventFunctionId != -1)
    			{
    				if (_isWin)
    				{
    					WinInterop.tdUnregisterCallback(registeredEventFunctionId);
    				}
    				else
    				{
    					LinuxInterop.tdUnregisterCallback(registeredEventFunctionId);
    				}
    				registeredEventFunctionId = -1;
    			}
    			if (registeredDeviceChangeEventFunctionId != -1)
    			{
    				if (_isWin)
    				{
    					WinInterop.tdUnregisterCallback(registeredDeviceChangeEventFunctionId);
    				}
    				else
    				{
    					LinuxInterop.tdUnregisterCallback(registeredDeviceChangeEventFunctionId);
    				}
    				registeredDeviceChangeEventFunctionId = -1;
    			}
    			if (registeredRawListenerFunctionId != -1)
    			{
    				if (_isWin)
    				{
    					WinInterop.tdUnregisterCallback(registeredRawListenerFunctionId);
    				}
    				else
    				{
    					LinuxInterop.tdUnregisterCallback(registeredRawListenerFunctionId);
    				}
    				registeredRawListenerFunctionId = -1;
    			}
    			if (registeredSensorListenerFunctionId != -1)
    			{
    				if (_isWin)
    				{
    					WinInterop.tdUnregisterCallback(registeredSensorListenerFunctionId);
    				}
    				else
    				{
    					LinuxInterop.tdUnregisterCallback(registeredSensorListenerFunctionId);
    				}
    				registeredSensorListenerFunctionId = -1;
    			}
    			eventList.Clear();
    			deviceChangeEventList.Clear();
    			rawListenerList.Clear();
    			sensorListenerList.Clear();
    			if (closeAll)
    			{
    				Close();
    			}
    			if (eventContextHandle.IsAllocated)
    			{
    				eventContextHandle.Free();
    			}
    			if (deviceChangeEventContextHandle.IsAllocated)
    			{
    				deviceChangeEventContextHandle.Free();
    			}
    			if (rawListenerContextHandle.IsAllocated)
    			{
    				rawListenerContextHandle.Free();
    			}
    			if (sensorListenerContextHandle.IsAllocated)
    			{
    				sensorListenerContextHandle.Free();
    			}
            }catch(Exception e){
                Console.WriteLine("Error during cleanup:" + e.ToString());
            }
		}

		private unsafe string AsString(char* input, bool release = true)
		{
			string text = Encoding.UTF8.GetString(Encoding.Unicode.GetBytes(new string(input)));
			if (text.Contains('\0'))
			{
				text = text.Substring(0, text.IndexOf('\0'));
			}
			if (release)
			{
				if (_isWin)
				{
					WinInterop.tdReleaseString(input);
				}
				else
				{
					LinuxInterop.tdReleaseString(input);
				}
			}
			return text;
		}

        public void SetConnected(bool connected)
        {
            this._isInitialized = connected;
        }

		public void Dispose()
		{
			Dispose(true);
			//GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
			}

			// Free unmanaged 
			CleanUp(true);
		}
    }
}