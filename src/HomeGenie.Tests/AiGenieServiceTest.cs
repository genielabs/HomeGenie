using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
namespace HomeGenie.Tests;
[TestFixture]
public class AiGenieServiceTest {
    Type type;
    [OneTimeSetUp] public void CompileDistributedService() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        var node=XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-base/programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")=="583");
        string service=CSharpSyntaxTree.ParseText("class Holder {"+(string)node.Element("ScriptContext")+"}").GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>().Single(n=>n.Identifier.Text=="AiGenieService").ToFullString();
        string code="using System;using System.Collections.Generic;using System.Linq;using System.Threading;"+service;
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location));
        var compilation=CSharpCompilation.Create("AiGenieProbe"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var result=compilation.Emit(stream);
        Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));
        type=Assembly.Load(stream.ToArray()).GetType("AiGenieService");
    }
    Dictionary<string,object> Request(string id="r",string owner="933")=>new(){["version"]=1,["requestId"]=id,["owner"]=owner,["provider"]="Fake",["messages"]=new object()};
    Dictionary<string,object> Reply(string state="completed")=>new(){["version"]=1,["requestId"]="r",["state"]=state,["text"]="answer"};
    IDictionary<string,object> Generate(object service,object request,Func<string,object,object> provider)=>(IDictionary<string,object>)type.GetMethod("Generate").Invoke(service,new[]{request,provider});
    IDictionary<string,object> Cancel(object service,string id)=>(IDictionary<string,object>)type.GetMethod("Cancel").Invoke(service,new object[]{new Dictionary<string,object>{["requestId"]=id}});
    [Test] public void NativeObjectsPassWithoutSerializationAndCallerEnvelopeIsNotModified() {
        using var service=(IDisposable)Activator.CreateInstance(type);var request=Request();
        var result=Generate(service,request,(route,payload)=>{
            Assert.That(route,Is.EqualTo("AI.Genies/Fake/Provider.Generate"));
            var data=(IDictionary<string,object>)payload;
            Assert.That(data["messages"],Is.SameAs(request["messages"]));
            Assert.That(data["cancellationToken"],Is.TypeOf<CancellationToken>());return Reply();});
        Assert.That(result["state"],Is.EqualTo("completed"));Assert.That(request.ContainsKey("cancellationToken"),Is.False);
    }
    [TestCase("output_limit")] [TestCase("context_limit")] [TestCase("cancelled")] [TestCase("failed")]
    public void PartialOutputIsNeverReturnedAsActionableText(string state) {
        using var service=(IDisposable)Activator.CreateInstance(type);
        var result=Generate(service,Request(),(r,p)=>Reply(state));Assert.That(result["text"],Is.Null);Assert.That(result["state"],Is.EqualTo(state));
    }
    [Test] public void InvalidIdentityNeverCallsProvider() {
        using var service=(IDisposable)Activator.CreateInstance(type);var request=Request();request["provider"]="Fake/Prompt.Submit";
        Assert.That(Generate(service,request,(r,p)=>throw new AssertionException("Must not dispatch"))["state"],Is.EqualTo("failed"));
    }
    [Test] public void MissingMalformedAndThrownProviderOutcomesFailExplicitly() {
        using var service=(IDisposable)Activator.CreateInstance(type);
        Assert.That(Generate(service,Request(),(r,p)=>null)["state"],Is.EqualTo("failed"));
        var wrong=Reply();wrong["requestId"]="other";
        Assert.That(Generate(service,Request(),(r,p)=>wrong)["state"],Is.EqualTo("failed"));
        var result=Generate(service,Request(),(r,p)=>throw new Exception("secret transport details"));
        Assert.That(result["state"],Is.EqualTo("failed"));Assert.That((string)result["error"],Does.Not.Contain("secret"));
    }
    [Test] public void ReentrantSameOwnerIsBusyButOtherOwnersAreIndependent() {
        using var service=(IDisposable)Activator.CreateInstance(type);
        Generate(service,Request(),(r,p)=>{
            Assert.That(Generate(service,Request("r2"),(a,b)=>Reply())["state"],Is.EqualTo("busy"));
            var second=Reply();second["requestId"]="r2";
            Assert.That(Generate(service,Request("r2","940"),(a,b)=>second)["state"],Is.EqualTo("completed"));return Reply();});
    }
    [Test] public void CancellationSuppressesEvenACompletedProviderResponseAndReleasesOwner() {
        using var service=(IDisposable)Activator.CreateInstance(type);
        var result=Generate(service,Request(),(r,p)=>{Assert.That(Cancel(service,"r")["state"],Is.EqualTo("cancelling"));return Reply();});
        Assert.That(result["state"],Is.EqualTo("cancelled"));Assert.That(result["text"],Is.Null);
        Assert.That(Generate(service,Request(),(r,p)=>Reply())["state"],Is.EqualTo("completed"));
    }
    [Test] public void ShutdownCancelsActiveTokenAndRejectsNewWork() {
        var service=(IDisposable)Activator.CreateInstance(type);
        var result=Generate(service,Request(),(r,p)=>{service.Dispose();Assert.That(((CancellationToken)((IDictionary<string,object>)p)["cancellationToken"]).IsCancellationRequested,Is.True);return Reply();});
        Assert.That(result["state"],Is.EqualTo("cancelled"));
        Assert.That(Generate(service,Request(),(r,p)=>Reply())["state"],Is.EqualTo("unavailable"));
    }
    [Test] public void CancellationFromAnotherThreadInterruptsCooperativeProvider() {
        using var service=(IDisposable)Activator.CreateInstance(type);
        using var entered=new ManualResetEventSlim();
        var task=Task.Run(()=>Generate(service,Request(),(r,p)=>{
            var token=(CancellationToken)((IDictionary<string,object>)p)["cancellationToken"];
            entered.Set();if(!token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10)))throw new Exception("Cancellation was not signalled");
            token.ThrowIfCancellationRequested();return Reply();}));
        Assert.That(entered.Wait(TimeSpan.FromSeconds(10)),Is.True);
        Assert.That(Cancel(service,"r")["state"],Is.EqualTo("cancelling"));
        Assert.That(task.Wait(TimeSpan.FromSeconds(10)),Is.True);
        Assert.That(task.Result["state"],Is.EqualTo("cancelled"));
    }
    [Test] public void UnsupportedContractNeverDispatches() {
        using var service=(IDisposable)Activator.CreateInstance(type);var request=Request();request["version"]=2;
        Assert.That(Generate(service,request,(r,p)=>throw new AssertionException("Must not dispatch"))["state"],Is.EqualTo("failed"));
    }
    [Test] public void DynamicApiDispatchPreservesPayloadAndReturnObjectIdentity() {
        string route="Test.AiGenie/"+Guid.NewGuid().ToString("N")+"/Generate";
        object payload=new object(),result=new object();
        HomeGenie.ProgramDynamicApi.Register(route,input=>{
            Assert.That(((MIG.MigInterfaceCommand)input).Data,Is.SameAs(payload));return result;});
        try {Assert.That(HomeGenie.ProgramDynamicApi.TryApiCall(new MIG.MigInterfaceCommand(route,payload)),Is.SameAs(result));}
        finally {HomeGenie.ProgramDynamicApi.UnRegister(route);}
    }
}
