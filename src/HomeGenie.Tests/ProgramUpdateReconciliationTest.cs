using System;
using System.IO;
using System.Collections.Generic;
using HomeGenie.Service;
using Newtonsoft.Json;
using NUnit.Framework;

namespace HomeGenie.Tests;
[TestFixture]
public class ProgramUpdateReconciliationTest
{
    private string folder;
    private int inspections,starts;
    private ProgramUpdateReport report;
    private List<string> requests;
    private bool reject;
    private ProgramUpdateReconciliation coordinator;
    [SetUp] public void Setup()
    {
        inspections=starts=0;reject=false;
        folder=Path.Combine(Path.GetTempPath(),"hg-reconcile-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        report=new ProgramUpdateReport {Items=new(){new ProgramUpdateItem {Repository="test",PackageId="base",Id="new",Reasons=new(){"missingRequired"}}}};
        requests=new();
        coordinator=new ProgramUpdateReconciliation(folder,()=>{inspections++;return report;},(fingerprint,id)=>{
            starts++;requests.Add(id);if(reject)throw new InvalidOperationException("Fixture refusal");
            return new ProgramUpdateJob {Id="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",RequestId=id,State="running"};
        },id=>new ProgramUpdateJob {Id=id,State="interrupted"});
    }
    [TearDown] public void Cleanup()=>Directory.Delete(folder,true);
    [Test] public void FirstNewReleaseReconcilesOnceWithoutAnOldUpdaterMarker()
    {
        var first=coordinator.Run("new-release");coordinator.Run("new-release");
        Assert.That(first.Phase,Is.EqualTo("accepted"));Assert.That(starts,Is.EqualTo(1));Assert.That(inspections,Is.EqualTo(1));
        Assert.That(File.Exists(Path.Combine(folder,"state.json")),Is.True);
    }
    [Test] public void ChangedReleaseOrExplicitUpdateMarkerTriggersANewPass()
    {
        coordinator.Run("one");coordinator.Run("two");ProgramUpdateReconciliation.MarkPending(folder);coordinator.Run("two");coordinator.Run("two");
        Assert.That(starts,Is.EqualTo(3));Assert.That(new HashSet<string>(requests).Count,Is.EqualTo(3));
    }
    [Test] public void EmptyReportDoesNotCreateAnUpdateJob()
    {
        report.Items.Clear();Assert.That(coordinator.Run("release").Phase,Is.EqualTo("complete"));coordinator.Run("release");Assert.That(starts,Is.Zero);
    }
    [Test] public void IncompleteCatalogPreservesFailureAndDoesNotLoopAtEveryBoot()
    {
        report.Issues.Add(new ProgramUpdateIssue {Message="Fixture catalog issue"});
        Assert.Throws<InvalidOperationException>(()=>coordinator.Run("release"));
        Assert.That(coordinator.Run("release").Phase,Is.EqualTo("failed"));Assert.That(starts,Is.Zero);
    }
    [Test] public void InterruptedAcceptedJobsAreNeverAutomaticallyRetried()
    {
        coordinator.Run("release");coordinator.Run("release");Assert.That(starts,Is.EqualTo(1));
    }
    [Test] public void CrashBeforeRecordingAcceptanceReusesThePersistedRequestIdentity()
    {
        var id=Guid.NewGuid().ToString("N");
        File.WriteAllText(Path.Combine(folder,"state.json"),JsonConvert.SerializeObject(new ProgramUpdateReconciliation.State {Release="release",Phase="prepared",RequestId=id,Fingerprint=report.Fingerprint}));
        coordinator.Run("release");Assert.That(requests,Is.EqualTo(new[]{id}));Assert.That(inspections,Is.Zero);
    }
    [Test] public void RefusedJobRequiresManualReviewRatherThanRepeatedAutomaticSubmissions()
    {
        reject=true;Assert.Throws<InvalidOperationException>(()=>coordinator.Run("release"));coordinator.Run("release");Assert.That(starts,Is.EqualTo(1));
    }
    [Test] public void MissingReleaseAndMarkerDoesNothing()
    {
        Assert.That(coordinator.Run(null),Is.Null);Assert.That(inspections,Is.Zero);
    }
    [Test] public void CorruptedStateIsNotOverwritten()
    {
        File.WriteAllText(Path.Combine(folder,"state.json"),"{}");Assert.Throws<InvalidDataException>(()=>coordinator.Run("release"));
        Assert.That(File.ReadAllText(Path.Combine(folder,"state.json")),Is.EqualTo("{}"));Assert.That(starts,Is.Zero);
    }
}
