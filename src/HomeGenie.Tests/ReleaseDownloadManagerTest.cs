using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HomeGenie.Service;
using Newtonsoft.Json;
using NUnit.Framework;

namespace HomeGenie.Tests;
[TestFixture,NonParallelizable]
public class ReleaseDownloadManagerTest
{
    private string folder,previous;
    private byte[] zip;
    private ReleaseInfo release;
    private Handler handler;
    private ReleaseDownloadManager manager;
    [SetUp] public void Setup()
    {
        previous=Environment.CurrentDirectory;folder=Path.Combine(Path.GetTempPath(),"hg-release-download-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);Environment.CurrentDirectory=folder;
        using(var output=new MemoryStream()) {using(var archive=new ZipArchive(output,ZipArchiveMode.Create,true)){
            void Add(string path,string text){using var writer=new StreamWriter(archive.CreateEntry(path).Open());writer.Write(text);}
            Add("homegenie/release_info.xml","<ReleaseInfo><Version>9.9.9</Version><Runtime>linux-x64</Runtime><ReleaseDate>2026-01-01T00:00:00Z</ReleaseDate></ReleaseInfo>");
            Add("homegenie/data/packages/test/base/package.json","{\"repository\":\"test\",\"id\":\"base\",\"version\":\"1.0.0\",\"programs\":[]}");
        }zip=output.ToArray();}
        release=new ReleaseInfo {Version="v9.9.9",Runtime="linux-x64",ReleaseDate=DateTime.UtcNow,AssetId=123,AssetSize=zip.Length,AssetDigest="sha256:"+Convert.ToHexString(SHA256.HashData(zip)),DownloadUrl="https://github.com/genielabs/HomeGenie/releases/download/v9.9.9/homegenie_9.9.9_linux-x64.zip"};
        handler=new Handler();handler.Respond=request=>Response(zip);manager=new ReleaseDownloadManager(Path.Combine(folder,"_update"),new HttpClient(handler));
    }
    [TearDown] public void Cleanup(){manager.Stop();Environment.CurrentDirectory=previous;Directory.Delete(folder,true);}
    private HttpResponseMessage Response(byte[] data){var r=new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(data)};r.Headers.ETag=new EntityTagHeaderValue("\"fixture\"");return r;}
    private ReleaseDownloadState Done(){Assert.That(SpinWait.SpinUntil(()=>!manager.Busy,10000),Is.True);return manager.Status();}
    [TestCase("linux-x64","start.sh")] [TestCase("win-x64","start.bat")] [TestCase("osx-x64","start.command")]
    public void OfficialOuterLauncherIsIgnoredDuringInPlaceUpdate(string runtime,string launcher)
    {
        release.Runtime=runtime;
        using var output=new MemoryStream();using(var archive=new ZipArchive(output,ZipArchiveMode.Create,true)){
            using(var writer=new StreamWriter(archive.CreateEntry("homegenie/release_info.xml").Open()))writer.Write($"<ReleaseInfo><Version>9.9.9</Version><Runtime>{runtime}</Runtime></ReleaseInfo>");
            using(var writer=new StreamWriter(archive.CreateEntry(launcher).Open()))writer.Write("launcher");
        }
        var bundle=output.ToArray();release.AssetSize=bundle.Length;release.AssetDigest="sha256:"+Convert.ToHexString(SHA256.HashData(bundle));handler.Respond=request=>Response(bundle);
        manager.Start(release);Assert.That(Done().State,Is.EqualTo("ready"));
        Assert.That(File.Exists(Path.Combine(folder,"_update/files",launcher)),Is.False);
    }
    [TestCase("unexpected.sh")] [TestCase("../start.sh")] [TestCase("/start.sh")]
    public void UnexpectedOuterFilesAreRejected(string name)
    {
        using var output=new MemoryStream();using(var archive=new ZipArchive(output,ZipArchiveMode.Create,true)){
            using var writer=new StreamWriter(archive.CreateEntry(name).Open());writer.Write("unexpected");
        }
        var bundle=output.ToArray();release.AssetSize=bundle.Length;release.AssetDigest="sha256:"+Convert.ToHexString(SHA256.HashData(bundle));handler.Respond=request=>Response(bundle);
        manager.Start(release);Assert.That(Done().State,Is.EqualTo("error"));
    }
    [Test] public void CompletedArchiveSurvivesReopenWithoutRedownloading()
    {
        manager.Start(release);Assert.That(Done().State,Is.EqualTo("ready"));
        manager=new ReleaseDownloadManager(Path.Combine(folder,"_update"),new HttpClient(handler));manager.Start(release);
        Assert.That(manager.Status().State,Is.EqualTo("ready"));Assert.That(handler.Calls,Is.EqualTo(1));
    }
    private void PartialFailure()
    {
        handler.Respond=request=>{var response=Response(zip);response.Content=new StreamContent(new BrokenStream(zip,zip.Length/2));response.Content.Headers.ContentLength=zip.Length;return response;};
        manager.Start(release);Assert.That(Done().State,Is.EqualTo("error"));
    }
    [TestCase(true)] [TestCase(false)] public void PartialDownloadsResumeSafelyOrRestartWhenRangeIsIgnored(bool supportsRange)
    {
        PartialFailure();manager=new ReleaseDownloadManager(Path.Combine(folder,"_update"),new HttpClient(handler));long received=manager.Status().Bytes;Assert.That(received,Is.GreaterThan(0));
        handler.Respond=request=>{
            Assert.That(request.Headers.Range.Ranges.First().From,Is.EqualTo(received));Assert.That(request.Headers.IfRange.EntityTag.Tag,Is.EqualTo("\"fixture\""));
            if(!supportsRange)return Response(zip);
            var response=Response(zip.AsSpan((int)received).ToArray());response.StatusCode=HttpStatusCode.PartialContent;response.Content.Headers.ContentRange=new ContentRangeHeaderValue(received,zip.Length-1,zip.Length);return response;
        };
        manager.Start(release);Assert.That(Done().State,Is.EqualTo("ready"));Assert.That(manager.Status().Bytes,Is.EqualTo(zip.Length));
    }
    [Test] public void ChangedValidatorIsNeverAppended()
    {
        PartialFailure();long length=new FileInfo(Path.Combine(folder,"_update/download/release.part")).Length;
        handler.Respond=request=>{var r=Response(zip.AsSpan((int)length).ToArray());r.StatusCode=HttpStatusCode.PartialContent;r.Content.Headers.ContentRange=new ContentRangeHeaderValue(length,zip.Length-1,zip.Length);r.Headers.ETag=new EntityTagHeaderValue("\"changed\"");return r;};
        manager.Start(release);Assert.That(Done().State,Is.EqualTo("error"));Assert.That(new FileInfo(Path.Combine(folder,"_update/download/release.part")).Length,Is.EqualTo(length));
    }
    [Test] public void FullPartialFileCanBeValidatedWithoutNetwork()
    {
        var state=new ReleaseDownloadState{Release=release,Key=ReleaseDownloadManager.Identity(release),State="paused",TotalBytes=zip.Length};
        string cache=Path.Combine(folder,"_update/download");File.WriteAllText(Path.Combine(cache,"state.json"),JsonConvert.SerializeObject(state));File.WriteAllBytes(Path.Combine(cache,"release.part"),zip);
        manager=new ReleaseDownloadManager(Path.Combine(folder,"_update"),new HttpClient(handler));manager.Start(release);Assert.That(Done().State,Is.EqualTo("ready"));Assert.That(handler.Calls,Is.Zero);
    }
    [Test] public void CorruptArchiveCannotEnableInstallation()
    {
        var corrupt=Encoding.UTF8.GetBytes("not a zip");release.AssetSize=corrupt.Length;release.AssetDigest="sha256:"+Convert.ToHexString(SHA256.HashData(corrupt));handler.Respond=request=>Response(corrupt);
        manager.Start(release);Assert.That(Done().State,Is.EqualTo("error"));Assert.Throws<InvalidOperationException>(()=>manager.Install(()=>UpdateChecker.InstallStatus.Success));
    }
    [Test] public void StagingChangesAreRejectedBeforeCallingInstaller()
    {
        manager.Start(release);Assert.That(Done().State,Is.EqualTo("ready"));File.AppendAllText(Path.Combine(folder,"_update/files/homegenie/release_info.xml"),"tampered");
        Assert.Throws<InvalidDataException>(()=>manager.Install(()=>throw new Exception("Installer must not run")));
    }
    [Test] public void VerifiedDownloadCanRunRealReleaseInstallerAndScheduleRequiredPrograms()
    {
        File.WriteAllText("groups.xml","user groups sentinel");manager.Start(release);Assert.That(Done().State,Is.EqualTo("ready"));
        Assert.That(manager.Install(()=>new UpdateChecker(null).InstallFiles()),Is.EqualTo(UpdateChecker.InstallStatus.RestartRequired));
        Assert.That(manager.Status().State,Is.EqualTo("installed"));Assert.That(File.Exists("data/program-updates/reconciliation/pending.txt"),Is.True);
        Assert.That(File.ReadAllText("groups.xml"),Is.EqualTo("user groups sentinel"));
    }
    [Test] public void ManualInstallationBlocksDownloadRequests()
    {
        using var lease=manager.BeginExternalInstall();Assert.Throws<InvalidOperationException>(()=>manager.Start(release));
    }
    [Test] public void NonGithubReleaseSourcesAreRejected()
    {
        release.DownloadUrl="http://example.com/archive.zip";Assert.Throws<InvalidDataException>(()=>manager.Start(release));Assert.That(handler.Calls,Is.Zero);
    }
    [TestCase(false)] [TestCase(true)] public void PauseAndServiceStopKeepARecoverablePartial(bool serviceStop)
    {
        using var blocked=new ManualResetEventSlim();
        handler.Respond=request=>{var r=Response(zip);r.Content=new StreamContent(new BlockingStream(zip,zip.Length/2,blocked));r.Content.Headers.ContentLength=zip.Length;return r;};
        manager.Start(release);Assert.That(blocked.Wait(3000),Is.True);
        if(serviceStop)manager.Stop();else manager.Pause();
        Assert.That(Done().State,Is.EqualTo("paused"));
        Assert.That(new FileInfo(Path.Combine(folder,"_update/download/release.part")).Length,Is.GreaterThan(0));
    }
    [Test] public void UnexpectedStagingFilesCannotBeInstalled()
    {
        manager.Start(release);Assert.That(Done().State,Is.EqualTo("ready"));File.WriteAllText(Path.Combine(folder,"_update/files/homegenie","unexpected.txt"),"extra");
        Assert.Throws<InvalidDataException>(()=>manager.Install(()=>throw new Exception("Must not run")));
    }
    [Test] public void DigestMismatchCannotProduceReadyState()
    {
        release.AssetDigest="sha256:"+new string('0',64);manager.Start(release);Assert.That(Done().State,Is.EqualTo("error"));
    }
    private sealed class BlockingStream:MemoryStream
    {
        private readonly int limit;private readonly ManualResetEventSlim blocked;
        public BlockingStream(byte[] bytes,int limit,ManualResetEventSlim blocked):base(bytes){this.limit=limit;this.blocked=blocked;}
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken token=default){
            if(Position>=limit){blocked.Set();await Task.Delay(Timeout.Infinite,token);return 0;}
            return await base.ReadAsync(buffer[..Math.Min(buffer.Length,limit-(int)Position)],token);
        }
    }
    [TestCase("linux-x64","Vulkan","_linux-x64-vulkan.zip")]
    [TestCase("win-x64","Cuda12","_win-x64-cuda12.zip")]
    [TestCase("linux-arm64","Release","_linux-arm64.zip")]
    public void ReleaseSelectionKeepsBuildEditionSeparateFromRuntime(string runtime,string configuration,string expected)
        =>Assert.That(UpdateChecker.AssetSuffix(runtime,configuration),Is.EqualTo(expected));
    [Test] public void TraversalEntryIsRejectedWithoutWritingOutsideStaging()
    {
        using var output=new MemoryStream();using(var archive=new ZipArchive(output,ZipArchiveMode.Create,true)){
            using var writer=new StreamWriter(archive.CreateEntry("homegenie/../../escape.txt").Open());writer.Write("escape");
        }
        var bad=output.ToArray();release.AssetSize=bad.Length;release.AssetDigest="sha256:"+Convert.ToHexString(SHA256.HashData(bad));handler.Respond=request=>Response(bad);
        manager.Start(release);Assert.That(Done().State,Is.EqualTo("error"));Assert.That(File.Exists(Path.Combine(folder,"_update","escape.txt")),Is.False);
    }
    [Test] public void AlreadyInstalledOrOlderReleasesCannotBeSubmittedAgain()
    {
        var installed=new ReleaseInfo {ReleaseDate=release.ReleaseDate};
        Assert.Throws<InvalidOperationException>(()=>UpdateChecker.EnsureReleaseIsNewer(installed,release));
        installed.ReleaseDate=release.ReleaseDate.AddDays(1);
        Assert.Throws<InvalidOperationException>(()=>UpdateChecker.EnsureReleaseIsNewer(installed,release));
        installed.ReleaseDate=release.ReleaseDate.AddDays(-1);
        Assert.DoesNotThrow(()=>UpdateChecker.EnsureReleaseIsNewer(installed,release));
    }
    [Test] public void StatusPollingOmitsTheManifestButKeepsItInTheDurableJournal()
    {
        manager.Start(release);Assert.That(Done().State,Is.EqualTo("ready"));
        Assert.That(manager.Status().Files,Is.Empty);
        var journal=JsonConvert.DeserializeObject<ReleaseDownloadState>(File.ReadAllText(Path.Combine(folder,"_update/download/state.json")));
        Assert.That(journal.Files.Count,Is.GreaterThan(0));
        var snapshot=manager.Status();snapshot.Release.Version="changed externally";
        Assert.That(manager.Status().Release.Version,Is.EqualTo(release.Version));
    }
    [Test] public void JournalWriteFailureDoesNotEscapeHostShutdown()
    {
        handler.Respond=request=>{Directory.CreateDirectory(Path.Combine(folder,"_update/download/state.json.tmp"));return Response(zip);};
        manager.Start(release);Assert.That(SpinWait.SpinUntil(()=>!manager.Busy,5000),Is.True);
        Assert.That(manager.Status().State,Is.EqualTo("error"));Assert.DoesNotThrow(()=>manager.Stop());
    }
    private sealed class Handler:HttpMessageHandler
    {
        public int Calls;public Func<HttpRequestMessage,HttpResponseMessage> Respond;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellation){Calls++;return Task.FromResult(Respond(request));}
    }
    private sealed class BrokenStream:MemoryStream
    {
        private readonly int limit;
        public BrokenStream(byte[] bytes,int limit):base(bytes){this.limit=limit;}
        public override int Read(byte[] buffer,int offset,int count){if(Position>=limit)throw new IOException("Simulated connection loss");return base.Read(buffer,offset,Math.Min(count,limit-(int)Position));}
        public override ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken token=default){if(Position>=limit)throw new IOException("Simulated connection loss");return base.ReadAsync(buffer[..Math.Min(buffer.Length,limit-(int)Position)],token);}
    }
}
