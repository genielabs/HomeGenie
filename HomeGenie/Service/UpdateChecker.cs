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
using System.IO.Compression;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Text;
using System.Timers;
using System.Xml.Serialization;

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

        private string endpointUrl = "http://www.homegenie.it/release_updates.php";
        private ReleaseInfo currentRelease;
        private List<ReleaseInfo> remoteUpdates;
        private Timer checkInterval;
        private const string updateFolder = "_update";

        private HomeGenieService homegenie;

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
        }

        public void Check()
        {
            if (UpdateProgress != null) UpdateProgress(this, new UpdateProgressEventArgs(UpdateProgressStatus.STARTED));
            GetCurrentRelease();
            GetRemoteUpdates();
            if (currentRelease != null && remoteUpdates != null && UpdateProgress != null)
            {
                UpdateProgress(this, new UpdateProgressEventArgs(UpdateProgressStatus.COMPLETED));
            }
            else
            {
                UpdateProgress(this, new UpdateProgressEventArgs(UpdateProgressStatus.ERROR));
            }
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

        public ReleaseInfo GetCurrentRelease()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(ReleaseInfo));
                var reader = new StreamReader("release_info.xml");
                currentRelease = (ReleaseInfo)serializer.Deserialize(reader);
                reader.Close();
            }
            catch { }
            return currentRelease;
        }

        public List<ReleaseInfo> RemoteUpdates
        {
            get { return remoteUpdates; }
        }

        public List<ReleaseInfo> GetRemoteUpdates()
        {
            var client = new WebClient();
            client.Headers.Add("user-agent", "HomeGenieUpdater/1.0 (compatible; MSIE 7.0; Windows NT 6.0)");
            try
            {
                string releaseXml = client.DownloadString(endpointUrl);
                var serializer = new XmlSerializer(typeof(List<ReleaseInfo>));
                using (TextReader reader = new StringReader(releaseXml))
                {
                    remoteUpdates.Clear();
                    var updates = (List<ReleaseInfo>)serializer.Deserialize(reader);
                    updates.Sort(delegate(ReleaseInfo a, ReleaseInfo b) { return a.ReleaseDate.CompareTo(b.ReleaseDate); });
                    foreach (var releaseInfo in updates)
                    {
                        if (currentRelease != null && currentRelease.ReleaseDate < releaseInfo.ReleaseDate)
                        {
                            remoteUpdates.Add(releaseInfo);
                            if (releaseInfo.UpdateBreak) break;
                        }
                    }
                }
            }
            catch (Exception) { }
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
            if (Directory.Exists(updateFolder)) Directory.Delete(updateFolder, true);
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
            if (ArchiveDownloadUpdate != null) ArchiveDownloadUpdate(this, new ArchiveDownloadEventArgs(releaseInfo, ArchiveDownloadStatus.DOWNLOADING));
            //
            string destinationFolder = Path.Combine(updateFolder, "files");
            string archiveName = Path.Combine(updateFolder, "archives", "hg_update_" + releaseInfo.Version.Replace(" ", "_").Replace(".", "_") + ".zip");
            if (!Directory.Exists(Path.GetDirectoryName(archiveName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(archiveName));
            }
            var client = new WebClient();
            client.Headers.Add("user-agent", "HomeGenieUpdater/1.0 (compatible; MSIE 7.0; Windows NT 6.0)");
            try
            {
                client.DownloadFile(releaseInfo.DownloadUrl, archiveName);
            }
            catch (Exception)
            {
                if (ArchiveDownloadUpdate != null) ArchiveDownloadUpdate(this, new ArchiveDownloadEventArgs(releaseInfo, ArchiveDownloadStatus.ERROR));
                return null;
                //                throw;
            }

            // Unarchive (unzip)
            if (ArchiveDownloadUpdate != null) ArchiveDownloadUpdate(this, new ArchiveDownloadEventArgs(releaseInfo, ArchiveDownloadStatus.DECOMPRESSING));

            bool errorOccurred = false;
            var files = new List<string>();
            try
            {
                using (ZipPackage package = (ZipPackage)Package.Open(archiveName, FileMode.Open, FileAccess.Read))
                {
                    foreach (PackagePart part in package.GetParts())
                    {
                        string target = Path.Combine(destinationFolder, part.Uri.OriginalString.Substring(1)).TrimStart(Directory.GetDirectoryRoot(Directory.GetCurrentDirectory()).ToArray()).TrimStart('/');
                        if (!Directory.Exists(Path.GetDirectoryName(target)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(target));
                        }

                        if (File.Exists(target)) File.Delete(target);

                        using (Stream source = part.GetStream(
                            FileMode.Open, FileAccess.Read))
                        using (Stream destination = File.OpenWrite(target))
                        {
                            byte[] buffer = new byte[4096];
                            int read;
                            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                destination.Write(buffer, 0, read);
                            }
                        }
                        files.Add(target);
                        //Console.WriteLine("Deflated {0}", target);
                    }
                }
            }
            catch (Exception)
            {
                errorOccurred = true;
            }

            if (ArchiveDownloadUpdate != null)
            {
                if (errorOccurred)
                    ArchiveDownloadUpdate(this, new ArchiveDownloadEventArgs(releaseInfo, ArchiveDownloadStatus.ERROR));
                else
                    ArchiveDownloadUpdate(this, new ArchiveDownloadEventArgs(releaseInfo, ArchiveDownloadStatus.COMPLETED));
            }

            return files;
        }

        public bool InstallFiles()
        {
            bool success = true;

            string oldFilesPath = Path.Combine("_update", "oldfiles");
            Utility.FolderCleanUp(oldFilesPath);
            if (Directory.Exists(Path.Combine("_update", "files", "HomeGenie")))
            {
                LogMessage("= Copying new files...");
                foreach (string file in Directory.EnumerateFiles(Path.Combine("_update", "files", "HomeGenie"), "*", SearchOption.AllDirectories))
                {
                    bool doNotCopy = false;

                    string destinationFolder = Path.GetDirectoryName(file).Replace(Path.Combine("_update", "files", "HomeGenie"), "").TrimStart('/').TrimStart('\\');
                    if (!String.IsNullOrWhiteSpace(destinationFolder) && !Directory.Exists(destinationFolder))
                    {
                        Directory.CreateDirectory(destinationFolder);
                    }
                    string destinationFile = Path.Combine(destinationFolder, Path.GetFileName(file)).TrimStart(Directory.GetDirectoryRoot(Directory.GetCurrentDirectory()).ToArray()).TrimStart('/').TrimStart('\\');

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

                    if (destinationFile.EndsWith(".xml") && File.Exists(destinationFile))
                    {
                        switch (destinationFile)
                        {
                            case "automationgroups.xml":
                                doNotCopy = true;
                                success = UpdateAutomationGroups(file);
                                break;
                            case "groups.xml":
                                doNotCopy = true;
                                success = UpdateGroups(file);
                                break;
                            case "lircconfig.xml":
                                doNotCopy = true;
                                break;
                            case "modules.xml":
                                doNotCopy = true;
                                break;
                            case "programs.xml":
                                doNotCopy = true;
                                success = UpdatePrograms(file);
                                break;
                            case "scheduler.xml":
                                doNotCopy = true;
                                success = UpdateScheduler(file);
                                break;
                            case "systemconfig.xml":
                                doNotCopy = true;
                                success = UpdateSystemConfig(file);
                                break;
                        }
                        if (!success)
                        {
                            break;
                        }
                    }

                    if (!doNotCopy)
                    {
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
                                catch { }
                            }
                            File.Copy(file, destinationFile, true);
                        }
                        catch (Exception e)
                        {
                            LogMessage("! Error copying file '" + destinationFile + "' (" + e.Message + ")");
                            success = false;
                            break;
                        }
                    }

                }

                if (!success)
                {
                    // TODO: should revert!
                    LogMessage("! ERROR update aborted.");
                }

            }

            return success;
        }


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


        private void checkInterval_Elapsed(object sender, ElapsedEventArgs e)
        {
            Check();
            if (IsUpdateAvailable)
            {
                // TODO: ...
            }
        }

        private void LogMessage(string message)
        {
            if (InstallProgressMessage != null)
            {
                InstallProgressMessage(this, message);
            }
        }

        private bool UpdateGroups(string file)
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
                        if (!configChanged) configChanged = true;
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

        private bool UpdateAutomationGroups(string file)
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
                        if (!configChanged) configChanged = true;
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

        private bool UpdateScheduler(string file)
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
                    if (homegenie.ProgramManager.SchedulerService.Get(item.Name) == null)
                    {
                        LogMessage("+ Adding Scheduler Item: " + item.Name);
                        homegenie.ProgramManager.SchedulerService.AddOrUpdate(item.Name, item.CronExpression);
                        if (!configChanged) configChanged = true;
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

        private bool UpdateSystemConfig(string file)
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
                        if (!configChanged) configChanged = true;
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

        private bool UpdatePrograms(string file)
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
                        if (program.Address < ProgramManager.USER_SPACE_PROGRAMS_START)
                        {
                            ProgramBlock oldProgram = homegenie.ProgramManager.Programs.Find(p => p.Address == program.Address);
                            if (oldProgram != null)
                            {

                                // Check new program against old one to find out if they differ
                                bool changed = ProgramsDiff(oldProgram, program);
                                if (!changed) continue;

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
                            catch { }

                            // Add the new program to the ProgramEngine
                            homegenie.ProgramManager.ProgramAdd(program);

                            if (!configChanged) configChanged = true;
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

    }
}
