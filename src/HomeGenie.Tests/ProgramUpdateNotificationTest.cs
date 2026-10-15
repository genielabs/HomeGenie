using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace HomeGenie.Tests;

[TestFixture]
public class ProgramUpdateNotificationTest
{
    private Type archive, notices;

    [OneTimeSetUp]
    public void CompileShippedHelper()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "HomeGenie.sln"))) root = root.Parent;
        Assert.That(root, Is.Not.Null);
        Configure(XDocument.Load(Path.Combine(root.FullName,
            "assets/build/all/data/packages/homegenie/homegenie-base/programs.xml")).Root.Elements("ProgramBlock")
            .Single(p => (string)p.Element("Address") == "582").Element("ScriptContext").Value);
    }

    public void Configure(string context)
    {
        context = Regex.Replace(context, @"^#using[^\r\n]*", "", RegexOptions.Multiline);
        var runtime = Path.GetDirectoryName(typeof(object).Assembly.Location);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
            .Where(p => Path.GetDirectoryName(p) == runtime).Append(typeof(JsonConvert).Assembly.Location).Distinct()
            .Select(p => MetadataReference.CreateFromFile(p));
        var code = "using System;using System.IO;using System.Text;using System.Linq;using System.Collections.Generic;" +
                   "using System.Security.Cryptography;using System.Text.RegularExpressions;using Newtonsoft.Json;using Newtonsoft.Json.Linq;" + context;
        var compilation = CSharpCompilation.Create("UpdateNotice" + Guid.NewGuid().ToString("N"),
            new[] { CSharpSyntaxTree.ParseText(code) }, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.That(result.Success, Is.True, string.Join("\n", result.Diagnostics));
        var assembly = Assembly.Load(stream.ToArray());
        archive = assembly.GetType("NotificationArchive");
        notices = assembly.GetType("ProgramUpdateNotification");
    }

    private static JObject Report(bool available = true, string fingerprint = "a") => new() {
        ["Status"] = "Ok", ["complete"] = true, ["fingerprint"] = fingerprint,
        ["items"] = available ? new JArray(new JObject { ["id"] = "test", ["updateAddresses"] = new JArray(10) }) : new JArray()
    };
    private JObject Reconcile(JArray rows, JObject report, string address = "582") =>
        (JObject)notices.GetMethod("Reconcile").Invoke(null, new object[] { rows, report, "automation", address, DateTime.UtcNow, 500, 90 });

    [Test]
    public void CreatesOnePersistentNoticeWithNavigationOnly()
    {
        var rows = new JArray(); var row = Reconcile(rows, Report());
        Assert.That(rows.Count, Is.EqualTo(1));
        Assert.That((bool)row["payload"]["Persistent"], Is.True);
        Assert.That((string)row["payload"]["Actions"][0]["Type"], Is.EqualTo("navigate"));
        Assert.That(row["payload"]["Actions"][0]["Endpoint"], Is.Null);
    }
    [Test]
    public void RepeatedChecksPreserveReadArchiveAndDoNotWrite()
    {
        var rows = new JArray(); var row = Reconcile(rows, Report());
        row["read"] = true; row["archived"] = true;
        var before = rows.ToString();
        Assert.That(Reconcile(rows, Report()), Is.Null);
        Assert.That(rows.ToString(), Is.EqualTo(before));
    }
    [Test]
    public void ChangedFindingsUpdateSameIdentityAndReopen()
    {
        var rows = new JArray(); var row = Reconcile(rows, Report());
        row["read"] = true; row["archived"] = true;
        var key = (string)row["key"];
        var updated = Reconcile(rows, Report(fingerprint: "b"));
        Assert.That(rows.Count, Is.EqualTo(1));
        Assert.That((string)updated["key"], Is.EqualTo(key));
        Assert.That((bool)updated["read"], Is.False);
        Assert.That((bool)updated["archived"], Is.False);
    }
    [Test]
    public void CompleteEmptyResultResolvesOnceAndSameFindingsCanReturn()
    {
        var rows = new JArray(); var row = Reconcile(rows, Report());
        Reconcile(rows, Report(false));
        Assert.That((bool)row["archived"], Is.True);
        Assert.That((bool)row["resolved"], Is.True);
        Assert.That(Reconcile(rows, Report(false)), Is.Null);
        Reconcile(rows, Report());
        Assert.That((bool)row["archived"], Is.False);
        Assert.That((bool)row["read"], Is.False);
        Assert.That((int)row["revision"], Is.EqualTo(2));
    }
    [TestCase("Error", true)]
    [TestCase("Ok", false)]
    public void IncompleteChecksNeverResolve(string status, bool complete)
    {
        var rows = new JArray(); Reconcile(rows, Report()); var before = rows.ToString();
        var report = Report(false); report["Status"] = status; report["complete"] = complete;
        Assert.That(Reconcile(rows, report), Is.Null);
        Assert.That(rows.ToString(), Is.EqualTo(before));
    }
    [Test]
    public void MissingRequiredProgramAlsoCreatesNotice()
    {
        var report = Report(); report["items"][0]["updateAddresses"] = new JArray();
        report["items"][0]["reasons"] = new JArray("missingRequired");
        Assert.That(Reconcile(new JArray(), report), Is.Not.Null);
    }
    [Test]
    public void ArchiveRoundtripKeepsAcknowledgementAcrossRestart()
    {
        var rows = new JArray(); var row = Reconcile(rows, Report()); row["read"] = true;
        var reloaded = JsonConvert.DeserializeObject<JArray>(rows.ToString(), new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
        Assert.That(Reconcile(reloaded, Report()), Is.Null);
        Assert.That((bool)reloaded[0]["read"], Is.True);
    }
    [Test]
    public void ResolutionCannotArchiveAnotherSender()
    {
        var rows = new JArray(); var row = Reconcile(rows, Report(), "other");
        Assert.That(Reconcile(rows, Report(false)), Is.Null);
        Assert.That((bool)row["archived"], Is.False);
    }
    [TestCase("https://example.com", false)]
    [TestCase("program-updates", true)]
    public void NavigationRejectsOtherDestinationsAndEndpoints(string destination, bool endpoint)
    {
        var payload = (JObject)Reconcile(new JArray(), Report())["payload"].DeepClone();
        payload["Actions"][0]["Destination"] = destination;
        if (endpoint) payload["Actions"][0]["Endpoint"] = "System/Anything/Run";
        Assert.Throws<TargetInvocationException>(() => archive.GetMethod("Decode").Invoke(null, new object[] { payload.ToString() }));
    }
    [Test]
    public void OutOfOrderProducerEventsCannotRegressProgress()
    {
        var rows = new JArray();
        var payload = new JObject { ["Version"]=1, ["Persistent"]=true, ["Id"]="job", ["Title"]="Updates", ["Message"]="Completed", ["Sequence"]=5 };
        object[] Args(JObject value) => new object[] {rows,value,"automation","582",DateTime.UtcNow,500,90};
        var row=(JObject)archive.GetMethod("Upsert").Invoke(null, Args(payload));
        row["read"]=true;row["archived"]=true;
        var before=rows.ToString();
        payload["Sequence"]=3;payload["Message"]="Running";
        archive.GetMethod("Upsert").Invoke(null,Args(payload));
        Assert.That(rows.ToString(),Is.EqualTo(before));
    }

}
