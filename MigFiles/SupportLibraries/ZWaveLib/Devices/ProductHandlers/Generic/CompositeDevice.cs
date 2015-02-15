using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZWaveLib.Devices.ProductHandlers.Generic
{

    /// <summary>
    /// Allows us to support devices that handle multiple generic device types.\
    /// 
    /// NOTE: This is a temporary fix until message parsing components are re-designed. 
    /// Message parsing depends more on command classes, etc, then generic device type. We should break out the parsing into commandclass classes 
    /// that handle parsing of any message part of that class.
    /// 
    /// For example, COMMAND_CLASS_METER would have it's own parsing class. This class would handle all related report types. The Meter.cs IZWaveDeviceHandler 
    /// would have very little parsing logic contained inside. 
    /// 
    /// </summary>
    public class CompositeDevice : IZWaveDeviceHandler
    {
        protected ZWaveNode nodeHost;

        private List<IZWaveDeviceHandler> _genericHandlers = new List<IZWaveDeviceHandler>();

        /// <summary>
        /// Adds a generic handler to the internal list.
        /// </summary>
        /// <param name="handler"></param>
        public void AddGenericHandler(IZWaveDeviceHandler handler)
        {
            _genericHandlers.Add(handler);
        }

        public void SetNodeHost(ZWaveNode node)
        {
            nodeHost = node;
            foreach (IZWaveDeviceHandler handler in _genericHandlers)
            {
                handler.SetNodeHost(node);
            }
        }

        public virtual bool CanHandleProduct(ManufacturerSpecific productspecs)
        {
            return false; // generic types must return false here
        }

        public virtual bool HandleRawMessageRequest(byte[] message)
        {
            bool handled = false;
            foreach (IZWaveDeviceHandler handler in _genericHandlers)
            {
                handled = handler.HandleRawMessageRequest(message);
                HandledStatusConsoleWriteHelper("HandleRawMessageRequest", handler, handled);
                if (handled)
                {
                    return true;
                }
            }
            return false;
        }

        public virtual bool HandleBasicReport(byte[] message)
        {
            bool handled = false;
            foreach (IZWaveDeviceHandler handler in _genericHandlers)
            {
                handled = handler.HandleBasicReport(message);
                HandledStatusConsoleWriteHelper("HandleBasicReport", handler, handled);
                if (handled)
                {
                    return true;
                }
            }
            return false;
        }

        public virtual bool HandleMultiInstanceReport(byte[] message)
        {
            bool handled = false;
            foreach (IZWaveDeviceHandler handler in _genericHandlers)
            {
                handled = handler.HandleMultiInstanceReport(message);
                HandledStatusConsoleWriteHelper("HandleMultiInstanceReport", handler, handled);
                if (handled)
                {
                    return true;
                }
            }
            return false;
        }

        private static void HandledStatusConsoleWriteHelper(string reportType, IZWaveDeviceHandler handler, bool handled) {
            // Comment out line below when ready to stop debugging to console.
            Console.WriteLine("CompositeDevice: {0} result for {1}... {2}",reportType, handler.GetType(), (handled ? "Handled!" : "NOT handled..."));
        }

    }
}
