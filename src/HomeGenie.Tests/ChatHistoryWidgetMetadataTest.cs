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
public class ChatHistoryWidgetMetadataTest {
    Assembly assembly; string folder;
    [OneTimeSetUp] public void CompileShippedHistory() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        var file=Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-base/programs.xml");
        var program=XDocument.Load(file).Root.Elements().Single(n=>(string)n.Element("Address")=="577");
        var tree=CSharpSyntaxTree.ParseText("class Holder {void Run(){"+(string)program.Element("ScriptSource")+"}}");
        var section=tree.GetRoot().DescendantNodes().OfType<SwitchSectionSyntax>().Single(n=>n.Labels.Any(label=>label.ToString().Contains("\"WidgetSaved\"")));
        string code="using System;using System.IO;using System.Linq;using System.Collections.Generic;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+(string)program.Element("ScriptContext")+@"
public class Probe {
    string dataFolder;string storeId=""940"";
    public Probe(string folder,JArray rows){dataFolder=folder;HistoryStore.Stores.Clear();HistoryStore.Stores[storeId]=rows.ToObject<List<ChatMessage>>();HistoryStore.Save(folder,storeId);}
    public object Patch(JObject data){lock(HistoryStore.SyncRoot){var currentHistory=HistoryStore.GetStore(dataFolder,storeId);
"+string.Join("\n",section.Statements)+@"}}
    public JArray Rows()=>JArray.FromObject(HistoryStore.GetStore(dataFolder,storeId));
    public JArray Reload(){HistoryStore.Stores.Clear();return Rows();}
    public object Context()=>HistoryStore.Context(HistoryStore.GetStore(dataFolder,storeId),null);
}";
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
            .Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location).Distinct();
        var compilation=CSharpCompilation.Create("WidgetHistory"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var result=compilation.Emit(stream);
        Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));assembly=Assembly.Load(stream.ToArray());
    }
    [SetUp] public void CreateFolder(){folder=Path.Combine(Path.GetTempPath(),"hg-widget-metadata-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);}
    [TearDown] public void Cleanup(){Directory.Delete(folder,true);}
    object Call(object probe,string method,params object[] args)=>probe.GetType().GetMethod(method).Invoke(probe,args);
    object Probe(bool legacy=false){
        var rows=JArray.Parse("[{'role':'user','parts':[{'text':'question'}]},{'role':'model','parts':[{'text':'widget source'}]}]");
        if(!legacy)rows[1]["metadata"]=JObject.Parse("{'requestId':'turn','outcome':'completed','contextProfileKey':'profile','actions':[{'State':'completed'}]}");
        return Activator.CreateInstance(assembly.GetType("Probe"),folder,rows);
    }
    JObject Request(bool legacy=false)=>new(){["Index"]=1,["ExpectedText"]="widget source",["RequestId"]=legacy?null:"turn",["WidgetId"]="custom/counter"};
    JObject Patch(object probe,JObject request)=>JObject.FromObject(Call(probe,"Patch",request));
    [TestCase(false)] [TestCase(true)] public void MetadataSurvivesDiskReloadAndPreservesSourceAndTelemetry(bool legacy){
        var probe=Probe(legacy);var before=(JArray)Call(probe,"Rows");
        Assert.That((string)Patch(probe,Request(legacy))["Status"],Is.EqualTo("Ok"));
        var restored=(JArray)Call(probe,"Reload");
        Assert.That((string)restored[1]["metadata"]["savedWidget"]["id"],Is.EqualTo("custom/counter"));
        Assert.That(JToken.DeepEquals(before[0],restored[0]),Is.True);
        Assert.That(JToken.DeepEquals(before[1]["parts"],restored[1]["parts"]),Is.True);
        if(!legacy){((JObject)restored[1]["metadata"]).Remove("savedWidget");Assert.That(JToken.DeepEquals(before[1]["metadata"],restored[1]["metadata"]),Is.True);}
    }
    [Test] public void LegacyUiMetadataDoesNotChangeModelContextOrReachTheModel(){
        var probe=Probe(true);var before=JToken.FromObject(Call(probe,"Context"));Patch(probe,Request(true));
        var after=JToken.FromObject(Call(probe,"Context"));
        Assert.That(JToken.DeepEquals(before,after),Is.True);Assert.That(after.ToString(),Does.Not.Contain("savedWidget"));
    }
    [TestCase("text")] [TestCase("index")] [TestCase("requestId")] [TestCase("user")] [TestCase("path")]
    public void StaleOrInvalidRequestsDoNotModifyHistory(string kind){
        var probe=Probe();var request=Request();var before=(JArray)Call(probe,"Rows");
        if(kind=="text")request["ExpectedText"]="other text";
        if(kind=="index")request["Index"]=99;
        if(kind=="requestId")request["RequestId"]="other turn";
        if(kind=="user"){request["Index"]=0;request["ExpectedText"]="question";request.Remove("RequestId");}
        if(kind=="path")request["WidgetId"]="../invalid";
        Assert.That((string)Patch(probe,request)["Status"],Is.EqualTo("Error"));
        Assert.That(JToken.DeepEquals(before,Call(probe,"Reload") as JArray),Is.True);
    }
    [Test] public void FailedSaveRestoresPreviousMetadataInMemory(){
        var probe=Probe();var before=(JArray)Call(probe,"Rows");
        var file=Path.Combine(folder,"history_940.json");File.Delete(file);Directory.CreateDirectory(file);
        Assert.That((string)Patch(probe,Request())["Status"],Is.EqualTo("Error"));
        Assert.That(JToken.DeepEquals(before,Call(probe,"Rows") as JArray),Is.True);
    }
    [Test] public void SaveAsChangesOnlySavedWidgetAndExportImportRetainsIt(){
        var probe=Probe();Patch(probe,Request());var second=Request();second["WidgetId"]="custom/copy";Patch(probe,second);
        var exported=((JArray)Call(probe,"Reload")).ToString(Formatting.None);var imported=JArray.Parse(exported);
        Assert.That((string)imported[1]["metadata"]["savedWidget"]["id"],Is.EqualTo("custom/copy"));
    }
}
