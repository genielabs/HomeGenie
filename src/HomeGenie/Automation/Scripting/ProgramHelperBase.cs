using System;
using System.Globalization;
using System.Threading;
using System.IO;
using System.Net;

using MIG;

using HomeGenie.Service;
using HomeGenie.Automation.Scripting;

namespace HomeGenie
{
    /// <summary>
    /// Program Helper Base class.\n
    /// Class instance accessor: **Program**
    /// </summary>
    public class ProgramHelperBase
    {
        protected HomeGenieService homegenie;

        public ProgramHelperBase(HomeGenieService hg)
        {
            homegenie = hg;
        }

        /// <summary>
        /// Gets the logger object.
        /// </summary>
        /// <value>The logger object.</value>
        public NLog.Logger Log
        {
            get { return MigService.Log; }
        }

        /// <summary>
        /// Playbacks a synthesized voice message from speaker.
        /// </summary>
        /// <param name="sentence">Message to output.</param>
        /// <param name="locale">Language locale string (eg. "en-US", "it-IT", "en-GB", "nl-NL",...).</param>
        /// <param name="goAsync">If true, the command will be executed asyncronously.</param>
        /// <remarks />
        /// <example>
        /// Example:
        /// <code>
        /// Program.Say("The garage door has been opened", "en-US");
        /// </code>
        /// </example>
        public ProgramHelperBase Say(string sentence, string locale = null, bool goAsync = false)
        {
            if (String.IsNullOrWhiteSpace(locale))
            {
                locale = Thread.CurrentThread.CurrentCulture.Name;
            }
            try
            {
                Utility.Say(sentence, locale, goAsync);
            }
            catch (Exception e)
            {
                HomeGenieService.LogError(e);
            }
            return this;
        }
        // these redundant method definitions are for Jint compatibility
        public ProgramHelperBase Say(string sentence)
        {
            return Say(sentence, null, false);
        }
        public ProgramHelperBase Say(string sentence, string locale)
        {
            return Say(sentence, locale, false);
        }

        /// <summary>
        /// Playbacks a wave file.
        /// </summary>
        /// <param name="waveUrl">URL of the audio wave file to play.</param>
        public ProgramHelperBase Play(string waveUrl)
        {
            try
            {
                string outputDirectory = Utility.GetTmpFolder();
                string file = Path.Combine(outputDirectory, "_wave_tmp." + Path.GetExtension(waveUrl));
                using (var webClient = new WebClient())
                {
                    byte[] audiodata = webClient.DownloadData(waveUrl);

                    if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);
                    if (File.Exists(file)) File.Delete(file);

                    var stream = File.OpenWrite(file);
                    stream.Write(audiodata, 0, audiodata.Length);
                    stream.Close();

                    webClient.Dispose();
                }

                Utility.Play(file);
            }
            catch (Exception e)
            {
                HomeGenieService.LogError(e);
            }
            return this;
        }

        /// <summary>
        /// Parses the given (api call) string as a `MigInterfaceCommand` object.
        /// </summary>
        /// <returns>The mig command.</returns>
        /// <param name="apiCall">Api Command (eg. "HomeAutomation.X10/A5/Control.Level/50").</param>
        public MigInterfaceCommand ParseApiCall(string apiCall)
        {
            return new MigInterfaceCommand(apiCall);
        }
        public MigInterfaceCommand ParseApiCall(object apiCall)
        {
            if (apiCall is MigInterfaceCommand)
            {
                return (MigInterfaceCommand)apiCall;
            }
            return ParseApiCall(apiCall.ToString());
        }

        /// <summary>
        /// Invoke an API command and get the result.
        /// </summary>
        /// <returns>The API command response.</returns>
        /// <param name="apiCommand">Any MIG/APP API command without the `/api/` prefix.</param>
        public object ApiCall(string apiCommand)
        {
            if (apiCommand.StartsWith("/api/"))
                apiCommand = apiCommand.Substring(5);
            return homegenie.InterfaceControl(new MigInterfaceCommand(apiCommand));
        }

        /// <summary>
        /// Invoke an API command and get the result.
        /// </summary>
        /// <returns>The API command response.</returns>
        /// <param name="apiCommand">Any MIG/APP API command without the `/api/` prefix.</param>
        /// <param name="data">Data object.</param>
        public object ApiCall(string apiCommand, object data)
        {
            if (apiCommand.StartsWith("/api/"))
                apiCommand = apiCommand.Substring(5);
            return homegenie.InterfaceControl(new MigInterfaceCommand(apiCommand, data));
        }

        /// <summary>
        /// Executes a function asynchronously.
        /// </summary>
        /// <returns>
        /// The Thread object of this asynchronous task.
        /// </returns>
        /// <param name='functionBlock'>
        /// Function name or inline delegate.
        /// </param>
        public Thread RunAsyncTask(Utility.AsyncFunction functionBlock)
        {
            return Utility.RunAsyncTask(functionBlock);
        }

        /// <summary>
        /// Executes the specified Automation Program.
        /// </summary>
        /// <param name='programId'>
        /// Program name or ID.
        /// </param>
        public void Run(string programId)
        {
            Run(programId, "");
        }

        /// <summary>
        /// Executes the specified Automation Program.
        /// </summary>
        /// <param name='programId'>
        /// Program name or ID.
        /// </param>
        /// <param name='options'>
        /// Program options.
        /// </param>
        public void Run(string programId, string options)
        {
            var program = homegenie.ProgramManager.Programs.Find(p => p.Address.ToString() == programId || p.Name == programId);
            if (program != null && !program.IsRunning) //  && program.Address != myProgramId
            {
                program.Engine.StartProgram(options);
            }
        }

        /// <summary>
        /// Wait until the given program is not running.
        /// </summary>
        /// <returns>ProgramHelper.</returns>
        /// <param name="programId">Program address or name.</param>
        public ProgramHelperBase WaitFor(string programId)
        {
            var program = homegenie.ProgramManager.Programs.Find(p => p.Address.ToString() == programId || p.Name == programId);
            if (program != null) // && program.Address != myProgramId)
            {
                while (!program.IsRunning)
                    Thread.Sleep(1000);
                Thread.Sleep(1000);
            }
            return this;
        }

        /// <summary>
        /// Returns a reference to the ProgramHelper of a program.
        /// </summary>
        /// <returns>ProgramHelper.</returns>
        /// <param name="programAddress">Program address (id).</param>
        public ProgramHelper WithAddress(int programAddress)
        {
            var program = homegenie.ProgramManager.Programs.Find(p => p.Address == programAddress);
            ProgramHelper programHelper = null;
            if (program != null)
            {
                programHelper = new ProgramHelper(homegenie, program.Address);
            }
            return programHelper;
        }

        /// <summary>
        /// Returns a reference to the ProgramHelper of a program.
        /// </summary>
        /// <returns>ProgramHelper.</returns>
        /// <param name="programName">Program name.</param>
        public ProgramHelper WithName(string programName)
        {
            var program = homegenie.ProgramManager.Programs.Find(p => p.Name.ToLower() == programName.ToLower());
            ProgramHelper programHelper = null;
            if (program != null)
            {
                programHelper = new ProgramHelper(homegenie, program.Address);
            }
            return programHelper;
        }

    }
}

