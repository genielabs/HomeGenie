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
public class AiGenieHistoryTest {
 Type history,store;
 [OneTimeSetUp] public void CompileShippedHistory() {
  var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
  var folder=Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie");
  XElement Program(string package,string address)=>XDocument.Load(Path.Combine(folder,package,"programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")==address);
  string Class(XElement program,string name)=>CSharpSyntaxTree.ParseText("class Holder{"+(string)program.Element("ScriptContext")+"}").GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text==name).ToFullString();
  var executor=Program("homegenie-base","581");var shared=Program("homegenie-base","583");
  var chat=Program("homegenie-base","577");string source=(string)chat.Element("ScriptSource");
  int begin=source.IndexOf("            string contextProfileKey =",StringComparison.Ordinal);int end=source.IndexOf("        case \"Add\":",begin,StringComparison.Ordinal);
  string getContext=source.Substring(begin,end-begin);
  string code="using System;using System.Linq;using System.Collections.Generic;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+Class(shared,"AiGenieHistory")+Class(chat,"ChatMessage")+Class(chat,"MessagePart")+
   "public static class HistoryProbe {public static object GetContext(JArray input,string profile){var currentHistory=input.ToObject<List<ChatMessage>>();var data=new JObject{[\"ContextProfileKey\"]=profile};"+getContext+"}}";
  var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location).Distinct();
  var compilation=CSharpCompilation.Create("CanonicalPolicy"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
  using var stream=new MemoryStream();var result=compilation.Emit(stream);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));
  var assembly=Assembly.Load(stream.ToArray());history=assembly.GetType("AiGenieHistory");store=assembly.GetType("HistoryProbe");
 }


 JObject Read(JArray messages,string projection="text",string profile="profile") => (JObject)history.GetMethod("Read").Invoke(null,new object[]{
  new JObject{["version"]=1,["storeId"]="940",["profileKey"]=profile,["projection"]=projection},
  new Func<string,object,object>((route,input)=>{Assert.That(route,Is.EqualTo("System.Utility/ChatHistory/GetContext"));var scope=(JObject)input;Assert.That((string)scope["StoreId"],Is.EqualTo("940"));return store.GetMethod("GetContext").Invoke(null,new object[]{messages,(string)scope["ContextProfileKey"]});})});
 JArray Project(JArray messages,string projection="text")=>(JArray)history.GetMethod("Project").Invoke(null,new object[]{new JObject{["version"]=1,["messages"]=messages,["projection"]=projection}});
 JObject Message(string role,string text,string id,string outcome,string profile="profile")=>new(){["role"]=role,["parts"]=new JArray(new JObject{["text"]=text}),["metadata"]=new JObject{["requestId"]=id,["outcome"]=outcome,["contextProfileKey"]=profile}};
 [TestCase("text")] [TestCase("structured")] public void OnlyMatchingCompletedTurnsReachProvider(string projection) {
  var raw=new JArray(Message("user","ok question","ok","submitted"),Message("model","ok answer","ok","completed"),
   Message("user","failed question","error","submitted"),Message("model","failed answer","error","error"),
   Message("user","cancelled question","cancel","submitted"),Message("model","[STOPPED]","cancel","cancelled"),
   Message("user","pending question","pending","submitted"),Message("model","different profile","other","completed","other-profile"));
  string before=raw.ToString();var result=Read(raw,projection);Assert.That(result["messages"].Count(),Is.EqualTo(2));
  Assert.That((string)result["messages"][1]["parts"][0]["text"],Is.EqualTo("ok answer"));Assert.That(raw.ToString(),Is.EqualTo(before));
 }
 [Test] public void StructuredProjectionPreservesHumanRequestsAndTechnicalContentExactly() {
  var raw=new JArray(Message("user","La funzionalità smart device è attiva sulla luce 1?","read","submitted"),
   Message("model","<think>reasoning</think>\n```genie-action\n{\"version\":1}\n```\nAnswer","read","completed"),
   Message("user","Attiva smart device su luce 1","write","submitted"));
  string before=raw.ToString();var result=Project(raw,"structured");Assert.That(JToken.DeepEquals(result,raw),Is.True);
  result[0]["parts"][0]["text"]="modified";Assert.That(raw.ToString(),Is.EqualTo(before));
 }
 [Test] public void TextProjectionRemovesOnlyExistingTechnicalChannelsAndKeepsReceiptsAndUserExamples() {
  string technical="<think>reasoning</think>\n```context-request\n{}\n```\n```genie-action\n{}\n```\nProgram updated 1002";
  var raw=new JArray(Message("user",technical,"turn","submitted"),Message("model",technical,"turn","completed"));
  var result=Project(raw);Assert.That((string)result[0]["parts"][0]["text"],Is.EqualTo(technical));Assert.That((string)result[1]["parts"][0]["text"],Is.EqualTo("Program updated 1002"));
  Assert.That((string)raw[1]["parts"][0]["text"],Is.EqualTo(technical));
 }
 [Test] public void LegacyUnprofiledHistoryKeepsItsExistingReadBehavior() {
  var raw=JArray.Parse("[{'role':'user','parts':[{'text':'old question'}]},{'role':'model','parts':[{'text':'old answer'}]}]");
  var unscoped=Read(raw,"structured",null);Assert.That(JToken.DeepEquals(unscoped["messages"],raw),Is.True);
  Assert.That(Read(raw)["messages"],Is.Empty);
 }
 [TestCase("{'version':2,'storeId':'940','projection':'text'}")]
 [TestCase("{'version':1,'storeId':'940','projection':'unknown'}")]
 [TestCase("{'version':1,'storeId':'940','projection':'text','profileKey':false}")]
 [TestCase("{'version':1,'storeId':'940','projection':'text','extra':true}")]
 public void InvalidReadCannotAccessStorage(string input) {
  Assert.Throws<TargetInvocationException>(()=>history.GetMethod("Read").Invoke(null,new object[]{JObject.Parse(input),new Func<string,object,object>((r,p)=>throw new AssertionException("Must not call storage"))}));
 }
 [TestCase(null)] [TestCase("[]")] [TestCase("{'Status':'Error','Message':'offline'}")]
 [TestCase("{'contextProfileKey':'different','messages':[]}")]
 public void UnavailableOrWrongProfileNeverFallsBackToAnotherHistory(string response) {
  var input=JObject.Parse("{'version':1,'storeId':'940','profileKey':'profile','projection':'text'}");
  Assert.Throws<TargetInvocationException>(()=>history.GetMethod("Read").Invoke(null,new object[]{input,new Func<string,object,object>((r,p)=>response==null?null:JToken.Parse(response))}));
 }
 [Test] public void TextProjectionIsStableForNativeCacheRebuild() {
  var raw=new JArray(Message("model","<think>hidden</think> final answer","turn","completed"));var once=Project(raw);Assert.That(JToken.DeepEquals(Project(once),once),Is.True);
 }
}
