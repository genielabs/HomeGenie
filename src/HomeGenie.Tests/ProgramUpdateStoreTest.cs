using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using HomeGenie.Automation;
using HomeGenie.Automation.Engines;
using HomeGenie.Data;
using HomeGenie.Service;
using Newtonsoft.Json;
using NUnit.Framework;

namespace HomeGenie.Tests;

[TestFixture, NonParallelizable]
public class ProgramUpdateStoreTest
{
    private string folder, previousDirectory, database;
    private byte[] previousDatabase;
    private ProgramBlock installed, packaged;
    private ControlledEngine compiler;
    private IProgramUpdateStore store;
    private ProgramUpdateTarget target;
    private HomeGenieService host;

    private static void Set(object owner, string name, object value) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
    private static string Hash(ProgramBlock p) => Utility.GetObjectChecksum(new {setup=p.ScriptSetup,source=p.ScriptSource,context=p.ScriptContext,data=p.Data});

    [SetUp] public void Setup()
    {
        previousDirectory = Environment.CurrentDirectory;
        folder = Path.Combine(Path.GetTempPath(), "hg-program-store-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder); Environment.CurrentDirectory = folder;
        database = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"programs.xml");
        previousDatabase = File.Exists(database) ? File.ReadAllBytes(database) : null;
        host = (HomeGenieService)RuntimeHelpers.GetUninitializedObject(typeof(HomeGenieService));
        var manager = (ProgramManager)RuntimeHelpers.GetUninitializedObject(typeof(ProgramManager));
        var programs = new TsList<ProgramBlock>();
        Set(manager,"automationPrograms",programs); Set(manager,"hgService",host); Set(host,"masterControlProgram",manager);
        installed = new ProgramBlock {Address=1777,Type="javascript",Name="User name",Group="User category",ScriptSource="old source",Data="old code data"};
        installed.PackageInfo = new ProgramPackageInfo {Repository="test",PackageId="base",Id="fixture",Version="1.0.0",Checksum=Hash(installed)};
        compiler = new ControlledEngine(installed); Set(installed,"programEngine",compiler); programs.Add(installed);
        packaged = new ProgramBlock {Address=1777,Type="javascript",Name="Package name",Group="Package category",ScriptSource="new source",Data="new code data"};
        packaged.PackageInfo = new ProgramPackageInfo {Repository="test",PackageId="base",Id="fixture",Version="2.0.0"};
        target = new ProgramUpdateTarget {Repository="test",PackageId="base",Id="fixture",Address=1777,Version="2.0.0",Checksum=Hash(packaged),
            PreviousVersion="1.0.0",PreviousChecksum=Hash(installed),Enabled=false};
        WriteCatalog();
        var owner = new PackageManager(host);
        var type = typeof(PackageManager).GetNestedType("UpdateStore", BindingFlags.NonPublic);
        store = (IProgramUpdateStore)Activator.CreateInstance(type, BindingFlags.Instance|BindingFlags.Public, null, new object[]{owner}, null);
        Directory.CreateDirectory("data/programs/1777"); File.WriteAllText("data/programs/1777/user.txt","preserve me");
        foreach(var file in new[]{"groups.xml","modules.xml","scheduler.xml"})File.WriteAllText(file,"untouched");
    }
    private void WriteCatalog()
    {
        Directory.CreateDirectory("data/packages/test/base");
        File.WriteAllText("data/packages/test/base/package.json",JsonConvert.SerializeObject(new PackageData {Repository="test",Id="base",Version="2.0.0",
            Programs=new(){new PackageDataItem {Repository="test",PackageId="base",Id="fixture",Hid="1777",Version="2.0.0",Checksum=target.Checksum}}}));
        using(var writer=XmlWriter.Create("data/packages/test/base/programs.xml",new XmlWriterSettings {Indent=true}))
            new XmlSerializer(typeof(List<ProgramBlock>)).Serialize(writer,new List<ProgramBlock>{packaged});
        var doc=XDocument.Load("data/packages/test/base/programs.xml");
        doc.Root.Elements("ProgramBlock").Single().SetElementValue("IsEnabled","true");
        // An unrelated malformed program must never be deserialized during this artifact's preflight.
        doc.Root.Add(new XElement("ProgramBlock",new XElement("Address","9999"),new XElement("IsEnabled","not-a-boolean")));
        doc.Save("data/packages/test/base/programs.xml");
    }
    [TearDown] public void Cleanup()
    {
        Environment.CurrentDirectory=previousDirectory;
        if(previousDatabase==null){if(File.Exists(database))File.Delete(database);}else File.WriteAllBytes(database,previousDatabase);
        Directory.Delete(folder,true);
    }
    private string Backup => Path.Combine(folder,"data","program-updates","fixture","0");
    private void AssertUnrelatedData()
    {
        Assert.That(File.ReadAllText("data/programs/1777/user.txt"),Is.EqualTo("preserve me"));
        foreach(var file in new[]{"groups.xml","modules.xml","scheduler.xml"})Assert.That(File.ReadAllText(file),Is.EqualTo("untouched"));
        Assert.That(installed.Name,Is.EqualTo("User name")); Assert.That(installed.Group,Is.EqualTo("User category"));
        Assert.That(installed.IsEnabled,Is.False);
    }
    [Test] public void PreflightReadsOnlyItsDisabledArtifactWithoutCompiling()
    {
        store.ValidateSource(target);
        Assert.That(compiler.Calls,Is.Zero); AssertUnrelatedData();
    }
    [Test] public void SuccessfulUpdatePreservesUserStateAndNeverImportsPackageAssets()
    {
        store.Apply(target,Backup);
        Assert.That(installed.ScriptSource,Is.EqualTo("new source"));
        Assert.That(installed.PackageInfo.Version,Is.EqualTo("2.0.0"));
        Assert.That(compiler.Calls,Is.EqualTo(1)); AssertUnrelatedData();
        Assert.That(XDocument.Load(Path.Combine(Backup,"program.xml")).Root.Element("ScriptSource").Value,Is.EqualTo("old source"));
    }
    [Test] public void CompileFailureRestoresOriginalCodeAndVersionAndLeavesBackupReadable()
    {
        compiler.FailFirst=true;
        var error=Assert.Throws<InvalidOperationException>(()=>store.Apply(target,Backup));
        Assert.That(error.Message,Does.Contain("Previous program restored"));
        Assert.That(installed.ScriptSource,Is.EqualTo("old source")); Assert.That(installed.Data,Is.EqualTo("old code data"));
        Assert.That(installed.PackageInfo.Version,Is.EqualTo("1.0.0")); Assert.That(compiler.Calls,Is.EqualTo(2));
        Assert.That(XDocument.Load(Path.Combine(Backup,"program.xml")),Is.Not.Null); AssertUnrelatedData();
    }
    [TestCase(true)]
    [TestCase(false)] public void ManagedCompileFailureRestoresTheOriginalBinaryWithoutRecompilingIt(bool reportedError)
    {
        installed.Type="csharp"; packaged.Type="csharp";
        compiler=new ControlledEngine(installed){FailFirst=reportedError};Set(installed,"programEngine",compiler);
        target.Checksum=Hash(packaged);WriteCatalog();
        string binary=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"programs","1777.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(binary));
        byte[] previous=File.Exists(binary)?File.ReadAllBytes(binary):null;
        try {
            File.WriteAllText(binary,"original binary fixture");
            Assert.Throws<InvalidOperationException>(()=>store.Apply(target,Backup));
            Assert.That(File.ReadAllText(binary),Is.EqualTo("original binary fixture"));
            Assert.That(installed.PackageInfo.Version,Is.EqualTo("1.0.0"));
            Assert.That(installed.IsEnabled,Is.False);Assert.That(compiler.Calls,Is.EqualTo(1));
        } finally {if(previous==null)File.Delete(binary);else File.WriteAllBytes(binary,previous);}
    }
    [Test] public void RollbackFailureIsExplicitAndKeepsTheProgramDisabled()
    {
        compiler.FailAlways=true;
        var error=Assert.Throws<InvalidOperationException>(()=>store.Apply(target,Backup));
        Assert.That(error.Message,Does.Contain("manual recovery"));
        Assert.That(installed.IsEnabled,Is.False); Assert.That(File.Exists(Path.Combine(Backup,"program.xml")),Is.True);
    }
    [Test] public void ChangedLocalCodeIsNotOverwrittenEvenAfterConfirmation()
    {
        installed.ScriptSource="user edit after confirmation";
        Assert.Throws<InvalidOperationException>(()=>store.Apply(target,Backup));
        Assert.That(installed.ScriptSource,Is.EqualTo("user edit after confirmation")); Assert.That(compiler.Calls,Is.Zero);
    }
    [Test] public void ArtifactChecksumMismatchFailsWithoutMutatingInstalledProgram()
    {
        packaged.ScriptSource="tampered";WriteCatalog();
        Assert.Throws<InvalidDataException>(()=>store.ValidateSource(target));
        Assert.That(installed.ScriptSource,Is.EqualTo("old source")); Assert.That(compiler.Calls,Is.Zero);
    }
    [Test] public void ArtifactPathTraversalIsRejected()
    {
        target.PackageId="../base";
        Assert.Throws<InvalidDataException>(()=>store.ValidateSource(target));
    }
    private sealed class ControlledEngine : ProgramEngineBase,IProgramEngine
    {
        public int Calls; public bool FailFirst,FailAlways;
        public ControlledEngine(ProgramBlock block):base(block){}
        public bool Load()=>true;
        public void Unload(){}
        public void Reset(){}
        public override List<ProgramError> Compile(){Calls++;return FailAlways||FailFirst&&Calls==1?new(){new ProgramError {ErrorMessage="Fixture compiler error"}}:new();}
    }
}
