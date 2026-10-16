using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace HomeGenie.Tests;
[TestFixture]
public class AiGenieProgramActionsTest {
    Type dispatcher,contract;
    int deployments,recoveries;
    JObject sent;
    [OneTimeSetUp] public void CompileShippedProgramDispatcher() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        XElement Program(string id)=>XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-base/programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")==id);
        string Class(XElement p,string name)=>CSharpSyntaxTree.ParseText("class Holder {"+(string)p.Element("ScriptContext")+"}").GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text==name).ToFullString();
        string code="using System;using System.Linq;using System.IO;using System.Text;using System.Collections.Generic;using System.Security.Cryptography;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+Class(Program("583"),"AiGeniePreparation")+Class(Program("583"),"AiGenieProgramActions")+Class(Program("581"),"GenieResponseContract");
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location);
        var compilation=CSharpCompilation.Create("AiProgramDispatch"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var result=compilation.Emit(stream);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));var assembly=Assembly.Load(stream.ToArray());dispatcher=assembly.GetType("AiGenieProgramActions");contract=assembly.GetType("GenieResponseContract");
    }
    [SetUp] public void Reset(){deployments=recoveries=0;sent=null;}
    JObject Request(string mode="create",string target="",int attempt=1)=>JObject.FromObject(new {
        version=1,requestId="stable-id",attempt,capabilities=new {automation=true},context=new {automationPrograms=true},resources=new {programIds=new[]{target}},
        committed=new {kind="program",action=mode,program_id=target},
        response=new {version=1,kind="program",explanation="Done",language="csharp",automation=new {action=mode,program_id=target,name="test",setup="Program.Run();",main="Program.GoBackground();",context=""}}
    });
    JObject Dispatch(JObject request,string state="completed",bool compile=false,Func<JObject,object> receipt=null,string recoveryState="completed") {
        Func<string,object,object> call=(route,payload)=>{
            if(route=="System.Utility/ActionExecutor/Response.Normalize") {
                try{return contract.GetMethod("Normalize").Invoke(null,new[]{payload});}
                catch(TargetInvocationException ex){return new JObject{["Status"]="Error",["Message"]=ex.InnerException.Message};}
            }
            if(route=="System.Utility/ActionExecutor/Program.Deploy") {
                deployments++;sent=JObject.Parse((string)payload);
                return receipt!=null?receipt(sent):new JObject{["RequestId"]=(string)sent["requestId"],["Index"]=(int)sent["attempt"],["State"]=state,["Success"]=state=="completed",["Error"]=state=="completed"?null:"failed",["Response"]=new JObject{["program_id"]="1001",["status"]=compile?"compile_failed":"success",["compiler_errors"]=new JArray("compile diagnostic")}};
            }
            Assert.That(route,Is.EqualTo("System.Utility/ActionExecutor/Program.Recover"));recoveries++;var recovery=JObject.Parse((string)payload);Assert.That((string)recovery["requestId"],Is.EqualTo("stable-id"));
            return new JObject{["RequestId"]="stable-id",["State"]=recoveryState,["Success"]=recoveryState=="completed"};
        };
        return (JObject)dispatcher.GetMethod("Dispatch").Invoke(null,new object[]{request,call});
    }
    [Test] public void CreationPreservesSourcesIdentityAndLegacyResultCard() {
        var request=Request();string before=request.ToString();var result=Dispatch(request);var reply=JObject.Parse((string)result["reply"]);
        Assert.That((string)sent["requestId"],Is.EqualTo("stable-id"));Assert.That((string)sent["program"]["main"],Is.EqualTo("Program.GoBackground();"));Assert.That((string)reply["automan_deploy"]["program_id"],Is.EqualTo("1001"));Assert.That((string)result["outcome"],Is.EqualTo("completed"));Assert.That(request.ToString(),Is.EqualTo(before));Assert.That(recoveries,Is.Zero);
    }
    [Test] public void UpdateKeepsTheExistingTargetAndRequiresItsReadSources() {
        Dispatch(Request("update","1001"));Assert.That((string)sent["program_id"],Is.EqualTo("1001"));Assert.That((string)sent["mode"],Is.EqualTo("update"));
    }
    [TestCase("capability")] [TestCase("context")] [TestCase("sources")] [TestCase("target")] [TestCase("action")]
    public void PermissionOrCommitFailuresCannotDeploy(string mode) {
        var request=Request("update","1001");
        if(mode=="capability")request["capabilities"]["automation"]=false;
        if(mode=="context")request["context"]["automationPrograms"]=false;
        if(mode=="sources")request["resources"]["programIds"]=new JArray("1002");
        if(mode=="target")request["committed"]["program_id"]="1002";
        if(mode=="action")request["committed"]["action"]="create";
        Assert.Throws<TargetInvocationException>(()=>Dispatch(request));Assert.That(deployments,Is.Zero);Assert.That(recoveries,Is.Zero);
    }
    [Test] public void CompileRepairsKeepIdentityAndStopAtFiveAttempts() {
        for(int attempt=1;attempt<=5;attempt++) {
            var result=Dispatch(Request(attempt:attempt),"failed",true);
            Assert.That((string)sent["requestId"],Is.EqualTo("stable-id"));Assert.That((int)sent["attempt"],Is.EqualTo(attempt));
            if(attempt<5){Assert.That((string)result["outcome"],Is.EqualTo("repair"));Assert.That((int)result["nextAttempt"],Is.EqualTo(attempt+1));Assert.That((string)result["feedback"],Does.Contain("compile diagnostic").And.Contain("Do not create a replacement"));Assert.That(recoveries,Is.Zero);}
            else {Assert.That((string)result["outcome"],Is.EqualTo("error"));Assert.That(recoveries,Is.EqualTo(1));Assert.That((int)result["actionCalls"],Is.EqualTo(2));}
        }
    }
    [TestCase(0)] [TestCase(6)]
    public void AttemptsOutsideTheBudgetCannotDeploy(int attempt) {Assert.Throws<TargetInvocationException>(()=>Dispatch(Request(attempt:attempt)));Assert.That(deployments,Is.Zero);}
    [TestCase(false)] [TestCase(true)]
    public void UnknownOutcomeNeverAutomaticallyRetriesOrRecovers(bool diagnostic) {
        var result=Dispatch(Request(),"unknown",diagnostic);Assert.That((string)result["outcome"],Is.EqualTo("error"));Assert.That(deployments,Is.EqualTo(1));Assert.That(recoveries,Is.Zero);
    }
    [TestCase("null")] [TestCase("identity")] [TestCase("attempt")]
    public void MissingOrUnrelatedReceiptsNeverTriggerRecovery(string mode) {
        Assert.Throws<TargetInvocationException>(()=>Dispatch(Request(),receipt:r=>mode=="null"?null:new JObject{["RequestId"]=mode=="identity"?"other":"stable-id",["Index"]=mode=="attempt"?9:1,["State"]="failed"}));Assert.That(deployments,Is.EqualTo(1));Assert.That(recoveries,Is.Zero);
    }
    [Test] public void FailedDeploymentRecoversOnlyTheSameIdentityAndReportsRecoveryMetrics() {
        var result=Dispatch(Request(),"failed",recoveryState:"failed");Assert.That(recoveries,Is.EqualTo(1));Assert.That((int)result["additionalFailedActions"],Is.EqualTo(1));Assert.That((string)result["outcome"],Is.EqualTo("error"));
    }
}
