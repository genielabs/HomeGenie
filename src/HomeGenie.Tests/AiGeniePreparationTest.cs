using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace HomeGenie.Tests;
[TestFixture]
public class AiGeniePreparationTest {
    Type preparation,contract,capabilities;
    [OneTimeSetUp] public void CompileDistributedPreparationAndContracts() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        XElement Program(string package,string id)=>XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie",package,"programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")==id);
        string Class(XElement p,string name)=>CSharpSyntaxTree.ParseText("class Holder {"+(string)p.Element("ScriptContext")+"}").GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text==name).ToFullString();
        string code="using System;using System.IO;using System.Linq;using System.Text;using System.Collections.Generic;using System.Security.Cryptography;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+
            Class(Program("homegenie-base","583"),"AiGeniePreparation")+Class(Program("homegenie-base","581"),"GenieResponseContract")+Class(Program("homegenie-ml-ai","933"),"GenieCapabilities");
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location);
        var compilation=CSharpCompilation.Create("AiPreparation"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var result=compilation.Emit(stream);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));
        var assembly=Assembly.Load(stream.ToArray());preparation=assembly.GetType("AiGeniePreparation");contract=assembly.GetType("GenieResponseContract");capabilities=assembly.GetType("GenieCapabilities");
    }
    JObject Compose(JObject request,Func<string,object,object> call)=>(JObject)preparation.GetMethod("Compose").Invoke(null,new object[]{request,call});
    JObject Normalize(JObject response,Func<string,object,object> call)=>(JObject)preparation.GetMethod("Normalize").Invoke(null,new object[]{response,call});
    JObject Request(int mask=7,string template="#use-skill homegenie-genie")=>new(){["version"]=1,["template"]=template,["capabilities"]=new JObject{["deviceControl"]=(mask&1)!=0,["automation"]=(mask&2)!=0,["widgets"]=(mask&4)!=0}};
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
    public void CompositionPreservesCapabilityInstructionsAndSelectsOnlyApplicableReferences(int mask) {
        var request=Request(mask);var before=request.ToString();string selectedTemplate=null;var shared=new JObject{["prompt"]="expanded prompt",["skills"]=new JArray("core"),["resources"]=new JArray()};
        var result=Compose(request,(route,payload)=>{Assert.That(route,Is.EqualTo("System.Utility/ContextEngine/Prompt.Compose"));selectedTemplate=(string)JObject.FromObject(payload)["template"];return shared;});
        Assert.That(selectedTemplate.Contains("references/automation.md"),Is.EqualTo((mask&2)!=0));
        Assert.That(selectedTemplate.Contains("homegenie-orchestration"),Is.EqualTo((mask&2)!=0));
        Assert.That(selectedTemplate.Contains("references/widgets.md"),Is.EqualTo((mask&4)!=0));
        string expected="expanded prompt"+(string)capabilities.GetMethod("Instructions").Invoke(null,new object[]{request["capabilities"]});
        Assert.That((string)result["prompt"],Is.EqualTo(expected));
        string fingerprint=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(expected))).ToLowerInvariant();
        Assert.That((string)result["fingerprint"],Is.EqualTo(fingerprint));Assert.That((string)shared["prompt"],Is.EqualTo("expanded prompt"));Assert.That(request.ToString(),Is.EqualTo(before));
    }
    [Test] public void CustomPromptIsNotReplacedOrGivenUnrequestedSkillReferences() {
        const string template="Custom text\n#use-skill custom\nMore text";
        Compose(Request(template:template),(route,payload)=>{Assert.That((string)JObject.FromObject(payload)["template"],Is.EqualTo(template));return new JObject{["prompt"]=template};});
    }
    [Test] public void InvalidCapabilitiesFailBeforeReadingSkills() {
        var request=Request();request["capabilities"]["automation"]="true";
        var error=Assert.Throws<TargetInvocationException>(()=>Compose(request,(r,p)=>throw new AssertionException("Must not call")));
        Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());
    }
    [Test] public void MissingPromptServiceAndErrorRepliesNeverBecomePromptText() {
        foreach(object reply in new object[]{null,new JObject{["Status"]="Error",["Message"]="missing skill"},new JObject{["prompt"]=42}})
            Assert.Throws<TargetInvocationException>(()=>Compose(Request(),(r,p)=>reply));
    }
    object Canonical(object payload) {
        try {return contract.GetMethod("Normalize").Invoke(null,new[]{payload});}
        catch(TargetInvocationException ex){return new JObject{["Status"]="Error",["Message"]=ex.InnerException.Message};}
    }
    [TestCase("{'version':1,'kind':'reply','explanation':'hello'}")]
    [TestCase("{'version':1,'kind':'device','explanation':'on','commands':['Test.Lights/1/Control.On']}")]
    [TestCase("{'version':1,'kind':'context','explanation':'read','resources':{'moduleIds':['Test.Lights/1']}}")]
    [TestCase("{'solution_type':'reply','explanation':'legacy'}")]
    public void NormalizationUsesExistingContractWithoutExecutingAnything(string json) {
        var input=JObject.Parse(json);int calls=0;
        var result=Normalize(input,(route,payload)=>{Assert.That(route,Is.EqualTo("System.Utility/ActionExecutor/Response.Normalize"));Assert.That(payload,Is.SameAs(input));calls++;return Canonical(payload);});
        Assert.That(calls,Is.EqualTo(1));Assert.That(result["version"].Value<int>(),Is.EqualTo(1));Assert.That(result["Status"],Is.Null);
    }
    [Test] public void InvalidContractRetainsRepairClassificationButServiceFailureDoesNot() {
        var input=JObject.Parse("{'version':1,'kind':'device','explanation':'bad','commands':['Test/1/Delete.All']}");
        var result=Normalize(input,(r,p)=>Canonical(p));Assert.That((string)result["errorKind"],Is.EqualTo("contract"));
        Assert.Throws<TargetInvocationException>(()=>Normalize(input,(r,p)=>null));
    }
    [Test] public void OversizedResponseIsRejectedBeforeDelegating() {
        var input=new JObject{["explanation"]=new string('x',512*1024)};
        Assert.Throws<TargetInvocationException>(()=>Normalize(input,(r,p)=>throw new AssertionException("Must not call")));
    }
}
