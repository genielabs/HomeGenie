using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace HomeGenie.Tests;
[TestFixture] public class GeminiPromptCancellationTest {
    Assembly assembly;
    string Id()=>Guid.NewGuid().ToString("N");
    [OneTimeSetUp] public void CompileShippedLifetimeAndGenerationBoundary() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        XElement Node(string package,string pid)=>XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie",package,"programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")==pid);
        string Class(XElement n,string name)=>CSharpSyntaxTree.ParseText((string)n.Element("ScriptContext")).GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(c=>c.Identifier.Text==name).ToFullString();
        var gemini=Node("homegenie-ml-ai","933");var service=Node("homegenie-base","583");
        var syntax=CSharpSyntaxTree.ParseText("class P{void Run(){"+(string)gemini.Element("ScriptSource")+"}}");
        Assert.That(syntax.GetDiagnostics().Where(d=>d.Severity==DiagnosticSeverity.Error),Is.Empty);
        string code="using System;using System.Linq;using System.Threading;using System.Collections.Generic;"+Class(gemini,"GeminiPromptLifetime")+Class(service,"AiGenieService");
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location));
        var c=CSharpCompilation.Create("PromptLifetime"+Id(),new[]{CSharpSyntaxTree.ParseText(code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var output=new MemoryStream();var result=c.Emit(output);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));assembly=Assembly.Load(output.ToArray());
    }
    object New()=>Activator.CreateInstance(assembly.GetType("GeminiPromptLifetime"));
    (bool ok,object lease,string state) Begin(object life,string id) {var args=new object[]{id,null,null};bool ok=(bool)life.GetType().GetMethod("TryBegin").Invoke(life,args);return(ok,args[1],(string)args[2]);}
    string State(object life,string id,bool cancel=false)=>JObject.FromObject(life.GetType().GetMethod("Status").Invoke(life,new object[]{id,cancel})).Value<string>("state");
    CancellationToken Token(object lease)=>(CancellationToken)lease.GetType().GetProperty("Token").GetValue(lease);
    [Test] public void CancellationKeepsOwnershipUntilTheWorkerActuallyUnwinds() {
        using var life=(IDisposable)New();string id=Id();var first=Begin(life,id);
        Assert.That(State(life,id,true),Is.EqualTo("cancelling"));Assert.That(Token(first.lease).IsCancellationRequested,Is.True);
        Assert.That(Begin(life,Id()).state,Is.EqualTo("busy"));Assert.That(State(life,id),Is.EqualTo("cancelling"));
        ((IDisposable)first.lease).Dispose();Assert.That(State(life,id),Is.EqualTo("cancelled"));
        var next=Begin(life,Id());Assert.That(next.ok,Is.True);((IDisposable)next.lease).Dispose();
    }
    [Test] public void StopArrivingBeforeSubmitFencesThatDelayedRequest() {
        using var life=(IDisposable)New();string id=Id();Assert.That(State(life,id,true),Is.EqualTo("cancelled"));
        Assert.That(Begin(life,id).state,Is.EqualTo("cancelled"));Assert.That(Begin(life,id).ok,Is.False);
    }
    [Test] public void StaleStopCannotCancelANewerRequest() {
        using var life=(IDisposable)New();var active=Begin(life,Id());State(life,Id(),true);
        Assert.That(Token(active.lease).IsCancellationRequested,Is.False);((IDisposable)active.lease).Dispose();
    }
    [Test] public void CancelledLeaseCannotCommitALateCompletedReply() {
        using var life=(IDisposable)New();string id=Id();var active=Begin(life,id);State(life,id,true);bool saved=false;
        var commit=active.lease.GetType().GetMethod("Commit").MakeGenericMethod(typeof(string));
        var ex=Assert.Throws<TargetInvocationException>(()=>commit.Invoke(active.lease,new object[]{new Func<string>(()=>{saved=true;return "late";})}));
        Assert.That(ex.InnerException,Is.TypeOf<OperationCanceledException>());Assert.That(saved,Is.False);((IDisposable)active.lease).Dispose();
    }
    [Test] public void CompletedCommitWinsALateStopWithoutPretendingItWasCancelled() {
        using var life=(IDisposable)New();string id=Id();var active=Begin(life,id);
        active.lease.GetType().GetMethod("Commit").MakeGenericMethod(typeof(string)).Invoke(active.lease,new object[]{new Func<string>(()=>"saved")});
        Assert.That(State(life,id,true),Is.EqualTo("completed"));Assert.That(Token(active.lease).IsCancellationRequested,Is.False);
        ((IDisposable)active.lease).Dispose();Assert.That(Begin(life,id).ok,Is.False);
    }
    [Test] public void ParentCancellationReachesTheProviderAndRejectsItsLateOutput() {
        using var service=(IDisposable)Activator.CreateInstance(assembly.GetType("AiGenieService"));using var parent=new CancellationTokenSource();
        var request=new Dictionary<string,object>{["version"]=1,["requestId"]="test",["owner"]="933",["provider"]="Fake",["messages"]=new object(),["cancellationToken"]=parent.Token};
        var answer=(IDictionary<string,object>)service.GetType().GetMethod("Generate").Invoke(service,new object[]{request,new Func<string,object,object>((_,payload)=> {
            var token=(CancellationToken)((IDictionary<string,object>)payload)["cancellationToken"];parent.Cancel();Assert.That(token.IsCancellationRequested,Is.True);
            return new Dictionary<string,object>{["version"]=1,["requestId"]="test",["state"]="completed",["text"]="late answer"};
        })});
        Assert.That(answer["state"],Is.EqualTo("cancelled"));Assert.That(answer["text"],Is.Null);
    }
    [Test] public void AlreadyCancelledParentNeverCallsTheProvider() {
        using var service=(IDisposable)Activator.CreateInstance(assembly.GetType("AiGenieService"));using var cts=new CancellationTokenSource();cts.Cancel();
        var req=new Dictionary<string,object>{["version"]=1,["requestId"]="test",["owner"]="933",["provider"]="Fake",["messages"]=new object(),["cancellationToken"]=cts.Token};
        var result=(IDictionary<string,object>)service.GetType().GetMethod("Generate").Invoke(service,new object[]{req,new Func<string,object,object>((_,_)=>throw new AssertionException("Must not generate"))});
        Assert.That(result["state"],Is.EqualTo("cancelled"));
    }
}
