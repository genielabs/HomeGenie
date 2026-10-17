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
public class ChatHistoryDeletionTest {
    Assembly assembly;string folder;
    [OneTimeSetUp] public void CompileShippedDeletionPaths() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        string packages=Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie");
        var history=XDocument.Load(Path.Combine(packages,"homegenie-base/programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")=="577");
        var historyTree=CSharpSyntaxTree.ParseText("class Holder {void Run(){"+(string)history.Element("ScriptSource")+"}}").GetRoot();
        var section=historyTree.DescendantNodes().OfType<SwitchSectionSyntax>().Single(n=>n.Labels.Any(l=>l.ToString()=="case \"RemoveAt\":"));
        string code="using System;using System.IO;using System.Linq;using System.Collections.Generic;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+(string)history.Element("ScriptContext")+@"
public enum Status {Error}
public class ResponseStatus {public Status Status;public string Message;public ResponseStatus(Status status,string message){Status=status;Message=message;}}
public class StorageProbe {
 string dataFolder,storeId=""test"";
 public StorageProbe(string folder){dataFolder=folder;}
 public void Seed(){HistoryStore.Stores.Clear();HistoryStore.Stores[storeId]=new List<ChatMessage>{ChatMessage.Create(""user"",""first""),ChatMessage.Create(""model"",""second"")};HistoryStore.Save(dataFolder,storeId);}
 public int MemoryCount()=>HistoryStore.GetStore(dataFolder,storeId).Count;
 public int Reload(){HistoryStore.Stores.Clear();return MemoryCount();}
 public object Remove(int target){var data=new JObject{[""Index""]=target};var currentHistory=HistoryStore.GetStore(dataFolder,storeId);
"+string.Join("\n",section.Statements)+"}}";
        var ai=XDocument.Load(Path.Combine(packages,"homegenie-ml-ai/programs.xml"));
        foreach(string id in new[]{"933","940"}) {
            string source=(string)ai.Root.Elements().Single(n=>(string)n.Element("Address")==id).Element("ScriptSource");
            var tree=CSharpSyntaxTree.ParseText("class Holder{void Run(){"+source+"}}").GetRoot();
            var handler=tree.DescendantNodes().OfType<InvocationExpressionSyntax>().Single(n=>n.Expression.ToString()=="Api.Handle" && n.ArgumentList.Arguments[0].ToString().Contains("API_HISTORY_REMOVE_AT"));
            var lambda=(LambdaExpressionSyntax)handler.ArgumentList.Arguments[1].Expression;
            code+="public class Wrapper"+id+" {public FakeApi Api=new FakeApi();public FakeCache sessionCache=new FakeCache();string storeId=\""+id+"\";public object Remove(object i)"+lambda.Body.ToFullString()+"}";
        }
        code+=@"
public class FakeCache {public bool Invalidated;public void Invalidate(){Invalidated=true;}}
public class FakeApi {
 public object LastData;public string LastRoute;
 public MIG.MigInterfaceCommand Parse(object i)=>i as MIG.MigInterfaceCommand ?? new MIG.MigInterfaceCommand(i.ToString());
 public object Call(string route,object data){LastRoute=route;LastData=data;return new{Count=1};}
}";
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location).Append(typeof(MIG.MigInterfaceCommand).Assembly.Location);
        var compilation=CSharpCompilation.Create("HistoryRemoval"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var result=compilation.Emit(stream);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));assembly=Assembly.Load(stream.ToArray());
    }
    [SetUp] public void CreateFolder(){folder=Path.Combine(Path.GetTempPath(),"hg-history-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);}
    [TearDown] public void Cleanup(){if(Directory.Exists(folder))Directory.Delete(folder,true);}
    object Storage(){var p=Activator.CreateInstance(assembly.GetType("StorageProbe"),folder);p.GetType().GetMethod("Seed").Invoke(p,null);return p;}
    object Call(object p,string method,params object[] args)=>p.GetType().GetMethod(method).Invoke(p,args);
    [Test] public void DeletedMessageStaysDeletedAfterReloadFromDisk(){var p=Storage();Assert.That((int)JObject.FromObject(Call(p,"Remove",0))["Count"],Is.EqualTo(1));Assert.That((int)Call(p,"Reload"),Is.EqualTo(1));Assert.That(File.ReadAllText(Path.Combine(folder,"history_test.json")),Does.Not.Contain("first"));}
    [Test] public void InvalidIndexDoesNotModifyPersistedHistory(){var p=Storage();string before=File.ReadAllText(Path.Combine(folder,"history_test.json"));Assert.That(JObject.FromObject(Call(p,"Remove",9))["Message"],Is.Not.Null);Assert.That(File.ReadAllText(Path.Combine(folder,"history_test.json")),Is.EqualTo(before));}
    [Test] public void FailedSaveReturnsErrorAndRestoresInMemoryMessage(){var p=Storage();string file=Path.Combine(folder,"history_test.json");File.Delete(file);Directory.CreateDirectory(file);Assert.That((string)JObject.FromObject(Call(p,"Remove",0))["Message"],Is.EqualTo("Could not save chat history."));Assert.That((int)Call(p,"MemoryCount"),Is.EqualTo(2));}
    [TestCase("933")] [TestCase("940")]
    public void WrapperExtractsNumericIndexFromRealMigTail(string id){var p=Activator.CreateInstance(assembly.GetType("Wrapper"+id));Call(p,"Remove",new MIG.MigInterfaceCommand("1"));var api=p.GetType().GetField("Api").GetValue(p);var data=api.GetType().GetField("LastData").GetValue(api);Assert.That((int)JObject.FromObject(data)["Index"],Is.EqualTo(1));Assert.That((string)JObject.FromObject(data)["StoreId"],Is.EqualTo(id));}
    [TestCase("-1")] [TestCase("bad")] [TestCase("1/extra")]
    public void InvalidTailNeverCallsStorage(string tail){var p=Activator.CreateInstance(assembly.GetType("Wrapper940"));Call(p,"Remove",new MIG.MigInterfaceCommand(tail));var api=p.GetType().GetField("Api").GetValue(p);Assert.That(api.GetType().GetField("LastData").GetValue(api),Is.Null);}
}
