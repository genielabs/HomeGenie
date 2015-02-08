using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZWaveLib.Devices.ProductHandlers.Generic
{
    /// <summary>
    /// Allows us to support devices that handle BOTH of these device types.
    /// </summary>
    public class MeterSensorCombo : IZWaveDeviceHandler
    {
        protected ZWaveNode nodeHost;

        protected Meter MeterHandler;
        protected Sensor SensorHandler;


        public MeterSensorCombo()
        {

            MeterHandler = new Meter();
            SensorHandler = new Sensor();

        }

        public void SetNodeHost(ZWaveNode node)
        {
            nodeHost = node;
            MeterHandler.SetNodeHost(node);
            SensorHandler.SetNodeHost(node);
            // nodeHost.RaiseUpdateParameterEvent(nodeHost, 0, ParameterType.METER_WATT, 0); // Why is this here?
        }

        public virtual bool CanHandleProduct(ManufacturerSpecific productspecs)
        {
            return false; // generic types must return false here
        }

        public virtual bool HandleRawMessageRequest(byte[] message)
        {
            //Console.WriteLine("   >>> MeterSensorCombo.HandleRawMessageRequest \n");
            if (MeterHandler.HandleRawMessageRequest(message))
            {
                return true;
            }
            if (SensorHandler.HandleRawMessageRequest(message))
            {
                return true;
            }
            return false;
        }

        public virtual bool HandleBasicReport(byte[] message)
        {
            //Console.WriteLine("   >>> MeterSensorCombo.HandleBasicReport \n");
            if (MeterHandler.HandleBasicReport(message))
            {
                //Console.WriteLine("   >>> MeterSensorCombo.HandleMultiInstanceReport : Meter handled it!\n");
                return true;
            }
            if (SensorHandler.HandleBasicReport(message))
            {
                //Console.WriteLine("   >>> MeterSensorCombo.HandleMultiInstanceReport : Sensor handled it!\n");
                return true;
            }
            return false;
        }

        public virtual bool HandleMultiInstanceReport(byte[] message)
        {
            //Console.WriteLine("   >>> MeterSensorCombo.HandleMultiInstanceReport \n");
            if (MeterHandler.HandleMultiInstanceReport(message))
            {
                //Console.WriteLine("   >>> MeterSensorCombo.HandleMultiInstanceReport : Meter handled it!\n");
                return true;
            }
            if (SensorHandler.HandleMultiInstanceReport(message))
            {
                //Console.WriteLine("   >>> MeterSensorCombo.HandleMultiInstanceReport : Sensor handled it!\n");
                return true;
            }
            return false;
        }

    }
}
