using System;
using System.Collections.Generic;
using System.Text;

namespace ZWaveLib.Devices.ProductHandlers.ZwaveME
{
    public class ZME_06436MotorControl : ProductHandlers.Generic.Switch
    {

        public override bool CanHandleProduct(ManufacturerSpecific productspecs)
        {
            return (productspecs.ManufacturerId == "0115" && productspecs.TypeId == "1000" && productspecs.ProductId == "0003");
        }

    }
}
