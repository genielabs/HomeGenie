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
using System.Collections.Generic;
using System.Linq;

using MIG;

namespace HomeGenie
{
    public static class ProgramDynamicApi
    {
        private static readonly Dictionary<string, Func<object, object>> DynamicApi = new Dictionary<string, Func<object, object>>();
        private static readonly object DynamicApiLock = new object();

        public static Func<object, object> Find(string request)
        {
            Func<object, object> handler = null;
            if (DynamicApi.ContainsKey(request))
            {
                handler = DynamicApi[request];
            }
            return handler;
        }
        public static Func<object, object> FindMatching(string request)
        {
            string matchingPath = "";
            for (int i = 0; i < DynamicApi.Keys.Count; i++)
            {
                string apiPath = null;
                try
                {
                    apiPath = DynamicApi.Keys.ElementAt(i);
                }
                catch
                {
                    // ignored
                }
                if (apiPath != null && request.StartsWith(apiPath) && matchingPath.Length < apiPath.Length)
                {
                    matchingPath = apiPath;
                }
            }
            return matchingPath.Length > 0 ? DynamicApi[matchingPath] : null;
        }
        public static void Register(string request, Func<object, object> handlerfn)
        {
            lock (DynamicApiLock)
            {
                DynamicApi[request] = handlerfn;
            }
        }
        public static void UnRegister(string request)
        {
            if (DynamicApi.ContainsKey(request))
            {
                lock (DynamicApiLock)
                {
                    DynamicApi.Remove(request);
                }
            }
        }

        public static object TryApiCall(MigInterfaceCommand command)
        {
            // Dynamic Interface API
            var registeredApi = command.Domain + "/" + command.Address + "/" + command.Command;
            var handler = Find(registeredApi);
            if (handler != null)
            {
                // explicit command API handlers registered in the form <domain>/<address>/<command>
                // receives only the remaining part of the request after the <command>
                var args = command.OriginalRequest.Substring(registeredApi.Length).Trim('/');
                return handler(args);
            }

            // else
            handler = FindMatching(command.OriginalRequest.Trim('/'));
            if (handler == null) return null;

            // other command API handlers
            if (command.Data == null || (command.Data is byte[] && (command.Data as byte[]).Length == 0))
            {
                // receives the full request as string if there is no `request.Data` payload
                return handler(command.OriginalRequest.Trim('/'));
            }

            // receives the original MigInterfaceCommand if `request.Data` actually holds some data
            // TODO: this might be be the only entry point in future releases (line #98 and #87 cases will be deprecated)
            return handler(command);
        }

    }
}
