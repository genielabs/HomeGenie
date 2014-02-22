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

namespace MIG.Interfaces
{
    public static class DynamicInterfaceAPI
    {
        private static Dictionary<string, Func<object, object>> _dynamicapi = new Dictionary<string, Func<object, object>>();

        public static Func<object, object> Find(string request)
        {
            Func<object, object> handler = null;
            if (_dynamicapi.ContainsKey(request))
            {
                handler = _dynamicapi[request];
            }
            return handler;
        }
        public static Func<object, object> FindMatching(string request)
        {
            Func<object, object> handler = null;
            for (int i = 0; i < _dynamicapi.Keys.Count; i++)
            {
                if (request.StartsWith(_dynamicapi.Keys.ElementAt(i)))
                {
                    handler = _dynamicapi[_dynamicapi.Keys.ElementAt(i)];
                    break;
                }
            }
            return handler;
        }
        public static void Register(string request, Func<object, object> handlerfn)
        {
            if (_dynamicapi.ContainsKey(request))
            {
                _dynamicapi[request] = handlerfn;
            }
            else
            {
                _dynamicapi.Add(request, handlerfn);
            }
        }
        public static void UnRegister(string request)
        {
            if (_dynamicapi.ContainsKey(request))
            {
                _dynamicapi.Remove(request);
            }
        }

    }
}
