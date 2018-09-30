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
using HomeGenie.Automation;
using HomeGenie.Service.Logging;
using System.Xml.Serialization;
using System.Collections.Generic;
using HomeGenie.Data;
using HomeGenie.Service.Constants;
using MIG.Config;
using MIG;
using System.Text;

namespace HomeGenie.Service
{
    public class BackupManager
    {
        private HomeGenieService homegenie;

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
            Utility.AddFileToZip(archiveName, "release_info.xml");
            // Statistics db
            if (File.Exists(StatisticsLogger.STATISTICS_DB_FILE))
            {
                homegenie.Statistics.CloseStatisticsDatabase();
                Utility.AddFileToZip(archiveName, StatisticsLogger.STATISTICS_DB_FILE);
                homegenie.Statistics.OpenStatisticsDatabase();
            }
            // Installed packages
            if (File.Exists(PackageManager.PACKAGE_LIST_FILE))
                Utility.AddFileToZip(archiveName, PackageManager.PACKAGE_LIST_FILE);
            // Add MIG Interfaces config/data files (lib/mig/*.xml)
            string migLibFolder = Path.Combine("lib", "mig");
            if (Directory.Exists(migLibFolder))
            {
                foreach (string f in Directory.GetFiles(migLibFolder, "*.xml"))
                {
                    // exclude Pepper1 Db from backup (only the p1db_custom.xml file will be included)
                    // in the future the p1db.xml file should be moved to a different path 
                    if (Path.GetFileName(f) != "p1db.xml")
                        Utility.AddFileToZip(archiveName, f);
                }
            }
        }

        public bool RestoreConfiguration(string archiveFolder, string selectedPrograms)
        {
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
            // Statistics db
            if (File.Exists(Path.Combine(archiveFolder, StatisticsLogger.STATISTICS_DB_FILE)))
            {
                File.Copy(Path.Combine(archiveFolder, StatisticsLogger.STATISTICS_DB_FILE), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, StatisticsLogger.STATISTICS_DB_FILE), true);
                homegenie.RaiseEvent(
                    Domains.HomeGenie_System,
                    Domains.HomeGenie_BackupRestore,
                    SourceModule.Master,
                    "HomeGenie Backup Restore",
                    Properties.InstallProgressMessage,
                    "= Restored: Statistics Database"
                );
            }
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
                string installFolder = Path.Combine(archiveFolder, "pkg");
                List<dynamic> pkgList = homegenie.PackageManager.LoadInstalledPackages();
                foreach (var pkg in pkgList)
                {
                    success = success && homegenie.PackageManager.InstallPackage(pkg.folder_url.ToString(), installFolder);
                }
            }
            // Update program database after package restore
            homegenie.UpdateProgramsDatabase();
            // Update system config
            UpdateSystemConfig(archiveFolder);
            // Remove old MIG Interfaces config/data files (lib/mig/*.xml)
            string migLibFolder = Path.Combine("lib", "mig");
            if (Directory.Exists(migLibFolder))
            {
                foreach (string f in Directory.GetFiles(migLibFolder, "*.xml"))
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
            // Restore MIG configuration/data files if present (from backup folder lib/mig/*.xml)
            migLibFolder = Path.Combine(archiveFolder, "lib", "mig");
            if (Directory.Exists(migLibFolder))
            {
                foreach (string f in Directory.GetFiles(migLibFolder, "*.xml"))
                {
                    File.Copy(f, Path.Combine("lib", "mig", Path.GetFileName(f)), true);
                    homegenie.RaiseEvent(
                        Domains.HomeGenie_System,
                        Domains.HomeGenie_BackupRestore,
                        SourceModule.Master,
                        "HomeGenie Backup Restore",
                        Properties.InstallProgressMessage,
                        "= Restored: '" + Path.Combine("lib", "mig", Path.GetFileName(f)) + "'"
                    );
                }
            }
            // Soft-reload system configuration from newly restored files and save config
            homegenie.SoftReload();
            // Restore user-space automation programs
            serializer = new XmlSerializer(typeof(List<ProgramBlock>));
            string programsDatabase = Path.Combine(archiveFolder, "programs.xml");
            
            // TODO: Deprecate Compat
            Compat_526.FixProgramsDatabase(programsDatabase);

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
                        homegenie.ProgramManager.CompileScript(program);
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
            homegenie.SaveData();

            return success;
        }

        // Backward compatibility method for HG < 1.1
        private bool UpdateSystemConfig(string configPath)
        {
            string configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "systemconfig.xml");
            File.Copy(Path.Combine(configPath, "systemconfig.xml"), configFile, true);
            return true;
        }

    }
}

