using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HomeGenie.Automation;
using HomeGenie.Data;
using Newtonsoft.Json.Linq;

namespace HomeGenie.Service;

/// <summary>Read-only views over existing module/feature metadata; never initializes module fields.</summary>
public static class ModuleDiscovery
{
    public static bool Applies(ProgramFeature feature, Module module, IEnumerable<string> implemented = null)
    {
        if (feature == null || module == null) return false;
        var declared = implemented?.ToArray() ?? Array.Empty<string>();
        if (declared.Length > 0) return declared.Any(pattern => Utility.MatchValues(pattern, feature.Property));
        bool matches = Utility.MatchValues(feature.ForDomains ?? "", module.Domain ?? "");
        string types = feature.ForTypes ?? "", properties = null, expected = null;
        int colon = types.IndexOf(':'), equals = types.IndexOf('=');
        if (equals > colon) { expected = types.Substring(equals + 1); types = types.Substring(0, equals); }
        if (colon >= 0) { properties = types.Substring(colon + 1).Trim(); types = types.Substring(0, colon).Trim(); }
        matches &= Utility.MatchValues(types, module.DeviceType.ToString());
        if (properties != null) {
            var property = module.Properties.ToArray().FirstOrDefault(p => Utility.MatchValues(properties, p.Name));
            matches &= property != null && (expected == null || Utility.MatchValues(expected, property.Value ?? ""));
        }
        return matches;
    }

    public static string[] ImplementedFeatures(Module module, IEnumerable<VirtualModule> virtualModules)
    {
        string parent = module.Properties.ToArray().FirstOrDefault(p => p.Name == Constants.Properties.VirtualModuleParentId)?.Value;
        var declared = virtualModules?.FirstOrDefault(v => v.ParentId == parent && v.Domain == module.Domain && v.Address == module.Address);
        return declared?.ImplementFeatures?.ToArray() ?? Array.Empty<string>();
    }

    private static void Page(int offset, int limit)
    { if (offset < 0 || limit < 1 || limit > 200) throw new ArgumentException("Use offset >= 0 and limit between 1 and 200."); }

    private static JObject Owner(ProgramBlock p) => new() {
        ["id"] = p.Address.ToString(), ["name"] = p.Name ?? "", ["enabled"] = p.IsEnabled
    };
    private static JObject Feature(ProgramBlock p, ProgramFeature f) => new() {
        ["property"] = f.Property, ["description"] = f.Description ?? "", ["fieldType"] = f.FieldType ?? "",
        ["program"] = Owner(p), ["appliesTo"] = new JObject { ["domains"] = f.ForDomains ?? "", ["types"] = f.ForTypes ?? "" }
    };

    public static JObject Features(IEnumerable<ProgramBlock> programs, Module module = null,
        IEnumerable<string> implemented = null, int? programId = null, string query = null,
        string property = null, int offset = 0, int limit = 50, bool includeDisabled = false)
    {
        Page(offset, limit);
        var rows = new List<JObject>();
        foreach (var p in programs.ToArray().OrderBy(p => p.Address)) {
            if ((!includeDisabled && !p.IsEnabled) || programId.HasValue && p.Address != programId.Value) continue;
            foreach (var f in (p.Features?.ToArray() ?? Array.Empty<ProgramFeature>()).OrderBy(f => f.Property, StringComparer.Ordinal)) {
                if (string.IsNullOrWhiteSpace(f.FieldType)) continue;
                if (module != null && (!p.IsEnabled || !Applies(f, module, implemented))) continue;
                if (property != null && f.Property != property) continue;
                if (!string.IsNullOrWhiteSpace(query) && !(p.Name + " " + f.Property + " " + f.Description).Contains(query, StringComparison.OrdinalIgnoreCase)) continue;
                rows.Add(Feature(p, f));
            }
        }
        return new JObject { ["version"] = 1, ["total"] = rows.Count, ["offset"] = offset, ["limit"] = limit,
            ["nextOffset"] = offset + limit < rows.Count ? offset + limit : (int?)null,
            ["items"] = new JArray(rows.Skip(offset).Take(limit)) };
    }

    public static JObject ProgramFeatures(IEnumerable<ProgramBlock> programs, string body, int? programId = null, string property = null)
    {
        var filter = string.IsNullOrWhiteSpace(body) ? new JObject() : JObject.Parse(body);
        if (filter.Properties().Any(p => !new[] { "programId", "query", "offset", "limit", "includeDisabled" }.Contains(p.Name)))
            throw new ArgumentException("Unknown feature filter.");
        if (programId.HasValue && filter["programId"] != null && (int)filter["programId"] != programId.Value)
            throw new ArgumentException("Conflicting program ID.");
        int? id = programId ?? (int?)filter["programId"];
        var all = programs.ToArray();
        if (id.HasValue && !all.Any(p => p.Address == id.Value)) throw new ArgumentException("Unknown program.");
        var result = Features(all, programId:id, property:property, query:(string)filter["query"],
            offset:(int?)filter["offset"] ?? 0, limit:(int?)filter["limit"] ?? 50, includeDisabled:programId.HasValue || ((bool?)filter["includeDisabled"] ?? false));
        if (property != null && (int)result["total"] == 0) throw new ArgumentException("Unknown feature.");
        return result;
    }

    public static JObject Schema(Module module, IEnumerable<ProgramBlock> programs,
        IEnumerable<string> implemented = null, int offset = 0, int limit = 100)
    {
        if (module == null) throw new ArgumentException("Unknown module.");
        Page(offset, limit);
        var owners = programs.Where(p => p.IsEnabled).ToArray();
        var applicable = owners.SelectMany(p => (p.Features?.ToArray() ?? Array.Empty<ProgramFeature>())
            .Where(f => Applies(f, module, implemented)).Select(f => (program:p, feature:f))).ToArray();
        var fields = module.Properties.ToArray().GroupBy(p => p.Name).ToDictionary(g => g.Key, g => g.First());
        var keys = fields.Keys.Concat(applicable.Select(f => f.feature.Property)).Distinct().OrderBy(k => k, StringComparer.Ordinal).ToArray();
        var properties = new JArray(); var features = new JArray();
        foreach (string key in keys.Skip(offset).Take(limit)) {
            fields.TryGetValue(key, out var field);
            var definitions = applicable.Where(f => f.feature.Property == key).ToArray();
            var definition = definitions.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.feature.FieldType)).feature;
            string fieldType = !string.IsNullOrWhiteSpace(field?.FieldType) ? field.FieldType : definition?.FieldType ?? "";
            var row = new JObject { ["key"] = key, ["description"] = definition?.Description ?? field?.Description ?? "",
                ["fieldType"] = fieldType, ["present"] = field != null,
                ["sensitive"] = Sensitive(key, field?.FieldType) || definitions.Any(d => Sensitive(key, d.feature.FieldType)),
                ["programs"] = new JArray(definitions.Select(d => {
                    var owner = Owner(d.program); owner["fieldType"] = d.feature.FieldType ?? ""; return owner;
                })) };
            if (string.IsNullOrWhiteSpace(fieldType)) properties.Add(row); else features.Add(row);
        }
        return new JObject { ["version"] = 1, ["module"] = new JObject { ["domain"] = module.Domain, ["address"] = module.Address,
                ["name"] = module.Name ?? "", ["description"] = module.Description ?? "", ["deviceType"] = module.DeviceType.ToString() },
            ["total"] = keys.Length, ["offset"] = offset, ["limit"] = limit,
            ["nextOffset"] = offset + limit < keys.Length ? offset + limit : (int?)null, ["properties"] = properties, ["features"] = features };
    }

    public static JObject ReadParameter(Module module, IEnumerable<ProgramBlock> programs, string key, IEnumerable<string> implemented = null)
    {
        if (module == null || string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A known module and parameter are required.");
        var field = module.Properties.ToArray().FirstOrDefault(p => p.Name == key);
        if (Sensitive(key, field?.FieldType) || programs.Any(p => (p.Features?.ToArray() ?? Array.Empty<ProgramFeature>())
            .Any(f => f.Property == key && Sensitive(key, f.FieldType))))
            throw new InvalidOperationException("Sensitive parameters are not available to AI context.");
        string value = field?.Value;
        if (value != null && Encoding.UTF8.GetByteCount(value) > 4096) throw new InvalidOperationException("Parameter exceeds the AI context read budget.");
        return new JObject { ["version"] = 1, ["domain"] = module.Domain, ["address"] = module.Address, ["key"] = key,
            ["present"] = field != null, ["value"] = value };
    }

    public static bool Sensitive(string key, string fieldType) =>
        (fieldType ?? "").Split(':')[0].Equals("password", StringComparison.OrdinalIgnoreCase) ||
        Regex.IsMatch(key ?? "", @"(?i)(password|passwd|secret|api[._-]?key|access[._-]?token|refresh[._-]?token|private[._-]?key|credential)");
}
