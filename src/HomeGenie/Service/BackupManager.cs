/*
   Copyright 2012-2025 G-Labs (https://github.com/genielabs)

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

/*
*     Author: Generoso Martello <gene@homegenie.it>
*     Project Homepage: https://homegenie.it
*/

using System;
using System.IO;
using HomeGenie.Automation;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HomeGenie.Data;
using HomeGenie.Service.Constants;
using MIG.Config;
using Group = HomeGenie.Data.Group;

namespace HomeGenie.Service
{
    public class BackupManager
    {
        private HomeGenieService homegenie;
        private readonly Regex dataFilesPattern = new Regex(
            @"$(?<=\.(xml|json|config|cfg|db))",
            RegexOptions.IgnoreCase
        );

        public BackupManager(HomeGenieService hg)
        {
            homegenie = hg;
        }

        public void BackupConfiguration(string archiveName)
        {
            homegenie.UpdateProgramsDatabase();
            homegenie.UpdateGroupsDatabase("Automation");
            homegenie.UpdateGroupsDatabase("Control");
            homegenie.SaveData();
            if (File.Exists(archiveName))
            {
                File.Delete(archiveName);
            }
            // Add USER-SPACE program files (only for arduino-type programs)
            foreach (var program in homegenie.ProgramManager.Programs)
            {
                // TODO: deprecate Arduino??
                if (program.Type.ToLower() == "arduino" && (program.Address >= ProgramManager.USERSPACE_PROGRAMS_START && program.Address < ProgramManager.PACKAGE_PROGRAMS_START))
                {
                    string arduinoFolder = Path.Combine("programs", "arduino", program.Address.ToString());
                    string[] filePaths = Directory.GetFiles(arduinoFolder);
                    foreach (string f in filePaths)
                    {
                        Utility.AddFileToZip(archiveName, Path.Combine(arduinoFolder, Path.GetFileName(f)));
                    }
                }
            }
            // Add system config files
            Utility.AddFileToZip(archiveName, "systemconfig.xml");
            Utility.AddFileToZip(archiveName, "automationgroups.xml");
            Utility.AddFileToZip(archiveName, "modules.xml");
            Utility.AddFileToZip(archiveName, "programs.xml");
            Utility.AddFileToZip(archiveName, "scheduler.xml");
            Utility.AddFileToZip(archiveName, "groups.xml");
            if (File.Exists("release_info.xml"))
            {
                Utility.AddFileToZip(archiveName, "release_info.xml");
            }
            // Installed packages
            if (File.Exists(PackageManager.PACKAGE_LIST_FILE))
                Utility.AddFileToZip(archiveName, PackageManager.PACKAGE_LIST_FILE);
            // Add MIG Interfaces config/data files (lib/mig/*.xml)
            string migLibFolder = Path.Combine("lib", "mig");
            if (Directory.Exists(migLibFolder))
            {
                var files = Directory.EnumerateFiles(migLibFolder)
                    .Where(f => dataFilesPattern.IsMatch(f))
                    .ToList();
                foreach (string f in files)
                {
                    // exclude Pepper1 Db from backup (only the p1db_custom.xml file will be included)
                    // in the future the p1db.xml file should be moved to a different path
                    // TODO: "p1db.xml" is now deprecated, this check could be removed
                    if (Path.GetFileName(f) != "p1db.xml")
                        Utility.AddFileToZip(archiveName, f);
                }
            }
            // Backup files explicitly declared by programs using `Data.AddToSystemBackup(..)` method
            var programs = homegenie.ProgramManager.Programs
                .Where(p => p.BackupFiles.Count > 0);
            foreach (var p in programs)
            {
                p.BackupFiles.ForEach((bf) =>
                {
                    string relativePath = Utility.GetRelativePath(AppDomain.CurrentDomain.BaseDirectory, bf);
                    if (Directory.Exists(relativePath))
                    {
                        var directoryFiles = Directory.EnumerateFiles(bf, "*", SearchOption.AllDirectories);
                        foreach (string file in directoryFiles)
                        {
                            Utility.AddFileToZip(archiveName, file);
                        }
                    }
                    else if (File.Exists(relativePath))
                    {
                        Utility.AddFileToZip(archiveName, relativePath);
                    }
                });
            }
            // Backup custom widgets
            string widgetsFolder = Path.Combine(Utility.GetDataFolder(), "widgets");
            var widgetFiles = Directory.EnumerateFiles(widgetsFolder, "*", SearchOption.AllDirectories);
            foreach (string file in widgetFiles)
            {
                Utility.AddFileToZip(archiveName, file);
            }
        }

        public bool RestoreConfiguration(string archiveFolder, string selectedPrograms)
        {
            selectedPrograms = "," + selectedPrograms + ",";
            bool success = true;
            // Import automation groups
            List<Group> automationGroups;
            var serializer = new XmlSerializer(typeof(List<Group>));
            using (var reader = new StreamReader(Path.Combine(archiveFolder, "automationgroups.xml")))
            {
                automationGroups = (List<Group>)serializer.Deserialize(reader);
            }
            foreach (var automationGroup in automationGroups)
            {
                if (homegenie.AutomationGroups.Find(g => g.Name == automationGroup.Name) == null)
                {
                    homegenie.AutomationGroups.Add(automationGroup);
                    homegenie.RaiseEvent(
                        Domains.HomeGenie_System,
                        Domains.HomeGenie_BackupRestore,
                        SourceModule.Master,
                        "HomeGenie Backup Restore",
                        Properties.InstallProgressMessage,
                        "= Added: Automation Group '" + automationGroup.Name + "'"
                    );
                }
            }
            homegenie.UpdateGroupsDatabase("Automation");
            // Copy system configuration files
            File.Copy(Path.Combine(archiveFolder, "groups.xml"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "groups.xml"), true);
            homegenie.RaiseEvent(
                Domains.HomeGenie_System,
                Domains.HomeGenie_BackupRestore,
                SourceModule.Master,
                "HomeGenie Backup Restore",
                Properties.InstallProgressMessage,
                "= Restored: Control Groups"
            );
            File.Copy(Path.Combine(archiveFolder, "modules.xml"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "modules.xml"), true);
            homegenie.RaiseEvent(
                Domains.HomeGenie_System,
                Domains.HomeGenie_BackupRestore,
                SourceModule.Master,
                "HomeGenie Backup Restore",
                Properties.InstallProgressMessage,
                "= Restored: Modules"
            );
            File.Copy(Path.Combine(archiveFolder, "scheduler.xml"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scheduler.xml"), true);
            homegenie.RaiseEvent(
                Domains.HomeGenie_System,
                Domains.HomeGenie_BackupRestore,
                SourceModule.Master,
                "HomeGenie Backup Restore",
                Properties.InstallProgressMessage,
                "= Restored: Scheduler Events"
            );
            // Remove all old non-system programs
            var rp = new List<ProgramBlock>();
            foreach (var program in homegenie.ProgramManager.Programs)
            {
                if (program.Address >= ProgramManager.USERSPACE_PROGRAMS_START)
                    rp.Add(program);
            }
            foreach (var program in rp)
            {
                homegenie.ProgramManager.ProgramRemove(program);
                homegenie.RaiseEvent(
                    Domains.HomeGenie_System,
                    Domains.HomeGenie_BackupRestore,
                    SourceModule.Master,
                    "HomeGenie Backup Restore",
                    Properties.InstallProgressMessage,
                    "= Removed: Program '" + program.Name + "' (" + program.Address + ")"
                );
            }
            // Restore installed packages
            if (File.Exists(Path.Combine(archiveFolder, PackageManager.PACKAGE_LIST_FILE)))
            {
                File.Copy(Path.Combine(archiveFolder, PackageManager.PACKAGE_LIST_FILE), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PackageManager.PACKAGE_LIST_FILE), true);
                // Restore packages from "installed_packages.json"
                List<dynamic> pkgList = homegenie.PackageManager.LoadInstalledPackages();
                foreach (var pkg in pkgList)
                {
                    bool installed;
                    string packageId = "";
                    if (pkg.folder_url != null)
                    {
                        // TODO: old package format --- to be deprecated
                        string installFolder = Path.Combine(archiveFolder, "pkg");
                        packageId = pkg.folder_url.ToString();
                        installed = homegenie.PackageManager.InstallPackageOld(packageId, installFolder);
                        success = success && installed;
                    }
                    else
                    {
                        packageId = pkg.repository.ToString() + "/" + pkg.package.ToString();
                        installed = homegenie.PackageManager.InstallPackage(pkg.repository.ToString(), pkg.package.ToString());
                        success = success && installed;
                    }
                    homegenie.RaiseEvent(
                        Domains.HomeGenie_System,
                        Domains.HomeGenie_BackupRestore,
                        SourceModule.Master,
                        "HomeGenie Backup Restore",
                        Properties.InstallProgressMessage,
                        installed ? "= Restored: Package '" + packageId + "'"
                            : "= Error: Could not restore package '" + packageId + "'"
                    );
                }
            }
            // Update program database after package restore
            homegenie.UpdateProgramsDatabase();
            // Update system config
            UpdateSystemConfig(archiveFolder, homegenie.SystemConfiguration.MigService.Gateways);



            #region OLD_CODE for compatibility with HG 1.3 -- TO BE DEPRECATED
            // Remove old MIG Interfaces config/data files (data/mig/*.xml)
            string migLibFolder = Path.Combine(Utility.GetDataFolder(), "mig");
            if (Directory.Exists(migLibFolder))
            {
                var files = Directory.EnumerateFiles(migLibFolder)
                    .Where(f => dataFilesPattern.IsMatch(f))
                    .ToList();
                foreach (string f in files)
                {
                    File.Delete(f);
                    homegenie.RaiseEvent(
                        Domains.HomeGenie_System,
                        Domains.HomeGenie_BackupRestore,
                        SourceModule.Master,
                        "HomeGenie Backup Restore",
                        Properties.InstallProgressMessage,
                        "= Removed: MIG Data File '" + f + "'"
                    );
                }
            }
            // Restore MIG configuration/data files if present (from backup folder data/mig/*.xml)
            migLibFolder = Path.Combine(archiveFolder, "lib", "mig");
            if (Directory.Exists(migLibFolder))
            {
                var files = Directory.EnumerateFiles(migLibFolder)
                    .Where(f => dataFilesPattern.IsMatch(f))
                    .ToList();
                foreach (string f in files)
                {
                    string destinationFile = Path.Combine(Utility.GetDataFolder(), "mig", Path.GetFileName(f));
                    try
                    {
                        File.Copy(f, destinationFile, true);
                        homegenie.RaiseEvent(
                            Domains.HomeGenie_System,
                            Domains.HomeGenie_BackupRestore,
                            SourceModule.Master,
                            "HomeGenie Backup Restore",
                            Properties.InstallProgressMessage,
                            "= Restored: '" + destinationFile + "'"
                        );
                    }
                    catch (Exception e)
                    {
                        homegenie.RaiseEvent(
                            Domains.HomeGenie_System,
                            Domains.HomeGenie_BackupRestore,
                            SourceModule.Master,
                            "HomeGenie Backup Restore",
                            Properties.InstallProgressMessage,
                            "= Error: " + e.Message + "'"
                        );
                    }
                }
            }
            #endregion



            // Restore data folder files (programs' data, mig interfaces config files, and widgets)
            var dataFolders = new List<string> { "programs", "widgets", "mig" };
            foreach (var folder in dataFolders)
            {
                string backupDataFolder = Path.Combine(archiveFolder, Utility.GetDataFolder(), folder);
                if (Directory.Exists(backupDataFolder))
                {
                    string dataFolder = Path.Combine(Utility.GetDataFolder(), folder);
                    // TODO: should remove destination folder before copying from backup files?
                    foreach (string file in Directory.EnumerateFiles(backupDataFolder, "*", SearchOption.AllDirectories))
                    {
                        string destinationFolder = Path.GetDirectoryName(file).Replace(backupDataFolder, "").TrimStart('/').TrimStart('\\');
                        destinationFolder = Path.Combine(dataFolder, destinationFolder);
                        string destinationFile = Path.Combine(destinationFolder, Path.GetFileName(file)).TrimStart(Directory.GetDirectoryRoot(AppDomain.CurrentDomain.BaseDirectory).ToArray()).TrimStart('/').TrimStart('\\');
                        if (!String.IsNullOrWhiteSpace(destinationFolder) && !Directory.Exists(destinationFolder))
                        {
                            Directory.CreateDirectory(destinationFolder);
                        }
                        File.Copy(file, destinationFile, true);
                        homegenie.RaiseEvent(
                            Domains.HomeGenie_System,
                            Domains.HomeGenie_BackupRestore,
                            SourceModule.Master,
                            "HomeGenie Backup Restore",
                            Properties.InstallProgressMessage,
                            "= Restored: '" + destinationFile + "'"
                        );
                    }
                }
            }

            // Soft-reload system configuration from newly restored files and save config
            homegenie.SoftReload();
            // Restore user-space automation programs
            serializer = new XmlSerializer(typeof(List<ProgramBlock>));
            string programsDatabase = Path.Combine(archiveFolder, "programs.xml");

            List<ProgramBlock> newProgramsData;
            using (var reader = new StreamReader(programsDatabase))
            {
                newProgramsData = (List<ProgramBlock>)serializer.Deserialize(reader);
            }
            foreach (var program in newProgramsData)
            {
                var currentProgram = homegenie.ProgramManager.Programs.Find(p => p.Address == program.Address);
                program.IsRunning = false;
                // Only restore USER-SPACE PROGRAMS
                if (selectedPrograms.Contains("," + program.Address + ",") && (program.Address >= ProgramManager.USERSPACE_PROGRAMS_START && program.Address < ProgramManager.PACKAGE_PROGRAMS_START))
                {
                    if (currentProgram == null)
                    {
                        homegenie.ProgramManager.ProgramAdd(program);
                        homegenie.RaiseEvent(
                            Domains.HomeGenie_System,
                            Domains.HomeGenie_BackupRestore,
                            SourceModule.Master,
                            "HomeGenie Backup Restore",
                            Properties.InstallProgressMessage,
                            "= Added: Program '" + program.Name + "' (" + program.Address + ")"
                        );
                    }
                    else
                    {
                        homegenie.ProgramManager.ProgramRemove(currentProgram);
                        homegenie.ProgramManager.ProgramAdd(program);
                        homegenie.RaiseEvent(
                            Domains.HomeGenie_System,
                            Domains.HomeGenie_BackupRestore,
                            SourceModule.Master,
                            "HomeGenie Backup Restore",
                            Properties.InstallProgressMessage,
                            "= Replaced: Program '" + program.Name + "' (" + program.Address + ")"
                        );
                    }
                    // Restore Arduino program folder ...
                    // TODO: this is untested yet...
                    if (program.Type.ToLower() == "arduino")
                    {
                        string sourceFolder = Path.Combine(archiveFolder, "programs", "arduino", program.Address.ToString());
                        string arduinoFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs", "arduino", program.Address.ToString());
                        if (Directory.Exists(arduinoFolder))
                            Directory.Delete(arduinoFolder, true);
                        Directory.CreateDirectory(arduinoFolder);
                        foreach (string newPath in Directory.GetFiles(sourceFolder))
                        {
                            File.Copy(newPath, newPath.Replace(sourceFolder, arduinoFolder), true);
                        }
                    }
                    else
                    {
                        homegenie.ProgramManager.ProgramCompile(program);
                    }
                }
                else if (currentProgram != null && (program.Address < ProgramManager.USERSPACE_PROGRAMS_START || program.Address >= ProgramManager.PACKAGE_PROGRAMS_START))
                {
                    // Only restore Enabled/Disabled status for SYSTEM PROGRAMS and packages
                    currentProgram.IsEnabled = program.IsEnabled;
                }
            }
            homegenie.UpdateProgramsDatabase();
            homegenie.RaiseEvent(
                Domains.HomeGenie_System,
                Domains.HomeGenie_BackupRestore,
                SourceModule.Master,
                "HomeGenie Backup Restore",
                Properties.InstallProgressMessage,
                "= Status: Backup Restore " + (success ? "Successful" : "Errors")
            );
            homegenie.VirtualModules.Clear();
            homegenie.SaveData();

            return success;
        }

        public bool UpdateSystemConfig(string backupConfigPath, List<Gateway>  gateways)
        {
            SystemConfiguration systemConfiguration;
            try
            {
                var backupConfigFile = Path.Combine(backupConfigPath, "systemconfig.xml");
                var serializer = new XmlSerializer(typeof(SystemConfiguration));
                using (var reader = new StreamReader(backupConfigFile))
                {
                    systemConfiguration = (SystemConfiguration)serializer.Deserialize(reader);
                    systemConfiguration.MigService.Gateways = gateways;
                }
                string systemConfigFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "systemconfig.xml");
                var ws = new System.Xml.XmlWriterSettings();
                ws.Indent = true;
                ws.Encoding = Encoding.UTF8;
                XmlSerializer x = new XmlSerializer(systemConfiguration.GetType());
                using (var wri = System.Xml.XmlWriter.Create(systemConfigFile, ws))
                {
                    x.Serialize(wri, systemConfiguration);
                }
                return true;
            }
            catch
            {
                // TODO: report error
            }
            return false;
        }
    }
}
