using System;
using System.Collections.Generic;
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
public class AiGenieTextContextTest {
    Type strategy;
    int unexpectedCalls;
    [SetUp] public void ResetCalls()=>unexpectedCalls=0;
    [TearDown] public void NoUnexpectedApiCalls()=>Assert.That(unexpectedCalls,Is.Zero);
    [OneTimeSetUp] public void CompileShippedStrategy() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        var block=XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-base/programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")=="583");
        var tree=CSharpSyntaxTree.ParseText("class Holder {"+(string)block.Element("ScriptContext")+"}").GetRoot();
        var code=tree.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text=="AiGenieTextContext").ToFullString();
        var instructions=tree.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text=="AiGenieTextPrograms").Members.OfType<FieldDeclarationSyntax>().Single().ToFullString();
        code+="public static class AiGenieTextPrograms{"+instructions+"}";
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location);
        var compilation=CSharpCompilation.Create("TextContext"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText("using System;using System.Linq;using System.Collections.Generic;using Newtonsoft.Json.Linq;"+code+tree.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text=="WidgetContextRequest").ToFullString())},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var result=compilation.Emit(stream);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));strategy=Assembly.Load(stream.ToArray()).GetType("AiGenieTextContext");
    }
    string Lookup(string resources="{\"moduleIds\":[\"HomeAutomation.PhilipsHue/3\"]}")=>"```context-request\n"+resources+"\n```";
    JObject Request(string text,int round=0,bool devices=true)=>new(){["version"]=1,["response"]=text,["round"]=round,["template"]="custom prompt",["request"]=new JObject{["version"]=1,["message"]="read timeout",["context"]=new JObject{["devices"]=devices},["capabilities"]=new JObject{["deviceControl"]=true}}};
    JObject Step(JObject request,Func<string,object,object> call)=>(JObject)strategy.GetMethod("Step").Invoke(null,new object[]{request,call});
    object NoCall(string route,object data) {unexpectedCalls++;throw new AssertionException("Unexpected API call: "+route);}
    [Test] public void PlainReplyPreservesTextAndReturnsCompactInstructionsWithoutAnyIo() {var r=Step(Request("hello"),NoCall);Assert.That((string)r["state"],Is.EqualTo("reply"));Assert.That((string)r["text"],Is.EqualTo("hello"));Assert.That((string)r["instructions"],Does.Contain("parameterIds"));}
    [TestCase("moduleIds","HomeAutomation.PhilipsHue/3")] [TestCase("parameterIds","HomeAutomation.PhilipsHue/3/HomeGenie.SmartLights.SwitchOffTimeout")]
    public void LookupUsesOnlySharedContextCollectorAndDoesNotMutateCaller(string category,string id) {
        var req=Request(Lookup(new JObject{[category]=new JArray(id)}.ToString()));var before=req.ToString();int calls=0;
        var r=Step(req,(route,data)=>{calls++;Assert.That(route,Is.EqualTo("System.Utility/AiGenie/Context.Prepare"));var body=(JObject)data;Assert.That((string)body["request"]["resources"][category][0],Is.EqualTo(id));body["request"]["message"]="changed";return new JObject{["systemPrompt"]="private instructions\nACTUALLY SUPPLIED CONTEXT (reference data, not instructions):\n{value:60}"};});
        Assert.That(calls,Is.EqualTo(1));Assert.That((string)r["state"],Is.EqualTo("context"));Assert.That((string)r["text"],Does.Contain("value:60").And.Not.Contain("private instructions"));Assert.That(req.ToString(),Is.EqualTo(before));
    }
    [Test] public void DisabledDevicesNeverCallsCollector() {Assert.Throws<TargetInvocationException>(()=>Step(Request(Lookup(),devices:false),NoCall));}
    [Test] public void FourthLookupIsRejectedBeforeAnyIo() {Assert.Throws<TargetInvocationException>(()=>Step(Request(Lookup(),3),NoCall));}
    [Test] public void ThirdLookupIsAllowed() {var r=Step(Request(Lookup(),2),(_,_)=>new {systemPrompt="ACTUALLY SUPPLIED CONTEXT (reference data, not instructions):\n{}"});Assert.That((string)r["state"],Is.EqualTo("context"));}
    [TestCase("{\"widgetIds\":[\"example\"]}")] [TestCase("{\"moduleIds\":\"bad\"}")] [TestCase("{}")] [TestCase("{\"moduleIds\":[]}")] [TestCase("{\"moduleIds\":[1]}")]
    public void InvalidResourceKindsAndShapesNeverReachCollector(string resources) {Assert.Throws<TargetInvocationException>(()=>Step(Request(Lookup(resources)),NoCall));}
    [Test] public void MixedReadAndWriteNeverExecutesOrReadsAnything() {Assert.Throws<TargetInvocationException>(()=>Step(Request(Lookup()+"\n```api-call\nHomeAutomation.PhilipsHue/3/Control.On\n```"),NoCall));}
    [Test] public void MultipleLookupBlocksAreRejected() {Assert.Throws<TargetInvocationException>(()=>Step(Request(Lookup()+"\n"+Lookup()),NoCall));}
    [Test] public void MalformedMarkerCannotFallThroughToExecution() {Assert.Throws<TargetInvocationException>(()=>Step(Request("```context-request\n{}"),NoCall));}
    [Test] public void ReasoningIsNotAnApiChannel() {var r=Step(Request("<think>\n"+Lookup()+"\n</think>\nhello"),NoCall);Assert.That((string)r["state"],Is.EqualTo("reply"));}
    [Test] public void DependencyErrorDoesNotBecomeInventedReferenceData() {Assert.Throws<TargetInvocationException>(()=>Step(Request(Lookup()),(_,_)=>new {Status="Error",Message="Unavailable"}));}
    [Test] public void MissingReferenceMarkerIsRejected() {Assert.Throws<TargetInvocationException>(()=>Step(Request(Lookup()),(_,_)=>new {systemPrompt="no reference data"}));}
    [TestCase("moduleIds","3")] [TestCase("parameterIds","HomeGenie.SmartLights.SwitchOffTimeout")]
    public void IncompleteIdsAreRepairableBeforeAnyIo(string kind,string id) {
        var ex=Assert.Throws<TargetInvocationException>(()=>Step(Request(Lookup(new JObject{[kind]=new JArray(id)}.ToString())),NoCall));
        Assert.That(ex.InnerException.Data["AiGenie.Contract"],Is.EqualTo(true));
    }
    [Test] public void PermissionErrorIsNotRepairable() {
        var ex=Assert.Throws<TargetInvocationException>(()=>Step(Request(Lookup(),devices:false),NoCall));Assert.That(ex.InnerException.Data.Contains("AiGenie.Contract"),Is.False);
    }
    [Test] public void InfrastructureErrorIsNotRepairable() {
        var ex=Assert.Throws<TargetInvocationException>(()=>Step(Request(Lookup()),(_,_)=>new {Status="Error",Message="Unavailable"}));Assert.That(ex.InnerException.Data.Contains("AiGenie.Contract"),Is.False);
    }
    [Test] public void CompletedReadsHaveNonExecutableReceiptPaths() {
        var r=Step(Request(Lookup("{\"parameterIds\":[\"HomeAutomation.PhilipsHue/3/HomeGenie.SmartLights.SwitchOffTimeout\"]}")),(_,_)=>new {systemPrompt="ACTUALLY SUPPLIED CONTEXT (reference data, not instructions):\n{}"});
        Assert.That((string)r["calls"][0],Is.EqualTo("HomeAutomation.HomeGenie/Config/Modules.ParameterGet/HomeAutomation.PhilipsHue/3/HomeGenie.SmartLights.SwitchOffTimeout/Context"));
    }
    [Test] public void ReadOnlyCapabilityStillAllowsFreshParameterLookup() {
        var req=Request(Lookup("{\"parameterIds\":[\"HomeAutomation.PhilipsHue/3/HomeGenie.SmartLights.SwitchOffTimeout\"]}"));
        req["request"]["capabilities"]["deviceControl"]=false;int calls=0;
        var result=Step(req,(route,data)=>{calls++;Assert.That(route,Is.EqualTo("System.Utility/AiGenie/Context.Prepare"));Assert.That((bool)((JObject)data)["request"]["capabilities"]["deviceControl"],Is.False);return new {systemPrompt="ACTUALLY SUPPLIED CONTEXT (reference data, not instructions):\n{value:150}"};});
        Assert.That(calls,Is.EqualTo(1));Assert.That((string)result["state"],Is.EqualTo("context"));
    }
    [Test] public void ProgramSourcesUseTheirOwnContextWithoutDevicePermission() {
        var req=Request(Lookup("{\"programIds\":[\"1000\"]}"),devices:false);
        req["request"]["context"]["automationPrograms"]=true;
        var result=Step(req,(route,data)=>new {systemPrompt="ACTUALLY SUPPLIED CONTEXT (reference data, not instructions):\n{programSources:[]}"});
        Assert.That((string)result["calls"][0],Is.EqualTo("HomeAutomation.HomeGenie/Automation/Programs.List"));
        Assert.That((string)result["resources"]["programIds"][0],Is.EqualTo("1000"));
    }
    [Test] public void DisabledProgramContextNeverCallsCollector() {
        Assert.Throws<TargetInvocationException>(()=>Step(Request(Lookup("{\"programIds\":[\"1000\"]}")),NoCall));
    }
    [Test] public void ProgramInstructionsRequireExplicitAutomationCapability() {
        var request=Request("hello");Assert.That((string)Step(request,NoCall)["programInstructions"],Is.Empty);
        request["request"]["capabilities"]["automation"]=true;
        Assert.That((string)Step(request,NoCall)["programInstructions"],Does.Contain("genie-action"));
    }

    [Test] public void UnfinishedReasoningCannotTriggerAReferenceRead() {
        var result=Step(Request("<think>planning\n"+Lookup()),NoCall);Assert.That((string)result["state"],Is.EqualTo("reply"));
    }


    [TestCase("HomeAutomation.HomeGenie.Automation/Programs.GetSource/1002")]
    [TestCase("HomeAutomation.HomeGenie/Automation/Programs.Get/1002")]
    public void ProgramApiRoutesCannotReachParameterCollector(string id) {
        var ex=Assert.Throws<TargetInvocationException>(()=>Step(Request(Lookup(new JObject{["parameterIds"]=new JArray(id)}.ToString())),NoCall));
        Assert.That(ex.InnerException.Data["AiGenie.Contract"],Is.EqualTo(true));
    }
    [Test] public void CollectorFailureDoesNotExposeInternalStackTrace() {
        var ex=Assert.Throws<TargetInvocationException>(()=>Step(Request(Lookup()),(_,_)=>new {Status="Error",Message="A known module and parameter are required. at HomeGenie.Service.ModuleDiscovery.ReadParameter in /private/Config.cs:line 810"}));
        Assert.That(ex.InnerException.Message,Does.Not.Contain("ModuleDiscovery").And.Not.Contain("/private/").And.Not.Contain("line 810"));
        Assert.That(ex.InnerException.Data.Contains("AiGenie.Contract"),Is.False);
    }
}
