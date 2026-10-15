using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HomeGenie.Service;
using Newtonsoft.Json;
using NUnit.Framework;

namespace HomeGenie.Tests;

[TestFixture]
public class ProgramUpdateJobsTest
{
    private const string Hash = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    private string folder;
    private Store store;
    private ProgramUpdateJobs jobs;
    private ConcurrentQueue<ProgramUpdateJob> notifications;
    [SetUp] public void Setup()
    {
        folder = Path.Combine(Path.GetTempPath(), "hg-program-updates-" + Guid.NewGuid().ToString("N"));
        store = new Store(); notifications = new();
        jobs = new ProgramUpdateJobs(folder, store, notifications.Enqueue);
    }
    [TearDown] public void Cleanup() { store.Release.Set(); jobs.Stop(); Directory.Delete(folder, true); }
    private ProgramUpdateJob Start(string requestId = null, string retryOf = null) => jobs.Start(store.Inspect().Fingerprint, requestId ?? Guid.NewGuid().ToString(), retryOf);
    private ProgramUpdateJob Done()
    {
        Assert.That(SpinWait.SpinUntil(() => !jobs.IsRunning, 5000), Is.True, "Job did not finish");
        return jobs.Status();
    }

    [Test] public void SerialUpdatesPreserveDisabledStateAndWriteJournal()
    {
        store.Add("base", "one", 10, true); store.Add("base", "two", 11, false);
        Start(); var result = Done();
        Assert.That(result.Completed, Is.EqualTo(2));
        Assert.That(store.Installed.Single(i => i.Address == 11).Enabled, Is.False);
        Assert.That(store.Applied.ToArray(), Is.EqualTo(new[] {"one", "two"}));
        Assert.That(File.Exists(Path.Combine(folder, result.Id + ".json")), Is.True);
        Assert.That(notifications.All(n => n.Id == result.Id), Is.True);
    }
    [TestCase(true)] [TestCase(null)] public void LocalChangesAndUnknownBaselinesAreSkipped(bool? changed)
    {
        store.Add("base", "one", 10, true);
        store.Installed[0].BaselineChecksum = changed == null ? null : "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";
        Start(); var result = Done();
        Assert.That(result.Skipped, Is.EqualTo(1));
        Assert.That(result.State, Is.EqualTo("review"));
        Assert.That(store.Applied, Is.Empty);
    }
    [Test] public void StaleOrIncompleteReportCannotStartWrites()
    {
        store.Add("base", "one", 10, true);
        Assert.Throws<InvalidOperationException>(() => jobs.Start("stale", Guid.NewGuid().ToString()));
        store.Incomplete = true;
        Assert.Throws<InvalidOperationException>(() => Start());
        Assert.That(store.Applied, Is.Empty);
    }
    [Test] public void InvalidSourceFailsPreflightBeforeAnyInstallation()
    {
        store.Add("base", "one", 10, true); store.Add("base", "two", 11, true); store.Invalid.Add("two");
        Assert.Throws<InvalidDataException>(() => Start());
        Assert.That(store.Applied, Is.Empty);
        Assert.That(jobs.IsRunning, Is.False);
    }
    [Test] public void SourceIsRevalidatedImmediatelyBeforeApplying()
    {
        store.Add("base", "one", 10, true); store.ChangeAfterPreflight = true;
        Start(); var result = Done();
        Assert.That(result.Failed, Is.EqualTo(1));
        Assert.That(store.Applied, Is.Empty);
    }
    [Test] public void NewRequiredProgramsAndDependenciesAreOrderedBeforeDependants()
    {
        store.Add("app", "consumer", 20, true);
        store.Add("base", "helper", null, true);
        store.Packages.Single(p => p.Id == "app").Programs.Add(new PackageDataItem {Repository="test",PackageId="base",Id="helper",Required=true,Version="2.0.0"});
        Start(); var result = Done();
        Assert.That(store.Applied.ToArray(), Is.EqualTo(new[] {"helper", "consumer"}));
        Assert.That(result.Completed, Is.EqualTo(2));
        Assert.That(store.Installed.Single(i => i.Id == "helper").Address, Is.GreaterThan(0));
    }
    [Test] public void ModifiedDependencyBlocksConsumerInsteadOfForcingIt()
    {
        store.Add("base", "helper", 10, true); store.Installed[0].BaselineChecksum = null;
        store.Add("app", "consumer", 20, true);
        store.Packages.Single(p => p.Id == "app").Programs.Add(new PackageDataItem {Repository="test",PackageId="base",Id="helper",Required=true,Version="2.0.0"});
        Start(); var result = Done();
        Assert.That(result.Items.Single(i => i.Id == "consumer").State, Is.EqualTo("blocked"));
        Assert.That(store.Applied, Is.Empty);
    }
    [Test] public void FailedItemDoesNotPreventIndependentUpdatesAndRetryExcludesNewUnrelatedItems()
    {
        store.Add("base", "one", 10, true); store.Add("base", "two", 11, true); store.Fail.Add("two");
        Start(); var failed = Done();
        Assert.That(failed.Completed, Is.EqualTo(1)); Assert.That(failed.Failed, Is.EqualTo(1));
        store.Add("base", "new-unrelated", 12, true); store.Fail.Clear();
        Start(retryOf: failed.Id); var retried = Done();
        Assert.That(retried.Items.Select(i => i.Id), Is.EqualTo(new[] {"two"}));
        Assert.That(store.Installed.Single(i => i.Id == "new-unrelated").Version, Is.EqualTo("1.0.0"));
    }
    [Test] public void ConcurrentStartsAreRejectedButIdenticalRequestIsIdempotent()
    {
        store.Add("base", "one", 10, true); store.Block = true;
        string request = Guid.NewGuid().ToString(); var accepted = Start(request);
        Assert.That(store.Entered.Wait(3000), Is.True);
        Assert.That(Start(request).Id, Is.EqualTo(accepted.Id));
        Assert.Throws<InvalidOperationException>(() => Start());
        store.Release.Set(); Done();
        Assert.That(store.Applied.Count, Is.EqualTo(1));
    }
    [Test] public void RequestIdentitySurvivesLaterJobsAndRestart()
    {
        store.Add("base", "one", 10, true);
        string request = Guid.NewGuid().ToString(), fingerprint = store.Inspect().Fingerprint;
        var first = Start(request); Done(); Start(); Done(); jobs.Stop();
        jobs = new ProgramUpdateJobs(folder, store, notifications.Enqueue);
        Assert.That(jobs.Start(fingerprint, request).Id, Is.EqualTo(first.Id));
        Assert.That(store.Applied.Count, Is.EqualTo(1));
    }
    [Test] public void InterruptedJournalIsNeverAutomaticallyReplayed()
    {
        jobs.Stop();
        var record = new ProgramUpdateJob {Id=Guid.NewGuid().ToString("N"),RequestId=Guid.NewGuid().ToString("N"),
            Items=new List<ProgramUpdateTarget> {new(){Id="one",State="running"},new(){Id="two",State="pending"}}};
        File.WriteAllText(Path.Combine(folder,"latest.json"), JsonConvert.SerializeObject(record));
        jobs = new ProgramUpdateJobs(folder, store, notifications.Enqueue);
        Assert.That(jobs.Status().State, Is.EqualTo("interrupted"));
        Assert.That(jobs.Status().Items.All(i => i.State == "interrupted"), Is.True);
        Assert.That(store.Applied, Is.Empty);
    }
    [Test] public void StopDrainsCurrentTransactionButDoesNotStartNextItem()
    {
        store.Add("base", "one", 10, true); store.Add("base", "two", 11, true); store.Block = true;
        Start(); Assert.That(store.Entered.Wait(3000), Is.True);
        var stopping = Task.Run(jobs.Stop);
        Assert.That(stopping.Wait(100), Is.False);
        store.Release.Set(); Assert.That(stopping.Wait(3000), Is.True);
        Assert.That(jobs.Status().Items.Single(i=>i.Id=="two").State, Is.EqualTo("interrupted"));
        Assert.Throws<InvalidOperationException>(()=>Start());
    }
    [TestCase("../latest")] [TestCase("not-a-job")] public void StatusRejectsUntrustedPaths(string id) =>
        Assert.Throws<InvalidOperationException>(() => jobs.Status(id));

    [Test] public void RequiredReconciliationInstallsMissingRootsButLeavesUnrelatedAndDisabledProgramsAlone()
    {
        store.Add("base","new-required",null,true);store.Add("other","optional-update",11,false);
        jobs.StartRequired(store.Inspect().Fingerprint,Guid.NewGuid().ToString());var result=Done();
        Assert.That(result.Kind,Is.EqualTo("required"));Assert.That(store.Applied.ToArray(),Is.EqualTo(new[]{"new-required"}));
        Assert.That(store.Installed.Single(i=>i.Address==11).Version,Is.EqualTo("1.0.0"));
        Assert.That(store.Installed.Single(i=>i.Address==11).Enabled,Is.False);
        Assert.That(jobs.StartRequired(store.Inspect().Fingerprint,Guid.NewGuid().ToString()).Items,Is.Empty);
        Done();
    }
    [TestCase(false)] [TestCase(true)] public void RequiredReconciliationSelectsOnlyNecessaryDependencyUpdatesAndProtectsEdits(bool modified)
    {
        store.Add("base","new-required",null,true);store.Add("dependency","service",12,false);store.Add("other","unrelated",13,true);
        store.Packages.Single(p=>p.Id=="base").Programs.Add(new PackageDataItem {Repository="test",PackageId="dependency",Id="service",Version="2.0.0",Required=true});
        if(modified)store.Installed.Single(i=>i.Address==12).ActualChecksum="BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";
        jobs.StartRequired(store.Inspect().Fingerprint,Guid.NewGuid().ToString());var result=Done();
        Assert.That(result.Items.Any(i=>i.Id=="unrelated"),Is.False);
        Assert.That(store.Installed.Single(i=>i.Address==12).Enabled,Is.False);
        if(modified) {Assert.That(result.Items.Single(i=>i.Id=="new-required").State,Is.EqualTo("blocked"));Assert.That(store.Applied,Is.Empty);}
        else Assert.That(store.Applied.ToArray(),Is.EqualTo(new[]{"service","new-required"}));
    }
    [Test] public void RequestIdentityCannotSwitchBetweenManualAndAutomaticModes()
    {
        store.Add("base","one",10,true);var id=Guid.NewGuid().ToString();var fingerprint=store.Inspect().Fingerprint;
        Start(id);Done();Assert.Throws<InvalidOperationException>(()=>jobs.StartRequired(fingerprint,id));
    }

    private sealed class Store : IProgramUpdateStore
    {
        public List<PackageData> Packages = new();
        public List<InstalledProgramSnapshot> Installed = new();
        public HashSet<string> Fail = new(), Invalid = new();
        public ConcurrentQueue<string> Applied = new();
        public ManualResetEventSlim Entered = new(false), Release = new(false);
        public bool Block, Incomplete, ChangeAfterPreflight;
        private readonly Dictionary<string,int> validations = new();
        public void Add(string package, string id, int? address, bool enabled)
        {
            var owner = Packages.FirstOrDefault(p => p.Id == package);
            if(owner==null){owner=new PackageData {Repository="test",Id=package,Version="2.0.0",Programs=new()};Packages.Add(owner);}
            owner.Programs.Add(new PackageDataItem {Repository="test",PackageId=package,Id=id,Hid=(address??100).ToString(),Version="2.0.0",Checksum=Hash,Required=!address.HasValue});
            if(address.HasValue)Installed.Add(new InstalledProgramSnapshot {Repository="test",PackageId=package,Id=id,Address=address.Value,Name=id,Version="1.0.0",Enabled=enabled,BaselineChecksum=Hash,ActualChecksum=Hash});
        }
        public ProgramUpdateReport Inspect() {
            var report=ProgramUpdateChecker.Check(Packages, Installed, Packages.Select(p=>new InstalledPackageIdentity {Repository=p.Repository,Id=p.Id}));
            if(Incomplete)report.Issues.Add(new ProgramUpdateIssue {Code="fixture",Message="Incomplete"});
            return report;
        }
        public IReadOnlyList<PackageData> Catalog()=>Packages;
        public void ValidateSource(ProgramUpdateTarget target){validations.TryGetValue(target.Id,out int seen);validations[target.Id]=seen+1;if(Invalid.Contains(target.Id)||ChangeAfterPreflight&&seen>0)throw new InvalidDataException("Source changed");}
        public bool DependencySatisfied(string repository,string package,string id,string version)=>Installed.Any(p=>p.Repository==repository&&p.PackageId==package&&p.Id==id&&ProgramUpdateChecker.TryCompareVersions(p.Version,version,out int c)&&c>=0);
        public void Apply(ProgramUpdateTarget target,string backupFolder)
        {
            if(Block){Entered.Set();if(!Release.Wait(5000))throw new TimeoutException("Fixture was not released");}
            if(Fail.Contains(target.Id))throw new InvalidOperationException("Compile failed; previous program restored");
            var installed=Installed.FirstOrDefault(i=>i.Address==target.Address&&i.Id==target.Id);
            if(installed==null){installed=new InstalledProgramSnapshot {Repository=target.Repository,PackageId=target.PackageId,Id=target.Id,Address=1000+Installed.Count,Enabled=true};Installed.Add(installed);target.Address=installed.Address;}
            installed.Version=target.Version;installed.BaselineChecksum=installed.ActualChecksum=target.Checksum;
            Applied.Enqueue(target.Id);
        }
    }
}
