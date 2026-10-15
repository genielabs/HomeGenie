using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Security.Cryptography;
using HomeGenie.Automation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HomeGenie.Service;

public partial class PackageManager
{
    private readonly object updateOperationGate = new();
    private readonly object updateInitializationGate = new();
    private ProgramUpdateJobs programUpdates;
    private volatile bool updatesStopping;

    public void InitializeProgramUpdates()
    {
        Updates().History();
        new ProgramUpdateReconciliation(Path.Combine(Utility.GetDataFolder(), "program-updates", "reconciliation"),
            GetProgramUpdates, Updates().StartRequired, id => Updates().Status(id))
            .Run(ProgramUpdateReconciliation.ReleaseIdentity(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "release_info.xml")));
    }
    public object GetProgramUpdateHistory() => Updates().History();
    public object CleanProgramUpdateBackups() => Updates().CleanSuccessfulBackups();
    public ProgramUpdateJob GetProgramUpdateJob(string id = null) => Updates().Status(id);
    public ProgramUpdateJob StartProgramUpdateJob(string fingerprint, string requestId, string retryOf = null) =>
        Updates().Start(fingerprint, requestId, retryOf);
    private ProgramUpdateJobs Updates()
    {
        lock (updateInitializationGate)
            return programUpdates ??= new ProgramUpdateJobs(Path.Combine(Utility.GetDataFolder(), "program-updates"),
                new UpdateStore(this), PublishProgramUpdate);
    }
    public void StopProgramUpdates() { updatesStopping = true; programUpdates?.Stop(); }

    private void PublishProgramUpdate(ProgramUpdateJob job)
    {
        if (job == null || updatesStopping) return;
        var archive = homegenie.ProgramManager.Programs.FindAll(p => p.PackageInfo?.Repository == "homegenie" && p.PackageInfo.PackageId == "homegenie-base" && p.PackageInfo.Id == "notification-archive")
            .OrderByDescending(p => p.IsEnabled).FirstOrDefault();
        if (archive == null) return;
        var payload = BuildProgramUpdateNotice(job);
        homegenie.RaiseEvent(this, "HomeAutomation.HomeGenie.Automation", archive.Address.ToString(), "Program updates", "Program.Notification", payload.ToString(Formatting.None));
    }

    public JObject GetProgramUpdateNotice() => BuildProgramUpdateNotice(GetProgramUpdateJob());
    public static JObject BuildProgramUpdateNotice(ProgramUpdateJob job)
    {
        if (job == null) return null;
        string message = job.State == "running" ? "Operations in progress" : job.Failed > 0 ? "Operation failed" : job.Skipped > 0 ? "Local version has been modified." : "The update was installed successfully.";
        string key = job.State == "running" ? "operations_in_progress" : job.Failed > 0 ? "operation_failed" : job.Skipped > 0 ? "local_version_modified" : "update_installed_successfully";
        string details = job.Processed + " / " + job.Items.Count + "\n\n" + string.Join("\n", job.Items.Select(i =>
            i.Repository + "/" + i.PackageId + "/" + i.Id + (i.Address.HasValue ? " : " + i.Address : "") + " — " + i.State + (i.Message == null ? "" : " : " + i.Message)));
        if (details.Length > 12000) details = details.Substring(0, char.IsHighSurrogate(details[11999]) ? 11999 : 12000) + "\n…";
        var payload = new JObject {
            ["Version"] = 1, ["Persistent"] = true, ["Id"] = "program-update-job-" + job.Id, ["Sequence"] = job.Sequence,
            ["Title"] = "Program updates", ["TitleKey"] = "programs",
            ["Message"] = message, ["MessageKey"] = key,
            ["Severity"] = job.State == "running" ? "info" : job.Failed > 0 || job.Skipped > 0 ? "warning" : "success",
            ["Details"] = details,
            ["Actions"] = new JArray(new JObject { ["Id"] = "show-updates", ["Type"] = "navigate", ["Destination"] = "program-updates",
                ["Label"] = "Show upgradable programs", ["LabelKey"] = "show_upgradable_programs" })
        };
        return payload;
    }

    private sealed class UpdateStore : IProgramUpdateStore
    {
        private readonly PackageManager owner;
        private HomeGenieService HG => owner.homegenie;
        public UpdateStore(PackageManager owner) { this.owner = owner; }
        public ProgramUpdateReport Inspect() => owner.GetProgramUpdates();
        private static string Segment(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "." || value == ".." || value.IndexOfAny(new[] {'/', '\\', '\0', ':'}) >= 0)
                throw new InvalidDataException("Invalid catalog identity.");
            return value;
        }
        private static string PackageFolder(string repository, string package) =>
            Path.Combine(Utility.GetDataFolder(), "packages", Segment(repository), Segment(package));
        public IReadOnlyList<PackageData> Catalog() => Directory.GetDirectories(Path.Combine(Utility.GetDataFolder(), "packages"))
            .SelectMany(repository => Directory.GetDirectories(repository))
            .Select(folder => JsonConvert.DeserializeObject<PackageData>(File.ReadAllText(Path.Combine(folder, "package.json")))).ToList();

        private sealed class Artifact { public ProgramBlock Program; public bool Enabled; }
        private Artifact Source(ProgramUpdateTarget target)
        {
            string folder = PackageFolder(target.Repository, target.PackageId);
            var catalog = JsonConvert.DeserializeObject<PackageData>(File.ReadAllText(Path.Combine(folder, "package.json")));
            if (catalog.Repository != target.Repository || catalog.Id != target.PackageId) throw new InvalidDataException("Catalog identity changed.");
            var item = catalog.Programs.Single(p => p.Repository == target.Repository && p.PackageId == target.PackageId && p.Id == target.Id);
            if (item.Version != target.Version || !string.Equals(item.Checksum, target.Checksum, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Catalog source changed. Refresh and confirm again.");
            // Deserialize only the selected artifact, and suppress its enabled setter.
            // Deserializing a whole enabled catalog would load unrelated runtime assemblies.
            var element = new XElement(XDocument.Load(Path.Combine(folder, "programs.xml")).Root.Elements("ProgramBlock")
                .Single(p => (string)p.Element("Address") == item.Hid));
            bool enabled = bool.TryParse((string)element.Element("IsEnabled"), out var packagedEnabled) && packagedEnabled;
            element.SetElementValue("IsEnabled", "false");
            using var reader = element.CreateReader();
            var source = (ProgramBlock)new XmlSerializer(typeof(ProgramBlock)).Deserialize(reader);
            if (source.PackageInfo.Repository != target.Repository || source.PackageInfo.PackageId != target.PackageId || source.PackageInfo.Id != target.Id ||
                !string.Equals(Hash(source), target.Checksum, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Program source does not match its manifest checksum or identity.");
            source.PackageInfo = new ProgramPackageInfo { Repository = target.Repository, PackageId = target.PackageId,
                PackageVersion = catalog.Version, Id = target.Id, Version = target.Version, Checksum = target.Checksum, Required = item.Required };
            return new Artifact { Program = source, Enabled = enabled };
        }
        public void ValidateSource(ProgramUpdateTarget target) => Source(target);
        private static string Hash(ProgramBlock p) => Utility.GetObjectChecksum(new { setup = p.ScriptSetup, source = p.ScriptSource, context = p.ScriptContext, data = p.Data });
        private static bool Same(ProgramBlock p, ProgramUpdateTarget t) => p.PackageInfo?.Repository == t.Repository && p.PackageInfo.PackageId == t.PackageId && p.PackageInfo.Id == t.Id;
        public bool DependencySatisfied(string repository, string package, string id, string version) => HG.ProgramManager.Programs.FindAll(p => p != null).Any(p =>
            p.PackageInfo?.Repository == repository && p.PackageInfo.PackageId == package && p.PackageInfo.Id == id &&
            ProgramUpdateChecker.TryCompareVersions(p.PackageInfo.Version, version, out int compare) && compare >= 0);

        private static void CopyCode(ProgramBlock to, ProgramBlock from)
        {
            to.Description = from.Description; to.ScriptSetup = from.ScriptSetup; to.ScriptSource = from.ScriptSource;
            to.ScriptContext = from.ScriptContext; to.Data = from.Data;
            to.PackageInfo = JsonConvert.DeserializeObject<ProgramPackageInfo>(JsonConvert.SerializeObject(from.PackageInfo));
        }
        private void Compile(ProgramBlock program)
        {
            var errors = HG.ProgramManager.ProgramCompile(program);
            if (errors?.Count > 0) throw new InvalidOperationException("Compilation failed: " + string.Join("; ", errors.Take(8).Select(e => e.ErrorMessage)));
            // CSharpEngine can log file-promotion errors without returning them.
            if ((program.Type.Equals("csharp", StringComparison.OrdinalIgnoreCase) || program.Type.Equals("visual", StringComparison.OrdinalIgnoreCase)) &&
                !File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs", program.Address + ".dll")))
                throw new IOException("Compiled assembly was not installed.");
        }

        private static bool ManagedBinary(ProgramBlock program) => program.Type.Equals("csharp", StringComparison.OrdinalIgnoreCase) || program.Type.Equals("visual", StringComparison.OrdinalIgnoreCase);
        private static string AssemblyPath(ProgramBlock program) => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs", program.Address + ".dll");
        private static string BinaryHash(string file) { using var stream = File.OpenRead(file); return Convert.ToHexString(SHA256.HashData(stream)); }
        private void Restore(ProgramBlock program, ProgramBlock backup, string backupFolder)
        {
            CopyCode(program, backup);
            if (ManagedBinary(program)) {
                string saved = Path.Combine(backupFolder, "assembly.dll");
                if (File.Exists(saved)) {
                    File.Copy(saved, AssemblyPath(program), true);
                    foreach (string suffix in new[] { ".dll.pdb", ".pdb" }) {
                        string original = Path.Combine(backupFolder, "assembly" + suffix);
                        string target = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs", program.Address + suffix);
                        if (File.Exists(original)) File.Copy(original, target, true); else if (File.Exists(target)) File.Delete(target);
                    }
                    return; // Restore the original binary, even if old source no longer compiles with this compiler.
                }
                // If no previous binary existed, do not let a failed file promotion reuse the new one.
                if (File.Exists(AssemblyPath(program))) File.Delete(AssemblyPath(program));
            }
            Compile(program);
        }

        public void Apply(ProgramUpdateTarget target, string backupFolder)
        {
            lock (owner.updateOperationGate)
            {
                var artifact = Source(target);
                var source = artifact.Program;
                bool isNew = target.PreviousVersion == null;
                var current = isNew ? null : HG.ProgramManager.ProgramGet(target.Address.Value);
                if (!isNew && (current == null || !Same(current, target) || current.PackageInfo.Version != target.PreviousVersion ||
                    current.IsEnabled != target.Enabled || !string.Equals(Hash(current), target.PreviousChecksum, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(current.PackageInfo.Checksum, target.PreviousChecksum, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Installed program changed since confirmation; skipped without overwriting it.");
                if (isNew && HG.ProgramManager.Programs.FindAll(p => Same(p, target)).Count > 0)
                    throw new InvalidOperationException("The missing program was already installed. Refresh before retrying.");

                if (!isNew && !string.Equals(current.Type, source.Type, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Program language changed; manual review is required.");
                Directory.CreateDirectory(backupFolder);
                target.Backup = Path.GetRelativePath(Utility.GetDataFolder(), backupFolder);
                ProgramBlock backup = null;
                bool enabled = isNew ? artifact.Enabled : current.IsEnabled;
                if (!isNew)
                {
                    using (var writer = new StringWriter()) {
                        new XmlSerializer(typeof(ProgramBlock)).Serialize(writer, current);
                        ProgramUpdateJobs.AtomicWrite(Path.Combine(backupFolder, "program.xml"), writer.ToString().Replace("encoding=\"utf-16\"", "encoding=\"utf-8\""));
                    }
                    backup = new ProgramBlock(); CopyCode(backup, current);
                    foreach (string suffix in new[] { ".dll", ".dll.pdb", ".pdb" })
                    {
                        var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs", current.Address + suffix);
                        if (File.Exists(file)) File.Copy(file, Path.Combine(backupFolder, "assembly" + suffix), true);
                    }
                }
                try
                {
                    if (isNew)
                    {
                      lock (HG.ProgramManager.Programs.LockObject) {
                        int address = source.Address;
                        bool Occupied(int id) => id <= 0 || HG.ProgramManager.ProgramGet(id) != null ||
                            HG.VirtualModules.Any(module => module.Address == id.ToString() || module.ParentId == id.ToString()) || Directory.Exists(Path.Combine(Utility.GetDataFolder(), "programs", id.ToString())) ||
                            File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programs", id + ".dll"));
                        if (Occupied(address)) { do { address = HG.ProgramManager.GeneratePid(); } while (Occupied(address)); }
                        source.IsEnabled = false; source.Address = address; target.Address = address; current = source;
                        HG.ProgramManager.ProgramAdd(current);
                      }
                    }
                    else { current.IsEnabled = false; CopyCode(current, source); }
                    Compile(current);
                    var savedAssembly = Path.Combine(backupFolder, "assembly.dll");
                    if (!isNew && ManagedBinary(current) && File.Exists(savedAssembly) &&
                        !string.Equals(target.PreviousChecksum, target.Checksum, StringComparison.OrdinalIgnoreCase) &&
                        BinaryHash(savedAssembly) == BinaryHash(AssemblyPath(current)))
                        throw new IOException("The compiler did not replace the previous program assembly.");
                    if (!ReferenceEquals(HG.ProgramManager.ProgramGet(current.Address), current) ||
                        !string.Equals(Hash(current), target.Checksum, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Program changed during compilation; update was not committed.");
                    current.IsEnabled = enabled;
                    if (!HG.UpdateProgramsDatabase()) throw new IOException("Cannot persist the updated programs database.");
                }
                catch (Exception error)
                {
                    if (current != null && !ReferenceEquals(HG.ProgramManager.ProgramGet(current.Address), current))
                        throw new InvalidOperationException(error.Message + " Program was removed or replaced concurrently; no rollback writes were attempted. Backup: " + target.Backup, error);
                    try
                    {
                        if (current != null)
                        {
                            current.IsEnabled = false;
                            if (isNew) HG.ProgramManager.ProgramRemove(current);
                            else { Restore(current, backup, backupFolder); current.IsEnabled = enabled; }
                            if (!HG.UpdateProgramsDatabase()) throw new IOException("Cannot persist rollback.");
                        }
                    }
                    catch (Exception rollback) { throw new InvalidOperationException(error.Message + " Rollback requires manual recovery from " + target.Backup + ": " + rollback.Message, error); }
                    throw new InvalidOperationException(error.Message + " Previous program restored; backup: " + target.Backup, error);
                }
            }
        }
    }
}
