using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace HomeGenie.Service;

// Runs before HomeGenieService creates interfaces or automation engines. Step IDs
// are permanent migration revisions, independent of release numbers and reboots.
public static class PostUpdateCheck
{
    public const string EmptyCompilerFilesStep = "empty-compiler-temporaries-v1";
    public const string LegacyWidgetPowerHistoryStep = "legacy-widget-power-history-v1";

    public static void Run(string baseDirectory, Action<string> log)
    {
        try
        {
            string root = Path.GetFullPath(baseDirectory);
            string folder = PackagePathGuard.Contained(Path.Combine(root, "data"), "post-update");
            string statePath = PackagePathGuard.Contained(folder, "completed.json");
            var completed = File.Exists(statePath)
                ? JsonSerializer.Deserialize<Dictionary<string, DateTimeOffset>>(File.ReadAllText(statePath))
                    ?? throw new InvalidDataException("Invalid post-update completion record.")
                : new Dictionary<string, DateTimeOffset>();
            if (!completed.ContainsKey(EmptyCompilerFilesStep))
            {
                log?.Invoke("Post-update: starting " + EmptyCompilerFilesStep);
                RemoveEmptyCompilerFiles(root, log);
                Complete(EmptyCompilerFilesStep);
            }

            if (!completed.ContainsKey(LegacyWidgetPowerHistoryStep) && RemoveLegacyWidgetPowerHistory(root, log))
                Complete(LegacyWidgetPowerHistoryStep);

            void Complete(string step)
            {
                completed.Add(step, DateTimeOffset.UtcNow);
                SaveCompleted(folder, statePath, completed);
                log?.Invoke("Post-update: completed " + step);
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or JsonException)
        {
            // Keep unfinished housekeeping pending and preserve unreadable state.
            log?.Invoke("Post-update: cleanup deferred: " + error.Message);
        }
    }

    private static void SaveCompleted(string folder, string statePath, Dictionary<string, DateTimeOffset> completed)
    {
        Directory.CreateDirectory(folder);
        string temp = PackagePathGuard.Contained(folder, "completed-" + Guid.NewGuid().ToString("N") + ".tmp");
        try {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
                JsonSerializer.Serialize(stream, completed);
                stream.Flush(true);
            }
            File.Move(temp, statePath, true);
        } finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private static bool RemoveLegacyWidgetPowerHistory(string root, Action<string> log)
    {
        string widgets = PackagePathGuard.Contained(Path.Combine(root, "data"), "widgets");
        string folder = PackagePathGuard.Contained(widgets, "_shared");
        if (!Directory.Exists(folder)) return true;

        const string legacyImport = "/widgets/_shared/power-history.js";
        string legacy = PackagePathGuard.Contained(folder, "power-history.js");
        if (File.Exists(legacy))
        {
            string replacement = Path.Combine(root, "app", "assets", "js", "widgets", "power-history.js");
            if (!File.Exists(replacement))
            {
                log?.Invoke("Post-update: widget power history cleanup deferred until the new app asset is installed.");
                return false;
            }

            foreach (string relative in new[] { "cyberpunk/telemetry.js", "isometric/power.js", "radio-casa/receiver.js" })
            {
                string widget = PackagePathGuard.Contained(widgets, relative);
                if (File.Exists(widget) && File.ReadAllText(widget).Contains(legacyImport, StringComparison.Ordinal))
                {
                    log?.Invoke("Post-update: widget power history cleanup deferred until " + relative + " is updated.");
                    return false;
                }
            }

            File.Delete(legacy);
            log?.Invoke("Post-update: removed legacy data/widgets/_shared/power-history.js");
        }

        // Preserve any unrelated files that were added to the old directory.
        if (!Directory.EnumerateFileSystemEntries(folder).Any()) Directory.Delete(folder);
        return true;
    }

    private static void RemoveEmptyCompilerFiles(string root, Action<string> log)
    {
        string folder = PackagePathGuard.Contained(root, "programs");
        if (!Directory.Exists(folder)) return;
        foreach (string entry in Directory.EnumerateFiles(folder)) {
            string name = Path.GetFileName(entry), id = null;
            if (name.EndsWith(".dll.pdb", StringComparison.Ordinal)) id = name.Substring(0, name.Length - 8);
            else if (name.EndsWith(".dll.mdb", StringComparison.Ordinal)) id = name.Substring(0, name.Length - 8);
            else if (name.EndsWith(".dll", StringComparison.Ordinal)) id = name.Substring(0, name.Length - 4);
            if (!Guid.TryParseExact(id, "D", out _)) continue;
            string file = PackagePathGuard.Contained(folder, name);
            if (new FileInfo(file).Length != 0) continue;
            File.Delete(file);
            log?.Invoke("Post-update: removed empty compiler temporary programs/" + name);
        }
    }

}
