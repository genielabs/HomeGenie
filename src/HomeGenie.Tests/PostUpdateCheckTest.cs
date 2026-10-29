using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using HomeGenie.Service;
using NUnit.Framework;

namespace HomeGenie.Tests;

[TestFixture]
public class PostUpdateCheckTest
{
    private string root;
    private string Marker => Path.Combine(root,"data/post-update/completed.json");
    [SetUp] public void Setup() {root=Path.Combine(Path.GetTempPath(),"hg-post-update-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);}
    [TearDown] public void Cleanup() {Directory.Delete(root,true);}
    private string Put(string relative,string value) {
        string path=Path.Combine(root,relative);Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,value);return path;
    }
    [Test] public void FreshInstallationRecordsCompletionWithoutCreatingRuntimeOrBackupDirectories() {
        PostUpdateCheck.Run(root,null);
        using var state=JsonDocument.Parse(File.ReadAllText(Marker));
        Assert.That(state.RootElement.TryGetProperty(PostUpdateCheck.EmptyCompilerFilesStep,out _),Is.True);
        foreach(string folder in new[]{"programs","runtimes","_update","tmp"})Assert.That(Directory.Exists(Path.Combine(root,folder)),Is.False,folder);
    }
    [Test] public void CompletedStepIsNotRepeatedOnRestart() {
        string empty=Put("programs/"+Guid.NewGuid()+".dll","");PostUpdateCheck.Run(root,null);
        string before=File.ReadAllText(Marker);Assert.That(File.Exists(empty),Is.False);
        File.WriteAllText(empty,"");PostUpdateCheck.Run(root,null);
        Assert.That(File.Exists(empty),Is.True);Assert.That(File.ReadAllText(Marker),Is.EqualTo(before));
    }
    [Test] public void LlamaBinariesInventoriesAndExistingBackupsAreNotReadValidatedOrChanged() {
        var preserved=new Dictionary<string,string> {
            ["LLamaSharp.KernelMemory.dll"]="legacy-adapter",
            ["LLamaSharp.SemanticKernel.dll"]="legacy-adapter",
            ["LLamaSharp.dll"]="managed-sentinel",
            ["runtimes/linux-x64/native/avx2/libllama.so"]="native-sentinel",
            ["HomeGenie.llamasharp-assets.txt"]="invalid-old-inventory",
            ["HomeGenie.deps.json"]="invalid-old-dependencies",
            ["_update/post-update/previous/backup.dll"]="backup-sentinel"
        };
        foreach(var item in preserved)Put(item.Key,item.Value);
        Assert.DoesNotThrow(()=>PostUpdateCheck.Run(root,null));
        foreach(var item in preserved)Assert.That(File.ReadAllText(Path.Combine(root,item.Key)),Is.EqualTo(item.Value),item.Key);
    }
    [Test] public void CorruptCompletionRecordIsPreservedWithoutBlockingStartupOrDeletingFiles() {
        Put("data/post-update/completed.json","invalid-json");string empty=Put("programs/"+Guid.NewGuid()+".dll","");
        var logs=new List<string>();Assert.DoesNotThrow(()=>PostUpdateCheck.Run(root,logs.Add));
        Assert.That(File.ReadAllText(Marker),Is.EqualTo("invalid-json"));Assert.That(File.Exists(empty),Is.True);
        Assert.That(logs,Has.Some.Contains("deferred"));
    }
    [Test] public void FailedCompletionWriteRemainsPendingAndCanBeRetried() {
        Directory.CreateDirectory(Marker);string empty=Put("programs/"+Guid.NewGuid()+".dll","");
        Assert.DoesNotThrow(()=>PostUpdateCheck.Run(root,null));Assert.That(File.Exists(Marker),Is.False);Assert.That(File.Exists(empty),Is.False);
        Directory.Delete(Marker);PostUpdateCheck.Run(root,null);Assert.That(File.Exists(Marker),Is.True);
    }
    [Test] public void ExistingOtherStepCompletionsArePreserved() {
        Put("data/post-update/completed.json","{\"previous-step\":\"2026-01-01T00:00:00+00:00\"}");
        PostUpdateCheck.Run(root,null);using var state=JsonDocument.Parse(File.ReadAllText(Marker));
        Assert.That(state.RootElement.TryGetProperty("previous-step",out _),Is.True);
        Assert.That(state.RootElement.TryGetProperty(PostUpdateCheck.EmptyCompilerFilesStep,out _),Is.True);
    }
    [Test] public void IntegrationRunsBeforeServiceConstructionWithoutImportingNativeCleanupTargets() {
        var folder=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(folder!=null&&!File.Exists(Path.Combine(folder.FullName,"HomeGenie.sln")))folder=folder.Parent;
        string worker=File.ReadAllText(Path.Combine(folder.FullName,"src/HomeGenie/ServiceWorker.cs"));
        Assert.That(worker.IndexOf("PostUpdateCheck.Run(",StringComparison.Ordinal),Is.LessThan(worker.IndexOf("new HomeGenieService(",StringComparison.Ordinal)));
        string project=File.ReadAllText(Path.Combine(folder.FullName,"src/HomeGenie/HomeGenie.csproj"));
        Assert.That(project,Does.Not.Contain("PostUpdateCheck.targets"));
        Assert.That(File.Exists(Path.Combine(folder.FullName,"src/HomeGenie/PostUpdateCheck.targets")),Is.False);
    }
    [Test] public void SymlinkedProgramsDirectoryIsPreservedWithoutBlockingStartup() {
        string outside=Path.Combine(Path.GetTempPath(),"hg-sentinel-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(outside);
        string sentinel=Path.Combine(outside,Guid.NewGuid()+".dll");File.WriteAllText(sentinel,"");string link=Path.Combine(root,"programs");
        try {
            try {Directory.CreateSymbolicLink(link,outside);}
            catch(Exception error) when(error is UnauthorizedAccessException or PlatformNotSupportedException or IOException) {
                Assert.Ignore("Symbolic-link creation is unavailable on this host.");
            }
            Assert.DoesNotThrow(()=>PostUpdateCheck.Run(root,null));Assert.That(File.Exists(sentinel),Is.True);
            Assert.That(File.Exists(Marker),Is.False);
        } finally {if(Directory.Exists(link))Directory.Delete(link);Directory.Delete(outside,true);}
    }
}
