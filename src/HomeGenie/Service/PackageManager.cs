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
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;

using HomeGenie.Automation;
using HomeGenie.Automation.Scheduler;
using HomeGenie.Data;
using HomeGenie.Service.Constants;

using MIG.Config;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HomeGenie.Service
{
    [Serializable()]
    public class PackageData
    {
        [JsonProperty("repository")]
        public string Repository { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("programs")]
        public List<PackageDataItem> Programs { get; set; }
        [JsonProperty("modules")]
        public List<PackageDataItem> Modules { get; set; }
        [JsonProperty("groups")]
        public List<PackageDataItem> Groups { get; set; }
        [JsonProperty("schedules")]
        public List<PackageDataItem> Schedules { get; set; }
    }

    [Serializable()]
    public class PackageDataItem
    {
        [JsonProperty("repository")]
        public string Repository { get; set; }
        [JsonProperty("packageId")]
        public string PackageId { get; set; }
        [JsonProperty("packageVersion")]
        public string PackageVersion { get; set; }
        [JsonProperty("hid")]
        public string Hid { get; set; } // internal homegenie id used for this package item 
        [JsonProperty("id")]
        public string Id { get; set; } // unique identifier for this package item
        [JsonProperty("required")]
        public bool Required { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("checksum")]
        public string Checksum { get; set; }
        [JsonProperty("installed")]
        public bool Installed { get; set; }
    }

    public class PackageManager
    {
        private HomeGenieService homegenie;
        private string widgetBasePath;

        public const string PACKAGE_LIST_FILE = "installed_packages.json";

        public PackageManager(HomeGenieService hg)
        {
            homegenie = hg;
            widgetBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "html", "pages", "control", "widgets");
        }


        #region New Methods

        public List<PackageData> GetPackagesList()
        {
            var packagesList = new List<PackageData>();
            // create PackageData by scanning all ./data/packages folders
            string packagesFolder = Path.Combine(Utility.GetDataFolder(), "packages");
            string[] repositories = Directory.GetDirectories(packagesFolder);
            foreach (string repository in repositories)
            {
                string repoFolder = Path.Combine(packagesFolder, repository);
                string[] packages = Directory.GetDirectories(repoFolder);
                foreach (string package in packages)
                {
                    string packageFile = Path.Combine(repoFolder, package, "package.json");
                    var pd = JsonConvert.DeserializeObject<PackageData>(File.ReadAllText(packageFile));
                    packagesList.Add(pd);
                }
            }
            // sort list by repository / package /
            packagesList.Sort((a, b) =>
            {
                if (a.Repository != b.Repository)
                {
                    return String.Compare(a.Repository, b.Repository, StringComparison.Ordinal);
                }
                return String.Compare(a.Id, b.Id, StringComparison.Ordinal);
            });
            packagesList.ForEach((pkg) =>
            {
                pkg.Groups.Sort((p1, p2) =>
                {
                    if (p1.Required != p2.Required)
                    {
                        return p2.Required.CompareTo(p1.Required);
                    }
                    return String.Compare(p1.Id, p2.Id, StringComparison.Ordinal);
                });
                pkg.Programs.Sort((p1, p2) =>
                {
                    if (p1.Required != p2.Required)
                    {
                        return p2.Required.CompareTo(p1.Required);
                    }
                    return String.Compare(p1.Id, p2.Id, StringComparison.Ordinal);
                });
                pkg.Schedules.Sort((p1, p2) =>
                {
                    if (p1.Required != p2.Required)
                    {
                        return p2.Required.CompareTo(p1.Required);
                    }
                    return String.Compare(p1.Id, p2.Id, StringComparison.Ordinal);
                });
                // set the 'Installed' flag
                pkg.Programs.ForEach((p) =>
                {
                    var installedInstances = homegenie.ProgramManager.Programs
                        .FindAll((pr) => pr.PackageInfo.Repository == p.Repository && pr.PackageInfo.PackageId == p.PackageId && pr.PackageInfo.Id == p.Id);
                    p.Installed = (installedInstances.Count > 0);
                    // TODO: should also check version/checksum and report if different
                    // TODO: should also check version/checksum and report if different
                });
                pkg.Schedules.ForEach((s) =>
                {
                    var sch = homegenie.ProgramManager.SchedulerService.Get(s.Hid);
                    s.Installed = (sch != null);
                });
                pkg.Groups.ForEach((g) =>
                {
                    var grp = homegenie.Groups.Find((gr) => gr.Name == g.Hid);
                    g.Installed = (grp != null);
                });
            });
            return packagesList;
        }

        public bool InstallPackage(string packageFile)
    {
            var packageData = JsonConvert.DeserializeObject<PackageData>(File.ReadAllText(packageFile));
            string repositoryFolder = Path.Combine(Utility.GetDataFolder(), "packages", packageData.Repository);
            string programsDatabase = Path.Combine(repositoryFolder, packageData.Id, "programs.xml");
            ProgramBlock programToInstall;
            var serializer = new XmlSerializer(typeof(List<ProgramBlock>));
            using (var reader = new StreamReader(programsDatabase))
            {
                var programs = (List<ProgramBlock>)serializer.Deserialize(reader);
                foreach (var item in packageData.Programs)
                {
                    if (item.Repository != packageData.Repository || item.PackageId != packageData.Id)
                    {
                        // external dependency
                        // TODO: external dependency are not automatically installed
                        // TODO: should report to user
                        continue;
                    }
                    programToInstall = programs.Find((p) => p.Address.ToString() == item.Hid);
                    var installedInstances = homegenie.ProgramManager.Programs
                        .FindAll((p) => p.PackageInfo.Repository == item.Repository && p.PackageInfo.PackageId == item.PackageId && p.PackageInfo.Id == item.Id);
                    if (installedInstances.Count > 0)
                    {
                        Console.WriteLine("Installing program " + JsonConvert.SerializeObject(item) + item.Hid);
                        // update program code
                        var install = programToInstall;
                        installedInstances.ForEach((p) =>
                        {
                            p.Description = install.Description;
                            p.ScriptSetup = install.ScriptSetup;
                            p.ScriptSource = install.ScriptSource;
                            p.PackageInfo.Checksum = item.Checksum;
                            homegenie.ProgramManager.ProgramCompile(p);
                        });
                    }
                    else if (programToInstall != null)
                    {
                        // add automation group if does not exist
                        if (!String.IsNullOrEmpty(programToInstall.Group))
                        {
                            bool programGroupExists = homegenie.AutomationGroups.Find((ag) => ag.Name == programToInstall.Group) != null;
                            if (!programGroupExists)
                            {
                                homegenie.AutomationGroups.Add(new Group()
                                {
                                    Name = programToInstall.Group
                                });
                                homegenie.UpdateGroupsDatabase("Automation");
                            }
                        }
                        // add the new programm
                        int newProgramId = int.Parse(item.Hid);
                        var prg = homegenie.ProgramManager.ProgramGet(newProgramId);
                        if (prg != null)
                        {
                            // assign a new id if the standard id is already taken
                            newProgramId = homegenie.ProgramManager.GeneratePid();
                        }
                        // not installed, install it
                        programToInstall.Address = newProgramId;
                        homegenie.ProgramManager.ProgramAdd(programToInstall);
                        homegenie.ProgramManager.ProgramCompile(programToInstall);
                    }
                    else
                    {
                        // TODO: this should never happen, maybe add reporting
                    }
                }
                homegenie.UpdateProgramsDatabase();
            }
            string modulesDatabase = Path.Combine(repositoryFolder, packageData.Id, "modules.xml");
            if (File.Exists(modulesDatabase))
            {
                serializer = new XmlSerializer(typeof(List<Module>));
                using (var reader = new StreamReader(modulesDatabase))
                {
                    var modules = (List<Module>)serializer.Deserialize(reader);
                    foreach (var module in modules)
                    {
                        homegenie.Modules.RemoveAll((m) => m.Domain == module.Domain && module.Address == m.Address);
                        homegenie.Modules.Add(module);
                    }
                    homegenie.UpdateModulesDatabase();
                }
            }
            string groupsDatabase = Path.Combine(repositoryFolder, packageData.Id, "groups.xml");
            if (File.Exists(groupsDatabase))
            {
                serializer = new XmlSerializer(typeof(List<Group>));
                using (var reader = new StreamReader(groupsDatabase))
                {
                    var pkgGroups = (List<Group>)serializer.Deserialize(reader);
                    foreach (var group in pkgGroups)
                    {
                        bool exists = homegenie.Groups.Find((g) => g.Name == group.Name) != null;
                        if (!exists)
                        {
                            homegenie.Groups.Add(group);
                        }
                        // merge modules
                        var targetGroup = homegenie.Groups.Find((g) => g.Name == group.Name);
                        foreach (var mr in group.Modules)
                        {
                            exists = targetGroup.Modules.Find((tmr) =>
                                tmr.Domain == mr.Domain && tmr.Address == mr.Address) != null;
                            if (!exists)
                            {
                                targetGroup.Modules.Add(mr);
                            }
                        }
                    }
                    homegenie.UpdateGroupsDatabase("");
                }
            }
            string scheduleDatabase = Path.Combine(repositoryFolder, packageData.Id, "schedules.xml");
            if (File.Exists(scheduleDatabase))
            {
                serializer = new XmlSerializer(typeof(List<SchedulerItem>));
                using (var reader = new StreamReader(scheduleDatabase))
                {
                    var pkgSchedules = (List<SchedulerItem>)serializer.Deserialize(reader);
                    foreach (var schedule in pkgSchedules)
                    {
                        homegenie.ProgramManager.SchedulerService.Remove(schedule.Name);
                        homegenie.ProgramManager.SchedulerService.Items.Add(schedule);
                    }
                    homegenie.UpdateSchedulerDatabase();
                }
            }
            return true;
        }

        public bool UninstallPackage(string packageFile)
        {
            var packageData = JsonConvert.DeserializeObject<PackageData>(File.ReadAllText(packageFile));
            foreach (var item in packageData.Programs)
            {
                var installedInstances = homegenie.ProgramManager.Programs
                    .FindAll((p) => p.PackageInfo.Repository == packageData.Repository && p.PackageInfo.PackageId == packageData.Id && p.PackageInfo.Id == item.Id);
                if (installedInstances.Count > 0)
                {
                    // remove programs
                    installedInstances.ForEach((p) =>
                    {
                        homegenie.ProgramManager.ProgramRemove(p);
                    });
                }
                // TODO: should also remove modules from groups?
                // TODO: should also remove groups and schedules?
            }
            return true;
        }

        public string CreatePackage(PackageData package)
        {
            string programsFile = Path.Combine(Utility.GetTmpFolder(), "programs.xml");
            string modulesFile = Path.Combine(Utility.GetTmpFolder(), "modules.xml");
            string groupsFile = Path.Combine(Utility.GetTmpFolder(), "groups.xml");
            string schedulesFile = Path.Combine(Utility.GetTmpFolder(), "schedules.xml");
            string packageFile = Path.Combine(Utility.GetTmpFolder(), "package.json");
            string bundleFile = Path.Combine(Utility.GetTmpFolder(), package.Id + "-" + package.Version + ".zip");
            try
            {
                // Clean-up
                File.Delete(programsFile);
                File.Delete(packageFile);
                File.Delete(bundleFile);
                // collect programs and modules
                bool saveProgramsRequired = false;
                var packagePrograms = new List<ProgramBlock>();
                var packageModules = new List<Module>();
                foreach (var item in package.Programs)
                {
                    if (item.Repository != package.Repository || item.PackageId != package.Id)
                    {
                        // item is an external dependency belonging to some other repository/package
                        continue;
                    }
                    var program = homegenie.ProgramManager.ProgramGet(int.Parse(item.Hid));
                    if (program != null)
                    {
                        saveProgramsRequired = true;
                        //if (program.PackageInfo.Repository == null)
                        {
                            program.PackageInfo.Repository = package.Repository;
                        }
                        //if (program.PackageInfo.PackageId == null)
                        {
                            program.PackageInfo.PackageId = package.Id;
                        }
                        // update package version only if repository/package id match
                        if (program.PackageInfo.Repository  == package.Repository && program.PackageInfo.PackageId == package.Id)
                        {
                            program.PackageInfo.PackageVersion = package.Version;
                        }
                        if (program.PackageInfo.Id == null)
                        {
                            program.PackageInfo.Id = item.Id;
                        }
                        program.PackageInfo.Version = item.Version;
                        program.PackageInfo.Required = item.Required;
                        item.Checksum = program.PackageInfo.Checksum = Utility.GetObjectChecksum(new
                        {
                            setup = program.ScriptSetup,
                            source = program.ScriptSource
                        });
                        packagePrograms.Add(program);
                        // lookup for modules belonging to this program
                        packageModules.AddRange(homegenie.Modules.FindAll((m) =>
                        {
                            var vm = Utility.ModuleParameterGet(m, Properties.VirtualModuleParentId);
                            return (m.Domain == Domains.HomeAutomation_HomeGenie && m.Address == program.Address.ToString())
                                   || (vm != null && vm.Value == program.Address.ToString());
                        }));
                    }
                }
                Utility.UpdateXmlDatabase(packagePrograms, programsFile, null);
                Utility.UpdateXmlDatabase(packageModules, modulesFile, null);
                // collect control groups
                var packageGroups = new List<Group>();
                foreach (var item in package.Groups)
                {
                    var group = homegenie.GetGroups("Control").Find((g) => g.Name == item.Hid);
                    if (group != null)
                    {
                        packageGroups.Add(group);
                    }
                }
                Utility.UpdateXmlDatabase(packageGroups, groupsFile, null);
                // collect schedules
                var packageSchedules = new List<SchedulerItem>();
                foreach (var item in package.Schedules)
                {
                    var schedule = homegenie.ProgramManager.SchedulerService.Get(item.Hid);
                    if (schedule != null)
                    {
                        packageSchedules.Add(schedule);
                    }
                }
                Utility.UpdateXmlDatabase(packageSchedules, schedulesFile, null);
                // add files to zip bundle
                File.WriteAllText(packageFile, JsonConvert.SerializeObject(package));
                Utility.AddFileToZip(bundleFile, packageFile,"package.json" );
                Utility.AddFileToZip(bundleFile, programsFile, "programs.xml");
                Utility.AddFileToZip(bundleFile, modulesFile,"modules.xml" );
                Utility.AddFileToZip(bundleFile, groupsFile,"groups.xml" );
                Utility.AddFileToZip(bundleFile, schedulesFile,"schedules.xml" );
                // move files to package folder in data/packages
                string packageFolder = Path.Combine(Utility.GetDataFolder(), "packages", package.Repository, package.Id);
                Utility.FolderCleanUp(packageFolder);
                File.Move(packageFile, Path.Combine(packageFolder, "package.json"));
                File.Move(programsFile, Path.Combine(packageFolder, "programs.xml"));
                File.Move(modulesFile, Path.Combine(packageFolder, "modules.xml"));
                File.Move(groupsFile, Path.Combine(packageFolder, "groups.xml"));
                File.Move(schedulesFile, Path.Combine(packageFolder, "schedules.xml"));
                // update programs db if required
                if (saveProgramsRequired)
                {
                    homegenie.UpdateProgramsDatabase();
                }
            }
            catch (Exception e)
            {
                homegenie.RaiseEvent(
                    Domains.HomeGenie_System,
                    Domains.HomeGenie_PackageInstaller,
                    SourceModule.Master,
                    "HomeGenie Package Installer",
                    Properties.InstallProgressMessage,
                    "= Error: " + e.Message
                );
                return null;
            }
            // TODO: cleanup temp files and folders
            return bundleFile;
        }

        public PackageData AddPackage(string archiveFileName)
        {
            // uncompress and verify package content
            string tempFolder = Path.Combine(Utility.GetTmpFolder(), Path.GetFileNameWithoutExtension(archiveFileName));
            Utility.FolderCleanUp(tempFolder);
            Utility.UncompressZip(archiveFileName, tempFolder);
            string packageJson = File.ReadAllText(Path.Combine(tempFolder, "package.json"));
            var packageData = JsonConvert.DeserializeObject<PackageData>(packageJson);
            // TODO: Data normalization and check
            string programsDatabase = Path.Combine(tempFolder, "programs.xml");
            var serializer1 = new XmlSerializer(typeof(List<ProgramBlock>));
            using (var reader = new StreamReader(programsDatabase))
            {
                // verify if package data checksum matches the one in programs.xml file
                var programs = (List<ProgramBlock>) serializer1.Deserialize(reader);
                foreach (var prg in packageData.Programs)
                {
                    // Get the program from programs.xml
                    var p1 = programs.Find((p) => p.Address.ToString() == prg.Hid);
                    if (p1 != null)
                    {
                        p1.PackageInfo.Checksum = Utility.GetObjectChecksum(new
                        {
                            setup = p1.ScriptSetup,
                            source = p1.ScriptSource
                        });
                        if (p1.PackageInfo.Checksum != prg.Checksum)
                        {
                            //TODO: Integrity check failed, should abort package importing
                            //Console.WriteLine(p1.PackageInfo.Checksum);
                            //Console.WriteLine(prg.Checksum);
                        }
                    }
                }
            }
            // copy the extracted package folder to the repository folder
            string repositoryFolder = Path.Combine(Utility.GetDataFolder(), "packages", packageData.Repository);
            if (!Directory.Exists(repositoryFolder))
            {
                Directory.CreateDirectory(repositoryFolder);
            }
            string pkgFolder = Path.Combine(repositoryFolder, packageData.Id);
            if (Directory.Exists(pkgFolder))
            {
                Directory.Delete(pkgFolder, true);
            }
            Directory.Move(tempFolder, pkgFolder);
            return packageData;
        }

        
        #endregion
        

        public bool InstallPackage(string pkgFolderUrl, string tempFolderPath)
        {
            string installFolder = Path.Combine(tempFolderPath, "pkg");
            dynamic pkgData = null;
            bool success = true;
            // Download package specs
            homegenie.RaiseEvent(
                Domains.HomeGenie_System,
                Domains.HomeGenie_PackageInstaller,
                SourceModule.Master,
                "HomeGenie Package Installer",
                Properties.InstallProgressMessage,
                "= Downloading: package.json"
            );
            using (var client = new WebClientPx())
            {
                try
                {
                    string pkgJson = "[" + client.DownloadString(pkgFolderUrl + "/package.json") + "]";
                    pkgData = (JsonConvert.DeserializeObject(pkgJson) as JArray)[0];
                }
                catch (Exception e)
                {
                    homegenie.RaiseEvent(
                        Domains.HomeGenie_System,
                        Domains.HomeGenie_PackageInstaller,
                        SourceModule.Master,
                        "HomeGenie Package Installer",
                        Properties.InstallProgressMessage,
                        "= ERROR: '" + e.Message + "'"
                    );
                    success = false;
                }
                client.Dispose();
            }
            // Download and install package files
            if (success && pkgData != null)
            {
                // Import Automation Programs in package
                foreach (var program in pkgData.programs)
                {
                    homegenie.RaiseEvent(
                        Domains.HomeGenie_System,
                        Domains.HomeGenie_PackageInstaller,
                        SourceModule.Master,
                        "HomeGenie Package Installer",
                        Properties.InstallProgressMessage,
                        "= Downloading: " + program.file.ToString()
                    );
                    Utility.FolderCleanUp(installFolder);
                    string programFile = Path.Combine(installFolder, program.file.ToString());
                    if (File.Exists(programFile))
                        File.Delete(programFile);
                    using (var client = new WebClientPx())
                    {
                        try
                        {
                            client.DownloadFile(pkgFolderUrl + "/" + program.file.ToString(), programFile);
                        }
                        catch (Exception e)
                        {
                            homegenie.RaiseEvent(
                                Domains.HomeGenie_System,
                                Domains.HomeGenie_PackageInstaller,
                                SourceModule.Master,
                                "HomeGenie Package Installer",
                                Properties.InstallProgressMessage,
                                "= ERROR: '" + e.Message + "'"
                            );
                            success = false;
                        }
                        client.Dispose();
                    }
                    if (success)
                    {
                        homegenie.RaiseEvent(
                            Domains.HomeGenie_System,
                            Domains.HomeGenie_PackageInstaller,
                            SourceModule.Master,
                            "HomeGenie Package Installer",
                            Properties.InstallProgressMessage,
                            "= Installing: " + program.name.ToString()
                        );
                        int pid = homegenie.ProgramManager.GeneratePid();
                        if (program.uid == null || !int.TryParse(program.uid.ToString(), out pid))
                            program.uid = pid;
                        // by default enable package programs after installing them
                        var enabled = true;
                        var oldProgram = homegenie.ProgramManager.ProgramGet(pid);
                        if (oldProgram != null)
                        {
                            homegenie.RaiseEvent(
                                Domains.HomeGenie_System,
                                Domains.HomeGenie_PackageInstaller,
                                SourceModule.Master,
                                "HomeGenie Package Installer",
                                Properties.InstallProgressMessage,
                                "= Replacing: '" + oldProgram.Name + "' with pid " + pid
                            );
                            // if the program was already installed, inherit IsEnabled
                            enabled = oldProgram.IsEnabled;
                            homegenie.ProgramManager.ProgramRemove(oldProgram);
                        }
                        var programBlock = ProgramImport(pid, programFile, program.group.ToString());
                        if (programBlock != null)
                        {
                            string groupName = programBlock.Group;
                            if (!String.IsNullOrWhiteSpace(groupName))
                            {
                                // Add automation program group if does not exist
                                Group newGroup = new Group() { Name = groupName };
                                if (homegenie.AutomationGroups.Find(g => g.Name == newGroup.Name) == null)
                                {
                                    homegenie.AutomationGroups.Add(newGroup);
                                    homegenie.UpdateGroupsDatabase("Automation");
                                }
                            }
                            programBlock.IsEnabled = enabled;
                            homegenie.RaiseEvent(
                                Domains.HomeGenie_System,
                                Domains.HomeGenie_PackageInstaller,
                                SourceModule.Master,
                                "HomeGenie Package Installer",
                                Properties.InstallProgressMessage,
                                "= Installed: '" + program.name.ToString() + "' as pid " + pid
                            );
                        }
                        else
                        {
                            // TODO: report error and stop the package install procedure
                            success = false;
                        }
                    }
                }
                // Import Widgets in package
                foreach (var widget in pkgData.widgets)
                {
                    homegenie.RaiseEvent(
                        Domains.HomeGenie_System,
                        Domains.HomeGenie_PackageInstaller,
                        SourceModule.Master,
                        "HomeGenie Package Installer",
                        Properties.InstallProgressMessage,
                        "= Downloading: " + widget.file.ToString()
                    );
                    Utility.FolderCleanUp(installFolder);
                    string widgetFile = Path.Combine(installFolder, widget.file.ToString());
                    if (File.Exists(widgetFile))
                        File.Delete(widgetFile);
                    using (var client = new WebClientPx())
                    {
                        try
                        {
                            client.DownloadFile(pkgFolderUrl + "/" + widget.file.ToString(), widgetFile);
                        }
                        catch (Exception e)
                        {
                            homegenie.RaiseEvent(
                                Domains.HomeGenie_System,
                                Domains.HomeGenie_PackageInstaller,
                                SourceModule.Master,
                                "HomeGenie Package Installer",
                                Properties.InstallProgressMessage,
                                "= ERROR: '" + e.Message + "'"
                            );
                            success = false;
                        }
                        client.Dispose();
                    }
                    if (success && WidgetImport(widgetFile, installFolder))
                    {
                        homegenie.RaiseEvent(
                            Domains.HomeGenie_System,
                            Domains.HomeGenie_PackageInstaller,
                            SourceModule.Master,
                            "HomeGenie Package Installer",
                            Properties.InstallProgressMessage,
                            "= Installed: '" + widget.name.ToString() + "'"
                        );
                    }
                    else
                    {
                        // TODO: report error and stop the package install procedure
                        success = false;
                    }
                }
                // Import MIG Interfaces in package
                foreach (var migface in pkgData.interfaces)
                {
                    homegenie.RaiseEvent(
                        Domains.HomeGenie_System,
                        Domains.HomeGenie_PackageInstaller,
                        SourceModule.Master,
                        "HomeGenie Package Installer",
                        Properties.InstallProgressMessage,
                        "= Downloading: " + migface.file.ToString()
                    );
                    Utility.FolderCleanUp(installFolder);
                    string migfaceFile = Path.Combine(installFolder, migface.file.ToString());
                    if (File.Exists(migfaceFile))
                        File.Delete(migfaceFile);
                    using (var client = new WebClientPx())
                    {
                        try
                        {
                            client.DownloadFile(pkgFolderUrl + "/" + migface.file.ToString(), migfaceFile);
                            Utility.UncompressZip(migfaceFile, installFolder);
                            File.Delete(migfaceFile);
                        }
                        catch (Exception e)
                        {
                            homegenie.RaiseEvent(
                                Domains.HomeGenie_System,
                                Domains.HomeGenie_PackageInstaller,
                                SourceModule.Master,
                                "HomeGenie Package Installer",
                                Properties.InstallProgressMessage,
                                "= ERROR: '" + e.Message + "'"
                            );
                            success = false;
                        }
                        client.Dispose();
                    }
                    if (success && InterfaceInstall(installFolder))
                    {
                        homegenie.RaiseEvent(
                            Domains.HomeGenie_System,
                            Domains.HomeGenie_PackageInstaller,
                            SourceModule.Master,
                            "HomeGenie Package Installer",
                            Properties.InstallProgressMessage,
                            "= Installed: '" + migface.name.ToString() + "'"
                        );
                    }
                    else
                    {
                        // TODO: report error and stop the package install procedure
                        success = false;
                    }
                }
            }
            else
            {
                success = false;
            }
            if (success)
            {
                pkgData.folder_url = pkgFolderUrl;
                pkgData.install_date = DateTime.UtcNow;
                AddInstalledPackage(pkgData);
                homegenie.RaiseEvent(
                    Domains.HomeGenie_System,
                    Domains.HomeGenie_PackageInstaller,
                    SourceModule.Master,
                    "HomeGenie Package Installer",
                    Properties.InstallProgressMessage,
                    "= Status: Package Install Successful"
                );
            }
            else
            {
                homegenie.RaiseEvent(
                    Domains.HomeGenie_System,
                    Domains.HomeGenie_PackageInstaller,
                    SourceModule.Master,
                    "HomeGenie Package Installer",
                    Properties.InstallProgressMessage,
                    "= Status: Package Install Error"
                );
            }

            return success;
        }

        public dynamic GetInstalledPackage(string pkgFolderUrl)
        {
            List<dynamic> pkgList = LoadInstalledPackages();
            return pkgList.Find(p => p.folder_url.ToString() == pkgFolderUrl);
        }

        public void AddInstalledPackage(dynamic pkgObject)
        {
            List<dynamic> pkgList = LoadInstalledPackages();
            pkgList.RemoveAll(p => p.folder_url.ToString() == pkgObject.folder_url.ToString());
            pkgList.Add(pkgObject);
            File.WriteAllText(PackageManager.PACKAGE_LIST_FILE, JsonConvert.SerializeObject(pkgList, Formatting.Indented));
        }

        public List<dynamic> LoadInstalledPackages()
        {
            List<dynamic> pkgList = new List<dynamic>();
            if (File.Exists(PackageManager.PACKAGE_LIST_FILE))
            {
                try
                {
                    pkgList = JArray.Parse(File.ReadAllText(PackageManager.PACKAGE_LIST_FILE)).ToObject<List<dynamic>>();
                }
                catch (Exception e)
                {
                    // TODO: report exception
                }
            }
            return pkgList;
        }

        public Interface GetInterfaceConfig(string configFile)
        {
            Interface iface = null;
            using (StreamReader ifaceReader = new StreamReader(configFile))
            {
                XmlSerializer ifaceSerializer = new XmlSerializer(typeof(Interface));
                iface = (Interface)ifaceSerializer.Deserialize(ifaceReader);
                ifaceReader.Close();
            }
            return iface;
        }

        public void AddWidgetMapping(string jsonMap)
        {
            /*
               // example widget mapping
               [
                  {
                      Description     : "Z-Wave.Me Floor Thermostat",
                      Widget          : "Bounz/Z-Wave.Me/thermostat",
                      MatchProperty   : "ZWaveNode.ManufacturerSpecific",
                      MatchValue      : "0115:0024:0001"
                  }
               ]
            */
            string mapConfigFile = "html/pages/control/widgets/configuration.json";
            var mapList = JArray.Parse(File.ReadAllText(mapConfigFile)).ToObject<List<dynamic>>();
            var widgetMap = JArray.Parse(jsonMap).ToObject<List<dynamic>>();
            try
            {
                foreach (var map in widgetMap)
                {
                    mapList.RemoveAll(m => m.MatchProperty.ToString() == map.MatchProperty.ToString() && m.MatchValue.ToString() == map.MatchValue.ToString());
                    mapList.Add(map);
                }
                File.WriteAllText(mapConfigFile, JsonConvert.SerializeObject(mapList, Formatting.Indented));
            }
            catch
            {
                // TODO: report exception
            }
        }

        public bool WidgetImport(string archiveFile, string importPath)
        {
            bool success = false;
            string widgetInfoFile = "widget.info";
            List<string> extractedFiles = Utility.UncompressZip(archiveFile, importPath);
            if (File.Exists(Path.Combine(importPath, widgetInfoFile)))
            {
                // Read "widget.info" and, if a mapping is present, add it to "html/pages/control/widgets/configuration.json"
                var mapping = File.ReadAllText(Path.Combine(importPath, widgetInfoFile));
                if (mapping.StartsWith("["))
                    AddWidgetMapping(mapping);
                foreach (string f in extractedFiles)
                {
                    // copy only files contained in sub-folders, avoid copying zip-root files
                    if (Path.GetDirectoryName(f) != "")
                    {
                        string destFolder = Path.Combine(widgetBasePath, Path.GetDirectoryName(f));
                        if (!Directory.Exists(destFolder))
                            Directory.CreateDirectory(destFolder);
                        File.Copy(Path.Combine(importPath, f), Path.Combine(widgetBasePath, f), true);
                    }
                }
                success = true;
            }
            return success;
        }

        public ProgramBlock ProgramImport(int newPid, string archiveName, string groupName)
        {
            ProgramBlock newProgram;
            char[] signature = new char[2];
            using (var reader = new StreamReader(archiveName))
            {
                reader.Read(signature, 0, 2);
            }
            if (signature[0] == 'P' && signature[1] == 'K')
            {
                // Read and uncompress zip file content (arduino program bundle)
                string zipFileName = archiveName.Replace(".hgx", ".zip");
                if (File.Exists(zipFileName))
                    File.Delete(zipFileName);
                File.Move(archiveName, zipFileName);
                string destFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Utility.GetTmpFolder(), "import");
                if (Directory.Exists(destFolder))
                    Directory.Delete(destFolder, true);
                Utility.UncompressZip(zipFileName, destFolder);
                string bundleFolder = Path.Combine("programs", "arduino", newPid.ToString());
                if (Directory.Exists(bundleFolder))
                    Directory.Delete(bundleFolder, true);
                if (!Directory.Exists(Path.Combine("programs", "arduino")))
                    Directory.CreateDirectory(Path.Combine("programs", "arduino"));
                Directory.Move(Path.Combine(destFolder, "src"), bundleFolder);
                archiveName = Path.Combine(destFolder, "program.hgx");
            }
            using (var reader = new StreamReader(archiveName))
            {
                var serializer = new XmlSerializer(typeof(ProgramBlock));
                newProgram = (ProgramBlock)serializer.Deserialize(reader);
            }
            newProgram.Address = newPid;
            newProgram.Group = groupName;
            homegenie.ProgramManager.ProgramAdd(newProgram);

            newProgram.IsEnabled = false;
            newProgram.ScriptErrors = "";
            newProgram.Engine.SetHost(homegenie);

            if (newProgram.Type.ToLower() != "arduino")
            {
                homegenie.ProgramManager.ProgramCompile(newProgram);
            }
            return newProgram;
        }

        public bool InterfaceInstall(string sourceFolder)
        {
            bool success = false;
            // install the interface package
            string configFile = Path.Combine(sourceFolder, "configuration.xml");
            var iface = GetInterfaceConfig(configFile);
            if (iface != null)
            {
                File.Delete(configFile);
                //
                // TODO: !IMPORTANT!
                // TODO: since AppDomains are not implemented in MIG, a RESTART is required to load the new Assembly
                // TODO: HG should ask for RESTART in the UI
                homegenie.MigService.RemoveInterface(iface.Domain);
                //
                string configletName = iface.Domain.Substring(iface.Domain.LastIndexOf(".") + 1).ToLower();
                string configletPath = Path.Combine("html", "pages", "configure", "interfaces", "configlet", configletName + ".html");
                if (File.Exists(configletPath))
                {
                    File.Delete(configletPath);
                }
                File.Move(Path.Combine(sourceFolder, "configlet.html"), configletPath);
                //
                string logoPath = Path.Combine("html", "images", "interfaces", configletName + ".png");
                if (File.Exists(logoPath))
                {
                    File.Delete(logoPath);
                }
                File.Move(Path.Combine(sourceFolder, "logo.png"), logoPath);
                // copy other interface files to mig folder (files and subfolders)
                string migFolder = Path.Combine("lib", "mig");
                DirectoryInfo dir = new DirectoryInfo(sourceFolder);
                // copy folders
                foreach (var d in dir.GetDirectories())
                {
                    string destFile = Path.Combine(migFolder, Path.GetFileName(d.FullName));
                    File.Move(d.FullName, destFile);
                }
                // copy root files ("soft" copy to prevent I/O errors overwriting in-use files)
                foreach (var f in dir.GetFiles())
                {
                    string destFile = Path.Combine(migFolder, Path.GetFileName(f.FullName));
                    if (File.Exists(destFile))
                    {
                        try { File.Delete(destFile + ".old"); } catch { }
                        try
                        {
                            File.Move(destFile, destFile + ".old");
                            File.Delete(destFile + ".old");
                        } catch  { }
                    }
                    File.Move(f.FullName, destFile);
                }
                //
                homegenie.SystemConfiguration.MigService.Interfaces.RemoveAll(i => i.Domain == iface.Domain);
                homegenie.SystemConfiguration.MigService.Interfaces.Add(iface);
                homegenie.SystemConfiguration.Update();
                homegenie.MigService.AddInterface(iface.Domain, iface.AssemblyName);

                success = true;
            }
            return success;
        }

    }
}
