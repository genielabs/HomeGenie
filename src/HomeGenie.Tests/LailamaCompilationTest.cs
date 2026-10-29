using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using HomeGenie.Automation.Engines;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace HomeGenie.Tests;

[TestFixture]
public class LailamaCompilationTest
{
    [Test]
    public void ShippedLailamaCompilesAgainstInstalledDependencies()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "HomeGenie.sln"))) root = root.Parent;
        Assert.That(root, Is.Not.Null, "Repository root not found.");
        var program = XDocument.Load(Path.Combine(root.FullName,
            "assets/build/all/data/packages/homegenie/homegenie-ml-ai/programs.xml"))
            .Root.Elements().Single(n => (string)n.Element("Address") == "940");
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "lailama-compile-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var result = CSharpAppFactory.CompileScript((string)program.Element("ScriptSetup"),
                (string)program.Element("ScriptSource"), (string)program.Element("ScriptContext"),
                Path.Combine(directory, "Lailama.dll"));
            Assert.That(result.Success, Is.True,
                string.Join(Environment.NewLine, result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
        }
        finally
        {
            foreach (string file in Directory.GetFiles(directory)) File.Delete(file);
            Directory.Delete(directory);
        }
    }

    [Test]
    public void AutomaticImportsDoNotRequireRemovedLlamaAdapters()
    {
        var parsed = CSharpAppFactory.ParseCode("", "", "");
        Assert.That(parsed.UsingNamespaces, Does.Not.Contain("LLamaSharp.KernelMemory"));
        Assert.That(parsed.UsingNamespaces, Does.Not.Contain("LLamaSharp.SemanticKernel"));
        Assert.That(parsed.UsingNamespaces, Does.Not.Contain("Microsoft.SemanticKernel"));
    }
}
