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
public class AiGenieProviderOptionsTest {
 Type turn;
 [OneTimeSetUp] public void CompileShippedOptions() {
  var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
  var folder=Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie");
  XElement Program(string package,string address)=>XDocument.Load(Path.Combine(folder,package,"programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")==address);
  string Class(XElement program,string name)=>CSharpSyntaxTree.ParseText("class Holder{"+(string)program.Element("ScriptContext")+"}").GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text==name).ToFullString();
  var executor=Program("homegenie-base","581");var shared=Program("homegenie-base","583");
  string code="using System;using System.Linq;using System.Collections.Generic;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+Class(shared,"AiGenieProviderOptions");
  var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location).Distinct();
  var compilation=CSharpCompilation.Create("CanonicalPolicy"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
  using var stream=new MemoryStream();var result=compilation.Emit(stream);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));
  var assembly=Assembly.Load(stream.ToArray());turn=assembly.GetType("AiGenieProviderOptions");
 }


 string Build(JArray providers,string selected="940")=>(string)turn.GetMethod("Build").Invoke(null,new object[]{providers,selected});
 JObject Provider(string id,string name,string route,bool enabled=true)=>new(){["Program"]=new JObject{["Address"]=id,["Name"]=name,["IsEnabled"]=enabled},["ImplementedInterface"]=new JObject{["ApiUrl"]=route}};
 [Test] public void OptionsUseEnabledGenieDescriptorsAndStableProgramIds() {
  var providers=new JArray(Provider("940","Lailama","AI.Genies/Lailama"),Provider("933","Gemini Genie","AI.Genies/Gemini"),
   Provider("930","Old intent handler","AI.IntentHandlers/Gemini"),Provider("1000","Disabled","AI.Genies/Other",false));
  string before=providers.ToString();Assert.That(Build(providers),Is.EqualTo("select:Gemini Genie=933,Lailama=940"));Assert.That(providers.ToString(),Is.EqualTo(before));
 }
 [Test] public void MissingSelectedProviderIsRetainedWithoutAutomaticFallback() {
  var providers=new JArray(Provider("933","Gemini Genie","AI.Genies/Gemini"));
  Assert.That(Build(providers),Is.EqualTo("select:Gemini Genie=933,Unavailable AI Genie (940)=940"));
  Assert.That(Build(new JArray()),Is.EqualTo("select:Unavailable AI Genie (940)=940"));
 }
 [Test] public void RenamingProviderPreservesItsSavedIdentityAndLabelsCannotInjectSelectItems() {
  var providers=new JArray(Provider("940","Local, AI=provider:test","AI.Genies/Lailama"));
  Assert.That(Build(providers),Is.EqualTo("select:Local  AI provider test=940"));
 }
 [Test] public void NonProgramIdsAndMalformedDescriptorsAreNotSelectable() {
  Assert.That(Build(new JArray(Provider("940,933","bad","AI.Genies/Other"),new JObject()),null),Is.EqualTo("select:"));
 }
}
