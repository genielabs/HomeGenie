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
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MIG.Interfaces.HomeAutomation
{
    partial class Insteon
    {
        private Task readerTask;
        private CancellationTokenSource cancellationTokenSource;

        private async Task Receive(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                insteonPlm.Receive();
                await Task.Delay(100); // wait 100 ms
            }
        }

        private bool StopListening()
        {
            if (this.readerTask == null)
            {
                return true;
            }

            this.cancellationTokenSource.Cancel();
            bool result = false;
            try
            {
                result = this.readerTask.Wait(2000);
            }
            catch (OperationCanceledException)
            {
                return true;
            }
            this.cancellationTokenSource.Dispose();
            this.cancellationTokenSource = null;
            this.readerTask = null;
            return result;
        }

        private void StartListening()
        {
            if (this.StopListening())
            {
                this.cancellationTokenSource = new CancellationTokenSource();
                this.readerTask = Task.Run(async () => await this.Receive(cancellationTokenSource.Token));
            }
        }

        private IDisposable SuspendListening()
        {
            return new ListeningSuspender(this);
        }

        private class ListeningSuspender: IDisposable
        {
            private readonly Insteon insteon;

            public ListeningSuspender(Insteon insteon)
            {
                this.insteon = insteon;
                if(!this.insteon.StopListening())
                {
                    throw new InvalidOperationException("Cannot suspend listener");
                }
            }

            void IDisposable.Dispose()
            {
                this.insteon.StartListening();
            }
        }
    }
}
