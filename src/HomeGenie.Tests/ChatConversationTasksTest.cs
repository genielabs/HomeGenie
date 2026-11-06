using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace HomeGenie.Tests;

[TestFixture]
public class ChatConversationTasksTest {
    Assembly assembly;
    string folder;

    [OneTimeSetUp] public void CompileDistributedConversationService() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln"))) root=root.Parent;
        var program=XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-base/programs.xml"))
            .Root.Elements().Single(n=>(string)n.Element("Address")=="577");
        var tree=CSharpSyntaxTree.ParseText("class Holder {void Run(){"+(string)program.Element("ScriptSource")+"}}").GetRoot();
        string Section(string command)=>string.Join("\n",tree.DescendantNodes().OfType<SwitchSectionSyntax>()
            .Single(n=>n.Labels.Any(l=>l.ToString()=="case \""+command+"\":" )).Statements);
        string code="using System;using System.IO;using System.Linq;using System.Text;using System.Collections.Generic;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+
            (string)program.Element("ScriptContext")+@"
public enum Status {Error}
public class ResponseStatus {public Status Status;public string Message;public ResponseStatus(Status status,string message){Status=status;Message=message;}}
public class Probe {
    string dataFolder,storeId=""940"";
    public Probe(string folder){dataFolder=folder;HistoryStore.Stores.Clear();}
    public object Call(string command,JObject data)=>ConversationTasks.Handle(dataFolder,storeId,command,data);
    public object Clear(){lock(HistoryStore.SyncRoot){var currentHistory=HistoryStore.GetStore(dataFolder,storeId);"+Section("Clear")+@"}}
    public object Import(JObject data){lock(HistoryStore.SyncRoot){var currentHistory=HistoryStore.GetStore(dataFolder,storeId);"+Section("Import")+@"}}
    public void Reload(){HistoryStore.Stores.Clear();}
    public object Context()=>HistoryStore.Context(HistoryStore.GetStore(dataFolder,storeId),null);
}";
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
            .Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location))
            .Append(typeof(JsonConvert).Assembly.Location).Append(typeof(SixLabors.ImageSharp.Image).Assembly.Location).Distinct();
        var compilation=CSharpCompilation.Create("ConversationTasks"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},
            paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var output=new MemoryStream();var result=compilation.Emit(output);
        Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));assembly=Assembly.Load(output.ToArray());
    }
    [SetUp] public void Setup(){folder=Path.Combine(Path.GetTempPath(),"hg-conversation-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);}
    [TearDown] public void Cleanup(){Directory.Delete(folder,true);}
    object Probe()=>Activator.CreateInstance(assembly.GetType("Probe"),folder);
    object Invoke(object probe,string method,params object[] args)=>probe.GetType().GetMethod(method).Invoke(probe,args);
    JObject Call(object probe,string command,JObject input=null)=>JObject.FromObject(Invoke(probe,"Call",command,input??new JObject()));
    JObject Snapshot(object probe)=>Call(probe,"Conversation.Get");
    JObject Create(object probe,JObject session,string id=null)=>Call(probe,"Task.Create",new JObject {
        ["conversationId"]=session["conversationId"],["taskId"]=id??Guid.NewGuid().ToString("N"),["title"]="Create a dashboard" });
    JObject UpdateRequest(JObject created)=>new JObject { ["conversationId"]=created["conversationId"],
        ["taskId"]=created["message"]["metadata"]["agenticTask"]["id"],["expectedRevision"]=created["message"]["metadata"]["agenticTask"]["revision"], ["state"]="running" };
    string FilePath=>Path.Combine(folder,"history_940.json");

    [Test] public void EmptyConversationIdentitySurvivesRestartAndReadDoesNotAdvanceRevision() {
        var p=Probe();var first=Snapshot(p);Invoke(p,"Reload");var second=Snapshot(p);
        Assert.That(Guid.TryParseExact((string)first["conversationId"],"N",out _),Is.True);
        Assert.That(JToken.DeepEquals(first,second),Is.True);
        Assert.That((int)first["revision"],Is.EqualTo(1));
    }
    [Test] public void LegacyArrayMigratesWithoutLosingTextOrWidgetMetadata() {
        var old=JArray.Parse("[{'role':'model','parts':[{'text':'widget'}],'metadata':{'savedWidget':{'id':'custom/test'}}}]");
        File.WriteAllText(FilePath,old.ToString());var p=Probe();var snapshot=Snapshot(p);Invoke(p,"Reload");
        Assert.That((string)Snapshot(p)["messages"][0]["parts"][0]["text"],Is.EqualTo("widget"));
        Assert.That((string)snapshot["messages"][0]["metadata"]["savedWidget"]["id"],Is.EqualTo("custom/test"));
        Assert.That((string)snapshot["messages"][0]["id"],Is.Not.Empty);
        Assert.That(JToken.Parse(File.ReadAllText(FilePath)),Is.TypeOf<JObject>());
    }
    [Test] public void TaskAndMessagesSurviveReloadWithStableIdentifiers() {
        var p=Probe();var created=Create(p,Snapshot(p));var update=UpdateRequest(created);update["message"]="Widget saved";
        var updated=Call(p,"Task.Update",update);Invoke(p,"Reload");var snapshot=Snapshot(p);
        Assert.That((string)updated["Status"],Is.EqualTo("Ok"));
        Assert.That((int)snapshot["revision"],Is.EqualTo(4));
        Assert.That((string)snapshot["messages"][0]["metadata"]["agenticTask"]["state"],Is.EqualTo("interrupted"));
        Assert.That((string)snapshot["messages"][0]["id"],Is.EqualTo((string)created["message"]["id"]));
        Assert.That((string)snapshot["messages"][1]["parts"][0]["text"],Is.EqualTo("Widget saved"));
        Assert.That((string)snapshot["messages"][1]["metadata"]["agenticTaskMessage"]["taskId"],Is.EqualTo((string)update["taskId"]));
    }
    [Test] public void RepeatingCreateIsIdempotentEvenAfterTaskProgress() {
        var p=Probe();var session=Snapshot(p);string id=Guid.NewGuid().ToString("N");var created=Create(p,session,id);
        Call(p,"Task.Update",UpdateRequest(created));var receipt=Create(p,session,id);
        Assert.That((string)receipt["message"]["metadata"]["agenticTask"]["state"],Is.EqualTo("running"));
        Assert.That(((JArray)Snapshot(p)["messages"]).Count,Is.EqualTo(1));
        Assert.That((int)receipt["revision"],Is.EqualTo(3));
    }
    [Test] public void ConcurrentUpdatesAcceptOneRevisionAndRejectTheOther() {
        var p=Probe();var created=Create(p,Snapshot(p));var request=UpdateRequest(created);
        var results=new JObject[2];Parallel.For(0,2,i=>results[i]=Call(p,"Task.Update",(JObject)request.DeepClone()));
        Assert.That(results.Count(r=>(string)r["Status"]=="Ok"),Is.EqualTo(1));
        Assert.That(results.Count(r=>(string)r["Status"]=="Error"),Is.EqualTo(1));
        Assert.That((int)Snapshot(p)["revision"],Is.EqualTo(3));
    }
    [Test] public void ClearInvalidatesLateRunnerWritesEvenWhenHistoryWasEmpty() {
        var p=Probe();var before=Snapshot(p);Invoke(p,"Clear");var after=Snapshot(p);
        Assert.That((string)after["conversationId"],Is.Not.EqualTo((string)before["conversationId"]));
        Assert.That((string)Create(p,before)["Status"],Is.EqualTo("Error"));
        Assert.That(((JArray)Snapshot(p)["messages"]).Count,Is.Zero);
    }
    [Test] public void ImportStartsANewConversationAndPreservesImportedTaskOutcomes() {
        var p=Probe();var before=Snapshot(p);Create(p,before);var rows=Snapshot(p)["messages"];
        Invoke(p,"Import",new JObject{["Data"]=rows.DeepClone()});
        var after=Snapshot(p);Assert.That((string)after["conversationId"],Is.Not.EqualTo((string)before["conversationId"]));
        Assert.That((string)after["messages"][0]["id"],Is.EqualTo((string)rows[0]["id"]));
        Assert.That((bool)after["messages"][0]["metadata"]["agenticTask"]["readOnly"],Is.True);
        Assert.That((string)after["messages"][0]["metadata"]["agenticTask"]["state"],Is.EqualTo("interrupted"));
        Assert.That((string)Create(p,before)["Status"],Is.EqualTo("Error"));
    }
    [Test] public void FailedSavePreservesMemoryRevisionAndLeavesNoStagingFiles() {
        var p=Probe();var created=Create(p,Snapshot(p));var before=Snapshot(p);
        File.Delete(FilePath);Directory.CreateDirectory(FilePath);
        Assert.That((string)Call(p,"Task.Update",UpdateRequest(created))["Status"],Is.EqualTo("Error"));
        Assert.That(JToken.DeepEquals(before,Snapshot(p)),Is.True);
        Assert.That(Directory.GetFiles(folder,"*.tmp"),Is.Empty);
    }
    [TestCase(false)] [TestCase(true)] public void FailedClearOrImportKeepsThePreviousConversationIdentity(bool import) {
        var p=Probe();Create(p,Snapshot(p));var before=Snapshot(p);
        File.Delete(FilePath);Directory.CreateDirectory(FilePath);
        var receipt=JObject.FromObject(import?Invoke(p,"Import",new JObject{["Data"]=new JArray()}):Invoke(p,"Clear"));
        Assert.That((string)receipt["Message"],Does.Contain("Could not"));
        Assert.That(JToken.DeepEquals(before,Snapshot(p)),Is.True);
    }
    [Test] public void MutatingAReceiptDoesNotMutateCanonicalHistory() {
        var p=Probe();var created=Create(p,Snapshot(p));created["message"]["metadata"]["agenticTask"]["title"]="changed outside storage";
        Assert.That((string)Snapshot(p)["messages"][0]["metadata"]["agenticTask"]["title"],Is.EqualTo("Create a dashboard"));
    }
    [Test] public void TaskOperationsAreUpdatedInPlaceAndCannotCompleteWithFailedWork() {
        var p=Probe();var created=Create(p,Snapshot(p));var request=UpdateRequest(created);
        string id=Guid.NewGuid().ToString("N");request["operation"]=new JObject { ["id"]=id,["title"]="Compile program",["state"]="running" };
        var running=Call(p,"Task.Update",request);request=UpdateRequest(running);
        request["operation"]=new JObject { ["id"]=id,["title"]="Compile program",["state"]="failed",["details"]="Syntax error" };
        var failed=Call(p,"Task.Update",request);request=UpdateRequest(failed);request["state"]="completed";
        Assert.That((string)Call(p,"Task.Update",request)["Status"],Is.EqualTo("Error"));
        Assert.That(((JArray)failed["message"]["metadata"]["agenticTask"]["operations"]).Count,Is.EqualTo(1));
    }
    [Test] public void FinishedTaskCannotBeRunAgainThroughUpdate() {
        var p=Probe();var created=Create(p,Snapshot(p));var request=UpdateRequest(created);request["state"]="cancelled";
        var stopped=Call(p,"Task.Update",request);
        Assert.That((string)Call(p,"Task.Update",UpdateRequest(stopped))["Status"],Is.EqualTo("Error"));
    }
    [Test] public void TaskTelemetryDoesNotLeakIntoExistingModelContext() {
        var p=Probe();var created=Create(p,Snapshot(p));var request=UpdateRequest(created);request["message"]="Internal progress";Call(p,"Task.Update",request);
        Assert.That((JArray)Invoke(p,"Context"),Is.Empty);
    }
    [TestCase("taskId")] [TestCase("conversationId")] [TestCase("expectedRevision")]
    public void InvalidIdentifiersOrRevisionsNeverChangeHistory(string key) {
        var p=Probe();var created=Create(p,Snapshot(p));var before=Snapshot(p);var request=UpdateRequest(created);request[key]="invalid";
        Assert.That((string)Call(p,"Task.Update",request)["Status"],Is.EqualTo("Error"));
        Assert.That(JToken.DeepEquals(before,Snapshot(p)),Is.True);
    }
    [Test] public void CorruptHistoryIsNotOverwrittenAsAnEmptyConversation() {
        File.WriteAllText(FilePath,"not JSON");var p=Probe();Assert.Throws<TargetInvocationException>(()=>Snapshot(p));
        Assert.That(File.ReadAllText(FilePath),Is.EqualTo("not JSON"));
    }
    [Test] public void CancellationIsIdempotentAndOnlyTheRunnerAcknowledgesIt() {
        var p=Probe();var created=Create(p,Snapshot(p));var running=Call(p,"Task.Update",UpdateRequest(created));
        var request=new JObject{["conversationId"]=created["conversationId"],["taskId"]=created["message"]["metadata"]["agenticTask"]["id"]};
        var cancelled=Call(p,"Task.Cancel",request);
        Assert.That((bool)cancelled["message"]["metadata"]["agenticTask"]["cancelRequested"],Is.True);
        Assert.That((string)cancelled["message"]["metadata"]["agenticTask"]["state"],Is.EqualTo("running"));
        Assert.That(JToken.DeepEquals(cancelled,Call(p,"Task.Cancel",request)),Is.True);
        var update=UpdateRequest(cancelled);update["state"]="completed";
        Assert.That((string)Call(p,"Task.Update",update)["Status"],Is.EqualTo("Error"));
        update["state"]="cancelled";var stopped=Call(p,"Task.Update",update);
        Assert.That((string)stopped["message"]["metadata"]["agenticTask"]["state"],Is.EqualTo("cancelled"));
    }
    [Test] public void OldRunnerRevisionCannotOverwriteACancellationRequest() {
        var p=Probe();var created=Create(p,Snapshot(p));var stale=UpdateRequest(created);
        Call(p,"Task.Cancel",new JObject{["conversationId"]=created["conversationId"],["taskId"]=stale["taskId"]});
        Assert.That((string)Call(p,"Task.Update",stale)["Status"],Is.EqualTo("Error"));
    }
    [Test] public void CancellationFailureDoesNotChangeTheTaskOrRevision() {
        var p=Probe();var created=Create(p,Snapshot(p));var before=Snapshot(p);
        File.Delete(FilePath);Directory.CreateDirectory(FilePath);
        var result=Call(p,"Task.Cancel",new JObject{["conversationId"]=created["conversationId"],["taskId"]=created["message"]["metadata"]["agenticTask"]["id"]});
        Assert.That((string)result["Status"],Is.EqualTo("Error"));Assert.That(JToken.DeepEquals(before,Snapshot(p)),Is.True);
    }
    [Test] public void RestartInterruptsActiveWorkOnceButPreservesCompletedOperations() {
        var p=Probe();var created=Create(p,Snapshot(p));var request=UpdateRequest(created);string operation=Guid.NewGuid().ToString("N");
        request["operation"]=new JObject{["id"]=operation,["title"]="Save widget",["state"]="running"};
        var running=Call(p,"Task.Update",request);request=UpdateRequest(running);
        request["operation"]=new JObject{["id"]=operation,["title"]="Save widget",["state"]="completed"};
        Call(p,"Task.Update",request);Invoke(p,"Reload");var after=Snapshot(p);var task=after["messages"][0]["metadata"]["agenticTask"];
        Assert.That((string)task["state"],Is.EqualTo("interrupted"));Assert.That((string)task["operations"][0]["state"],Is.EqualTo("completed"));
        Invoke(p,"Reload");Assert.That(JToken.DeepEquals(after,Snapshot(p)),Is.True);
    }
    [Test] public void CompletedTaskIsUnchangedAfterRestartAndCannotBeCancelled() {
        var p=Probe();var created=Create(p,Snapshot(p));var running=Call(p,"Task.Update",UpdateRequest(created));var update=UpdateRequest(running);update["state"]="completed";
        var completed=Call(p,"Task.Update",update);var before=Snapshot(p);Invoke(p,"Reload");Assert.That(JToken.DeepEquals(before,Snapshot(p)),Is.True);
        var result=Call(p,"Task.Cancel",new JObject{["conversationId"]=created["conversationId"],["taskId"]=update["taskId"]});
        Assert.That(JToken.DeepEquals(result,completed),Is.True);
    }

    [Test] public void GoalCreationPersistsUserAndTaskTogetherAndFinalSummaryEntersContext() {
        var p=Probe();var snapshot=Snapshot(p);string id=Guid.NewGuid().ToString("N");
        var request=new JObject{["conversationId"]=snapshot["conversationId"],["taskId"]=id,["title"]="Widget set",["goalText"]="Build two widgets",["userMessage"]="Build two widgets",["profileKey"]="profile"};
        var created=Call(p,"Task.Create",request);Assert.That(((JArray)Snapshot(p)["messages"]).Count,Is.EqualTo(2));
        Assert.That((JArray)Invoke(p,"Context"),Is.Empty);
        var update=UpdateRequest(created);update["runnerProgramId"]=1032;var bound=Call(p,"Task.Update",update);
        Assert.That((int)bound["message"]["metadata"]["agenticTask"]["runnerProgramId"],Is.EqualTo(1032));
        update=UpdateRequest(bound);update["state"]="failed";update["message"]="Unable to save: disk full";Call(p,"Task.Update",update);
        var context=(JArray)Invoke(p,"Context");Assert.That(context.Count,Is.EqualTo(2));Assert.That((string)context[1]["parts"][0]["text"],Does.Contain("disk full"));
    }
    [Test] public void GoalArtifactsRejectUnrelatedWidgetPaths() {
        var p=Probe();var snapshot=Snapshot(p);string id=Guid.NewGuid().ToString("N");
        var created=Call(p,"Task.Create",new JObject{["conversationId"]=snapshot["conversationId"],["taskId"]=id,["title"]="Widget set",["goalText"]="Build widgets",["userMessage"]="Build widgets",["profileKey"]="profile"});
        var update=UpdateRequest(created);update["artifacts"]=new JArray(new JObject{["kind"]="widget",["id"]="custom/unrelated"});
        Assert.That((string)Call(p,"Task.Update",update)["Status"],Is.EqualTo("Error"));
        update["artifacts"]=new JArray(new JObject{["kind"]="widget",["id"]="goals/g"+id+"/counter"});
        var saved=Call(p,"Task.Update",update);Assert.That((string)saved["Status"],Is.EqualTo("Ok"));
        update=UpdateRequest(saved);update["artifacts"]=new JArray();Assert.That((string)Call(p,"Task.Update",update)["Status"],Is.EqualTo("Error"));
    }
    JObject CreateGoal(object p,string parent=null) {
        var input=new JObject{["conversationId"]=Snapshot(p)["conversationId"],["taskId"]=Guid.NewGuid().ToString("N"),["title"]="Widget set",
            ["goalText"]="Build widgets",["userMessage"]="Build widgets",["profileKey"]="profile"};
        if(parent!=null)input["parentTaskId"]=parent;return Call(p,"Task.Create",input);
    }
    [Test] public void ReadableWidgetFolderIsPersistedInheritedAndImmutable() {
        var p=Probe();var created=CreateGoal(p);var request=UpdateRequest(created);request["widgetPrefix"]="cyberpunk";
        var assigned=Call(p,"Task.Update",request);Assert.That((string)assigned["Status"],Is.EqualTo("Ok"));
        request=UpdateRequest(assigned);request["widgetPrefix"]="renamed";
        var before=Snapshot(p);Assert.That((string)Call(p,"Task.Update",request)["Status"],Is.EqualTo("Error"));Assert.That(JToken.DeepEquals(before,Snapshot(p)),Is.True);
        request=UpdateRequest(assigned);request["state"]="completed";request["artifacts"]=new JArray(new JObject{["kind"]="widget",["id"]="cyberpunk/clock"});
        var completed=Call(p,"Task.Update",request);Assert.That((string)completed["Status"],Is.EqualTo("Ok"));
        var child=CreateGoal(p,(string)request["taskId"]);Assert.That((string)child["message"]["metadata"]["agenticTask"]["widgetPrefix"],Is.EqualTo("cyberpunk"));
        Invoke(p,"Reload");Assert.That((string)Snapshot(p)["messages"].Last["metadata"]["agenticTask"]["widgetPrefix"],Is.EqualTo("cyberpunk"));
    }
    [Test] public void DifferentObjectivesCannotClaimTheSameWidgetFolder() {
        var p=Probe();var first=CreateGoal(p);var request=UpdateRequest(first);request["widgetPrefix"]="cyberpunk";
        var assigned=Call(p,"Task.Update",request);request=UpdateRequest(assigned);request["state"]="failed";Call(p,"Task.Update",request);
        var other=CreateGoal(p);request=UpdateRequest(other);request["widgetPrefix"]="cyberpunk";
        Assert.That((string)Call(p,"Task.Update",request)["Status"],Is.EqualTo("Error"));
    }
    [TestCase("../escape")][TestCase("/absolute")][TestCase("other/folder")]
    public void FolderMetadataRejectsPathsWithoutChangingHistory(string prefix) {
        var p=Probe();var created=CreateGoal(p);var before=Snapshot(p);var request=UpdateRequest(created);request["widgetPrefix"]=prefix;
        Assert.That((string)Call(p,"Task.Update",request)["Status"],Is.EqualTo("Error"));Assert.That(JToken.DeepEquals(before,Snapshot(p)),Is.True);
    }
    [Test] public void ExistingLegacyArtifactsCannotBeReboundToANewFolder() {
        var p=Probe();var created=CreateGoal(p);var request=UpdateRequest(created);
        request["artifacts"]=new JArray(new JObject{["kind"]="widget",["id"]="goals/g"+request["taskId"]+"/clock"});
        var saved=Call(p,"Task.Update",request);request=UpdateRequest(saved);request["widgetPrefix"]="cyberpunk";
        Assert.That((string)Call(p,"Task.Update",request)["Status"],Is.EqualTo("Error"));
    }

}
