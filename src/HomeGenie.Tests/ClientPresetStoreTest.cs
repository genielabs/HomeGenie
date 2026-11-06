using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Xml.Linq;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace HomeGenie.Tests;

[TestFixture]
public class ClientPresetStoreTest
{
    private string root;
    private StoreProxy store;
    private Type storeType, failingStoreType;
    [OneTimeSetUp] public void CompileDistributedProgram()
    {
        var repo = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (repo != null && !File.Exists(Path.Combine(repo.FullName, "HomeGenie.sln"))) repo = repo.Parent;
        Assert.That(repo, Is.Not.Null, "Cannot locate distributed program 111.");
        string folder = Path.Combine(repo.FullName, "assets/build/all/data/packages/homegenie/homegenie-base");
        var program = XDocument.Load(Path.Combine(folder, "programs.xml")).Root.Elements()
            .Single(p => (string)p.Element("Address") == "111");
        string setup = (string)program.Element("ScriptSetup"), source = (string)program.Element("ScriptSource"), context = (string)program.Element("ScriptContext");
        string checksum = Convert.ToHexString(MD5.HashData(System.Text.Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(new { setup, source, context, data = (string)program.Element("Data") }))));
        var manifest = JObject.Parse(File.ReadAllText(Path.Combine(folder, "package.json")))["programs"].Single(p => (string)p["hid"] == "111");
        Assert.That(checksum, Is.EqualTo((string)program.Element("PackageInfo").Element("Checksum")));
        Assert.That(checksum, Is.EqualTo((string)manifest["checksum"]));
        string code = "using System;using System.IO;using System.Linq;using System.Text;using Newtonsoft.Json;using Newtonsoft.Json.Linq;" + """
            public class PresetCommand { public object Data; public string GetOption(int index) => "room"; }
            public class PresetApi { public PresetCommand Parse(object args) => new(); public void Handle(string path, Func<object, object> handler) {} }
            public class PresetData { public string GetFolder(string name) => "folder"; public void AddToSystemBackup(string folder) {} }
            public class PresetProgram { public void Run() {} public void GoBackground() {} }
            public class Distributed111 { PresetApi Api = new(); PresetData Data = new(); PresetProgram Program = new();
            """ + "void Setup() {" + setup + "} void Run() {" + source + "}" + context + "}" + """
            public class FailingStore : Distributed111.ClientPresetStore {
                public FailingStore(string root) : base(root) {}
                protected override void PublishFile(string temporary, string destination) => throw new IOException("simulated publication failure");
            }
            """;
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
            .Where(p => Path.GetDirectoryName(p) == Path.GetDirectoryName(typeof(object).Assembly.Location))
            .Append(typeof(JObject).Assembly.Location).Distinct();
        var compilation = CSharpCompilation.Create("PresetProgramProbe" + Guid.NewGuid().ToString("N"), new[] { CSharpSyntaxTree.ParseText(code) },
            paths.Select(p => MetadataReference.CreateFromFile(p)), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream(); var result = compilation.Emit(stream);
        Assert.That(result.Success, Is.True, string.Join("\n", result.Diagnostics));
        var assembly = Assembly.Load(stream.ToArray());
        storeType = assembly.GetType("Distributed111+ClientPresetStore"); failingStoreType = assembly.GetType("FailingStore");
        // A second compilation has different statics. Its store must still share the folder lock.
        using var secondStream = new MemoryStream();
        var second = compilation.WithAssemblyName("PresetProgramProbe" + Guid.NewGuid().ToString("N")).Emit(secondStream);
        Assert.That(second.Success, Is.True, string.Join("\n", second.Diagnostics));
        otherStoreType = Assembly.Load(secondStream.ToArray()).GetType("Distributed111+ClientPresetStore");
    }
    private Type otherStoreType;
    private StoreProxy NewStore(bool failing = false) => new(Activator.CreateInstance(failing ? failingStoreType : storeType, root));
    private sealed class StoreProxy(object instance)
    {
        private object Invoke(string method, params object[] args)
        {
            try { return instance.GetType().GetMethod(method).Invoke(instance, args); }
            catch (TargetInvocationException error) { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
        }
        public JObject Save(string name, JObject payload) => (JObject)Invoke("Save", name, payload);
        public JObject Get(string name) => (JObject)Invoke("Get", name);
        public JObject Delete(string name, JObject payload) => (JObject)Invoke("Delete", name, payload);
        public string[] List() => (string[])Invoke("List");
    }
    [SetUp] public void SetUp() {root = Path.Combine(Path.GetTempPath(), "hg-presets-" + Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);store = NewStore();}
    [TearDown] public void TearDown() {Directory.Delete(root, true);}
    private static JObject Payload(string name = "local", long? revision = null) {
        var value = JObject.Parse("{config:{modules:[{id:'Demo:1',fields:[{key:'Sensor.Value',value:1,timestamp:1}]}],dashboards:[{name:'room',widgets:[{configuration:{boundModules:{events:'Demo:1',adapters:'Demo:2'}}}]}]},preferences:{theme:'dark'}}");
        value["config"]["dashboards"][0]["name"] = name;
        if (revision.HasValue) value["expectedRevision"] = revision.Value;
        return value;
    }
    private static void Ok(JObject receipt, long revision, bool changed = true) {
        Assert.That(receipt["Status"]!.Value<string>(), Is.EqualTo("Ok"));Assert.That(receipt["revision"]!.Value<long>(), Is.EqualTo(revision));Assert.That(receipt["changed"]!.Value<bool>(), Is.EqualTo(changed));
    }
    private static void Conflict(JObject receipt) {Assert.That(receipt["Code"]!.Value<string>(), Is.EqualTo("PresetConflict"));}

    [Test] public void NewPresetAndConditionalSaveIncrementRevision() {
        Ok(store.Save("room", Payload()), 1);Ok(store.Save("room", Payload("edited",1)),2);
        Assert.That(store.Get("room")["config"]!["dashboards"]![0]!["name"]!.Value<string>(),Is.EqualTo("edited"));
    }
    [Test] public void NoOpAndRuntimeSamplesDoNotRewriteOrIncrement() {
        store.Save("room",Payload());string before = File.ReadAllText(Path.Combine(root,"room.json"));
        var payload=Payload();payload["config"]!["modules"]![0]!["fields"]![0]!["value"]=42;
        Ok(store.Save("room",payload),1,false);Assert.That(File.ReadAllText(Path.Combine(root,"room.json")),Is.EqualTo(before));
    }
    [Test] public void BindingNamesAndPreferencesStillCountAsEdits() {
        store.Save("room",Payload());var payload=Payload(revision:1);
        payload["config"]!["dashboards"]![0]!["widgets"]![0]!["configuration"]!["boundModules"]!["events"]="Demo:3";
        Ok(store.Save("room",payload),2);payload["expectedRevision"]=2;payload["preferences"]!["theme"]="light";Ok(store.Save("room",payload),3);
    }
    [Test] public void ObjectKeyOrderDoesNotCreateRevision() {
        var a=Payload();store.Save("room",a);var b=Payload();b["preferences"]=new JObject { ["b"]=2,["a"]=1 };b["expectedRevision"]=1;
        Ok(store.Save("room",b),2);b["preferences"]=new JObject { ["a"]=1,["b"]=2 };Ok(store.Save("room",b),2,false);
    }
    [Test] public void StaleAndUnconditionalChangedWritesAreRejected() {
        store.Save("room",Payload());store.Save("room",Payload("second",1));
        Conflict(store.Save("room",Payload("stale",1)));Conflict(store.Save("room",Payload("old-client")));
        Assert.That(store.Get("room")["revision"]!.Value<long>(),Is.EqualTo(2));
    }
    [Test] public void LoadedContentProofWorksOnlyWhileCurrentContentMatches() {
        var baseline=Payload();store.Save("room",baseline);var change=Payload("edit");change["baseContent"]=baseline;
        Ok(store.Save("room",change),2);var stale=Payload("stale");stale["baseContent"]=baseline;Conflict(store.Save("room",stale));
    }
    [Test] public void LegacyReadIsReadOnlyAndFirstRealChangeMigrates() {
        string file=Path.Combine(root,"room.json"), original=Payload().ToString();File.WriteAllText(file,original);
        Assert.That(store.Get("room")["revision"]!.Value<long>(),Is.EqualTo(1));Ok(store.Save("room",Payload()),1,false);
        Assert.That(File.ReadAllText(file),Is.EqualTo(original));Ok(store.Save("room",Payload("edit",1)),2);
    }
    [Test] public void DeleteAndRecreateKeepGenerationAndPreventStaleRevival() {
        store.Save("room",Payload());Conflict(store.Delete("room",new JObject()));
        Ok(store.Delete("room",new JObject{["expectedRevision"]=1}),2);Assert.That(store.List(),Is.Empty);
        Conflict(store.Save("room",Payload("stale",1)));Conflict(store.Save("room",Payload("old-client")));
        Ok(store.Save("room",Payload("new",2)),3);Conflict(store.Save("room",Payload("stale",2)));
    }
    [Test] public void CreateOnlyNeverOverwritesAnExistingPreset() {
        store.Save("room",Payload());var request=Payload("different",1);request["createOnly"]=true;Conflict(store.Save("room",request));
    }
    [Test] public void RecompiledProgramSharesTheExistingFolderLock() {
        var a = Activator.CreateInstance(storeType, root);
        var b = Activator.CreateInstance(otherStoreType, root);
        var firstGate = storeType.GetField("gate", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(a);
        var nextGate = otherStoreType.GetField("gate", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(b);
        Assert.That(nextGate, Is.SameAs(firstGate));
    }
    [Test] public void SeparatelyCompiledProgramsCoordinateConcurrentWrites() {
        store.Save("room",Payload());var second=new StoreProxy(Activator.CreateInstance(otherStoreType, root));
        var tasks=new[]{Task.Run(()=>store.Save("room",Payload("a",1))),Task.Run(()=>second.Save("room",Payload("b",1)))};
        Task.WaitAll(tasks);Assert.That(tasks.Count(t=>t.Result["Status"]!.Value<string>()=="Ok"),Is.EqualTo(1));Assert.That(store.Get("room")["revision"]!.Value<long>(),Is.EqualTo(2));
    }
    [Test] public void FailedPublicationLeavesPreviousFileAndCleansOnlyOwnTemporaryFile() {
        store.Save("room",Payload());string before=File.ReadAllText(Path.Combine(root,"room.json"));
        File.WriteAllText(Path.Combine(root,"unrelated.tmp"),"keep");
        Assert.Throws<IOException>(()=>NewStore(true).Save("room",Payload("edit",1)));
        Assert.That(File.ReadAllText(Path.Combine(root,"room.json")),Is.EqualTo(before));Assert.That(Directory.GetFiles(root,".preset-*.tmp"),Is.Empty);Assert.That(File.ReadAllText(Path.Combine(root,"unrelated.tmp")),Is.EqualTo("keep"));
    }
    [Test] public void InvalidExistingDataIsNotTreatedAsMissing() {
        File.WriteAllText(Path.Combine(root,"room.json"),"broken");Assert.Throws<Newtonsoft.Json.JsonReaderException>(()=>store.Save("room",Payload()));
        Assert.That(File.ReadAllText(Path.Combine(root,"room.json")),Is.EqualTo("broken"));
    }
    [Test] public void InvalidPayloadAndDefaultDeletionLeaveDataUntouched() {
        Assert.That(store.Save("room",new JObject())["Status"]!.Value<string>(),Is.EqualTo("Error"));Assert.That(store.List(),Is.Empty);
        store.Save("default",Payload());Assert.That(store.Delete("default",new JObject{["expectedRevision"]=1})["Status"]!.Value<string>(),Is.EqualTo("Error"));
    }
    [Test] public void UnicodeNamesRoundTripAndTemporaryFilesAreNotPresets() {
        Ok(store.Save("città-தமிழ்",Payload()),1);File.WriteAllText(Path.Combine(root,"foreign.tmp"),"temporary");Assert.That(store.List(),Is.EqualTo(new[]{"città-தமிழ்"}));
    }
}
