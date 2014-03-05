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

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: http://homegenie.it
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;

using System.Configuration;
using System.Configuration.Install;
using System.ComponentModel;
//using System.Reflection;
using System.ServiceModel;
using System.ServiceProcess;

using HomeGenie.Service;

namespace HomeGenie.WCF
{

    public class LogEntry
    {
        public string Domain;
        public string Source;
        public string Description;
        public string Property;
        public string Value;
    }

    // Define a service contract.
    [ServiceContract(Namespace = "http://HomeGenie.WCF", CallbackContract = typeof(IManagerCallbacks))]
    public interface IManager
    {
        [OperationContract]
        bool Subscribe();
        [OperationContract]
        bool Unsubscribe();
        [OperationContract]
        int GetHttpServicePort();
        [OperationContract]
        void RaiseOnEventLogged(LogEntry logentry);
    }
    public interface IManagerCallbacks
    {
        [OperationContract(IsOneWay = true)]
        void OnEventLogged(LogEntry logentry, DateTime timestamp);
    }
    // Implement the ICalculator service contract in a service class.
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Single, InstanceContextMode = InstanceContextMode.Single)]
    public class ManagerService : IManager
    {
        private static readonly List<IManagerCallbacks> subscribers = new List<IManagerCallbacks>();

        public bool Subscribe()
        {
            try
            {
                IManagerCallbacks callback = OperationContext.Current.GetCallbackChannel<IManagerCallbacks>();
                if (!subscribers.Contains(callback))
                    subscribers.Add(callback);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Unsubscribe()
        {
            try
            {
                IManagerCallbacks callback = OperationContext.Current.GetCallbackChannel<IManagerCallbacks>();
                if (subscribers.Contains(callback))
                    subscribers.Remove(callback);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private HomeGenie.Service.HomeGenieService homegenie;
        internal void SetHomeGenieHost(HomeGenie.Service.HomeGenieService hg)
        {
            homegenie = hg;
        }

        public int GetHttpServicePort()
        {
            return homegenie.GetHttpServicePort();
        }

        public void RaiseOnEventLogged(LogEntry logMessage)
        {
            var t = new Thread(() =>
            {
                Thread.Sleep(100);
                subscribers.ForEach(delegate(IManagerCallbacks callback)
                {
                    try
                    {
                        if (((ICommunicationObject)callback).State == CommunicationState.Opened)
                        {
                            callback.OnEventLogged(logMessage, DateTime.Now);
                        }
                        else
                        {
                            subscribers.Remove(callback);
                        }
                    }
                    catch { }
                });
            });
            t.Start();
        }

    }
}
