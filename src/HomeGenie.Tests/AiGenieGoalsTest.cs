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
    JObject geminiDiagnostics;
    Dictionary<string,JObject> runners;
    bool goalDeploymentsSettled;
    JObject savedGoalCapabilities;
    string History=>"System.Utility/ChatHistory/";
    [OneTimeSetUp] public void CompileShippedGoals() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        var xml=XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-base/programs.xml")).Root;
        string Context(string id)=>(string)xml.Elements().Single(n=>(string)n.Element("Address")==id).Element("ScriptContext");
        var goal=CSharpSyntaxTree.ParseText(Context("583")).GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text=="AiGenieGoals").ToFullString();
        var programActions=CSharpSyntaxTree.ParseText(Context("583")).GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text=="AiGenieProgramActions").ToFullString();
        var deploymentState=CSharpSyntaxTree.ParseText(Context("581")).GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(n=>n.Identifier.Text is "ApiCommandExecution" or "ActionLedger" or "ProgramDeploymentSession" or "GoalDeploymentStatus");
        var code="using System;using System.IO;using System.Linq;using System.Text;using System.Threading;using System.Collections.Generic;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+Context("577")+goal+programActions+string.Join("\n",deploymentState)+@"
public static class WidgetContextRequest{public static void Validate(JObject request){}}
public static class AiGenieContext{public static JObject Prepare(JObject scope,string template,string folder,Func<string,object,object> call){call(""test-template"",template);return new JObject{[""systemPrompt""]=""ACTUALLY SUPPLIED CONTEXT: {}""};}}
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
    const string SaveWidgetContract = "version=1, action=\"save_widget\", name, widget, title, message, and folder for the first save of a new set.";
    string provider; string lastSystemPrompt; Queue<JObject> diagnosticReplies; JArray firstLocalMessages, secondLocalMessages;
    JObject HistoryCall(string command,JObject input)=>JObject.FromObject(Invoke("ConversationTasks","Handle",folder,provider,command,input));
    JObject TaskRecord()=> (JObject)HistoryCall("Task.Get",Identity())["message"]["metadata"]["agenticTask"];
    string Prefix()=>(string)TaskRecord()["widgetPrefix"]??"goals/g"+((string)TaskRecord()["rootTaskId"]??taskId);
    JObject Identity()=>new JObject{["conversationId"]=conversation,["taskId"]=taskId};
    [SetUp] public void Setup(){
        provider="933";lastSystemPrompt=null;diagnosticReplies=new();firstLocalMessages=secondLocalMessages=null;
        folder=Path.Combine(Path.GetTempPath(),"hg-goals-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        assembly.GetType("HistoryStore").GetProperty("Stores").SetValue(null,Activator.CreateInstance(assembly.GetType("HistoryStore").GetProperty("Stores").PropertyType));
        goals=Activator.CreateInstance(assembly.GetType("AiGenieGoals"));conversation=(string)HistoryCall("Conversation.Get",new JObject())["conversationId"];taskId=Guid.NewGuid().ToString("N");
        dashboardConflict=false;dashboardApplications=0;creates=runs=generations=saves=0;stopOnGenerate=clearOnGenerate=failSave=false;job=null;replies=new Queue<string>();changeOnGenerate=null;lastMessages=null;failSaveAt=2;
        geminiDiagnostics=null;
        runners=new();goalDeploymentsSettled=true;
        savedGoalCapabilities=null;
        installedPrograms=new();originals=new();deploymentTargets=new();deployments=recoveries=0;unknownDeployment=false;
    }
    [TearDown] public void Cleanup(){((IDisposable)goals).Dispose();Directory.Delete(folder,true);}
    bool dashboardConflict;int dashboardApplications;
    string ProviderName => provider=="940"?"Lailama":provider=="933"?"Gemini":"Gemini-"+provider;
    object Call(string route,object value){
        if(route.Contains("Client.PresetsApplyDashboard/")) {
            dashboardApplications++;var input=(JObject)value;
            Assert.That((long)input["expectedRevision"],Is.EqualTo(7));
            Assert.That((string)input["clientId"],Does.StartWith("ai:"));
            if(dashboardConflict)return new{Status="Error",Code="PresetConflict",Message="Preset changed"};
            return new JObject{["Status"]="Ok",["revision"]=8,["preset"]="demo",["dashboardName"]=input["dashboardName"],["widgetId"]=input["widgetId"]};
        }
        if(route.StartsWith(History))return HistoryCall(route.Substring(History.Length),(JObject)value);
        if(route.EndsWith("Programs.Add")){
            creates++;var program=(JObject)value;
            Assert.That((string)program["ScriptSetup"],Does.Contain("Program.Run(false)"));Assert.That((bool)program["AutoRestartEnabled"],Is.False);
            string source=(string)program["ScriptSource"];int a=source.IndexOf("JObject.Parse(")+14,b=source.IndexOf("),[\"running\"]",a);
            job=JObject.Parse(JsonConvert.DeserializeObject<string>(source.Substring(a,b-a)));
            string pid=(1031+creates).ToString();
            runners[pid]=(JObject)program.DeepClone();
            runners[pid]["Address"]=int.Parse(pid);
            runners[pid]["IsRunning"]=false;
            return new{ResponseValue=pid};
        }
        if(route.EndsWith("Programs.List"))return new JArray(runners.Values.Concat(installedPrograms.Values));
        if(route.Contains("Programs.Get") && installedPrograms.TryGetValue(route.Split('/').Last(),out var installed))return installed.DeepClone();
        if(route.Contains("Programs.Get"))return runners.TryGetValue(route.Split('/').Last(),out var runner)?runner.DeepClone():null;
        if(route.Contains("Programs.Delete")){runners.Remove(route.Split('/').Last());return new{Status="Ok"};}
        if(route.EndsWith("Program.GoalSettled"))return new{Status="Ok",settled=goalDeploymentsSettled};
        if(route.Contains("Programs.Run")){runs++;return new{Status="OK"};}
        if(route.Contains("Programs.Disable"))return new{Status="OK"};
        if(route=="test-profile"){Assert.That((string)((JObject)value)["profileKey"],Is.EqualTo("profile-one"));return null;}
        if(route=="test-template"){
            if(provider=="940")Assert.That(value,Is.EqualTo("#use-kb homegenie-core/references/widget-authoring.md"));
            return null;
        }
        if(route=="HomeAutomation.HomeGenie/Automation/Programs.Implements/^@AI:/regex")
            return new[]{new{Program=new{Address=int.Parse(provider),IsEnabled=true},ImplementedInterface=new{ApiUrl="AI.Genies/"+ProviderName}}};
        if(route.EndsWith("/Goals.Describe")) {
            Assert.That(route,Is.EqualTo("AI.Genies/"+ProviderName+"/Goals.Describe"));
            return new{version=1,widgets=true,automation=provider!="940",dashboard=provider!="940"};
        }
        if(route.Contains("Modules.Get") && savedGoalCapabilities!=null)
            return new{Properties=new[]{new{Name="AI.Chat.Capabilities",Value=savedGoalCapabilities.ToString()}}};
        if(route.Contains("Modules.Get"))return provider=="940"
            ? new {Properties=new object[]{new{Name="Status.Init",Value="Ready"},new{Name="LLM.Id",Value="model.gguf"}}}
            : new {Properties=Array.Empty<object>()};
        if(route=="AI.Genies/Lailama/Provider.Text.Generate"){
            var request=(IDictionary<string,object>)value;
            Assert.That((string)request["systemPrompt"],Does.Contain("CURRENT OBJECTIVE PROTOCOL").And.Contain(SaveWidgetContract));
            lastSystemPrompt=(string)request["systemPrompt"];
            lastMessages=(JArray)((JArray)request["messages"]).DeepClone();
            firstLocalMessages??=lastMessages;
            if(generations==1)secondLocalMessages=lastMessages;
            generations++;
            if(stopOnGenerate){
                HistoryCall("Task.Cancel",Identity());
                Assert.That(((CancellationToken)request["cancellationToken"]).WaitHandle.WaitOne(TimeSpan.FromSeconds(3)),Is.True);
                return new {version=1,requestId=request["requestId"],state="cancelled",text=""};
            }
            if(diagnosticReplies.Count>0) {
                var result=diagnosticReplies.Dequeue();
                result["version"]=1;result["requestId"]=(string)request["requestId"];
                return result;
            }
            return new {version=1,requestId=request["requestId"],state="completed",text=replies.Dequeue()};
        }
        if(route.EndsWith("/Generate")){
            var request=(IDictionary<string,object>)value;
            lastSystemPrompt=(string)request["systemPrompt"];
            Assert.That(request["purpose"],Is.EqualTo("widget-goal"));
            Assert.That(request["owner"],Is.EqualTo(provider));
            Assert.That(request["provider"],Is.EqualTo(ProviderName));
            Assert.That(JObject.FromObject(request["output"])["maxOutputTokens"],Is.Null);
            generations++;lastMessages=(JArray)((JArray)request["messages"]).DeepClone();changeOnGenerate?.Invoke();
            if(stopOnGenerate)HistoryCall("Task.Cancel",Identity());
            if(clearOnGenerate)Invoke("HistoryStore","Replace",folder,"933",Activator.CreateInstance(typeof(List<>).MakeGenericType(assembly.GetType("ChatMessage"))),false);
            return new{state="completed",text=replies.Dequeue(),diagnostics=geminiDiagnostics};
        }
        if(route.EndsWith("/Cancel"))return new{state="cancelled"};
        if(route.EndsWith("Program.Deploy") || route.EndsWith("Program.GoalUpdate")) {
            var update=value as IDictionary<string,object>;
            var request=JObject.Parse(update==null?(string)value:(string)update["payload"]);
            if(update!=null) {
                var current=installedPrograms[(string)request["program_id"]];
                Assert.That((string)request["mode"],Is.EqualTo("update"));
                Assert.That(((Func<JObject,bool>)update["verifyTarget"])(current),Is.True);
                var changed=(JObject)current.DeepClone();changed["ScriptSource"]="Another edit";
                Assert.That(((Func<JObject,bool>)update["verifyTarget"])(changed),Is.False);
            }
            deployments++;string id=(string)request["requestId"],target=(string)request["program_id"];
            if(!deploymentTargets.TryGetValue(id,out var pid)) {
                pid=(string)request["mode"]=="create"?(2000+deploymentTargets.Count).ToString():target;deploymentTargets[id]=pid;
                if(installedPrograms.TryGetValue(pid,out var prior))originals[id]=(JObject)prior.DeepClone();
            }
            var source=(JObject)request["program"];bool bad=((string)source["main"]).Contains("BAD");
            installedPrograms[pid]=new JObject{["Address"]=int.Parse(pid),["Group"]="Automan",["Name"]=source["name"],["Type"]=source["language"],
                ["ScriptSetup"]=source["setup"],["ScriptSource"]=source["main"],["ScriptContext"]=source["context"],["Description"]=source["description"],
                ["IsEnabled"]=!bad,["ScriptErrors"]=bad?"compiler diagnostic":""};
            if(update!=null) {
                foreach(var field in new[]{"Group","Description","Data","AutoRestartEnabled","Cloneable","PackageInfo"})
                    installedPrograms[pid][field]=originals[id][field]?.DeepClone();
                installedPrograms[pid]["IsEnabled"]=!bad && originals[id]["IsEnabled"]?.Value<bool>()==true;
            }
            return new JObject{["RequestId"]=id,["Index"]=request["attempt"],["State"]=unknownDeployment?"unknown":bad?"failed":"completed",["Success"]=!bad && !unknownDeployment,
                ["Response"]=new JObject{["status"]=unknownDeployment?"unknown":bad?"compile_failed":"success",["program_id"]=pid,["compiler_errors"]=new JArray("Compilation diagnostic")}};
        }
        if(route.EndsWith("Program.Recover")) {
            recoveries++;string id=(string)JObject.Parse((string)value)["requestId"];
            if(!unknownDeployment && originals.TryGetValue(id,out var original))installedPrograms[deploymentTargets[id]]=(JObject)original.DeepClone();
            return new JObject{["RequestId"]=id,["State"]=unknownDeployment?"unknown":"completed",["Success"]=!unknownDeployment,["Error"]=unknownDeployment?"Inspect uncertain deployment":null};
        }
        if(route.EndsWith("Widgets.Parse")) {
            if(((string)value).Contains("export default",StringComparison.Ordinal))
                return new {Status="Error",Message="Unexpected token at line 2"};
            return new{Status=0};
        }
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
    void StartSelected(JArray types,bool dashboardTarget=false,bool widgetCapability=true) {
        var input=StartRequest(widgetCapability);
        input["providerId"]=provider;
        var envelope=(JObject)input["input"];
        envelope["goalTypes"]=types;
        if(dashboardTarget)envelope["dashboardTarget"]=new JObject{["preset"]="demo",["expectedRevision"]=7,["clientId"]=new string('a',32)};
        Invoke("AiGenieGoals","Start",input,new Func<string,object,object>(Call));
    }
    string MakeParent(){Start();replies.Enqueue(Action("save_widget"));replies.Enqueue(Complete);Run();Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));string parent=taskId;taskId=Guid.NewGuid().ToString("N");return parent;}
    JObject Run()=>JObject.FromObject(Invoke("AiGenieGoals","Run",new Dictionary<string,object>{["job"]=job,["running"]=new Func<bool>(()=>true)},new Func<string,object,object>(Call),folder,Path.Combine(folder,"widget_objective_prompt.md")));
    void CleanRunners(string address=null)=>Invoke("AiGenieGoals","CleanupRunners",new Func<string,object,object>(Call),address,new Action<string>(_=>{}));
    string Action(string action,string name="counter") {
        var value=new JObject{["version"]=1,["action"]=action,["title"]="Counter",["message"]="Counter prepared",
            ["name"]=name,["widget"]=new JObject{["html"]="<div>Counter</div>",["css"]="div{}",["javascript"]="class Counter {}"}};
        if(TaskRecord()["widgetPrefix"]==null && !(TaskRecord()["artifacts"] as JArray ?? new JArray()).Any(a=>(string)a["kind"]=="widget"))value["folder"]="counter-widgets";
        return value.ToString();
    }
    string Complete=>"{\"version\":1,\"action\":\"complete\",\"title\":\"Done\",\"message\":\"Saved the counter\"}";
    void DashboardStart() {
        var input=StartRequest();((JObject)input["input"])["dashboardTarget"]=new JObject{["preset"]="demo",["expectedRevision"]=7,["clientId"]=new string('a',32)};
        Invoke("AiGenieGoals","Start",input,new Func<string,object,object>(Call));
    }
    const string ApplyDashboard="{\"version\":1,\"action\":\"apply_dashboard\",\"title\":\"Apply dashboard\",\"message\":\"Apply saved container\",\"mode\":\"create\",\"dashboardName\":\"Demo room\",\"name\":\"counter\"}";
    [Test] public void DashboardApplicationIsRecordedSeparatelyFromWidgetSave() {
        DashboardStart();replies.Enqueue(Action("save_widget"));replies.Enqueue(ApplyDashboard);replies.Enqueue(Complete);Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));Assert.That(dashboardApplications,Is.EqualTo(1));
        var applied=TaskRecord()["artifacts"].Single(a=>(string)a["kind"]=="dashboard");
        Assert.That((string)applied["preset"],Is.EqualTo("demo"));Assert.That((string)applied["requestId"],Is.EqualTo(taskId));
        Assert.That((string)applied["widgetId"],Is.EqualTo(Prefix()+"/counter"));
    }
    [Test] public void DashboardConflictStopsWithoutRegeneratingSavedWidgets() {
        DashboardStart();dashboardConflict=true;replies.Enqueue(Action("save_widget"));replies.Enqueue(ApplyDashboard);Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("failed"));Assert.That(generations,Is.EqualTo(2));
        Assert.That(saves,Is.EqualTo(3));Assert.That(dashboardApplications,Is.EqualTo(1));
        Assert.That(((JArray)TaskRecord()["artifacts"]).Count,Is.EqualTo(1));
    }
    [Test] public void DashboardContractRejectsInvalidShapeBeforeRunning() {
        var action=JObject.Parse(ApplyDashboard);action["dashboardName"]=42;
        Assert.Throws<TargetInvocationException>(()=>Invoke("AiGenieGoals","Validate",action));
    }
    [Test] public void UnsavedDashboardCannotBeApplied() {
        DashboardStart();for(int i=0;i<3;i++)replies.Enqueue(ApplyDashboard);Run();
        Assert.That(dashboardApplications,Is.Zero);Assert.That((string)TaskRecord()["state"],Is.EqualTo("failed"));
    }
    [Test] public void StopBeforeDashboardActionPreventsPresetWrite() {
        DashboardStart();changeOnGenerate=()=>{if(generations==2)HistoryCall("Task.Cancel",Identity());};
        replies.Enqueue(Action("save_widget"));replies.Enqueue(ApplyDashboard);Run();
        Assert.That(dashboardApplications,Is.Zero);Assert.That(saves,Is.EqualTo(3));Assert.That((string)TaskRecord()["state"],Is.EqualTo("cancelled"));
    }
    [Test] public void DashboardRevisionKeepsItsIdentityAndUsesUpdate() {
        DashboardStart();replies.Enqueue(Action("save_widget"));replies.Enqueue(ApplyDashboard);replies.Enqueue(Complete);Run();
        string parent=taskId;taskId=Guid.NewGuid().ToString("N");var request=StartRequest();var input=(JObject)request["input"];
        input["parentTaskId"]=parent;input["dashboardTarget"]=job["dashboardTarget"].DeepClone();
        Invoke("AiGenieGoals","Start",request,new Func<string,object,object>(Call));
        replies.Enqueue(Action("save_widget"));replies.Enqueue(ApplyDashboard.Replace("create","update"));replies.Enqueue(Complete);Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));Assert.That(dashboardApplications,Is.EqualTo(2));
        Assert.That(TaskRecord()["artifacts"].Count(a=>(string)a["kind"]=="dashboard"),Is.EqualTo(1));
        Assert.That((string)TaskRecord()["artifacts"].Single(a=>(string)a["kind"]=="dashboard")["requestId"],Is.EqualTo(taskId));
    }
    [Test] public void DashboardRevisionRejectsAnotherPresetBeforeInference() {
        DashboardStart();replies.Enqueue(Action("save_widget"));replies.Enqueue(ApplyDashboard);replies.Enqueue(Complete);Run();
        string parent=taskId;taskId=Guid.NewGuid().ToString("N");var request=StartRequest();var input=(JObject)request["input"];
        input["parentTaskId"]=parent;input["dashboardTarget"]=new JObject{["preset"]="another",["expectedRevision"]=7,["clientId"]=new string('a',32)};
        Invoke("AiGenieGoals","Start",request,new Func<string,object,object>(Call));Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("failed"));Assert.That(generations,Is.EqualTo(3));Assert.That(dashboardApplications,Is.EqualTo(1));
    }
    void StartLailama(string parent=null) {
        if(provider!="940") {
            provider="940";
            conversation=(string)HistoryCall("Conversation.Get",new JObject())["conversationId"];
        }
        var request=StartRequest();
        request["providerId"]=provider;
        request["modelId"]="model.gguf";
        if(parent!=null)((JObject)request["input"])["parentTaskId"]=parent;
        Invoke("AiGenieGoals","Start",request,new Func<string,object,object>(Call));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EditableObjectiveGuidanceIsSharedAndPrecedesTheProtocol(bool local) {
        const string guidance = "Prefer a compact, readable layout.";
        if (local) StartLailama(); else Start();
        File.WriteAllText(Path.Combine(folder,"widget_objective_prompt.md"),guidance);
        replies.Enqueue(Action("save_widget"));
        replies.Enqueue(Complete);
        Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));
        Assert.That(lastSystemPrompt,Does.Contain(guidance).And.Contain(SaveWidgetContract));
        Assert.That(lastSystemPrompt,Does.Contain("name=\"room-status\", folder=\"room-tools\"")
            .And.Contain("name=\"room_status\", folder=\"room_tools\"").And.Contain("#status_text"));
        Assert.That(lastSystemPrompt.IndexOf(guidance,StringComparison.Ordinal),
            Is.LessThan(lastSystemPrompt.IndexOf("CURRENT OBJECTIVE PROTOCOL",StringComparison.Ordinal)));
    }

    [Test] public void LocalGoalCreatesOneWidgetAndRevisesItInPlace(){
        StartLailama();
        replies.Enqueue("\x60\x60\x60json\n"+Action("save_widget")+"\n\x60\x60\x60");
        replies.Enqueue(Complete);
        Run();
        var original=TaskRecord();
        Assert.That((string)original["state"],Is.EqualTo("completed"));
        var widgetId=(string)original["artifacts"][0]["id"];
        Assert.That(lastMessages.Select(message=>(string)message["role"]),Is.EqualTo(new[]{"user","user"}));
        Assert.That((string)lastMessages[0]["parts"][0]["text"],Is.EqualTo("Create a counter widget"));
        Assert.That((string)lastMessages.Last["parts"][0]["text"],Does.Contain("HOST RESULT: saved widget "+widgetId)
            .And.Contain("/widgets/"+widgetId+".module.js").And.Contain(widgetId.Replace('/','-')));
        Assert.That(lastMessages.ToString(),Does.Not.Contain("class Counter {}"));
        var parent=taskId;
        taskId=Guid.NewGuid().ToString("N");
        firstLocalMessages=null;
        StartLailama(parent);
        replies.Enqueue(Action("save_widget").Replace("<div>","<div class='updated'>"));
        replies.Enqueue(Complete);
        Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));
        Assert.That((string)TaskRecord()["artifacts"][0]["id"],Is.EqualTo(widgetId));
        Assert.That(firstLocalMessages.ToString(),Does.Contain("HOST-SUPPLIED EXISTING WIDGET SOURCES").And.Contain("<div>Counter</div>"));
        Assert.That(lastMessages.ToString(),Does.Not.Contain("HOST-SUPPLIED EXISTING WIDGET SOURCES").And.Not.Contain("class Counter {}"));
    }

    [Test] public void LocalGoalKeepsRejectedOutputAndContextLimitInTheSameFailedStep(){
        StartLailama();
        var rejected=JObject.Parse(Action("save_widget","folder/counter")).ToString(Formatting.None);
        diagnosticReplies.Enqueue(new JObject {["state"]="completed",["text"]=rejected,
            ["diagnostics"]=new JObject {["inputTokens"]=3800,["reservedOutputTokens"]=4096,["contextSize"]=8192,["generatedTokens"]=700}});
        diagnosticReplies.Enqueue(new JObject {["state"]="context_limit",["error"]="Background prompt and context exceed the local model context budget.",
            ["diagnostics"]=new JObject {["inputTokens"]=4700,["reservedOutputTokens"]=4096,["contextSize"]=8192}});
        Run();
        var task=TaskRecord();
        Assert.That((string)task["state"],Is.EqualTo("failed"));
        Assert.That(((JArray)task["operations"]).Count,Is.EqualTo(1));
        var step=task["operations"][0];
        Assert.That((string)step["state"],Is.EqualTo("failed"));
        Assert.That((string)step["details"],Does.Contain("save_widget.name must start with a lowercase letter (a-z)").And.Contain("Replace underscores with hyphens").And.Contain(rejected)
            .And.Contain("3800").And.Contain("4700").And.Contain("Generated 700 tokens"));
        Assert.That(lastMessages.Count,Is.EqualTo(2));
        Assert.That(lastMessages.ToString(),Does.Contain("Replace underscores with hyphens")
            .And.Not.Contain("Generated 700 tokens").And.Not.Contain("class Counter {}"));
        Assert.That(saves,Is.Zero);
    }

    [Test] public void LocalGoalReportsNameFolderAndJavaScriptErrorsInOneRetry(){
        StartLailama();
        var rejected=JObject.Parse(Action("save_widget","counter_widget"));
        rejected["folder"]="counter_widgets";
        rejected["widget"]["javascript"]="class Counter {} export default Counter;";
        replies.Enqueue(rejected.ToString());
        replies.Enqueue(Action("save_widget"));
        replies.Enqueue(Complete);
        Run();
        Assert.That(generations,Is.EqualTo(3));
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));
        Assert.That(saves,Is.EqualTo(3));
        Assert.That(Directory.GetDirectories(folder).Select(Path.GetFileName),Does.Not.Contain("counter_widgets"));
        Assert.That((string)secondLocalMessages.Last["parts"][0]["text"],Does.Contain("save_widget.name")
            .And.Contain("save_widget.folder").And.Contain("widget.javascript")
            .And.Contain("Unexpected token at line 2").And.Contain("export default"));
    }

    [Test] public void LocalGoalKeepsOnlyTheLatestCorrectionAndTheConfirmedSave(){
        StartLailama();
        replies.Enqueue(Action("save_widget"));
        replies.Enqueue(Action("save_widget","invalid_name"));
        replies.Enqueue(Action("save_widget","second"));
        replies.Enqueue(Complete);
        Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));
        Assert.That(generations,Is.EqualTo(4));
        Assert.That(saves,Is.EqualTo(3));
        Assert.That(lastMessages.Count,Is.EqualTo(2));
        Assert.That((string)lastMessages.Last["parts"][0]["text"],Does.Contain("HOST RESULT: saved widget "+Prefix()+"/counter")
            .And.Contain("Reuse the saved widget name").And.Not.Contain("Replace underscores with hyphens"));
    }

    [Test] public void LocalGoalCannotSaveASecondWidget(){
        StartLailama();
        replies.Enqueue(Action("save_widget"));
        for(int i=0;i<3;i++)replies.Enqueue(Action("save_widget","second"));
        Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("failed"));
        Assert.That(saves,Is.EqualTo(3));
        Assert.That(((JArray)TaskRecord()["artifacts"]).Count,Is.EqualTo(1));
    }

    [Test] public void LocalStopCancelsNativeGenerationWithoutSaving(){
        StartLailama();
        stopOnGenerate=true;
        Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("cancelled"));
        Assert.That(saves,Is.Zero);
    }

    [TestCase("completed")] [TestCase("failed")] [TestCase("cancelled")]
    public void FinishedRunnerCanBeCleanedWithoutChangingItsHistoryOrResults(string state) {
        Start();
        if(state=="completed") {replies.Enqueue(Action("save_widget"));replies.Enqueue(Complete);}
        else if(state=="cancelled") {stopOnGenerate=true;replies.Enqueue(Complete);}
        else for(int i=0;i<3;i++)replies.Enqueue(Complete);
        Run();
        var history=TaskRecord();
        Assert.That((string)history["state"],Is.EqualTo(state));
        var files=Directory.GetFiles(folder,"*.html",SearchOption.AllDirectories);
        runners["1032"]["IsRunning"]=true;
        CleanRunners("1032");
        Assert.That(runners.ContainsKey("1032"),Is.True);
        runners["1032"]["IsRunning"]=false;
        CleanRunners("1032");
        CleanRunners("1032");
        Assert.That(runners,Is.Empty);
        Assert.That(JToken.DeepEquals(history,TaskRecord()),Is.True);
        Assert.That(Directory.GetFiles(folder,"*.html",SearchOption.AllDirectories),Is.EquivalentTo(files));
        if(state=="completed") {
            string parent=taskId;taskId=Guid.NewGuid().ToString("N");
            Start(parent);
            Assert.That(runners.Keys,Is.EquivalentTo(new[]{"1033"}));
        }
    }

    [Test] public void StartupSweepCleansInterruptedAndClearedRunnersAndPreservesOtherPrograms() {
        Start();
        CleanRunners();
        Assert.That(runners.ContainsKey("1032"),Is.True);
        Invoke("HistoryStore","Load",folder,provider);
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("interrupted"));
        runners["1033"]=(JObject)runners["1032"].DeepClone();
        runners["1033"]["Address"]=1033;
        runners["1033"]["ScriptContext"]="manual source";
        installedPrograms["2000"]=new JObject{["Address"]=2000,["Group"]="Automan",["IsRunning"]=false};
        CleanRunners();
        Assert.That(runners.Keys,Is.EquivalentTo(new[]{"1033"}));
        Assert.That(installedPrograms.ContainsKey("2000"),Is.True);

        taskId=Guid.NewGuid().ToString("N");
        runners.Remove("1033");
        Start();
        Invoke("HistoryStore","Replace",folder,provider,Activator.CreateInstance(typeof(List<>).MakeGenericType(assembly.GetType("ChatMessage"))),false);
        CleanRunners();
        Assert.That(runners,Is.Empty);
    }

    [Test] public void UnconfirmedDeploymentRetainsTheRunnerUntilRecoveryIsSettled() {
        Start();replies.Enqueue(Action("save_widget"));replies.Enqueue(Complete);Run();
        goalDeploymentsSettled=false;
        CleanRunners();
        Assert.That(runners.ContainsKey("1032"),Is.True);
        goalDeploymentsSettled=true;
        CleanRunners();
        Assert.That(runners,Is.Empty);
    }

    [TestCase("unknown",false)] [TestCase("compile_failed",false)]
    [TestCase("completed",true)] [TestCase("restored",true)] [TestCase("retained_disabled",true)]
    public void GoalDeploymentCheckReadsOnlySessionsOwnedByThatGoal(string state,bool settled) {
        var sessions=Directory.CreateDirectory(Path.Combine(folder,"program-deployments")).FullName;
        File.WriteAllText(Path.Combine(sessions,Guid.NewGuid().ToString("N")+"-other.json"),"{\"State\":\"unknown\"}");
        Assert.That(Invoke("GoalDeploymentStatus","IsSettled",folder,taskId),Is.True);
        File.WriteAllText(Path.Combine(sessions,taskId+"-step.json"),JsonConvert.SerializeObject(new{State=state}));
        Assert.That(Invoke("GoalDeploymentStatus","IsSettled",folder,taskId),Is.EqualTo(settled));
    }

    [Test] public void LostStartReceiptNeverCreatesOrRunsASecondProgram(){Start();Start();Assert.That(creates,Is.EqualTo(1));Assert.That(runs,Is.EqualTo(1));Assert.That((int)TaskRecord()["runnerProgramId"],Is.EqualTo(1032));}
    [Test] public void DisabledWidgetCapabilityCreatesNothing(){Assert.Throws<TargetInvocationException>(()=>Invoke("AiGenieGoals","Start",StartRequest(false),new Func<string,object,object>(Call)));Assert.That(creates,Is.Zero);}
    [Test] public void SavedWidgetIsReadBackAndLinkedBeforeCompletion(){Start();replies.Enqueue(Action("save_widget"));replies.Enqueue(Complete);Run();Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));Assert.That(((JArray)TaskRecord()["artifacts"]).Count,Is.EqualTo(1));Assert.That(saves,Is.EqualTo(3));}
    [Test] public void SuccessfulGeminiStepsKeepDiagnosticsBesideTheResult() {
        Start();
        geminiDiagnostics=new JObject {["provider"]="Gemini",["model"]="gemini-test",["elapsedMs"]=120,["finishReason"]="STOP",
            ["promptTokenCount"]=100,["candidatesTokenCount"]=24,["thoughtsTokenCount"]=6};
        replies.Enqueue(Action("save_widget"));
        replies.Enqueue(Complete);
        Run();
        var task=TaskRecord();
        Assert.That((string)task["state"],Is.EqualTo("completed"));
        Assert.That((string)task["operations"][0]["details"],Does.Contain("Saved:").And.Contain("model default").And.Contain("Thinking: 6 tokens"));
        Assert.That((string)task["operations"][1]["details"],Does.Contain("Gemini gemini-test, 120 ms").And.Contain("finish STOP"));
    }
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
    [Test] public void AutomationOnlyCompletesWithoutWidgetPermissionAndRejectsWidgetWrites() {
        StartSelected(new JArray("automation"),widgetCapability:false);
        replies.Enqueue(Action("save_widget"));replies.Enqueue(ProgramAction());replies.Enqueue(Complete);Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));
        Assert.That(deployments,Is.EqualTo(1));Assert.That(saves,Is.Zero);
        Assert.That(lastSystemPrompt,Does.Contain("Widget creation is disabled").And.Not.Contain(SaveWidgetContract));
    }
    [Test] public void WidgetOnlyCannotApplyDashboardOrSaveProgramsEvenWithAClientTarget() {
        StartSelected(new JArray("widgets"),dashboardTarget:true);
        replies.Enqueue(Action("save_widget"));replies.Enqueue(ProgramAction());replies.Enqueue(ApplyDashboard);replies.Enqueue(Complete);Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));
        Assert.That(saves,Is.EqualTo(3));Assert.That(deployments,Is.Zero);Assert.That(dashboardApplications,Is.Zero);
        Assert.That(lastSystemPrompt,Does.Contain("do not apply dashboards").And.Contain("save_program is forbidden"));
    }
    [Test] public void DashboardSelectionIncludesWidgetsAndCanCombineWithAutomation() {
        StartSelected(new JArray("dashboard","automation"),dashboardTarget:true);
        replies.Enqueue(ProgramAction());replies.Enqueue(Action("save_widget"));replies.Enqueue(ApplyDashboard);replies.Enqueue(Complete);Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));
        Assert.That(TaskRecord()["artifacts"].Select(item=>(string)item["kind"]),Is.EqualTo(new[]{"program","widget","dashboard"}));
    }
    [Test] public void GeminiCloneUsesItsOwnHistoryAndRegisteredGenerationRoute() {
        string originalConversation=conversation;
        provider="1042";conversation=(string)HistoryCall("Conversation.Get",new JObject())["conversationId"];
        Assert.That(conversation,Is.Not.EqualTo(originalConversation));
        StartSelected(new JArray("automation"));
        replies.Enqueue(ProgramAction());replies.Enqueue(Complete);Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));
        Assert.That((string)job["providerId"],Is.EqualTo("1042"));
        provider="933";
        Assert.That(HistoryCall("Conversation.Get",new JObject())["messages"].Count(),Is.Zero);
    }
    [Test] public void UnregisteredCloneCannotCreateAnObjective() {
        var request=StartRequest();
        request["providerId"]="1042";
        Assert.Throws<TargetInvocationException>(()=>Invoke("AiGenieGoals","Start",request,new Func<string,object,object>(Call)));
        Assert.That(creates,Is.Zero);
    }
    [Test] public void ChatCapabilitiesAndProviderSupportLimitExplicitSelections() {
        savedGoalCapabilities=new JObject{["automation"]=false};
        Assert.Throws<TargetInvocationException>(()=>StartSelected(new JArray("automation")));
        provider="940";
        savedGoalCapabilities=null;
        Assert.Throws<TargetInvocationException>(()=>StartSelected(new JArray("dashboard"),dashboardTarget:true));
        Assert.That(creates,Is.Zero);
    }
    [Test] public void ProgramDeploymentProducesAConfirmedArtifactAndUsesRealIdInContinuation(){
        Start();replies.Enqueue(ProgramAction());replies.Enqueue(Complete);Run();Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));
        Assert.That((string)TaskRecord()["artifacts"][0]["kind"],Is.EqualTo("program"));Assert.That((string)TaskRecord()["artifacts"][0]["id"],Is.EqualTo("2000"));
        Assert.That(lastMessages.ToString(),Does.Contain("ID=2000"));Assert.That(deployments,Is.EqualTo(1));
    }
    void SeedEditorProgram() {
        installedPrograms["1500"]=new JObject{["Address"]=1500,["Group"]="My programs",["Name"]="Existing counter",
            ["Description"]="Original description",["IsEnabled"]=false,["Type"]="csharp",["Data"]="original data",
            ["ScriptSetup"]="Program.Run();",["ScriptSource"]="Program.Emit(\"Test.Value\", 0);",["ScriptContext"]=""};
    }
    JObject StartProgramEdit(string parent=null) {
        var input=StartRequest();
        var envelope=(JObject)input["input"];
        envelope["goalTypes"]=new JArray("automation");
        if(parent==null)envelope["programTarget"]="1500";
        else envelope["parentTaskId"]=parent;
        return JObject.FromObject(Invoke("AiGenieGoals","Start",input,new Func<string,object,object>(Call)));
    }
    [Test] public void EditorProgramKeepsPidAcrossRepairAndRevision() {
        SeedEditorProgram();StartProgramEdit();StartProgramEdit();
        Assert.That(creates,Is.EqualTo(1));
        replies.Enqueue(ProgramAction(key:"replacement"));replies.Enqueue(ProgramAction("BAD",key:"selected-program"));
        replies.Enqueue(ProgramAction(key:"selected-program"));replies.Enqueue(Complete);Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"),TaskRecord().ToString());
        Assert.That((string)TaskRecord()["artifacts"][0]["id"],Is.EqualTo("1500"));
        Assert.That(lastSystemPrompt,Does.Contain("selected-program").And.Contain("Existing counter"));
        string parent=taskId;taskId=Guid.NewGuid().ToString("N");StartProgramEdit(parent);
        replies.Enqueue(ProgramAction("Program.Emit(\"Test.Value\", 2);",key:"selected-program"));replies.Enqueue(Complete);Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("completed"));
        Assert.That(installedPrograms.Keys,Is.EquivalentTo(new[]{"1500"}));
        Assert.That(deployments,Is.EqualTo(3));
        var installed=installedPrograms["1500"];
        Assert.That((string)installed["Name"],Is.EqualTo("Existing counter"));
        Assert.That((string)installed["Group"],Is.EqualTo("My programs"));
        Assert.That((string)installed["Description"],Is.EqualTo("Original description"));
        Assert.That((string)installed["Data"],Is.EqualTo("original data"));
        Assert.That((bool)installed["IsEnabled"],Is.False);
        Assert.That((string)installed["ScriptSource"],Does.Contain(", 2)"));
    }
    [Test] public void PackagedProgramCannotBeSelectedInEditorObjective() {
        SeedEditorProgram();installedPrograms["1500"]["PackageInfo"]=new JObject{["packageId"]="homegenie-example"};
        Assert.Throws<TargetInvocationException>(()=>StartProgramEdit());
        Assert.That(creates,Is.Zero);
    }
    [Test] public void EditorObjectiveDoesNotOverwriteConcurrentSourceChanges() {
        SeedEditorProgram();StartProgramEdit();
        changeOnGenerate=()=>installedPrograms["1500"]["ScriptSource"]="Manual edit";
        replies.Enqueue(ProgramAction(key:"selected-program"));Run();
        Assert.That((string)TaskRecord()["state"],Is.EqualTo("failed"));
        Assert.That((string)installedPrograms["1500"]["ScriptSource"],Is.EqualTo("Manual edit"));
        Assert.That(deployments,Is.Zero);
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
