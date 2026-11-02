using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using HomeGenie.Automation.Engines;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
namespace HomeGenie.Tests;
[TestFixture]
public class ChatAttachmentScriptsCompilationTest {
    [TestCase("homegenie-base", "577")]
    [TestCase("homegenie-base", "583")]
    [TestCase("homegenie-ml-ai", "933")]
    [TestCase("homegenie-ml-ai", "940")]
    public void ShippedAttachmentScriptsCompile(string package, string address) {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"HomeGenie.sln")))root=root.Parent;
        Assert.That(root,Is.Not.Null);
        var node=XDocument.Load(Path.Combine(root.FullName,"assets/build/all/data/packages/homegenie",package,"programs.xml"))
            .Root.Elements().Single(n=>(string)n.Element("Address")==address);
        string folder=Path.Combine(Path.GetTempPath(),"hg-chat-compile-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try {
            var result=CSharpAppFactory.CompileScript((string)node.Element("ScriptSetup"),(string)node.Element("ScriptSource"),
                (string)node.Element("ScriptContext"),Path.Combine(folder,"Chat.dll"));
            Assert.That(result.Success,Is.True,string.Join(Environment.NewLine,result.Diagnostics.Where(d=>d.Severity==DiagnosticSeverity.Error)));
        } finally {foreach(string file in Directory.GetFiles(folder))File.Delete(file);Directory.Delete(folder);}
    }
}
