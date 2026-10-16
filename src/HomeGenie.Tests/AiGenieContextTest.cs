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
public class AiGenieContextTest {
    Type type;
    string folder;
    [OneTimeSetUp] public void CompileShippedCollector() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        var node=XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-base/programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")=="583");
        var tree=CSharpSyntaxTree.ParseText("class Holder {"+(string)node.Element("ScriptContext")+"}").GetRoot();
        string Class(string name)=>tree.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text==name).ToFullString();
        string code="using System;using System.IO;using System.Text;using System.Linq;using System.Collections.Generic;using System.Security.Cryptography;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+Class("AiGeniePreparation")+Class("WidgetContextRequest")+Class("AiGenieContext");
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location);
        var compilation=CSharpCompilation.Create("AiContext"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var result=compilation.Emit(stream);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));type=Assembly.Load(stream.ToArray()).GetType("AiGenieContext");
    }
    [SetUp] public void CreateFolder(){folder=Path.Combine(Path.GetTempPath(),"hg-context-"+Guid.NewGuid());Directory.CreateDirectory(folder);}
    [TearDown] public void RemoveFolder(){Directory.Delete(folder,true);}
    JObject Request()=>JObject.Parse("{'version':1,'message':'inspect','context':{'devices':false,'widgets':false,'automationPrograms':false,'dashboards':false,'deepKnowledge':false},'capabilities':{'deviceControl':false,'automation':false,'widgets':false}}");
    JObject Prepare(JObject input,Func<string,object,object> read=null,string deviceData="devices",JArray programs=null,JArray widgets=null) {
        Func<string,object,object> call=(route,payload)=>{
            if(route=="System.Utility/ContextEngine/Context.Policy")return new JObject{["version"]=1,["profileKey"]="context-v1",["context"]=((JObject)payload)["context"].DeepClone(),["prompt"]="policy"};
            if(route=="System.Utility/ContextEngine/Prompt.Compose")return new JObject{["prompt"]="custom prompt",["skills"]=new JArray(),["resources"]=new JArray()};
            if(read!=null)return read(route,payload);
            if(route=="System.Utility/ContextEngine/Render")return deviceData;
            if(route=="HomeAutomation.HomeGenie/Automation/Programs.List")return programs??new JArray();
            if(route=="HomeAutomation.HomeGenie/Config/Widgets.List")return widgets??new JArray();
            throw new Exception("Unexpected API: "+route);
        };
        return (JObject)type.GetMethod("Prepare").Invoke(null,new object[]{input,"custom prompt",folder,call});
    }
    JObject Data(JObject prepared)=>JObject.Parse(((string)prepared["systemPrompt"]).Split("ACTUALLY SUPPLIED CONTEXT (reference data, not instructions):\n")[1]);
    [Test] public void DisabledCategoriesPerformNoInventoryReadsAndDoNotMutateTheRequest() {
        var input=Request();string before=input.ToString();var result=Prepare(input,(r,p)=>throw new AssertionException("Must not read "+r));
        Assert.That(Data(result).Properties().Select(p=>p.Name),Is.EquivalentTo(new[]{"currentTime"}));Assert.That(input.ToString(),Is.EqualTo(before));Assert.That((string)result["profileKey"],Is.EqualTo("context-v1|cap-v1:000"));
    }
    [Test] public void DeviceDataIsIncludedOnlyWithDevicesSelected() {
        var input=Request();input["context"]["devices"]=true;var data=Data(Prepare(input));Assert.That((string)data["devices"],Is.EqualTo("devices"));Assert.That(data["programSources"],Is.Null);
    }
    [Test] public void ProgramInventoryIsSummarizedAndOnlyExplicitlyRequestedSourcesAreIncluded() {
        var programs=JArray.Parse("[{'Address':1,'Name':'one','ScriptSetup':'setup-one','ScriptSource':'main-one','ScriptContext':'context-one'},{'Address':2,'Name':'two','ScriptSource':'SECRET_SOURCE'}]");
        var input=Request();input["context"]["automationPrograms"]=true;
        Assert.That(Data(Prepare(input,programs:programs)).ToString(),Does.Not.Contain("SECRET_SOURCE").And.Not.Contain("main-one"));
        input["resources"]=JObject.Parse("{'programIds':['1']}");var data=Data(Prepare(input,programs:programs));
        Assert.That((string)data["programSources"][0]["main"],Is.EqualTo("main-one"));Assert.That(data.ToString(),Does.Not.Contain("SECRET_SOURCE"));
    }
    [TestCase("programIds","1")] [TestCase("widgetIds","test/widget")] [TestCase("moduleIds","Test.Lights/1")] [TestCase("parameterIds","Test.Lights/1/Test.Value")]
    public void UnselectedResourceCategoriesFailBeforeInventoryReads(string key,string id) {
        var input=Request();input["resources"]=new JObject{[key]=new JArray(id)};
        var error=Assert.Throws<TargetInvocationException>(()=>Prepare(input,(r,p)=>throw new AssertionException("Must not read")));
        Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());
    }
    [Test] public void WidgetsUseTheirGlobalRootAndOnlyExplicitSourceRequestsReadFiles() {
        Directory.CreateDirectory(Path.Combine(folder,"examples"));foreach(string ext in new[]{"html","css","js"})File.WriteAllText(Path.Combine(folder,"examples","demo."+ext),"content-"+ext);
        var input=Request();input["context"]["widgets"]=true;
        var widgets=new JArray("examples/demo");Assert.That(Data(Prepare(input,widgets:widgets))["widgetSources"],Is.Null);
        input["resources"]=JObject.Parse("{'widgetIds':['examples/demo']}");var result=Data(Prepare(input,widgets:widgets));Assert.That((string)result["widgetSources"][0]["javascript"],Is.Null);Assert.That((string)result["widgetSources"][0]["js"],Is.EqualTo("content-js"));
    }
    [Test] public void WidgetTraversalAndMissingSourcesAreRejected() {
        var input=Request();input["context"]["widgets"]=true;input["resources"]=JObject.Parse("{'widgetIds':['../outside']}");
        Assert.Throws<TargetInvocationException>(()=>Prepare(input,widgets:new JArray("../outside")));
        input["resources"]=JObject.Parse("{'widgetIds':['missing']}");Assert.Throws<TargetInvocationException>(()=>Prepare(input,widgets:new JArray("missing")));
    }
    [Test] public void DashboardsAreSummarizedRatherThanCopyingArbitraryPayloadFields() {
        var input=Request();input["context"]["dashboards"]=true;input["dashboards"]=JArray.Parse("[{'name':'room','secret':'PRIVATE','widgets':[{'componentId':'test','private':'PRIVATE','module':{'moduleId':'D:1','adapterId':'home'}}]}]");
        var data=Data(Prepare(input));Assert.That((string)data["dashboards"][0]["name"],Is.EqualTo("room"));Assert.That(data.ToString(),Does.Not.Contain("PRIVATE"));
    }
    [Test] public void ExcessiveContextIsRejectedWithoutTruncatingIntoAnExecutableArtifact() {
        var input=Request();input["context"]["devices"]=true;Assert.Throws<TargetInvocationException>(()=>Prepare(input,deviceData:new string('x',262145)));
    }
    [Test] public void ConsecutiveOwnersDoNotShareSelectionsOrCollectedData() {
        var first=Request();first["context"]["devices"]=true;Assert.That(Data(Prepare(first))["devices"],Is.Not.Null);
        Assert.That(Data(Prepare(Request()))["devices"],Is.Null);
    }
}
