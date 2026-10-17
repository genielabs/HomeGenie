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
public class LailamaContinuationBudgetTest {
    Type fixedExecutor, original;
    [OneTimeSetUp] public void CompileDistributedAdapterWithContinuationHarness() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        var node=XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-ml-ai/programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")=="940");
        var tree=CSharpSyntaxTree.ParseText("class Holder {"+(string)node.Element("ScriptContext")+"}").GetRoot();
        var adapter=tree.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text=="LailamaInteractiveExecutor").ToFullString();
        // Deterministic harness for the 0.27.0 continuation accounting, not native inference.
        // Source: LLama/LLamaInteractExecutor.cs PreprocessInputs and PostProcess, v0.27.0.
        // Decode batches are simulated so the observed 2447-input/2048-output failure
        // can be reproduced without loading model weights or changing test-machine RAM.
        string harness=@"
public class LLamaContext {}
public class InteractiveExecutor {
 public class InferStateArgs {public int RemainedTokens;}
 bool initial;
 public InteractiveExecutor(LLamaContext context) {}
 protected virtual Task PreprocessInputs(string text,InferStateArgs args,CancellationToken token) {
  token.ThrowIfCancellationRequested();if(!initial)args.RemainedTokens-=int.Parse(text);return Task.CompletedTask;
 }
 public int[] Turn(int inputTokens,int maxOutput,bool first,CancellationToken token) {
  initial=first;var args=new InferStateArgs{RemainedTokens=maxOutput};
  PreprocessInputs(inputTokens.ToString(),args,token).GetAwaiter().GetResult();
  int consumed=0;
  while(consumed<inputTokens) {consumed+=Math.Min(512,inputTokens-consumed);if(args.RemainedTokens<=0&&maxOutput!=-1)return new[]{consumed,0,args.RemainedTokens};}
  return new[]{consumed,maxOutput==-1?3:args.RemainedTokens,args.RemainedTokens};
 }
}";
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location));
        var compilation=CSharpCompilation.Create("ContinuationBudget"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText("using System;using System.Threading;using System.Threading.Tasks;"+harness+adapter)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var result=compilation.Emit(stream);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));var assembly=Assembly.Load(stream.ToArray());fixedExecutor=assembly.GetType("LailamaInteractiveExecutor");original=assembly.GetType("InteractiveExecutor");
    }
    int[] Turn(Type type,int input,int output,bool first=false,CancellationToken token=default) {
        var executor=Activator.CreateInstance(type,new object[]{null});return (int[])type.GetMethod("Turn").Invoke(executor,new object[]{input,output,first,token});
    }
    [Test] public void ObservedLargeReferenceInputIsFullyConsumedAndRetainsOutputLimit() {
        var broken=Turn(original,2447,2048);Assert.That(broken[0],Is.EqualTo(512));Assert.That(broken[1],Is.Zero);
        var corrected=Turn(fixedExecutor,2447,2048);Assert.That(corrected[0],Is.EqualTo(2447));Assert.That(corrected[1],Is.EqualTo(2048));
    }
    [TestCase(10,64)] [TestCase(2048,2048)] [TestCase(8192,64)]
    public void ContinuationInputDoesNotReduceOrExpandOutputBudget(int input,int output) {var r=Turn(fixedExecutor,input,output);Assert.That(r[0],Is.EqualTo(input));Assert.That(r[1],Is.EqualTo(output));}
    [Test] public void InitialPromptBudgetIsUnchanged() {Assert.That(Turn(fixedExecutor,2447,2048,true),Is.EqualTo(Turn(original,2447,2048,true)));}
    [Test] public void UnlimitedOutputSentinelIsPreserved() {Assert.That(Turn(fixedExecutor,2447,-1)[2],Is.EqualTo(-1));}
    [Test] public void CancelledPreprocessingStillStopsBeforeAnyGeneration() {
        using var c=new CancellationTokenSource();c.Cancel();var ex=Assert.Throws<TargetInvocationException>(()=>Turn(fixedExecutor,2447,2048,token:c.Token));Assert.That(ex.InnerException,Is.InstanceOf<OperationCanceledException>());
    }
}
