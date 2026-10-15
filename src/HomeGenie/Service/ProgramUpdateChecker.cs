using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace HomeGenie.Service
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class InstalledProgramSnapshot
    {
        public string Repository { get; set; }
        public string PackageId { get; set; }
        public string Id { get; set; }
        public int Address { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public bool Enabled { get; set; }
        public string BaselineChecksum { get; set; }
        public string ActualChecksum { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class InstalledPackageIdentity
    {
        public string Repository { get; set; }
        public string Id { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ProgramUpdateIssue
    {
        public string Code { get; set; }
        public string Identity { get; set; }
        public string Message { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ProgramUpdateItem
    {
        public string Repository { get; set; }
        public string PackageId { get; set; }
        public string Id { get; set; }
        public string PackageVersion { get; set; }
        public string AvailableVersion { get; set; }
        public string AvailableChecksum { get; set; }
        public string SuggestedAddress { get; set; }
        public bool Required { get; set; }
        public List<string> Reasons { get; set; } = new List<string>();
        public List<InstalledProgramSnapshot> Instances { get; set; } = new List<InstalledProgramSnapshot>();
        public List<int> UpdateAddresses { get; set; } = new List<int>();
        // null means that a trustworthy baseline is unavailable, not "unmodified".
        public Dictionary<int, bool?> LocalChanges { get; set; } = new Dictionary<int, bool?>();
        public List<string> RequiredBy { get; set; } = new List<string>();
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ProgramUpdateReport
    {
        [JsonProperty("Status")]
        public string Status => Issues.Count == 0 ? "Ok" : "Error";
        public int SchemaVersion => 1;
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        public bool Complete => Issues.Count == 0;
        public string Fingerprint => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { Items, Issues }))));
        public int UpdateCount => Items.Sum(i => i.UpdateAddresses.Count);
        public int AffectedProgramCount => Items.Count;
        public int ReviewCount => Items.Count(i => i.UpdateAddresses.Any(address => i.LocalChanges[address] != false));
        public int MissingRequiredCount => Items.Count(i => i.Reasons.Contains("missingRequired"));
        public int MissingDependencyCount => Items.Count(i => i.Reasons.Contains("missingDependency"));
        public List<ProgramUpdateItem> Items { get; set; } = new List<ProgramUpdateItem>();
        public List<ProgramUpdateIssue> Issues { get; set; } = new List<ProgramUpdateIssue>();
    }

    /// <summary>Read-only update analysis. Never installs, compiles or enables programs.</summary>
    public static class ProgramUpdateChecker
    {
        private static string Key(string repository, string package) => repository + "/" + package;
        private static string Key(PackageDataItem item) => Key(item.Repository, item.PackageId) + "/" + item.Id;
        private static bool Valid(string value) => !string.IsNullOrWhiteSpace(value) &&
            value != "." && value != ".." && value.IndexOfAny(new[] { '/', '\\', '\0' }) < 0;
        private static bool HasIdentity(PackageDataItem item) => item != null &&
            Valid(item.Repository) && Valid(item.PackageId) && Valid(item.Id);

        public static ProgramUpdateReport Check(IEnumerable<PackageData> catalog,
            IEnumerable<InstalledProgramSnapshot> installedPrograms,
            IEnumerable<InstalledPackageIdentity> installedPackages)
        {
            var report = new ProgramUpdateReport();
            void Issue(string code, string identity, string message) => report.Issues.Add(
                new ProgramUpdateIssue { Code = code, Identity = identity, Message = message });
            var programs = (installedPrograms ?? Array.Empty<InstalledProgramSnapshot>()).Where(p => p != null)
                .Select(p => new InstalledProgramSnapshot {
                    Repository = p.Repository, PackageId = p.PackageId, Id = p.Id, Address = p.Address,
                    Name = p.Name, Version = p.Version, Enabled = p.Enabled,
                    BaselineChecksum = p.BaselineChecksum, ActualChecksum = p.ActualChecksum
                }).ToList();
            var packages = new Dictionary<string, PackageData>(StringComparer.Ordinal);
            var entries = new Dictionary<string, PackageDataItem>(StringComparer.Ordinal);
            var duplicatePackages = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pkg in catalog ?? Array.Empty<PackageData>())
            {
                if (pkg == null || !Valid(pkg.Repository) || !Valid(pkg.Id) || pkg.Programs == null)
                { Issue("invalidPackage", "", "Package identity or program catalog is missing."); continue; }
                var key = Key(pkg.Repository, pkg.Id);
                if (!packages.TryAdd(key, pkg))
                { duplicatePackages.Add(key); Issue("duplicatePackage", key, "Ambiguous package identity."); }
            }
            foreach (var duplicate in duplicatePackages) packages.Remove(duplicate);
            foreach (var pkg in packages.Values)
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var item in pkg.Programs)
                {
                    if (!HasIdentity(item)) { Issue("invalidProgram", Key(pkg.Repository, pkg.Id), "Program identity is missing or invalid."); continue; }
                    if (item.Repository != pkg.Repository || item.PackageId != pkg.Id) continue;
                    if (!seen.Add(Key(item)))
                    { entries.Remove(Key(item)); Issue("duplicateProgram", Key(item), "Ambiguous program identity."); continue; }
                    entries.Add(Key(item), item);
                }
            }

            var instances = programs.Where(p => Valid(p.Repository) && Valid(p.PackageId) && Valid(p.Id))
                .GroupBy(p => Key(p.Repository, p.PackageId) + "/" + p.Id)
                .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Address).ToList(), StringComparer.Ordinal);
            var active = new HashSet<string>(programs.Where(p => Valid(p.Repository) && Valid(p.PackageId))
                .Select(p => Key(p.Repository, p.PackageId)), StringComparer.Ordinal);
            foreach (var pkg in installedPackages ?? Array.Empty<InstalledPackageIdentity>())
                if (pkg != null && Valid(pkg.Repository) && Valid(pkg.Id)) active.Add(Key(pkg.Repository, pkg.Id));
                else Issue("invalidInstalledPackage", "", "Installed package identity is invalid.");

            var candidates = new Dictionary<string, ProgramUpdateItem>(StringComparer.Ordinal);
            ProgramUpdateItem Candidate(PackageDataItem entry)
            {
                var key = Key(entry);
                if (!candidates.TryGetValue(key, out var candidate))
                {
                    if (!IsHash(entry.Checksum)) Issue("invalidChecksum", key, "Available program checksum is missing or invalid.");
                    candidate = new ProgramUpdateItem {
                        Repository = entry.Repository, PackageId = entry.PackageId, Id = entry.Id,
                        PackageVersion = packages[Key(entry.Repository, entry.PackageId)].Version,
                        AvailableVersion = entry.Version, AvailableChecksum = entry.Checksum,
                        SuggestedAddress = entry.Hid, Required = entry.Required,
                        Instances = instances.TryGetValue(key, out var found) ? found : new List<InstalledProgramSnapshot>()
                    };
                    foreach (var instance in candidate.Instances)
                        candidate.LocalChanges[instance.Address] = IsHash(instance.BaselineChecksum) && IsHash(instance.ActualChecksum)
                            ? !string.Equals(instance.BaselineChecksum, instance.ActualChecksum, StringComparison.OrdinalIgnoreCase)
                            : (bool?)null;
                    candidates.Add(key, candidate);
                }
                return candidate;
            }

            // Updates are determined per installed instance. Never propose a downgrade.
            foreach (var pair in instances)
            {
                if (!entries.TryGetValue(pair.Key, out var entry)) continue;
                foreach (var instance in pair.Value)
                {
                    if (!TryCompareVersions(entry.Version, instance.Version, out var comparison))
                    { Issue("invalidVersion", pair.Key, "Cannot compare available and installed program versions."); continue; }
                    if (comparison <= 0) continue;
                    var candidate = Candidate(entry);
                    if (!candidate.Reasons.Contains("updateAvailable")) candidate.Reasons.Add("updateAvailable");
                    candidate.UpdateAddresses.Add(instance.Address);
                }
            }

            // Traverse required dependencies only from installed packages (or their dependencies).
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var visiting = new HashSet<string>(StringComparer.Ordinal);
            void Visit(string packageKey)
            {
                if (visiting.Contains(packageKey))
                { Issue("dependencyCycle", packageKey, "Required package dependencies contain a cycle."); return; }
                if (visited.Contains(packageKey)) return;
                if (!packages.TryGetValue(packageKey, out var pkg))
                { Issue("missingPackage", packageKey, "Installed package is absent from the available catalog."); visited.Add(packageKey); return; }
                visiting.Add(packageKey);
                foreach (var requirement in pkg.Programs.Where(p => p != null && p.Required))
                {
                    if (!HasIdentity(requirement)) continue;
                    var identity = Key(requirement);
                    bool external = requirement.Repository != pkg.Repository || requirement.PackageId != pkg.Id;
                    if (!entries.TryGetValue(identity, out var entry))
                    { Issue("missingDependency", identity, "Required program is absent from its owning package catalog."); continue; }
                    instances.TryGetValue(identity, out var found);
                    bool installedSatisfiesRequirement = found != null && found.Any(p =>
                        TryCompareVersions(p.Version, requirement.Version, out var c) && c >= 0);
                    if (!installedSatisfiesRequirement &&
                        (!TryCompareVersions(entry.Version, requirement.Version, out var availableComparison) || availableComparison < 0))
                    { Issue("unsatisfiedDependency", identity, "The available program does not satisfy the required version."); continue; }
                    if ((found == null || found.Count == 0) && (external || active.Contains(packageKey)))
                    {
                        var candidate = Candidate(entry);
                        candidate.Required = true;
                        var reason = external ? "missingDependency" : "missingRequired";
                        if (!candidate.Reasons.Contains(reason)) candidate.Reasons.Add(reason);
                        if (!candidate.RequiredBy.Contains(packageKey)) candidate.RequiredBy.Add(packageKey);
                    }
                    if (external)
                    {
                        // A dependency refers to a package program, not a specific runtime address.
                        if (!installedSatisfiesRequirement && found != null)
                        {
                            var candidate = Candidate(entry);
                            if (!candidate.RequiredBy.Contains(packageKey)) candidate.RequiredBy.Add(packageKey);
                            if (!candidate.Reasons.Contains("dependencyUpdate")) candidate.Reasons.Add("dependencyUpdate");
                        }
                        Visit(Key(entry.Repository, entry.PackageId));
                    }
                }
                visiting.Remove(packageKey); visited.Add(packageKey);
            }
            foreach (var key in active.OrderBy(k => k, StringComparer.Ordinal)) Visit(key);
            report.Items = candidates.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Value).ToList();
            foreach (var item in report.Items)
            {
                item.RequiredBy.Sort(StringComparer.Ordinal);
                item.Reasons.Sort(StringComparer.Ordinal);
                item.UpdateAddresses.Sort();
            }
            report.Issues = report.Issues.GroupBy(i => (i.Code, i.Identity, i.Message)).Select(g => g.First())
                .OrderBy(i => i.Identity, StringComparer.Ordinal).ThenBy(i => i.Code, StringComparer.Ordinal).ToList();
            return report;
        }

        private static bool IsHash(string value) => value != null && Regex.IsMatch(value, "^[0-9a-fA-F]{32}$");

        // Numeric components, prerelease identifiers and build metadata; 1.2 == 1.2.0.
        // Unknown formats are reported rather than guessed or sorted lexicographically.
        public static bool TryCompareVersions(string left, string right, out int comparison)
        {
            comparison = 0;
            const string pattern = @"^(?<core>[0-9]+(?:\.[0-9]+){1,3})(?:-(?<pre>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$";
            var a = Regex.Match(left ?? "", pattern); var b = Regex.Match(right ?? "", pattern);
            if (!a.Success || !b.Success) return false;
            var ac = a.Groups["core"].Value.Split('.'); var bc = b.Groups["core"].Value.Split('.');
            for (var i = 0; i < Math.Max(ac.Length, bc.Length); i++)
            {
                comparison = BigInteger.Parse(i < ac.Length ? ac[i] : "0").CompareTo(BigInteger.Parse(i < bc.Length ? bc[i] : "0"));
                if (comparison != 0) return true;
            }
            var ap = a.Groups["pre"].Value; var bp = b.Groups["pre"].Value;
            if (ap == "" || bp == "") { comparison = ap == bp ? 0 : ap == "" ? 1 : -1; return true; }
            var av = ap.Split('.'); var bv = bp.Split('.');
            for (var i = 0; i < Math.Min(av.Length, bv.Length); i++)
            {
                bool an = Regex.IsMatch(av[i], "^[0-9]+$"), bn = Regex.IsMatch(bv[i], "^[0-9]+$");
                var ai = an ? BigInteger.Parse(av[i]) : BigInteger.Zero;
                var bi = bn ? BigInteger.Parse(bv[i]) : BigInteger.Zero;
                comparison = an && bn ? ai.CompareTo(bi) : an != bn ? (an ? -1 : 1) : string.CompareOrdinal(av[i], bv[i]);
                if (comparison != 0) return true;
            }
            comparison = av.Length.CompareTo(bv.Length); return true;
        }
    }
}
