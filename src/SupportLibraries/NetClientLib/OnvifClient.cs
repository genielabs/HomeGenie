/*
   Copyright 2012-2025 G-Labs (https://github.com/genielabs)

   This program is free software: you can redistribute it and/or modify
   it under the terms of the GNU Affero General Public License as
   published by the Free Software Foundation, either version 3 of the
   License, or (at your option) any later version.

   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU Affero General Public License for more details.

   You should have received a copy of the GNU Affero General Public License
   along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: https://homegenie.it
 */

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
