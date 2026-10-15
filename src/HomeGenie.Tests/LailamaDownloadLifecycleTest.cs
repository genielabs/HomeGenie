using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using NUnit.Framework;

namespace HomeGenie.Tests
{
    [TestFixture]
    public class LailamaDownloadLifecycleTest
    {
        private MethodInfo probe;

        [OneTimeSetUp]
        public void CompileShippedDownloadLoop()
        {
            var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "HomeGenie.sln")))
                root = root.Parent;
            Assert.That(root, Is.Not.Null);
            var path = Path.Combine(root.FullName, "assets/build/all/data/packages/homegenie/homegenie-ml-ai/programs.xml");
            var source = XDocument.Load(path).Root.Elements("ProgramBlock")
                .Single(p => (string)p.Element("Address") == "940").Element("ScriptSource").Value;
            probe = CompileProbe(source);
        }

        public static MethodInfo CompileProbe(string source)
        {
            // Execute the actual shipped script block, replacing only its external services.
            var start = source.IndexOf("if (!File.Exists(localModelFile))", StringComparison.Ordinal);
            var end = source.IndexOf("// --- 4. LLM ENGINE INITIALIZATION", start, StringComparison.Ordinal);
            var block = source.Substring(start, end - start);
            var code = $$$"""
using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
public static class DownloadProbe {
 public static int[] Run(string scenario) {
  var frames=scenario.Split(','); int frameIndex=0, pauses=0, calls=0, initialized=0;
  Func<string[]> frame=()=>frames[frameIndex].Split(':');
  var File=new FakeFile(()=>bool.Parse(frame()[2]));
  var Program=new FakeProgram(()=>frame()[0]!="Stopped");
  var Api=new FakeApi(()=>{calls++;return JsonConvert.SerializeObject(new {
    Success=true, Data=frame()[0]=="Missing" ? null : new {
     Status=frame()[0],ProgressPercentage=double.Parse(frame()[1],CultureInfo.InvariantCulture),
     Id="task",DestinationFilePath="model.gguf",ErrorMessage=(string)null
    }});});
  bool reconfigureRequired=frame()[0]=="Reconfigure";
  string localModelFile="model.gguf",selectedModel="model";
  var modelsList=new Dictionary<string,Model>{{"model",new Model()}};
  const string FIELD_DOWNLOAD="download",FIELD_DOWNLOAD_ID="id",FIELD_DOWNLOAD_TASK="task",
   FIELD_DOWNLOAD_ERROR="error",FIELD_DOWNLOAD_PROGRESS="progress",FIELD_DOWNLOAD_REPOSITORY="repo",FIELD_STATUS_ERROR="statusError";
  void Pause(int seconds) {
   if(seconds!=2)throw new Exception("Unexpected polling interval");
   pauses++;if(++frameIndex>=frames.Length)throw new Exception("Unexpected extra poll/restart");
   reconfigureRequired=frame()[0]=="Reconfigure";
  }
  Action main=()=>{
{{{block}}}
   initialized++;
  };
  main();return new[]{initialized,pauses,calls};
 }
}
public class FakeFile { readonly Func<bool> exists; public FakeFile(Func<bool> f){exists=f;} public bool Exists(string p)=>exists(); }
public class FakeProgram {
 readonly Func<bool> running;public FakeProgram(Func<bool> f){running=f;}
 public bool IsRunning=>running();public FakeProgram Emit(string key,object value)=>this;
 public FakeParameter Parameter(string key)=>null;
}
public class FakeParameter {public string Value;}
public class Model {public string Repository="test";}
public class FakeApi {readonly Func<string> status;public FakeApi(Func<string> f){status=f;}public string Call(string route,string path)=>status();}
""";
            var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
                .Append(typeof(JsonConvert).Assembly.Location).Distinct();
            var compilation = CSharpCompilation.Create("DownloadProbe" + Guid.NewGuid().ToString("N"),
                new[] { CSharpSyntaxTree.ParseText(code) }, paths.Select(p => MetadataReference.CreateFromFile(p)),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using var stream = new MemoryStream();
            var result = compilation.Emit(stream);
            Assert.That(result.Success, Is.True, string.Join("\n", result.Diagnostics));
            return Assembly.Load(stream.ToArray()).GetType("DownloadProbe").GetMethod("Run");
        }

        [TestCase("Downloading:100:false,Completed:100:false,Completed:100:true", 1, 2, 3)]
        [TestCase("Downloading:100:false,Downloading:100:true,Completed:100:true", 1, 2, 3)]
        [TestCase("Paused:100:false,Error:100:false,Completed:100:true", 1, 2, 3)]
        [TestCase("Missing:0:false,Completed:100:true", 1, 1, 2)]
        [TestCase("Downloading:40:false,Stopped:40:false", 0, 1, 1)]
        [TestCase("Downloading:40:false,Reconfigure:40:false", 0, 1, 1)]
        [TestCase("Completed:100:true", 1, 0, 0)]
        public void WaitsForFinalizedModelWithoutRestart(string scenario, int initializations, int pauses, int polls)
        {
            Assert.That((int[])probe.Invoke(null, new object[] { scenario }),
                Is.EqualTo(new[] { initializations, pauses, polls }));
        }
    }
}
