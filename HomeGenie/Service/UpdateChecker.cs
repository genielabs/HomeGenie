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

using HomeGenie.Automation;
using HomeGenie.Automation.Scheduler;
using HomeGenie.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Timers;
using System.Xml.Serialization;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace HomeGenie.Service
{
    [Serializable]
    public class ReleaseInfo
    {
        public string Name { get; set; }

        public string Version { get; set; }

        public string Description { get; set; }

        public string ReleaseNote { get; set; }

        public DateTime ReleaseDate { get; set; }

        public string DownloadUrl;
        public bool RequireRestart;
        public bool UpdateBreak;
    }

    public class ArchiveDownloadEventArgs
    {
        public ArchiveDownloadStatus Status;
        public ReleaseInfo ReleaseInfo;

        public ArchiveDownloadEventArgs(ReleaseInfo releaseInfo, ArchiveDownloadStatus status)
        {
            this.ReleaseInfo = releaseInfo;
            this.Status = status;
        }
    }

    public enum ArchiveDownloadStatus
    {
        UNDEFINED,
        DOWNLOADING,
        COMPLETED,
        DECOMPRESSING,
        ERROR
    }

    public class UpdateProgressEventArgs
    {
        public UpdateProgressStatus Status;

        public UpdateProgressEventArgs(UpdateProgressStatus status)
        {
            this.Status = status;
        }
    }

    public enum UpdateProgressStatus
    {
        UNDEFINED,
        STARTED,
        COMPLETED,
        ERROR
    }

    public class UpdateChecker
    {
        public delegate void ArchiveDownloadEvent(object sender, ArchiveDownloadEventArgs args);

        public ArchiveDownloadEvent ArchiveDownloadUpdate;

        public delegate void UpdateProgressEvent(object sender, UpdateProgressEventArgs args);

        public UpdateProgressEvent UpdateProgress;

        public delegate void InstallProgressMessageEvent(object sender, string message);

        public InstallProgressMessageEvent InstallProgressMessage;

        private const string releaseFile = "release_info.xml";
        private const string githubRepository = "HomeGenie";
        private string githubReleases = String.Format("https://api.github.com/repos/genielabs/{0}/releases", githubRepository);
        // TODO: deprecate this
        private const string endpointUrl = "http://www.homegenie.it/release_updates_v1_1.php";

        private ReleaseInfo currentRelease;
        private List<ReleaseInfo> remoteUpdates;
        private Timer checkInterval;
        private const string updateFolder = "_update";

        private HomeGenieService homegenie;


        // TODO: this is just a temporary hack not meant to be used in production enviroment
        public static bool Validator(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors
        )
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }
            // Work-around missing certificates
            string remoteCertificateHash = certificate.GetCertHashString();
            List<string> acceptedCertificates = new List<string>() {
                // Amazon AWS github files hosting
                "89E471D8A4977D0D9C6E67E557BF36A74A5A01DB",
                // github.com
                "D79F076110B39293E349AC89845B0380C19E2F8B",
                // api.github.com
                "CF059889CAFF8ED85E5CE0C2E4F7E6C3C750DD5C",
                "358574EF6735A7CE406950F3C0F680CF803B2E19",
                // genielabs.github.io
                "CCAA484866460E91532C9C7C232AB1744D299D33"
            };
            // try to load acceptedCertificates from file "certaccept.xml"
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<string>));
                using (StringReader stringReader = new StringReader(File.ReadAllText("certaccept.xml")))
                {
                    List<string> cert = (List<string>)xmlSerializer.Deserialize(stringReader);
                    acceptedCertificates.Clear();
                    acceptedCertificates.AddRange(cert);
                }
            } catch { }
            // verify against accepted certificate hash strings
            if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors 
                && acceptedCertificates.Contains(remoteCertificateHash))
            {
                Console.WriteLine("Applied 'SSL certificates issue' work-around.");
                return true;
            }
            else
            {
                Console.WriteLine("SSL validation error! Remote hash is: {0}", remoteCertificateHash);
                return false;
            }
        }


        // TODO: add automatic interval check and "UpdateAvailable", "UpdateChecking" events

        public UpdateChecker(HomeGenieService hg)
        {
            homegenie = hg;
            //
            checkInterval = new Timer(1000 * 60 * 60 * 24); // 24 hours interval between update checks
            checkInterval.AutoReset = true;
            checkInterval.Elapsed += checkInterval_Elapsed;
            //
            remoteUpdates = new List<ReleaseInfo>();

            // TODO: SSL connection certificate validation:
            // TODO: this is just an hack to fix certificate issues happening sometimes on api.github.com site,
            // TODO: not meant to be used in production enviroment
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                // TODO: this is just an hack to fix certificate issues on mono < 4.0,
                ServicePointManager.ServerCertificateValidationCallback = Validator;
            }
        }

        public bool Check()
        {
            if (UpdateProgress != null)
                UpdateProgress(this, new UpdateProgressEventArgs(UpdateProgressStatus.STARTED));
            GetGitHubUpdates();
            if (UpdateProgress != null)
            {
                if (currentRelease != null && remoteUpdates != null)
                    UpdateProgress(this, new UpdateProgressEventArgs(UpdateProgressStatus.COMPLETED));
                else
                    UpdateProgress(this, new UpdateProgressEventArgs(UpdateProgressStatus.ERROR));
            }
            return remoteUpdates != null;
        }

        public void Start()
        {
            Check();
            checkInterval.Start();
        }

        public void Stop()
        {
            checkInterval.Stop();
        }

        public static ReleaseInfo GetReleaseFile(string file)
        {
            ReleaseInfo release = null;
            try
            {
                var serializer = new XmlSerializer(typeof(ReleaseInfo));
                var reader = new StreamReader(file);
                release = (ReleaseInfo)serializer.Deserialize(reader);
                reader.Close();
            }
            catch { }
            return release;
        }

        public ReleaseInfo GetCurrentRelease()
        {
            return currentRelease = GetReleaseFile(releaseFile);
        }

        public List<ReleaseInfo> RemoteUpdates
        {
            get { return remoteUpdates; }
        }

        public List<ReleaseInfo> GetGitHubUpdates()
        {
            GetCurrentRelease();
            //githubReleases
            using (var client = new WebClient())
            {
                client.Headers.Add("user-agent", "HomeGenieUpdater/1.0 (compatible; MSIE 7.0; Windows NT 6.0)");
                try
                {
                    bool collectingComplete = false;
                    string releaseJson = client.DownloadString(githubReleases);
                    var deserializerSettings = new JsonSerializerSettings()
                    {
                        DateParseHandling = Newtonsoft.Json.DateParseHandling.None
                    };
                    dynamic releases = JsonConvert.DeserializeObject(releaseJson, deserializerSettings) as JArray;
                    if (remoteUpdates != null)
                        remoteUpdates.Clear();
                    else
                        remoteUpdates = new List<ReleaseInfo>();
                    foreach(var rel in releases)
                    {
                        foreach(dynamic relFile in (rel.assets as JArray))
                        {
                            if (relFile.browser_download_url.ToString().EndsWith(".tgz"))
                            {
                                var releaseDate = DateTime.ParseExact(relFile.updated_at.ToString(), "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                                if (currentRelease.ReleaseDate < releaseDate && remoteUpdates.Count == 0)
                                {
                                    var r = new ReleaseInfo();
                                    r.Name = githubRepository;
                                    r.Version = rel.tag_name.ToString();
                                    r.Description = rel.name.ToString();
                                    r.ReleaseNote = rel.body.ToString();
                                    r.RequireRestart = false; // this flag is now useless since "restart" flag is dynamically computed by update process
                                    r.UpdateBreak = true; // TODO: store this flag somewhere in the github entry
                                    r.DownloadUrl = relFile.browser_download_url.ToString();
                                    r.ReleaseDate = releaseDate;
                                    remoteUpdates.Add(r);
                                }
                                else if (currentRelease.ReleaseDate < releaseDate)
                                {
                                    string relInfo = String.Format("\r\n\r\n[{0} {1:yyyy-MM-dd}]\r\n{2}", rel.tag_name.ToString(), releaseDate, rel.body.ToString());
                                    remoteUpdates[0].ReleaseNote += relInfo;
                                }
                                else
                                {
                                    collectingComplete = true;
                                }
                            }
                        }
                        // updates from github contains the whole HG bundle so we always consider the most recent one
                        if (collectingComplete)
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    remoteUpdates = null;
                }
                finally
                {
                    client.Dispose();
                }
            }
            return remoteUpdates;
        }

        // TODO: deprecate this
        public List<ReleaseInfo> GetRemoteUpdates()
        {
            using (var client = new WebClient())
            {
                client.Headers.Add("user-agent", "HomeGenieUpdater/1.0 (compatible; MSIE 7.0; Windows NT 6.0)");
                try
                {
                    string releaseXml = client.DownloadString(endpointUrl);
                    var serializer = new XmlSerializer(typeof(List<ReleaseInfo>));
                    using (TextReader reader = new StringReader(releaseXml))
                    {
                        remoteUpdates.Clear();
                        var updates = (List<ReleaseInfo>)serializer.Deserialize(reader);
                        updates.Sort(delegate(ReleaseInfo a, ReleaseInfo b)
                        {
                            return a.ReleaseDate.CompareTo(b.ReleaseDate);
                        });
                        foreach (var releaseInfo in updates)
                        {
                            if (currentRelease != null && currentRelease.ReleaseDate < releaseInfo.ReleaseDate)
                            {
                                remoteUpdates.Add(releaseInfo);
                                if (releaseInfo.UpdateBreak)
                                    break;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    client.Dispose();
                }
            }
            return remoteUpdates;
        }

        public bool IsUpdateAvailable
        {
            get
            {
                bool update = false;
                if (remoteUpdates != null)
                {
                    foreach (var releaseInfo in remoteUpdates)
                    {
                        if (currentRelease != null && currentRelease.ReleaseDate < releaseInfo.ReleaseDate)
                        {
                            update = true;
                            break;
                        }
                    }
                }
                return update;
            }
        }

        public string UpdateBaseFolder
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_update", "files", "HomeGenie"); }
        }

        public bool DownloadUpdateFiles()
        {
            bool success = true;
            //
            if (Directory.Exists(updateFolder))
                Directory.Delete(updateFolder, true);
            //
            if (remoteUpdates != null)
            {
                foreach (var releaseInfo in remoteUpdates)
                {
                    if (currentRelease != null && currentRelease.ReleaseDate < releaseInfo.ReleaseDate)
                    {
                        var files = DownloadAndUncompress(releaseInfo);
                        if (files == null) // || files.Count == 0)
                        {
                            success = false;
                        }
                    }
                }
            }
            return success;
        }

        public List<string> DownloadAndUncompress(ReleaseInfo releaseInfo)
        {
            if (ArchiveDownloadUpdate != null)
                ArchiveDownloadUpdate(this, new ArchiveDownloadEventArgs(releaseInfo, ArchiveDownloadStatus.DOWNLOADING));
            //
            string destinationFolder = Path.Combine(updateFolder, "files");
            string archiveName = Path.Combine(updateFolder, "archives", "hg_update_" + releaseInfo.Version.Replace(" ", "_").Replace(".", "_") + ".zip");
            if (!Directory.Exists(Path.GetDirectoryName(archiveName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(archiveName));
            }
            using (var client = new WebClient())
            {
                client.Headers.Add("user-agent", "HomeGenieUpdater/1.0 (compatible; MSIE 7.0; Windows NT 6.0)");
                try
                {
                    client.DownloadFile(releaseInfo.DownloadUrl, archiveName);
                }
                catch (Exception)
                {
                    if (ArchiveDownloadUpdate != null)
                        ArchiveDownloadUpdate(this, new ArchiveDownloadEventArgs(releaseInfo, ArchiveDownloadStatus.ERROR));
                    return null;
                    //                throw;
                }
                finally
                {
                    client.Dispose();
                }
            }

            // Unarchive (unzip)
            if (ArchiveDownloadUpdate != null)
                ArchiveDownloadUpdate(this, new ArchiveDownloadEventArgs(releaseInfo, ArchiveDownloadStatus.DECOMPRESSING));

            bool errorOccurred = false;
            var files = Utility.UncompressTgz(archiveName, destinationFolder);
            errorOccurred = (files.Count == 0);

            if (ArchiveDownloadUpdate != null)
            {
                if (errorOccurred)
                    ArchiveDownloadUpdate(this, new ArchiveDownloadEventArgs(releaseInfo, ArchiveDownloadStatus.ERROR));
                else
                    ArchiveDownloadUpdate(this, new ArchiveDownloadEventArgs(releaseInfo, ArchiveDownloadStatus.COMPLETED));
            }

            // update release_info.xml file with last releaseInfo ReleaseDate field in order to reflect github release date
            if (files.Contains(Path.Combine("homegenie", releaseFile)))
            {
                var ri = GetReleaseFile(Path.Combine(destinationFolder, "homegenie", releaseFile));
                ri.ReleaseDate = releaseInfo.ReleaseDate.ToUniversalTime();
                XmlSerializer serializer = new XmlSerializer(typeof(ReleaseInfo)); 
                using (TextWriter writer = new StreamWriter(Path.Combine(destinationFolder, "homegenie", releaseFile)))
                {
                    serializer.Serialize(writer, ri); 
                } 
            }

            return files;
        }

        public enum InstallStatus
        {
            Success,
            RestartRequired,
            Error
        }

        public InstallStatus InstallFiles()
        {
            var status = InstallStatus.Success;
            bool restartRequired = false;
            string oldFilesPath = Path.Combine("_update", "oldfiles");
            string newFilesPath = Path.Combine("_update", "files", "HomeGenie_update");
            string fullReleaseFolder = Path.Combine("_update", "files", "homegenie");
            if (Directory.Exists(fullReleaseFolder))
            {
                Directory.Move(fullReleaseFolder, newFilesPath);
            }
            Utility.FolderCleanUp(oldFilesPath);
            if (Directory.Exists(newFilesPath))
            {
                LogMessage("= Copying new files...");
                foreach (string file in Directory.EnumerateFiles(newFilesPath, "*", SearchOption.AllDirectories))
                {
                    bool doNotCopy = false;

                    string destinationFolder = Path.GetDirectoryName(file).Replace(newFilesPath, "").TrimStart('/').TrimStart('\\');
                    string destinationFile = Path.Combine(destinationFolder, Path.GetFileName(file)).TrimStart(Directory.GetDirectoryRoot(Directory.GetCurrentDirectory()).ToArray()).TrimStart('/').TrimStart('\\');

                    // Update file only if different from local one
                    bool processFile = false;
                    if (File.Exists(destinationFile))
                    {
                        using (var md5 = MD5.Create())
                        {
                            string localHash, remoteHash = "";
                            try
                            {
                                // Try getting files' hash
                                using (var stream = File.OpenRead(destinationFile))
                                {
                                    localHash = BitConverter.ToString(md5.ComputeHash(stream));
                                }
                                using (var stream = File.OpenRead(file))
                                {
                                    remoteHash = BitConverter.ToString(md5.ComputeHash(stream));
                                }
                                if (localHash != remoteHash)
                                {
                                    processFile = true;
                                    //Console.WriteLine("CHANGED {0}", destinationFile);
                                    //Console.WriteLine("   - LOCAL  {0}", localHash);
                                    //Console.WriteLine("   - REMOTE {0}", remoteHash);
                                }
                            }
                            catch (Exception e)
                            {
                                // this mostly happen if the destinationFile is un use and cannot be opened,
                                // file is then ignored if hash cannot be calculated
                            }
                        }
                    }
                    else
                    {
                        processFile = true;
                        //Console.WriteLine("NEW FILE {0}", file);
                    }

                    if (processFile)
                    {

                        // Some files needs to be handled differently than just copying
                        if (destinationFile.EndsWith(".xml") && File.Exists(destinationFile))
                        {
                            switch (destinationFile)
                            {
                            case "automationgroups.xml":
                                doNotCopy = true;
                                status = UpdateAutomationGroups(file) ? InstallStatus.Success : InstallStatus.Error;;
                                break;
                            case "groups.xml":
                                doNotCopy = true;
                                status = UpdateGroups(file) ? InstallStatus.Success : InstallStatus.Error;
                                break;
                            case "lircconfig.xml":
                                doNotCopy = true;
                                break;
                            case "modules.xml":
                                doNotCopy = true;
                                break;
                            case "programs.xml":
                                doNotCopy = true;
                                status = UpdatePrograms(file) ? InstallStatus.Success : InstallStatus.Error;;
                                break;
                            case "scheduler.xml":
                                doNotCopy = true;
                                status = UpdateScheduler(file) ? InstallStatus.Success : InstallStatus.Error;;
                                break;
                            case "systemconfig.xml":
                                doNotCopy = true;
                                status = UpdateSystemConfig(file) ? InstallStatus.Success : InstallStatus.Error;;
                                break;
                            }
                            if (status == InstallStatus.Error)
                            {
                                break;
                            }
                        }
                        else if (destinationFile.EndsWith("homegenie_stats.db"))
                        {
                            doNotCopy = true;
                        }

                        // Update the file
                        if (!doNotCopy)
                        {
                            if (destinationFile.EndsWith(".exe") || destinationFile.EndsWith(".dll") || destinationFile.EndsWith(".so"))
                                restartRequired = true;

                            if (!String.IsNullOrWhiteSpace(destinationFolder) && !Directory.Exists(destinationFolder))
                            {
                                Directory.CreateDirectory(destinationFolder);
                            }

                            // backup current file before replacing it
                            if (File.Exists(destinationFile))
                            {
                                string oldFile = Path.Combine(oldFilesPath, destinationFile);
                                Directory.CreateDirectory(Path.GetDirectoryName(oldFile));

                                LogMessage("+ Backup file '" + oldFile + "'");

                                // TODO: delete oldFilesPath before starting update
                                //File.Delete(oldFile); 

                                if (destinationFile.EndsWith(".exe") || destinationFile.EndsWith(".dll"))
                                {
                                    // this will allow replace of new exe and dll files
                                    File.Move(destinationFile, oldFile);
                                }
                                else
                                {
                                    File.Copy(destinationFile, oldFile);
                                }
                            }

                            try
                            {
                                LogMessage("+ Copying file '" + destinationFile + "'");
                                if (!String.IsNullOrWhiteSpace(Path.GetDirectoryName(destinationFile)) && !Directory.Exists(Path.GetDirectoryName(destinationFile)))
                                {
                                    try
                                    {
                                        Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
                                        LogMessage("+ Created folder '" + Path.GetDirectoryName(destinationFile) + "'");
                                    }
                                    catch
                                    {
                                    }
                                }
                                File.Copy(file, destinationFile, true);
                            }
                            catch (Exception e)
                            {
                                LogMessage("! Error copying file '" + destinationFile + "' (" + e.Message + ")");
                                status = InstallStatus.Error;
                                break;
                            }
                        }
                    }

                }

                if (status == InstallStatus.Error)
                {
                    // TODO: should revert!
                    LogMessage("! ERROR update aborted.");
                }
                else if (restartRequired)
                {
                    status = InstallStatus.RestartRequired;
                }

            }

            return status;
        }

        // TODO: deprecate this
        public bool IsRestartRequired
        {
            get
            {
                bool restartRequired = false;
                if (remoteUpdates != null)
                {
                    foreach (var releaseInfo in remoteUpdates)
                    {
                        if (releaseInfo.RequireRestart)
                        {
                            restartRequired = true;
                            break;
                        }
                    }
                }
                return restartRequired;
            }
        }

        public bool UpdateGroups(string file)
        {
            bool success = true;
            //
            // add new modules groups 
            //
            try
            {
                var serializer = new XmlSerializer(typeof(List<Group>));
                var reader = new StreamReader(file);
                var modulesGroups = (List<Group>)serializer.Deserialize(reader);
                reader.Close();
                //
                bool configChanged = false;
                foreach (var group in modulesGroups)
                {
                    if (homegenie.Groups.Find(g => g.Name == group.Name) == null)
                    {
                        LogMessage("+ Adding Modules Group: " + group.Name);
                        homegenie.Groups.Add(group);
                        if (!configChanged)
                            configChanged = true;
                    }
                }
                //
                if (configChanged)
                {
                    homegenie.UpdateGroupsDatabase("");
                }
            }
            catch
            {
                success = false;
            }
            if (!success)
            {
                LogMessage("! ERROR updating Modules Groups");
            }
            return success;
        }

        public bool UpdateAutomationGroups(string file)
        {
            bool success = true;
            //
            // add new automation groups 
            //
            try
            {
                var serializer = new XmlSerializer(typeof(List<Group>));
                var reader = new StreamReader(file);
                var automationGroups = (List<Group>)serializer.Deserialize(reader);
                reader.Close();
                //
                bool configChanged = false;
                foreach (var group in automationGroups)
                {
                    if (homegenie.AutomationGroups.Find(g => g.Name == group.Name) == null)
                    {
                        LogMessage("+ Adding Automation Group: " + group.Name);
                        homegenie.AutomationGroups.Add(group);
                        if (!configChanged)
                            configChanged = true;
                    }
                }
                //
                if (configChanged)
                {
                    homegenie.UpdateGroupsDatabase("Automation");
                }
            }
            catch
            {
                success = false;
            }
            if (!success)
            {
                LogMessage("! ERROR updating Automation Groups");
            }
            return success;
        }

        public bool UpdateScheduler(string file)
        {
            bool success = true;
            //
            // add new scheduler items
            //
            try
            {
                var serializer = new XmlSerializer(typeof(List<SchedulerItem>));
                var reader = new StreamReader(file);
                var schedulerItems = (List<SchedulerItem>)serializer.Deserialize(reader);
                reader.Close();
                //
                bool configChanged = false;
                foreach (var item in schedulerItems)
                {
                    // it will only import the new ones
                    if (homegenie.ProgramManager.SchedulerService.Get(item.Name) == null)
                    {
                        LogMessage("+ Adding Scheduler Item: " + item.Name);
                        homegenie.ProgramManager.SchedulerService.AddOrUpdate(item.Name, item.CronExpression, item.Data, item.Description, item.Script);
                        if (!configChanged)
                            configChanged = true;
                    }
                }
                //
                if (configChanged)
                {
                    homegenie.UpdateSchedulerDatabase();
                }
            }
            catch
            {
                success = false;
            }
            if (!success)
            {
                LogMessage("! ERROR updating Scheduler Items");
            }
            return success;
        }

        public bool UpdateSystemConfig(string file)
        {
            bool success = true;
            //
            // add new MIG interfaces
            //
            try
            {
                var serializer = new XmlSerializer(typeof(SystemConfiguration));
                var reader = new StreamReader(file);
                var config = (SystemConfiguration)serializer.Deserialize(reader);
                //
                bool configChanged = false;
                foreach (var iface in config.MigService.Interfaces)
                {
                    if (homegenie.SystemConfiguration.MigService.GetInterface(iface.Domain) == null)
                    {
                        LogMessage("+ Adding MIG Interface: " + iface.Domain);
                        homegenie.SystemConfiguration.MigService.Interfaces.Add(iface);
                        if (!configChanged)
                            configChanged = true;
                    }
                }
                //
                if (configChanged)
                {
                    homegenie.SystemConfiguration.Update();
                }
            }
            catch
            {
                success = false;
            }
            if (!success)
            {
                LogMessage("! ERROR updating System Configuration");
            }
            return success;
        }

        public bool UpdatePrograms(string file)
        {
            bool success = true;
            try
            {
                var serializer = new XmlSerializer(typeof(List<ProgramBlock>));
                var reader = new StreamReader(file);
                var newProgramList = (List<ProgramBlock>)serializer.Deserialize(reader);
                reader.Close();
                //
                if (newProgramList.Count > 0)
                {
                    bool configChanged = false;
                    foreach (var program in newProgramList)
                    {

                        // Only system programs are to be updated
                        if (program.Address < ProgramManager.USERSPACE_PROGRAMS_START)
                        {
                            ProgramBlock oldProgram = homegenie.ProgramManager.Programs.Find(p => p.Address == program.Address);
                            if (oldProgram != null)
                            {

                                // Check new program against old one to find out if they differ
                                bool changed = ProgramsDiff(oldProgram, program);
                                if (!changed)
                                    continue;

                                // Preserve IsEnabled status if program already exist
                                program.IsEnabled = oldProgram.IsEnabled;
                                LogMessage("* Updating Automation Program: " + program.Name + " (" + program.Address + ")");
                                homegenie.ProgramManager.ProgramRemove(oldProgram);

                            }
                            else
                            {
                                LogMessage("+ Adding Automation Program: " + program.Name + " (" + program.Address + ")");
                            }

                            // Try copying the new program files (binary dll or arduino sketch files)
                            try
                            {
                                if (program.Type.ToLower() == "csharp")
                                {
                                    File.Copy(Path.Combine(UpdateBaseFolder, "programs", program.Address + ".dll"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs", program.Address + ".dll"), true);
                                }
                                else if (program.Type.ToLower() == "arduino")
                                {
                                    // copy arduino project files...
                                    // TODO: this is untested yet
                                    string sourceFolder = Path.Combine(UpdateBaseFolder, "programs", "arduino", program.Address.ToString());
                                    string arduinoFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs", "arduino", program.Address.ToString());
                                    Utility.FolderCleanUp(arduinoFolder);
                                    foreach (string newPath in Directory.GetFiles(sourceFolder))
                                    {
                                        File.Copy(newPath, newPath.Replace(sourceFolder, arduinoFolder), true);
                                        LogMessage("* Updating Automation Program: " + program.Name + " (" + program.Address + ") - " + Path.GetFileName(newPath));
                                    }
                                }
                            }
                            catch
                            {
                            }

                            // Add the new program to the ProgramEngine
                            homegenie.ProgramManager.ProgramAdd(program);

                            if (!configChanged)
                                configChanged = true;
                        }

                    }

                    if (configChanged)
                    {
                        // Save new programs config
                        homegenie.UpdateProgramsDatabase();
                    }
                }
                //
                File.Delete(file);
                if (Directory.Exists(Path.Combine(homegenie.UpdateChecker.UpdateBaseFolder, "programs")))
                {
                    Directory.Delete(Path.Combine(homegenie.UpdateChecker.UpdateBaseFolder, "programs"), true);
                }
            }
            catch
            {
                success = false;
            }

            if (!success)
            {
                LogMessage("+ ERROR updating Automation Programs");
            }
            return success;
        }

        private bool ProgramsDiff(ProgramBlock oldProgram, ProgramBlock newProgram)
        {
            bool unchanged = (JsonConvert.SerializeObject(oldProgram.ConditionType) == JsonConvert.SerializeObject(newProgram.ConditionType)) &&
                             (JsonConvert.SerializeObject(oldProgram.Conditions) == JsonConvert.SerializeObject(newProgram.Conditions)) &&
                             (JsonConvert.SerializeObject(oldProgram.Commands) == JsonConvert.SerializeObject(newProgram.Commands)) &&
                             (oldProgram.ScriptCondition == newProgram.ScriptCondition) &&
                             (oldProgram.ScriptSource == newProgram.ScriptSource) &&
                             (oldProgram.Name == newProgram.Name) &&
                             (oldProgram.Description == newProgram.Description) &&
                             (oldProgram.Group == newProgram.Group) &&
                             (oldProgram.Type == newProgram.Type);
            return !unchanged;
        }

        private void LogMessage(string message)
        {
            if (InstallProgressMessage != null)
            {
                InstallProgressMessage(this, message);
            }
        }

        private void checkInterval_Elapsed(object sender, ElapsedEventArgs e)
        {
            Check();
            if (IsUpdateAvailable)
            {
                // TODO: ...
            }
        }

    }
}
