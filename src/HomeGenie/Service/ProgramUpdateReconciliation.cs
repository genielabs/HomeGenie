using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace HomeGenie.Service;

// Durable one-shot bridge between a release replacement and program-only installation.
// The first release using this code also works when installed by an older updater.
public sealed class ProgramUpdateReconciliation
{
    private readonly string folder;
    private readonly Func<ProgramUpdateReport> inspect;
    private readonly Func<string,string,ProgramUpdateJob> start;
    private readonly Func<string,ProgramUpdateJob> status;
    public ProgramUpdateReconciliation(string folder, Func<ProgramUpdateReport> inspect,
        Func<string,string,ProgramUpdateJob> start, Func<string,ProgramUpdateJob> status)
    { this.folder=folder;this.inspect=inspect;this.start=start;this.status=status; }

    public sealed class State
    {
        public string Release {get;set;}
        public string PendingToken {get;set;}
        public string Phase {get;set;}
        public string RequestId {get;set;}
        public string Fingerprint {get;set;}
        public string JobId {get;set;}
        public string Error {get;set;}
    }
    public static void MarkPending(string folder) => ProgramUpdateJobs.AtomicWrite(Path.Combine(folder,"pending.txt"),Guid.NewGuid().ToString("N"));
    public static string ReleaseIdentity(string path)
    {
        if (!File.Exists(path)) return null;
        var root=XDocument.Load(path).Root;
        string version=(string)root?.Element("Version"),date=(string)root?.Element("ReleaseDate");
        if (string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(date)) throw new InvalidDataException("Invalid release identity; required programs were not reconciled.");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(version+"\n"+date)));
    }
    public State Run(string release)
    {
        string pendingPath=Path.Combine(folder,"pending.txt"),statePath=Path.Combine(folder,"state.json");
        string token=File.Exists(pendingPath)?File.ReadAllText(pendingPath).Trim():null;
        if (token!=null && !Guid.TryParseExact(token,"N",out _)) throw new InvalidDataException("Invalid reconciliation marker.");
        if (release==null && token==null) return null;
        var previous=File.Exists(statePath)?JsonConvert.DeserializeObject<State>(File.ReadAllText(statePath)):null;
        if (File.Exists(statePath) && (previous==null || !new[]{"prepared","accepted","complete","failed"}.Contains(previous.Phase) || !Guid.TryParse(previous.RequestId,out _)))
            throw new InvalidDataException("Invalid required-program reconciliation record; manual review is required.");
        bool same=previous!=null && previous.Release==release && previous.PendingToken==token;
        if (same && previous.Phase!="prepared") return previous;
        var state=same?previous:new State {Release=release,PendingToken=token,RequestId=Guid.NewGuid().ToString("N"),Phase="prepared"};
        void Save()=>ProgramUpdateJobs.AtomicWrite(statePath,JsonConvert.SerializeObject(state));
        try {
            if (!same)
            {
                var report=inspect();
                if (!report.Complete) throw new InvalidOperationException("Required-program reconciliation deferred: package inspection is incomplete. Review Programs updates.");
                state.Fingerprint=report.Fingerprint;
                if (!report.Items.Any(i=>i.Instances.Count==0 && i.Reasons.Any(r=>r is "missingRequired" or "missingDependency")))
                { state.Phase="complete";Save();return state; }
                // Persist the request UUID BEFORE launching. Recovery reuses it, never creates a second job.
                Save();
            }
            var job=start(state.Fingerprint,state.RequestId);
            state.JobId=job.Id;state.Phase="accepted";Save();return state;
        }
        catch (Exception error)
        {
            // An ambiguous accepted request must not be retried with a fresh identity.
            // A prepared record is retained if acceptance cannot be determined.
            try {
                if (state.JobId!=null && status(state.JobId)!=null) state.Phase="accepted";
                else state.Phase="failed";
                state.Error=error.Message;Save();
            } catch { /* Keep the previous flushed state; do not hide the original error. */ }
            throw;
        }
    }
}
