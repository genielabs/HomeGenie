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
public class AiGenieCompositeActionsTest {
    Type dispatcher,contract;
    int deployments,recoveries;
    JObject sent;
    [OneTimeSetUp] public void CompileShippedCompositeCoordinator() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        XElement Program(string id)=>XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-base/programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")==id);
        string Class(XElement p,string name)=>CSharpSyntaxTree.ParseText("class Holder {"+(string)p.Element("ScriptContext")+"}").GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text==name).ToFullString();
        var shared=Program("583");string code="using System;using System.IO;using System.Text;using System.Linq;using System.Collections.Generic;using System.Security.Cryptography;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+
            Class(shared,"GenieCapabilities")+Class(shared,"AiGeniePreparation")+Class(shared,"AiGenieProgramActions")+Class(shared,"CompositeSolutionContract")+Class(shared,"AiGenieCompositeActions")+Class(Program("581"),"GenieResponseContract");
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location);
        var compilation=CSharpCompilation.Create("AiComposite"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var result=compilation.Emit(stream);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));var assembly=Assembly.Load(stream.ToArray());dispatcher=assembly.GetType("AiGenieCompositeActions");contract=assembly.GetType("GenieResponseContract");
    }
    [SetUp] public void Reset(){deployments=recoveries=0;sent=null;}
    JObject Request(int attempt=1) {
        var request=JObject.Parse("{'version':1,'requestId':'composite-id','attempt':1,'capabilities':{'automation':true,'widgets':true},'committed':{'kind':'composite','action':'create','program_id':''},'response':{'version':1,'kind':'composite','language':'csharp','explanation':'Simulator','automation':{'action':'create','program_id':'','name':'simulator','setup':'Program.Run();','main':'Program.GoBackground();','context':''},'widget':{'html':'<div>test</div>','css':'','javascript':'class Demo {}'},'binding':{'domain':'HomeAutomation.HomeGenie.Automation','address':'$program','properties':['Sensor.Level']}}}");
        request["attempt"]=attempt;if(attempt>1)request["committedBinding"]=request["response"]["binding"].DeepClone();return request;
    }
    JObject Dispatch(JObject request,string state="completed",bool compile=false,bool recoverySuccess=true,string programId="1001") {
        Func<string,object,object> call=(route,payload)=>{
            if(route=="System.Utility/ActionExecutor/Response.Normalize") {
                try{return contract.GetMethod("Normalize").Invoke(null,new[]{payload});}
                catch(TargetInvocationException ex){return new JObject{["Status"]="Error",["Message"]=ex.InnerException.Message};}
            }
            if(route=="System.Utility/ActionExecutor/Program.Deploy") {
                deployments++;sent=JObject.Parse((string)payload);
                return new JObject{["RequestId"]=(string)sent["requestId"],["Index"]=(int)sent["attempt"],["State"]=state,["Success"]=state=="completed",["Error"]="deployment error",["Response"]=new JObject{["program_id"]=programId,["status"]=compile?"compile_failed":"success",["compiler_errors"]=new JArray("diagnostic")}};
            }
            Assert.That(route,Is.EqualTo("System.Utility/ActionExecutor/Program.Recover"));recoveries++;Assert.That((string)JObject.Parse((string)payload)["requestId"],Is.EqualTo("composite-id"));
            return new JObject{["RequestId"]="composite-id",["State"]=recoverySuccess?"completed":"failed",["Success"]=recoverySuccess,["Error"]="recovery error"};
        };
        return (JObject)dispatcher.GetMethod("Dispatch").Invoke(null,new object[]{request,call});
    }
    [Test] public void SuccessfulProgramProducesTheSameCompositeEnvelopeWithWaitingWidget() {
        var request=Request();string before=request.ToString();var result=Dispatch(request);var solution=JObject.Parse((string)result["reply"])["ai_solution"];
        Assert.That((string)solution["program"]["id"],Is.EqualTo("1001"));Assert.That((string)solution["widget"]["state"],Is.EqualTo("waiting"));Assert.That((string)solution["widget"]["dependsOn"],Is.EqualTo("program"));Assert.That((string)solution["widget"]["binding"]["address"],Is.EqualTo("1001"));Assert.That((string)solution["widget"]["javascript"],Is.EqualTo("class Demo {}"));Assert.That(request.ToString(),Is.EqualTo(before));Assert.That(deployments,Is.EqualTo(1));Assert.That(recoveries,Is.Zero);
    }
    [TestCase("automation")] [TestCase("widgets")]
    public void BothCapabilitiesAreRequiredBeforeAnyDeployment(string key) {
        var request=Request();request["capabilities"][key]=false;Assert.Throws<TargetInvocationException>(()=>Dispatch(request));Assert.That(deployments,Is.Zero);
    }
    [TestCase("binding")] [TestCase("widget")] [TestCase("automation")]
    public void IncompleteCompositeIsRejectedBeforeCreatingAProgram(string field) {
        var request=Request();((JObject)request["response"]).Remove(field);Assert.Throws<TargetInvocationException>(()=>Dispatch(request));Assert.That(deployments,Is.Zero);
    }
    [Test] public void ProgramPlaceholderCannotReferToAnotherDomain() {
        var request=Request();request["response"]["binding"]["domain"]="Other.Domain";Assert.Throws<TargetInvocationException>(()=>Dispatch(request));Assert.That(deployments,Is.Zero);
    }
    [TestCase("domain")] [TestCase("address")] [TestCase("properties")]
    public void CompilerRepairCannotChangeTheWidgetContract(string field) {
        var request=Request(2);request["response"]["binding"][field]=field=="properties"?new JArray("Sensor.Other"):new JValue(field=="domain"?"Other.Domain":"other");
        Assert.Throws<TargetInvocationException>(()=>Dispatch(request));Assert.That(deployments,Is.Zero);
    }
    [Test] public void RepairKeepsFullCompositeFeedbackBindingAndRequestIdentity() {
        var request=Request();var result=Dispatch(request,"failed",true);Assert.That((string)result["outcome"],Is.EqualTo("repair"));Assert.That((string)result["feedback"],Does.Contain("kind=composite").And.Contain("including widget and binding"));Assert.That(JToken.DeepEquals(result["binding"],request["response"]["binding"]),Is.True);
        request["attempt"]=result["nextAttempt"];request["committedBinding"]=result["binding"].DeepClone();Dispatch(request);Assert.That((string)sent["requestId"],Is.EqualTo("composite-id"));Assert.That((int)sent["attempt"],Is.EqualTo(2));Assert.That((string)sent["mode"],Is.EqualTo("create"));Assert.That((string)sent["program_id"],Is.Empty);
    }
    [Test] public void RepairWithoutItsPreviousBindingFailsBeforeDeploying() {
        var request=Request(2);request.Remove("committedBinding");Assert.Throws<TargetInvocationException>(()=>Dispatch(request));Assert.That(deployments,Is.Zero);
    }
    [TestCase(false)] [TestCase(true)]
    public void UnknownOutcomeBlocksWidgetAndNeverRetriesOrRecovers(bool compile) {
        var result=Dispatch(Request(),"unknown",compile);var solution=JObject.Parse((string)result["reply"])["ai_solution"];Assert.That((string)solution["widget"]["state"],Is.EqualTo("blocked"));Assert.That((string)solution["program"]["state"],Is.EqualTo("unknown"));Assert.That(deployments,Is.EqualTo(1));Assert.That(recoveries,Is.Zero);
    }
    [Test] public void FinalCompileFailureBlocksWidgetAndRetainsRecoveryError() {
        var result=Dispatch(Request(5),"failed",true,false);var solution=JObject.Parse((string)result["reply"])["ai_solution"];Assert.That((string)solution["widget"]["state"],Is.EqualTo("blocked"));Assert.That((string)solution["program"]["recoveryError"],Is.EqualTo("recovery error"));Assert.That((int)result["actionCalls"],Is.EqualTo(2));Assert.That(recoveries,Is.EqualTo(1));
    }
    [Test] public void CompletedDeploymentWithoutIdentityNeverExposesAWidget() {
        Assert.Throws<TargetInvocationException>(()=>Dispatch(Request(),programId:""));Assert.That(deployments,Is.EqualTo(1));Assert.That(recoveries,Is.Zero);
    }
}
