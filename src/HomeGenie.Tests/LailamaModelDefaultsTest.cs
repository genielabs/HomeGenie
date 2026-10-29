using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using YamlDotNet.Serialization;

namespace HomeGenie.Tests;
[TestFixture]
public class LailamaModelDefaultsTest {
    Type profile, policy;
    string repository;
    [OneTimeSetUp] public void CompileActualDefaultsAndSchema() {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null && !File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        repository=root.FullName;
        var program=XDocument.Load(Path.Combine(repository,"assets/build/all/data/packages/homegenie/homegenie-ml-ai/programs.xml"))
            .Root.Elements().Single(n=>(string)n.Element("Address")=="940");
        var tree=CSharpSyntaxTree.ParseText("class Holder {"+(string)program.Element("ScriptContext")+"}").GetRoot();
        var declarations=tree.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(n=>n.Identifier.Text=="ModelInfo" || n.Identifier.Text=="LailamaModelDefaults").Select(n=>n.ToFullString());
        var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
            .Where(p=>Path.GetDirectoryName(p)==Path.GetDirectoryName(typeof(object).Assembly.Location));
        var compilation=CSharpCompilation.Create("ModelDefaults"+Guid.NewGuid().ToString("N"),
            new[]{CSharpSyntaxTree.ParseText("using System;using System.Collections.Generic;"+string.Join("\n",declarations))},
            paths.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream=new MemoryStream();var result=compilation.Emit(stream);
        Assert.That(result.Success,Is.True,string.Join("\n",result.Diagnostics));
        var assembly=Assembly.Load(stream.ToArray());profile=assembly.GetType("ModelInfo");policy=assembly.GetType("LailamaModelDefaults");
        string source=(string)program.Element("ScriptSource");
        Assert.That(source,Does.Contain("var lastDefaultsModel = selectedModel;")
            .And.Contain("foreach (var setting in defaults) Program.Emit(setting.Key, setting.Value);")
            .And.Contain("if (defaults.Count > 0)")
            .And.Contain("Presentation = \"toast\"")
            .And.Contain("string.Join(\"\\n\", lines)"));
        Assert.That(source.IndexOf("LailamaModelDefaults.ForSelection",StringComparison.Ordinal),
            Is.GreaterThan(source.IndexOf("When.ModuleParameterChanged",StringComparison.Ordinal)));
    }
    object Model(double? temperature=null,int? tokens=null,uint? context=null) {
        var model=Activator.CreateInstance(profile);
        profile.GetField("DefaultTemperature").SetValue(model,temperature);
        profile.GetField("DefaultMaxTokens").SetValue(model,tokens);
        profile.GetField("DefaultContextSize").SetValue(model,context);return model;
    }
    Dictionary<string,string> Select(object model,string previous="old",string selected="new") {
        var models=(IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string),profile));models.Add("new",model);
        return (Dictionary<string,string>)policy.GetMethod("ForSelection").Invoke(null,new object[]{previous,selected,models});
    }
    [Test] public void NewSelectionAppliesOnlyDeclaredDefaultsWithInvariantNumbers() {
        var culture=CultureInfo.CurrentCulture;
        try {CultureInfo.CurrentCulture=new CultureInfo("it-IT");var values=Select(Model(.7,2048,8192));
            Assert.That(values,Has.Count.EqualTo(3));
            Assert.That(values["ConfigureOptions.ModelTemperature"],Is.EqualTo("0.7"));
            Assert.That(values["ConfigureOptions.MaxTokens"],Is.EqualTo("2048"));
            Assert.That(values["ConfigureOptions.ContextSize"],Is.EqualTo("8192"));
        } finally {CultureInfo.CurrentCulture=culture;}
    }
    [TestCase("new","new")][TestCase("old","")][TestCase("old","unknown")]
    public void SameSelectionStartupAndUnknownModelsDoNotResetOptions(string previous,string selected) {
        Assert.That(Select(Model(.7),previous,selected),Is.Empty);
    }
    [Test] public void AbsentFieldsPreserveUserOptions() {Assert.That(Select(Model()),Is.Empty);}
    [TestCase(-1d,null,null)][TestCase(3d,null,null)][TestCase(double.NaN,null,null)]
    [TestCase(null,32,null)][TestCase(null,32768,null)][TestCase(null,null,512u)][TestCase(null,null,8193u)]
    public void InvalidDefaultsFailBeforeAnyEmission(double? temperature,int? tokens,uint? context) {
        var ex=Assert.Throws<TargetInvocationException>(()=>Select(Model(temperature,tokens,context)));
        Assert.That(ex.InnerException,Is.InstanceOf<ArgumentOutOfRangeException>());
    }
    [Test] public void DistributedYamlDeserializesAllFiveProfilesWithNoOversizedDefaults() {
        string yaml=File.ReadAllText(Path.Combine(repository,"assets/build/all/data/programs/940/models.yaml"));
        var type=typeof(Dictionary<,>).MakeGenericType(typeof(string),profile);
        var models=(IDictionary)new DeserializerBuilder().Build().Deserialize(yaml,type);
        Assert.That(models.Count,Is.EqualTo(5));
        foreach(DictionaryEntry entry in models) {
            var values=(Dictionary<string,string>)policy.GetMethod("ForSelection").Invoke(null,new object[]{"different",(string)entry.Key,models});
            Assert.That(values.Keys,Is.EquivalentTo(new[]{"ConfigureOptions.ModelTemperature"}));
            Assert.That(profile.GetField("TopK").GetValue(entry.Value),Is.EqualTo(20));
            Assert.That(profile.GetField("MinP").GetValue(entry.Value),Is.EqualTo(0f));
        }
    }
}
