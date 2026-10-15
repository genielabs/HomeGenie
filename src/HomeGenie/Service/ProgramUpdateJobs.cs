using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace HomeGenie.Service;

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class ProgramUpdateTarget
{
    public string Repository { get; set; }
    public string PackageId { get; set; }
    public string Id { get; set; }
    public int? Address { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string Checksum { get; set; }
    public string PreviousVersion { get; set; }
    public string PreviousChecksum { get; set; }
    public bool? Enabled { get; set; }
    public string State { get; set; } = "pending";
    public string Message { get; set; }
    public string Backup { get; set; }
    public bool ManualRecoveryRequired { get; set; }
    public string Key => Repository + "/" + PackageId + "/" + Id + "/" + (PreviousVersion == null ? "new" : Address?.ToString());
}

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class ProgramUpdateJob
{
    public string Id { get; set; }
    public string RequestId { get; set; }
    public string Fingerprint { get; set; }
    public string RetryOf { get; set; }
    public string State { get; set; } = "running";
    public string CreatedAt { get; set; }
    public string UpdatedAt { get; set; }
    public long Sequence { get; set; }
    public List<ProgramUpdateTarget> Items { get; set; } = new();
    public int Completed => Items.Count(i => i.State == "completed");
    public int Failed => Items.Count(i => i.State is "failed" or "blocked" or "interrupted");
    public int Skipped => Items.Count(i => i.State == "skipped");
    public int Processed => Items.Count(i => i.State is not ("pending" or "running"));
}

public interface IProgramUpdateStore
{
    ProgramUpdateReport Inspect();
    IReadOnlyList<PackageData> Catalog();
    void ValidateSource(ProgramUpdateTarget target);
    bool DependencySatisfied(string repository, string package, string id, string version);
    void Apply(ProgramUpdateTarget target, string backupFolder);
}

// One serial worker and a flushed journal. Recovery never replays an ambiguous write.
public sealed partial class ProgramUpdateJobs
{
    private readonly object gate = new();
    private readonly string folder;
    private readonly IProgramUpdateStore store;
    private readonly Action<ProgramUpdateJob> publish;
    private Task worker;
    private bool stopping;
    private ProgramUpdateJob current;

    public ProgramUpdateJobs(string folder, IProgramUpdateStore store, Action<ProgramUpdateJob> publish)
    {
        this.folder = folder; this.store = store; this.publish = publish;
        Directory.CreateDirectory(folder);
        var latest = Path.Combine(folder, "latest.json");
        if (!File.Exists(latest)) return;
        current = JsonConvert.DeserializeObject<ProgramUpdateJob>(File.ReadAllText(latest));
        if (current == null || !Guid.TryParseExact(current.Id, "N", out _) || current.Items == null ||
            !new[] { "running", "completed", "review", "partial", "interrupted" }.Contains(current.State) || current.Items.Any(i => i == null))
            throw new InvalidDataException("Invalid program update journal; manual recovery is required.");
        if (current.State == "running")
        {
            foreach (var item in current.Items.Where(i => i.State is "pending" or "running"))
            { item.State = "interrupted"; item.Message = "Service stopped before this result was confirmed. Review current state before retrying."; }
            current.State = "interrupted"; Save(); Notify(current);
        }
    }

    public bool IsRunning { get { lock (gate) return current?.State == "running"; } }
    public ProgramUpdateJob Status(string id = null)
    {
        lock (gate) {
            if (id == null || id == current?.Id) return Copy(current);
            if (!Guid.TryParseExact(id, "N", out _)) throw new InvalidOperationException("Invalid job id.");
            var file = Path.Combine(folder, id + ".json");
            if (!File.Exists(file)) throw new InvalidOperationException("Program update job not found.");
            var historical = JsonConvert.DeserializeObject<ProgramUpdateJob>(File.ReadAllText(file));
            if (historical.State == "running") historical.State = "interrupted";
            return historical;
        }
    }
    private static ProgramUpdateJob Copy(ProgramUpdateJob job) => job == null ? null :
        JsonConvert.DeserializeObject<ProgramUpdateJob>(JsonConvert.SerializeObject(job));

    public ProgramUpdateJob Start(string fingerprint, string requestId, string retryOf = null)
    {
        lock (gate)
        {
            if (stopping) throw new InvalidOperationException("Service is stopping.");
            MaintainBackups();
            if (!Guid.TryParse(requestId, out var requestUuid)) throw new InvalidOperationException("A request UUID is required.");
            requestId = requestUuid.ToString("N");
            var requestFile = Path.Combine(folder, "requests", requestId + ".json");
            if (File.Exists(requestFile)) {
                var previousId = File.ReadAllText(requestFile);
                var previousJob = Status(previousId);
                if (previousJob.Fingerprint != fingerprint || previousJob.RetryOf != retryOf) throw new InvalidOperationException("Request UUID was already used with different inputs.");
                return previousJob;
            }
            if (current?.RequestId == requestId)
            {
                if (current.Fingerprint != fingerprint || current.RetryOf != retryOf) throw new InvalidOperationException("Request UUID was already used with different inputs.");
                return Copy(current);
            }
            if (current?.State == "running") throw new InvalidOperationException("A program update job is already running.");
            if (retryOf != null && (current?.Id != retryOf || current.Failed == 0)) throw new InvalidOperationException("No retryable job matches this request.");
            var report = store.Inspect();
            if (!report.Complete || report.Fingerprint != fingerprint) throw new InvalidOperationException("The update report changed or is incomplete. Refresh and confirm again.");
            var catalog = store.Catalog();
            var targets = Plan(report);
            if (retryOf != null)
            {
                var retryKeys = current.Items.Where(i => i.State is "failed" or "blocked" or "interrupted").Select(i => i.Key).ToHashSet();
                targets = targets.Where(t => retryKeys.Contains(t.Key)).ToList();
            }
            // Preflight every artifact before mutating any installed program.
            foreach (var target in targets.Where(t => t.State == "pending")) store.ValidateSource(target);
            targets = Order(targets, catalog);
            var previous = current;
            current = new ProgramUpdateJob { Id = Guid.NewGuid().ToString("N"), RequestId = requestId,
                Fingerprint = fingerprint, RetryOf = retryOf, CreatedAt = DateTime.UtcNow.ToString("O"), Items = targets };
            try { Save(); AtomicWrite(requestFile, current.Id); } catch { current = previous; throw; }
            var accepted = current;
            worker = Task.Run(() => Run(accepted, catalog));
            return Copy(current);
        }
    }

    public static List<ProgramUpdateTarget> Plan(ProgramUpdateReport report)
    {
        var result = new List<ProgramUpdateTarget>();
        foreach (var item in report.Items)
        {
            ProgramUpdateTarget Target() => new() { Repository = item.Repository, PackageId = item.PackageId,
                Id = item.Id, Version = item.AvailableVersion, Checksum = item.AvailableChecksum };
            foreach (int address in item.UpdateAddresses)
            {
                var installed = item.Instances.Single(p => p.Address == address);
                var target = Target(); target.Address = address; target.Name = installed.Name;
                target.PreviousVersion = installed.Version; target.PreviousChecksum = installed.ActualChecksum; target.Enabled = installed.Enabled;
                if (!item.LocalChanges.TryGetValue(address, out var changed) || changed != false)
                { target.State = "skipped"; target.Message = "Local code is modified or its baseline is unknown. Manual review is required."; }
                result.Add(target);
            }
            if (item.Instances.Count == 0 && item.Reasons.Any(r => r is "missingRequired" or "missingDependency"))
            { var target = Target(); target.Name = item.Id; result.Add(target); }
        }
        return result;
    }

    private static IEnumerable<PackageDataItem> Requirements(ProgramUpdateTarget target, IReadOnlyList<PackageData> catalog) =>
        catalog.Single(p => p.Repository == target.Repository && p.Id == target.PackageId).Programs
            .Where(p => p.Required && (p.Repository != target.Repository || p.PackageId != target.PackageId));

    public static List<ProgramUpdateTarget> Order(List<ProgramUpdateTarget> targets, IReadOnlyList<PackageData> catalog)
    {
        var result = new List<ProgramUpdateTarget>(); var visiting = new HashSet<string>(); var visited = new HashSet<string>();
        void Visit(ProgramUpdateTarget target)
        {
            if (visited.Contains(target.Key)) return;
            if (!visiting.Add(target.Key)) throw new InvalidOperationException("Program update dependency cycle.");
            foreach (var requirement in Requirements(target, catalog))
            foreach (var dependency in targets.Where(t => t.Repository == requirement.Repository && t.PackageId == requirement.PackageId && t.Id == requirement.Id)) Visit(dependency);
            visiting.Remove(target.Key); visited.Add(target.Key); result.Add(target);
        }
        foreach (var target in targets) Visit(target);
        return result;
    }

    private void Run(ProgramUpdateJob job, IReadOnlyList<PackageData> catalog)
    {
        try
        {
            Notify(job);
            foreach (var target in job.Items.Where(i => i.State == "pending"))
            {
                lock (gate)
                {
                    if (stopping) { target.State = "interrupted"; target.Message = "Service is stopping."; Save(job); continue; }
                    var unmet = Requirements(target, catalog).FirstOrDefault(r => !store.DependencySatisfied(r.Repository, r.PackageId, r.Id, r.Version));
                    if (unmet != null) { target.State = "blocked"; target.Message = "Unsatisfied dependency: " + unmet.Repository + "/" + unmet.PackageId + "/" + unmet.Id; Save(job); Notify(job); continue; }
                    target.State = "running";
                    target.Backup = Path.GetRelativePath(Path.GetDirectoryName(folder), Path.Combine(folder, job.Id, job.Items.IndexOf(target).ToString()));
                    Save(job);
                }
                Notify(job);
                try
                {
                    store.ValidateSource(target);
                    var backup = Path.Combine(folder, job.Id, job.Items.IndexOf(target).ToString());
                    store.Apply(target, backup);
                    lock (gate) { target.State = "completed"; target.Message = null; Save(job); }
                }
                catch (Exception error) { lock (gate) { target.State = "failed"; target.Message = error.Message; target.ManualRecoveryRequired = error.Message.Contains("manual recovery", StringComparison.OrdinalIgnoreCase) || error.Message.Contains("concurrently", StringComparison.OrdinalIgnoreCase); Save(job); } }
                Notify(job);
            }
            lock (gate) { job.State = job.Items.Any(item => item.State == "interrupted") ? "interrupted" : job.Failed > 0 ? "partial" : job.Skipped > 0 ? "review" : "completed"; Save(job); }
            Notify(job);
        }
        catch (Exception error)
        {
            lock (gate)
            {
                foreach (var item in job.Items.Where(i => i.State is "pending" or "running")) { item.State = "interrupted"; item.Message = "Update journal or execution failed: " + error.Message; }
                job.State = "interrupted";
                try { Save(job); } catch { /* Previous flushed journal remains recoverable; never replay automatically. */ }
            }
            Notify(job);
        }
    }

    private void Save(ProgramUpdateJob job = null)
    {
        job ??= current;
        job.UpdatedAt = DateTime.UtcNow.ToString("O");
        job.Sequence++;
        string json = JsonConvert.SerializeObject(job);
        AtomicWrite(Path.Combine(folder, job.Id + ".json"), json);
        if (ReferenceEquals(current, job)) AtomicWrite(Path.Combine(folder, "latest.json"), json);
    }
    internal static void AtomicWrite(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (var stream = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
        using (var writer = new StreamWriter(stream)) { writer.Write(text); writer.Flush(); stream.Flush(true); }
        File.Move(path + ".tmp", path, true);
    }
    private void Notify(ProgramUpdateJob job) {
        try { ProgramUpdateJob snapshot; lock (gate) snapshot = Copy(job); publish?.Invoke(snapshot); }
        catch { /* Notification delivery must not change installation results. */ }
    }

    public void Stop()
    {
        Task pending;
        lock (gate) { stopping = true; pending = worker; }
        // Finish the current transaction before HG saves/disposes its program inventory.
        pending?.GetAwaiter().GetResult();
    }
}
