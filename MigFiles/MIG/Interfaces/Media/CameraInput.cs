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
using System.Text;

using System.Runtime.InteropServices;
using MIG.Interfaces.HomeAutomation.Commons;

namespace MIG.Interfaces.Media
{



    public class CameraInput : MIGInterface
    {

        #region Implemented MIG Commands
        // typesafe enum
        public sealed class Command : GatewayCommand
        {

            public static Dictionary<int, string> CommandsList = new Dictionary<int, string>()
            {
                {101, "Camera.GetPicture"},
                {102, "Camera.GetLuminance"},
                {202, "Camera.SetDevice"}
            };

            // <context>.<command> enum   -   eg. Control.On where <context> :== "Control" and <command> :== "On"
            public static readonly Command CAMERA_GETPICTURE = new Command(101);
            public static readonly Command CAMERA_GETLUMINANCE = new Command(102);
            public static readonly Command CAMERA_SETDEVICE = new Command(202);

            private readonly String name;
            private readonly int value;

            private Command(int value)
            {
                this.name = CommandsList[value];
                this.value = value;
            }

            public Dictionary<int, string> ListCommands()
            {
                return Command.CommandsList;
            }

            public int Value
            {
                get { return this.value; }
            }

            public override String ToString()
            {
                return name;
            }

            public static implicit operator String(Command a)
            {
                return a.ToString();
            }

            public static explicit operator Command(int idx)
            {
                return new Command(idx);
            }

            public static explicit operator Command(string str)
            {
                if (CommandsList.ContainsValue(str))
                {
                    var cmd = from c in CommandsList where c.Value == str select c.Key;
                    return new Command(cmd.First());
                }
                else
                {
                    throw new InvalidCastException();
                }
            }
            public static bool operator ==(Command a, Command b)
            {
                return a.value == b.value;
            }
            public static bool operator !=(Command a, Command b)
            {
                return a.value != b.value;
            }
        }
        #endregion


        public struct PictureBuffer
        {
            public int Size;
            public IntPtr Data;
        }

        public class CameraCaptureV4LInterop
        {
            #region Managed to Unmanaged Interop

            [DllImport("CameraCaptureV4L.so", EntryPoint = "TakePicture")]
            public static extern PictureBuffer TakePicture(string device, uint width, uint height, uint jpegQuantity);
            [DllImport("CameraCaptureV4L.so", EntryPoint = "GetFrame")]
            public static extern PictureBuffer GetFrame(IntPtr source);
            [DllImport("CameraCaptureV4L.so", EntryPoint = "OpenCameraStream")]
            public static extern IntPtr OpenCameraStream(string device, uint width, uint height, uint fps);
            [DllImport("CameraCaptureV4L.so", EntryPoint = "CloseCameraStream")]
            public static extern void CloseCameraStream(IntPtr source);

            #endregion
        }

        public class CameraConfiguration
        {
            public string Device = "/dev/video0";
            public uint Width = 320;
            public uint Height = 240;
            public uint Fps = 2;
        }

        private IntPtr cameraSource = IntPtr.Zero;
        private CameraConfiguration configuration = new CameraConfiguration();
        private object readPictureLock = new object();

        #region public members

        public CameraConfiguration GetConfiguration()
        {
            return configuration;
        }

        public void SetConfiguration(string device, uint width, uint height, uint fps)
        {
            configuration.Device = device;
            configuration.Width = width;
            configuration.Height = height;
            configuration.Fps = fps;
        }

        #endregion

        #region MIG Interface members

        public event Action<InterfaceModulesChangedAction> InterfaceModulesChangedAction;
        public event Action<InterfacePropertyChangedAction> InterfacePropertyChangedAction;

        /// <summary>
        /// Gets the domain.
        /// ** Do not modify this function. **
        /// </summary>
        /// <value>
        /// The domain.
        /// </value>
        public string Domain
        {
            get
            {
                string domain = this.GetType().Namespace.ToString();
                domain = domain.Substring(domain.LastIndexOf(".") + 1) + "." + this.GetType().Name.ToString();
                return domain;
            }
        }
        
        public bool IsEnabled { get; set; }

        public List<MIGServiceConfiguration.Interface.Option> Options { get; set; }

        public List<InterfaceModule> GetModules()
        {
            List<InterfaceModule> modules = new List<InterfaceModule>();

            InterfaceModule module = new InterfaceModule();
            module.Domain = this.Domain;
            module.Address = "AV0";
            module.Description = "Video 4 Linux Video Input";
            module.ModuleType = MIG.ModuleTypes.Sensor;
            modules.Add(module);

            return modules;
        }

        /// <summary>
        /// Connect to the automation interface/controller device.
        /// </summary>
        public bool Connect()
        {
            if (cameraSource != IntPtr.Zero)
            {
                Disconnect();
            }
            if (this.GetOption("Configuration") != null && !string.IsNullOrEmpty(this.GetOption("Configuration").Value))
            {
                var config = this.GetOption("Configuration").Value.Split(',');
                SetConfiguration(config[0], uint.Parse(config[1]), uint.Parse(config[2]), uint.Parse(config[3]));
            }
            cameraSource = CameraCaptureV4LInterop.OpenCameraStream(configuration.Device, configuration.Width, configuration.Height, configuration.Fps);
            if (InterfaceModulesChangedAction != null) InterfaceModulesChangedAction(new InterfaceModulesChangedAction(){ Domain = this.Domain });
            //
            InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                Domain = this.Domain,
                SourceId = "AV0",
                SourceType = "Camera Input",
                Path = "Widget.DisplayModule",
                Value = "homegenie/generic/camerainput"
            });
            //
            return (cameraSource != IntPtr.Zero);
        }
        /// <summary>
        /// Disconnect the automation interface/controller device.
        /// </summary>
        public void Disconnect()
        {
            if (cameraSource != IntPtr.Zero)
            {
                CameraCaptureV4LInterop.CloseCameraStream(cameraSource);
                cameraSource = IntPtr.Zero;
            }
        }
        /// <summary>
        /// Gets a value indicating whether the interface/controller device is connected or not.
        /// </summary>
        /// <value>
        /// <c>true</c> if it is connected; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnected
        {
            get { return (cameraSource != IntPtr.Zero); }
        }
        /// <summary>
        /// Returns true if the device has been found in the system
        /// </summary>
        /// <returns></returns>
        public bool IsDevicePresent()
        {
            // eg. check against libusb for device presence by vendorId and productId
            return true;
        }

        public object InterfaceControl(MIGInterfaceCommand request)
        {
            request.Response = ""; //default success value
            //
            if (request.Command == Command.CAMERA_GETPICTURE)
            {
                // get picture from camera <nodeid>
                // TODO: there is actually only single camera support 
                if (cameraSource != IntPtr.Zero)
                {
                    lock (readPictureLock)
                    {
                        var pictureBuffer = CameraCaptureV4LInterop.GetFrame(cameraSource);
                        var data = new byte[pictureBuffer.Size];
                        Marshal.Copy(pictureBuffer.Data, data, 0, pictureBuffer.Size);
                        return data;
                    }
                }
            }
            else if (request.Command == Command.CAMERA_GETLUMINANCE)
            {
                // TODO: ....
            }
            else if (request.Command == Command.CAMERA_SETDEVICE)
            {
                this.GetOption("Configuration").Value = request.GetOption(0) + "," + request.GetOption(1) + "," + request.GetOption(2) + "," + request.GetOption(3);
                Connect();
            }
            //
            return request.Response;
        }

        #endregion

    }
}
