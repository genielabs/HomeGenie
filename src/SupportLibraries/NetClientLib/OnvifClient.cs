using System;
using System.Threading;
using OnvifDiscovery;
using OnvifDiscovery.Models;

namespace NetClientLib
{
    public class OnvifClient
    {
        private Discovery onvifDiscovery;
        private CancellationTokenSource cancellationTokenSource;

        public Action<DiscoveryDevice> OnOnvifDeviceDiscovered;

        public async void Start()
        {
            Stop();
            onvifDiscovery = new Discovery();
            cancellationTokenSource = new CancellationTokenSource();
            try
            {
                await onvifDiscovery.Discover(1, OnNewDevice, cancellationTokenSource.Token);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                // TODO: should report error
            }
        }

        public void Stop()
        {
            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
            }
            onvifDiscovery = null;
        }
        
        private void OnNewDevice (DiscoveryDevice device)
        {
            // New device discovered
            if (OnOnvifDeviceDiscovered != null)
            {
                OnOnvifDeviceDiscovered(device);
            }
            
        }
    }
}
