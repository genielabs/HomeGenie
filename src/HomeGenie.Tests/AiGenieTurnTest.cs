using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace HomeGenie.Tests;
[TestFixture]
public class AiGenieTurnTest {
 Type turn;
 [OneTimeSetUp] public void CompileShippedTurn() {
  var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
  var folder=Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie");
  XElement Program(string package,string address)=>XDocument.Load(Path.Combine(folder,package,"programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")==address);
  string Class(XElement program,string name)=>CSharpSyntaxTree.ParseText("class Holder{"+(string)program.Element("ScriptContext")+"}").GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text==name).ToFullString();
  var executor=Program("homegenie-base","581");var shared=Program("homegenie-base","583");
  string code="using System;using System.Linq;using System.Collections.Generic;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+Class(shared,"AiGenieTurn");
  var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location).Distinct();
  var compilation=CSharpCompilation.Create("CanonicalPolicy"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
  using var stream=new MemoryStream();var result=compilation.Emit(stream);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));
  var assembly=Assembly.Load(stream.ToArray());turn=assembly.GetType("AiGenieTurn");
 }

 System.Collections.Generic.IDictionary<string,object> Create(bool persist=true,bool receipts=true,Func<string,object,object> call=null) =>
  (System.Collections.Generic.IDictionary<string,object>)turn.GetMethod("Create").Invoke(null,new object[]{
   new System.Collections.Generic.Dictionary<string,object>{{"version",1},{"storeId","940"},{"persist",persist},{"contextCalls",receipts}},call??((route,data)=>null)});
 static JObject Snapshot(System.Collections.Generic.IDictionary<string,object> state,string outcome="completed")=>JObject.FromObject(((Func<string,object>)state["snapshot"])(outcome));
 [TestCase(false)] [TestCase(true)] public void UserAndFinalReplyKeepMetadataAndOnlyOneFinalWrite(bool receipts) {
  var writes=new System.Collections.Generic.List<JObject>();var state=Create(receipts:receipts,call:(route,data)=>{Assert.That(route,Is.EqualTo("System.Utility/ChatHistory/Add"));writes.Add(JObject.FromObject(data));return null;});
  ((Action<string>)state["profile"])("context-v1:10000|cap-v1:100");
  ((Action<object>)state["saveUser"])(new{role="user",parts=new[]{new{text="hello"}}});
  ((Action)state["firstToken"])();((Action)state["generationComplete"])();
  ((Action<long,int,int>)state["actionsCompleted"])(7,1,0);
  ((JArray)state["contextCalls"]).Add("read-api");
  ((Action<object,string>)state["saveReply"])(new{role="model",parts=new[]{new{text="done"}}},"completed");
  ((Func<string,string,string>)state["error"])("late error","error");
  Assert.That(writes.Count,Is.EqualTo(2));Assert.That((string)writes[0]["Metadata"]["outcome"],Is.EqualTo("submitted"));
  var metadata=writes[1]["Metadata"];Assert.That((string)metadata["requestId"],Is.EqualTo(state["requestId"]));
  Assert.That((string)metadata["contextProfileKey"],Is.EqualTo("context-v1:10000|cap-v1:100"));
  Assert.That((long)metadata["actionsMs"],Is.EqualTo(7));Assert.That((int)metadata["actionCount"],Is.EqualTo(1));
  Assert.That(metadata["firstTokenMs"].Type,Is.EqualTo(JTokenType.Integer));Assert.That(metadata["generationMs"].Type,Is.EqualTo(JTokenType.Integer));
  Assert.That(metadata["contextCalls"]!=null,Is.EqualTo(receipts));
  if(receipts)Assert.That(metadata["contextCalls"].Count(),Is.EqualTo(1));
 }
 [Test] public void ScheduledNoHistoryTurnsNeverWrite() {
  var state=Create(persist:false,call:(r,p)=>throw new AssertionException("No persistence expected"));
  ((Action<object>)state["saveUser"])(new{role="user"});((Func<string,string,string>)state["error"])("cancelled","cancelled");
  Assert.That((string)Snapshot(state)["requestId"],Is.EqualTo(state["requestId"]));
 }
 [Test] public void TurnsHaveIndependentProfilesCountersReceiptsAndFinalFlags() {
  var first=Create();var second=Create();Assert.That(first["requestId"],Is.Not.EqualTo(second["requestId"]));
  ((Action<string>)first["profile"])("first");((Action<long,int,int>)first["actionsCompleted"])(10,2,1);((JArray)first["contextCalls"]).Add("read");
  var clean=Snapshot(second);Assert.That(clean["contextProfileKey"].Type,Is.EqualTo(JTokenType.Null));Assert.That((int)clean["actionCount"],Is.Zero);Assert.That(clean["contextCalls"],Is.Empty);
 }
 [Test] public void FailedWriteCanBeRetriedWithoutClaimingPersistence() {
  int calls=0;var state=Create(call:(r,p)=>{if(++calls==1)throw new InvalidOperationException("offline");return null;});
  var save=(Action<object,string>)state["saveReply"];Assert.Throws<InvalidOperationException>(()=>save(new{role="model"},"error"));
  save(new{role="model"},"error");save(new{role="model"},"error");Assert.That(calls,Is.EqualTo(2));
 }
 [Test] public void ImportedMetricsPreserveReceiptsWithoutExecutingActions() {
  var state=Create(call:(r,p)=>throw new AssertionException("No API calls expected"));
  var data=JObject.Parse("{'generationMs':25,'actionsMs':7,'actionCount':1,'failedActions':0,'actions':[{'Success':true}]}");
  ((Action<JObject>)state["import"])(data);data["actions"][0]["Success"]=false;
  var result=Snapshot(state);Assert.That((long)result["generationMs"],Is.EqualTo(25));Assert.That((bool)result["actions"][0]["Success"],Is.True);
  Assert.Throws<InvalidOperationException>(()=>((Action<JObject>)state["import"])(new JObject()));
  Assert.That((int)Snapshot(state)["actionCount"],Is.EqualTo(1));
 }
 [Test] public void ErrorAndCancellationRetainOutcomeAndVisibleText() {
  JObject saved=null;var state=Create(call:(r,p)=>{saved=JObject.FromObject(p);return null;});
  string text=((Func<string,string,string>)state["error"])("[STOPPED]","cancelled");
  Assert.That(text,Is.EqualTo("[STOPPED]"));Assert.That((string)saved["Metadata"]["outcome"],Is.EqualTo("cancelled"));Assert.That((string)saved["Message"]["parts"][0]["text"],Is.EqualTo(text));
 }
}
