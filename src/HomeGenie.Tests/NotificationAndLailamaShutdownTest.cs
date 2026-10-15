using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace HomeGenie.Tests
{
    [TestFixture]
    public class NotificationAndLailamaShutdownTest
    {
        private MethodInfo decode;
        private Type lifetimeType;

        [OneTimeSetUp]
        public void CompileShippedHelpers()
        {
            var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "HomeGenie.sln"))) root = root.Parent;
            Assert.That(root, Is.Not.Null);
            string Context(string package, string address) => XDocument.Load(Path.Combine(root.FullName,
                "assets/build/all/data/packages/homegenie", package, "programs.xml")).Root.Elements("ProgramBlock")
                .Single(p => (string)p.Element("Address") == address).Element("ScriptContext").Value;
            Configure(Context("homegenie-base", "582"), Context("homegenie-ml-ai", "940"));
        }

        public void Configure(string notifications, string lailama)
        {
            notifications = Regex.Replace(notifications, @"^#using[^\r\n]*", "", RegexOptions.Multiline);
            lailama = lailama.Substring(lailama.IndexOf("public sealed class LailamaNativeLifetime", StringComparison.Ordinal));
            var code = "using System;using System.IO;using System.Text;using System.Linq;using System.Collections.Generic;" +
                "using System.Security.Cryptography;using System.Text.RegularExpressions;using System.Threading;" +
                "using System.Threading.Tasks;using Newtonsoft.Json;using Newtonsoft.Json.Linq;\n" + notifications + "\n" + lailama;
            var runtime = Path.GetDirectoryName(typeof(object).Assembly.Location);
            var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
                .Where(p => Path.GetDirectoryName(p) == runtime).Append(typeof(JsonConvert).Assembly.Location).Distinct()
                .Select(p => MetadataReference.CreateFromFile(p));
            var compilation = CSharpCompilation.Create("ShutdownHelpers" + Guid.NewGuid().ToString("N"),
                new[] { CSharpSyntaxTree.ParseText(code) }, references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using var stream = new MemoryStream();
            var result = compilation.Emit(stream);
            Assert.That(result.Success, Is.True, string.Join("\n", result.Diagnostics));
            var assembly = Assembly.Load(stream.ToArray());
            decode = assembly.GetType("NotificationArchive").GetMethod("Decode");
            lifetimeType = assembly.GetType("LailamaNativeLifetime");
        }

        private JObject Decode(string text) => (JObject)decode.Invoke(null, new object[] { text });
        private dynamic Lifetime() => Activator.CreateInstance(lifetimeType);
        private static JObject Payload() => new JObject {
            ["Version"] = 1, ["Persistent"] = true, ["Id"] = "test", ["Title"] = "Test", ["Message"] = "Ready"
        };

        [TestCase(null)]
        [TestCase("")]
        [TestCase("ordinary notification")]
        [TestCase("123")]
        [TestCase("true")]
        [TestCase("null")]
        [TestCase("[]")]
        [TestCase("\"normal text\"")]
        public void LegacyNonObjectNotificationsAreIgnored(string value) => Assert.That(Decode(value), Is.Null);

        [TestCase("normal text")]
        [TestCase("123")]
        [TestCase("null")]
        [TestCase("[]")]
        [TestCase("\"quoted text\"")]
        [TestCase("{\"answer\":42}")]
        public void LegacyWrapperMessageIsNotRequiredToBeAnObject(string value)
        {
            Assert.That(Decode(new JObject { ["Title"] = "Old", ["Message"] = value }.ToString()), Is.Null);
        }

        [Test]
        public void DirectPersistentMessageIsNeverReinterpretedAsNestedEnvelope()
        {
            var payload = Payload();
            var nested = Payload(); nested["Id"] = "nested";
            payload["Message"] = nested.ToString();
            Assert.That((string)Decode(payload.ToString())["Id"], Is.EqualTo("test"));
        }

        [Test]
        public void WrappedPersistentMessageRetainsTitleFallback()
        {
            var payload = Payload(); payload.Remove("Title");
            var decoded = Decode(new JObject { ["Title"] = "Outer", ["Message"] = payload.ToString() }.ToString());
            Assert.That((string)decoded["Id"], Is.EqualTo("test"));
            Assert.That((string)decoded["Title"], Is.EqualTo("Outer"));
        }

        [TestCase("\"invalid\"")]
        [TestCase("[1]")]
        public void MalformedPersistentActionsRemainErrors(string actions)
        {
            var payload = Payload(); payload["Actions"] = JToken.Parse(actions);
            var exception = Assert.Throws<TargetInvocationException>(() => Decode(payload.ToString()));
            Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void PersistentNotificationStillRequiresAnIdentity()
        {
            var payload = Payload(); payload.Remove("Id");
            var exception = Assert.Throws<TargetInvocationException>(() => Decode(payload.ToString()));
            Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void IdleShutdownDisposesExactlyOnceAndRejectsNewWork()
        {
            dynamic lifetime = Lifetime(); int released = 0;
            Action release = () => Interlocked.Increment(ref released);
            Assert.That((bool)lifetime.Stop(release, (Action<string>)null, 2000), Is.True);
            Assert.That((bool)lifetime.Stop(release, (Action<string>)null, 2000), Is.True);
            Assert.That(released, Is.EqualTo(1));
            CancellationTokenSource token;
            Assert.That((bool)lifetime.TryBegin(out token), Is.False);
        }

        [Test]
        public void ShutdownCancelsButNeverDisposesResourcesInUse()
        {
            dynamic lifetime = Lifetime(); CancellationTokenSource token; int released = 0;
            Assert.That((bool)lifetime.TryBegin(out token), Is.True);
            Action release = () => Interlocked.Increment(ref released);
            try
            {
                Assert.That((bool)lifetime.Stop(release, (Action<string>)null, 25), Is.False);
                Assert.That(SpinWait.SpinUntil(() => token.IsCancellationRequested, 2000), Is.True);
                Assert.That(released, Is.Zero);
            }
            finally { lifetime.End(token); }
            Assert.That((bool)lifetime.Stop(release, (Action<string>)null, 2000), Is.True);
            Assert.That(released, Is.EqualTo(1));
        }

        [Test]
        public void SlowNativeDisposeDoesNotHoldTheShutdownCallerIndefinitely()
        {
            dynamic lifetime = Lifetime();
            using var entered = new ManualResetEventSlim(); using var finish = new ManualResetEventSlim();
            Action release = () => { entered.Set(); finish.Wait(); };
            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                Assert.That((bool)lifetime.Stop(release, (Action<string>)null, 25), Is.False);
                Assert.That(elapsed.ElapsedMilliseconds, Is.LessThan(3000));
                Assert.That(entered.Wait(2000), Is.True);
            }
            finally { finish.Set(); lifetime.Stop(release, (Action<string>)null, 2000); }
        }

        [Test]
        public void OnlyTheOwningOperationCanEndOrReplaceItsCancellationSource()
        {
            dynamic lifetime = Lifetime(); CancellationTokenSource first, second, rejected;
            Assert.That((bool)lifetime.TryBegin(out first), Is.True);
            Assert.That((bool)lifetime.TryBegin(out rejected), Is.False);
            lifetime.End(first);
            Assert.That((bool)lifetime.TryBegin(out second), Is.True);
            lifetime.End(first);
            lifetime.CancelCurrent();
            Assert.That(second.IsCancellationRequested, Is.True);
            Assert.That((bool)lifetime.TryBegin(out rejected), Is.False);
            lifetime.End(second);
            Assert.That((bool)lifetime.Stop((Action)(() => { }), (Action<string>)null, 2000), Is.True);
        }

        [Test]
        public void InterruptedCallerDoesNotReleaseAnOperationOwnedByItsWorker()
        {
            dynamic lifetime = Lifetime(); CancellationTokenSource token;
            lifetime.TryBegin(out token);
            using var entered = new ManualResetEventSlim(); using var finish = new ManualResetEventSlim();
            int released = 0;
            var worker = Task.Run(() => { entered.Set(); try { finish.Wait(); } finally { lifetime.End(token); } });
            var caller = new Thread(() => { try { worker.GetAwaiter().GetResult(); } catch (ThreadInterruptedException) { } });
            caller.IsBackground = true;
            Action release = () => Interlocked.Increment(ref released);
            try
            {
                caller.Start(); Assert.That(entered.Wait(2000), Is.True);
                caller.Interrupt(); Assert.That(caller.Join(2000), Is.True);
                Assert.That((bool)lifetime.Stop(release, (Action<string>)null, 25), Is.False);
                Assert.That(released, Is.Zero);
                Assert.That((bool)lifetime.CleanupCompleted, Is.False);
            }
            finally { finish.Set(); worker.GetAwaiter().GetResult(); lifetime.Stop(release, (Action<string>)null, 2000); }
            Assert.That(released, Is.EqualTo(1));
        }

        [Test]
        public void BlockingCancellationCallbackDoesNotBlockTheShutdownCaller()
        {
            dynamic lifetime = Lifetime(); CancellationTokenSource token;
            lifetime.TryBegin(out token);
            using var entered = new ManualResetEventSlim(); using var finish = new ManualResetEventSlim();
            using var registration = token.Token.Register(() => { entered.Set(); finish.Wait(); });
            int released = 0;
            Action release = () => Interlocked.Increment(ref released);
            try
            {
                Assert.That((bool)lifetime.Stop(release, (Action<string>)null, 25), Is.False);
                Assert.That(entered.Wait(2000), Is.True);
                Assert.That(released, Is.Zero);
            }
            finally { finish.Set(); lifetime.End(token); lifetime.Stop(release, (Action<string>)null, 2000); }
            Assert.That(released, Is.EqualTo(1));
        }

        [Test]
        public void ShutdownContinuesWhenTheLoggerIsAlreadyDisposed()
        {
            dynamic lifetime = Lifetime(); bool released = false;
            Action<string> log = _ => throw new ObjectDisposedException("LoggerFactory");
            Assert.That((bool)lifetime.Stop((Action)(() => released = true), log, 2000), Is.True);
            Assert.That(released, Is.True);
        }

        [Test]
        public void NativeCleanupFailureIsLoggedWithoutAnUnhandledException()
        {
            dynamic lifetime = Lifetime(); var logs = new ConcurrentQueue<string>();
            Assert.That((bool)lifetime.Stop((Action)(() => throw new InvalidOperationException("native failure")),
                (Action<string>)logs.Enqueue, 2000), Is.True);
            Assert.That(logs.Any(line => line.Contains("cleanup failed")), Is.True);
        }
    }
}
