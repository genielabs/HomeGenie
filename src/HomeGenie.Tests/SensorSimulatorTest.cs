using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace HomeGenie.Tests;

[TestFixture]
public class SensorSimulatorTest
{
    private Type script;
    [OneTimeSetUp]
    public void CompileDistributedProgram()
    {
        var repo = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (repo != null && !File.Exists(Path.Combine(repo.FullName, "HomeGenie.sln"))) repo = repo.Parent;
        Assert.That(repo, Is.Not.Null);
        string folder = Path.Combine(repo.FullName, "assets/build/all/data/packages/homegenie/homegenie-home-demo");
        var program = XDocument.Load(Path.Combine(folder, "programs.xml")).Root.Elements()
            .Single(p => (string)p.Element("Address") == "410");
        string setup = (string)program.Element("ScriptSetup"), source = (string)program.Element("ScriptSource"), context = (string)program.Element("ScriptContext");
        string checksum = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { setup, source, context, data = (string)program.Element("Data") }))));
        var manifest = JObject.Parse(File.ReadAllText(Path.Combine(folder, "package.json")))["programs"].Single(p => (string)p["hid"] == "410");
        Assert.That(checksum, Is.EqualTo((string)program.Element("PackageInfo").Element("Checksum")));
        Assert.That(checksum, Is.EqualTo((string)manifest["checksum"]));
        Assert.That((string)manifest["version"], Is.EqualTo((string)program.Element("PackageInfo").Element("Version")));
        string code = "using System; using System.Collections.Generic; public class Distributed410 {" + context +
            " public FakeProgram Program = new(); public FakeModules Modules = new(); void Pause(double seconds) {} public void Main(){" + source + "} public void Setup(){" + setup + "} }" + """
            public class FakeProgram {
                int iteration; public bool IsRunning => iteration++ == 0;
                public void Setup(Action setup) => setup(); public void Run() {}
                public FakeProgram AddFeature(params string[] fields) => this;
            }
            public class FakeField {public double DecimalValue; public double IdleTime = 60;}
            public class FakeInstance {public string Domain = "Demo"; public string Address = "1";}
            public class FakeModule {
                public FakeInstance Instance = new(); public Dictionary<string,FakeField> Fields = new(); public Dictionary<string,double> Emitted = new();
                public FakeField Parameter(string key) { if (!Fields.TryGetValue(key,out var field)) Fields[key] = field = new(); return field; }
                public void Emit(string key, double value) {Emitted[key] = value;Parameter(key).DecimalValue = value;}
            }
            public class FakeModules {
                public Dictionary<string,List<FakeModule>> Features = new(); string selected;
                public FakeModules WithFeature(string feature) {selected = feature;return this;}
                public void Each(Func<FakeModule,bool> visit) {if (Features.TryGetValue(selected,out var modules)) foreach(var module in modules) if(visit(module)) break;}
            }
            """;
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
            .Where(p => Path.GetDirectoryName(p) == Path.GetDirectoryName(typeof(object).Assembly.Location));
        var compilation = CSharpCompilation.Create("SensorProbe" + Guid.NewGuid().ToString("N"), new[] { CSharpSyntaxTree.ParseText(code) },
            paths.Select(p => MetadataReference.CreateFromFile(p)), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var output = new MemoryStream();var result = compilation.Emit(output);
        Assert.That(result.Success, Is.True, string.Join("\n", result.Diagnostics));
        script = Assembly.Load(output.ToArray()).GetType("Distributed410");
    }
    private double Value(DateTime time, string kind, string id = "Demo:1") =>
        (double)script.GetMethod("GetSensorValue", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] {time, kind, id});
    private double Power(double max, double level) =>
        (double)script.GetMethod("GetPowerValue", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] {max, level});

    [Test]
    public void ValuesStayBoundedAcrossBothOrdinaryAndLeapYears()
    {
        foreach (int year in new[] {2027, 2028})
        for (var day = new DateTime(year, 1, 1); day.Year == year; day = day.AddDays(1))
        for (int hour = 0; hour < 24; hour++) {
            var time = day.AddHours(hour);
            Assert.That(Value(time, "Sensor.Temperature"), Is.InRange(16, 29));
            Assert.That(Value(time, "Sensor.Humidity"), Is.InRange(35, 65));
            Assert.That(Value(time, "Sensor.Luminance"), Is.InRange(0, 1000));
        }
    }
    [Test]
    public void SeasonAndDaylightAffectTheProfile()
    {
        var winter = new DateTime(2027, 1, 15, 12, 0, 0);var summer = new DateTime(2027, 7, 15, 12, 0, 0);
        Assert.That(Value(summer, "temperature"), Is.GreaterThan(Value(winter, "temperature") + 4));
        Assert.That(Value(winter, "humidity"), Is.GreaterThan(Value(summer, "humidity")));
        Assert.That(Value(summer, "luminance"), Is.GreaterThan(Value(winter, "luminance")));
        Assert.That(Value(summer.Date.AddHours(6), "luminance"),
            Is.GreaterThan(Value(winter.Date.AddHours(6), "luminance")));
    }
    [Test]
    public void NightLuminanceVariesBetweenZeroAndOneHundredLux()
    {
        var midnight = new DateTime(2027, 1, 15);
        var values = Enumerable.Range(0, 48)
            .Select(minute => Value(midnight.AddMinutes(minute), "Sensor.Luminance")).ToArray();
        Assert.That(values, Is.All.InRange(0, 100));
        Assert.That(values.Min(), Is.Zero);
        Assert.That(values.Max(), Is.EqualTo(100));
    }
    [Test]
    public void ValuesVarySmoothlyAcrossMinuteMonthAndYearBoundaries()
    {
        foreach (var start in new[] {new DateTime(2027, 1, 31, 23, 59, 0), new DateTime(2027, 12, 31, 23, 59, 0), new DateTime(2028, 2, 29, 12, 0, 0), new DateTime(2027, 7, 15, 7, 0, 0)}) {
            Assert.That(Math.Abs(Value(start, "temperature") - Value(start.AddMinutes(1), "temperature")), Is.LessThan(.05));
            Assert.That(Math.Abs(Value(start, "humidity") - Value(start.AddMinutes(1), "humidity")), Is.LessThan(.15));
            Assert.That(Math.Abs(Value(start, "luminance") - Value(start.AddMinutes(1), "luminance")), Is.LessThan(12));
        }
    }
    [Test]
    public void RepeatedCallsAndDifferentEmitSchedulesCannotCauseRandomWalkDrift()
    {
        var time = new DateTime(2027, 3, 12, 10, 0, 0);double expected = Value(time, "humidity");
        for (int i = 0; i < 1000; i++) Assert.That(Value(time, "humidity"), Is.EqualTo(expected));
        Assert.That(Value(time, "temperature", "Demo:1"), Is.Not.EqualTo(Value(time, "temperature", "Demo:2")));
        Assert.That(Value(time, " Sensor.Temperature "), Is.EqualTo(Value(time, "temperature")));
    }
    [Test]
    public void UnsupportedTypesFailExplicitly()
    {
        var error = Assert.Throws<TargetInvocationException>(() => Value(DateTime.Now, "Meter.Unknown"));
        Assert.That(error.InnerException, Is.TypeOf<ArgumentException>());
    }
    [Test]
    public void PowerTracksLevelAndCannotBecomeNegativeOrExceedMaximum()
    {
        Assert.That(Power(60, 0), Is.Zero);Assert.That(Power(60, .5), Is.EqualTo(30));Assert.That(Power(60, 1), Is.EqualTo(60));
        Assert.That(Power(60, -1), Is.Zero);Assert.That(Power(60, 3), Is.EqualTo(60));
        Assert.That(Power(double.NaN, 1), Is.Zero);Assert.That(Power(60, double.NaN), Is.Zero);
    }
    [Test]
    public void DistributedLoopPreservesFrequencyFeatureSelectionAndRegularOffSamples()
    {
        dynamic program = Activator.CreateInstance(script);dynamic sensor = Activator.CreateInstance(script.Assembly.GetType("FakeModule"));
        sensor.Parameter("Simulator.Sensor.DataFrequency").DecimalValue = 30.0;
        sensor.Parameter("Sensor.Temperature").DecimalValue = -90.0; // old values must not seed the profile
        sensor.Parameter("Sensor.Humidity").IdleTime = 2.0;
        sensor.Parameter("Simulator.Meter.Watts.Max").DecimalValue = 60.0;
        var listType = typeof(System.Collections.Generic.List<>).MakeGenericType(sensor.GetType());
        dynamic list = Activator.CreateInstance(listType);list.Add(sensor);
        program.Modules.Features.Add("Simulator.Sensor.Temperature", list);
        program.Modules.Features.Add("Simulator.Sensor.Humidity", list);
        program.Modules.Features.Add("Simulator.Meter.Watts", list);
        program.Main();
        Assert.That((double)sensor.Emitted["Sensor.Temperature"], Is.InRange(16, 29));
        Assert.That((bool)sensor.Emitted.ContainsKey("Sensor.Humidity"), Is.False);
        Assert.That((bool)sensor.Emitted.ContainsKey("Sensor.Luminance"), Is.False);
        Assert.That((double)sensor.Emitted["Meter.Watts"], Is.Zero);
        dynamic paused = Activator.CreateInstance(script);sensor.Emitted.Clear();sensor.Parameter("Simulator.Sensor.DataFrequency").DecimalValue = 0.0;
        paused.Modules.Features.Add("Simulator.Sensor.Temperature", list);paused.Main();
        Assert.That((int)sensor.Emitted.Count, Is.Zero);
    }
}
