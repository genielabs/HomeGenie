using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace HomeGenie.Tests;
[TestFixture]
public class AiGenieDeviceActionsTest {
    Type dispatcher,contract;
    int executionCalls,normalizations;
    JObject sent;
    [OneTimeSetUp] public void CompileShippedDispatcher() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        XElement Program(string id)=>XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-base/programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")==id);
        string Class(XElement p,string name)=>CSharpSyntaxTree.ParseText("class Holder {"+(string)p.Element("ScriptContext")+"}").GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text==name).ToFullString();
        string code="using System;using System.Linq;using System.IO;using System.Text;using System.Collections.Generic;using System.Security.Cryptography;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+Class(Program("583"),"AiGeniePreparation")+Class(Program("583"),"AiGenieDeviceActions")+Class(Program("581"),"GenieResponseContract");
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location);
        var compilation=CSharpCompilation.Create("AiDispatch"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var result=compilation.Emit(stream);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));var assembly=Assembly.Load(stream.ToArray());dispatcher=assembly.GetType("AiGenieDeviceActions");contract=assembly.GetType("GenieResponseContract");
    }
    [SetUp] public void Reset(){executionCalls=normalizations=0;sent=null;}
    JObject Request(params string[] commands)=>new(){["version"]=1,["requestId"]="same-request",["capabilities"]=JObject.Parse("{'deviceControl':true,'automation':false,'widgets':false}"),["context"]=JObject.Parse("{'devices':true}"),["response"]=new JObject{["version"]=1,["kind"]="device",["explanation"]="Done",["commands"]=new JArray(commands)}};
    JObject Dispatch(JObject request,Func<string,string,bool> exists=null,Func<JObject,object> execute=null) {
        Func<string,object,object> call=(route,payload)=>{
            if(route=="System.Utility/ActionExecutor/Response.Normalize") {
                normalizations++;try{return contract.GetMethod("Normalize").Invoke(null,new[]{payload});}
                catch(TargetInvocationException ex){return new JObject{["Status"]="Error",["Message"]=ex.InnerException.Message};}
            }
            Assert.That(route,Is.EqualTo("System.Utility/ActionExecutor/Execute"));executionCalls++;sent=JObject.Parse((string)payload);
            return execute!=null?execute(sent):Receipts(sent);
        };
        return (JObject)dispatcher.GetMethod("Dispatch").Invoke(null,new object[]{request,call,exists??((d,a)=>true)});
    }
    JArray Receipts(JObject input,bool success=true) {
        var commands=((string)input["response"]).Split('\n').Skip(1).Where(c=>c!="```");
        return new JArray(commands.Select((c,i)=>new JObject{["Index"]=i,["RequestId"]=(string)input["requestId"],["Command"]=c,["Success"]=success,["State"]=success?"completed":"failed"}));
    }
    [Test] public void DispatchPreservesIdentityResultsAndApiCardWithoutMutatingInput() {
        var request=Request("Test.Lights/1/Control.On","Test.Lights/2/Control.Level/50");string before=request.ToString();var result=Dispatch(request);
        Assert.That(executionCalls,Is.EqualTo(1));Assert.That(normalizations,Is.EqualTo(1));Assert.That((string)sent["requestId"],Is.EqualTo("same-request"));
        Assert.That((string)result["outcome"],Is.EqualTo("completed"));Assert.That(result["results"].Count(),Is.EqualTo(2));
        Assert.That((string)result["reply"],Is.EqualTo("Done\n\n```api-call\nTest.Lights/1/Control.On\nTest.Lights/2/Control.Level/50\n```"));Assert.That(request.ToString(),Is.EqualTo(before));
    }
    [TestCase("capability")] [TestCase("context")]
    public void DisabledCapabilityOrContextCannotReachTheExecutor(string mode) {
        var request=Request("Test.Lights/1/Control.On");request[mode=="capability"?"capabilities":"context"][mode=="capability"?"deviceControl":"devices"]=false;
        Assert.Throws<TargetInvocationException>(()=>Dispatch(request));Assert.That(executionCalls,Is.Zero);Assert.That(normalizations,Is.Zero);
    }
    [Test] public void InvalidLaterCommandRejectsTheWholeBatchBeforeAnyEffect() {
        Assert.Throws<TargetInvocationException>(()=>Dispatch(Request("Test.Lights/1/Control.On","Test.Lights/2/Delete.All")));Assert.That(executionCalls,Is.Zero);
    }
    [Test] public void UnknownLaterModuleRejectsTheWholeBatchBeforeAnyEffect() {
        int targets=0;Assert.Throws<TargetInvocationException>(()=>Dispatch(Request("Test.Lights/1/Control.On","Test.Lights/2/Control.On"),(d,a)=>{targets++;return a=="1";}));
        Assert.That(targets,Is.EqualTo(2));Assert.That(executionCalls,Is.Zero);
    }
    [Test] public void ParameterSetResolvesItsActualModuleAndFailureDoesNotClaimConfirmation() {
        var request=Request("HomeAutomation.HomeGenie/Config/Modules.ParameterSet/Test.Lights/7/Test.Timeout/60");
        var result=Dispatch(request,(d,a)=>{Assert.That(d,Is.EqualTo("Test.Lights"));Assert.That(a,Is.EqualTo("7"));return true;},input=>Receipts(input,false));
        Assert.That((string)result["outcome"],Is.EqualTo("error"));Assert.That((string)result["reply"],Does.StartWith("Configuration was not confirmed."));
    }
    [TestCase("null")] [TestCase("count")] [TestCase("identity")] [TestCase("command")] [TestCase("index")]
    public void UncertainOrMismatchedReceiptsNeverTriggerAutomaticRedispatch(string mode) {
        Assert.Throws<TargetInvocationException>(()=>Dispatch(Request("Test.Lights/1/Control.On"),execute:input=>{
            if(mode=="null")return null;if(mode=="count")return new JArray();var result=Receipts(input);
            result[0][mode=="identity"?"RequestId":mode=="command"?"Command":"Index"]=mode=="index"?new JValue(9):new JValue("other");return result;}));
        Assert.That(executionCalls,Is.EqualTo(1));
    }
    [Test] public void CallerRetryKeepsTheLedgerIdentityRatherThanInventingAnotherRequest() {
        var request=Request("Test.Lights/1/Control.On");Dispatch(request);string id=(string)sent["requestId"];Dispatch(request);Assert.That((string)sent["requestId"],Is.EqualTo(id));
    }
}
