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
public class LailamaAbortRecoveryTest {
    Type probe;
    [OneTimeSetUp] public void CompileShippedRecovery() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        var block=XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-ml-ai/programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")=="940");
        var method=CSharpSyntaxTree.ParseText("class Holder{"+(string)block.Element("ScriptContext")+"}").GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(n=>n.Identifier.Text=="RecoverFailedProgram").ToFullString();
        string code="using System;using System.Linq;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+"""
public class FakeApi {
 public string Mode;public int Recoveries;
 public object Call(string route,object input) {
  if(route.EndsWith("/Results")) {
   if(Mode=="null")return null;
   if(Mode=="empty")return new JArray();
   var row=new JObject{["RequestId"]="turn",["ActionId"]="turn:deploy-1",["Index"]=1,["State"]=Mode=="completed"?"completed":Mode=="unknown"?"unknown":"failed",["Response"]=new JObject{["status"]="compile_failed"}};
   var rows=new JArray(row);
   if(Mode=="newer-unknown")rows.Add(new JObject{["RequestId"]="turn",["ActionId"]="turn:deploy-2",["Index"]=2,["State"]="unknown"});
   if(Mode=="recovered")rows.Add(new JObject{["RequestId"]="turn",["ActionId"]="turn:recover",["State"]="completed"});
   if(Mode=="unrelated")row["RequestId"]="other";
   return rows;
  }
  if(route!="System.Utility/ActionExecutor/Program.Recover")throw new Exception("Unexpected route");
  Recoveries++;if(Mode=="throw")throw new Exception("Failed to recover");
  return new JObject{["RequestId"]="turn",["Success"]=Mode!="unconfirmed"};
 }
}
public class RecoveryProbe {
 public FakeApi Api=new FakeApi();
"""+method+"public int Calls()=>Api.Recoveries;public void Configure(string mode){Api.Mode=mode;} }";
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location);
        var compilation=CSharpCompilation.Create("AbortRecovery"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var result=compilation.Emit(stream);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics.Where(d=>d.Severity==DiagnosticSeverity.Error)));probe=Assembly.Load(stream.ToArray()).GetType("RecoveryProbe");
    }
    (JObject,int) Run(string mode) {var host=Activator.CreateInstance(probe);probe.GetMethod("Configure").Invoke(host,new object[]{mode});var result=(JObject)probe.GetMethod("RecoverFailedProgram").Invoke(host,new object[]{"turn"});return (result,(int)probe.GetMethod("Calls").Invoke(host,null));}
    [TestCase("null")] [TestCase("empty")] [TestCase("completed")] [TestCase("unknown")] [TestCase("newer-unknown")] [TestCase("recovered")] [TestCase("unrelated")]
    public void OnlyTheLatestConfirmedCompilationFailureCanBeRecovered(string mode) {var (result,calls)=Run(mode);Assert.That(calls,Is.Zero);Assert.That(result.HasValues,Is.False);}
    [Test] public void FailedCompilationRecoversOnceAndReportsIt() {var (result,calls)=Run("failed");Assert.That(calls,Is.EqualTo(1));Assert.That((bool)result["success"],Is.True);}
    [TestCase("throw")] [TestCase("unconfirmed")]
    public void FailedOrUncertainRecoveryIsNotRetried(string mode) {var (result,calls)=Run(mode);Assert.That(calls,Is.EqualTo(1));Assert.That((bool)result["success"],Is.False);Assert.That((string)result["message"],Does.Contain("not confirmed"));}
}
