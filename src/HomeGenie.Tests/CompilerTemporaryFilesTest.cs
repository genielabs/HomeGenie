using System;
using System.IO;
using System.Text.Json;
using HomeGenie.Automation.Engines;
using HomeGenie.Service;
using NUnit.Framework;

namespace HomeGenie.Tests;

[TestFixture]
public class CompilerTemporaryFilesTest
{
    private string root;
    [SetUp] public void Setup() { root=Path.Combine(Path.GetTempPath(),"hg-compiler-temp-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root); }
    [TearDown] public void Cleanup() { Directory.Delete(root,true); }
    private string Output() => Path.Combine(root,Guid.NewGuid()+".dll");
    [Test] public void FailedCompilationKeepsDiagnosticsAndRemovesBothOutputFiles() {
        string output=Output();
        var result=CSharpAppFactory.CompileScript("return false;","this is not valid C#;","",output);
        Assert.That(result.Success,Is.False);Assert.That(result.Diagnostics,Is.Not.Empty);
        Assert.That(File.Exists(output),Is.False);Assert.That(File.Exists(output+".pdb"),Is.False);
    }
    [Test] public void SuccessfulCompilationKeepsDllAndDebugSymbolsForTheCaller() {
        string output=Output();
        var result=CSharpAppFactory.CompileScript("return false;","int count = 1;","",output);
        Assert.That(result.Success,Is.True);Assert.That(new FileInfo(output).Length,Is.GreaterThan(0));
        Assert.That(new FileInfo(output+".pdb").Length,Is.GreaterThan(0));
    }
    [Test] public void FailureOpeningDebugOutputRemovesOnlyTheCreatedAssembly() {
        string output=Output();Directory.CreateDirectory(output+".pdb");
        Assert.Catch(()=>CSharpAppFactory.CompileScript("","","",output));
        Assert.That(File.Exists(output),Is.False);Assert.That(Directory.Exists(output+".pdb"),Is.True);
    }
    [Test] public void EmptyGuidMigrationPreservesNumericNonemptyUnknownAndNestedFiles() {
        string folder=Path.Combine(root,"programs");Directory.CreateDirectory(folder);
        string empty=Path.Combine(folder,Guid.NewGuid()+".dll");
        File.WriteAllText(empty,"");File.WriteAllText(empty+".pdb","");File.WriteAllText(empty+".mdb","");
        var preserved=new[]{"940.dll","940.dll.pdb","readme.txt","invalid-guid.dll",Guid.NewGuid()+".dll"};
        for(int i=0;i<preserved.Length;i++)File.WriteAllText(Path.Combine(folder,preserved[i]),i==4?"sentinel":"");
        string child=Path.Combine(folder,"arduino");Directory.CreateDirectory(child);
        string nested=Path.Combine(child,Guid.NewGuid()+".dll");File.WriteAllText(nested,"");
        PostUpdateCheck.Run(root,null);
        Assert.That(File.Exists(empty),Is.False);Assert.That(File.Exists(empty+".pdb"),Is.False);Assert.That(File.Exists(empty+".mdb"),Is.False);
        foreach(string file in preserved)Assert.That(File.Exists(Path.Combine(folder,file)),Is.True,file);
        Assert.That(File.Exists(nested),Is.True);
        using var state=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"data/post-update/completed.json")));
        Assert.That(state.RootElement.TryGetProperty(PostUpdateCheck.EmptyCompilerFilesStep,out _),Is.True);
    }
    [Test] public void NonDirectoryProgramsPathIsPreserved() {
        File.WriteAllText(Path.Combine(root,"programs"),"directory-blocker");
        // A root-level file named programs is not a directory and is preserved.
        Assert.DoesNotThrow(()=>PostUpdateCheck.Run(root,null));
        Assert.That(File.ReadAllText(Path.Combine(root,"programs")),Is.EqualTo("directory-blocker"));
    }
    [Test] public void CompletionWriteFailureLeavesTheOptionalStepPendingWithoutBlockingStartup() {
        Directory.CreateDirectory(Path.Combine(root,"data/post-update/completed.json"));
        string folder=Path.Combine(root,"programs");Directory.CreateDirectory(folder);
        string empty=Path.Combine(folder,Guid.NewGuid()+".dll");File.WriteAllText(empty,"");
        Assert.DoesNotThrow(()=>PostUpdateCheck.Run(root,null));Assert.That(File.Exists(empty),Is.False);
        string marker=Path.Combine(root,"data/post-update/completed.json");Assert.That(File.Exists(marker),Is.False);
        Directory.Delete(marker);PostUpdateCheck.Run(root,null);Assert.That(File.Exists(marker),Is.True);
    }
}
