using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace HomeGenie.Service;

public sealed partial class ProgramUpdateJobs
{
    private DateTime lastMaintenance;
    // Only directories derived from a validated journal ID and item index may be removed.
    // Never follow links, accept paths from item.Backup, or remove journals/request identities.
    private bool SafeTree(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) return false;
        return !Directory.Exists(path) || Directory.EnumerateFileSystemEntries(path).All(SafeTree);
    }
    private List<ProgramUpdateJob> Records(List<string> issues)
    {
        var records = new List<ProgramUpdateJob>();
        foreach (var file in Directory.GetFiles(folder, "*.json"))
        {
            string id = Path.GetFileNameWithoutExtension(file);
            if (id == "latest") continue;
            if (!Guid.TryParseExact(id, "N", out _)) { issues.Add("Unrecognized update record; its files were preserved."); continue; }
            try {
                var job = id == current?.Id ? Copy(current) : JsonConvert.DeserializeObject<ProgramUpdateJob>(File.ReadAllText(file));
                if (job?.Id != id || job.Items == null || job.Items.Any(i => i == null) ||
                    !new[] {"running","completed","partial","review","interrupted"}.Contains(job.State) ||
                    !DateTimeOffset.TryParse(job.UpdatedAt, out _)) throw new InvalidDataException("Invalid record.");
                records.Add(job);
            } catch { issues.Add("Cannot inspect update record " + id + "; its backups were preserved."); }
        }
        return records.OrderByDescending(j => j.CreatedAt).ToList();
    }
    private static bool Protected(ProgramUpdateTarget item) => item.ManualRecoveryRequired ||
        item.State is "pending" or "running" or "interrupted" ||
        (item.Message?.Contains("manual recovery", StringComparison.OrdinalIgnoreCase) ?? false) ||
        (item.Message?.Contains("concurrently", StringComparison.OrdinalIgnoreCase) ?? false);

    private void MaintainBackups()
    {
        if (DateTime.UtcNow - lastMaintenance < TimeSpan.FromHours(1) || current?.State == "running") return;
        // Cleanup failure must not prevent a program update or hide the read-only history.
        try { Clean(false, DateTimeOffset.UtcNow); lastMaintenance = DateTime.UtcNow; } catch { }
    }
    private void Clean(bool successfulOnly, DateTimeOffset now)
    {
        if (current?.State == "running") throw new InvalidOperationException("A program update job is already running.");
        var issues = new List<string>();
        if (!SafeTree(folder)) throw new InvalidDataException("Backup storage contains a symbolic link; cleanup was refused.");
        foreach (var job in Records(issues).Where(j => j.State != "running"))
        {
            if (!DateTimeOffset.TryParse(job.UpdatedAt, out var updated)) continue;
            var jobFolder = Path.Combine(folder, job.Id);
            if (!Directory.Exists(jobFolder)) continue;
            for (int index = 0; index < job.Items.Count; index++)
            {
                var item = job.Items[index];
                if (Protected(item) || successfulOnly && item.State != "completed") continue;
                if (!successfulOnly && now - updated < TimeSpan.FromDays(item.State == "completed" ? 7 : 30)) continue;
                string target = Path.GetFullPath(Path.Combine(jobFolder, index.ToString()));
                string expectedRoot = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!target.StartsWith(expectedRoot, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Invalid backup path.");
                if (Directory.Exists(target)) Directory.Delete(target, true);
            }
        }
    }
    public object History()
    {
        lock (gate) { MaintainBackups(); return HistoryCore(); }
    }
    public object CleanSuccessfulBackups()
    {
        lock (gate) { Clean(true, DateTimeOffset.UtcNow); return HistoryCore(); }
    }
    private object HistoryCore()
    {
        var issues = new List<string>();
        var records = Records(issues);
        long total = 0, removable = 0;
        var availableBackups = new List<string>();
        int protectedCount = 0;
        // Refuse link traversal for measurement too; report the limitation visibly.
        if (!SafeTree(folder)) issues.Add("Backup storage contains a symbolic link; measurement and cleanup were refused.");
        else foreach (var job in records)
        for (int index = 0; index < job.Items.Count; index++)
        {
            string path = Path.Combine(folder, job.Id, index.ToString());
            if (!Directory.Exists(path)) continue;
            availableBackups.Add(Path.GetRelativePath(Path.GetDirectoryName(folder), path));
            long bytes = Directory.EnumerateFiles(path,"*",SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);
            total += bytes;
            if (Protected(job.Items[index])) protectedCount++;
            else if (job.State != "running" && job.Items[index].State == "completed") removable += bytes;
        }
        return new { Status = "Ok", jobs = records.Take(50), totalJobs = records.Count, backupBytes = total,
            removableBytes = removable, protectedBackups = protectedCount, availableBackups, issues,
            retention = new { successfulDays = 7, failedDays = 30 } };
    }
}
