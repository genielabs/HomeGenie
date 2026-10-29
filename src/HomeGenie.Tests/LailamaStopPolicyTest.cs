using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using LLama;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace HomeGenie.Tests;
[TestFixture]
public class LailamaStopPolicyTest {
    Type policy;
    string source;
    [OneTimeSetUp] public void CompileShippedPolicy() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null && !File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        var program=XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie/homegenie-ml-ai/programs.xml"))
            .Root.Elements().Single(n=>(string)n.Element("Address")=="940");
        source=(string)program.Element("ScriptSource");
        var declaration=CSharpSyntaxTree.ParseText("class Holder {"+(string)program.Element("ScriptContext")+"}").GetRoot()
            .DescendantNodes().OfType<ClassDeclarationSyntax>().Single(n=>n.Identifier.Text=="LailamaStopPolicy").ToFullString();
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
            .Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location));
        var compilation=CSharpCompilation.Create("Stops"+Guid.NewGuid().ToString("N"),
            new[]{CSharpSyntaxTree.ParseText("using System;using System.Linq;using System.Collections.Generic;"+declaration)},
            paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var result=compilation.Emit(stream);
        Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));policy=Assembly.Load(stream.ToArray()).GetType("LailamaStopPolicy");
    }
    string[] Stops(string template,string[] legacy=null)=>(string[])policy.GetMethod("ForTemplate").Invoke(null,new object[]{template,legacy??new[]{"System:","Answer: ","<|im_end|>"}});
    [TestCase("chatml")][TestCase("qwen35-no-think")]
    public void ModernTemplatesUseOnlyChatmlTechnicalMarkers(string template) {
        Assert.That(Stops(template),Is.EqualTo(new[]{"<|im_end|>","<|im_start|>","<|endoftext|>"}));
    }
    [TestCase("Security Alarm System:")][TestCase("Answer: ")][TestCase("The API Response:")]
    [TestCase("Configured User:")][TestCase("My Assistant:")]
    public void OrdinaryTextDoesNotStopModernTemplates(string text) {
        foreach(string template in new[]{"chatml","qwen35-no-think"}) {
            var processor=new AntipromptProcessor(Stops(template));
            foreach(char c in text)Assert.That(processor.Add(c.ToString()),Is.False,template+": "+text);
        }
    }
    [TestCase("<|im_end|>")][TestCase("<|im_start|>")][TestCase("<|endoftext|>")]
    public void TechnicalMarkersStillStopAcrossChunkBoundaries(string text) {
        foreach(string template in new[]{"chatml","qwen35-no-think"}) {
            var processor=new AntipromptProcessor(Stops(template));bool stopped=false;
            foreach(char c in text)stopped=processor.Add(c.ToString());
            Assert.That(stopped,Is.True);
        }
    }
    [TestCase("alpaca")][TestCase("openchat")][TestCase("deepseek")][TestCase("phi3")]
    [TestCase("llama3")][TestCase("gemma")][TestCase("unknown")][TestCase("")][TestCase(null)]
    public void OtherTemplatesKeepExistingStopConfiguration(string template) {
        var legacy=new[]{"User:","System:","### Instruction","<end_of_turn>"};
        var actual=Stops(template,legacy);Assert.That(actual,Is.EqualTo(legacy));Assert.That(actual,Is.Not.SameAs(legacy));
    }
    [Test] public void ModernResultsAreNotSharedMutableArrays() {
        var first=Stops("chatml");first[0]="changed";Assert.That(Stops("chatml")[0],Is.EqualTo("<|im_end|>"));
    }
    [Test] public void PolicyIsSelectedAfterTemplateResolutionBeforeLoadingModel() {
        int policyIndex=source.IndexOf("LailamaStopPolicy.ForTemplate(templateType, LailamaInferenceParams.AntiPrompts)",StringComparison.Ordinal);
        Assert.That(policyIndex,Is.GreaterThan(source.IndexOf("templateType = modelsList[selectedModel].Template",StringComparison.Ordinal)));
        Assert.That(policyIndex,Is.LessThan(source.IndexOf("LLamaWeights.LoadFromFile",StringComparison.Ordinal)));
    }
}
