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
        string method=tree.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(n=>n.Identifier.Text=="ExecuteApiCommands").ToFullString();
        string Extra(string package,string address,params string[] names) {
            var block=XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie",package,"programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")==address);
            var parsed=CSharpSyntaxTree.ParseText("class Holder{"+(string)block.Element("ScriptContext")+"}").GetRoot();
            return string.Join("\n",parsed.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(n=>names.Contains(n.Identifier.Text)));
        }
        string extras=Extra("homegenie-base","583","AiGeniePreparation","AiGenieDeviceActions")+Extra("homegenie-ml-ai","940","LailamaCapabilityPolicy","LailamaConfigurationActions");
        string code="using System;using System.IO;using System.Linq;using System.Text;using System.Collections.Generic;using System.Text.RegularExpressions;using System.Security.Cryptography;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+
            extras+Class("ApiCommandExecution")+Class("ActionLedger")+Class("GenieResponseContract")+Class("ModuleSettingAction")+"public class ExecutorProbe { public FakeData Data=new FakeData();"+method+"}"+"""
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
public static class ConfigurationProbe {
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
     return executor.ExecuteApiCommands(api,commands,(string)input["requestId"]);
   };
   Func<string,object,object> dispatch=(route,payload)=> {
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
    [Test] public void CheckboxesRequireCanonicalValues() {
      probe.GetMethod("Validate").Invoke(null,new object[]{"checkbox",false,true,"Off"});
      Assert.Throws<TargetInvocationException>(()=>probe.GetMethod("Validate").Invoke(null,new object[]{"checkbox",false,true,"false"}));
    }
}
