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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace HomeGenie.Tests;
[TestFixture]
public class AiGenieGoalsTest {
    Assembly assembly;object goals;string folder,conversation,taskId;JObject job;int creates,runs,generations,saves;bool stopOnGenerate,clearOnGenerate,failSave;Queue<string> replies;Action changeOnGenerate;JArray lastMessages;int failSaveAt=2;Dictionary<string,JObject> installedPrograms, originals;Dictionary<string,string> deploymentTargets;int deployments,recoveries;bool unknownDeployment;
    string History=>"System.Utility/ChatHistory/";
    [OneTimeSetUp] public void CompileShippedGoals() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        var xml=XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-base/programs.xml")).Root;
        string Context(string id)=>(string)xml.Elements().Single(n=>(string)n.Element("Address")==id).Element("ScriptContext");
        var goal=CSharpSyntaxTree.ParseText(Context("583")).GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text=="AiGenieGoals").ToFullString();
        var programActions=CSharpSyntaxTree.ParseText(Context("583")).GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text=="AiGenieProgramActions").ToFullString();
        var code="using System;using System.IO;using System.Linq;using System.Text;using System.Collections.Generic;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+Context("577")+goal+programActions+@"
public static class WidgetContextRequest{public static void Validate(JObject request){}}
public static class AiGenieContext{public static JObject Prepare(JObject scope,string template,string folder,Func<string,object,object> call)=>new JObject{[""systemPrompt""]=""ACTUALLY SUPPLIED CONTEXT: {}""};}
public static class AiGenieCoordinator{public static JObject Message(string role,string text)=>new JObject{[""role""]=role,[""parts""]=new JArray(new JObject{[""text""]=text})};}
public static class AiGeniePreparation{public static JObject Normalize(JObject input,Func<string,object,object> call)=>input;}
public static class AiGenieHistory{public static JObject Read(JObject input,Func<string,object,object> call){call(""test-profile"",input);return new JObject{[""messages""]=new JArray()};}}
";
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location))
            .Append(typeof(JsonConvert).Assembly.Location).Append(typeof(SixLabors.ImageSharp.Image).Assembly.Location).Distinct();
        var compiled=CSharpCompilation.Create("Goals"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var output=new MemoryStream();var result=compiled.Emit(output);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));assembly=Assembly.Load(output.ToArray());
    }
    object Invoke(string type,string method,params object[] args)=>assembly.GetType(type).GetMethod(method).Invoke(type=="AiGenieGoals"?goals:null,args);
    JObject HistoryCall(string command,JObject input)=>JObject.FromObject(Invoke("ConversationTasks","Handle",folder,"933",command,input));
    JObject TaskRecord()=> (JObject)HistoryCall("Task.Get",Identity())["message"]["metadata"]["agenticTask"];
    string Prefix()=>(string)TaskRecord()["widgetPrefix"]??"goals/g"+((string)TaskRecord()["rootTaskId"]??taskId);
    JObject Identity()=>new JObject{["conversationId"]=conversation,["taskId"]=taskId};
    [SetUp] public void Setup(){
        folder=Path.Combine(Path.GetTempPath(),"hg-goals-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        assembly.GetType("HistoryStore").GetProperty("Stores").SetValue(null,Activator.CreateInstance(assembly.GetType("HistoryStore").GetProperty("Stores").PropertyType));
        goals=Activator.CreateInstance(assembly.GetType("AiGenieGoals"));conversation=(string)HistoryCall("Conversation.Get",new JObject())["conversationId"];taskId=Guid.NewGuid().ToString("N");
        creates=runs=generations=saves=0;stopOnGenerate=clearOnGenerate=failSave=false;job=null;replies=new Queue<string>();changeOnGenerate=null;lastMessages=null;failSaveAt=2;
        installedPrograms=new();originals=new();deploymentTargets=new();deployments=recoveries=0;unknownDeployment=false;
    }
    [TearDown] public void Cleanup(){((IDisposable)goals).Dispose();Directory.Delete(folder,true);}
    object Call(string route,object value){
        if(route.StartsWith(History))return HistoryCall(route.Substring(History.Length),(JObject)value);
        if(route.EndsWith("Programs.Add")){
            creates++;var program=(JObject)value;
            Assert.That((string)program["ScriptSetup"],Does.Contain("Program.Run(false)"));Assert.That((bool)program["AutoRestartEnabled"],Is.False);
            string source=(string)program["ScriptSource"];int a=source.IndexOf("JObject.Parse(")+14,b=source.IndexOf("),[\"running\"]",a);
            job=JObject.Parse(JsonConvert.DeserializeObject<string>(source.Substring(a,b-a)));
            return new{ResponseValue=(1031+creates).ToString()};
        }
        if(route.Contains("Programs.Get") && installedPrograms.TryGetValue(route.Split('/').Last(),out var installed))return installed.DeepClone();
        if(route.Contains("Programs.Get"))return new{Description="AI-GOAL-RUNNER:"+taskId,ScriptErrors=""};
        if(route.Contains("Programs.Run")){runs++;return new{Status="OK"};}
        if(route.Contains("Programs.Disable"))return new{Status="OK"};
        if(route=="test-profile"){Assert.That((string)((JObject)value)["profileKey"],Is.EqualTo("profile-one"));return null;}
        if(route.Contains("Modules.Get"))return new{Properties=new object[0]};
        if(route.EndsWith("/Generate")){
            generations++;lastMessages=(JArray)((JArray)((IDictionary<string,object>)value)["messages"]).DeepClone();changeOnGenerate?.Invoke();
            if(stopOnGenerate)HistoryCall("Task.Cancel",Identity());
            if(clearOnGenerate)Invoke("HistoryStore","Replace",folder,"933",Activator.CreateInstance(typeof(List<>).MakeGenericType(assembly.GetType("ChatMessage"))),false);
            return new{state="completed",text=replies.Dequeue()};
        }
        if(route.EndsWith("/Cancel"))return new{state="cancelled"};
        if(route.EndsWith("Program.Deploy")) {
            deployments++;var request=JObject.Parse((string)value);string id=(string)request["requestId"],target=(string)request["program_id"];
            if(!deploymentTargets.TryGetValue(id,out var pid)) {
                pid=(string)request["mode"]=="create"?(2000+deploymentTargets.Count).ToString():target;deploymentTargets[id]=pid;
                if(installedPrograms.TryGetValue(pid,out var prior))originals[id]=(JObject)prior.DeepClone();
            }
            var source=(JObject)request["program"];bool bad=((string)source["main"]).Contains("BAD");
            installedPrograms[pid]=new JObject{["Address"]=int.Parse(pid),["Group"]="Automan",["Name"]=source["name"],["Type"]=source["language"],
                ["ScriptSetup"]=source["setup"],["ScriptSource"]=source["main"],["ScriptContext"]=source["context"],["Description"]=source["description"],
                ["IsEnabled"]=!bad,["ScriptErrors"]=bad?"compiler diagnostic":""};
            return new JObject{["RequestId"]=id,["Index"]=request["attempt"],["State"]=unknownDeployment?"unknown":bad?"failed":"completed",["Success"]=!bad && !unknownDeployment,
                ["Response"]=new JObject{["status"]=unknownDeployment?"unknown":bad?"compile_failed":"success",["program_id"]=pid,["compiler_errors"]=new JArray("Compilation diagnostic")}};
        }
        if(route.EndsWith("Program.Recover")) {
            recoveries++;string id=(string)JObject.Parse((string)value)["requestId"];
            if(!unknownDeployment && originals.TryGetValue(id,out var original))installedPrograms[deploymentTargets[id]]=(JObject)original.DeepClone();
            return new JObject{["RequestId"]=id,["State"]=unknownDeployment?"unknown":"completed",["Success"]=!unknownDeployment,["Error"]=unknownDeployment?"Inspect uncertain deployment":null};
        }
        if(route.EndsWith("Widgets.Parse"))return new{Status=0};
        if(route.Contains("Widgets.Save/")){
            saves++;if(failSave && saves==failSaveAt)return new{Status="Error",Message="Disk full"};
            var command=new MIG.MigInterfaceCommand(route);string type=command.GetOption(0);string widgetId=Uri.UnescapeDataString(command.GetOption(1));
            Assert.That(widgetId,Does.StartWith(Prefix()+"/"),"The whole widget ID must be one API option.");
            string path=Path.Combine(folder,widgetId);Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path+"."+type,(string)value);
            if(type=="js")File.WriteAllText(path+".module.js","module");return new{Status=0};
        }
        if(route.Contains("Widgets.Delete/")){
            var command=new MIG.MigInterfaceCommand(route);string widgetId=Uri.UnescapeDataString(command.GetOption(0));
            Assert.That(widgetId,Does.StartWith(Prefix()+"/"));string path=Path.Combine(folder,widgetId);foreach(var ext in new[]{"html","css","js","module.js"})File.Delete(path+"."+ext);return new{Status=0};
        }
        throw new InvalidOperationException("Unexpected route: "+route);
    }
    Dictionary<string,object> StartRequest(bool allowed=true)=>new(){["providerId"]="933",["template"]="Custom persona",["input"]=new JObject{["goalId"]=taskId,["conversationId"]=conversation,["request"]=new JObject{["version"]=1,["message"]="Create a counter widget"}},
        ["prepared"]=new JObject{["profileKey"]="profile-one",["context"]=new JObject(),["capabilities"]=new JObject{["widgets"]=allowed,["automation"]=true}}};
    JObject Start(string parent=null){var input=StartRequest();if(parent!=null)((JObject)input["input"])["parentTaskId"]=parent;return JObject.FromObject(Invoke("AiGenieGoals","Start",input,new Func<string,object,object>(Call)));}
    string MakeParent(){Start();replies.Enqueue(Action("save_widget"));replies.Enqueue(Complete);Run();Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));string parent=taskId;taskId=Guid.NewGuid().ToString("N");return parent;}
    JObject Run()=>JObject.FromObject(Invoke("AiGenieGoals","Run",new Dictionary<string,object>{["job"]=job,["running"]=new Func<bool>(()=>true)},new Func<string,object,object>(Call),folder));
    string Action(string action,string name="counter") {
        var value=new JObject{["version"]=1,["action"]=action,["title"]="Counter",["message"]="Counter prepared",
            ["name"]=name,["widget"]=new JObject{["html"]="<div>Counter</div>",["css"]="div{}",["javascript"]="class Counter {}"}};
        if(TaskRecord()["widgetPrefix"]==null && !(TaskRecord()["artifacts"] as JArray ?? new JArray()).Any(a=>(string)a["kind"]=="widget"))value["folder"]="counter-widgets";
        return value.ToString();
    }
    string Complete=>"{\"version\":1,\"action\":\"complete\",\"title\":\"Done\",\"message\":\"Saved the counter\"}";
    [Test] public void LostStartReceiptNeverCreatesOrRunsASecondProgram(){Start();Start();Assert.That(creates,Is.EqualTo(1));Assert.That(runs,Is.EqualTo(1));Assert.That((int)TaskRecord()["runnerProgramId"],Is.EqualTo(1032));}
    [Test] public void DisabledWidgetCapabilityCreatesNothing(){Assert.Throws<TargetInvocationException>(()=>Invoke("AiGenieGoals","Start",StartRequest(false),new Func<string,object,object>(Call)));Assert.That(creates,Is.Zero);}
    [Test] public void SavedWidgetIsReadBackAndLinkedBeforeCompletion(){Start();replies.Enqueue(Action("save_widget"));replies.Enqueue(Complete);Run();Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));Assert.That(((JArray)TaskRecord()["artifacts"]).Count,Is.EqualTo(1));Assert.That(saves,Is.EqualTo(3));}
    [Test] public void ValidationFailureCanBeCorrectedWithoutLeavingAFailedOperation(){Start();replies.Enqueue("{}");replies.Enqueue(Action("save_widget"));replies.Enqueue(Complete);Run();Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));Assert.That(generations,Is.EqualTo(3));}
    [Test] public void FalseCompletionIsRejectedAndRepairBudgetIsBounded(){Start();foreach(var _ in Enumerable.Range(0,3))replies.Enqueue(Complete);Run();Assert.That((string)TaskRecord()["state"],Is.EqualTo("failed"));Assert.That(generations,Is.EqualTo(3));Assert.That(saves,Is.Zero);}
    [Test] public void StopDuringGenerationPreventsAnyWrites(){Start();stopOnGenerate=true;replies.Enqueue(Action("save_widget"));Run();Assert.That((string)TaskRecord()["state"],Is.EqualTo("cancelled"));Assert.That(saves,Is.Zero);}
    [Test] public void ClearingHistoryFencesTheOldWorker(){Start();clearOnGenerate=true;replies.Enqueue(Action("save_widget"));Run();Assert.That(saves,Is.Zero);Assert.That((string)HistoryCall("Task.Get",Identity())["Status"],Is.EqualTo("Error"));}
    [Test] public void DiskFailureDoesNotAskTheModelToRetryOrLeaveNewPartialWidget(){Start();failSave=true;replies.Enqueue(Action("save_widget"));Run();Assert.That((string)TaskRecord()["state"],Is.EqualTo("failed"));Assert.That(generations,Is.EqualTo(1));Assert.That(Directory.GetFiles(folder,"*.html",SearchOption.AllDirectories),Is.Empty);}
    [TestCase("../outside")][TestCase("/absolute")][TestCase("other/widget")]
    public void ModelCannotChooseAnExternalWidgetPath(string name){Start();for(int i=0;i<3;i++)replies.Enqueue(Action("save_widget",name));Run();Assert.That(saves,Is.Zero);Assert.That((string)TaskRecord()["state"],Is.EqualTo("failed"));}
    [Test] public void RevisionReadsAndUpdatesTheOriginalWidgetInsteadOfCreatingACopy(){
        string parent=MakeParent();Start(parent);replies.Enqueue(Action("save_widget").Replace("<div>Counter</div>","<div>Revised</div>"));replies.Enqueue(Complete);Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));Assert.That((string)TaskRecord()["parentTaskId"],Is.EqualTo(parent));
        Assert.That((string)TaskRecord()["artifacts"][0]["id"],Is.EqualTo(Prefix()+"/counter"));
        Assert.That(File.ReadAllText(Path.Combine(folder,Prefix()+"/counter.html")),Is.EqualTo("<div>Revised</div>"));
        Assert.That(Directory.GetFiles(folder,"counter.html",SearchOption.AllDirectories).Length,Is.EqualTo(1));
        Assert.That(lastMessages.ToString(),Does.Contain("HOST-SUPPLIED EXISTING WIDGET SOURCES").And.Contain("<div>Counter</div>"));
    }
    [Test] public void ARevisionMustActuallySaveAChangeBeforeCompleting(){
        string parent=MakeParent();Start(parent);for(int i=0;i<3;i++)replies.Enqueue(Complete);Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("failed"));Assert.That(saves,Is.EqualTo(3));
    }
    [Test] public void UnknownOrActiveParentCreatesNoRunner(){
        Assert.Throws<TargetInvocationException>(()=>Start(Guid.NewGuid().ToString("N")));Assert.That(creates,Is.Zero);
        string failed=taskId;taskId=Guid.NewGuid().ToString("N");Start();string active=taskId;taskId=Guid.NewGuid().ToString("N");
        Assert.Throws<TargetInvocationException>(()=>Start(active));Assert.That(creates,Is.EqualTo(1));
    }
    [Test] public void ConcurrentManualEditIsPreserved(){
        string parent=MakeParent();Start(parent);string file=Path.Combine(folder,Prefix()+"/counter.html");
        changeOnGenerate=()=>File.WriteAllText(file,"Manual edit");replies.Enqueue(Action("save_widget"));Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("failed"));Assert.That(File.ReadAllText(file),Is.EqualTo("Manual edit"));Assert.That(saves,Is.EqualTo(3));
    }
    [Test] public void FailureDuringRevisionRestoresPreviousSources(){
        string parent=MakeParent();Start(parent);failSave=true;failSaveAt=saves+2;
        replies.Enqueue(Action("save_widget").Replace("<div>Counter</div>","<div>New</div>"));Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("failed"));Assert.That(File.ReadAllText(Path.Combine(folder,Prefix()+"/counter.html")),Is.EqualTo("<div>Counter</div>"));
    }
    [Test] public void RevisionCannotOverwriteAnUnselectedSiblingWidget(){
        string parent=MakeParent();Start(parent);string file=Path.Combine(folder,Prefix()+"/unselected.html");File.WriteAllText(file,"Keep this");
        replies.Enqueue(Action("save_widget","unselected"));Run();Assert.That((string)TaskRecord()["state"],Is.EqualTo("failed"));
        Assert.That(File.ReadAllText(file),Is.EqualTo("Keep this"));Assert.That(saves,Is.EqualTo(3));
    }
    [Test] public void RevisionsCanBeChainedWithoutChangingTheWidgetNamespace(){
        string root=MakeParent();Start(root);replies.Enqueue(Action("save_widget"));replies.Enqueue(Complete);Run();
        string parent=taskId;taskId=Guid.NewGuid().ToString("N");Start(parent);replies.Enqueue(Action("save_widget"));replies.Enqueue(Complete);Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));Assert.That((string)TaskRecord()["rootTaskId"],Is.EqualTo(root));
        Assert.That(Directory.GetFiles(folder,"counter.html",SearchOption.AllDirectories).Length,Is.EqualTo(1));
    }
    string ProgramAction(string main="Program.Emit(\"Test.Value\", 1);",string key="counter-service")=>new JObject{
        ["version"]=1,["action"]="save_program",["title"]="Create program",["message"]="Counter service",["name"]=key,
        ["program"]=new JObject{["name"]="Counter service",["language"]="csharp",["setup"]="Program.Run();",["main"]=main,["context"]=""}}.ToString();
    [Test] public void ProgramDeploymentProducesAConfirmedArtifactAndUsesRealIdInContinuation(){
        Start();replies.Enqueue(ProgramAction());replies.Enqueue(Complete);Run();Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));
        Assert.That((string)TaskRecord()["artifacts"][0]["kind"],Is.EqualTo("program"));Assert.That((string)TaskRecord()["artifacts"][0]["id"],Is.EqualTo("2000"));
        Assert.That(lastMessages.ToString(),Does.Contain("ID=2000"));Assert.That(deployments,Is.EqualTo(1));
    }
    [Test] public void CompilerRepairKeepsOneDeploymentAndOneProgramIdentity(){
        Start();replies.Enqueue(ProgramAction("BAD"));replies.Enqueue(ProgramAction());replies.Enqueue(Complete);Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));Assert.That(deployments,Is.EqualTo(2));Assert.That(deploymentTargets.Count,Is.EqualTo(1));
        Assert.That(installedPrograms.Count,Is.EqualTo(1));Assert.That(lastMessages.ToString(),Does.Contain("COMPILATION FAILED"));
    }
    [Test] public void ProgramUpdateReusesTheSelectedResultId(){
        Start();replies.Enqueue(ProgramAction());replies.Enqueue(Complete);Run();string parent=taskId;taskId=Guid.NewGuid().ToString("N");
        Start(parent);replies.Enqueue(ProgramAction("Program.Emit(\"Test.Value\", 2);"));replies.Enqueue(Complete);Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));Assert.That(installedPrograms.Count,Is.EqualTo(1));
        Assert.That((string)installedPrograms["2000"]["ScriptSource"],Does.Contain(", 2)"));
    }
    [Test] public void StoppingCompilerRepairRestoresThePreviousProgram(){
        Start();replies.Enqueue(ProgramAction());replies.Enqueue(Complete);Run();string original=(string)installedPrograms["2000"]["ScriptSource"];
        string parent=taskId;taskId=Guid.NewGuid().ToString("N");Start(parent);replies.Enqueue(ProgramAction("BAD"));replies.Enqueue(ProgramAction());
        int baseGeneration=generations;changeOnGenerate=()=>{if(generations==baseGeneration+2)HistoryCall("Task.Cancel",Identity());};Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("cancelled"));Assert.That((string)installedPrograms["2000"]["ScriptSource"],Is.EqualTo(original));Assert.That(recoveries,Is.EqualTo(1));
    }
    [Test] public void DisabledProgramCapabilityMakesNoDeployment(){
        var request=StartRequest();((JObject)request["prepared"])["capabilities"]["automation"]=false;
        Invoke("AiGenieGoals","Start",request,new Func<string,object,object>(Call));for(int i=0;i<3;i++)replies.Enqueue(ProgramAction());Run();
        Assert.That(deployments,Is.Zero);Assert.That((string)TaskRecord()["state"],Is.EqualTo("failed"));
    }
    [Test] public void UncertainProgramOutcomeIsNotRetried(){
        Start();unknownDeployment=true;replies.Enqueue(ProgramAction());Run();Assert.That(deployments,Is.EqualTo(1));Assert.That((string)TaskRecord()["state"],Is.EqualTo("failed"));
    }
    [Test] public void ProgramCompilerRepairCannotSwitchToAnotherLogicalName(){
        Start();replies.Enqueue(ProgramAction("BAD"));for(int i=0;i<3;i++)replies.Enqueue(ProgramAction(key:"replacement"));Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("failed"));Assert.That(deployments,Is.EqualTo(1));Assert.That(recoveries,Is.EqualTo(1));
    }
    [Test] public void ProgramManualEditsDuringGenerationAreNotOverwritten(){
        Start();replies.Enqueue(ProgramAction());replies.Enqueue(Complete);Run();string parent=taskId;taskId=Guid.NewGuid().ToString("N");Start(parent);
        changeOnGenerate=()=>installedPrograms["2000"]["ScriptSource"]="Manual edit";replies.Enqueue(ProgramAction());Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("failed"));Assert.That((string)installedPrograms["2000"]["ScriptSource"],Is.EqualTo("Manual edit"));Assert.That(deployments,Is.EqualTo(1));
    }
    [Test] public void AManualEditDuringCompilerRepairIsNotUndoneByRecovery(){
        Start();replies.Enqueue(ProgramAction("BAD"));replies.Enqueue(ProgramAction());
        changeOnGenerate=()=>{if(generations==2)installedPrograms["2000"]["ScriptSource"]="Manual repaired code";};Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("failed"));Assert.That((string)installedPrograms["2000"]["ScriptSource"],Is.EqualTo("Manual repaired code"));
        Assert.That(deployments,Is.EqualTo(1));Assert.That(recoveries,Is.Zero);
    }
    [Test] public void FirstSaveUsesReadableFolderAndPersistsItBeforeSourceWrites() {
        Start();replies.Enqueue(Action("save_widget"));replies.Enqueue(Complete);Run();
        Assert.That(Prefix(),Is.EqualTo("counter-widgets"));
        Assert.That((string)TaskRecord()["artifacts"][0]["id"],Is.EqualTo("counter-widgets/counter"));
        Assert.That(File.ReadAllText(Path.Combine(folder,"counter-widgets/.ai-goal")),Is.EqualTo(taskId));
        Assert.That(lastMessages.ToString(),Does.Contain("/widgets/counter-widgets/counter.module.js").And.Contain("counter-widgets-counter"));
    }
    [TestCase("directory")][TestCase("file")][TestCase("case")]
    public void ExistingNamesReceiveASuffixWithoutAdoptingExistingContent(string kind) {
        if(kind=="directory")Directory.CreateDirectory(Path.Combine(folder,"counter-widgets"));
        else File.WriteAllText(Path.Combine(folder,kind=="case"?"Counter-Widgets.js":"counter-widgets.html"),"Keep this");
        Start();replies.Enqueue(Action("save_widget"));replies.Enqueue(Complete);Run();
        Assert.That(Prefix(),Is.EqualTo("counter-widgets-2"));Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));
    }
    [Test] public void NewObjectiveAfterHistoryClearDoesNotReuseAnExistingFolder() {
        MakeParent();Invoke("HistoryStore","Replace",folder,"933",Activator.CreateInstance(typeof(List<>).MakeGenericType(assembly.GetType("ChatMessage"))),false);
        conversation=(string)HistoryCall("Conversation.Get",new JObject())["conversationId"];
        Start();replies.Enqueue(Action("save_widget"));replies.Enqueue(Complete);Run();
        Assert.That(Prefix(),Is.EqualTo("counter-widgets-2"));
    }
    [Test] public void AllWidgetsUseTheAssignedSuffixAndContinuationReceivesExactPaths() {
        Directory.CreateDirectory(Path.Combine(folder,"counter-widgets"));Start();
        replies.Enqueue(Action("save_widget"));var parent=JObject.Parse(Action("save_widget","dashboard"));parent.Remove("folder");replies.Enqueue(parent.ToString());replies.Enqueue(Complete);Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));
        Assert.That(((JArray)TaskRecord()["artifacts"]).Select(a=>(string)a["id"]),Is.EqualTo(new[]{"counter-widgets-2/counter","counter-widgets-2/dashboard"}));
        Assert.That(lastMessages.ToString(),Does.Contain("/widgets/counter-widgets-2/counter.module.js").And.Contain("counter-widgets-2-counter"));
    }
    [Test] public void MissingFolderCanBeRepairedBeforeAnyWidgetIsSaved() {
        Start();var first=JObject.Parse(Action("save_widget"));first.Remove("folder");replies.Enqueue(first.ToString());
        replies.Enqueue(Action("save_widget"));replies.Enqueue(Complete);Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));Assert.That(saves,Is.EqualTo(3));Assert.That(generations,Is.EqualTo(3));
    }
    [TestCase("../outside")][TestCase("other/name")][TestCase("/absolute")][TestCase("CON")][TestCase("con")]
    public void InvalidFolderIsRejectedBeforeCreatingFiles(string name) {
        Start();for(int i=0;i<3;i++){var action=JObject.Parse(Action("save_widget"));action["folder"]=name;replies.Enqueue(action.ToString());}Run();
        Assert.That(saves,Is.Zero);Assert.That(Directory.GetDirectories(folder),Is.Empty);
    }
    [Test] public void LaterSaveCannotChangeTheAssignedFolder() {
        Start();replies.Enqueue(Action("save_widget"));var wrong=JObject.Parse(Action("save_widget","other"));wrong["folder"]="different";
        for(int i=0;i<3;i++)replies.Enqueue(wrong.ToString());Run();
        Assert.That(Prefix(),Is.EqualTo("counter-widgets"));Assert.That(saves,Is.EqualTo(3));Assert.That((string)TaskRecord()["state"],Is.EqualTo("failed"));
    }
    [Test] public void LegacyWidgetRevisionPreservesGuidPathAndImports() {
        Start();string prefix="goals/g"+taskId;
        string path=Path.Combine(folder,prefix,"counter");Directory.CreateDirectory(Path.GetDirectoryName(path));
        foreach(string ext in new[]{"html","css","js","module.js"})File.WriteAllText(path+"."+ext,"legacy");
        var update=Identity();update["expectedRevision"]=TaskRecord()["revision"];update["state"]="running";
        Assert.That((string)HistoryCall("Task.Update",update)["Status"],Is.EqualTo("Ok"));
        update["expectedRevision"]=TaskRecord()["revision"];update["state"]="completed";
        update["artifacts"]=new JArray(new JObject{["kind"]="widget",["id"]=prefix+"/counter"});
        Assert.That((string)HistoryCall("Task.Update",update)["Status"],Is.EqualTo("Ok"));
        string parent=taskId;taskId=Guid.NewGuid().ToString("N");Start(parent);replies.Enqueue(Action("save_widget"));replies.Enqueue(Complete);Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));Assert.That((string)TaskRecord()["artifacts"][0]["id"],Is.EqualTo(prefix+"/counter"));
        Assert.That(TaskRecord()["widgetPrefix"],Is.Null);
    }
    [Test] public void UnverifiedFolderOwnerStopsRevisionBeforeGenerationOrWrites() {
        string parent=MakeParent();File.WriteAllText(Path.Combine(folder,"counter-widgets/.ai-goal"),Guid.NewGuid().ToString("N"));
        int before=generations;Start(parent);Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("failed"));Assert.That(generations,Is.EqualTo(before));Assert.That(saves,Is.EqualTo(3));
    }
    [Test] public void ConcurrentReservationsAssignDifferentFolders() {
        var method=assembly.GetType("AiGenieGoals").GetMethod("ReserveWidgetFolder",BindingFlags.NonPublic|BindingFlags.Static);
        var results=new System.Collections.Concurrent.ConcurrentBag<string>();
        Parallel.For(0,8,_=>results.Add((string)method.Invoke(null,new object[]{folder,"panel",Guid.NewGuid().ToString("N")})));
        Assert.That(results.Distinct().Count(),Is.EqualTo(8));Assert.That(results,Does.Contain("panel").And.Contain("panel-8"));
    }

}
