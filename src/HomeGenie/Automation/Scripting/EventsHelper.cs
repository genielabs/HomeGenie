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
        /// Call the specified `handler` after HomeGenie service started.
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
        /// Call the specified `handler` when HomeGenie service is stopping.
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
        /// Call the specified `handler` when the program is beign stopped.
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
        /// Call the specified `handler` function when a parameter of a module changed.
        /// If either the `handler` returns false or changes the event value, the propagation will stop.
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
        /// Call the specified `handler` function when a parameter of a module is changing.
        /// If either the `handler` returns false or changes the event value, the propagation will stop.
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
        /// Define a `handler` function that will be called when a web service call starting with `apiCall` is received.
        /// Use this to create user-defined web service API methods.
        /// </summary>
        /// <returns>EventsHelper</returns>
        /// <param name="apiCall">API call.</param>
        /// <param name="handler">Handler.</param>
        /// <remarks />
        /// <example>
        /// API methods should respect the following format:
        /// <code>
        /// <domain>/<address>/<command>[/<option_0>[/.../<option_n>]]
        /// </code>
        /// For instance, a program that control Philips Hue lights will implement API methods like this:
        /// <code>
        ///     When.WebServiceCallReceived( "HomeAutomation.PhilipsHue", (args) =>
        ///     {
        ///         // handle the received request
        ///     });
        /// </code>
        ///
        /// So an API call to set a Philips Hue light with address *3* to *50%* can be done via HTTP GET
        /// <code>
        /// GET /api/HomeAutomation.PhilipsHue/3/Control.Level/50
        /// </code>
        /// or from a csharp program
        /// <code>
        /// var responseObject = Program.ApiCall("HomeAutomation.PhilipsHue/3/Control.Level/50");
        /// </code>
        /// When this call is received by the handler, the object `args` passed to it must be parsed using `Program.ParseApiCall` method, which will return an object containing the following fields
        /// <code>
        /// var request = Program.ParseApiCall(args);
        /// //  request -> {
        /// //      Domain,         // (string)
        /// //      Address,        // (string)
        /// //      Command,        // (string)
        /// //      Data,           // (object)
        /// //      OriginalRequest // (string)
        /// //  }
        /// </code>
        /// This object also provide a method `request.GetOption(<index>)` to get eventual options passed with this call.
        ///
        /// **Example**
        /// <code>
        ///     When.WebServiceCallReceived( "HomeAutomation.PhilipsHue", (args) =>
        ///     {
        ///         var request = Program.ParseApiCall(args);
        ///         // request.Domain          -> "HomeAutomtion.PhilipsHue"
        ///         // request.Address         -> 3
        ///         // request.Command         -> Control.Level
        ///         // request.GetOption(0)    -> 50
        ///         // request.Data            -> null (not used in this case)
        ///         // request.OriginalRequest -> "HomeAutomation.PhilipsHue/3/Control.Level/50"
        ///         if (request.Domain == "HomeAutomtion.PhilipsHue" && request.Command == "Control.Level")
        ///         {
        ///             var deviceAddress = request.Address;
        ///             var deviceLevel = request.GetOption(0); // the first option has index 0
        ///             // TODO: set dimming level of light with address 'deviceAddress' to 'dimmerLevel' %
        ///             return new ResponseText("OK");
        ///         }
        ///         return new ResponseText("ERROR");
        ///     });
        /// </code>
        /// </example>
        public EventsHelper WebServiceCallReceived(string apiCall, Func<object, object> handler)
        {
            var program = homegenie.ProgramManager.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            program.Engine.RegisterDynamicApi(apiCall, handler);
            return this;
        }

        public EventsHelper WebServiceCallReceived(string apiCall, Func<object, string> handler)
        {
            var program = homegenie.ProgramManager.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            program.Engine.RegisterDynamicApi(apiCall, handler);
            return this;
        }

    }
}
