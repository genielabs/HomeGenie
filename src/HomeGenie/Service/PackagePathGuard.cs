using System;
using System.IO;
using System.Linq;

namespace HomeGenie.Service;

// Values decoded by MIG remain untrusted when they are used as filesystem paths.
internal static class PackagePathGuard
{
    internal static string Segment(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value is "." or ".." ||
            value.EndsWith('.') || value.EndsWith(' ') ||
            value.IndexOfAny(new[] {'/', '\\', '\0', ':', '<', '>', '"', '|', '?', '*'}) >= 0 ||
            value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new InvalidDataException("Invalid package path component.");
        // Windows device names are not ordinary files, even with an extension.
        string stem = value.Split('.')[0].ToUpperInvariant();
        if (stem is "CON" or "PRN" or "AUX" or "NUL" ||
            stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) ||
                                 stem.StartsWith("LPT", StringComparison.Ordinal)) && stem[3] is >= '1' and <= '9')
            throw new InvalidDataException("Invalid package path component.");
        return value;
    }

    internal static string Contained(string root, string relative)
    {
        if (string.IsNullOrEmpty(relative) || Path.IsPathRooted(relative) ||
            relative[0] is '/' or '\\' || relative.Contains(':') || relative.Contains('\0'))
            throw new InvalidDataException("Invalid package relative path.");
        string[] parts = relative.Replace('\\', '/').Split('/');
        foreach (string part in parts)
        {
            if (part == "..") throw new InvalidDataException("Package path escapes its directory.");
            if (part.Length != 0 && part != ".") Segment(part);
        }
        string fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        string path = Path.GetFullPath(Path.Combine(fullRoot, relative.Replace('\\', Path.DirectorySeparatorChar)));
        string prefix = Path.EndsInDirectorySeparator(fullRoot) ? fullRoot : fullRoot + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!path.StartsWith(prefix, comparison))
            throw new InvalidDataException("Package path escapes its directory.");
        // The supplied root is trusted (and may be a configured data-directory
        // link); archive/catalog-controlled descendants must not redirect it.
        string current = fullRoot;
        foreach (string part in Path.GetRelativePath(fullRoot, path).Split(Path.DirectorySeparatorChar))
        {
            current = Path.Combine(current, part);
            NotLink(current);
        }
        return path;
    }

    internal static void NotLink(string path)
    {
        try
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Package path contains a symbolic link.");
        }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }
    }

    internal static void NoLinksInTree(string path)
    {
        NotLink(path);
        if (Directory.Exists(path))
            foreach (string entry in Directory.EnumerateFileSystemEntries(path)) NoLinksInTree(entry);
    }

    internal static string CreateWorkspace(string tempRoot)
    {
        string path = Contained(tempRoot, "package-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    internal static void RemoveWorkspace(string tempRoot, string workspace)
    {
        // Cleanup of a private, generated directory must not hide an import error.
        try
        {
            string path = Contained(tempRoot, Path.GetRelativePath(Path.GetFullPath(tempRoot), workspace));
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
