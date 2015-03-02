using MIG.Interfaces.HomeAutomation.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TelldusLib;

namespace MIG.Interfaces.HomeAutomation
{
    public class Tellstick : MIGInterface
    {
        TellstickController controller;
        List<InterfaceModule> interfaceModules = new List<InterfaceModule>();
        public Tellstick()
        {
            controller = new TellstickController();
        }

        public string Domain
        {
            get
            {
                string domain = this.GetType().Namespace.ToString();
                domain = domain.Substring(domain.LastIndexOf(".") + 1) + "." + this.GetType().Name.ToString();
                return domain;
            }
        }

        public List<InterfaceModule> GetModules()
        {
            return interfaceModules;
        }

        private ModuleTypes GetDeviceType(string protocol)
        {
            if (protocol.IndexOf("dimmer") > -1)
                return ModuleTypes.Dimmer;
            if (protocol.IndexOf("switch") > -1)
                return ModuleTypes.Switch;
            return ModuleTypes.Generic;
        }

        public List<MIGServiceConfiguration.Interface.Option> Options
        {
            get;
            set;
        }

        public event Action<InterfacePropertyChangedAction> InterfacePropertyChangedAction;

        public event Action<InterfaceModulesChangedAction> InterfaceModulesChangedAction;

        public object InterfaceControl(MIGInterfaceCommand command)
        {
            string returnValue = "";
            bool raisePropertyChanged = false;
            string parameterPath = "Status.Level";
            string raiseParameter = "";
            switch (command.Command)
            {
                case "Control.On":
                    controller.TurnOn(int.Parse(command.NodeId));
                    raisePropertyChanged = true;
                    raiseParameter = "1";
                    break;
                case "Control.Off":
                    raisePropertyChanged = true;
                    controller.TurnOff(int.Parse(command.NodeId));
                    raiseParameter = "0";
                    break;
                case "Control.Level":
                    raisePropertyChanged = true;
                    raiseParameter = (double.Parse(command.GetOption(0)) / 100).ToString();
                    controller.Dim(int.Parse(command.NodeId), (int)Math.Round(double.Parse(command.GetOption(0))));
                    break;
                default:
                    Console.WriteLine("TS:" + command.Command + " | " + command.NodeId);
                    break;
            }

            if (raisePropertyChanged)
            {
                try
                {
                    //ZWaveNode node = _controller.GetDevice ((byte)int.Parse (nodeid));
                    InterfacePropertyChangedAction(new InterfacePropertyChangedAction()
                    {
                        Domain = this.Domain,
                        SourceId = command.NodeId,
                        SourceType = "Tellstick Node",
                        Path = parameterPath,
                        Value = raiseParameter
                    });
                }
                catch
                {
                }
            }
            return returnValue;
        }

        public bool IsConnected
        {
            get { return controller.IsConnected; }
        }

        public bool Connect()
        {
            controller.Init();
            var n = controller.GetNumberOfDevices();
            controller.SetConnected(n >= 0);

            for (var i = 0; i < n; i++)
            {
                var id = controller.GetDeviceId(i);
                interfaceModules.Add(new InterfaceModule
                {
                    Domain = Domain,
                    Address = id.ToString(),
                    Description = controller.GetName(id),
                    ModuleType = GetDeviceType(controller.GetProtocol(id))
                });
                var lastCommand = controller.LastSentCommand(id, 0);
                if (lastCommand > 0)
                {
                    InterfacePropertyChangedAction(new InterfacePropertyChangedAction()
                    {
                        Domain = this.Domain,
                        SourceId = id.ToString(),
                        SourceType = "Tellstick Node",
                        Path = ModuleParameters.MODPAR_STATUS_LEVEL,
                        Value = lastCommand
                    });
                }
            }

            controller.RegisterDeviceEvent(OnDeviceUpdated, null);
            controller.RegisterSensorEvent(SensorUpdated, null);

            return true;
        }

        private int OnDeviceUpdated(int deviceId, int method, string data, int callbackId, object obj, UnmanagedException ex)
        {
            var path = ModuleParameters.MODPAR_STATUS_LEVEL;
            int value = 0;
            if (method == (int)TelldusLib.Command.TURNON)
            {
                path = ModuleParameters.MODPAR_STATUS_LEVEL;
                value = 1;
            }
            else if (method == (int)TelldusLib.Command.TURNOFF)
            {
                path = ModuleParameters.MODPAR_STATUS_LEVEL;
                value = 0;
            }

            var module = interfaceModules.FirstOrDefault(i => i.Address == deviceId.ToString());

            InterfacePropertyChangedAction(new InterfacePropertyChangedAction()
            {
                Domain = Domain,
                SourceId = module.Address,
                SourceType = "Tellstick Sensor",
                Path = path,
                Value = value
            });

            return 1;
        }

        private int SensorUpdated(
            string protocol, string model, int id, int dataType, string val, int timestamp, int callbackId, object obj,
            UnmanagedException ex)
        {
            Console.WriteLine("TS: " + protocol + ", " + model + ", " + id + ", " + dataType + ", " + val + ", " + timestamp + ", " + callbackId);
            var module = interfaceModules.FirstOrDefault(i => i.Address == id.ToString());
            if (module == null)
            {

                module = new InterfaceModule
                {
                    Domain = Domain,
                    Address = id.ToString(),
                    Description = model + " - " + protocol,
                    ModuleType = ModuleTypes.Sensor
                };
                interfaceModules.Add(module);

                InterfaceModulesChangedAction(new InterfaceModulesChangedAction
                {
                    Domain = Domain
                });
            }

            var path = ModuleParameters.MODPAR_STATUS_LEVEL;
            if (dataType == (int)TelldusLib.DataType.TEMPERATURE)
                path = ModuleParameters.MODPAR_SENSOR_TEMPERATURE;
            else if (dataType == (int)TelldusLib.DataType.HUMIDITY)
                path = ModuleParameters.MODPAR_SENSOR_HUMIDITY;

            InterfacePropertyChangedAction(new InterfacePropertyChangedAction()
            {
                Domain = Domain,
                SourceId = module.Address,
                SourceType = "Tellstick Sensor",
                Path = path,
                Value = val
            });

            //Sensor.Temperature
            //MODPAR_SENSOR_TEMPERATURE

            return 1;
        }

        public void Disconnect()
        {
            controller.Close();
        }

        public bool IsDevicePresent()
        {
            return true;
        }
    }
}
