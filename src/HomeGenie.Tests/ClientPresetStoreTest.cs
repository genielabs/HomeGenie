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
        Assert.That((string)manifest["version"], Is.EqualTo((string)program.Element("PackageInfo").Element("Version")), "Program version must match the XML source.");
        Assert.That((string)manifest["packageVersion"], Is.EqualTo((string)JObject.Parse(File.ReadAllText(Path.Combine(folder, "package.json")))["version"]), "Program packageVersion must match its catalog package.");
        string code = "using System;using System.IO;using System.Linq;using System.Text;using Newtonsoft.Json;using Newtonsoft.Json.Linq;" + """
            public class PresetCommand { public object Data; public string GetOption(int index) => "room"; }
            public class PresetApi { public object Call(string route) => new string[] { "room/panel" }; public System.Collections.Generic.Dictionary<string, Func<object, object>> Handlers = new(); public PresetCommand Parse(object args) => args as PresetCommand ?? new(); public void Handle(string path, Func<object, object> handler) { Handlers[path] = handler; } }
            public class PresetData { public string Root; public string GetFolder(string name) => Root ?? "folder"; public void AddToSystemBackup(string folder) {} }
            public class PresetProgram { public void Run() {} public void GoBackground() {} public System.Collections.Generic.List<Tuple<string, object>> Events = new(); public void Emit(string field, object value) { Events.Add(Tuple.Create(field, value)); } }
            public class Distributed111 { PresetApi Api = new(); PresetData Data = new(); PresetProgram Program = new();
            """ + "void Setup() {" + setup + "} void Run() {" + source + "}" + context + "}" + """
            public class FailingStore : Distributed111.ClientPresetStore {
                public FailingStore(string root) : base(root) {}
                public FailingStore(string root, Action<JObject> publish) : base(root, publish) {}
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
        public JObject ApplyDashboard(string name, JObject payload, string[] catalog) => (JObject)Invoke("ApplyDashboard", name, payload, catalog);
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

    static JObject DashboardRequest(string mode = "create", string name = "My room", long revision = 1) => new() {
        ["mode"] = mode, ["dashboardName"] = name, ["widgetId"] = "room/panel", ["expectedRevision"] = revision,
        ["clientId"] = "ai:origin", ["requestId"] = "goal" };
    [Test] public void DashboardCreationPreservesOtherSettingsAndStandardWidgets() {
        var before=Payload();store.Save("room",before);
        var reply=store.ApplyDashboard("room",DashboardRequest(),new[]{"room/panel"});Ok(reply,2);
        var after=store.Get("room");
        Assert.That(JToken.DeepEquals(after["config"]["dashboards"][0],before["config"]["dashboards"][0]),Is.True);
        Assert.That(JToken.DeepEquals(after["config"]["modules"],before["config"]["modules"]),Is.True);
        Assert.That(JToken.DeepEquals(after["preferences"],before["preferences"]),Is.True);
        Assert.That((string)after["config"]["dashboards"][1]["config"]["layout"]["widget"]["configuration"]["componentId"],Is.EqualTo("room/panel"));
        Assert.That((string)reply["dashboardName"],Is.EqualTo("My room"));
    }
    [Test] public void DashboardUpdatePreservesSameContainerBindingsAndIsNoOpWhenUnchanged() {
        var before=Payload();var dashboard=before["config"]["dashboards"][0];
        dashboard["config"]=JObject.Parse("{theme:{color:'red'},layout:{mode:'standard',widget:{type:'custom-widget',module:{moduleId:'Demo:1'},configuration:{componentId:'room/panel',size:'dashboard',boundModules:{light:'Demo:2'}}}}}");
        store.Save("room",before);Ok(store.ApplyDashboard("room",DashboardRequest("update","local"),new[]{"room/panel"}),2);
        var after=store.Get("room");Assert.That(JToken.DeepEquals(after["config"]["dashboards"][0]["widgets"],dashboard["widgets"]),Is.True);
        Assert.That(JToken.DeepEquals(after["config"]["dashboards"][0]["config"]["layout"]["widget"],dashboard["config"]["layout"]["widget"]),Is.True);
        Ok(store.ApplyDashboard("room",DashboardRequest("update","local",2),new[]{"room/panel"}),2,false);
    }
    [Test] public void StaleDashboardApplicationDoesNotOverwriteOrRebase() {
        store.Save("room",Payload());store.Save("room",Payload("other edit",1));
        Conflict(store.ApplyDashboard("room",DashboardRequest(),new[]{"room/panel"}));
        Assert.That(((JArray)store.Get("room")["config"]["dashboards"]).Count,Is.EqualTo(1));
    }
    [Test] public void DashboardApplicationRequiresCatalogMembershipAndExplicitCreateOrUpdate() {
        store.Save("room",Payload());
        Assert.That((string)store.ApplyDashboard("room",DashboardRequest(),Array.Empty<string>())["Code"],Is.EqualTo("InvalidDashboard"));
        Assert.That((string)store.ApplyDashboard("room",DashboardRequest("create","local"),new[]{"room/panel"})["Code"],Is.EqualTo("DashboardExists"));
        Assert.That((string)store.ApplyDashboard("room",DashboardRequest("update","absent"),new[]{"room/panel"})["Code"],Is.EqualTo("DashboardNotFound"));
        Assert.That((long)store.Get("room")["revision"],Is.EqualTo(1));
    }
    [Test] public void ConcurrentDashboardCreatesOnlyOneConditionalWinner() {
        store.Save("room",Payload());var other=NewStore();
        var results=new JObject[2];Parallel.Invoke(()=>results[0]=store.ApplyDashboard("room",DashboardRequest(name:"One"),new[]{"room/panel"}),
            ()=>results[1]=other.ApplyDashboard("room",DashboardRequest(name:"Two"),new[]{"room/panel"}));
        Assert.That(results.Count(r=>(string)r["Status"]=="Ok"),Is.EqualTo(1));
        Assert.That(((JArray)store.Get("room")["config"]["dashboards"]).Count,Is.EqualTo(2));
    }
    [Test] public void FailedDashboardPublicationLeavesPresetUnchanged() {
        store.Save("room",Payload());string before=File.ReadAllText(Path.Combine(root,"room.json"));
        Assert.Throws<IOException>(()=>NewStore(true).ApplyDashboard("room",DashboardRequest(),new[]{"room/panel"}));
        Assert.That(File.ReadAllText(Path.Combine(root,"room.json")),Is.EqualTo(before));
    }

    [Test] public void FirstSaveAfterReloadAllowsLocalDashboardNavigationOnlyInItsBaseProof() {
        var original=Payload();original["preferences"]["ui"]=new JObject{["dashboard"]="Dashboard",["language"]="it"};store.Save("room",original);
        var loaded=(JObject)original.DeepClone();loaded["preferences"]["ui"]["dashboard"]="Prova AI";
        var request=(JObject)loaded.DeepClone();request["baseContent"]=loaded.DeepClone();
        ((JArray)request["config"]["modules"]).Add(new JObject{["id"]="HomeAutomation.HomeGenie.Automation:1051"});
        Ok(store.Save("room",request),2);
        Assert.That((string)store.Get("room")["preferences"]["ui"]["dashboard"],Is.EqualTo("Prova AI"));
    }
    [TestCase("layout")][TestCase("language")][TestCase("binding")]
    public void ReloadNavigationExceptionNeverRebasesRealRemoteConfigurationChanges(string changed) {
        var original=Payload();original["preferences"]["ui"]=new JObject{["dashboard"]="Dashboard",["language"]="it"};store.Save("room",original);
        var remote=(JObject)original.DeepClone();remote["expectedRevision"]=1;
        if(changed=="layout")remote["config"]["dashboards"][0]["name"]="Other browser";
        if(changed=="language")remote["preferences"]["ui"]["language"]="en";
        if(changed=="binding")remote["config"]["dashboards"][0]["widgets"][0]["configuration"]["boundModules"]["events"]="Demo:3";
        store.Save("room",remote);
        var local=(JObject)original.DeepClone();local["preferences"]["ui"]["dashboard"]="Prova AI";local["baseContent"]=local.DeepClone();
        Conflict(store.Save("room",local));Assert.That((long)store.Get("room")["revision"],Is.EqualTo(2));
    }
    [Test] public void KnownRevisionConflictIsNotBypassedByNavigationException() {
        var original=Payload();original["preferences"]["ui"]=new JObject{["dashboard"]="Dashboard"};store.Save("room",original);
        var newer=(JObject)original.DeepClone();newer["preferences"]["ui"]["dashboard"]="Other";newer["expectedRevision"]=1;store.Save("room",newer);
        var old=(JObject)original.DeepClone();old["baseContent"]=original.DeepClone();old["expectedRevision"]=1;
        Conflict(store.Save("room",old));
    }

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
    private StoreProxy AnnouncingStore(Action<JObject> publish, bool failing = false) =>
        new(Activator.CreateInstance(failing ? failingStoreType : storeType, root, publish));
    [Test] public void EmitsOnlyAfterSaveCommitWithBoundedOriginAndWithoutUserConfiguration() {
        JObject received=null;
        var announcing=AnnouncingStore(change=>{
            Assert.That(JObject.Parse(File.ReadAllText(Path.Combine(root,"room.json")))["revision"]!.Value<long>(),Is.EqualTo(1));
            received=(JObject)change.DeepClone();
        });
        var request=Payload();request["clientId"]="browser-a";request["requestId"]="write-1";
        Ok(announcing.Save("room",request),1);
        Assert.That(received,Is.Not.Null);Assert.That((int)received["version"],Is.EqualTo(1));
        Assert.That((string)received["name"],Is.EqualTo("room"));Assert.That((string)received["operation"],Is.EqualTo("save"));
        Assert.That((string)received["clientId"],Is.EqualTo("browser-a"));Assert.That((string)received["requestId"],Is.EqualTo("write-1"));
        Assert.That(received["config"],Is.Null);Assert.That(announcing.Get("room")["clientId"],Is.Null);
    }
    [Test] public void NoOpConflictAndInvalidInputDoNotEmit() {
        var changes=new System.Collections.Generic.List<JObject>();var announcing=AnnouncingStore(change=>changes.Add(change));
        announcing.Save("room",Payload());changes.Clear();
        Ok(announcing.Save("room",Payload()),1,false);Conflict(announcing.Save("room",Payload("stale",0)));
        announcing.Save("room",new JObject());Assert.That(changes,Is.Empty);
    }
    [Test] public void DeleteEmitsCommittedTombstoneAndRepeatedDeleteDoesNotEmit() {
        JObject received=null;var announcing=AnnouncingStore(change=>received=change);
        announcing.Save("room",Payload());received=null;
        Ok(announcing.Delete("room",new JObject{["expectedRevision"]=1,["clientId"]="browser-b",["requestId"]="delete-1"}),2);
        Assert.That((string)received["operation"],Is.EqualTo("delete"));Assert.That((long)received["revision"],Is.EqualTo(2));
        Assert.That((string)received["requestId"],Is.EqualTo("delete-1"));Assert.That((bool)announcing.Get("room")["deleted"],Is.True);
        received=null;Ok(announcing.Delete("room",new JObject{["expectedRevision"]=2}),2,false);Assert.That(received,Is.Null);
    }
    [Test] public void FailedPublicationDoesNotEmitOrAdvanceRevision() {
        store.Save("room",Payload());int events=0;var failing=AnnouncingStore(_=>events++,true);
        Assert.Throws<IOException>(()=>failing.Save("room",Payload("edited",1)));
        Assert.That(events,Is.Zero);Assert.That((long)store.Get("room")["revision"],Is.EqualTo(1));
    }
    [Test] public void FailedEventDeliveryDoesNotTurnACommittedSaveIntoAnError() {
        var announcing=AnnouncingStore(_=>throw new InvalidOperationException("event transport failed"));
        Ok(announcing.Save("room",Payload()),1);Assert.That((long)announcing.Get("room")["revision"],Is.EqualTo(1));
    }
    [Test] public void EventsKeepCommitOrderAndInvalidOriginMetadataIsNotBroadcast() {
        var changes=new System.Collections.Generic.List<JObject>();var announcing=AnnouncingStore(change=>changes.Add(change));
        var request=Payload();request["clientId"]=new string('x',129);request["requestId"]=new JObject();announcing.Save("room",request);
        announcing.Save("room",Payload("edited",1));Assert.That(changes.Select(c=>(long)c["revision"]),Is.EqualTo(new long[]{1,2}));
        foreach(var change in changes) {Assert.That((string)change["clientId"],Is.Empty);Assert.That((string)change["requestId"],Is.Empty);}
    }

    [TestCase("object")][TestCase("bytes")][TestCase("text")][TestCase("dictionary")]
    public void DashboardApiHandlerAcceptsNativeAndHttpPayloads(string transport) {
        store.Save("room",Payload());
        var type=storeType.DeclaringType;var instance=Activator.CreateInstance(type);
        var data=type.GetField("Data",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(instance);
        data.GetType().GetField("Root").SetValue(data,root);
        type.GetMethod("Run",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(instance,null);
        var api=type.GetField("Api",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(instance);
        var handlers=(System.Collections.Generic.Dictionary<string,Func<object,object>>)api.GetType().GetField("Handlers").GetValue(api);
        var command=Activator.CreateInstance(type.Assembly.GetType("PresetCommand"));
        var payload=DashboardRequest();
        object body=transport switch {
            "object"=>payload,
            "bytes"=>System.Text.Encoding.UTF8.GetBytes(payload.ToString()),
            "text"=>payload.ToString(),
            _=>payload.ToObject<System.Collections.Generic.Dictionary<string,object>>()
        };
        command.GetType().GetField("Data").SetValue(command,body);
        var reply=(JObject)handlers["HomeAutomation.HomeGenie/Config/Client.PresetsApplyDashboard/"](command);
        Ok(reply,2);Assert.That((string)reply["dashboardName"],Is.EqualTo("My room"));
        Assert.That((string)store.Get("room")["config"]["dashboards"][1]["config"]["layout"]["widget"]["configuration"]["componentId"],Is.EqualTo("room/panel"));
    }

    [Test] public void DistributedApiHandlerEmitsTheDeclaredPropertyOnlyForCommittedChanges() {
        var type=storeType.DeclaringType;var instance=Activator.CreateInstance(type);
        var data=type.GetField("Data",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(instance);
        data.GetType().GetField("Root").SetValue(data,root);
        type.GetMethod("Run",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(instance,null);
        var api=type.GetField("Api",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(instance);
        var handlers=(System.Collections.Generic.Dictionary<string,Func<object,object>>)api.GetType().GetField("Handlers").GetValue(api);
        var command=Activator.CreateInstance(type.Assembly.GetType("PresetCommand"));
        var payload=Payload();payload["clientId"]="browser-api";payload["requestId"]="api-save";
        command.GetType().GetField("Data").SetValue(command,System.Text.Encoding.UTF8.GetBytes(payload.ToString()));
        var save=handlers["HomeAutomation.HomeGenie/Config/Client.PresetsAdd/"];
        Ok((JObject)save(command),1);Ok((JObject)save(command),1,false);
        var program=type.GetField("Program",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(instance);
        var events=(System.Collections.Generic.List<Tuple<string,object>>)program.GetType().GetField("Events").GetValue(program);
        Assert.That(events.Count,Is.EqualTo(1));Assert.That(events[0].Item1,Is.EqualTo("Client.PresetsChanged"));
        var notice=JObject.Parse((string)events[0].Item2);Assert.That((string)notice["clientId"],Is.EqualTo("browser-api"));
        Assert.That((string)notice["requestId"],Is.EqualTo("api-save"));
    }

}
