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
public class AiGenieChatFeedbackTest {
 Type bridge,history,message;JArray rows;string sourceId,store,eventId;int generated;
 [OneTimeSetUp] public void CompileShippedBridgeAndHistory() {
  var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
  while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
  var xml=XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-base/programs.xml"));
  string Class(string id,string name)=>CSharpSyntaxTree.ParseText("class Holder{"+(string)xml.Root.Elements().Single(p=>(string)p.Element("Address")==id).Element("ScriptContext")+"}")
   .GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text==name).ToFullString();
  var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
   .Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location);
  string code="using System;using System.IO;using System.Text;using System.Linq;using System.Collections.Generic;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+
   Class("583","AiGenieChatFeedback")+Class("577","HistoryStore")+Class("577","ChatMessage")+Class("577","MessagePart");
  var compilation=CSharpCompilation.Create("Feedback"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
  using var stream=new MemoryStream();var result=compilation.Emit(stream);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));
  var assembly=Assembly.Load(stream.ToArray());bridge=assembly.GetType("AiGenieChatFeedback");history=assembly.GetType("HistoryStore");message=assembly.GetType("ChatMessage");
 }
 [SetUp] public void Reset(){sourceId=Guid.NewGuid().ToString("N");store="933";eventId=Guid.NewGuid().ToString("N");generated=0;rows=new JArray(new JObject{
  ["role"]="model",["parts"]=new JArray(new JObject{["text"]="{\"ai_widget\":{\"version\":1,\"mode\":\"presentation\",\"html\":\"<div>Choices</div>\"}}"}),
  ["metadata"]=new JObject{["requestId"]=sourceId,["outcome"]="completed",["contextProfileKey"]="profile-a"}});}
 JObject Request()=>new JObject{["version"]=1,["providerId"]=store,["sourceRequestId"]=sourceId,["eventId"]=eventId,["event"]="choice",["data"]=new JObject{["choice"]="roots"}};
 object Call(string name,params object[] args)=>bridge.GetMethod(name).Invoke(null,args);
 object Api(string route,object data) {Assert.That(route,Is.EqualTo("System.Utility/ChatHistory/Get"));return rows.DeepClone();}
 JObject Save(JObject request,JObject feedback,string outcome="completed") {
  generated++;var row=new JObject{["role"]="model",["parts"]=new JArray(new JObject{["text"]="Follow the roots."}),
   ["metadata"]=new JObject{["requestId"]=Guid.NewGuid().ToString("N"),["contextProfileKey"]="profile-a",["outcome"]=outcome,["feedback"]=feedback.DeepClone()}};rows.Add(row);return row;
 }
 JObject Run(JObject request=null,Func<JObject,JObject,object> process=null)=> (JObject)Call("Run",new Dictionary<string,object>{["storeId"]=store,["providerId"]=store,["request"]=request??Request(),["process"]=process??((r,f)=>Save(r,f))},new Func<string,object,object>(Api));
 [Test] public void ContinuesWithOnlyAssistantRowAndOriginalProfile(){var result=Run();Assert.That((string)result["state"],Is.EqualTo("completed"));Assert.That(rows.Count,Is.EqualTo(2));Assert.That(rows.All(r=>(string)r["role"]=="model"),Is.True);Assert.That((string)rows[1]["metadata"]["feedback"]["contextProfileKey"],Is.EqualTo("profile-a"));Assert.That(rows[1]["metadata"]["feedback"]["providerId"],Is.Null);}
 [Test] public void SavedDuplicateReplaysWithoutGeneration(){Run();var result=Run();Assert.That((bool)result["replayed"],Is.True);Assert.That(generated,Is.EqualTo(1));}
 [Test] public void DuplicateWithDifferentDataIsRejected(){Run();var request=Request();request["data"]["choice"]="stairs";Assert.That((string)Run(request)["state"],Is.EqualTo("rejected"));Assert.That(generated,Is.EqualTo(1));}
 [Test] public void UnknownOutcomeIsNotAutomaticallyRepeated(){int invoked=0;Func<JObject,JObject,object> process=(r,f)=>{invoked++;return null;};Assert.That((string)Run(process:process)["state"],Is.EqualTo("unknown"));Run(process:process);Assert.That(invoked,Is.EqualTo(1));}
 [TestCase("busy")][TestCase("rejected")] public void PreGenerationRefusalCanBeRetried(string state){Run(process:(r,f)=>Call("Error",state,"Try later"));Assert.That((string)Run()["state"],Is.EqualTo("completed"));}
 [TestCase("cancelled")][TestCase("error")] public void SavedFailureIsAnAuthoritativeReplay(string state){Run(process:(r,f)=>Save(r,f,state));Assert.That((string)Run()["state"],Is.EqualTo(state));Assert.That(generated,Is.EqualTo(1));}
 [Test] public void ExceptionAfterPersistenceRecoversSavedReply(){var result=Run(process:(r,f)=>{Save(r,f);throw new Exception("transport lost");});Assert.That((string)result["Status"],Is.EqualTo("Ok"));}
 [Test] public void RemovedPresentationIsRejected(){rows.Clear();Assert.That((string)Run()["state"],Is.EqualTo("rejected"));Assert.That(generated,Is.Zero);}
 [Test] public void OrdinaryOrUnfinishedWidgetCannotSendFeedback(){rows[0]["parts"][0]["text"]="ordinary response";Assert.That((string)Run()["state"],Is.EqualTo("rejected"));}
 [Test] public void WrongProviderIsRejected(){var request=Request();request["providerId"]="940";Assert.That((string)Run(request)["state"],Is.EqualTo("rejected"));}
 [Test] public void ConcurrentFeedbackIsBusy(){using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();var task=Task.Run(()=>Run(process:(r,f)=>{entered.Set();release.Wait();return Save(r,f);}));Assert.That(entered.Wait(5000),Is.True);try{Assert.That((string)Run()["state"],Is.EqualTo("busy"));}finally{release.Set();}Assert.That(task.Wait(5000),Is.True);}
 [TestCase("unknown")][TestCase("oversize")][TestCase("version")][TestCase("array")][TestCase("id")]
 public void MalformedRequestsFailBeforeGeneration(string kind){var r=Request();if(kind=="unknown")r["storeId"]="940";if(kind=="oversize")r["data"]["text"]=new string('a',17000);if(kind=="version")r["version"]=2;if(kind=="array")r["data"]=new JArray();if(kind=="id")r["eventId"]="bad id";Assert.That((string)Run(r)["state"],Is.EqualTo("rejected"));Assert.That(generated,Is.Zero);}
 [Test] public void PublicRouteUsesOriginalEnabledProvider(){string routed=null;var result=Call("Route",Request(),new Func<string,object,object>((route,data)=>{
  if(route.EndsWith("/regex"))return new JArray(new JObject{["Program"]=new JObject{["Address"]="933",["IsEnabled"]=true},["ImplementedInterface"]=new JObject{["ApiUrl"]="AI.Genies/Gemini",["Options"]=new JObject{["chat"]=new JObject{["feedback"]=true}}}});routed=route;return new JObject{["Status"]="Ok"};}));Assert.That(routed,Is.EqualTo("AI.Genies/Gemini/Chat.Feedback"));}
 [Test] public void JsonCannotSupplyNativeGenerationDelegate(){Assert.That((string)((JObject)Call("Run",Request(),new Func<string,object,object>(Api)))["state"],Is.EqualTo("rejected"));}
 [Test] public void ContextProjectsSelectionWithoutChangingExportRows(){Run();int count=rows.Count;var listType=typeof(List<>).MakeGenericType(message);var list=JsonConvert.DeserializeObject(rows.ToString(),listType);var context=(JObject)history.GetMethod("Context").Invoke(null,new[]{list,"profile-a"});var messages=(JArray)context["messages"];Assert.That(messages.Count,Is.EqualTo(3));Assert.That((string)messages[1]["role"],Is.EqualTo("user"));Assert.That((string)messages[1]["parts"][0]["text"],Does.Contain("roots").And.Contain(sourceId));Assert.That(rows.Count,Is.EqualTo(count));Assert.That(((JArray)((JObject)history.GetMethod("Context").Invoke(null,new[]{list,"other"}))["messages"]).Count,Is.Zero);}
 [Test] public void DeletedAttemptDoesNotInvalidateRegeneratedPresentation() {
  var oldSource=(JObject)rows[0].DeepClone();
  rows.Insert(0,new JObject{["role"]="model",["parts"]=new JArray(new JObject{["text"]="Earlier conversation"})});
  rows.Insert(1,new JObject{["role"]="user",["parts"]=new JArray(new JObject{["text"]="First attempt"})});
  rows.RemoveAt(rows.Count-1);rows.RemoveAt(rows.Count-1);
  Assert.That((string)Run()["state"],Is.EqualTo("rejected"));Assert.That(generated,Is.Zero);
  sourceId=Guid.NewGuid().ToString("N");oldSource["metadata"]["requestId"]=sourceId;
  rows.Add(new JObject{["role"]="user",["parts"]=new JArray(new JObject{["text"]="Regenerate with updated prompt"})});rows.Add(oldSource);
  int before=rows.Count;var receipt=Run();Assert.That((string)receipt["state"],Is.EqualTo("completed"));
  Assert.That(rows.Count,Is.EqualTo(before+1));Assert.That((string)rows.Last["role"],Is.EqualTo("model"));
  Assert.That((string)rows.Last["metadata"]["feedback"]["sourceRequestId"],Is.EqualTo(sourceId));Assert.That(generated,Is.EqualTo(1));
 }

}
