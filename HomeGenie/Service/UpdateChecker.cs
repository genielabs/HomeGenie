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

        public ArchiveDownloadEventArgs(ReleaseInfo ri, ArchiveDownloadStatus status)
        {
            this.ReleaseInfo = ri;
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

        private string _endpointurl = "http://generoso.info/homegenie/release_updates.php";
        private ReleaseInfo _currentrelease;
        private List<ReleaseInfo> _remoteupdates;
        private Timer _interval;
        private const string _updatefolder = "_update";

        // TODO: add automatic interval check and "UpdateAvailable", "UpdateChecking" events

        public UpdateChecker()
        {
            _interval = new Timer(1000 * 60 * 60 * 24); // 24 hours interval between update checks
            _interval.AutoReset = true;
            _interval.Elapsed += _interval_Elapsed;
            //
            _remoteupdates = new List<ReleaseInfo>();
        }

        public void Check()
        {
            if (UpdateProgress != null) UpdateProgress(this, new UpdateProgressEventArgs(UpdateProgressStatus.STARTED));
            GetCurrentRelease();
            GetRemoteUpdates();
            if (_currentrelease != null && _remoteupdates != null && UpdateProgress != null)
            {
                UpdateProgress(this, new UpdateProgressEventArgs(UpdateProgressStatus.COMPLETED));
            }
            else
            {
                UpdateProgress(this, new UpdateProgressEventArgs(UpdateProgressStatus.ERROR));
            }
            //
            // TODO: remove the following lines at some point... 
            string newupdater = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HomeGenieUpdaterNew.exe");
            string currentupdater = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HomeGenieUpdater.exe");
            if (File.Exists(newupdater))
            {
                try
                {
                    File.Copy(newupdater, currentupdater, true);
                    File.Delete(newupdater);
                }
                catch { }
            }
        }

        public void Start()
        {
            Check();
            _interval.Start();
            //
            /*
            string destfolder = @"C:\Users\Gene\Desktop\HomeGenie_1_00_beta_r318_update\";
            Directory.SetCurrentDirectory(destfolder);
            foreach (string file in Directory.EnumerateFiles(
                    ".", "*.*", SearchOption.AllDirectories))
            {
                Console.WriteLine(file);
                Utility.AddFileToZip("hgupdate.zip", file);
            }
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            Directory.GetDirectoryRoot(Directory.GetCurrentDirectory());
            */

        }

        public void Stop()
        {
            _interval.Stop();
        }

        public ReleaseInfo GetCurrentRelease()
        {
            try
            {
                XmlSerializer oserializer = new XmlSerializer(typeof(ReleaseInfo));
                StreamReader oreader = new StreamReader("release_info.xml");
                _currentrelease = (ReleaseInfo)oserializer.Deserialize(oreader);
                oreader.Close();
            }
            catch { }
            return _currentrelease;
        }

        public List<ReleaseInfo> RemoteUpdates
        {
            get { return _remoteupdates; }
        }

        public List<ReleaseInfo> GetRemoteUpdates()
        {
            WebClient webcli = new WebClient();
            webcli.Headers.Add("user-agent", "HomeGenieUpdater/1.0 (compatible; MSIE 7.0; Windows NT 6.0)");
            try
            {
                string relxml = webcli.DownloadString(_endpointurl);
                var serializer = new XmlSerializer(typeof(List<ReleaseInfo>));
                using (TextReader reader = new StringReader(relxml))
                {
                    _remoteupdates.Clear();
                    List<ReleaseInfo> rupdates = (List<ReleaseInfo>)serializer.Deserialize(reader);
                    rupdates.Sort(delegate(ReleaseInfo a, ReleaseInfo b) { return a.ReleaseDate.CompareTo(b.ReleaseDate); });
                    foreach (ReleaseInfo ri in rupdates)
                    {
                        if (_currentrelease != null && _currentrelease.ReleaseDate < ri.ReleaseDate)
                        {
                            _remoteupdates.Add(ri);
                            if (ri.UpdateBreak) break;
                        }
                    }
                }
            }
            catch (Exception) { }
            return _remoteupdates;
        }

        public bool IsUpdateAvailable
        {
            get
            {
                bool update = false;
                if (_remoteupdates != null)
                {
                    foreach (ReleaseInfo ri in _remoteupdates)
                    {
                        if (_currentrelease != null && _currentrelease.ReleaseDate < ri.ReleaseDate)
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
            if (Directory.Exists(_updatefolder)) Directory.Delete(_updatefolder, true);
            //
            if (_remoteupdates != null)
            {
                foreach (ReleaseInfo ri in _remoteupdates)
                {
                    if (_currentrelease != null && _currentrelease.ReleaseDate < ri.ReleaseDate)
                    {
                        List<string> files = DownloadAndUncompress(ri);
                        if (files == null) // || files.Count == 0)
                        {
                            success = false;
                        }
                    }
                }
            }
            return success;
        }

        public List<string> DownloadAndUncompress(ReleaseInfo ri)
        {
            if (ArchiveDownloadUpdate != null) ArchiveDownloadUpdate(this, new ArchiveDownloadEventArgs(ri, ArchiveDownloadStatus.STARTED));
            //
            string destfolder = Path.Combine(_updatefolder, "files");
            string archivename = Path.Combine(_updatefolder, "archives", "hg_update_" + ri.Version.Replace(" ", "_").Replace(".", "_") + ".zip");
            if (!Directory.Exists(Path.GetDirectoryName(archivename)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(archivename));
            }
            WebClient webcli = new WebClient();
            webcli.Headers.Add("user-agent", "HomeGenieUpdater/1.0 (compatible; MSIE 7.0; Windows NT 6.0)");
            try
            {
                webcli.DownloadFile(ri.DownloadUrl, archivename);
            }
            catch (Exception)
            {
                if (ArchiveDownloadUpdate != null) ArchiveDownloadUpdate(this, new ArchiveDownloadEventArgs(ri, ArchiveDownloadStatus.ERROR));
                return null;
                //                throw;
            }

            // Unarchive (unzip)
            if (ArchiveDownloadUpdate != null) ArchiveDownloadUpdate(this, new ArchiveDownloadEventArgs(ri, ArchiveDownloadStatus.DECOMPRESSING));

            bool decompresserror = false;
            List<string> files = new List<string>();
            try
            {
                using (ZipPackage package = (ZipPackage)Package.Open(archivename, FileMode.Open, FileAccess.Read))
                {
                    foreach (PackagePart part in package.GetParts())
                    {
                        string target = Path.Combine(destfolder, part.Uri.OriginalString.Substring(1)).TrimStart(Directory.GetDirectoryRoot(Directory.GetCurrentDirectory()).ToArray()).TrimStart('/');
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
                decompresserror = true;
            }

            if (ArchiveDownloadUpdate != null)
            {
                if (decompresserror)
                    ArchiveDownloadUpdate(this, new ArchiveDownloadEventArgs(ri, ArchiveDownloadStatus.ERROR));
                else
                    ArchiveDownloadUpdate(this, new ArchiveDownloadEventArgs(ri, ArchiveDownloadStatus.COMPLETED));
            }

            return files;
        }

        public void Upgrade()
        {
            // - restart only if any *.dll *.exe *.so file
            // - copy all files
            // - update current_release.xml file
            // - delete _updates folder
        }


        //

        void _interval_Elapsed(object sender, ElapsedEventArgs e)
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
                bool restartrequired = false;
                if (_remoteupdates != null)
                {
                    foreach (ReleaseInfo ri in _remoteupdates)
                    {
                        if (ri.RequireRestart)
                        {
                            restartrequired = true;
                            break;
                        }
                    }
                }
                return restartrequired;
            }
        }
    }
}
