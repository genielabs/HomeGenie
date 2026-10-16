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
public class GeminiDiscoveryContractTest
{
    private Type request, contract, executorContract, probe;
    [OneTimeSetUp] public void CompileDistributedContractsAndLookupBlock()
    {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null && !File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        Assert.That(root,Is.Not.Null);
        XElement Program(string package,string id)=>XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie",package,"programs.xml"))
            .Root.Elements("ProgramBlock").Single(n=>(string)n.Element("Address")==id);
        var gemini=Program("homegenie-ml-ai","933");var executor=Program("homegenie-base","581");
        string Class(XElement p,string name)=>CSharpSyntaxTree.ParseText("class Holder {"+(string)p.Element("ScriptContext")+"}").GetRoot()
            .DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text==name).ToFullString();
        string source=(string)gemini.Element("ScriptSource");
        int begin=source.IndexOf("    var moduleIds = resources[",StringComparison.Ordinal);
        int end=source.IndexOf("    if ((bool)selected[\"automationPrograms\"])",begin,StringComparison.Ordinal);
        string block=source.Substring(begin,end-begin);
        var code="using System;using System.IO;using System.Text;using System.Linq;using System.Collections.Generic;using Newtonsoft.Json;using Newtonsoft.Json.Linq;\n"+
            Class(gemini,"WidgetContextRequest")+Class(gemini,"GenieResponseContract")+
            "namespace Executor {"+Class(executor,"GenieResponseContract")+"}\n"+$$$"""
public static class DiscoveryLookupProbe {
 public static JObject Run(JObject resources,bool allowed,bool error) {
    var selected=JObject.FromObject(new {devices=allowed});var data=new JObject();var Api=new DiscoveryApi(error);
    Func<object,JToken> contextToken=value=>value is string text?JToken.Parse(text):JToken.FromObject(value);
{{{block}}}
    return new JObject { ["data"]=data,["calls"]=Api.Calls };
 }
}
public class DiscoveryApi {
 public int Calls;readonly bool error;public DiscoveryApi(bool error){this.error=error;}
 public object Call(string route) {
   Calls++;
   if(!(route.StartsWith("HomeAutomation.HomeGenie/Config/Modules.Get/") && route.EndsWith("/Schema")) &&
      !(route.StartsWith("HomeAutomation.HomeGenie/Config/Modules.ParameterGet/")&&route.EndsWith("/Context")))
       throw new Exception("Unexpected non-discovery API");
   if(error)return new JObject{["Status"]="Error",["Message"]="Sensitive parameters are not available to AI context."};
   if(route.Contains("Legacy.Lights"))return new JObject{["Name"]="ApiKey",["Value"]="PRIVATE"};
   var parts=route.Split('/');
   if(route.Contains("Old.Schema"))return new JObject{["version"]=1,["module"]=new JObject{["domain"]=parts[3],["address"]=parts[4]},["properties"]=new JArray()};
   if(route.Contains("Modules.Get"))return new JObject{["version"]=1,["module"]=new JObject{["domain"]=parts[3],["address"]=parts[4]},["properties"]=new JArray(),["features"]=new JArray()};
   return new JObject{["version"]=1,["domain"]=parts[3],["address"]=parts[4],["key"]=parts[5],["present"]=true,["value"]="100"};
 }
}
""";
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location))
            .Append(typeof(JsonConvert).Assembly.Location).Distinct();
        var compilation=CSharpCompilation.Create("DiscoveryContract"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},
            paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var result=compilation.Emit(stream);
        Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));
        var assembly=Assembly.Load(stream.ToArray());request=assembly.GetType("WidgetContextRequest");contract=assembly.GetType("GenieResponseContract");
        executorContract=assembly.GetType("Executor.GenieResponseContract");probe=assembly.GetType("DiscoveryLookupProbe");
    }
    private JObject Run(JObject resources,bool allowed=true,bool error=false)=>(JObject)probe.GetMethod("Run").Invoke(null,new object[]{resources,allowed,error});
    [Test] public void LookupsUseOnlyDescriptiveAndContextualReadApis()
    {
        var result=Run(JObject.Parse("{'moduleIds':['Test.Lights/1'],'parameterIds':['Test.Lights/1/Test.Feature']}"));
        Assert.That((int)result["calls"],Is.EqualTo(2));Assert.That(result["data"]["moduleDescriptions"].Count(),Is.EqualTo(1));
        Assert.That(result["data"]["moduleParameters"].Count(),Is.EqualTo(1));
    }
    [Test] public void DisabledDevicesContextRejectsReadsBeforeCallingApis()
    {
        var error=Assert.Throws<TargetInvocationException>(()=>Run(JObject.Parse("{'moduleIds':['Test.Lights/1']}"),false));
        Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(error.InnerException.Message,Does.Contain("Enable Devices context"));
    }
    [Test] public void ReadErrorsAreNotForwardedAsSuccessfulContext()
    {
        var error=Assert.Throws<TargetInvocationException>(()=>Run(JObject.Parse("{'parameterIds':['Test.Lights/1/ApiKey']}"),error:true));
        Assert.That(error.InnerException.Message,Does.Contain("Sensitive parameters"));
    }
    [Test] public void OlderBackendRawParameterResponseCannotLeakIntoModelContext()
    {
        var error=Assert.Throws<TargetInvocationException>(()=>Run(JObject.Parse("{'parameterIds':['Legacy.Lights/1/ApiKey']}")));
        Assert.That(error.InnerException.Message,Does.Contain("Update HomeGenie").And.Not.Contain("PRIVATE"));
    }
    [TestCase("Test.Lights/1/extra")] [TestCase("Test.Lights/1%2Fextra")] [TestCase("Test.Lights/1?query")]
    public void MalformedModuleIdsCannotInjectApiPaths(string id)
    { Assert.Throws<TargetInvocationException>(()=>Run(new JObject{["moduleIds"]=new JArray(id)})); }
    [Test] public void BothDistributedNormalizersAcceptDiscoveryAndRejectUnknownResources()
    {
        foreach(var type in new[]{contract,executorContract}) {
            var value=JObject.Parse("{'version':1,'kind':'context','explanation':'Inspect','resources':{'moduleIds':['Test.Lights/1'],'parameterIds':['Test.Lights/1/Test.Feature']}} ");
            Assert.That(type.GetMethod("Normalize").Invoke(null,new object[]{value}),Is.Not.Null);
            value["resources"]["writeParameters"]=new JArray("forbidden");
            Assert.Throws<TargetInvocationException>(()=>type.GetMethod("Normalize").Invoke(null,new object[]{value}));
            var schema=(JObject)type.GetMethod("Schema").Invoke(null,null);
            Assert.That(schema.ToString(),Does.Contain("moduleIds").And.Contain("parameterIds"));
        }
    }
    [Test] public void StructuredRequestAcceptsDiscoveryButNotWriteInstructions()
    {
        var input=JObject.Parse("{'version':1,'message':'Inspect','resources':{'moduleIds':['Test.Lights/1']}}");
        request.GetMethod("Validate").Invoke(null,new object[]{input});
        input["resources"]["parameterSet"]=new JArray("forbidden");
        Assert.Throws<TargetInvocationException>(()=>request.GetMethod("Validate").Invoke(null,new object[]{input}));
    }
    [Test] public void ConfigurationCommandsRequireExactTargetsScalarValuesAndRejectExtraOptions() {
        foreach(var type in new[]{contract,executorContract}) {
            var method=type.GetMethod("DeviceCommand");
            string prefix="HomeAutomation.HomeGenie/Config/Modules.ParameterSet/Test.Lights/1/Test.Timeout/";
            Assert.That((string[])method.Invoke(null,new object[]{prefix+"60"}),Is.EqualTo(new[]{"Test.Lights","1"}));
            foreach(string command in new[]{prefix+"60/30",prefix+"60/30/extra",prefix+"60/%33%30",prefix+"NaN/30",prefix+"60/30?query",
                "HomeAutomation.HomeGenie/Config/Modules.ParameterConfigure/Test.Lights/1/Test.Timeout/60/30", "x"})
                Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,new object[]{command}));
        }
    }
    [Test] public void OlderNestedPropertySchemaIsRejectedBeforeItEntersContext() {
        var error=Assert.Throws<TargetInvocationException>(()=>Run(JObject.Parse("{'moduleIds':['Old.Schema/1']}")));
        Assert.That(error.InnerException.Message,Does.Contain("Update HomeGenie"));
    }
}
