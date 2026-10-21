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
public class AiGeniePresentationTest {
    Type contract,presentation;
    [OneTimeSetUp] public void CompileShippedPresentation() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        var xml=XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-base/programs.xml"));
        string Class(string id,string name)=>CSharpSyntaxTree.ParseText("class Holder{"+(string)xml.Root.Elements().Single(p=>(string)p.Element("Address")==id).Element("ScriptContext")+"}")
            .GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text==name).ToFullString();
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
            .Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location)).Append(typeof(JsonConvert).Assembly.Location);
        string code="using System;using System.Text;using System.Linq;using System.Collections.Generic;using Newtonsoft.Json;using Newtonsoft.Json.Linq;"+
            Class("581","GenieResponseContract")+Class("583","AiGeniePresentation");
        var compilation=CSharpCompilation.Create("Presentation"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(code)},
            paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var result=compilation.Emit(stream);
        Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));
        var assembly=Assembly.Load(stream.ToArray());contract=assembly.GetType("GenieResponseContract");presentation=assembly.GetType("AiGeniePresentation");
    }
    JObject Widget()=>JObject.Parse("{'version':1,'kind':'widget','explanation':'Choose a path','widget':{'html':'<div>Portal</div>','css':'','javascript':'class Portal extends ControllerInstance {}'}}");
    JObject Normalize(JObject value)=>(JObject)contract.GetMethod("Normalize").Invoke(null,new object[]{value});
    string Format(JObject value)=>(string)presentation.GetMethod("Format").Invoke(null,new object[]{Normalize(value)});
    [Test] public void OmittedModePreservesExistingSourceFences() {
        var widget=Widget();var normalized=Normalize(widget);
        Assert.That(normalized["widget"]["mode"],Is.Null);
        Assert.That(Format(widget),Does.Contain("```html").And.Contain("```javascript").And.Not.Contain("ai_widget"));
    }
    [Test] public void PresentationPreservesExplanationAndExactSourcesInHistoryEnvelope() {
        var widget=Widget();widget["widget"]["mode"]="presentation";
        widget["widget"]["html"]="<div><think>literal</think>```html</div>";
        var result=JObject.Parse(Format(widget))["ai_widget"];
        Assert.That((int)result["version"],Is.EqualTo(1));Assert.That((string)result["mode"],Is.EqualTo("presentation"));
        Assert.That((string)result["explanation"],Is.EqualTo("Choose a path"));
        Assert.That((string)result["html"],Is.EqualTo((string)widget["widget"]["html"]));
        Assert.That((string)result["javascript"],Is.EqualTo((string)widget["widget"]["javascript"]));
        Assert.That((string)result["css"],Does.Contain("assets/styles/widget-ui.css"));
    }
    [TestCase("'preview'")][TestCase("''")][TestCase("null")][TestCase("true")][TestCase("1")]
    public void UnknownOrMalformedModesAreRejected(string json) {
        var widget=Widget();widget["widget"]["mode"]=JToken.Parse(json.Replace("'","\""));
        Assert.Throws<TargetInvocationException>(()=>Normalize(widget));
    }
    [TestCase("widget")][TestCase("composite")]
    public void SchemaExposesOptionalPresentationModeForBothWidgetKinds(string kind) {
        var schema=(JObject)contract.GetMethod("Schema").Invoke(null,null);
        var variant=schema["anyOf"].Single(v=>(string)v["properties"]["kind"]["enum"][0]==kind);
        var widget=variant["properties"]["widget"];
        Assert.That(widget["properties"]["mode"]["enum"].Values<string>(),Is.EqualTo(new[]{"presentation"}));
        Assert.That(widget["required"].Values<string>(),Does.Not.Contain("mode"));
    }
}
