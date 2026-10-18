using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace HomeGenie.Tests;
[TestFixture]
public class AiGenieCoordinatorTest {
    Assembly compiled;
    Type coordinator,contract;
    int generations,deployments,executions,recoveries,reads;
    bool running,stopAfterGeneration,normalizerUnavailable;
    int compileFailures;
    string folder;
    readonly List<JArray> observedMessages=new();
    readonly List<JObject> observedDeployments=new();
    readonly Queue<string> outputs=new();
    [OneTimeSetUp] public void CompileShippedCoordinatorAndHistoryBridge() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        XElement Program(string pkg,string id)=>XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie",pkg,"programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")==id);
        string Class(XElement p,string name)=>CSharpSyntaxTree.ParseText("class Holder {"+(string)p.Element("ScriptContext")+"}").GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text==name).ToFullString();
        string code="using System;using System.IO;using System.Linq;using System.Text;using System.Threading;using System.Collections.Generic;using System.Security.Cryptography;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+
            (string)Program("homegenie-base","583").Element("ScriptContext")+Class(Program("homegenie-base","581"),"GenieResponseContract")+Class(Program("homegenie-ml-ai","933"),"ChatRequestMetrics");
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location);
        var compilation=CSharpCompilation.Create("AiCoordinator"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var result=compilation.Emit(stream);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));compiled=Assembly.Load(stream.ToArray());coordinator=compiled.GetType("AiGenieCoordinator");contract=compiled.GetType("GenieResponseContract");
    }
    [SetUp] public void Reset() {
        generations=deployments=executions=recoveries=reads=compileFailures=0;running=true;stopAfterGeneration=normalizerUnavailable=false;
        observedMessages.Clear();observedDeployments.Clear();outputs.Clear();folder=Path.Combine(Path.GetTempPath(),"hg-coordinator-"+Guid.NewGuid());Directory.CreateDirectory(folder);
    }
    [TearDown] public void Cleanup(){Directory.Delete(folder,true);}
    object Invoke(string name,string method,params object[] args) {
        try{return compiled.GetType(name).GetMethod(method).Invoke(null,args);}
        catch(TargetInvocationException ex){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();throw;}
    }
    Dictionary<string,object> Request(bool devices=true)=>new(){
        ["version"]=1,["requestId"]="run-id",["owner"]="933",["provider"]="Fake",["elapsedBefore"]=0L,["scheduled"]=false,
        ["template"]="custom instructions",["systemPrompt"]="prepared prompt",["output"]=new object(),
        ["messages"]=JArray.Parse("[{'role':'user','parts':[{'text':'test'}]}]"),
        ["request"]=JObject.FromObject(new{version=1,message="test",context=new{devices,automationPrograms=true,widgets=true,dashboards=false,deepKnowledge=false},capabilities=new{deviceControl=true,automation=true,widgets=true}}),
        ["preparedContext"]=JObject.FromObject(new{systemPrompt="prepared prompt",profileKey="profile",context=new{devices,automationPrograms=true,widgets=true,dashboards=false,deepKnowledge=false},capabilities=new{deviceControl=true,automation=true,widgets=true}})
    };
    JObject Run(Dictionary<string,object> request=null)=> (JObject)Invoke("AiGenieCoordinator","Run",request??Request(),(Func<string,object,object>)Call,folder,(Func<bool>)(()=>running));
    object Call(string route,object payload) {
        if(route=="System.Utility/AiGenie/Generate") {
            generations++;var data=(IDictionary<string,object>)payload;observedMessages.Add((JArray)((JArray)data["messages"]).DeepClone());
            if(stopAfterGeneration)running=false;
            if(outputs.Count==0)throw new Exception("No fake output available");
            return new Dictionary<string,object>{["version"]=1,["requestId"]=data["requestId"],["state"]="completed",["text"]=outputs.Dequeue(),["error"]=null};
        }
        if(route=="System.Utility/ActionExecutor/Response.Normalize") {
            if(normalizerUnavailable)return null;
            try{return contract.GetMethod("Normalize").Invoke(null,new[]{payload});}
            catch(TargetInvocationException ex){return new JObject{["Status"]="Error",["Message"]=ex.InnerException.Message};}
        }
        if(route=="System.Utility/ContextEngine/Context.Policy")return new JObject{["context"]=((JObject)payload)["context"].DeepClone(),["profileKey"]="profile",["prompt"]="policy"};
        if(route=="System.Utility/ContextEngine/Prompt.Compose")return new JObject{["prompt"]="expanded prompt"};
        if(route=="System.Utility/ContextEngine/Render")return "device directory";
        if(route.StartsWith("HomeAutomation.HomeGenie/Config/Modules.Get/")&&route.EndsWith("/Schema")){reads++;return JObject.Parse("{'version':1,'module':{'domain':'Test.Lights','address':'1'},'properties':[],'features':[]}");}
        if(route=="HomeAutomation.HomeGenie/Automation/Programs.List")return JArray.Parse("[{'Address':'1001','Name':'test','ScriptSetup':'Program.Run();','ScriptSource':'old source','ScriptContext':''}]");
        if(route=="HomeAutomation.HomeGenie/Config/Widgets.List")return new JArray();
        if(route=="System.Utility/AiGenie/Device.Execute")return Invoke("AiGenieDeviceActions","Dispatch",payload,(Func<string,object,object>)Call,(Func<string,string,bool>)((d,a)=>true));
        if(route=="System.Utility/AiGenie/Program.Execute")return Invoke("AiGenieProgramActions","Dispatch",payload,(Func<string,object,object>)Call);
        if(route=="System.Utility/AiGenie/Composite.Execute")return Invoke("AiGenieCompositeActions","Dispatch",payload,(Func<string,object,object>)Call);
        if(route=="System.Utility/ActionExecutor/Execute") {
            executions++;var data=JObject.Parse((string)payload);var commands=((string)data["response"]).Split('\n').Skip(1).Where(c=>c!="```");
            return new JArray(commands.Select((c,i)=>new JObject{["Index"]=i,["RequestId"]=(string)data["requestId"],["Command"]=c,["Success"]=true,["State"]="completed"}));
        }
        if(route=="System.Utility/ActionExecutor/Program.Deploy") {
            deployments++;var data=JObject.Parse((string)payload);observedDeployments.Add((JObject)data.DeepClone());bool failed=deployments<=compileFailures;
            return new JObject{["RequestId"]=(string)data["requestId"],["Index"]=(int)data["attempt"],["State"]=failed?"failed":"completed",["Success"]=!failed,["Response"]=new JObject{["status"]=failed?"compile_failed":"success",["program_id"]="1001",["compiler_errors"]=new JArray("diagnostic")}};
        }
        if(route=="System.Utility/ActionExecutor/Program.Recover"){recoveries++;return new JObject{["RequestId"]="run-id",["State"]="completed",["Success"]=true};}
        throw new Exception("Unexpected API (history must stay in the wrapper): "+route);
    }
    string Reply()=>"{'version':1,'kind':'reply','explanation':'hello'}".Replace('\'','"');
    string Device()=>"{'version':1,'kind':'device','explanation':'on','commands':['Test.Lights/1/Control.On']}".Replace('\'','"');
    string Lookup()=>"{'version':1,'kind':'context','explanation':'read','resources':{'moduleIds':['Test.Lights/1']}}".Replace('\'','"');
    string Program(string mode="create",string target="")=>JObject.FromObject(new{version=1,kind="program",explanation="program",language="csharp",automation=new{action=mode,program_id=target,name="test",setup="Program.Run();",main="Program.GoBackground();",context=""}}).ToString(Formatting.None);
    string Composite(string property="Sensor.Counter") {
        var response=JObject.Parse(Program());response["kind"]="composite";response["widget"]=JObject.Parse("{'html':'<div>counter</div>','css':'','javascript':'class Demo {}'}");
        response["binding"]=new JObject{["domain"]="HomeAutomation.HomeGenie.Automation",["address"]="$program",["properties"]=new JArray(property)};return response.ToString(Formatting.None);
    }
    [Test] public void PlainReplyHasNoSideEffectsOrHistoryWritesAndInputsStayUnchanged() {
        outputs.Enqueue(Reply());var request=Request();string before=((JArray)request["messages"]).ToString();var result=Run(request);
        Assert.That((string)result["reply"],Is.EqualTo("hello"+Environment.NewLine));Assert.That((string)result["outcome"],Is.EqualTo("completed"));Assert.That((int)result["metrics"]["actionCount"],Is.Zero);Assert.That(((JArray)request["messages"]).ToString(),Is.EqualTo(before));Assert.That(executions+deployments,Is.Zero);
    }
    [TestCase("")] [TestCase(".demo { color: red; }")]
    public void StandaloneWidgetKeepsCodeFencesAndCssFallback(string css) {
        var response=JObject.Parse("{'version':1,'kind':'widget','explanation':'widget','widget':{'html':'<div>hello</div>','css':'','javascript':'class Demo {}'}}");response["widget"]["css"]=css;outputs.Enqueue(response.ToString());
        var result=Run();Assert.That((string)result["reply"],Does.Contain("```html\n<div>hello</div>\n```").And.Contain("```javascript\nclass Demo {}\n```"));
        Assert.That((string)result["reply"],Does.Contain(css==""?"assets/styles/widget-ui.css":css));Assert.That(executions+deployments,Is.Zero);
    }
    [Test] public void ContextLookupAddsOnlyReferenceDataAndUsesANewGeneration() {
        outputs.Enqueue(Lookup());outputs.Enqueue(Reply());var result=Run();Assert.That(generations,Is.EqualTo(2));Assert.That(reads,Is.EqualTo(1));Assert.That(observedMessages[1].ToString(),Does.Contain("REQUESTED REFERENCE DATA").And.Contain("moduleDescriptions"));Assert.That((string)result["outcome"],Is.EqualTo("completed"));
    }
    [Test] public void ContextLookupLimitStopsBeforeAFourthRead() {
        for(int i=0;i<4;i++)outputs.Enqueue(Lookup());var result=Run();Assert.That(generations,Is.EqualTo(4));Assert.That(reads,Is.EqualTo(3));Assert.That((string)result["error"],Does.Contain("Too many context"));
    }
    [Test] public void DisabledDevicesContextPreventsTargetedReads() {
        outputs.Enqueue(Lookup());var result=Run(Request(false));Assert.That(reads,Is.Zero);Assert.That((string)result["error"],Does.Contain("Enable Devices"));
    }
    [Test] public void InvalidModelOutputHasAtMostTwoRepairsAndNeverActs() {
        outputs.Enqueue("{");outputs.Enqueue("{");outputs.Enqueue("{");var result=Run();Assert.That(generations,Is.EqualTo(3));Assert.That(observedMessages[1].ToString(),Does.Contain("RESPONSE CONTRACT ERROR"));Assert.That((string)result["outcome"],Is.EqualTo("error"));Assert.That(executions+deployments,Is.Zero);
    }
    [Test] public void NormalizerOutageIsNotSentToTheModelForRepair() {
        normalizerUnavailable=true;outputs.Enqueue(Reply());var result=Run();Assert.That(generations,Is.EqualTo(1));Assert.That((string)result["error"],Does.Contain("normalizer unavailable"));
    }
    [Test] public void DeviceDispatchPreservesIdentityAndMetrics() {
        outputs.Enqueue(Device());var result=Run();Assert.That(executions,Is.EqualTo(1));Assert.That((string)result["metrics"]["actions"][0]["RequestId"],Is.EqualTo("run-id"));Assert.That((int)result["metrics"]["actionCount"],Is.EqualTo(1));Assert.That((string)result["reply"],Does.Contain("```api-call"));
    }
    [Test] public void DisabledCapabilityCannotReachDeviceDispatch() {
        var request=Request();((JObject)request["preparedContext"])["capabilities"]["deviceControl"]=false;outputs.Enqueue(Device());var result=Run(request);Assert.That(executions,Is.Zero);Assert.That((string)result["error"],Does.Contain("capability is disabled"));
    }
    [Test] public void ProgramCompilerRepairKeepsDeploymentIdentityAndAttempts() {
        compileFailures=1;outputs.Enqueue(Program());outputs.Enqueue(Program());var result=Run();Assert.That(deployments,Is.EqualTo(2));Assert.That(observedDeployments.All(d=>(string)d["requestId"]=="run-id"),Is.True);Assert.That(observedDeployments.Select(d=>(int)d["attempt"]),Is.EqualTo(new[]{1,2}));Assert.That((string)result["reply"],Does.Contain("automan_deploy"));Assert.That((int)result["metrics"]["failedActions"],Is.EqualTo(1));
    }
    [Test] public void CompilerRepairCannotSwitchAnExistingTarget() {
        compileFailures=1;var request=Request();((JObject)request["request"])["resources"]=JObject.Parse("{'programIds':['1001','1002']}");outputs.Enqueue(Program("update","1001"));outputs.Enqueue(Program("update","1002"));var result=Run(request);Assert.That(deployments,Is.EqualTo(1));Assert.That((string)result["error"],Does.Contain("cannot change"));
    }
    [Test] public void ProgramRepairLimitTriggersSameIdentityRecoveryOnce() {
        compileFailures=5;for(int i=0;i<5;i++)outputs.Enqueue(Program());var result=Run();Assert.That(deployments,Is.EqualTo(5));Assert.That(recoveries,Is.EqualTo(1));Assert.That((int)result["metrics"]["actionCount"],Is.EqualTo(6));Assert.That((string)result["outcome"],Is.EqualTo("error"));
    }
    [Test] public void CompositeRepairKeepsBindingAndWaitingWidget() {
        compileFailures=1;outputs.Enqueue(Composite());outputs.Enqueue(Composite());var result=Run();var solution=JObject.Parse((string)result["reply"])["ai_solution"];Assert.That((string)solution["widget"]["state"],Is.EqualTo("waiting"));Assert.That((string)solution["program"]["id"],Is.EqualTo("1001"));Assert.That(observedMessages[1].ToString(),Does.Contain("kind=composite"));
    }
    [Test] public void CompositeRepairCannotChangeBinding() {
        compileFailures=1;outputs.Enqueue(Composite());outputs.Enqueue(Composite("Sensor.Other"));var result=Run();Assert.That(deployments,Is.EqualTo(1));Assert.That((string)result["error"],Does.Contain("widget data contract"));
    }
    [Test] public void ScheduledWorkCannotBecomeAReplyOrWidget() {
        var request=Request();request["scheduled"]=true;outputs.Enqueue(Reply());var result=Run(request);Assert.That((string)result["error"],Does.Contain("scheduled task"));Assert.That(deployments+executions,Is.Zero);
    }
    [Test] public void StopBeforeGenerationDoesNotCallTheProvider() {
        running=false;var result=Run();Assert.That(generations,Is.Zero);Assert.That((string)result["error"],Does.Contain("stopping"));
    }
    [Test] public void StopAfterGenerationPreventsSideEffects() {
        stopAfterGeneration=true;outputs.Enqueue(Device());var result=Run();Assert.That(generations,Is.EqualTo(1));Assert.That(executions,Is.Zero);Assert.That((string)result["error"],Does.Contain("stopping"));
    }
    public class HistoryApi {
        public List<JObject> Writes=new();
        public Func<object,object> Factory;
        public object Call(string route,object data=null){if(route=="System.Utility/AiGenie/Turn.Create")return Factory(data);Assert.That(route,Is.EqualTo("System.Utility/ChatHistory/Add"));Writes.Add(JObject.FromObject(data));return null;}
    }
    [Test] public void WrapperImportsMetricsAndPersistsOnlyOneFinalReply() {
        outputs.Enqueue(Device());var result=Run();var api=new HistoryApi();api.Factory=payload=>compiled.GetType("AiGenieTurn").GetMethod("Create").Invoke(null,new object[]{payload,new Func<string,object,object>((route,data)=>api.Call(route,data))});var type=compiled.GetType("ChatRequestMetrics");var instance=Activator.CreateInstance(type,new object[]{api,"933",true});
        type.GetMethod("SaveUser").Invoke(instance,new object[]{JObject.Parse("{'role':'user','parts':[{'text':'test'}]}")});
        type.GetMethod("ImportRunMetrics").Invoke(instance,new object[]{result["metrics"]});var reply=JObject.Parse("{'role':'model','parts':[{'text':'done'}]}");
        type.GetMethod("SaveReply").Invoke(instance,new object[]{reply,"completed"});type.GetMethod("SaveReply").Invoke(instance,new object[]{reply,"completed"});
        Assert.That(api.Writes.Count,Is.EqualTo(2));Assert.That((int)api.Writes[1]["Metadata"]["actionCount"],Is.EqualTo(1));Assert.That(api.Writes[1]["Metadata"]["actions"].Count(),Is.EqualTo(1));
    }
}
