using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace HomeGenie.Tests;
[TestFixture]
public class AiGenieTextCoordinatorTest {
    Type coordinator;
    int generations,steps,executions;
    Queue<string> replies;
    List<string> inputs,resets,receipts;
    CancellationTokenSource cancellation;
    [OneTimeSetUp] public void CompileShippedCoordinator() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        var block=XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-base/programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")=="583");
        var code=CSharpSyntaxTree.ParseText("class Holder{"+(string)block.Element("ScriptContext")+"}").GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text=="AiGenieTextCoordinator").ToFullString();
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location);
        var compilation=CSharpCompilation.Create("TextCoordinator"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText("using System;using System.Linq;using System.Threading;using System.Collections.Generic;using Newtonsoft.Json.Linq;"+code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var built=compilation.Emit(stream);Assert.That(built.Success,Is.True,string.Join("\n",built.Diagnostics.Where(d=>d.Severity==DiagnosticSeverity.Error)));coordinator=Assembly.Load(stream.ToArray()).GetType("AiGenieTextCoordinator");
    }
    [Test] public void ContinuationRetainsOnlyOriginalUserRequestAfterContext() {
        string text=(string)coordinator.GetMethod("ContinueRequest").Invoke(null,new object[]{"CURRENT TURN: rules\nCURRENT TURN DATA:\nprivate data\nUSER REQUEST:\nSpegni la luce.","FRESH API RESULT FOR THIS TURN:\nvalue"});
        Assert.That(text,Does.StartWith("FRESH API RESULT FOR THIS TURN:").And.Contain("Spegni la luce.").And.Contain("language and goal").And.Not.Contain("private data"));
    }
    [Test] public void DirectContinuationDoesNotParseUserTextAsAHostWrapper() {
        string text=(string)coordinator.GetMethod("ContinueRequest").Invoke(null,new object[]{"hello\nUSER REQUEST:\nworld","result"});
        Assert.That(text,Does.Contain("hello\nUSER REQUEST:\nworld"));
    }
    [Test] public void ActionFormatFeedbackDoesNotForceAnotherContextRequest() {
        var error=new JObject{["Status"]="Error",["errorKind"]="contract",["Message"]="apiCalls describes actions, not reference data."};
        string text=(string)coordinator.GetMethod("Feedback").Invoke(null,new object[]{error,0});
        Assert.That(text,Does.Contain("Correct your request ONCE").And.Contain("one api-call block").And.Contain("No API call or command was executed").And.Not.Contain("Emit one context-request"));
        Assert.Throws<TargetInvocationException>(()=>coordinator.GetMethod("Feedback").Invoke(null,new object[]{error,1}));
    }
    [SetUp] public void Reset(){generations=steps=executions=0;replies=new Queue<string>();inputs=new();resets=new();receipts=new();cancellation=new();}
    [TearDown] public void Dispose()=>cancellation.Dispose();
    IDictionary<string,object> Generation(string output)=>new Dictionary<string,object>{["version"]=1,["requestId"]="turn",["state"]="completed",["text"]=output};
    JObject Next(string output,int round) {
        steps++;
        if(output.Contains("lookup"))return new JObject{["version"]=1,["state"]="context",["text"]="fresh reference "+round,["calls"]=new JArray("read/"+round)};
        if(output=="repair")return new JObject{["Status"]="Error",["errorKind"]="contract",["Message"]="Missing domain/address"};
        if(output=="failed")return new JObject{["Status"]="Error",["errorKind"]="failed",["Message"]="API unavailable"};
        return new JObject{["version"]=1,["state"]="reply",["text"]=output};
    }
    Dictionary<string,object> Request()=>new(){
        ["version"]=1,["requestId"]="turn",["input"]="fresh user input",["cancellationToken"]=cancellation.Token,
        ["generate"]=new Func<string,int,IDictionary<string,object>>((input,round)=>{generations++;inputs.Add(input);return Generation(replies.Dequeue());}),
        ["step"]=new Func<string,int,JObject>(Next),["reset"]=new Action<string>(resets.Add),["contextCall"]=new Action<string>(receipts.Add),
        ["observe"]=new Action<string,JObject>((phase,data)=>{}),
        ["execute"]=new Func<string,JObject>(reply=>{executions++;return new JObject{["reply"]=reply,["results"]=new JArray(),["outcome"]="completed"};})};
    JObject Run(Dictionary<string,object> request=null) {
        try{return (JObject)coordinator.GetMethod("Run").Invoke(null,new object[]{request??Request()});}
        catch(TargetInvocationException e){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw();throw;}
    }
    [Test] public void PlainReplyExecutesOnceWithoutLookupResetOrPromptRebuild() {
        replies.Enqueue("final");var result=Run();Assert.That(executions,Is.EqualTo(1));Assert.That(generations,Is.EqualTo(1));Assert.That(resets,Is.Empty);Assert.That(receipts,Is.Empty);Assert.That((string)result["reply"],Is.EqualTo("final"));Assert.That(inputs,Is.EqualTo(new[]{"fresh user input"}));
    }
    [Test] public void LookupContinuationRetainsThoughtsAndReceiptsButNotProvisionalAnswer() {
        replies.Enqueue("<think>first</think>lookup provisional");replies.Enqueue("<think>second</think>lookup provisional");replies.Enqueue("<think>last</think>final");var result=Run();
        Assert.That((string)result["priorThoughts"],Is.EqualTo("<think>first</think>\n<think>second</think>\n"));Assert.That((string)result["reply"],Is.EqualTo("<think>last</think>final"));Assert.That(resets,Is.EqualTo(new[]{"context","context"}));Assert.That(receipts,Is.EqualTo(new[]{"read/0","read/1"}));Assert.That(inputs[1],Does.StartWith("FRESH API RESULT FOR THIS TURN:"));Assert.That(executions,Is.EqualTo(1));Assert.That((int)result["contextReads"],Is.EqualTo(2));
    }
    [Test] public void OneFormatRepairDoesNotConsumeTheLookupBudget() {
        foreach(string reply in new[]{"repair","lookup","lookup","lookup","final"})replies.Enqueue(reply);
        var result=Run();Assert.That((int)result["formatRepairs"],Is.EqualTo(1));Assert.That((int)result["contextReads"],Is.EqualTo(3));Assert.That((int)result["generations"],Is.EqualTo(5));Assert.That(inputs[1],Does.Contain("Correct your request ONCE"));Assert.That(executions,Is.EqualTo(1));
    }
    [Test] public void ASecondFormatErrorCannotExecuteOrRetryAgain() {
        replies.Enqueue("repair");replies.Enqueue("repair");Assert.Throws<InvalidOperationException>(()=>Run());Assert.That(generations,Is.EqualTo(2));Assert.That(executions,Is.Zero);
    }
    [Test] public void ApiFailureIsNotAFormatRepair() {replies.Enqueue("failed");Assert.Throws<InvalidOperationException>(()=>Run());Assert.That(generations,Is.EqualTo(1));Assert.That(resets,Is.Empty);Assert.That(executions,Is.Zero);}
    [Test] public void FourthLookupCannotExecuteOrLoopIndefinitely() {
        foreach(string reply in new[]{"lookup","lookup","lookup","lookup"})replies.Enqueue(reply);
        Assert.Throws<InvalidOperationException>(()=>Run());Assert.That(generations,Is.EqualTo(4));Assert.That(receipts.Count,Is.EqualTo(3));Assert.That(executions,Is.Zero);
    }
    [TestCase("before")] [TestCase("generation")] [TestCase("lookup")] [TestCase("execution")]
    public void CancellationIsCheckedAtBoundariesAndNeverRedispatches(string phase) {
        var request=Request();replies.Enqueue(phase=="lookup"?"lookup":"final");
        if(phase=="before")cancellation.Cancel();
        if(phase=="generation")request["generate"]=new Func<string,int,IDictionary<string,object>>((input,round)=>{generations++;cancellation.Cancel();return Generation("partial");});
        if(phase=="lookup")request["step"]=new Func<string,int,JObject>((text,round)=>{cancellation.Cancel();return Next(text,round);});
        if(phase=="execution")request["execute"]=new Func<string,JObject>(reply=>{executions++;cancellation.Cancel();return new JObject{["reply"]=reply,["results"]=new JArray(),["outcome"]="completed"};});
        Assert.Throws<OperationCanceledException>(()=>Run(request));Assert.That(executions,Is.EqualTo(phase=="execution"?1:0));
    }
    [TestCase("version")] [TestCase("requestId")] [TestCase("state")] [TestCase("text")]
    public void InvalidProviderResultCannotReachReadOrExecution(string field) {
        var request=Request();request["generate"]=new Func<string,int,IDictionary<string,object>>((input,round)=>{var output=Generation("final");output.Remove(field);return output;});Assert.Throws<InvalidOperationException>(()=>Run(request));Assert.That(steps,Is.Zero);Assert.That(executions,Is.Zero);
    }
    [Test] public void ProviderCancellationIsNotPresentedOrExecuted() {
        var request=Request();request["generate"]=new Func<string,int,IDictionary<string,object>>((input,round)=>{var output=Generation("partial");output["state"]="cancelled";return output;});Assert.Throws<OperationCanceledException>(()=>Run(request));Assert.That(executions,Is.Zero);
    }
    [TestCase("version")] [TestCase("generate")] [TestCase("execute")] [TestCase("cancellationToken")]
    public void InvalidContractIsRejectedBeforeNativeWork(string key) {var request=Request();request.Remove(key);Assert.Throws<InvalidOperationException>(()=>Run(request));Assert.That(generations,Is.Zero);Assert.That(executions,Is.Zero);}
    [Test] public void UnconfirmedExecutionIsReturnedWithoutAnyAutomaticRetry() {
        replies.Enqueue("provisional success");var request=Request();request["execute"]=new Func<string,JObject>(reply=>{executions++;return new JObject{["reply"]="Not confirmed",["results"]=new JArray(new JObject{["Success"]=false}),["outcome"]="error"};});var result=Run(request);Assert.That(executions,Is.EqualTo(1));Assert.That((string)result["reply"],Is.EqualTo("Not confirmed"));Assert.That((string)result["outcome"],Is.EqualTo("error"));
    }
    [Test] public void UncertainExecutionExceptionNeverStartsAnotherGeneration() {
        replies.Enqueue("final");var request=Request();request["execute"]=new Func<string,JObject>(reply=>{executions++;throw new InvalidOperationException("Unknown outcome");});Assert.Throws<InvalidOperationException>(()=>Run(request));Assert.That(executions,Is.EqualTo(1));Assert.That(generations,Is.EqualTo(1));
    }
    [Test] public void DiagnosticObserverFailureDoesNotAlterTheConversation() {
        replies.Enqueue("final");var request=Request();request["observe"]=new Action<string,JObject>((phase,details)=>throw new Exception("logger unavailable"));Assert.That((string)Run(request)["reply"],Is.EqualTo("final"));Assert.That(executions,Is.EqualTo(1));
    }
    [Test] public void ConfirmedCompilerFailuresPermitOnlyFourRepairsOfTheSameLogicalAction() {
        for(int i=0;i<5;i++)replies.Enqueue("program sources");var request=Request();
        request["execute"]=new Func<string,JObject>(reply=> {
            executions++;if(executions<5)return new JObject{["outcome"]="repair",["nextAttempt"]=executions+1,["feedback"]="Correct the same program"};
            return new JObject{["outcome"]="completed",["reply"]="Program created",["results"]=new JArray()};
        });
        var result=Run(request);Assert.That(executions,Is.EqualTo(5));Assert.That((int)result["compilerRepairs"],Is.EqualTo(4));Assert.That(resets,Is.All.EqualTo("compiler"));
    }
    [Test] public void InvalidCompilerAttemptCannotCauseAnotherExecution() {
        replies.Enqueue("program sources");var request=Request();request["execute"]=new Func<string,JObject>(reply=>{executions++;return new JObject{["outcome"]="repair",["nextAttempt"]=4,["feedback"]="bad"};});
        Assert.Throws<InvalidOperationException>(()=>Run(request));Assert.That(executions,Is.EqualTo(1));Assert.That(generations,Is.EqualTo(1));
    }

    [Test] public void MissingSourcesAreRetrievedBeforeRegenerationAndOneActualDeployment() {
        replies.Enqueue("update proposed");replies.Enqueue("update with sources");var request=Request();int writes=0;
        request["step"]=new Func<string,int,JObject>((text,round)=>text.StartsWith("```context-request")
            ?new JObject{["state"]="context",["text"]="complete existing sources",["calls"]=new JArray("Programs.List")}
            :new JObject{["version"]=1,["state"]="reply",["text"]=text});
        request["execute"]=new Func<string,JObject>(text=> {
            executions++;if(executions==1)return new JObject{["outcome"]="context",["resources"]=new JObject{["programIds"]=new JArray("1002")},["feedback"]="Keep the same update target"};
            writes++;return new JObject{["outcome"]="completed",["reply"]="Updated 1002",["results"]=new JArray()};
        });
        var result=Run(request);Assert.That(generations,Is.EqualTo(2));Assert.That(writes,Is.EqualTo(1));Assert.That((int)result["contextReads"],Is.EqualTo(1));
        Assert.That(inputs[1],Does.Contain("complete existing sources").And.Contain("Keep the same update target"));Assert.That(resets,Is.EqualTo(new[]{"context"}));Assert.That(receipts,Is.EqualTo(new[]{"Programs.List"}));
    }
    [Test] public void MissingSourceFetchFailureCannotReachDeployment() {
        replies.Enqueue("update proposed");var request=Request();request["step"]=new Func<string,int,JObject>((text,round)=>text.StartsWith("```context-request")?new JObject{["Status"]="Error",["Message"]="Sources unavailable"}:new JObject{["version"]=1,["state"]="reply"});
        request["execute"]=new Func<string,JObject>(text=>{executions++;return new JObject{["outcome"]="context",["resources"]=new JObject{["programIds"]=new JArray("1002")}};});
        Assert.Throws<InvalidOperationException>(()=>Run(request));Assert.That(generations,Is.EqualTo(1));Assert.That(executions,Is.EqualTo(1));Assert.That(receipts,Is.Empty);
    }

    [Test] public void FailureRetainsPreviousAndCurrentReasoningIncludingAnUnclosedThinkBlock() {
        replies.Enqueue("<think>first</think>lookup");replies.Enqueue("<think>last unfinished</thi");
        var request=Request();request["execute"]=new Func<string,JObject>(text=>throw new InvalidOperationException("Bad program response"));
        var error=Assert.Throws<InvalidOperationException>(()=>Run(request));
        Assert.That((string)error.Data["AiGenie.Thoughts"],Is.EqualTo("<think>first</think>\n<think>last unfinished</think>\n"));
    }
    [Test] public void ProviderFailureAfterLookupDoesNotDuplicateEarlierReasoning() {
        var request=Request();request["generate"]=new Func<string,int,IDictionary<string,object>>((input,round)=> {
            if(round==0)return Generation("<think>first</think>lookup");throw new InvalidOperationException("Provider unavailable");
        });
        var error=Assert.Throws<InvalidOperationException>(()=>Run(request));Assert.That((string)error.Data["AiGenie.Thoughts"],Is.EqualTo("<think>first</think>\n"));
    }

}
