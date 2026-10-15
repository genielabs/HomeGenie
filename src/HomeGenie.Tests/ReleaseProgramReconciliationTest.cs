using System;
using System.IO;
using HomeGenie.Service;
using NUnit.Framework;

namespace HomeGenie.Tests;
[TestFixture,NonParallelizable]
public class ReleaseProgramReconciliationTest
{
    private string folder,previous;
    [SetUp] public void Setup() {previous=Environment.CurrentDirectory;folder=Path.Combine(Path.GetTempPath(),"hg-release-reconcile-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);Environment.CurrentDirectory=folder;}
    [TearDown] public void Cleanup(){Environment.CurrentDirectory=previous;Directory.Delete(folder,true);}
    private string Marker=>Path.Combine("data","program-updates","reconciliation","pending.txt");
    private static void Staged(string name,string text){var path=Path.Combine("_update","files","HomeGenie_update",name);Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,text);}
    [Test] public void ReleaseCopiesCatalogThenSchedulesReconciliationForTheNewRuntime()
    {
        Staged(Path.Combine("data","packages","test","base","package.json"),"catalog fixture");
        File.WriteAllText("groups.xml","user groups fixture");
        var result=new UpdateChecker(null).InstallFiles();
        Assert.That(result,Is.EqualTo(UpdateChecker.InstallStatus.RestartRequired));
        Assert.That(File.ReadAllText(Path.Combine("data","packages","test","base","package.json")),Is.EqualTo("catalog fixture"));
        Assert.That(Guid.TryParseExact(File.ReadAllText(Marker),"N",out _),Is.True);
        Assert.That(File.ReadAllText("groups.xml"),Is.EqualTo("user groups fixture"));
    }
    [Test] public void FailedReleaseInstallationDoesNotScheduleProgramInstallation()
    {
        File.WriteAllText("groups.xml","user groups fixture");Staged("groups.xml","<invalid");
        Assert.That(new UpdateChecker(null).InstallFiles(),Is.EqualTo(UpdateChecker.InstallStatus.Error));
        Assert.That(File.Exists(Marker),Is.False);Assert.That(File.ReadAllText("groups.xml"),Is.EqualTo("user groups fixture"));
    }
    [Test] public void NoStagedReleaseDoesNotCreateAMarker()
    {
        Assert.That(new UpdateChecker(null).InstallFiles(),Is.EqualTo(UpdateChecker.InstallStatus.Success));Assert.That(File.Exists(Marker),Is.False);
    }
}
