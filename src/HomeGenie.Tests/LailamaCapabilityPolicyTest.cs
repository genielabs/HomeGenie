using System;
using System.Collections.Generic;
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
public class LailamaCapabilityPolicyTest {
    Type host,policy;
    [OneTimeSetUp] public void CompileRealPolicyAndExecutionMethods() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        var node=XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-ml-ai/programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")=="940");
        var tree=CSharpSyntaxTree.ParseText("class Holder{"+(string)node.Element("ScriptContext")+"}").GetRoot();
        var types=tree.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(n=>new[]{"ApiCommandExecution","LailamaCapabilityPolicy","LailamaConfigurationActions"}.Contains(n.Identifier.Text));
        var methods=tree.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(n=>new[]{"ExecuteResponse","ExtractAndExecute","ExtractResponse","ReadChatSetting","CurrentDeviceContextAllowed"}.Contains(n.Identifier.Text));
        string code="using System;using System.Linq;using System.Collections.Generic;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+string.Join("\n",types)+@"
public class FakeProperty {public string Name;public object Value;}
public class FakeModule {public List<FakeProperty> Properties=new List<FakeProperty>();}
public class FakeOption {public string Value=""On"";}
public class FakeProgram {public FakeModule Module=new FakeModule();public FakeOption Setting=new FakeOption();public FakeOption Option(string name)=>Setting;}
public class FakeApi {public int Calls;public object Call(string route,object value){Calls++;return new JArray();}}
public class ExecutionProbe {
 const string OPTION_SEND_CONTEXT_DATA=""SendContextData"";
 public FakeProgram Program=new FakeProgram();public FakeApi Api=new FakeApi();
 public void Configure(string capabilities,bool master,bool devices) {Program.Setting.Value=master?""On"":""Off"";Program.Module.Properties.Clear();if(capabilities!=null)Program.Module.Properties.Add(new FakeProperty{Name=""AI.Chat.Capabilities"",Value=capabilities});Program.Module.Properties.Add(new FakeProperty{Name=""AI.Chat.Context"",Value=new JObject{[""devices""]=devices}.ToString()});}
 public int Calls()=>Api.Calls;
"+string.Join("\n",methods)+"}";
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location);
        var compilation=CSharpCompilation.Create("LocalCapabilities"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var result=compilation.Emit(stream);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));var assembly=Assembly.Load(stream.ToArray());host=assembly.GetType("ExecutionProbe");policy=assembly.GetType("LailamaCapabilityPolicy");
        Assert.That((string)node.Element("ScriptSetup"),Does.Contain("capabilities=new[]{\"deviceControl\"}"));
    }
    object Probe(string caps="{\"deviceControl\":false}",bool master=true,bool devices=true){var p=Activator.CreateInstance(host);Configure(p,caps,master,devices);return p;}
    void Configure(object p,string caps,bool master=true,bool devices=true)=>host.GetMethod("Configure").Invoke(p,new object[]{caps,master,devices});
    int Calls(object p)=>(int)host.GetMethod("Calls").Invoke(p,null);
    object Execute(object p,string method,string text)=>host.GetMethod(method).Invoke(p,method=="ExecuteResponse"?new object[]{text,"test-request"}:new object[]{text});
    const string On="```api-call\nHomeAutomation.PhilipsHue/3/Control.On\n```";
    [TestCase("ExecuteResponse")] [TestCase("ExtractAndExecute")]
    public void ReadOnlyBlocksRealExecutionEntryPointsBeforeExecutorIo(string method){var p=Probe();var ex=Assert.Throws<TargetInvocationException>(()=>Execute(p,method,On));Assert.That(ex.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(Calls(p),Is.Zero);}
    [TestCase("ExecuteResponse")] [TestCase("ExtractAndExecute")]
    public void EnabledControlPreservesExistingExecutionPath(string method){var p=Probe("{\"deviceControl\":true}");Execute(p,method,On);Assert.That(Calls(p),Is.EqualTo(1));}
    [TestCase("ExecuteResponse")] [TestCase("ExtractAndExecute")]
    public void ReadOnlyDoesNotRejectPlainParameterAnswers(string method){var p=Probe();Execute(p,method,"The timeout is 150 seconds.");Assert.That(Calls(p),Is.EqualTo(1));}
    [Test] public void PermissionIsReReadBeforeDispatchRatherThanTakenFromPromptSnapshot(){var p=Probe("{\"deviceControl\":true}");host.GetMethod("ReadChatSetting").Invoke(p,new object[]{"AI.Chat.Capabilities"});Configure(p,"{\"deviceControl\":false}");Assert.Throws<TargetInvocationException>(()=>Execute(p,"ExecuteResponse",On));Assert.That(Calls(p),Is.Zero);}
    [TestCase(false,true)] [TestCase(true,false)]
    public void DisabledDeviceContextAlsoBlocksControl(bool master,bool devices){var p=Probe("{\"deviceControl\":true}",master,devices);Assert.Throws<TargetInvocationException>(()=>Execute(p,"ExecuteResponse",On));Assert.That(Calls(p),Is.Zero);}
    [TestCase("{\"deviceControl\":\"false\"}")] [TestCase("not json")]
    public void InvalidPersistentSettingFailsClosed(string caps){var p=Probe(caps);Assert.Throws<TargetInvocationException>(()=>Execute(p,"ExecuteResponse",On));Assert.That(Calls(p),Is.Zero);}
    [Test] public void MissingSettingKeepsExistingControlDefault(){var p=Probe(null);Execute(p,"ExecuteResponse",On);Assert.That(Calls(p),Is.EqualTo(1));}
    [Test] public void UnsupportedCapabilitiesCannotBeEnabled(){var result=(JObject)policy.GetMethod("Normalize").Invoke(null,new object[]{JObject.Parse("{deviceControl:false,automation:true,widgets:true}"),true});Assert.That((bool)result["deviceControl"],Is.False);Assert.That((bool)result["automation"],Is.False);Assert.That((bool)result["widgets"],Is.False);}
    [TestCase("ExecuteResponse")] [TestCase("ExtractAndExecute")]
    public void LegacyStructuredActionsCannotBypassReadOnlyMode(string method){var p=Probe();string response="{\"actions\":[{\"type\":\"api-call\",\"target\":{\"domain\":\"HomeAutomation.PhilipsHue\",\"address\":\"3\"},\"command\":\"Control.Off\"}]}";Assert.Throws<TargetInvocationException>(()=>Execute(p,method,response));Assert.That(Calls(p),Is.Zero);}
    [TestCase("ExecuteResponse")] [TestCase("ExtractAndExecute")]
    public void ReadOnlyConfigurationCannotReachEitherExecutor(string method) {
        var p=Probe();string response="```api-call\nHomeAutomation.HomeGenie/Config/Modules.ParameterSet/Test.Lights/1/Test.Timeout/60\n```";
        Assert.Throws<TargetInvocationException>(()=>Execute(p,method,response));Assert.That(Calls(p),Is.Zero);
    }

}
