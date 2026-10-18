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
public class AiGenieTextArtifactsTest {
    Type dispatcher,contract;
    int deployments,recoveries,normalizations;
    string ledgerState="completed";
    bool compile;
    [OneTimeSetUp] public void CompileDistributedAdapterAndExecutors() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        XElement Program(string id)=>XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-base/programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")==id);
        string Class(XElement p,string name)=>CSharpSyntaxTree.ParseText("class Holder {"+(string)p.Element("ScriptContext")+"}").GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text==name).ToFullString();
        var shared=Program("583");string code="using System;using System.IO;using System.Text;using System.Linq;using System.Collections.Generic;using System.Security.Cryptography;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+
            string.Join("\n",new[]{"GenieCapabilities","AiGeniePreparation","AiGenieTextPrograms","AiGenieTextArtifacts","AiGenieProgramActions","CompositeSolutionContract","AiGenieCompositeActions","AiGeniePresentation"}.Select(n=>Class(shared,n)))+Class(Program("581"),"GenieResponseContract");
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location);
        var compilation=CSharpCompilation.Create("TextArtifacts"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var result=compilation.Emit(stream);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));var assembly=Assembly.Load(stream.ToArray());dispatcher=assembly.GetType("AiGenieTextArtifacts");contract=assembly.GetType("GenieResponseContract");
    }
    [SetUp] public void Reset(){deployments=recoveries=normalizations=0;ledgerState="completed";compile=false;}
    JObject Artifact(string kind="widget") {
        var a=JObject.Parse("{'version':1,'kind':'widget','language':'javascript','explanation':'Preview','widget':{'html':'<div #value></div>','css':'','javascript':'class Demo extends ControllerInstance {}'}}");
        a["kind"]=kind;
        if(kind=="composite" || kind=="program") {
            a["language"]="csharp";a["automation"]=JObject.Parse("{'action':'create','program_id':'','name':'simulator','setup':'Program.Run();','main':'Program.GoBackground();','context':''}");
            if(kind=="composite")a["binding"]=JObject.Parse("{'domain':'HomeAutomation.HomeGenie.Automation','address':'$program','properties':['Sensor.Level']}");
            else a.Remove("widget");
        }
        return a;
    }
    JObject Request(string kind="widget")=>new(){["version"]=1,["requestId"]="artifact-test",["response"]="```genie-action\n"+Artifact(kind).ToString(Formatting.None)+"\n```",["state"]=new JObject(),["capabilities"]=new JObject{["automation"]=true,["widgets"]=true,["deviceControl"]=false},["context"]=new JObject(),["resources"]=new JObject()};
    object Call(string route,object payload) {
        if(route.EndsWith("/Response.Normalize")) {
            normalizations++;
            try{return contract.GetMethod("Normalize").Invoke(null,new[]{payload});}
            catch(TargetInvocationException ex){return new JObject{["Status"]="Error",["Message"]=ex.InnerException.Message};}
        }
        if(route.EndsWith("/Program.Deploy")) {
            deployments++;var sent=JObject.Parse((string)payload);
            return new JObject{["RequestId"]="artifact-test",["Index"]=sent["attempt"].DeepClone(),["State"]=ledgerState,["Success"]=ledgerState=="completed",["Response"]=new JObject{["program_id"]="1004",["status"]=compile?"compile_failed":"success",["compiler_errors"]=new JArray("compiler diagnostic")}};
        }
        Assert.That(route,Does.EndWith("/Program.Recover"));recoveries++;return new JObject{["RequestId"]="artifact-test",["State"]="completed",["Success"]=true};
    }
    JObject Dispatch(JObject request)=>(JObject)dispatcher.GetMethod("Dispatch").Invoke(null,new object[]{request,new Func<string,object,object>(Call)});
    [Test] public void StandaloneWidgetUsesExistingInlineFormatWithoutDeployingAnything() {
        var req=Request();string before=req.ToString();var r=Dispatch(req);
        Assert.That((string)r["reply"],Does.Contain("```html").And.Contain("```css").And.Contain("```javascript").And.Not.Contain("genie-action"));
        Assert.That((int)r["actionCalls"],Is.Zero);Assert.That(deployments+recoveries,Is.Zero);Assert.That(req.ToString(),Is.EqualTo(before));
    }
    [TestCase("widgets")][TestCase("automation")]
    public void CompositeRequiresBothCapabilitiesBeforeAnyIo(string disabled) {
        var req=Request("composite");req["capabilities"][disabled]=false;
        Assert.Throws<TargetInvocationException>(()=>Dispatch(req));Assert.That(normalizations+deployments+recoveries,Is.Zero);
    }
    [Test] public void DisabledWidgetsCannotProduceAPreview() {
        var req=Request();req["capabilities"]["widgets"]=false;Assert.Throws<TargetInvocationException>(()=>Dispatch(req));Assert.That(normalizations,Is.Zero);
    }
    [Test] public void CompositeReusesDeploymentAndResolvesProgramBeforeWaitingPreview() {
        var r=Dispatch(Request("composite"));var solution=JObject.Parse((string)r["reply"])["ai_solution"];
        Assert.That(deployments,Is.EqualTo(1));Assert.That((string)solution["program"]["id"],Is.EqualTo("1004"));
        Assert.That((string)solution["widget"]["state"],Is.EqualTo("waiting"));Assert.That((string)solution["widget"]["binding"]["address"],Is.EqualTo("1004"));
        Assert.That((string)r["state"]["binding"]["address"],Is.EqualTo("$program"));
    }
    [Test] public void ExistingProgramPathStillUsesTheSameExecutor() {var r=Dispatch(Request("program"));Assert.That(deployments,Is.EqualTo(1));Assert.That((string)r["reply"],Does.Contain("automan_deploy"));}
    [Test] public void CompilerRepairPreservesCompositeAndBinding() {
        compile=true;ledgerState="failed";var req=Request("composite");var first=Dispatch(req);
        Assert.That((string)first["outcome"],Is.EqualTo("repair"));Assert.That((string)first["feedback"],Does.Contain("kind=composite"));
        req["state"]=first["state"].DeepClone();compile=false;ledgerState="completed";var final=Dispatch(req);
        Assert.That((string)final["outcome"],Is.EqualTo("completed"));Assert.That(deployments,Is.EqualTo(2));
    }
    [TestCase("binding")][TestCase("program")][TestCase("widget")]
    public void RepairCannotChangeTheCommittedArtifactOrBinding(string change) {
        compile=true;ledgerState="failed";var req=Request("composite");var first=Dispatch(req);req["state"]=first["state"].DeepClone();var a=Artifact(change=="binding"?"composite":change);
        if(change=="binding")a["binding"]["properties"]=new JArray("Other.Field");
        req["response"]="```genie-action\n"+a+"\n```";Assert.Throws<TargetInvocationException>(()=>Dispatch(req));Assert.That(deployments,Is.EqualTo(1));
    }
    [Test] public void UnknownProgramOutcomeBlocksWidgetWithoutAutomaticRecovery() {
        ledgerState="unknown";var r=Dispatch(Request("composite"));var solution=JObject.Parse((string)r["reply"])["ai_solution"];
        Assert.That((string)solution["widget"]["state"],Is.EqualTo("blocked"));Assert.That((string)r["outcome"],Is.EqualTo("error"));Assert.That(recoveries,Is.Zero);
    }
    [Test] public void ExhaustedCompileAttemptsRecoverAndKeepPreviewBlocked() {
        compile=true;ledgerState="failed";var req=Request("composite");JObject r=null;
        for(int i=0;i<5;i++){r=Dispatch(req);req["state"]=r["state"].DeepClone();}
        Assert.That(deployments,Is.EqualTo(5));Assert.That(recoveries,Is.EqualTo(1));Assert.That((string)JObject.Parse((string)r["reply"])["ai_solution"]["widget"]["state"],Is.EqualTo("blocked"));
    }
    [Test] public void CapabilityRevokedDuringRepairPreventsFurtherDeployment() {
        compile=true;ledgerState="failed";var req=Request("composite");var first=Dispatch(req);req["state"]=first["state"].DeepClone();req["capabilities"]["widgets"]=false;
        Assert.Throws<TargetInvocationException>(()=>Dispatch(req));Assert.That(deployments,Is.EqualTo(1));
    }
    [Test] public void MixedReadAndArtifactCannotReachNormalizerOrExecutor() {
        var req=Request();req["response"]=(string)req["response"]+"\n```context-request\n{}\n```";
        Assert.Throws<TargetInvocationException>(()=>Dispatch(req));Assert.That(normalizations+deployments,Is.Zero);
    }
    [Test] public void UnfinishedReasoningCannotCreateAWidget() {
        var req=Request();req["response"]="<think>planning\n"+(string)req["response"];
        Assert.That((string)Dispatch(req)["state"],Is.EqualTo("not_program"));Assert.That(normalizations+deployments,Is.Zero);
    }
}
