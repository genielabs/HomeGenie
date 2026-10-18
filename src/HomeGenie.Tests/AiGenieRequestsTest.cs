using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace HomeGenie.Tests;
[TestFixture]
public class AiGenieRequestsTest {
 Type type;
 [OneTimeSetUp] public void CompileShippedService() {
  var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
  while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
  var program=XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-base/programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")=="583");
  string source=CSharpSyntaxTree.ParseText("class Holder{"+(string)program.Element("ScriptContext")+"}").GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text=="AiGenieRequests").ToFullString();
  string code="using System;using System.Collections.Generic;using System.Linq;using System.Text;using System.Threading;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+source;
  var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location).Distinct();
  var compilation=CSharpCompilation.Create("BackgroundProbe"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
  using var stream=new MemoryStream();var result=compilation.Emit(stream);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));
  type=Assembly.Load(stream.ToArray()).GetType("AiGenieRequests");
 }
 JObject Submit(object service,object input,Func<string,object,object> call,string fallback="940")=>(JObject)type.GetMethod("Generate").Invoke(service,new object[]{input,fallback,call});
 JObject Status(object service,JObject accepted,bool cancel=false)=>(JObject)type.GetMethod("Status").Invoke(service,new object[]{new JObject{["version"]=1,["requestId"]=accepted["requestId"]},cancel});
 JObject Wait(object service,JObject accepted) {
  JObject result=null;
  Assert.That(SpinWait.SpinUntil(()=>{result=Status(service,accepted);return !new[]{"queued","running","cancelling"}.Contains((string)result["state"]);},TimeSpan.FromSeconds(10)),Is.True);
  return result;
 }
 JObject Request(string provider=null)=>provider==null?new(){["version"]=1,["prompt"]="Summarize this text."}:new(){["version"]=1,["prompt"]="Summarize this text.",["providerId"]=provider};
 JObject Descriptor(string id,string route,bool enabled=true,bool supports=true)=>new(){["Program"]=new JObject{["Address"]=id,["IsEnabled"]=enabled},["ImplementedInterface"]=new JObject{["ApiUrl"]=route,["Options"]=new JObject{["requests"]=new JObject{["generate"]=supports}}}};
 Func<string,object,object> Transport(Func<IDictionary<string,object>,object> generate=null,Action<JObject> prepared=null,JArray descriptors=null) => (route,payload)=> {
  if(route.EndsWith("Programs.Implements/^@AI:/regex"))return descriptors??new JArray(Descriptor("940","AI.Genies/Lailama"),Descriptor("933","AI.Genies/Gemini"));
  if(route.StartsWith("HomeAutomation.HomeGenie/Config/Modules.Get/"))return new JObject{["Properties"]=new JArray(new JObject{["Name"]="AI.Chat.Context",["Value"]="{\"devices\":false,\"widgets\":false}"})};
  if(route=="System.Utility/AiGenie/Context.Prepare") {var p=(JObject)payload;prepared?.Invoke(p);return new JObject{["systemPrompt"]="Fresh allowed context."};}
  if(route.EndsWith("/Provider.Text.Generate")) {
   var data=(IDictionary<string,object>)payload;
   return generate?.Invoke(data)??new Dictionary<string,object>{["version"]=1,["requestId"]=data["requestId"],["state"]="completed",["text"]="```api-call\nDo.Not.Execute\n```"};
  }
  throw new InvalidOperationException("Unexpected route; history and action APIs must never be called: "+route);
 };
 [Test] public void DefaultProviderUsesPersistedContextAndReturnsUninterpretedTextWithoutMutatingInput() {
  using var service=(IDisposable)Activator.CreateInstance(type);var input=Request();string before=input.ToString();
  var accepted=Submit(service,input,Transport(prepared:p=>{
   Assert.That((bool)p["request"]["context"]["devices"],Is.False);
   Assert.That(((JObject)p["request"]["capabilities"]).Properties().All(v=>!(bool)v.Value),Is.True);
  }));
  Assert.That((string)accepted["state"],Is.EqualTo("queued"));Assert.That((string)accepted["providerId"],Is.EqualTo("940"));
  var done=Wait(service,accepted);Assert.That((string)done["state"],Is.EqualTo("completed"));Assert.That((string)done["text"],Does.Contain("Do.Not.Execute"));Assert.That(input.ToString(),Is.EqualTo(before));
 }
 [Test] public void ExplicitProviderOverridesDefaultAndByteJsonIsAccepted() {
  using var service=(IDisposable)Activator.CreateInstance(type);
  var accepted=Submit(service,System.Text.Encoding.UTF8.GetBytes(Request("933").ToString()),Transport());
  Assert.That((string)accepted["providerId"],Is.EqualTo("933"));Assert.That((string)Wait(service,accepted)["state"],Is.EqualTo("completed"));
 }
 [Test] public void InvalidRequestsAndPermissionOverridesNeverDispatch() {
  using var service=(IDisposable)Activator.CreateInstance(type);
  var invalid=Request();invalid["context"]=new JObject{["devices"]=true};
  Assert.That((string)Submit(service,invalid,(r,p)=>throw new Exception())["state"],Is.EqualTo("failed"));
  Assert.That((string)Submit(service,Request("../Gemini"),(r,p)=>throw new Exception())["state"],Is.EqualTo("failed"));
 }
 [TestCase(false,true)] [TestCase(true,false)]
 public void UnavailableOrOldProviderNeverFallsBack(bool enabled,bool supports) {
  using var service=(IDisposable)Activator.CreateInstance(type);
  var accepted=Submit(service,Request(),Transport(descriptors:new JArray(Descriptor("940","AI.Genies/Lailama",enabled,supports),Descriptor("933","AI.Genies/Gemini"))));
  Assert.That((string)accepted["state"],Is.EqualTo("failed"));Assert.That(accepted["requestId"],Is.Null);
 }
 [Test] public void CancellationRemainsPendingUntilInferenceExitsAndSuppressesLateCompletion() {
  using var service=(IDisposable)Activator.CreateInstance(type);using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
  var call=Transport(data=>{entered.Set();release.Wait(TimeSpan.FromSeconds(10));return new Dictionary<string,object>{["version"]=1,["requestId"]=data["requestId"],["state"]="completed",["text"]="late"};});
  var accepted=Submit(service,Request(),call);
  try {
   Assert.That(entered.Wait(TimeSpan.FromSeconds(10)),Is.True);
   Assert.That((string)Status(service,accepted,true)["state"],Is.EqualTo("cancelling"));
   Assert.That((string)Submit(service,Request(),call)["state"],Is.EqualTo("busy"));
  } finally {release.Set();}
  var done=Wait(service,accepted);Assert.That((string)done["state"],Is.EqualTo("cancelled"));Assert.That(done["text"].Type,Is.EqualTo(JTokenType.Null));
 }
 [TestCase("output_limit")] [TestCase("context_limit")] [TestCase("failed")]
 public void PartialOutputIsNotPublishedAsCompletedText(string state) {
  using var service=(IDisposable)Activator.CreateInstance(type);
  var accepted=Submit(service,Request(),Transport(data=>new Dictionary<string,object>{["version"]=1,["requestId"]=data["requestId"],["state"]=state,["text"]="partial"}));
  var done=Wait(service,accepted);Assert.That((string)done["state"],Is.EqualTo(state));Assert.That(done["text"].Type,Is.EqualTo(JTokenType.Null));
 }
 [Test] public void ShutdownCancelsTokenAndRejectsNewRequests() {
  var service=(IDisposable)Activator.CreateInstance(type);using var entered=new ManualResetEventSlim();
  var accepted=Submit(service,Request(),Transport(data=>{var token=(CancellationToken)data["cancellationToken"];entered.Set();token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10));token.ThrowIfCancellationRequested();return null;}));
  Assert.That(entered.Wait(TimeSpan.FromSeconds(10)),Is.True);service.Dispose();
  Assert.That((string)Wait(service,accepted)["state"],Is.EqualTo("cancelled"));
  Assert.That((string)Submit(service,Request(),Transport())["state"],Is.EqualTo("failed"));
 }
 [Test] public void UnknownResultsAreExplicitAndProviderSettingsAreReadEachTime() {
  using var service=(IDisposable)Activator.CreateInstance(type);int reads=0;
  var call=Transport(prepared:p=>Interlocked.Increment(ref reads));
  Assert.That((string)Status(service,new JObject{["requestId"]="missing"})["state"],Is.EqualTo("not_found"));
  Wait(service,Submit(service,Request(),call));Wait(service,Submit(service,Request(),call));Assert.That(reads,Is.EqualTo(2));
 }

 [Test] public void ExecuteUsesTheSameAsyncRegistryAndRetainsActionResults() {
  using var service=(IDisposable)Activator.CreateInstance(type);
  var descriptors=new JArray(Descriptor("933","AI.Genies/Gemini"));descriptors[0]["ImplementedInterface"]["Options"]["requests"]["execute"]=true;
  Func<string,object,object> call=(route,payload)=> {
   if(route.EndsWith("Programs.Implements/^@AI:/regex"))return descriptors;
   Assert.That(route,Is.EqualTo("AI.Genies/Gemini/Provider.Request.Execute"));
   var data=(IDictionary<string,object>)payload;
   return new JObject{["version"]=1,["requestId"]=(string)data["requestId"],["state"]="completed",["text"]="Done",["effectsMayHaveOccurred"]=true,
    ["result"]=new JObject{["actions"]=new JArray("receipt")}};
  };
  var accepted=(JObject)type.GetMethod("Execute").Invoke(service,new object[]{Request("933"),"940",call});
  var done=Wait(service,accepted);Assert.That((string)done["state"],Is.EqualTo("completed"));Assert.That((bool)done["effectsMayHaveOccurred"],Is.True);
  Assert.That((string)done["result"]["actions"][0],Is.EqualTo("receipt"));
 }
 [Test] public void ExecuteTransportFailureRetainsAnExplicitUncertainEffectsFlag() {
  using var service=(IDisposable)Activator.CreateInstance(type);
  var descriptors=new JArray(Descriptor("933","AI.Genies/Gemini"));descriptors[0]["ImplementedInterface"]["Options"]["requests"]["execute"]=true;
  Func<string,object,object> call=(route,payload)=>route.EndsWith("Programs.Implements/^@AI:/regex")?descriptors:throw new Exception("Provider stopped after dispatch");
  var accepted=(JObject)type.GetMethod("Execute").Invoke(service,new object[]{Request("933"),"940",call});
  var done=Wait(service,accepted);Assert.That((string)done["state"],Is.EqualTo("failed"));Assert.That((bool)done["effectsMayHaveOccurred"],Is.True);
 }

 JObject Execute(object service,JObject input,Func<string,object,object> call)=>(JObject)type.GetMethod("Execute").Invoke(service,new object[]{input,"933",call});
 Func<string,object,object> WaitingTransport(Action<IDictionary<string,object>> started=null) => (route,payload)=> {
  if(route.EndsWith("Programs.Implements/^@AI:/regex")) {
   var descriptors=new JArray(Descriptor("933","AI.Genies/Gemini"));descriptors[0]["ImplementedInterface"]["Options"]["requests"]["execute"]=true;return descriptors;
  }
  var data=(IDictionary<string,object>)payload;started?.Invoke(data);
  var token=(CancellationToken)data["cancellationToken"];
  Assert.That(token.WaitHandle.WaitOne(TimeSpan.FromSeconds(8)),Is.True,"Native work was not cancelled");
  return new JObject{["version"]=1,["requestId"]=(string)data["requestId"],["state"]="cancelled",["effectsMayHaveOccurred"]=false,["result"]=new JObject{["actions"]=new JArray()}};
 };
 [Test] public void NativeWaitTimeoutRequestsCancellationWithoutResubmission() {
  using var service=(IDisposable)Activator.CreateInstance(type);
  var input=Request("933");input["wait"]=true;input["timeoutSeconds"]=1;
  var result=Execute(service,input,WaitingTransport());
  Assert.That((string)result["state"],Is.AnyOf("cancelling","cancelled"));
  Assert.That((string)Wait(service,result)["state"],Is.EqualTo("cancelled"));
 }
 [TestCase(false)] [TestCase(true)] public void JintThreadInterruptionRunsNativeWaitCleanupAndCancelsInference(bool disposeEngineFirst) {
  using var service=(IDisposable)Activator.CreateInstance(type);using var entered=new ManualResetEventSlim();using var cancelled=new ManualResetEventSlim();
  string id=null;
  var transport=WaitingTransport(data=>{id=(string)data["requestId"];entered.Set();((CancellationToken)data["cancellationToken"]).Register(()=>cancelled.Set());});
  Exception escaped=null;Jint.Engine exposedEngine=null;
  var caller=new Thread(()=>{
   try {
    using var engine=new Jint.Engine();exposedEngine=engine;
    engine.SetValue("run",new Func<object>(()=> {var input=Request("933");input["wait"]=true;return Execute(service,input,transport);}));
    engine.Execute("run();");
   } catch(Exception ex){escaped=ex;}
  });
  caller.Start();
  try {
   Assert.That(entered.Wait(TimeSpan.FromSeconds(5)),Is.True);if(disposeEngineFirst)exposedEngine.Dispose();caller.Interrupt();
   Assert.That(cancelled.Wait(TimeSpan.FromSeconds(5)),Is.True,"Jint interruption must not orphan inference");
   Assert.That(caller.Join(5000),Is.True);Assert.That(escaped,Is.Not.Null);
   Assert.That((string)Wait(service,new JObject{["requestId"]=id})["state"],Is.EqualTo("cancelled"));
  } finally {if(caller.IsAlive){caller.Interrupt();caller.Join(5000);}}
 }
 [Test] public void InvalidWaitSettingsNeverStartWork() {
  using var service=(IDisposable)Activator.CreateInstance(type);var input=Request();input["wait"]="yes";
  Assert.That((string)Execute(service,input,(r,p)=>throw new AssertionException("Must not dispatch"))["state"],Is.EqualTo("failed"));
  input["wait"]=true;input["timeoutSeconds"]=0;
  Assert.That((string)Execute(service,input,(r,p)=>throw new AssertionException("Must not dispatch"))["state"],Is.EqualTo("failed"));
 }
}
