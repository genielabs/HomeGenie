using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace HomeGenie.Tests;
[TestFixture]
public class LailamaConfigurationActionsTest
{
    private Type probe;
    [OneTimeSetUp] public void CompileShippedExecutor() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        var node=XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-base/programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")=="581");
        var tree=CSharpSyntaxTree.ParseText("class Holder {"+(string)node.Element("ScriptContext")+"}").GetRoot();
        string Class(string name)=>tree.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text==name).ToFullString();
        var executorMethods = new[] { "ExecuteApiCommands", "SaveScheduledAction", "ReadScheduleTarget" };
        string method = string.Join("\n", tree.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(node => executorMethods.Contains(node.Identifier.Text)).Select(node => node.ToFullString()));
        string Extra(string package,string address,params string[] names) {
            var block=XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie",package,"programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")==address);
            var parsed=CSharpSyntaxTree.ParseText("class Holder{"+(string)block.Element("ScriptContext")+"}").GetRoot();
            return string.Join("\n",parsed.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(n=>names.Contains(n.Identifier.Text)));
        }
        string extras=Extra("homegenie-base","583","GenieCapabilities","AiGeniePreparation","AiGenieDeviceActions")+Extra("homegenie-ml-ai","940","LailamaCapabilityPolicy","LailamaConfigurationActions");
        string code="using System;using System.IO;using System.Linq;using System.Text;using System.Collections.Generic;using System.Text.RegularExpressions;using System.Security.Cryptography;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+
            extras+Class("ApiCommandExecution")+Class("ActionLedger")+Class("GenieResponseContract")+Class("CodeBlockExtractor")+Class("ModuleSettingAction")+"public class ExecutorProbe { public FakeData Data=new FakeData();"+method+"}"+"""
public class FakeData {public string Folder;public string GetFolder()=>Folder;}
public class FakeApi {
 public int Calls,Reads;public string Mode,Value="30";
 public object Call(string route) {
  if(route.Contains("Modules.Get") && route.EndsWith("/Schema"))return JObject.Parse("{'version':1,'module':{'domain':'Test.Lights','address':'1'},'properties':[],'features':[{'key':'Test.Timeout','fieldType':'slider:30:3600:30:30','present':true,'sensitive':false,'programs':[{'fieldType':'slider:30:3600:30:30','enabled':true}]}]}");
  if(route.Contains("Modules.ParameterGet") && route.EndsWith("/Context")) {
   Reads++;
   return new JObject{["version"]=1,["domain"]="Test.Lights",["address"]="1",["key"]="Test.Timeout",["present"]=true,["value"]=Mode=="stale"&&Reads==2?"90":Mode=="mismatch"&&Reads==3?"90":Value};
  }
  if(!route.StartsWith("HomeAutomation.HomeGenie/Config/Modules.ParameterSet/"))throw new Exception("Unexpected route");
  Calls++;if(Mode=="throw")throw new Exception();if(Mode=="null")return null;
  Value=route.Split('/')[6];return JObject.Parse("{'Status':'OK'}");
 }
}
public class ScheduleApi {
 public JArray Requests = new JArray();
 public object Call(string route) {
  var parts = route.Split('/');
  if (parts[2] == "Programs.Get") return new JObject { ["Address"] = int.Parse(parts[3]), ["Name"] = "Demo scene" };
  if (parts[2] == "Modules.Get") return new JObject {
   ["Domain"] = parts[3], ["Address"] = parts[4], ["Name"] = "Light " + parts[4], ["DeviceType"] = "Light"
  };
  throw new InvalidOperationException(route);
 }
 public object Call(string route, string payload) {
  if (!route.StartsWith("HomeAutomation.HomeGenie/Automation/Scheduling.Update/"))
   throw new InvalidOperationException(route);
  var request = JObject.Parse(payload);
  Requests.Add(request);
  return new JObject { ["Status"] = "Ok" };
 }
}
public static class ConfigurationProbe {
 public static JObject ExtractScheduleDescription(string response) {
   var segments = CodeBlockExtractor.ExtractInterleaved(response, out var descriptions);
   return new JObject { ["reply"] = string.Join("\n", segments), ["commands"] = new JArray(CodeBlockExtractor.ExtractCommands(segments)), ["descriptions"] = descriptions };
 }
 public static JObject Schedule(string[] commands, string[] descriptions) {
  string folder = Path.Combine(Path.GetTempPath(), "hg-schedule-ledger-" + Guid.NewGuid());
  Directory.CreateDirectory(folder);
  try {
   var executor = new ExecutorProbe();
   executor.Data.Folder = folder;
   var api = new ScheduleApi();
   var received = new DateTime(2026, 12, 25, 7, 30, 42, DateTimeKind.Utc);
   var response = new JObject { ["version"] = 1, ["kind"] = "device", ["explanation"] = "Scheduled action", ["commands"] = new JArray(commands) };
   if (descriptions != null) response["scheduleDescriptions"] = new JArray(descriptions);
   var dispatched = AiGenieDeviceActions.Dispatch(new JObject {
    ["version"] = 1, ["requestId"] = "request", ["response"] = response, ["receivedAtUtc"] = received,
    ["capabilities"] = new JObject { ["deviceControl"] = true }, ["context"] = new JObject { ["devices"] = true }
   }, (route, payload) => {
    if (route == "System.Utility/ActionExecutor/Response.Normalize") return GenieResponseContract.Normalize((JObject)payload);
    if (route != "System.Utility/ActionExecutor/Execute") throw new InvalidOperationException(route);
    var input = JObject.Parse((string)payload);
    var routes = CodeBlockExtractor.ExtractCommands(CodeBlockExtractor.ExtractInterleaved((string)input["response"]));
    return executor.ExecuteApiCommands(api, routes, (string)input["requestId"], input["receivedAtUtc"].Value<DateTime>(), input["scheduleDescriptions"] as JArray);
   }, (domain, address) => domain == "Test.Lights");
   var results = dispatched["results"];
   var replay = executor.ExecuteApiCommands(api, commands, "request", received);
   return new JObject { ["requests"] = api.Requests, ["results"] = results, ["replay"] = JArray.FromObject(replay) };
  } finally { Directory.Delete(folder, true); }
 }
 public static JObject Run(string mode,string value) {
  string folder=Path.Combine(Path.GetTempPath(),"hg-config-ledger-"+Guid.NewGuid());Directory.CreateDirectory(folder);
  try {
   var executor=new ExecutorProbe();executor.Data.Folder=folder;var api=new FakeApi{Mode=mode};
   string command="HomeAutomation.HomeGenie/Config/Modules.ParameterSet/Test.Lights/1/Test.Timeout/"+value;
   string response="```api-call\n"+command+"\n```\nChanged successfully.";
   var caps=new JObject{["deviceControl"]=true,["automation"]=false,["widgets"]=false};
   Func<string,object,object> execute=(route,payload)=> {
     if(route=="System.Utility/ActionExecutor/Response.Normalize")return GenieResponseContract.Normalize((JObject)payload);
     if(route!="System.Utility/ActionExecutor/Execute")throw new Exception("Unexpected executor route");
     var input=JObject.Parse((string)payload);
     var commands=((string)input["response"]).Split('\n').Skip(1).Where(x=>x!="```");
     return executor.ExecuteApiCommands(api,commands,(string)input["requestId"], scheduleDescriptions: input["scheduleDescriptions"] as JArray);
   };
   Func<string,object,object> dispatch=(route,payload)=> {
     if(route=="System.Utility/AiGenie/Capabilities.Normalize")return GenieCapabilities.Request((JObject)payload,"normalize",null);
     if(route!="System.Utility/AiGenie/Device.Execute")throw new Exception("Bypassed shared dispatcher");
     return AiGenieDeviceActions.Dispatch((JObject)payload,execute,(d,a)=>d=="Test.Lights"&&a=="1");
   };
   var result=(JArray)LailamaConfigurationActions.Execute(response,"request",caps,true,dispatch);
   var replay=(JArray)LailamaConfigurationActions.Execute(response,"request",caps,true,dispatch);
   string reply=LailamaConfigurationActions.VerifiedReply(response,result);
   return new JObject{["result"]=result[0],["replay"]=replay[0],["calls"]=api.Calls,["reply"]=reply,["reads"]=api.Reads};
  } finally {Directory.Delete(folder,true);}
 }
 public static void Validate(string type,bool sensitive,bool active,string value) {
  var description=JObject.Parse("{'version':1,'module':{'domain':'Test.Lights','address':'1'},'properties':[],'features':[{'key':'Test.Timeout','fieldType':'slider:30:3600:30:30','present':true,'sensitive':false,'programs':[{'fieldType':'slider:30:3600:30:30','enabled':true}]}]}");
  description["features"][0]["sensitive"]=sensitive;description["features"][0]["programs"][0]["fieldType"]=type;description["features"][0]["programs"][0]["enabled"]=active;
  ModuleSettingAction.Validate(description,"Test.Lights","1","Test.Timeout",value);
 }
}

""";
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location).Distinct();
        var compilation=CSharpCompilation.Create("ConfigurationExecutor"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var result=compilation.Emit(stream);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));
        probe=Assembly.Load(stream.ToArray()).GetType("ConfigurationProbe");
    }
    private JObject Run(string mode="normal",string value="60")=>(JObject)probe.GetMethod("Run").Invoke(null,new object[]{mode,value});
    [Test] public void StandardSetIsVerifiedAndRecordedAndReplayNeverRepeatsWrite() {
        var result=Run();Assert.That((bool)result["result"]["Success"],Is.True,result.ToString());Assert.That((string)result["result"]["Response"]["previousValue"],Is.EqualTo("30"));
        Assert.That((string)result["result"]["Response"]["value"],Is.EqualTo("60"));Assert.That((bool)result["replay"]["Replayed"],Is.True);Assert.That((int)result["calls"],Is.EqualTo(1));Assert.That((int)result["reads"],Is.EqualTo(3));
    }
    [TestCase("0")] [TestCase("31")] [TestCase("3630")] 
    public void InvalidValuesCannotWrite(string value) {var r=Run(value:value);Assert.That((bool)r["result"]["Success"],Is.False);Assert.That((int)r["calls"],Is.Zero);}
    [Test] public void ConcurrentChangeBeforeWriteFailsWithoutSideEffects() {var r=Run("stale");Assert.That((string)r["result"]["State"],Is.EqualTo("failed"));Assert.That((int)r["calls"],Is.Zero);}
    [TestCase("throw")] [TestCase("null")] [TestCase("mismatch")]
    public void UncertainWritesNeverAdvertiseSuccessOrRepeat(string mode) {var r=Run(mode);Assert.That((string)r["result"]["State"],Is.EqualTo("unknown"));Assert.That((int)r["calls"],Is.EqualTo(1));Assert.That((string)r["reply"],Does.StartWith("Configuration was not confirmed."));}
    [TestCase("password",false,true,"60")] [TestCase("text",false,true,"60")] [TestCase("slider:30:3600:30",true,true,"60")] [TestCase("slider:30:3600:30",false,false,"60")]
    public void UnsupportedSensitiveAndInactiveSettingsAreRejected(string type,bool sensitive,bool active,string value) {
      Assert.Throws<TargetInvocationException>(()=>probe.GetMethod("Validate").Invoke(null,new object[]{type,sensitive,active,value}));
    }
    [Test]
    public void TextScheduleDescriptionsStayOutOfTheChatReply()
    {
        const string command = "Test.Lights/1/Control.Off/once-in/300";
        var response = "Spegnimento programmato.\n\n```api-call\n" + command + "\n```\n\n```schedule-descriptions\n[\"Spegni\"]\n```";
        var outcome = (JObject)probe.GetMethod("ExtractScheduleDescription").Invoke(null, new object[] { response });
        Assert.That(outcome["commands"].Values<string>(), Is.EqualTo(new[] { command }));
        Assert.That(outcome["descriptions"].Values<string>(), Is.EqualTo(new[] { "Spegni" }));
        Assert.That((string)outcome["reply"], Is.EqualTo("Spegnimento programmato.\n```api-call\n" + command + "\n```"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void OneTimeCommandsShareAScheduleAndReplayPerDevice(bool withDescriptions)
    {
        string[] commands = {
            "Test.Lights/1/Control.Off/once-in/300", "Test.Lights/2/Control.Off/once-in/300",
            "Test.Lights/3/Control.Off/once-in/300", "Test.Lights/3/Control.On/once-in/300",
            "Test.Lights/4/Control.On/once-in/600"
        };
        var outcome = (JObject)probe.GetMethod("Schedule").Invoke(null, new object[] { commands, withDescriptions ? new[] { "Spegni", "Spegni", "Spegni", "Accendi", "Allumer" } : null });
        var requests = (JArray)outcome["requests"];
        var results = (JArray)outcome["results"];
        Assert.That(requests, Has.Count.EqualTo(3));
        Assert.That(requests[0]["BoundModules"].Select(module => ((string)module["Domain"], (string)module["Address"])),
            Is.EqualTo(new[] { ("Test.Lights", "1"), ("Test.Lights", "2"), ("Test.Lights", "3") }));
        Assert.That(requests.Select(request => (string)request["Description"]),
            Is.EqualTo(withDescriptions ? new[] { "Spegni", "Accendi", "Allumer" } : new[] { "Turn off", "Turn on", "Turn on" }));
        Assert.That((string)requests[0]["Script"], Is.EqualTo("$$.boundModules.off();"));
        Assert.That(requests[0]["BoundDevices"].Values<string>(), Is.EquivalentTo(new[] { "Light", "Dimmer", "Color", "Switch" }));
        Assert.That((string)requests[1]["Script"], Is.EqualTo("$$.boundModules.on();"));
        Assert.That(requests[1]["BoundModules"].Count(), Is.EqualTo(1));
        var expected = new DateTime(2026, 12, 25, 7, 41, 0, DateTimeKind.Utc).ToLocalTime();
        var data = JObject.Parse((string)requests[2]["Data"]);
        Assert.That((string)requests[2]["CronExpression"], Is.EqualTo(
            expected.ToString("m H d M", System.Globalization.CultureInfo.InvariantCulture) + " *"));
        Assert.That((int)data["itemType"], Is.EqualTo(4));
        Assert.That((string)data["from"], Is.EqualTo(expected.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)));
        Assert.That(data["to"], Is.EqualTo(data["from"]));
        Assert.That((string)data["time"][0]["start"], Is.EqualTo(expected.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture)));
        Assert.That(data["time"][0]["end"], Is.EqualTo(data["time"][0]["start"]));
        Assert.That(results.Select(result => (string)result["Command"]), Is.EqualTo(commands));
        Assert.That(results.All(result => (bool)result["Success"]), Is.True);
        Assert.That((string)requests[0]["Name"], Is.EqualTo("Once.request.0"));
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        var groupedTime = expected.AddMinutes(-5).ToString("yyyy-MM-dd HH:mm:ss zzz", culture);
        Assert.That(results.Take(3).Select(result => (string)result["ScheduledLocalTime"]).Distinct(), Is.EqualTo(new[] { groupedTime }));
        Assert.That((string)results[4]["ScheduledLocalTime"], Is.EqualTo(expected.ToString("yyyy-MM-dd HH:mm:ss zzz", culture)));
        var replay = (JArray)outcome["replay"];
        Assert.That(replay.All(result => (bool)result["Replayed"]), Is.True);
        Assert.That(replay.Select(result => (string)result["ScheduledLocalTime"]), Is.EqualTo(results.Select(result => (string)result["ScheduledLocalTime"])));
    }

    [TestCase("Test.Lights/1/Control.Off")]
    [TestCase("Test.Lights/1/Control.Toggle")]
    [TestCase("HomeAutomation.HomeGenie/Automation/Programs.Run/1036")]
    public void OneTimeDelayUsesTheTurnStartAndPreservesTheTarget(string route)
    {
        var receivedAt = new DateTime(2026, 12, 25, 7, 30, 42, DateTimeKind.Utc);
        object[] arguments = { route + "/once-in/300", receivedAt, null };
        var parse = probe.Assembly.GetType("GenieResponseContract").GetMethod("OneTimeCommand");
        Assert.That(parse.Invoke(null, arguments), Is.EqualTo(route));
        Assert.That(arguments[2], Is.EqualTo(receivedAt.AddMinutes(5)));
    }

    [TestCase("Test.Lights/1/Control.Off/once-in/0")]
    [TestCase("Test.Lights/1/Control.Level/once-in/60")]
    [TestCase("Test.Lights/1/Control.Off/once-at/2026-02-30T07:00")]
    public void InvalidOneTimeCommandsAreRejected(string route)
    {
        var parse = probe.Assembly.GetType("GenieResponseContract").GetMethod("OneTimeCommand");
        Assert.Throws<TargetInvocationException>(() => parse.Invoke(null, new object[] { route, DateTime.UtcNow, null }));
    }

    [Test] public void CheckboxesRequireCanonicalValues() {
      probe.GetMethod("Validate").Invoke(null,new object[]{"checkbox",false,true,"Off"});
      Assert.Throws<TargetInvocationException>(()=>probe.GetMethod("Validate").Invoke(null,new object[]{"checkbox",false,true,"false"}));
    }
}
