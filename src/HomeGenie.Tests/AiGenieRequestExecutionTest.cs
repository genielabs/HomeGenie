using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
public class AiGenieRequestExecutionTest {
 Assembly assembly;
 CancellationTokenSource cancellation;
 Queue<string> outputs;
 List<JArray> messages;
 JObject context,caps;
 int controls,deploys,reads,compileFailures;
 bool revoke,stopBeforeAction,stopAfterAction,unknownAction,masterOff;
 string folder;
 [OneTimeSetUp] public void CompileShippedExecution() {
  var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
  XElement Program(string pkg,string id)=>XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie",pkg,"programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")==id);
  string Class(XElement p,string name)=>CSharpSyntaxTree.ParseText("class Holder{"+(string)p.Element("ScriptContext")+"}").GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text==name).ToFullString();
  string source="using System;using System.IO;using System.Text;using System.Linq;using System.Threading;using System.Security.Cryptography;using System.Collections.Generic;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+
    (string)Program("homegenie-base","583").Element("ScriptContext")+Class(Program("homegenie-base","581"),"GenieResponseContract");
  var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location);
  var compilation=CSharpCompilation.Create("BackgroundExecute"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(source)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
  using var stream=new MemoryStream();var result=compilation.Emit(stream);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));assembly=Assembly.Load(stream.ToArray());
 }
 [SetUp] public void Reset() {
  cancellation=new();outputs=new();messages=new();controls=deploys=reads=compileFailures=0;
  revoke=stopBeforeAction=stopAfterAction=unknownAction=masterOff=false;
  context=JObject.Parse("{'devices':true,'widgets':false,'automationPrograms':false,'dashboards':false,'deepKnowledge':false}");
  caps=JObject.Parse("{'deviceControl':true,'automation':true,'widgets':true}");folder=Path.Combine(Path.GetTempPath(),"hg-execute-"+Guid.NewGuid());Directory.CreateDirectory(folder);
 }
 [TearDown] public void Cleanup(){cancellation.Dispose();Directory.Delete(folder,true);}
 object Invoke(string cls,string method,params object[] arguments) {
  try{return assembly.GetType(cls).GetMethod(method).Invoke(null,arguments);}
  catch(TargetInvocationException ex){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();throw;}
 }
 object Call(string route,object payload) {
  if(route=="System.Utility/ActionExecutor/Response.Schema")return Invoke("GenieResponseContract","Schema");
  if(route=="System.Utility/ActionExecutor/Response.Normalize") {
   try{return Invoke("GenieResponseContract","Normalize",payload);}
   catch(Exception ex){return new JObject{["Status"]="Error",["Message"]=ex.Message};}
  }
  if(route=="System.Utility/ContextEngine/Context.Policy")return new JObject{["context"]=((JObject)payload)["context"].DeepClone(),["profileKey"]="test",["prompt"]="policy"};
  if(route=="System.Utility/ContextEngine/Prompt.Compose")return new JObject{["prompt"]=JObject.FromObject(payload)["template"]?.ToString()??"instructions"};
  if(route=="System.Utility/ContextEngine/Render")return "Test.Lights/1 Porch Light";
  if(route.EndsWith("/Test.Lights/1/Schema")){reads++;return JObject.Parse("{'version':1,'module':{'domain':'Test.Lights','address':'1'},'properties':[],'features':[]}");}
  if(route.EndsWith("Modules.ParameterGet/Test.Lights/1/Sensor.Counter/Context")){reads++;return JObject.Parse("{'version':1,'domain':'Test.Lights','address':'1','key':'Sensor.Counter','present':true,'value':'90'}");}
  if(route.StartsWith("HomeAutomation.HomeGenie/Config/Modules.Get/")) {
   var properties=new JArray(new JObject{["Name"]="AI.Chat.Context",["Value"]=context.ToString(Formatting.None)},new JObject{["Name"]="AI.Chat.Capabilities",["Value"]=caps.ToString(Formatting.None)});
   if(masterOff)properties.Add(new JObject{["Name"]="ConfigureOptions.SendContextData",["Value"]="Off"});
   return new JObject{["Properties"]=properties};
  }
  if(route=="System.Utility/AiGenie/Context.Prepare") {
   var p=(JObject)payload;return Invoke("AiGenieContext","Prepare",p["request"],(string)p["template"],folder,(Func<string,object,object>)Call);
  }
  if(route=="HomeAutomation.HomeGenie/Config/Widgets.List")return new JArray();
  if(route=="HomeAutomation.HomeGenie/Automation/Programs.List")return new JArray();
  if(route=="System.Utility/ActionExecutor/Execute") {
   controls++;if(unknownAction)throw new Exception("Transport interrupted");
   var p=JObject.Parse((string)payload);var commands=((string)p["response"]).Split('\n').Skip(1).Where(s=>s!="```");
   var result=new JArray(commands.Select((command,index)=>new JObject{["Index"]=index,["RequestId"]=(string)p["requestId"],["Command"]=command,["Success"]=true,["State"]="completed"}));
   if(stopAfterAction)cancellation.Cancel();return result;
  }
  if(route=="System.Utility/ActionExecutor/Program.Deploy") {
   deploys++;var p=JObject.Parse((string)payload);bool failed=deploys<=compileFailures;
   return new JObject{["RequestId"]=(string)p["requestId"],["ActionId"]="execute-test:deploy-"+deploys,["Index"]=deploys,["State"]=unknownAction?"unknown":failed?"failed":"completed",["Success"]=!failed&&!unknownAction,
    ["Response"]=new JObject{["status"]=failed?"compile_failed":"success",["program_id"]="1001",["compiler_errors"]=new JArray("compile error")}};
  }
  if(route=="System.Utility/ActionExecutor/Results")return new JArray();
  throw new Exception("Unexpected API: "+route+" (no chat history or UI event calls permitted)");
 }
 IDictionary<string,object> Generate(JArray supplied,string instructions,object output) {
  messages.Add((JArray)supplied.DeepClone());string response=outputs.Dequeue();
  if(revoke)caps["deviceControl"]=false;
  if(stopBeforeAction)cancellation.Cancel();
  return new Dictionary<string,object>{["version"]=1,["requestId"]="execute-test",["state"]="completed",["text"]=response};
 }
 JObject Run(string protocol)=> (JObject)Invoke("AiGenieRequestExecution","Run",new Dictionary<string,object>{["version"]=1,["requestId"]="execute-test",["providerId"]="933",["prompt"]="Perform the requested task",
  ["template"]="custom instructions",["protocol"]=protocol,["cancellationToken"]=cancellation.Token,["generate"]=new Func<JArray,string,object,IDictionary<string,object>>(Generate)},(Func<string,object,object>)Call,folder);
 string Device(string protocol)=>protocol=="text"?"Switching on.\n```api-call\nTest.Lights/1/Control.On\n```":JObject.FromObject(new{version=1,kind="device",explanation="Switching on.",commands=new[]{"Test.Lights/1/Control.On"}}).ToString();
 string Program(string protocol) {
  string source=JObject.FromObject(new{version=1,kind="program",explanation="Create a helper",language="csharp",automation=new{action="create",program_id="",name="Test helper",setup="Program.Run();",main="Program.GoBackground();",context=""}}).ToString();
  return protocol=="text"?"```genie-action\n"+source+"\n```":source;
 }
 [TestCase("structured")] [TestCase("text")]
 public void DeviceCommandsUseTheExistingExecutorAndReturnCorrelatedReceipts(string protocol) {
  outputs.Enqueue(Device(protocol));var result=Run(protocol);
  Assert.That((string)result["state"],Is.EqualTo("completed"),result.ToString());Assert.That(controls,Is.EqualTo(1));
  Assert.That((bool)result["effectsMayHaveOccurred"],Is.True);Assert.That(((JArray)result["result"]["actions"]).Count,Is.EqualTo(1));
  Assert.That(messages[0].Count,Is.EqualTo(1));Assert.That((string)result["result"]["actions"][0]["response"][0]["RequestId"],Is.EqualTo("execute-test"));
 }
 [TestCase("structured")] [TestCase("text")]
 public void DisabledDeviceCapabilityAndMidGenerationRevocationBlockTheAction(string protocol) {
  revoke=true;outputs.Enqueue(Device(protocol));var result=Run(protocol);
  Assert.That((string)result["state"],Is.EqualTo("failed"));Assert.That(controls,Is.Zero);Assert.That((bool)result["effectsMayHaveOccurred"],Is.False);
 }
 [TestCase("structured")] [TestCase("text")]
 public void MasterSharingOffBlocksDeviceControlsEvenWhenTheCapabilityIsEnabled(string protocol) {
  masterOff=true;outputs.Enqueue(Device(protocol));var result=Run(protocol);
  Assert.That((string)result["state"],Is.EqualTo("failed"));Assert.That(controls,Is.Zero);
 }
 [TestCase("structured")] [TestCase("text")]
 public void CancellationBeforeActionNeverDispatches(string protocol) {
  stopBeforeAction=true;outputs.Enqueue(Device(protocol));var result=Run(protocol);
  Assert.That((string)result["state"],Is.EqualTo("cancelled"));Assert.That(controls,Is.Zero);Assert.That((bool)result["effectsMayHaveOccurred"],Is.False);
 }
 [TestCase("structured")] [TestCase("text")]
 public void CancellationAfterActionPreservesItsReceiptWithoutClaimingRollback(string protocol) {
  stopAfterAction=true;outputs.Enqueue(Device(protocol));var result=Run(protocol);
  Assert.That((string)result["state"],Is.EqualTo("cancelled"));Assert.That(controls,Is.EqualTo(1));Assert.That((bool)result["effectsMayHaveOccurred"],Is.True);
  Assert.That(((JArray)result["result"]["actions"]).Count,Is.EqualTo(1));Assert.That(result["text"].Type,Is.EqualTo(JTokenType.Null));
 }
 [TestCase("structured")] [TestCase("text")]
 public void UnknownActionOutcomeIsNeverAutomaticallyRetried(string protocol) {
  unknownAction=true;outputs.Enqueue(Device(protocol));var result=Run(protocol);
  Assert.That((string)result["state"],Is.EqualTo("failed"));Assert.That(controls,Is.EqualTo(1));Assert.That((bool)result["effectsMayHaveOccurred"],Is.True);
  Assert.That((string)result["result"]["actions"][0]["state"],Is.EqualTo("unknown"));
 }
 [TestCase("structured")] [TestCase("text")]
 public void ProgramCompilationRepairUsesTheExistingBoundedCoordinator(string protocol) {
  compileFailures=1;outputs.Enqueue(Program(protocol));outputs.Enqueue(Program(protocol));var result=Run(protocol);
  Assert.That((string)result["state"],Is.EqualTo("completed"),result.ToString());Assert.That(deploys,Is.EqualTo(2));Assert.That(controls,Is.Zero);
  Assert.That(((JArray)result["result"]["actions"]).Count,Is.EqualTo(2));
 }
 [TestCase("structured")] [TestCase("text")]
 public void FreshParameterReadCompletesWithoutActionsOrChatHistory(string protocol) {
  outputs.Enqueue(protocol=="text"?"```context-request\n{\"parameterIds\":[\"Test.Lights/1/Sensor.Counter\"]}\n```":JObject.FromObject(new{version=1,kind="context",explanation="Read the value",resources=new{parameterIds=new[]{"Test.Lights/1/Sensor.Counter"}}}).ToString());
  outputs.Enqueue(protocol=="text"?"The counter is 90.":"{\"version\":1,\"kind\":\"reply\",\"explanation\":\"The counter is 90.\"}");
  var result=Run(protocol);Assert.That((string)result["state"],Is.EqualTo("completed"),result.ToString());Assert.That(reads,Is.EqualTo(1));
  Assert.That(controls+deploys,Is.Zero);Assert.That((bool)result["effectsMayHaveOccurred"],Is.False);Assert.That(messages[1].ToString(),Does.Contain("90"));
 }

 [TestCase("```api-call\nTest.Lights/1/Control.On\n```")]
 [TestCase("<think>Use the requested command.</think>\n```api-call\nTest.Lights/1/Control.On\n```")]
 public void LocalCommandWithoutProseHasAValidCanonicalEnvelope(string reply) {
  outputs.Enqueue(reply);var result=Run("text");
  Assert.That((string)result["state"],Is.EqualTo("completed"),result.ToString());Assert.That(controls,Is.EqualTo(1));
 }
}
