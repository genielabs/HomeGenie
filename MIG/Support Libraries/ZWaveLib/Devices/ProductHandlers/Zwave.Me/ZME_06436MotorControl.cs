using System;
using System.Collections.Generic;
using System.Text;

namespace ZWaveLib.Devices.ProductHandlers.ZwaveME
{
    class ZME_06436MotorControl : IZWaveDeviceHandler
    {
        ZWaveNode mynode = null;

        public void SetNodeHost(ZWaveNode node)
        {
            this.mynode = node;
        }

        public bool CanHandleProduct(ManufacturerSpecific productspecs)
        {
            return (productspecs.ManufacturerId == "0073" && productspecs.TypeId == "03E8" && productspecs.ProductId == "0003");
        }

        public bool HandleRawMessageRequest(byte[] message)
        {
            return false;
        }

        public bool HandleBasicReport(byte[] message)
        {
            return false;
        }

        public bool HandleMultiInstanceReport(byte[] message)
        {
            return false;
        }

    }
}
