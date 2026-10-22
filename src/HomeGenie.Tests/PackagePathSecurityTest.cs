using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using HomeGenie.Automation;
using HomeGenie.Service;
using MIG;
using Newtonsoft.Json;
using NUnit.Framework;

namespace HomeGenie.Tests;

[TestFixture, NonParallelizable]
public class PackagePathSecurityTest
{
    private string root, previous;
    private PackageManager manager;
    [SetUp] public void Setup()
    {
        previous = Environment.CurrentDirectory;
        root = Path.Combine(Path.GetTempPath(), "hg-package-path-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Environment.CurrentDirectory = root;
        File.WriteAllText("sentinel.txt", "unchanged");
        manager = new PackageManager(null);
    }
    [TearDown] public void Cleanup()
    {
        Environment.CurrentDirectory = previous;
        // Only remove this fixture's explicitly created private root. .NET
        // deletes directory links themselves, without recursing into targets.
        Directory.Delete(root, true);
    }
    private byte[] Archive(params (string Name, string Text)[] files)
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
            foreach (var file in files)
            {
                using var writer = new StreamWriter(zip.CreateEntry(file.Name).Open());
                writer.Write(file.Text);
            }
        return output.ToArray();
    }
    private byte[] Package(string repository = "repo", string id = "safe", string version = "1.0")
    {
        var manifest = new PackageData { Repository = repository, Id = id, Version = version, Programs = new List<PackageDataItem>() };
        using var writer = new StringWriter();
        new XmlSerializer(typeof(List<ProgramBlock>)).Serialize(writer, new List<ProgramBlock>());
        return Archive(("package.json", JsonConvert.SerializeObject(manifest)), ("programs.xml", writer.ToString()), ("assets/nested.txt", version));
    }
    private byte[] Multipart(byte[] payload, string filename = "normal.zip")
    {
        var header = Encoding.UTF8.GetBytes("--local-boundary\r\nContent-Disposition: form-data; name=\"uploadFile\"; filename=\"" + filename + "\"\r\nContent-Type: application/zip\r\n\r\n");
        return header.Concat(payload).Concat(Encoding.UTF8.GetBytes("\r\n--local-boundary--\r\n")).ToArray();
    }
    private string WriteArchive(byte[] data)
    {
        string path = Path.Combine(root, Guid.NewGuid().ToString("N") + ".zip");
        File.WriteAllBytes(path, data); return path;
    }
    private void SentinelUnchanged() => Assert.That(File.ReadAllText(Path.Combine(root, "sentinel.txt")), Is.EqualTo("unchanged"));
    private void NoWorkspaces() => Assert.That(Directory.GetDirectories(Path.Combine(root, "tmp")), Is.Empty);
    private void Link(string path, string target, bool directory = true)
    {
        try { if (directory) Directory.CreateSymbolicLink(path, target); else File.CreateSymbolicLink(path, target); }
        catch (Exception e) when (e is UnauthorizedAccessException or IOException or PlatformNotSupportedException)
        { Assert.Ignore("Symbolic-link creation is unavailable: " + e.GetType().Name); }
    }

    [TestCase("..%2Fsentinel.txt")]
    [TestCase("..%5Csentinel.txt")]
    [TestCase("%2Ftmp%2Foutside.zip")]
    [TestCase("C%3A%5Coutside.zip")]
    [TestCase("%5C%5Cserver%5Cshare%5Coutside.zip")]
    [TestCase("%00.zip")]
    [TestCase("")]
    [TestCase(".")]
    [TestCase("..")]
    [TestCase("CON.zip")]
    public void ActualMigDecodedUploadNameIsRejectedBeforeAnyWrite(string encoded)
    {
        var command = new MigInterfaceCommand("HomeAutomation.HomeGenie/Config/Packages.Upload/" + encoded);
        Assert.Throws<InvalidDataException>(() => manager.UploadPackage(Multipart(Package()), command.GetOption(0)));
        SentinelUnchanged(); Assert.That(Directory.Exists(Path.Combine(root, "tmp")), Is.False);
    }
    [TestCase("normal.zip")]
    [TestCase("package with spaces.zip")]
    [TestCase("pacchetto-caff%C3%A8.zip")]
    [TestCase(".zip")]
    [TestCase("literal%252Fname.zip")]
    public void UploadUsesPrivateServerPathAndIgnoresMultipartFilename(string encoded)
    {
        Directory.CreateDirectory("tmp");File.WriteAllText(Path.Combine("tmp", "keep.txt"), "keep");
        var command = new MigInterfaceCommand("HomeAutomation.HomeGenie/Config/Packages.Upload/" + encoded);
        var result = manager.UploadPackage(Multipart(Package(), "../../sentinel.txt"), command.GetOption(0));
        Assert.That(result.Id, Is.EqualTo("safe"));
        Assert.That(File.ReadAllText(Path.Combine(root, "data", "packages", "repo", "safe", "assets", "nested.txt")), Is.EqualTo("1.0"));
        Assert.That(File.ReadAllText(Path.Combine("tmp", "keep.txt")), Is.EqualTo("keep"));
        SentinelUnchanged(); NoWorkspaces();
    }
    [Test] public void BenignUploadNameCannotBypassZipValidation()
    {
        byte[] archive = Archive(("package.json", "{}"), ("../sentinel.txt", "changed"));
        Assert.Throws<InvalidDataException>(() => manager.UploadPackage(Multipart(archive), "normal.zip"));
        SentinelUnchanged(); NoWorkspaces();
    }
    [Test] public void OrdinaryPackageReplacementStillWorks()
    {
        manager.UploadPackage(Multipart(Package()), "normal.zip");
        string obsolete = Path.Combine(root, "data", "packages", "repo", "safe", "obsolete.txt");File.WriteAllText(obsolete, "old");
        var result = manager.UploadPackage(Multipart(Package(version: "2.0")), "normal.zip");
        Assert.That(result.Version, Is.EqualTo("2.0")); Assert.That(File.Exists(obsolete), Is.False);
        Assert.That(File.ReadAllText(Path.Combine(root, "data", "packages", "repo", "safe", "assets", "nested.txt")), Is.EqualTo("2.0"));
        SentinelUnchanged(); NoWorkspaces();
    }
    [Test] public void InvalidArchiveCleansOnlyItsOwnWorkspace()
    {
        Directory.CreateDirectory("tmp");File.WriteAllText(Path.Combine("tmp", "keep.txt"), "keep");
        Assert.Catch(() => manager.UploadPackage(Multipart(Encoding.UTF8.GetBytes("not a zip")), "normal.zip"));
        Assert.That(File.ReadAllText(Path.Combine("tmp", "keep.txt")), Is.EqualTo("keep"));
        SentinelUnchanged(); NoWorkspaces();
    }
    [TestCase("..", "safe")]
    [TestCase("../repo", "safe")]
    [TestCase("repo\\child", "safe")]
    [TestCase("/outside", "safe")]
    [TestCase("C:outside", "safe")]
    [TestCase("repo", "..")]
    [TestCase("repo", "../../sentinel")]
    [TestCase("repo", "\\\\server\\share")]
    [TestCase("repo", ".")]
    [TestCase("repo", "name.")]
    [TestCase("repo", "name ")]
    [TestCase("repo", "NUL")]
    [TestCase(null, "safe")]
    [TestCase("repo", null)]
    public void ManifestIdentityIsRejectedBeforeCatalogChanges(string repository, string id)
    {
        Directory.CreateDirectory(Path.Combine("data", "packages", "repo", "safe"));
        string keep = Path.Combine("data", "packages", "repo", "safe", "keep.txt");File.WriteAllText(keep, "keep");
        Assert.Throws<InvalidDataException>(() => manager.AddPackage(WriteArchive(Package(repository, id))));
        Assert.That(File.ReadAllText(keep), Is.EqualTo("keep")); SentinelUnchanged(); NoWorkspaces();
    }
    [TestCase("../sentinel.txt")]
    [TestCase("..\\sentinel.txt")]
    [TestCase("nested/../../sentinel.txt")]
    [TestCase("/absolute.txt")]
    [TestCase("C:/absolute.txt")]
    [TestCase("C:\\absolute.txt")]
    [TestCase("\\\\server\\share\\absolute.txt")]
    [TestCase("../outside-directory/")]
    [TestCase("nested/.. /sentinel.txt")]
    public void EveryZipEntryIsValidatedBeforeFirstFileIsWritten(string unsafeName)
    {
        string unpack = Path.Combine(root, "unpack");Directory.CreateDirectory(unpack);
        string zip = WriteArchive(Archive(("valid.txt", "must not be written"), (unsafeName, "bad")));
        Assert.Throws<InvalidDataException>(() => Utility.UncompressZip(zip, unpack));
        Assert.That(Directory.GetFileSystemEntries(unpack), Is.Empty); SentinelUnchanged();
    }
    [Test] public void ZipContainingSymbolicLinkIsRejectedBeforeExtraction()
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            using (var writer = new StreamWriter(zip.CreateEntry("valid.txt").Open())) writer.Write("not written");
            var link = zip.CreateEntry("link");link.ExternalAttributes = unchecked((int)0xa1ff0000);
            using var writer2 = new StreamWriter(link.Open());writer2.Write("../sentinel.txt");
        }
        string unpack = Path.Combine(root, "unpack");Directory.CreateDirectory(unpack);
        Assert.Throws<InvalidDataException>(() => Utility.UncompressZip(WriteArchive(output.ToArray()), unpack));
        Assert.That(Directory.GetFileSystemEntries(unpack), Is.Empty);SentinelUnchanged();
    }
    [Test] public void NormalNestedArchivesAndOverwritesStillWork()
    {
        string unpack = Path.Combine(root, "unpack");Directory.CreateDirectory(unpack);File.WriteAllText(Path.Combine(unpack, "existing.txt"), "old");
        string zip = WriteArchive(Archive(("./", ""), ("./package.json", "{}"), ("existing.txt", "new"), ("widgets/demo/widget.js", "js"), ("src\\main.cpp", "code")));
        var result = Utility.UncompressZip(zip, unpack);
        Assert.That(result.Count, Is.EqualTo(4));
        Assert.That(File.ReadAllText(Path.Combine(unpack, "existing.txt")), Is.EqualTo("new"));
        Assert.That(File.ReadAllText(Path.Combine(unpack, "widgets", "demo", "widget.js")), Is.EqualTo("js"));
        Assert.That(File.ReadAllText(Path.Combine(unpack, "src", "main.cpp")), Is.EqualTo("code"));
    }
    [Test] public void ExistingExtractionDirectoryLinkIsRejected()
    {
        string outside = Path.Combine(root, "outside");Directory.CreateDirectory(outside);
        string unpack = Path.Combine(root, "unpack");Link(unpack, outside);
        Assert.Throws<InvalidDataException>(() => Utility.UncompressZip(WriteArchive(Archive(("file.txt", "bad"))), unpack));
        Assert.That(Directory.GetFileSystemEntries(outside), Is.Empty);
    }
    [Test] public void ExistingExtractionFileLinkCannotOverwriteSentinel()
    {
        string unpack = Path.Combine(root, "unpack");Directory.CreateDirectory(unpack);
        Link(Path.Combine(unpack, "file.txt"), Path.Combine(root, "sentinel.txt"), false);
        Assert.Throws<InvalidDataException>(() => Utility.UncompressZip(WriteArchive(Archive(("file.txt", "bad"))), unpack));
        SentinelUnchanged();
    }
    [Test] public void RepositoryDirectoryLinkCannotRedirectPackageInstallation()
    {
        Directory.CreateDirectory(Path.Combine("data", "packages"));
        string outside = Path.Combine(root, "outside");Directory.CreateDirectory(outside);File.WriteAllText(Path.Combine(outside, "keep.txt"), "keep");
        Link(Path.Combine(root, "data", "packages", "repo"), outside);
        Assert.Throws<InvalidDataException>(() => manager.AddPackage(WriteArchive(Package())));
        Assert.That(File.ReadAllText(Path.Combine(outside, "keep.txt")), Is.EqualTo("keep"));NoWorkspaces();
    }
    [Test] public void ConfiguredDataDirectoryLinkRemainsUsable()
    {
        string data = Path.Combine(root, "configured-data");Directory.CreateDirectory(data);Link(Path.Combine(root, "data"), data);
        manager.AddPackage(WriteArchive(Package()));
        Assert.That(File.Exists(Path.Combine(data, "packages", "repo", "safe", "package.json")), Is.True);NoWorkspaces();
    }
    [Test] public void LinkedExistingPackageContentsAreNotRecursivelyDeleted()
    {
        manager.AddPackage(WriteArchive(Package()));
        string outside = Path.Combine(root, "outside");Directory.CreateDirectory(outside);File.WriteAllText(Path.Combine(outside, "keep.txt"), "keep");
        Link(Path.Combine(root, "data", "packages", "repo", "safe", "link"), outside);
        Assert.Throws<InvalidDataException>(() => manager.AddPackage(WriteArchive(Package(version: "2.0"))));
        Assert.That(File.ReadAllText(Path.Combine(outside, "keep.txt")), Is.EqualTo("keep"));
        Assert.That(File.Exists(Path.Combine("data", "packages", "repo", "safe", "package.json")), Is.True);NoWorkspaces();
    }
    [Test] public void ConcurrentUploadsHaveIndependentTemporaryWorkspaces()
    {
        Parallel.For(0, 4, i => manager.UploadPackage(Multipart(Package(id: "package-" + i)), "same-name.zip"));
        for (int i = 0; i < 4; i++) Assert.That(File.Exists(Path.Combine("data", "packages", "repo", "package-" + i, "package.json")), Is.True);
        SentinelUnchanged();NoWorkspaces();
    }
}
