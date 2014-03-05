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
        public string Name;
        public string Version;
        public string Description;
        public string ReleaseNote;
        public DateTime ReleaseDate;
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
        STARTED,
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

        private string endpointUrl = "http://www.homegenie.it/release_updates.php";
        private ReleaseInfo currentRelease;
        private List<ReleaseInfo> remoteUpdates;
        private Timer checkInterval;
        private const string updateFolder = "_update";

        // TODO: add automatic interval check and "UpdateAvailable", "UpdateChecking" events

        public UpdateChecker()
        {
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
            //
            // TODO: remove the following lines at some point... 
            string newUpdaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HomeGenieUpdaterNew.exe");
            string currentUpdaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HomeGenieUpdater.exe");
            if (File.Exists(newUpdaterPath))
            {
                try
                {
                    File.Copy(newUpdaterPath, currentUpdaterPath, true);
                    File.Delete(newUpdaterPath);
                }
                catch { }
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
            if (ArchiveDownloadUpdate != null) ArchiveDownloadUpdate(this, new ArchiveDownloadEventArgs(releaseInfo, ArchiveDownloadStatus.STARTED));
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

        void checkInterval_Elapsed(object sender, ElapsedEventArgs e)
        {
            Check();
            if (IsUpdateAvailable)
            {
                // TODO: ...
            }
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
    }
}
