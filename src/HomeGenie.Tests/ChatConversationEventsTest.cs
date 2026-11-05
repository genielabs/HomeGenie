using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace HomeGenie.Tests;

[TestFixture]
public class ChatConversationEventsTest {
    Assembly assembly;
    string folder;
    [OneTimeSetUp] public void CompileActualHandlers() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        string packages=Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie");
        XElement Program(string package,string id)=>XDocument.Load(Path.Combine(packages,package,"programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")==id);
        var history=Program("homegenie-base","577");
        string code="using System;using System.IO;using System.Linq;using System.Text;using System.Collections.Generic;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+
            (string)history.Element("ScriptContext")+@"
public enum Status {Error}
public class ResponseStatus {public Status Status;public string Message;public ResponseStatus(Status status,string message){Status=status;Message=message;}}
public class FakeRequest {public string Command;public object Data;}
public class FakeApi {
    public Dictionary<string,Func<object,object>> Routes=new Dictionary<string,Func<object,object>>();
    public string LastRoute;public object LastData;
    public FakeApi Handle(string route,Func<object,object> handler){Routes[route]=handler;return this;}
    public FakeRequest Parse(object input)=>input as FakeRequest;
    public object Call(string route,object data){LastRoute=route;LastData=data;return new{Status=""Ok""};}
}
public class FakeProgram {public void GoBackground(){}}
public class FakeData {public string Folder;public string GetFolder()=>Folder;}
public class FakeModules {
    public string Domain,Address,Folder;public List<JObject> Events=new List<JObject>();
    public FakeModules InDomain(string value){Domain=value;return this;}
    public FakeModules WithAddress(string value){Address=value;return this;}
    public FakeModules Get()=>this;
    public void Emit(string key,string value){
        if(System.Threading.Monitor.IsEntered(HistoryStore.SyncRoot))throw new Exception(""Event emitted while holding history lock"");
        var hint=JObject.Parse(value);var stored=JObject.Parse(File.ReadAllText(Path.Combine(Folder,""history_""+Address+"".json"")));
        if((long)stored[""revision""]!=(long)hint[""revision""])throw new Exception(""Event preceded persistence"");
        Events.Add(new JObject{[""domain""]=Domain,[""address""]=Address,[""key""]=key,[""value""]=hint});
    }
}
public class HistoryRuntime {
    public FakeApi Api=new FakeApi();public FakeProgram Program=new FakeProgram();public FakeData Data=new FakeData();public FakeModules Modules=new FakeModules();
    public HistoryRuntime(string folder){Data.Folder=folder;Modules.Folder=folder;HistoryStore.Stores.Clear();Register();}
    void Register(){"+(string)history.Element("ScriptSource")+@"}
    public object Call(string command,JObject data){data[""StoreId""]=""933"";return Api.Routes[""System.Utility/ChatHistory""](new FakeRequest{Command=command,Data=data});}
    public JArray Events()=>JArray.FromObject(Modules.Events);
}";
        foreach(var item in new[]{("933","Gemini"),("940","Lailama")}) {
            string source=(string)Program("homegenie-ml-ai",item.Item1).Element("ScriptSource");
            int end=source.IndexOf("// Provider-scoped attachment routes",StringComparison.Ordinal);
            Assert.That(end,Is.GreaterThan(0));
            code+="public class Wrapper"+item.Item1+" {public FakeApi Api=new FakeApi();public Wrapper"+item.Item1+"(){"+source.Substring(0,end)+"}}";
        }
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
            .Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location))
            .Append(typeof(JsonConvert).Assembly.Location).Append(typeof(SixLabors.ImageSharp.Image).Assembly.Location).Distinct();
        var compilation=CSharpCompilation.Create("ChatEvents"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},
            paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var output=new MemoryStream();var result=compilation.Emit(output);
        Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));assembly=Assembly.Load(output.ToArray());
    }
    [SetUp] public void Setup(){folder=Path.Combine(Path.GetTempPath(),"hg-chat-events-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);}
    [TearDown] public void Cleanup(){Directory.Delete(folder,true);}
    object Runtime()=>Activator.CreateInstance(assembly.GetType("HistoryRuntime"),folder);
    object Invoke(object runtime,string method,params object[] args)=>runtime.GetType().GetMethod(method).Invoke(runtime,args);
    JObject Call(object runtime,string command,JObject input=null)=>JObject.FromObject(Invoke(runtime,"Call",command,input??new JObject()));
    JArray Events(object runtime)=>(JArray)Invoke(runtime,"Events");

    [Test] public void NotificationFollowsSaveAndTargetsTheGenieWithOnlyIdentityAndRevision() {
        var runtime=Runtime();var snapshot=Call(runtime,"Conversation.Get");
        var events=Events(runtime);Assert.That(events.Count,Is.EqualTo(1));
        Assert.That((string)events[0]["address"],Is.EqualTo("933"));
        Assert.That((string)events[0]["key"],Is.EqualTo("Chat.Changed"));
        Assert.That(((JObject)events[0]["value"]).Properties().Select(p=>p.Name),Is.EquivalentTo(new[]{"version","conversationId","revision"}));
        Assert.That((string)events[0]["value"]["conversationId"],Is.EqualTo((string)snapshot["conversationId"]));
    }
    [Test] public void ConditionalReadReturnsNoMessagesAndDoesNotEmitAnotherEvent() {
        var runtime=Runtime();var snapshot=Call(runtime,"Conversation.Get");
        var same=Call(runtime,"Conversation.Get",new JObject{["conversationId"]=snapshot["conversationId"],["revision"]=snapshot["revision"]});
        Assert.That((bool)same["unchanged"],Is.True);Assert.That(same["messages"],Is.Null);
        Assert.That(Events(runtime).Count,Is.EqualTo(1));
    }
    [Test] public void OrdinaryAddDeletionAndClearAreAlsoVisibleToOtherClients() {
        var runtime=Runtime();var before=Call(runtime,"Conversation.Get");
        Call(runtime,"Add",new JObject{["Message"]=new JObject{["role"]="user",["parts"]=new JArray(new JObject{["text"]="hello"})}});
        var added=Call(runtime,"Conversation.Get",new JObject{["conversationId"]=before["conversationId"],["revision"]=before["revision"]});
        Assert.That((bool)added["unchanged"],Is.False);Assert.That(added["messages"].Count(),Is.EqualTo(1));
        Call(runtime,"RemoveAt",new JObject{["Index"]=0});
        var deleted=Call(runtime,"Conversation.Get");Assert.That(deleted["messages"],Is.Empty);
        Call(runtime,"Clear");var cleared=Call(runtime,"Conversation.Get");
        Assert.That((string)cleared["conversationId"],Is.Not.EqualTo((string)before["conversationId"]));
        Assert.That(Events(runtime).Count,Is.EqualTo(4));
    }
    [Test] public void FailedSaveAndRejectedTaskUpdatesDoNotAnnounceSuccess() {
        var runtime=Runtime();var session=Call(runtime,"Conversation.Get");
        Call(runtime,"Task.Create",new JObject{["conversationId"]=Guid.NewGuid().ToString("N"),["taskId"]=Guid.NewGuid().ToString("N"),["title"]="wrong conversation"});
        string file=Path.Combine(folder,"history_933.json");File.Delete(file);Directory.CreateDirectory(file);
        var failed=Call(runtime,"Task.Create",new JObject{["conversationId"]=session["conversationId"],["taskId"]=Guid.NewGuid().ToString("N"),["title"]="cannot save"});
        Assert.That((string)failed["Status"],Is.EqualTo("Error"));Assert.That(Events(runtime).Count,Is.EqualTo(1));
    }
    [TestCase("933","Gemini")] [TestCase("940","Lailama")]
    public void ProviderWrappersFixOwnerAndAreRegisteredWithoutInitializingAModel(string id,string name) {
        var wrapper=Activator.CreateInstance(assembly.GetType("Wrapper"+id));
        var api=wrapper.GetType().GetField("Api").GetValue(wrapper);
        var routes=(System.Collections.IDictionary)api.GetType().GetField("Routes").GetValue(api);
        foreach(string operation in new[]{"Conversation.Get","Task.Create","Task.Get","Task.Update","Task.Cancel"}) {
            var input=new JObject{["StoreId"]="another-owner",["conversationId"]="conversation"};
            var request=Activator.CreateInstance(assembly.GetType("FakeRequest"));request.GetType().GetField("Data").SetValue(request,input);
            ((Delegate)routes["AI.Genies/"+name+"/"+operation]).DynamicInvoke(request);
            Assert.That((string)api.GetType().GetField("LastRoute").GetValue(api),Is.EqualTo("System.Utility/ChatHistory/"+operation));
            Assert.That((string)((JObject)api.GetType().GetField("LastData").GetValue(api))["StoreId"],Is.EqualTo(id));
            Assert.That((string)input["StoreId"],Is.EqualTo("another-owner"));
        }
    }
}
