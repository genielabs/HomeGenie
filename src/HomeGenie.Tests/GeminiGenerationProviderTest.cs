using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace HomeGenie.Tests;
[TestFixture]
public class GeminiGenerationProviderTest {
    Type type;
    Type serviceType;
    [OneTimeSetUp] public void CompileShippedAdapter() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        var node=XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-ml-ai/programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")=="933");
        var tree=CSharpSyntaxTree.ParseText("class Holder {"+(string)node.Element("ScriptContext")+"}").GetRoot();
        var adapter=tree.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text=="GeminiGenerationProvider");
        var service=XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-base/programs.xml")).Root.Elements().Single(n=>(string)n.Element("Address")=="583");
        string code="using System;using System.Linq;using System.Collections.Generic;using System.Threading;using Newtonsoft.Json.Linq;"+adapter.ToFullString()+CSharpSyntaxTree.ParseText("class Holder {"+(string)service.Element("ScriptContext")+"}").GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text=="AiGenieService").ToFullString();
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location);
        var compilation=CSharpCompilation.Create("GeminiProvider"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var result=compilation.Emit(stream);Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));
        var assembly=Assembly.Load(stream.ToArray());type=assembly.GetType("GeminiGenerationProvider");serviceType=assembly.GetType("AiGenieService");
    }
    Dictionary<string,object> Request(CancellationToken token=default)=>new(){["version"]=1,["requestId"]="request",["messages"]=new object(),["cancellationToken"]=token};
    JObject Completion(string finish="STOP")=>JObject.Parse("{'candidates':[{'finishReason':'"+finish+"','content':{'parts':[{'text':'private thought','thought':true},{'text':'visible '},{'text':'answer'},{'inlineData':{}}]}}]}");
    IDictionary<string,object> Generate(object request,Func<object,CancellationToken,JObject> send)=>(IDictionary<string,object>)type.GetMethod("Generate").Invoke(null,new[]{request,send});
    [Test] public void PayloadPreservesMessagesSchemaAndSystemPromptAndFiltersThoughts() {
        var request=Request();var output=new object();request["output"]=output;request["systemPrompt"]="instructions";
        var result=Generate(request,(body,token)=>{var data=(IDictionary<string,object>)body;
            Assert.That(data["contents"],Is.SameAs(request["messages"]));Assert.That(data["generationConfig"],Is.SameAs(output));
            Assert.That(JObject.FromObject(body)["system_instruction"]["parts"][0]["text"].Value<string>(),Is.EqualTo("instructions"));return Completion();});
        Assert.That(result["state"],Is.EqualTo("completed"));Assert.That(result["text"],Is.EqualTo("visible answer"));
    }
    [Test] public void CachedContentDoesNotAlsoSendSystemInstructions() {
        var request=Request();request["systemPrompt"]="instructions";request["providerOptions"]=new Dictionary<string,object>{["cachedContent"]="cachedContents/123"};
        Generate(request,(body,token)=>{var data=(IDictionary<string,object>)body;Assert.That(data["cached_content"],Is.EqualTo("cachedContents/123"));Assert.That(data.ContainsKey("system_instruction"),Is.False);return Completion();});
    }
    [Test] public void TextOnlyPayloadHasNoGenerationSchemaOrSystemPrompt() {
        Generate(Request(),(body,token)=>{Assert.That(((IDictionary<string,object>)body).Keys,Is.EquivalentTo(new[]{"contents"}));return Completion();});
    }
    [TestCase("MAX_TOKENS","output_limit")] [TestCase("SAFETY","failed")] [TestCase("OTHER","failed")]
    public void IncompleteResponseNeverReturnsText(string finish,string expected) {
        var result=Generate(Request(),(b,t)=>Completion(finish));Assert.That(result["state"],Is.EqualTo(expected));Assert.That(result["text"],Is.Null);
    }
    [Test] public void EmptyMissingAndMalformedCandidatesFail() {
        foreach(var response in new[]{new JObject(),JObject.Parse("{'candidates':[{'finishReason':'STOP','content':{'parts':[]}}]}"),JObject.Parse("{'candidates':[{'finishReason':'STOP','content':'bad'}]}")}) {
            var result=Generate(Request(),(b,t)=>response);Assert.That(result["state"],Is.EqualTo("failed"));Assert.That(result["text"],Is.Null);
        }
    }
    [Test] public void PrecancelledRequestNeverCallsTransport() {
        using var source=new CancellationTokenSource();source.Cancel();
        Assert.That(Generate(Request(source.Token),(b,t)=>throw new AssertionException("Must not send"))["state"],Is.EqualTo("cancelled"));
    }
    [Test] public void CancelledAfterSendDoesNotExposeCompletion() {
        using var source=new CancellationTokenSource();
        var result=Generate(Request(source.Token),(b,t)=>{source.Cancel();return Completion();});Assert.That(result["state"],Is.EqualTo("cancelled"));Assert.That(result["text"],Is.Null);
    }
    [Test] public void TimeoutIsFailureRatherThanUserCancellation() {
        Assert.That(Generate(Request(),(b,t)=>throw new OperationCanceledException())["state"],Is.EqualTo("failed"));
    }
    [Test] public void HttpErrorsPreserveStatusWithoutLeakingCredentialsOrBody() {
        var result=Generate(Request(),(b,t)=>throw new HttpRequestException("secret API key and body",null,HttpStatusCode.TooManyRequests));
        Assert.That(result["error"],Is.EqualTo("Gemini returned HTTP 429."));Assert.That(result["text"],Is.Null);
    }
    [Test] public void ProviderExceptionsAreSanitized() {
        var result=Generate(Request(),(b,t)=>throw new Exception("secret"));Assert.That(result["state"],Is.EqualTo("failed"));Assert.That((string)result["error"],Does.Not.Contain("secret"));
    }
    [Test] public void CommonServiceCallsTheProviderThroughNativeDynamicDispatch() {
        using var service=(IDisposable)Activator.CreateInstance(serviceType);int calls=0;
        var request=Request();request.Remove("cancellationToken");request["owner"]="933";request["provider"]="GeminiProbe";
        request["providerOptions"]=new Dictionary<string,object>{["cachedContent"]="cachedContents/test"};
        string route="AI.Genies/GeminiProbe/Provider.Generate";
        HomeGenie.ProgramDynamicApi.Register(route,input=>Generate(((MIG.MigInterfaceCommand)input).Data,(body,token)=>{
            calls++;var data=(IDictionary<string,object>)body;
            Assert.That(data["contents"],Is.SameAs(request["messages"]));Assert.That(data["cached_content"],Is.EqualTo("cachedContents/test"));return Completion();}));
        try {
            Func<string,object,object> call=(path,data)=>HomeGenie.ProgramDynamicApi.TryApiCall(new MIG.MigInterfaceCommand(path,data));
            var result=(IDictionary<string,object>)serviceType.GetMethod("Generate").Invoke(service,new object[]{request,call});
            Assert.That(result["state"],Is.EqualTo("completed"));Assert.That(result["text"],Is.EqualTo("visible answer"));Assert.That(calls,Is.EqualTo(1));
        } finally {HomeGenie.ProgramDynamicApi.UnRegister(route);}
    }
}
