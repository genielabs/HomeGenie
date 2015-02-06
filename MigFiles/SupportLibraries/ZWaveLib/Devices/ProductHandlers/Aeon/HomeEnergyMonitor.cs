using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZWaveLib.Devices.ProductHandlers.Generic;

namespace ZWaveLib.Devices.ProductHandlers.Aeon
{
    /// <summary>
    /// PROBLEM: Meter and MultiLevel require different command classes. We use new generic MeterSensorCombo to handle both.
    /// </summary>
    public class HomeEnergyMonitor : MeterSensorCombo
    {

        public override bool CanHandleProduct(ManufacturerSpecific productspecs)
        {
            // TODO: Support HEM v2 also.
            return (productspecs.ManufacturerId == "0086" && productspecs.TypeId == "0002" && productspecs.ProductId == "0009");
        }


    }
}
