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

using HomeGenie.Data;
using HomeGenie.Service;

namespace HomeGenie.Automation.Scripting
{
    /// <summary>
    /// Events Helper class.\n
    /// Class instance accessor: **When**
    /// </summary>
    [Serializable]
    public class EventsHelper
    {
        HomeGenieService homegenie = null;
        int myProgramId = -1;

        public EventsHelper(HomeGenieService hg, int programId)
        {
            homegenie = hg;
            myProgramId = programId;
        }

        /// <summary>
        /// Call the specified <handler> after HomeGenie service started.
        /// </summary>
        /// <returns>EventsHelper</returns>
        /// <param name="handler">The handler function to call.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        ///     When.SystemStarted( () =>
        ///     {
        ///         Program.Say("HomeGenie is now ready!");
        ///         // returning false will prevent this event from being routed to other listeners
        ///         return false;
        ///     }); 
        /// </code></example>
        public EventsHelper SystemStarted(Func<bool> handler)
        {
            var program = homegenie.ProgramManager.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            program.Engine.SystemStarted = handler;
            return this;
        }
        
        /// <summary>
        /// Call the specified <handler> when HomeGenie service is stopping.
        /// </summary>
        /// <returns>EventsHelper</returns>
        /// <param name="handler">The handler function to call.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        ///     When.SystemStopping( () =>
        ///     {
        ///         Program.Say("See ya soon!");
        ///         // returning true will route this event to other listeners
        ///         return true;
        ///     }); 
        /// </code></example>
        public EventsHelper SystemStopping(Func<bool> handler)
        {
            var program = homegenie.ProgramManager.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            program.Engine.SystemStopping = handler;
            return this;
        }
        
        /// <summary>
        /// Call the specified <handler> when the program is beign stopped.
        /// </summary>
        /// <returns>EventsHelper</returns>
        /// <param name="handler">The handler function to call.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        ///     When.Stopping( () =>
        ///     {
        ///         Program.Say("Oh-oh! I'm quitting!");
        ///         // returning true will route this event to other listeners
        ///         return true;
        ///     }); 
        /// </code></example>
        public EventsHelper ProgramStopping(Func<bool> handler)
        {
            var program = homegenie.ProgramManager.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            program.Engine.Stopping = handler;
            return this;
        }

        /// <summary>
        /// Call the specified <handler> function when a parameter of a module changed.
        /// </summary>
        /// <returns>EventsHelper</returns>
        /// <param name="handler">The handler function to call.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        ///     When.ModuleParameterChange( (module, parameter) =>
        ///     {
        ///         if (module.Is("Kitchen Motion Sensor") && parameter.Is("Status.Level"))
        ///         {
        ///             // ...
        ///             return false;
        ///         }
        ///         return true;
        ///     }); 
        /// </code></example>
        /// <seealso cref="ModuleParameterIsChanging"/>
        public EventsHelper ModuleParameterChanged(Func<ModuleHelper, ModuleParameter, bool> handler)
        {
            var program = homegenie.ProgramManager.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            program.Engine.ModuleChangedHandler = handler;
            return this;
        }

        /// <summary>
        /// Call the specified <handler> function when a parameter of a module is changing.
        /// </summary>
        /// <returns>EventsHelper</returns>
        /// <param name="handler">The handler function to call.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        ///     When.ModuleParameterIsChanging( (module, parameter) =>
        ///     {
        ///         if (module.Is("Kitchen Motion Sensor") && parameter.Is("Status.Level"))
        ///         {
        ///             // ...
        ///             // stop event propagation
        ///             return false;
        ///         }
        ///         // continue event propagation
        ///         return true;
        ///     }); 
        /// </code></example>
        /// <seealso cref="ModuleParameterChanged"/>
        public EventsHelper ModuleParameterIsChanging(Func<ModuleHelper, ModuleParameter, bool> handler)
        {
            var program = homegenie.ProgramManager.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            program.Engine.ModuleIsChangingHandler = handler;
            return this;

        }

        /// <summary>
        /// Define a <handler> function to call when a web service call starting with <apiCall> is received.
        /// This is used to create and handle user-defined web service API methods.
        /// </summary>
        /// <returns>EventsHelper</returns>
        /// <param name="apiCall">API call.</param>
        /// <param name="handler">Handler.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        ///     When.WebServiceCallReceived( "Hello.World", (args) =>
        ///     {
        ///         var returnstring = "";
        ///         if (args == "Greet")
        ///         {
        ///             returnstring = "Hello HomeGenie World!";
        ///         }
        ///         return returnstring;
        ///     }); 
        /// </code>
        /// In the snippet above, if we wanted to create an "Hello World" program that respond to the custom API call:
        /// \n
        /// http://<hg_server_address>/api/Hello.World/Greet
        /// </example>
        public EventsHelper WebServiceCallReceived(string apiCall, Func<object, object> handler)
        {
            var program = homegenie.ProgramManager.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            program.Engine.registeredApiCalls.Add(apiCall);
            homegenie.ProgramManager.RegisterDynamicApi(apiCall, handler);
            return this;
        }

        public EventsHelper WebServiceCallReceived(string apiCall, Func<object, string> handler)
        {
            var program = homegenie.ProgramManager.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            program.Engine.registeredApiCalls.Add(apiCall);
            homegenie.ProgramManager.RegisterDynamicApi(apiCall, handler);
            return this;
        }


        //TODO: deprecate this
        [Obsolete("use 'ModuleParameterChanged' instead")]
        public EventsHelper ModuleParameterChange(Func<ModuleHelper, ModuleParameter, bool> handler)
        {
            return ModuleParameterChanged(handler);
        }

    }
}
