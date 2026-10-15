---
name: homegenie-orchestration
description: Create a Director that schedules and runs cooperating automation programs through the unified Genie.
compatibility: AI.Genies/Gemini orchestration APIs; automation capability required.
metadata:
  version: "1.0.2"
---
# Unattended orchestration

When the user explicitly requests a Director and cooperating agents, deploy the Director as kind=program in this response. One program per response does not require separate interactive user prompts: the Director can generate its helpers through the scheduling API below. Do not replace a requested multi-program design with one combined program or require another confirmation when the request is already clear.

Generated programs use `AI.Genies/Gemini` endpoints, never the disabled legacy `AI.Automan/Gemini` provider.

## APIs

- `Api.Call("AI.Genies/Gemini/Prompt.Schedule", prompt)` queues one create/update request. It returns an object with Status=Ok, taskId and state=queued, or Status=Error and Message. Validate the result before waiting. The worker uses the shared Action Executor and compilation repair. Jobs run serially; it does not accept widget/device tasks.
- `Api.Call("AI.Genies/Gemini/Task.Status", taskId)` returns state=queued/running/completed/failed, with programId after successful deployment and message on failure. It can return Status=Error for an expired/unknown job. Stop waiting on any failure. Use a bounded wait (e.g. 10 minutes), Pause(2) between checks, and check Program.IsRunning. Never wait forever for a program that failed compilation.
- `Api.Call("AI.Genies/Gemini/Prompt.Ask", text)` returns plain AI text, without deployment, chat history or system context. Use it for summarization/translation. On failure it returns Status=Error and Message: check the actual result type/status before writing a summary. Never use Prompt.Schedule to summarize data.

Use `JObject.FromObject(result)` when a backend API returns an object; use `JObject.Parse(text)` only for a JSON string. Do not assume the frontend data.response wrapper exists in backend calls.

## Director and helper lifecycle (required)

The HG scheduler repeatedly evaluates Setup. `Program.WithName(name).Run()` sets WillRun for that target's Setup; it does NOT explicitly start the helper's Main. Even if a user requests that spelling, implement the intended execution using the actual native API `Program.Run(name)` (or `Program.Run(addressString)`). `Program.WithName(name)` is for existence checks, IsRunning and parameter access.

1. Deploy a persistent Director with one-time options inside Program.Setup, an automatic Program.Run() trigger in Setup, and a Main loop using while (Program.IsRunning) and Pause.
2. Give helpers unique names, e.g. `"News Scout #" + Program.Module.Address`, and reuse existing helpers. If missing, schedule creation and wait for Task.Status=completed with a bounded timeout. Treat Status=Error, failed, unknown task or timeout as failure; never infer successful deployment merely from program existence after timeout.
3. Request explicit-run helpers: their Setup contains only Program.Setup option declarations. It must NOT contain Program.Run(), unconditional automatic triggers or GoBackground. Their Main performs ONE job, emits Running on entry, Completed only on success or Error plus a message on failure, and returns. This prevents repeated automatic executions after the Main exits.
4. After confirmed creation, explicitly execute the Scout with `Program.Run(scoutName)`. Poll `Program.WithName(scoutName).IsRunning` with Pause and a deadline, checking the Director's Program.IsRunning. Do not use unbounded Program.WaitFor. Once stopped, require its completion status AND a fresh, nonempty output file before starting the Editor with `Program.Run(editorName)` and the same bounded wait/validation. An idle program, a stale Completed status or a leftover file is not proof of this cycle's success: record the output modification time before starting and verify it advances; use a cycle identifier if needed.
5. On timeout, report the failure and do not start the dependent stage or start another copy of a still-running helper. Do not emit overall success unless both jobs and the new final file have been verified. Completed reports a result; returning from Main ends execution.
6. Repeat at the requested interval with interruptible Pause and Program.IsRunning checks. Be explicit whether the interval is measured from cycle start or completion; a fixed cadence should subtract elapsed processing time rather than always sleeping another full 60 minutes.

Do not create a fresh helper on every cycle or silently combine the requested three-program design. Include the above lifecycle requirements in the prompts sent to Prompt.Schedule, not just in the Director's own sources.

## Files and text results (required)

Use `Path.Combine(Data.GetFolder("SharedAutomanWorkspace"), "raw_news.txt")` and the same named folder for the summary; create it if needed. Do not hardcode installation-relative `data/programs/...` paths in generated source. Use atomic replacement or ensure the producer has finished before reading. Fetch raw RSS/XML with Net.WebService(url).Call(), parse it as XML and extract text, stripping markup from descriptions. Isolate feed failures so working feeds remain usable; zero extracted news is failure, not Completed.

For a file editor option, the third field is its label and the fourth field embeds the file path, exactly:

```csharp
Program.Setup(() => {
    string reportPath = Path.Combine(Data.GetFolder("SharedAutomanWorkspace"), "top10_news.md");
    Program.AddOption("ReportFile", "", "Top 10 news", $"editor:file:{reportPath}:markdown");
});
```

`editor:file:text/markdown` is invalid: it omits the file path. Setting the option Value to a path does not fix an invalid FieldType.

Prompt.Ask returns a plain string on success, or an error object on failure. Accept ONLY a nonempty string as the summary. For any non-string result, extract Status/Message if it is an object and report Error, then return; never save JObject.ToString(), Response, ResponseValue, an exception or a fallback object serialization as Markdown. Request a clean Markdown list of up to ten actual facts from the supplied news, without inventing extra facts if fewer are available. Verify a fresh nonempty summary before reporting Completed. Expose its file through the correct editor option.

Keep ordinary deployment/compilation/recovery in the host. Calling the documented Prompt.Schedule from a requested Director is the explicit orchestration exception, not permission to invoke raw Programs.Add/Compile APIs.

Scheduling is limited to 32 waiting tasks. Task records are bounded and memory-only: queued jobs/status are lost on Genie restart and are not replayed automatically. Never blindly reschedule after a timeout or unknown task; check whether the uniquely named helper already exists. Disabling Automation blocks newly queued jobs and jobs not yet started. Running jobs finish through the existing serialized executor.
