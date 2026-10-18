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
public class AiGenieCanonicalPolicyTest {
 Type policy,contract;
 [OneTimeSetUp] public void CompileShippedPolicy() {
  var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
  var folder=Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie");
  XElement Program(string package,string address)=>XDocument.Load(Path.Combine(folder,package,"programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")==address);
  string Class(XElement program,string name)=>CSharpSyntaxTree.ParseText("class Holder{"+(string)program.Element("ScriptContext")+"}").GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text==name).ToFullString();
  var executor=Program("homegenie-base","581");var shared=Program("homegenie-base","583");
  Assert.That((string)executor.Element("ScriptSource"),Does.Contain("System.Utility/ActionExecutor/Response.Schema"));
  foreach(string address in new[]{"933","940"})Assert.That((string)Program("homegenie-ml-ai",address).Element("ScriptContext"),Does.Not.Contain("class GenieResponseContract").And.Not.Contain("class GenieCapabilities"));
  string code="using System;using System.Text;using System.Linq;using System.Collections.Generic;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+Class(executor,"GenieResponseContract")+Class(shared,"GenieCapabilities");
  var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location).Distinct();
  var compilation=CSharpCompilation.Create("CanonicalPolicy"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
  using var stream=new MemoryStream();var result=compilation.Emit(stream);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));
  var assembly=Assembly.Load(stream.ToArray());policy=assembly.GetType("GenieCapabilities");contract=assembly.GetType("GenieResponseContract");
 }
 JObject Request(JObject input,string operation,Func<string,object,object> call=null)=>(JObject)policy.GetMethod("Request").Invoke(null,new object[]{input,operation,call});
 [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
 public void SchemaIsCanonicalFilteredByCurrentPermissionsWithoutActions(int mask) {
  var selected=new JObject{["deviceControl"]=(mask&1)!=0,["automation"]=(mask&2)!=0,["widgets"]=(mask&4)!=0};
  var input=new JObject{["version"]=1,["capabilities"]=selected};string before=input.ToString();
  var canonical=(JObject)contract.GetMethod("Schema").Invoke(null,null);string original=canonical.ToString();int calls=0;
  var actual=Request(input,"schema",(route,data)=>{Assert.That(route,Is.EqualTo("System.Utility/ActionExecutor/Response.Schema"));Assert.That(data,Is.Null);calls++;return canonical;});
  bool Allowed(string kind)=>kind=="reply"||kind=="context"||kind=="device"&&(mask&1)!=0||kind=="program"&&(mask&2)!=0||kind=="widget"&&(mask&4)!=0||kind=="composite"&&(mask&6)==6;
  var expected=new JObject{["anyOf"]=new JArray(((JArray)canonical["anyOf"]).Where(v=>Allowed((string)v["properties"]["kind"]["enum"][0])).Select(v=>v.DeepClone()))};
  Assert.That(JToken.DeepEquals(actual,expected),Is.True);Assert.That(calls,Is.EqualTo(1));Assert.That(input.ToString(),Is.EqualTo(before));Assert.That(canonical.ToString(),Is.EqualTo(original));
 }
 [Test] public void ProviderDefaultsRemainDistinctAndExplicitPreferencesWin() {
  var input=new JObject{["version"]=1};Assert.That(Request(input,"normalize").Properties().All(p=>(bool)p.Value),Is.True);
  input["defaults"]=new JObject{["deviceControl"]=true,["automation"]=false,["widgets"]=false};
  Assert.That(Request(input,"normalize").ToString(Formatting.None),Is.EqualTo("{\"deviceControl\":true,\"automation\":false,\"widgets\":false}"));
  input["capabilities"]=new JObject{["automation"]=true};Assert.That((bool)Request(input,"normalize")["automation"],Is.True);
 }
 [TestCase("{'version':1,'capabilities':{'unknown':true}}")]
 [TestCase("{'version':1,'capabilities':{'automation':'true'}}")]
 [TestCase("{'version':1,'capabilities':[]}")]
 [TestCase("{'version':1,'defaults':{'automation':true}}")]
 [TestCase("{'version':2}")]
 public void InvalidSelectionsFailBeforeCallingExecutor(string json) {
  Assert.Throws<TargetInvocationException>(()=>Request(JObject.Parse(json),"schema",(r,p)=>throw new AssertionException("Must not call")));
 }
 [TestCase(null)] [TestCase("{'Status':'Error'}")] [TestCase("{'anyOf':[]}")]
 public void MissingOrOutdatedExecutorFailsClosed(string json) {
  Assert.Throws<TargetInvocationException>(()=>Request(new JObject{["version"]=1},"schema",(r,p)=>json==null?null:JObject.Parse(json)));
 }
}
