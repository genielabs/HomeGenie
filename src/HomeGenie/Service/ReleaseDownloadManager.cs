using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ICSharpCode.SharpZipLib.Zip;

namespace HomeGenie.Service;

[JsonObject(NamingStrategyType=typeof(CamelCaseNamingStrategy))]
public sealed class ReleaseDownloadState
{
    public string Key {get;set;}
    public ReleaseInfo Release {get;set;}
    public string State {get;set;}="idle";
    public long Bytes {get;set;}
    public long? TotalBytes {get;set;}
    public string ETag {get;set;}
    public string LastModified {get;set;}
    public string ArchiveHash {get;set;}
    public bool RequiresRestart {get;set;}
    public long InstallProcessStartedAt {get;set;}
    public string Error {get;set;}
    public Dictionary<string,string> Files {get;set;}=new();
    public double? Percent=>TotalBytes>0?Math.Min(100,100.0*Bytes/TotalBytes.Value):null;
}

// Owns one persistent GitHub release download. Browser lifetimes never own the worker.
public sealed class ReleaseDownloadManager
{
    private readonly object gate=new();
    private readonly string root,cache,stage;
    private readonly HttpClient client;
    private ReleaseDownloadState current=new();
    private CancellationTokenSource cancellation;
    private Task worker;
    private bool stopping,installing;
    private string Journal=>Path.Combine(cache,"state.json");
    private string Partial=>Path.Combine(cache,"release.part");
    private string Archive=>Path.Combine(cache,"release.zip");
    public ReleaseDownloadManager(string root,HttpClient client=null)
    {
        this.root=Path.GetFullPath(root);cache=Path.Combine(this.root,"download");stage=Path.Combine(this.root,"files");
        this.client=client??new HttpClient{Timeout=TimeSpan.FromMinutes(10)};
        Directory.CreateDirectory(cache);
        if(!File.Exists(Journal))return;
        current=JsonConvert.DeserializeObject<ReleaseDownloadState>(File.ReadAllText(Journal));
        if(current?.Release==null||current.Key!=Identity(current.Release)||current.Files==null)throw new InvalidDataException("Invalid release download journal.");
        ValidateRelease(current.Release);
        if(current.State is "downloading" or "validating" or "extracting")current.State="paused";
        if(current.State=="installing") {current.State="error";current.Error="Installation was interrupted or the service restarted. Check the installed release before continuing.";}
        if(current.State=="ready"&&!File.Exists(Path.Combine(stage,"homegenie","release_info.xml")))current.State="paused";
        current.Bytes=File.Exists(Partial)?new FileInfo(Partial).Length:File.Exists(Archive)?new FileInfo(Archive).Length:0;
        Save();
    }
    public static string Identity(ReleaseInfo r)=>Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(
        JsonConvert.SerializeObject(new{r.Version,r.Runtime,r.DownloadUrl,r.AssetId,r.AssetSize,r.AssetUpdatedAt,r.AssetDigest}))));
    private static void ValidateRelease(ReleaseInfo r)
    {
        if(r==null||!Uri.TryCreate(r.DownloadUrl,UriKind.Absolute,out var u)||u.Scheme!="https"||u.Host!="github.com"||
            !u.AbsolutePath.StartsWith("/genielabs/HomeGenie/releases/download/",StringComparison.OrdinalIgnoreCase)||u.UserInfo.Length!=0||string.IsNullOrWhiteSpace(r.Runtime)||string.IsNullOrWhiteSpace(r.Version))
            throw new InvalidDataException("Only HomeGenie GitHub release assets are supported.");
    }
    private ReleaseDownloadState Snapshot()=>new() {
        Key=current.Key,Release=current.Release==null?null:JsonConvert.DeserializeObject<ReleaseInfo>(JsonConvert.SerializeObject(current.Release)),
        State=current.State,Bytes=current.Bytes,TotalBytes=current.TotalBytes,ETag=current.ETag,LastModified=current.LastModified,
        ArchiveHash=current.ArchiveHash,Error=current.Error,RequiresRestart=current.RequiresRestart,InstallProcessStartedAt=current.InstallProcessStartedAt
        // The staging manifest stays in the journal; progress polling must not transmit thousands of file hashes.
    };
    private void Save()=>ProgramUpdateJobs.AtomicWrite(Journal,JsonConvert.SerializeObject(current));
    public ReleaseDownloadState Status(){lock(gate)return Snapshot();}
    public bool Busy {get{lock(gate)return installing||worker is {IsCompleted:false};}}
    public ReleaseDownloadState Start(ReleaseInfo release,bool restart=false)
    {
        ValidateRelease(release);
        lock(gate){
            if(stopping||installing)throw new InvalidOperationException("Updater is stopping or installing.");
            string key=Identity(release);
            if(worker is {IsCompleted:false}) {if(current.Key!=key)throw new InvalidOperationException("Another release download is running.");return Snapshot();}
            if(restart){File.Delete(Partial);File.Delete(Archive);current.ETag=current.LastModified=null;}
            if(!restart&&current.Key==key&&current.State=="ready"&&File.Exists(Path.Combine(stage,"homegenie","release_info.xml")))return Snapshot();
            if(current.Key==key&&current.State=="error"&&File.Exists(Archive))File.Delete(Archive);
            if(current.Key!=key){
                File.Delete(Partial);File.Delete(Archive);
                current=new ReleaseDownloadState{Key=key,Release=JsonConvert.DeserializeObject<ReleaseInfo>(JsonConvert.SerializeObject(release)),TotalBytes=release.AssetSize>0?release.AssetSize:null};
            }
            current.State="downloading";current.Error=null;Save();
            cancellation?.Dispose();cancellation=new CancellationTokenSource();var token=cancellation.Token;
            worker=Task.Run(()=>Download(token));return Snapshot();
        }
    }
    public ReleaseDownloadState Pause(){lock(gate){if(installing)throw new InvalidOperationException("Installation is running.");cancellation?.Cancel();return Snapshot();}}
    public void Stop(){Task task;lock(gate){stopping=true;cancellation?.Cancel();task=worker;}try{task?.Wait(TimeSpan.FromSeconds(5));}catch(AggregateException){ /* A failed journal must not abort host shutdown. */ }}
    private void Phase(string state){lock(gate){current.State=state;Save();}}
    private async Task Download(CancellationToken token)
    {
        try {
            if(!File.Exists(Archive))await Transfer(token);
            token.ThrowIfCancellationRequested();Phase("validating");
            string hash;using(var stream=File.OpenRead(Archive))hash=Convert.ToHexString(await SHA256.HashDataAsync(stream,token));
            if(!string.IsNullOrWhiteSpace(current.Release.AssetDigest)&&!string.Equals("sha256:"+hash,current.Release.AssetDigest,StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("GitHub asset digest does not match the downloaded archive.");
            using(var zip=new ZipFile(Archive))if(!zip.TestArchive(true))throw new InvalidDataException("ZIP integrity check failed.");
            Phase("extracting");var files=Extract(token);
            lock(gate){current.Files=files;current.ArchiveHash=hash;current.Bytes=new FileInfo(Archive).Length;current.State="ready";current.Error=null;Save();}
        }
        catch(OperationCanceledException) when(token.IsCancellationRequested){lock(gate){current.State="paused";Save();}}
        catch(Exception error){lock(gate){current.State="error";current.Error=error.Message;Save();}}
    }
    private async Task Transfer(CancellationToken token)
    {
        long offset=File.Exists(Partial)?new FileInfo(Partial).Length:0;
        if(offset>0&&current.Release.AssetSize==offset){File.Move(Partial,Archive,true);return;}
        // Without a validator, never append a partial file from a previous connection.
        if(offset>0&&current.ETag==null&&current.LastModified==null){File.Delete(Partial);offset=0;}
        using var request=new HttpRequestMessage(HttpMethod.Get,current.Release.DownloadUrl);
        request.Headers.UserAgent.ParseAdd("HomeGenie-Updater/2.0");request.Headers.AcceptEncoding.ParseAdd("identity");
        if(offset>0){request.Headers.Range=new RangeHeaderValue(offset,null);request.Headers.TryAddWithoutValidation("If-Range",current.ETag??current.LastModified);}
        using var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);
        if(response.StatusCode==HttpStatusCode.RequestedRangeNotSatisfiable&&offset>0){File.Delete(Partial);lock(gate){current.ETag=current.LastModified=null;current.Bytes=0;Save();}await Transfer(token);return;}
        response.EnsureSuccessStatusCode();
        if(response.Content.Headers.ContentEncoding.Any(e=>e!="identity"))throw new InvalidDataException("Unexpected encoded release response.");
        var range=response.Content.Headers.ContentRange;
        if(response.StatusCode==HttpStatusCode.PartialContent){
            if(offset==0||range?.From!=offset||!range.HasLength||range.To!=range.Length-1)throw new InvalidDataException("Invalid partial release response.");
            if(current.ETag!=null&&response.Headers.ETag?.ToString()!=current.ETag || current.ETag==null&&current.LastModified!=null&&response.Content.Headers.LastModified?.ToString("R")!=current.LastModified)
                throw new InvalidDataException("Release changed during resume. Restart the download.");
        }else if(response.StatusCode==HttpStatusCode.OK)offset=0;
        else throw new InvalidDataException("Unexpected release response status.");
        long? total=response.StatusCode==HttpStatusCode.PartialContent?range.Length:response.Content.Headers.ContentLength;
        if(current.Release.AssetSize>0&&total.HasValue&&total!=current.Release.AssetSize)throw new InvalidDataException("Release size changed.");
        lock(gate){current.TotalBytes=current.Release.AssetSize>0?current.Release.AssetSize:total;current.Bytes=offset;
            current.ETag=response.Headers.ETag is {IsWeak:false}?response.Headers.ETag.ToString():null;
            current.LastModified=response.Content.Headers.LastModified?.ToString("R");Save();}
        await using(var output=new FileStream(Partial,offset==0?FileMode.Create:FileMode.Append,FileAccess.Write,FileShare.Read))
        await using(var input=await response.Content.ReadAsStreamAsync(token)){
            var buffer=new byte[65536];var last=DateTime.UtcNow;
            while(true){int count;
                using(var readTimeout=CancellationTokenSource.CreateLinkedTokenSource(token)){
                    readTimeout.CancelAfter(TimeSpan.FromSeconds(45));
                    try{count=await input.ReadAsync(buffer,readTimeout.Token);}
                    catch(OperationCanceledException) when(!token.IsCancellationRequested){throw new TimeoutException("Release download stopped receiving data. The partial file can be resumed.");}
                }if(count==0)break;await output.WriteAsync(buffer.AsMemory(0,count),token);
                lock(gate){current.Bytes+=count;if(current.TotalBytes.HasValue&&current.Bytes>current.TotalBytes.Value)throw new InvalidDataException("Release exceeds expected size.");
                    if(DateTime.UtcNow-last>TimeSpan.FromMilliseconds(500)){output.Flush();Save();last=DateTime.UtcNow;}}
            }
            await output.FlushAsync(token);output.Flush(true);
        }
        if(current.TotalBytes.HasValue&&new FileInfo(Partial).Length!=current.TotalBytes)throw new EndOfStreamException("Release download is incomplete.");
        File.Move(Partial,Archive,true);
    }
    private void ClearDirectory(string path)
    {
        string full=Path.GetFullPath(path);if(!full.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.Ordinal))throw new InvalidDataException("Invalid staging path.");
        bool Linked(string p)=>(File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0||Directory.Exists(p)&&Directory.EnumerateFileSystemEntries(p).Any(Linked);
        if(Directory.Exists(full)){if(Linked(full))throw new InvalidDataException("Staging contains a symbolic link.");Directory.Delete(full,true);}
    }
    private Dictionary<string,string> Extract(CancellationToken token)
    {
        string unpack=Path.Combine(cache,"unpack");ClearDirectory(unpack);Directory.CreateDirectory(unpack);
        using(var zip=new ZipFile(Archive))foreach(ZipEntry entry in zip){
            token.ThrowIfCancellationRequested();if(!entry.IsFile)continue;
            string name=entry.Name.Replace('\\','/');
            if(name.Split('/').Any(p=>p is ".." or ".")||name.Contains(':')||((entry.ExternalFileAttributes>>16)&0xf000)==0xa000)
                throw new InvalidDataException("Invalid release archive path.");
            // Bundles include an outer launcher. In-place updates only install homegenie/.
            string launcher=current.Release.Runtime.StartsWith("win-",StringComparison.Ordinal)?"start.bat":current.Release.Runtime.StartsWith("osx-",StringComparison.Ordinal)?"start.command":"start.sh";
            if(name==launcher)continue;
            if(!name.StartsWith("homegenie/",StringComparison.Ordinal))throw new InvalidDataException("Invalid release archive path.");
            string path=Path.GetFullPath(Path.Combine(unpack,name));if(!path.StartsWith(unpack+Path.DirectorySeparatorChar,StringComparison.Ordinal))throw new InvalidDataException("Archive path escapes staging.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));using var input=zip.GetInputStream(entry);using var output=File.Create(path);input.CopyTo(output);
        }
        string releasePath=Path.Combine(unpack,"homegenie","release_info.xml");var info=UpdateChecker.GetReleaseFile(releasePath);
        if(info==null||info.Version.TrimStart('v')!=current.Release.Version.TrimStart('v')||info.Runtime!=current.Release.Runtime)throw new InvalidDataException("Archive release/runtime does not match the selected asset.");
        info.ReleaseDate=current.Release.ReleaseDate.ToUniversalTime();using(var writer=new StreamWriter(releasePath))new System.Xml.Serialization.XmlSerializer(typeof(ReleaseInfo)).Serialize(writer,info);
        var hashes=Directory.GetFiles(unpack,"*",SearchOption.AllDirectories).ToDictionary(p=>Path.GetRelativePath(unpack,p),Hash);
        token.ThrowIfCancellationRequested();ClearDirectory(stage);Directory.Move(unpack,stage);return hashes;
    }
    private static string Hash(string file){using var input=File.OpenRead(file);return Convert.ToHexString(SHA256.HashData(input));}
    public IDisposable BeginExternalInstall()
    {
        lock(gate){if(stopping||installing||worker is {IsCompleted:false})throw new InvalidOperationException("Another update operation is running.");installing=true;}
        return new Lease(()=>{lock(gate){installing=false;if(current.State=="ready"){current.State="paused";Save();}}});
    }
    private sealed class Lease : IDisposable
    {
        private Action release;
        public Lease(Action release){this.release=release;}
        public void Dispose()=>Interlocked.Exchange(ref release,null)?.Invoke();
    }
    public UpdateChecker.InstallStatus Install(Func<UpdateChecker.InstallStatus> install)
    {
        lock(gate){
            if(stopping||installing||worker is {IsCompleted:false}||current.State!="ready")throw new InvalidOperationException("No verified release is ready to install.");
            if(!File.Exists(Archive)||Hash(Archive)!=current.ArchiveHash||current.Files.Count==0)throw new InvalidDataException("Release archive changed after verification.");
            var stagedFiles=Directory.GetFiles(stage,"*",SearchOption.AllDirectories).Select(p=>Path.GetRelativePath(stage,p)).ToHashSet(StringComparer.Ordinal);
            if(!stagedFiles.SetEquals(current.Files.Keys))throw new InvalidDataException("Release staging contains missing or unexpected files.");
            foreach(var item in current.Files){string path=Path.GetFullPath(Path.Combine(stage,item.Key));if(!path.StartsWith(stage+Path.DirectorySeparatorChar,StringComparison.Ordinal)||!File.Exists(path)||Hash(path)!=item.Value)throw new InvalidDataException("Release staging changed after verification.");}
            installing=true;current.State="installing";current.InstallProcessStartedAt=new DateTimeOffset(System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()).ToUnixTimeMilliseconds();Save();
        }
        try{var result=install();lock(gate){current.State=result==UpdateChecker.InstallStatus.Error?"error":"installed";current.RequiresRestart=result==UpdateChecker.InstallStatus.RestartRequired;Save();}return result;}
        catch(Exception error){lock(gate){current.State="error";current.Error=error.Message;Save();}throw;}
        finally{lock(gate)installing=false;}
    }
}
