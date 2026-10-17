using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
namespace HomeGenie.Tests;
[TestFixture]
public class AiGenieProgramLifecycleTest {
    Type lifecycle;
    [OneTimeSetUp] public void CompileDistributedLifecycle() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        var block=XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-base/programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")=="583");
        var code=CSharpSyntaxTree.ParseText("class Holder{"+(string)block.Element("ScriptContext")+"}").GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text=="AiGenieProgramLifecycle").ToFullString();
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location));
        var compilation=CSharpCompilation.Create("Lifecycle"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText("using System;using System.Collections.Generic;"+code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var result=compilation.Emit(stream);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));lifecycle=Assembly.Load(stream.ToArray()).GetType("AiGenieProgramLifecycle");
    }
    bool Handle(object instance,Action<string> disable,string field="Automan.Program.Request",string value="Quit",string domain="HomeAutomation.HomeGenie.Automation",string id="1002") =>
        (bool)lifecycle.GetMethod("Handle").Invoke(instance,new object[]{domain,id,field,value,"583",disable});
    [Test] public void QuitDisablesOnlyTheEmittingProgramWithoutDeletingIt() {
        string route=null;Assert.That(Handle(Activator.CreateInstance(lifecycle),r=>route=r,value:" qUiT "),Is.True);
        Assert.That(route,Is.EqualTo("HomeAutomation.HomeGenie/Automation/Programs.Disable/1002"));
    }
    [TestCase("Automan.Program.Status","Completed")]
    [TestCase("Automan.Program.Status","Running")]
    [TestCase("Automan.Program.Status","Quit")]
    [TestCase("Automan.Program.Request","Completed")]
    [TestCase("Automan.Program.Message","Quit")]
    [TestCase("Automan.Program.Request",null)]
    public void OtherSignalsHaveNoEffect(string field,string value) {
        Assert.That(Handle(Activator.CreateInstance(lifecycle),_=>Assert.Fail("Unexpected action"),field,value),Is.False);
    }
    [TestCase("HomeAutomation.PhilipsHue","3")]
    [TestCase("HomeAutomation.HomeGenie.Automation","583")]
    [TestCase("HomeAutomation.HomeGenie.Automation","1002/Programs.Delete")]
    public void UnrelatedOrInvalidTargetsHaveNoEffect(string domain,string id) {
        Assert.That(Handle(Activator.CreateInstance(lifecycle),_=>Assert.Fail("Unexpected action"),domain:domain,id:id),Is.False);
    }
    [Test] public void PendingDuplicateIsIgnoredAndFinishedProgramCanRequestQuitAgain() {
        var instance=Activator.CreateInstance(lifecycle);using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
        int calls=0;var pending=Task.Run(()=>Handle(instance,_=>{Interlocked.Increment(ref calls);entered.Set();Assert.That(release.Wait(5000),Is.True);}));
        try {Assert.That(entered.Wait(5000),Is.True);Assert.That(Handle(instance,_=>Interlocked.Increment(ref calls)),Is.False);}
        finally {release.Set();}
        Assert.That(pending.GetAwaiter().GetResult(),Is.True);Assert.That(calls,Is.EqualTo(1));
        Assert.That(Handle(instance,_=>calls++),Is.True);Assert.That(calls,Is.EqualTo(2));
    }
    [Test] public void StoppedServiceIgnoresQueuedRequests() {
        var instance=Activator.CreateInstance(lifecycle);lifecycle.GetMethod("Stop").Invoke(instance,null);
        Assert.That(Handle(instance,_=>Assert.Fail("Unexpected action")),Is.False);
    }
    [Test] public void DisableFailureIsNotSilentlyReportedAsSuccessAndReleasesPendingEntry() {
        var instance=Activator.CreateInstance(lifecycle);
        Assert.Throws<TargetInvocationException>(()=>Handle(instance,_=>throw new InvalidOperationException("unavailable")));
        Assert.That(Handle(instance,_=>{}),Is.True);
    }
}
