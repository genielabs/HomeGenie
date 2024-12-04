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
 *     Project Homepage: https://homegenie.it
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
        /// Calls the specified `handler` after HomeGenie service started.
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
        /// Calls the specified `handler` when HomeGenie service is stopping.
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
        /// Calls the specified `handler` when the program is beign stopped.
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
        /// Calls the specified `handler` function when a parameter of a module changed.
        /// If either the `handler` returns false or changes the event value, the propagation will stop.
        /// </summary>
        /// <returns>EventsHelper</returns>
        /// <param name="handler">The handler function to call.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        ///     When.ModuleParameterChanged( (module, parameter) =>
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
        /// Calls the specified `handler` function when a parameter of a module is changing.
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

        [Obsolete("This method is deprecated, use Api.Handle(...) instead.")]
        public EventsHelper WebServiceCallReceived(string apiCall, Func<object, object> handler)
        {
            var program = homegenie.ProgramManager.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            program.Engine.RegisterDynamicApi(apiCall, handler);
            return this;
        }
        [Obsolete("This method is deprecated, use Api.Handle(...) instead.")]
        public EventsHelper WebServiceCallReceived(string apiCall, Func<object, string> handler)
        {
            var program = homegenie.ProgramManager.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            program.Engine.RegisterDynamicApi(apiCall, handler);
            return this;
        }

    }
}
