using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace HomeGenie.Tests;
[TestFixture]
public class AiGenieProviderRecoveryTest {
 Type lifetime,gemini,modelSettings,failedInit;string geminiSource,lailamaSource,folder;
 [OneTimeSetUp] public void CompileShippedPaths() {
  var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
  while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
  string Context(string package,string pid,string field)=>XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie",package,"programs.xml")).Root.Elements().Single(p=>(string)p.Element("Address")==pid).Element(field).Value;
  var ctx=Context("homegenie-ml-ai","940","ScriptContext");lailamaSource=Context("homegenie-ml-ai","940","ScriptSource");geminiSource=Context("homegenie-ml-ai","933","ScriptSource");
  var ctxTree=CSharpSyntaxTree.ParseText("class Holder{"+ctx+"}").GetRoot();
  var lifetimeClass=ctxTree.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text=="LailamaNativeLifetime").ToFullString();
  var paramsMethod=ctxTree.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(n=>n.Identifier.Text=="GetOptimizedParams").ToFullString();
  var gemTree=CSharpSyntaxTree.ParseText("class Holder{void Main(){"+geminiSource+"}}").GetRoot();
  var prepare=gemTree.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single(n=>n.Identifier.Text=="prepareScopedContext").Parent.Parent.ToFullString();
  var process=gemTree.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single(n=>n.Identifier.Text=="processGeniePrompt");
  var body=(BlockSyntax)process.DescendantNodes().OfType<LockStatementSyntax>().First().Statement;
  var prelude=string.Concat(body.Statements.TakeWhile(n=>!n.DescendantNodesAndSelf().OfType<VariableDeclaratorSyntax>().Any(v=>v.Identifier.Text=="telemetry")).Select(n=>n.ToFullString()));
  var policyTree=CSharpSyntaxTree.ParseText("class Holder{"+Context("homegenie-ml-ai","578","ScriptContext")+"}").GetRoot();
  var policy=policyTree.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text=="ChatContextPolicy").ToFullString();
  var initTree=CSharpSyntaxTree.ParseText("class Holder{void Main(){"+lailamaSource+"}}").GetRoot();
  var init=initTree.DescendantNodes().OfType<TryStatementSyntax>().Single(n=>n.Finally?.ToString().Contains("if (initializationFailed) dispose();")==true);
  var catches=string.Concat(init.Catches.Select(n=>n.ToFullString()));
  string helpers="""
public class Parsed { public object Data {get;set;} }
public class ResponseText { public ResponseText(string value){} }
public class DataFolder {public string Folder;public string GetFolder()=>Folder;}
public class ProfileApi {
 public Parsed Parse(object value)=>new Parsed{Data=value};
 public object Call(string route,object payload) {
  var r=(JObject)payload;
  if(route.EndsWith("Context.Policy"))return JObject.FromObject(ChatContextPolicy.Build(r));
  if(route.EndsWith("Context.Prepare")) {
   var input=(JObject)r["request"];var result=JObject.FromObject(ChatContextPolicy.Build(new JObject{["version"]=1,["context"]=input["context"]??new JObject()}));
   var caps=input["capabilities"] as JObject??new JObject{["deviceControl"]=true,["automation"]=true,["widgets"]=true};
   result["profileKey"]=(string)result["profileKey"]+"|cap-v1:"+string.Concat(new[]{"deviceControl","automation","widgets"}.Select(k=>caps[k]?.Value<bool>()!=false?"1":"0"));
   result["capabilities"]=caps.DeepClone();result["systemPrompt"]="test";return result;
  }
  throw new InvalidOperationException("Unexpected route "+route);
 }
}
public class ProfileHarness {
 public JObject StoredContext,StoredCapabilities;public DataFolder Data=new DataFolder();public ProfileApi Api=new ProfileApi();
 string currentCacheName;DateTime cacheExpiration;
 public object Process(object userCommand,JObject feedback) {
  Func<object,JToken> contextToken=value=>value is string text?JToken.Parse(text):JToken.FromObject(value);
  Func<string,JToken> storedChatSetting=name=>name=="AI.Chat.Context"?StoredContext:StoredCapabilities;
PREPARE
PRELUDE
  return preparedContext;
 }
}
public enum GGMLType { GGML_TYPE_Q4_0,GGML_TYPE_Q8_0 }
public class ModelParams {
 public ModelParams(string path){} public uint ContextSize,BatchSize,UBatchSize;public int GpuLayerCount;
 public bool FlashAttention,UseMemorymap;public GGMLType TypeK,TypeV;
}
public class SettingsHarness {public long Ram;public long GetTotalMemoryGB()=>Ram;
PARAMETERS
}
public class FakeProgram {public FakeProgram Notify(string s)=>this;public FakeProgram Emit(string n,object v)=>this;}
public class InitHarness {
 public LailamaNativeLifetime nativeLifetime=new LailamaNativeLifetime();public int Released;
 public List<string> Logs=new List<string>();FakeProgram Program=new FakeProgram();
 const string FIELD_STATUS_ERROR="error",FIELD_STATUS_INIT="init";
 public void Simulate(Action operation) {
  bool initializationFailed=false;CancellationTokenSource initializationCts;
  if(!nativeLifetime.TryBegin(out initializationCts))throw new Exception("busy");
  Action<string> lifecycleLog=Logs.Add;Action dispose=()=>nativeLifetime.Stop(()=>Released++,lifecycleLog);
  try {operation();}
CATCHES
FINALLY
 }
}
""";
  helpers=helpers.Replace("PREPARE",prepare).Replace("PRELUDE",prelude).Replace("PARAMETERS",paramsMethod).Replace("CATCHES",catches).Replace("FINALLY",init.Finally.ToFullString());
  string code="using System;using System.IO;using System.Net;using System.Text;using System.Linq;using System.Collections.Generic;using System.Threading;using System.Threading.Tasks;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+lifetimeClass+policy+helpers;
  var refs=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location).Select(p=>MetadataReference.CreateFromFile(p));
  using var stream=new MemoryStream();var result=CSharpCompilation.Create("Recovery"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},refs,new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)).Emit(stream);
  Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));var assembly=Assembly.Load(stream.ToArray());
  lifetime=assembly.GetType("LailamaNativeLifetime");gemini=assembly.GetType("ProfileHarness");modelSettings=assembly.GetType("SettingsHarness");failedInit=assembly.GetType("InitHarness");
  folder=Path.Combine(Path.GetTempPath(),"hg-profile-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"gemini_system_prompt.txt"),"test");
 }
 [OneTimeTearDown] public void Cleanup(){if(folder!=null&&Directory.Exists(folder)){File.Delete(Path.Combine(folder,"gemini_system_prompt.txt"));Directory.Delete(folder);}}
 JObject Context()=>JObject.Parse("{'devices':true,'widgets':true,'automationPrograms':true,'dashboards':false,'deepKnowledge':false}");
 JObject Caps()=>JObject.Parse("{'deviceControl':true,'automation':true,'widgets':true}");
 dynamic Profile(){dynamic h=Activator.CreateInstance(gemini);h.StoredContext=Context();h.StoredCapabilities=Caps();h.Data.Folder=folder;return h;}
 JObject Feedback()=>new JObject{["contextProfileKey"]="context-v1:11100|cap-v1:111"};
 [Test] public void FeedbackUsesStoredNonDefaultContext(){dynamic h=Profile();var result=(JObject)h.Process(new JObject{["version"]=1,["message"]="selected roots"},Feedback());Assert.That((string)result["profileKey"],Is.EqualTo("context-v1:11100|cap-v1:111"));}
 [Test] public void OrdinaryScopedPromptUsesTheSameDefaults(){dynamic h=Profile();var result=(JObject)h.Process(new JObject{["version"]=1,["message"]="hello"},null);Assert.That((string)result["profileKey"],Is.EqualTo("context-v1:11100|cap-v1:111"));}
 [Test] public void ExplicitContextStillOverridesStoredProfile(){dynamic h=Profile();var result=(JObject)h.Process(new JObject{["version"]=1,["message"]="hello",["context"]=new JObject{["devices"]=false}},null);Assert.That((string)result["profileKey"],Is.EqualTo("context-v1:00000|cap-v1:111"));}
 [TestCase("context")][TestCase("capabilities")] public void ActualSettingChangesStillRejectOldPresentation(string setting){dynamic h=Profile();if(setting=="context")h.StoredContext=new JObject{["devices"]=false};else h.StoredCapabilities=new JObject{["deviceControl"]=false,["automation"]=false,["widgets"]=true};var result=(JObject)h.Process(new JObject{["version"]=1,["message"]="choice"},Feedback());Assert.That((string)result["state"],Is.EqualTo("rejected"));}
 [Test] public void MissingSavedSettingsKeepCanonicalDefaults(){dynamic h=Profile();h.StoredContext=null;h.StoredCapabilities=null;var result=(JObject)h.Process(new JObject{["version"]=1,["message"]="hello"},null);Assert.That((string)result["profileKey"],Is.EqualTo("context-v1:10000|cap-v1:111"));}
 [TestCase(8L,2048U)][TestCase(16L,8192U)][TestCase(31L,4096U)][TestCase(31L,32768U)] public void ConfiguredCapacityWinsOverRamProfile(long ram,uint capacity){dynamic h=Activator.CreateInstance(modelSettings);h.Ram=ram;dynamic p=h.GetOptimizedParams("model.gguf",capacity);Assert.That((uint)p.ContextSize,Is.EqualTo(capacity));Assert.That((uint)p.BatchSize,Is.EqualTo(ram<=8?128U:512U));}
 [TestCase(0U)][TestCase(512U)][TestCase(65536U)] public void InvalidContextCapacityFailsBeforeNativeAllocation(uint capacity){dynamic h=Activator.CreateInstance(modelSettings);Assert.Throws<ArgumentOutOfRangeException>(()=>h.GetOptimizedParams("model.gguf",capacity));}
 [Test] public void InterruptedCallerCannotReleaseWorkerLease(){dynamic life=Activator.CreateInstance(lifetime);CancellationTokenSource lease;life.TryBegin(out lease);using var entered=new ManualResetEventSlim();using var finish=new ManualResetEventSlim();int released=0;bool interrupted=false;Task<int> work=life.RunOwned<int>(lease,(Func<int>)(()=>{entered.Set();finish.Wait();return 42;}));var caller=new Thread(()=>{try{work.GetAwaiter().GetResult();}catch(ThreadInterruptedException){interrupted=true;}});caller.IsBackground=true;
  try {caller.Start();Assert.That(entered.Wait(2000),Is.True);caller.Interrupt();Assert.That(caller.Join(2000),Is.True);Assert.That(interrupted,Is.True);Assert.That((bool)life.Stop((Action)(()=>Interlocked.Increment(ref released)),(Action<string>)null,20),Is.False);Assert.That(released,Is.Zero);}
  finally{finish.Set();work.GetAwaiter().GetResult();life.Stop((Action)(()=>Interlocked.Increment(ref released)),(Action<string>)null,2000);}Assert.That(released,Is.EqualTo(1));
 }
 [Test] public void WorkerFailureStillReleasesLease(){dynamic life=Activator.CreateInstance(lifetime);CancellationTokenSource lease;life.TryBegin(out lease);Task<int> work=life.RunOwned<int>(lease,(Func<int>)(()=>throw new InvalidOperationException("test")));Assert.Throws<InvalidOperationException>(()=>work.GetAwaiter().GetResult());CancellationTokenSource next;Assert.That((bool)life.TryBegin(out next),Is.True);life.End(next);}
 [Test] public void FailedInitializationLogsStackAndCleansAfterEndingLease(){dynamic h=Activator.CreateInstance(failedInit);var clock=System.Diagnostics.Stopwatch.StartNew();h.Simulate((Action)(()=>throw new InvalidOperationException("init test")));Assert.That((int)h.Released,Is.EqualTo(1));Assert.That(clock.ElapsedMilliseconds,Is.LessThan(2000));Assert.That(((IEnumerable<string>)h.Logs).Any(s=>s.Contains("InvalidOperationException")&&s.Contains("init test")&&s.Contains(" at ")),Is.True);}
 [Test] public void SuccessfulInitializationKeepsResourcesUntilStop(){dynamic h=Activator.CreateInstance(failedInit);h.Simulate((Action)(()=>{}));Assert.That((int)h.Released,Is.Zero);Assert.That((bool)h.nativeLifetime.IsStopping,Is.False);h.nativeLifetime.Stop((Action)(()=>{}),(Action<string>)null,2000);}
 [Test] public void BothNativeBackgroundApisUseWorkerOwnership(){var tree=CSharpSyntaxTree.ParseText("class Holder{void Main(){"+lailamaSource+"}}").GetRoot();foreach(string route in new[]{"/Provider.Text.Generate","/Provider.Request.Execute"}){var handler=tree.DescendantNodes().OfType<InvocationExpressionSyntax>().Single(n=>n.Expression.ToString()=="Api.Handle"&&n.ArgumentList.Arguments[0].ToString().Contains('"'+route+'"'));Assert.That(handler.ToString(),Does.Contain("nativeLifetime.RunOwned<object>"));Assert.That(handler.ToString(),Does.Not.Contain("nativeLifetime.End(lease)"));}}
}
