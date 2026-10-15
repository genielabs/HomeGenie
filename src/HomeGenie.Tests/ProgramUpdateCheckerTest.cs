using System;
using System.Collections.Generic;
using System.Linq;
using HomeGenie.Service;
using Newtonsoft.Json;
using NUnit.Framework;

namespace HomeGenie.Tests
{
    [TestFixture]
    public class ProgramUpdateCheckerTest
    {
        private const string Hash = "11111111111111111111111111111111";
        private const string ChangedHash = "22222222222222222222222222222222";
        private static PackageDataItem Entry(string id, string version = "2.0.0", bool required = true, string package = "base") =>
            new PackageDataItem { Repository = "test", PackageId = package, Id = id, Version = version,
                Required = required, Hid = "500", Checksum = ChangedHash };
        private static PackageData Package(string id, params PackageDataItem[] items) =>
            new PackageData { Repository = "test", Id = id, Version = "3.0.0", Programs = items.ToList() };
        private static InstalledProgramSnapshot Installed(string id, string version = "1.0.0", int address = 1005,
            string package = "base", bool enabled = false) => new InstalledProgramSnapshot {
                Repository = "test", PackageId = package, Id = id, Version = version, Address = address,
                Enabled = enabled, BaselineChecksum = Hash, ActualChecksum = Hash, Name = "User name"
            };
        private static InstalledPackageIdentity Active(string id = "base") =>
            new InstalledPackageIdentity { Repository = "test", Id = id };

        [Test]
        public void EmptyInstallationDoesNotInstallEveryAvailableRequiredProgram()
        {
            var report = ProgramUpdateChecker.Check(new[] { Package("base", Entry("service")) }, null, null);
            Assert.That(report.Complete, Is.True);
            Assert.That(report.Items, Is.Empty);
        }

        [Test]
        public void DetectsNewRequiredButNotUninstalledOptionalPrograms()
        {
            var report = ProgramUpdateChecker.Check(new[] { Package("base", Entry("new"), Entry("legacy", required: false)) },
                null, new[] { Active() });
            Assert.That(report.Items.Select(i => i.Id), Is.EqualTo(new[] { "new" }));
            Assert.That(report.MissingRequiredCount, Is.EqualTo(1));
        }

        [Test]
        public void InstalledProgramActivatesItsPackageWithoutRegistry()
        {
            var report = ProgramUpdateChecker.Check(new[] { Package("base", Entry("old", "1.0.0"), Entry("new")) },
                new[] { Installed("old") }, null);
            Assert.That(report.Items.Single().Id, Is.EqualTo("new"));
        }

        [Test]
        public void TracksClonesByPackageIdentityAndPreservesEnabledStateAndCustomizations()
        {
            var first = Installed("service", address: 2001); first.ActualChecksum = ChangedHash;
            var second = Installed("service", "3.0.0", 2002, enabled: true);
            var catalog = new[] { Package("base", Entry("service")) };
            var before = JsonConvert.SerializeObject(new { catalog, first, second });
            var report = ProgramUpdateChecker.Check(catalog, new[] { first, second }, null);
            var item = report.Items.Single();
            Assert.That(item.UpdateAddresses, Is.EqualTo(new[] { 2001 }));
            Assert.That(item.Instances.Select(i => i.Enabled), Is.EqualTo(new[] { false, true }));
            Assert.That(item.LocalChanges[2001], Is.True);
            Assert.That(item.LocalChanges[2002], Is.False);
            Assert.That(JsonConvert.SerializeObject(new { catalog, first, second }), Is.EqualTo(before));
        }

        [Test]
        public void MissingBaselineIsUnknownRatherThanUnmodified()
        {
            var program = Installed("service"); program.BaselineChecksum = null;
            var report = ProgramUpdateChecker.Check(new[] { Package("base", Entry("service")) }, new[] { program }, null);
            Assert.That(report.Items.Single().LocalChanges[program.Address], Is.Null);
        }

        [Test]
        public void SameVersionOrNewerInstalledVersionDoesNotProduceAnUpdate()
        {
            var report = ProgramUpdateChecker.Check(new[] { Package("base", Entry("a"), Entry("b")) },
                new[] { Installed("a", "2.0.0"), Installed("b", "3.0.0", 1006) }, null);
            Assert.That(report.Complete, Is.True);
            Assert.That(report.Items, Is.Empty);
        }

        [Test]
        public void ExternalReferenceIsResolvedFromOwningManifestAndDeduplicated()
        {
            var dep = Package("dep", Entry("worker", "2.0.0", package: "dep"));
            var report = ProgramUpdateChecker.Check(new[] {
                Package("base", Entry("worker", "1.0.0", package: "dep")),
                Package("other", Entry("worker", "1.0.0", package: "dep")), dep },
                null, new[] { Active(), Active("other") });
            var candidate = report.Items.Single();
            Assert.That(candidate.AvailableVersion, Is.EqualTo("2.0.0"));
            Assert.That(candidate.RequiredBy, Is.EqualTo(new[] { "test/base", "test/other" }));
            Assert.That(candidate.Reasons, Does.Contain("missingDependency"));
            Assert.That(report.MissingDependencyCount, Is.EqualTo(1));
        }

        [Test]
        public void TraversesTransitiveDependencies()
        {
            var report = ProgramUpdateChecker.Check(new[] {
                Package("base", Entry("worker", package: "dep")),
                Package("dep", Entry("worker", package: "dep"), Entry("leaf", package: "leaf")),
                Package("leaf", Entry("leaf", package: "leaf")) }, null, new[] { Active() });
            Assert.That(report.Complete, Is.True);
            Assert.That(report.Items.Select(i => i.Id), Is.EquivalentTo(new[] { "worker", "leaf" }));
        }

        [Test]
        public void IgnoresOptionalDependencyUnlessItsProgramIsAlreadyInstalled()
        {
            var report = ProgramUpdateChecker.Check(new[] {
                Package("base", Entry("worker", required: false, package: "dep")),
                Package("dep", Entry("worker", package: "dep")) }, null, new[] { Active() });
            Assert.That(report.Items, Is.Empty);
        }

        [Test]
        public void ExistingDisabledRequiredProgramIsNotReportedMissing()
        {
            var report = ProgramUpdateChecker.Check(new[] { Package("base", Entry("service", "1.0.0")) },
                new[] { Installed("service", enabled: false) }, null);
            Assert.That(report.Items, Is.Empty);
        }

        [Test]
        public void MissingDependencyIsAnExplicitIncompleteReport()
        {
            var report = ProgramUpdateChecker.Check(new[] { Package("base", Entry("worker", package: "unknown")) }, null, new[] { Active() });
            Assert.That(report.Status, Is.EqualTo("Error"));
            Assert.That(report.Issues.Select(i => i.Code), Does.Contain("missingDependency"));
        }

        [Test]
        public void DependencyCycleDoesNotLoopOrSilentlySucceed()
        {
            var report = ProgramUpdateChecker.Check(new[] {
                Package("base", Entry("a"), Entry("b", package: "dep")),
                Package("dep", Entry("b", package: "dep"), Entry("a")) }, null, new[] { Active() });
            Assert.That(report.Issues.Select(i => i.Code), Does.Contain("dependencyCycle"));
        }

        [Test]
        public void DuplicateCatalogIdentityIsNotResolvedArbitrarily()
        {
            var report = ProgramUpdateChecker.Check(new[] { Package("base", Entry("a")), Package("base", Entry("a", "3.0.0")) },
                new[] { Installed("a") }, null);
            Assert.That(report.Complete, Is.False);
            Assert.That(report.Items, Is.Empty);
        }

        [Test]
        public void MalformedVersionIsNotAnUpgrade()
        {
            var report = ProgramUpdateChecker.Check(new[] { Package("base", Entry("a", "latest")) }, new[] { Installed("a") }, null);
            Assert.That(report.Complete, Is.False);
            Assert.That(report.Items, Is.Empty);
        }

        [Test]
        public void CatalogTooOldForDependencyIsReported()
        {
            var report = ProgramUpdateChecker.Check(new[] {
                Package("base", Entry("worker", "4.0.0", package: "dep")),
                Package("dep", Entry("worker", "2.0.0", package: "dep")) }, null, new[] { Active() });
            Assert.That(report.Issues.Select(i => i.Code), Does.Contain("unsatisfiedDependency"));
        }

        [Test]
        public void NewerInstalledDependencySatisfiesRequirementEvenIfCatalogIsOlder()
        {
            var report = ProgramUpdateChecker.Check(new[] {
                Package("base", Entry("worker", "4.0.0", package: "dep")),
                Package("dep", Entry("worker", "2.0.0", package: "dep")) },
                new[] { Installed("worker", "5.0.0", package: "dep") }, new[] { Active() });
            Assert.That(report.Complete, Is.True);
            Assert.That(report.Items, Is.Empty);
        }

        [Test]
        public void DuplicateProgramRecordsDoNotChooseAnArbitraryVersion()
        {
            var report = ProgramUpdateChecker.Check(new[] { Package("base", Entry("a"), Entry("a", "3.0.0")) },
                new[] { Installed("a") }, null);
            Assert.That(report.Complete, Is.False);
            Assert.That(report.Items, Is.Empty);
        }

        [Test]
        public void FingerprintIsStableAcrossReadOnlyChecksAndChangesWithInventory()
        {
            var catalog = new[] { Package("base", Entry("a")) };
            var program = Installed("a");
            var first = ProgramUpdateChecker.Check(catalog, new[] { program }, null);
            first.CheckedAt = DateTime.MinValue;
            var second = ProgramUpdateChecker.Check(catalog, new[] { program }, null);
            Assert.That(first.Fingerprint, Is.EqualTo(second.Fingerprint));
            program.Version = "2.0.0";
            Assert.That(ProgramUpdateChecker.Check(catalog, new[] { program }, null).Fingerprint, Is.Not.EqualTo(first.Fingerprint));
        }

        [Test]
        public void CountsInstalledCopiesAndReportsOnlyTargetsRequiringReview()
        {
            var old = Installed("a"); var other = Installed("a", address: 2000);
            other.ActualChecksum = ChangedHash;
            var report = ProgramUpdateChecker.Check(new[] { Package("base", Entry("a")) }, new[] { old, other }, null);
            Assert.That(report.UpdateCount, Is.EqualTo(2));
            Assert.That(report.AffectedProgramCount, Is.EqualTo(1));
            Assert.That(report.ReviewCount, Is.EqualTo(1));
        }

        [Test]
        public void ReportUsesStableJsonContractAndIndependentSnapshots()
        {
            var installed = Installed("a");
            var report = ProgramUpdateChecker.Check(new[] { Package("base", Entry("a")) }, new[] { installed }, null);
            var fingerprint = report.Fingerprint;
            installed.Version = "9.0.0";
            Assert.That(report.Items.Single().Instances.Single().Version, Is.EqualTo("1.0.0"));
            Assert.That(report.Fingerprint, Is.EqualTo(fingerprint));
            var json = Newtonsoft.Json.Linq.JObject.Parse(JsonConvert.SerializeObject(report));
            Assert.That((string)json["Status"], Is.EqualTo("Ok"));
            Assert.That((int)json["schemaVersion"], Is.EqualTo(1));
            Assert.That((string)json["items"][0]["repository"], Is.EqualTo("test"));
        }

        [Test]
        public void InvalidAvailableChecksumIsAnExplicitDiagnostic()
        {
            var entry = Entry("a"); entry.Checksum = null;
            var report = ProgramUpdateChecker.Check(new[] { Package("base", entry) }, new[] { Installed("a") }, null);
            Assert.That(report.Complete, Is.False);
            Assert.That(report.Issues.Select(i => i.Code), Does.Contain("invalidChecksum"));
        }

        [TestCase("1.10.0", "1.9.0", 1)]
        [TestCase("1.2", "1.2.0", 0)]
        [TestCase("2.0.0", "2.0.0-preview.3", 1)]
        [TestCase("2.0.0-preview.10", "2.0.0-preview.2", 1)]
        [TestCase("2.0.0-alpha", "2.0.0-beta", -1)]
        [TestCase("2.0.0+build.1", "2.0.0+build.2", 0)]
        [TestCase("2.0.0-1", "2.0.0-alpha", -1)]
        public void ComparesProgramVersionsNumerically(string a, string b, int expected)
        {
            Assert.That(ProgramUpdateChecker.TryCompareVersions(a, b, out var comparison), Is.True);
            Assert.That(Math.Sign(comparison), Is.EqualTo(expected));
        }
    }
}
