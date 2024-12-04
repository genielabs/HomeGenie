using System;
using HomeGenie.Service;
using HomeGenie.Service.Constants;
using MIG;
using Newtonsoft.Json;
using WebSocketSharp;

namespace HomeGenie.Automation.Scripting
{
    /// <summary>
    /// API Helper class.\n
    /// Class instance accessor: **Api**
    /// </summary>
    [Serializable]
    public class ApiHelper
    {
        HomeGenieService homegenie = null;
        int myProgramId;

        public ApiHelper(HomeGenieService hg, int programId)
        {
            homegenie = hg;
            myProgramId = programId;
        }

        /// <summary>
        /// Defines a `handler` function that will be called when a web service call starting with `apiCall` is received.
        /// Use this to add user-defined web service API methods.
        /// </summary>
        /// <returns>ApiHelper</returns>
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
        ///     Api.Handle( "HomeAutomation.PhilipsHue", (args) =>
        ///     {
        ///         var request = Api.Parse(args);
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
        /// var responseObject = Api.Call("HomeAutomation.PhilipsHue/3/Control.Level/50");
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
        ///     Api.Handle( "HomeAutomation.PhilipsHue", (args) =>
        ///     {
        ///         var request = Api.Parse(args);
        ///         // request.Domain          -> "HomeAutomation.PhilipsHue"
        ///         // request.Address         -> 3
        ///         // request.Command         -> Control.Level
        ///         // request.GetOption(0)    -> 50
        ///         // request.Data            -> null (not used in this case)
        ///         // request.OriginalRequest -> "HomeAutomation.PhilipsHue/3/Control.Level/50"
        ///         if (request.Domain == "HomeAutomation.PhilipsHue" && request.Command == "Control.Level")
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
        public ApiHelper Handle(string apiCall, Func<object, object> handler)
        {
            var program = homegenie.ProgramManager.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            program.Engine.RegisterDynamicApi(apiCall, handler);
            return this;
        }
/*
        public ApiHelper Handle(string apiCall, Func<object, string> handler)
        {
            var program = homegenie.ProgramManager.Programs.Find(p => p.Address.ToString() == myProgramId.ToString());
            program.Engine.RegisterDynamicApi(apiCall, handler);
            return this;
        }
*/
        
        /// <summary>
        /// Parses the given (api call) string as a `MigInterfaceCommand` object.
        /// </summary>
        /// <returns>The mig command.</returns>
        /// <param name="apiCall">Api Command instance (MigInterfaceCommand) or string (eg. "HomeAutomation.X10/A5/Control.Level/50").</param>
        public MigInterfaceCommand Parse(object apiCall)
        {
            if (apiCall is MigInterfaceCommand)
            {
                return (MigInterfaceCommand)apiCall;
            }
            return new MigInterfaceCommand(apiCall.ToString());
        }

        /// <summary>
        /// Invokes an API command and gets the result.
        /// </summary>
        /// <returns>The API command response.</returns>
        /// <param name="apiCommand">Any MIG/APP API command without the `/api/` prefix.</param>
        /// <param name="data">Data object. (optional)</param>
        public object Call(string apiCommand, object data = null)
        {
            if (apiCommand.StartsWith("/api/"))
                apiCommand = apiCommand.Substring(5);
            var result = homegenie.InterfaceControl(new MigInterfaceCommand(apiCommand, data));
            // Try system API if not handled as MigInterfaceCommand
            if (result == null && (apiCommand.StartsWith($"{Domains.HomeAutomation_HomeGenie}/") || apiCommand.StartsWith($"{Domains.HomeAutomation_HomeGenie_Automation}/")))
            {
                string port = homegenie.GetHttpServicePort();
                NetHelper netHelper = new NetHelper(homegenie);
                netHelper
                    .WebService($"http://localhost:{port}/api/{apiCommand}")
                    .Put(JsonConvert.SerializeObject(data));
                var username = homegenie.SystemConfiguration.HomeGenie.Username;
                var password = homegenie.SystemConfiguration.HomeGenie.Password;
                if (!String.IsNullOrEmpty(username) && !String.IsNullOrEmpty(password))
                {
                    netHelper.WithCredentials(
                        username,
                        password
                    );
                }
                result = netHelper.GetData();
                if (result != null && result.ToString().IsNullOrEmpty())
                {
                    result = null;
                }
            }
            return result;
        }

    }
}