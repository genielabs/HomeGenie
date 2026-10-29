using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using LLama.Common;
using LLama.Sampling;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace HomeGenie.Tests;

[TestFixture]
public class LailamaInferenceOptionsTest
{
    Type probe;

    [OneTimeSetUp]
    public void CompileShippedOptionApplication()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "HomeGenie.sln"))) root = root.Parent;
        var program = XDocument.Load(Path.Combine(root.FullName,
            "assets/build/all/data/packages/homegenie/homegenie-ml-ai/programs.xml"))
            .Root.Elements().Single(n => (string)n.Element("Address") == "940");
        var tree = CSharpSyntaxTree.ParseText("class Holder {" + (string)program.Element("ScriptContext") + "}").GetRoot();
        var modelInfo = tree.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Single(n => n.Identifier.Text == "ModelInfo").ToFullString();
        var method = tree.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(n => n.Identifier.Text == "ApplyInferenceOptions").ToFullString();
        string code = "using System;using LLama.Common;using LLama.Sampling;" + """
            public class OptionsProbe {
                const string OPTION_MODEL_TEMPERATURE="ModelTemperature", OPTION_MAX_TOKENS="MaxTokens";
                public FakeOptions Program = new FakeOptions();
                public InferenceParams LailamaInferenceParams = new InferenceParams {
                    SamplingPipeline = new DefaultSamplingPipeline { TopP=0.95f, RepeatPenalty=1.2f }
                };
                public void Apply() => ApplyInferenceOptions();
            """ + method + "}" + modelInfo + """
            public class FakeOptions {
                public double Temperature=1, Tokens=2048;
                public FakeValue Option(string name) => new FakeValue {
                    DecimalValue = name=="ModelTemperature" ? Temperature : Tokens
                };
            }
            public class FakeValue { public double DecimalValue; }
            """;
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
            .Where(p => Path.GetDirectoryName(p) == Path.GetDirectoryName(typeof(object).Assembly.Location))
            .Append(typeof(InferenceParams).Assembly.Location).Distinct();
        var compilation = CSharpCompilation.Create("InferenceOptions" + Guid.NewGuid().ToString("N"),
            new[] { CSharpSyntaxTree.ParseText(code) }, paths.Select(p => MetadataReference.CreateFromFile(p)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.That(result.Success, Is.True, string.Join("\n", result.Diagnostics));
        probe = Assembly.Load(stream.ToArray()).GetType("OptionsProbe");
    }

    object Create(double temperature, double tokens)
    {
        var instance = Activator.CreateInstance(probe);
        var options = probe.GetField("Program").GetValue(instance);
        options.GetType().GetField("Temperature").SetValue(options, temperature);
        options.GetType().GetField("Tokens").SetValue(options, tokens);
        return instance;
    }

    InferenceParams Parameters(object instance) => (InferenceParams)probe.GetField("LailamaInferenceParams").GetValue(instance);
    void Apply(object instance) => probe.GetMethod("Apply").Invoke(instance, null);

    [TestCase(0, 64)]
    [TestCase(0.4, 1536)]
    [TestCase(1, 2048)]
    [TestCase(2, 4096)]
    public void UiValuesReachTheActualSamplerAndOutputBudget(double temperature, int tokens)
    {
        var instance = Create(temperature, tokens);
        var parameters = Parameters(instance);
        var sampler = parameters.SamplingPipeline;
        Apply(instance);
        Assert.That(parameters.SamplingPipeline, Is.Not.SameAs(sampler));
        var applied = (DefaultSamplingPipeline)parameters.SamplingPipeline;
        Assert.That(applied.Temperature, Is.EqualTo((float)temperature));
        Assert.That(parameters.MaxTokens, Is.EqualTo(tokens));
        Assert.That(applied.TopP, Is.EqualTo(0.95f));
        Assert.That(applied.RepeatPenalty, Is.EqualTo(1.2f));
    }

    [Test]
    public void PackageChecksumMatchesShippedSources()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "HomeGenie.sln"))) root = root.Parent;
        var program = XDocument.Load(Path.Combine(root.FullName,
            "assets/build/all/data/packages/homegenie/homegenie-ml-ai/programs.xml"))
            .Root.Elements().Single(n => (string)n.Element("Address") == "940");
        var checksum = HomeGenie.Service.Utility.GetObjectChecksum(new {
            setup = (string)program.Element("ScriptSetup"), source = (string)program.Element("ScriptSource"),
            context = (string)program.Element("ScriptContext"), data = (string)program.Element("Data")
        });
        Assert.That(checksum, Is.EqualTo((string)program.Element("PackageInfo").Element("Checksum")));
    }

    [Test]
    public void ModelProfileOverridesSamplingWithoutChangingUiTemperatureOrBudget()
    {
        var instance = Create(0.7, 2048);
        var profileType = probe.Assembly.GetType("ModelInfo");
        var profile = Activator.CreateInstance(profileType);
        foreach (var pair in new[] { ("TopP", 0.8f), ("MinP", 0f), ("RepeatPenalty", 1f), ("PresencePenalty", 1.5f) })
            profileType.GetField(pair.Item1).SetValue(profile, pair.Item2);
        profileType.GetField("TopK").SetValue(profile, 20);
        probe.GetMethod("ApplyInferenceOptions", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(instance, new[] { profile });
        var sampler = (DefaultSamplingPipeline)Parameters(instance).SamplingPipeline;
        Assert.That(sampler.Temperature, Is.EqualTo(0.7f));
        Assert.That(sampler.TopP, Is.EqualTo(0.8f));
        Assert.That(sampler.TopK, Is.EqualTo(20));
        Assert.That(sampler.MinP, Is.EqualTo(0));
        Assert.That(sampler.RepeatPenalty, Is.EqualTo(1));
        Assert.That(sampler.PresencePenalty, Is.EqualTo(1.5f));
        Assert.That(Parameters(instance).MaxTokens, Is.EqualTo(2048));
    }

    [TestCase(-0.1, 2048)]
    [TestCase(2.1, 2048)]
    [TestCase(double.NaN, 2048)]
    [TestCase(double.PositiveInfinity, 2048)]
    [TestCase(1, 63)]
    [TestCase(1, 4097)]
    public void InvalidValuesFailBeforeChangingTheSampler(double temperature, int tokens)
    {
        var instance = Create(temperature, tokens);
        var parameters = Parameters(instance);
        var initial = ((DefaultSamplingPipeline)parameters.SamplingPipeline).Temperature;
        var error = Assert.Throws<TargetInvocationException>(() => Apply(instance));
        Assert.That(error.InnerException, Is.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(((DefaultSamplingPipeline)parameters.SamplingPipeline).Temperature, Is.EqualTo(initial));
    }
}
