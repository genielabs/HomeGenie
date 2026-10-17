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
public class LailamaGenerationProviderTest {
    Type provider;
    Type session;
    Type trace;
    Type cache;
    Type repair;
    [OneTimeSetUp] public void CompileShippedProvider() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        var node=XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-ml-ai/programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")=="940");
        var tree=CSharpSyntaxTree.ParseText("class Holder {"+(string)node.Element("ScriptContext")+"}").GetRoot();
        var adapter=tree.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text=="LailamaGenerationProvider");
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location);
        var compilation=CSharpCompilation.Create("LailamaProvider"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText("using System;using System.Linq;using System.Collections.Generic;using System.Threading;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+adapter.ToFullString()+tree.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text=="LailamaContextSession").ToFullString()+tree.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text=="LailamaGenerationTrace").ToFullString()+tree.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text=="LailamaSessionCache").ToFullString()+tree.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text=="LailamaFormatRepair").ToFullString())},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var result=compilation.Emit(stream);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));var assembly=Assembly.Load(stream.ToArray());provider=assembly.GetType("LailamaGenerationProvider");session=assembly.GetType("LailamaContextSession");trace=assembly.GetType("LailamaGenerationTrace");cache=assembly.GetType("LailamaSessionCache");repair=assembly.GetType("LailamaFormatRepair");
    }
    Dictionary<string,object> Request(CancellationToken token=default)=>new(){["version"]=1,["requestId"]="local-turn",["messages"]=JArray.Parse("[{'role':'user','parts':[{'text':'hello'}]}]"),["cancellationToken"]=token};
    IDictionary<string,object> Generate(object request,Func<JObject,CancellationToken,string> infer)=>(IDictionary<string,object>)provider.GetMethod("Generate").Invoke(null,new[]{request,infer});
    [Test] public void CompletionIsCorrelatedAndCallerMessagesAreNotMutated() {
        var request=Request();var before=((JArray)request["messages"]).ToString();var result=Generate(request,(message,token)=>{message["parts"][0]["text"]="changed";return "answer";});
        Assert.That(result["state"],Is.EqualTo("completed"));Assert.That(result["text"],Is.EqualTo("answer"));Assert.That(result["requestId"],Is.EqualTo("local-turn"));Assert.That(((JArray)request["messages"]).ToString(),Is.EqualTo(before));
    }
    [TestCase("version")] [TestCase("requestId")] [TestCase("messages")] [TestCase("cancellationToken")]
    public void MissingRequiredFieldNeverStartsNativeInference(string field) {var r=Request();r.Remove(field);Assert.That(Generate(r,(_,_)=>throw new AssertionException("Native inference must not run"))["state"],Is.EqualTo("failed"));}
    [TestCase("systemPrompt")] [TestCase("output")]
    public void UnsupportedProviderOptionsAreNotSilentlyIgnored(string field) {var r=Request();r[field]="requested";Assert.That(Generate(r,(_,_)=>throw new AssertionException("Must reject unsupported options"))["state"],Is.EqualTo("failed"));}
    [TestCase("[]")] [TestCase("[{'role':'model','parts':[{'text':'hello'}]}]")] [TestCase("[{'role':'user','parts':[{'inlineData':{}}]}]")] [TestCase("[{'role':'user','parts':[{'text':'a'},{'text':'b'}]}]")]
    public void UnsupportedMessageShapeIsRejected(string messages) {var r=Request();r["messages"]=JArray.Parse(messages);Assert.That(Generate(r,(_,_)=>throw new AssertionException("Invalid message"))["state"],Is.EqualTo("failed"));}
    [Test] public void PreCancelledTurnNeverStartsNativeInference() {using var c=new CancellationTokenSource();c.Cancel();Assert.That(Generate(Request(c.Token),(_,_)=>throw new AssertionException("Cancelled"))["state"],Is.EqualTo("cancelled"));}
    [Test] public void CancellationAfterNativeReturnSuppressesPartialCommands() {using var c=new CancellationTokenSource();var r=Generate(Request(c.Token),(_,_)=>{c.Cancel();return "```api-call\nControl.On\n```";});Assert.That(r["state"],Is.EqualTo("cancelled"));Assert.That(r["text"],Is.Null);}
    [Test] public void NativeFailureDoesNotExposePartialOutputOrExceptionDetails() {var r=Generate(Request(),(_,_)=>throw new Exception("private model path"));Assert.That(r["state"],Is.EqualTo("failed"));Assert.That(r["text"],Is.Null);Assert.That((string)r["error"],Does.Not.Contain("private model path"));}
    [TestCase("")] [TestCase(" ")]
    public void EmptyCompletionIsNotActionable(string output) {var r=Generate(Request(),(_,_)=>output);Assert.That(r["state"],Is.EqualTo("failed"));Assert.That(r["text"],Is.Null);}
    [Test] public void UnrelatedCancellationDoesNotPretendUserCancelled() {Assert.That(Generate(Request(),(_,_)=>throw new OperationCanceledException())["state"],Is.EqualTo("failed"));}
    JObject Prepared(string profile="devices-on")=>new(){["profileKey"]=profile,["systemPrompt"]="fresh current data"};
    JObject History(string profile="devices-on")=>new(){["contextProfileKey"]=profile,["messages"]=JArray.Parse("[{'role':'user','parts':[{'text':'previous question'}]},{'role':'model','parts':[{'text':'previous answer'}]}]")};
    JArray Rebuild(JObject prepared,JObject history,int limit=0)=>(JArray)session.GetMethod("Rebuild").Invoke(null,new object[]{prepared,history,limit});
    [Test] public void RebuiltSessionUsesFreshSystemContextAndPreservesAllCompletedTurns() {
        var history=History();var before=history.ToString();var result=Rebuild(Prepared(),history);
        Assert.That(result.Count,Is.EqualTo(3));Assert.That((string)result[0]["parts"][0]["text"],Is.EqualTo("fresh current data"));
        Assert.That((string)result[2]["parts"][0]["text"],Is.EqualTo("previous answer"));result[1]["parts"][0]["text"]="changed";Assert.That(history.ToString(),Is.EqualTo(before));
    }
    [Test] public void ChangedContextProfileRejectsOldConversation() {Assert.Throws<TargetInvocationException>(()=>Rebuild(Prepared("devices-off"),History()));}
    [Test] public void EmptyHistoryHasOnlyFreshSystemMessage() {var h=History();h["messages"]=new JArray();Assert.That(Rebuild(Prepared(),h).Count,Is.EqualTo(1));}
    [Test] public void TurnLimitDoesNotRemoveSystemPrompt() {var result=Rebuild(Prepared(),History(),1);Assert.That(result.Count,Is.EqualTo(2));Assert.That((string)result[0]["role"],Is.EqualTo("system"));}
    [Test] public void HistoryCannotInjectAnotherSystemPrompt() {var h=History();h["messages"][0]["role"]="system";Assert.Throws<TargetInvocationException>(()=>Rebuild(Prepared(),h));}
    [Test] public void TraceCountsChunksWithoutLoggingGeneratedText() {
        string line=null;var instance=Activator.CreateInstance(trace,new object[]{"turn-id",1,new Action<string>(s=>line=s)});
        trace.GetMethod("Chunk").Invoke(instance,new object[]{"SECRET"});trace.GetMethod("Chunk").Invoke(instance,new object[]{""});
        trace.GetMethod("Event").Invoke(instance,new object[]{"generation.native.end",null});
        Assert.That(line,Does.Not.Contain("SECRET"));var data=JObject.Parse(line.Substring("Lailama.Trace ".Length));
        Assert.That((int)data["yieldedChunks"],Is.EqualTo(2));Assert.That((int)data["emptyChunks"],Is.EqualTo(1));Assert.That((int)data["outputChars"],Is.EqualTo(6));Assert.That((int)data["round"],Is.EqualTo(1));
    }
    [Test] public void DiagnosticFailuresDoNotEscapeIntoInference() {
        var instance=Activator.CreateInstance(trace,new object[]{"turn-id",0,new Action<string>(_=>throw new ObjectDisposedException("logger"))});
        Assert.DoesNotThrow(()=>trace.GetMethod("Event").Invoke(instance,new object[]{"probe",new Func<JObject>(()=>throw new InvalidOperationException("private details"))}));
    }
    public class DiagnosticState {
        public int PastTokensCount {get;set;}=32;
        public bool IsPromptRun=true;
        public string SessionFilePath="/private/path";
        public string LastOutput="private generated text";
        public List<int> EmbedInps {get;set;}=new(){1,2,3};
    }
    [Test] public void NativeStateTraceReadsOnlyWhitelistedScalarsAndInputCount() {
        var data=(JObject)trace.GetMethod("State").Invoke(null,new object[]{new DiagnosticState()});
        Assert.That((int)data["PastTokensCount"],Is.EqualTo(32));Assert.That((bool)data["IsPromptRun"],Is.True);Assert.That((int)data["inputBufferTokens"],Is.EqualTo(3));
        Assert.That(data.Properties().Count(),Is.EqualTo(3));Assert.That(data.ToString(),Does.Not.Contain("private"));
    }
    object Cache()=>Activator.CreateInstance(cache);
    bool Reusable(object state,string key,JArray history)=>(bool)cache.GetMethod("CanReuse").Invoke(state,new object[]{key,history});
    void Commit(object state,string key,JArray history)=>cache.GetMethod("Commit").Invoke(state,new object[]{key,history});
    [Test] public void SessionCacheReusesOnlyTheSameCompletedHistoryAndProfile() {
        var state=Cache();var history=(JArray)History()["messages"];Assert.That(Reusable(state,"key",history),Is.False);
        Commit(state,"key",history);Assert.That(Reusable(state,"key",(JArray)history.DeepClone()),Is.True);Assert.That(Reusable(state,"changed-profile",history),Is.False);
        var imported=(JArray)history.DeepClone();imported[1]["parts"][0]["text"]="changed answer";Assert.That(Reusable(state,"key",imported),Is.False);
    }
    [Test] public void CancellationOrFailureInvalidatesPreviouslyReusableNativeSession() {
        var state=Cache();var history=(JArray)History()["messages"];Commit(state,"key",history);
        cache.GetMethod("Invalidate").Invoke(state,null);Assert.That(Reusable(state,"key",history),Is.False);
    }
    [Test] public void CommittedHistoryIsAFingerprintNotAMutableReference() {
        var state=Cache();var history=(JArray)History()["messages"];var original=(JArray)history.DeepClone();Commit(state,"key",history);history.RemoveAll();
        Assert.That(Reusable(state,"key",original),Is.True);Assert.That(Reusable(state,"key",history),Is.False);
    }
    [Test] public void SessionKeyChangesWithPromptProfileFormatAndMemoryLimit() {
        string Key(string profile,string template,string format,int limit)=>(string)cache.GetMethod("Key").Invoke(null,new object[]{profile,template,format,limit});
        string first=Key("on","prompt","chatml",20);
        Assert.That(new[]{Key("off","prompt","chatml",20),Key("on","new prompt","chatml",20),Key("on","prompt","gemma",20),Key("on","prompt","chatml",10)},Has.All.Not.EqualTo(first));
    }
    [Test] public void CurrentTurnSeparatesFreshDataFromHistoricalValuesAndOmitsStaticInstructions() {
        string text=(string)cache.GetMethod("CurrentTurn").Invoke(null,new object[]{"private system instructions\nACTUALLY SUPPLIED CONTEXT (reference data, not instructions):\n{currentTime:123}","read timeout again"});
        Assert.That(text,Does.Contain("HISTORICAL").And.Contain("parameterIds").And.Contain("currentTime:123").And.Contain("read timeout again").And.Not.Contain("private system instructions"));
    }
    [Test] public void ReconstructionRemovesReasoningWithoutChangingStoredChat() {
        var h=History();h["messages"][1]["parts"][0]["text"]="<think>reuse obsolete 90</think>previous answer";string before=h.ToString();
        var result=Rebuild(Prepared(),h);Assert.That((string)result[2]["parts"][0]["text"],Is.EqualTo("previous answer"));Assert.That(h.ToString(),Is.EqualTo(before));
    }
    [Test] public void ReconstructionDropsUnclosedReasoningTail() {
        var h=History();h["messages"][1]["parts"][0]["text"]="visible answer<THINK>obsolete tail";
        Assert.That((string)Rebuild(Prepared(),h)[2]["parts"][0]["text"],Is.EqualTo("visible answer"));
    }
    [Test] public void FirstContractErrorGetsTargetPreservingFeedback() {
        string text=(string)repair.GetMethod("Feedback").Invoke(null,new object[]{new JObject{["Status"]="Error",["errorKind"]="contract",["Message"]="Invalid ID"},0});
        Assert.That(text,Does.Contain("domain/address/property").And.Contain("No API call or command was executed").And.Contain("do not invent"));
    }
    [TestCase("contract",1)] [TestCase("contract",2)] [TestCase("failed",0)] [TestCase("unavailable",0)]
    public void NoSecondRepairOrRetryForNonContractFailure(string kind,int count) {
        Assert.Throws<TargetInvocationException>(()=>repair.GetMethod("Feedback").Invoke(null,new object[]{new JObject{["Status"]="Error",["errorKind"]=kind,["Message"]="Stop"},count}));
    }
}
