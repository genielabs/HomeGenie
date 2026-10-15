using System;
using System.IO;
using HomeGenie.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace HomeGenie.Tests;
[TestFixture]
public class ProgramUpdateRetentionTest
{
    private string folder;
    private ProgramUpdateJobs jobs;
    [SetUp] public void Setup() { folder=Path.Combine(Path.GetTempPath(),"hg-retention-"+Guid.NewGuid().ToString("N")); jobs=new ProgramUpdateJobs(folder,null,null); }
    [TearDown] public void Cleanup() { jobs.Stop(); Directory.Delete(folder,true); }
    private string Record(int days,string state,bool manual=false,string jobState="completed")
    {
        string id=Guid.NewGuid().ToString("N");
        var job=new ProgramUpdateJob {Id=id,State=jobState,CreatedAt=DateTime.UtcNow.AddDays(-days).ToString("O"),UpdatedAt=DateTime.UtcNow.AddDays(-days).ToString("O"),
          Items=new(){new ProgramUpdateTarget {State=state,ManualRecoveryRequired=manual,Backup="../../unrelated"}}};
        File.WriteAllText(Path.Combine(folder,id+".json"),JsonConvert.SerializeObject(job));
        Directory.CreateDirectory(Path.Combine(folder,id,"0"));File.WriteAllText(Path.Combine(folder,id,"0","program.xml"),"fixture");return id;
    }
    private bool Exists(string id)=>Directory.Exists(Path.Combine(folder,id,"0"));
    [Test] public void RetentionUsesSevenAndThirtyDaysAndKeepsRecoveryBackups()
    {
        var recent=Record(6,"completed");var old=Record(8,"completed");var failedRecent=Record(29,"failed",jobState:"partial");
        var failedOld=Record(31,"failed",jobState:"partial");var manual=Record(100,"failed",true,"partial");var interrupted=Record(100,"interrupted",jobState:"interrupted");
        jobs.History();
        Assert.That(Exists(recent),Is.True);Assert.That(Exists(old),Is.False);Assert.That(Exists(failedRecent),Is.True);Assert.That(Exists(failedOld),Is.False);
        Assert.That(Exists(manual),Is.True);Assert.That(Exists(interrupted),Is.True);
        Assert.That(File.Exists(Path.Combine(folder,old+".json")),Is.True,"History must survive cleanup");
    }
    [Test] public void ManualCleanupRemovesOnlyCompletedBackupsAndIgnoresPayloadPaths()
    {
        var success=Record(0,"completed");var failed=Record(0,"failed",jobState:"partial");var protectedSuccess=Record(0,"completed",true);
        Directory.CreateDirectory(Path.Combine(folder,"unknown"));File.WriteAllText(Path.Combine(folder,"unknown","keep"),"keep");
        var result=JObject.FromObject(jobs.CleanSuccessfulBackups());
        Assert.That(Exists(success),Is.False);Assert.That(Exists(failed),Is.True);Assert.That(Exists(protectedSuccess),Is.True);
        Assert.That(File.Exists(Path.Combine(folder,"unknown","keep")),Is.True);Assert.That((long)result["removableBytes"],Is.Zero);
        Assert.That((int)result["totalJobs"],Is.EqualTo(3));
    }
    [Test] public void MalformedJournalIsReportedAndItsFilesPreserved()
    {
        var id=Record(100,"completed");File.WriteAllText(Path.Combine(folder,id+".json"),"invalid");
        var result=JObject.FromObject(jobs.History());Assert.That(Exists(id),Is.True);Assert.That(((JArray)result["issues"]).Count,Is.EqualTo(1));
    }
    [Test] public void CleanupRefusesSymbolicLinksAndPreservesTheirTarget()
    {
        var id=Record(0,"completed");var link=Path.Combine(folder,id,"0");
        var outside=Path.Combine(Path.GetTempPath(),"hg-outside-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(outside);
        File.WriteAllText(Path.Combine(outside,"keep"),"keep");
        try {
            Directory.Delete(link,true);
            try { Directory.CreateSymbolicLink(link,outside); }
            catch (UnauthorizedAccessException) { Assert.Ignore("Symbolic links require additional OS privileges."); }
            catch (IOException e) when (OperatingSystem.IsWindows() && (e.HResult & 0xffff) == 1314) { Assert.Ignore("Windows denies symbolic-link creation without the required privilege."); }
            Assert.Throws<InvalidDataException>(()=>jobs.CleanSuccessfulBackups());
            Assert.That(File.ReadAllText(Path.Combine(outside,"keep")),Is.EqualTo("keep"));
        } finally {
            if(Directory.Exists(link))Directory.Delete(link);
            Directory.Delete(outside,true);
        }
    }
    [Test] public void RecordedRunningJobsCannotBeCleaned()
    {
        var id=Record(100,"completed",jobState:"running");jobs.CleanSuccessfulBackups();Assert.That(Exists(id),Is.True);
    }
}
