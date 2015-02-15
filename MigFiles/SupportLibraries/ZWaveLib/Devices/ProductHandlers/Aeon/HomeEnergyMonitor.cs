using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZWaveLib.Devices.ProductHandlers.Generic;

namespace ZWaveLib.Devices.ProductHandlers.Aeon
{
    /// <summary>
    /// Supports both v1 and v2 of HEM.
    /// 
    /// NOTE: Meter and MultiLevel require different command classes. We use new generic CompositeDevice to handle both.
    /// </summary>
    public class HomeEnergyMonitor : CompositeDevice
    {

        private static string HEMv1ProductId = "0009";
        private static string HEMv2ProductId = "001C";


        public HomeEnergyMonitor()
        {
            // This unit supports Meter and Sensor generic device classes.
            base.AddGenericHandler(new Meter());
            base.AddGenericHandler(new Sensor());
        }

        public override bool CanHandleProduct(ManufacturerSpecific productspecs)
        {
            // TODO: Support HEM v2 also.
            return (productspecs.ManufacturerId == "0086" && productspecs.TypeId == "0002" &&
                (productspecs.ProductId == HEMv1ProductId ||productspecs.ProductId == HEMv2ProductId) );
        }


    }
}
