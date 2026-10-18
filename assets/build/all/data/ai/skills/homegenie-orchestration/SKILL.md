---
name: homegenie-orchestration
description: Create a Director that schedules and runs cooperating automation programs through the unified Genie.
compatibility: AI.Genies/Gemini orchestration APIs; automation capability required.
metadata:
  version: "1.0.4"
---
# Unattended orchestration

When the user explicitly requests a Director and cooperating agents, deploy the Director as kind=program in this response. One program per response does not require separate interactive user prompts: the Director can generate its helpers through the scheduling API below. Do not replace a requested multi-program design with one combined program or require another confirmation when the request is already clear.

Generated programs use `AI.Genies/Gemini` endpoints, never the disabled legacy `AI.Automan/Gemini` provider.

## APIs

- `Api.Call("AI.Genies/Gemini/Request.Execute", new { version = 1, prompt = text })` starts an independent action-capable request. It returns requestId and state=queued, or failed/busy with error. The provider's persisted context/capabilities apply; automation must be enabled to create helpers. The common `System.Utility/AiGenie/Request.Execute` route also accepts an optional providerId (otherwise the configured default). If submission returns busy without a requestId, nothing started: Pause(2) and retry within the same deadline. Do not resubmit an accepted request or an unknown outcome.
- `Api.Call("AI.Genies/Gemini/Request.Status", new { version = 1, requestId })` returns queued/running/cancelling/completed/failed/cancelled/busy/output_limit/context_limit/not_found. Use Pause(2), a deadline and Program.IsRunning checks; cancel on timeout/stop via Request.Cancel with the same version/requestId. Do not automatically repeat an uncertain request. For helper creation, require completed AND a confirmed Program.Deploy receipt in result.actions: endpoint ends in /Program.Deploy, response.Success=true, response.State=completed and response.Response.status=success. Read the helper ID from response.Response.program_id. A completed plain reply without that receipt is not a deployment. Do not assume the former Task.Status programId field exists.
- `Api.Call("System.Utility/AiGenie/Request.Generate", new { version = 1, prompt = text, providerId = "933" })` starts text-only generation. Omit providerId to use AI Genie's configured default. It returns version, requestId, providerId and state=queued, or failed/busy with error. Poll `Request.Status` on the same service using `{ version = 1, requestId }`. Wait with Pause(2), a deadline and Program.IsRunning checks. Only state=completed supplies final text. On deadline or program stop, call `Request.Cancel` with the same ID and stop this workflow. Requests use the provider's allowed context, never chat history, and never execute generated commands. Direct provider routes such as `AI.Genies/Gemini/Request.Generate` accept the same body without providerId. Do not call the removed Gemini Prompt.Ask endpoint.

Use `JObject.FromObject(result)` when a backend API returns an object; use `JObject.Parse(text)` only for a JSON string. Do not assume the frontend data.response wrapper exists in backend calls.

## Director and helper lifecycle (required)

The HG scheduler repeatedly evaluates Setup. `Program.WithName(name).Run()` sets WillRun for that target's Setup; it does NOT explicitly start the helper's Main. Even if a user requests that spelling, implement the intended execution using the actual native API `Program.Run(name)` (or `Program.Run(addressString)`). `Program.WithName(name)` is for existence checks, IsRunning and parameter access.

1. Deploy a persistent Director with one-time options inside Program.Setup, an automatic Program.Run() trigger in Setup, and a Main loop using while (Program.IsRunning) and Pause.
2. Give helpers unique names, e.g. `"News Scout #" + Program.Module.Address`, and reuse existing helpers. If missing, request creation through Request.Execute and wait for Request.Status=completed plus a successful deployment receipt with a bounded timeout. Treat failed, cancelled, busy, output/context limits, not_found or timeout as failure; never infer successful deployment merely from program existence after timeout.
3. Request explicit-run helpers: their Setup contains only Program.Setup option declarations. It must NOT contain Program.Run(), unconditional automatic triggers or GoBackground. Their Main performs ONE job, emits Running on entry, Completed only on success or Error plus a message on failure, and returns. This prevents repeated automatic executions after the Main exits.
4. After confirmed creation, explicitly execute the Scout with `Program.Run(scoutName)`. Poll `Program.WithName(scoutName).IsRunning` with Pause and a deadline, checking the Director's Program.IsRunning. Do not use unbounded Program.WaitFor. Once stopped, require its completion status AND a fresh, nonempty output file before starting the Editor with `Program.Run(editorName)` and the same bounded wait/validation. An idle program, a stale Completed status or a leftover file is not proof of this cycle's success: record the output modification time before starting and verify it advances; use a cycle identifier if needed.
5. On timeout, report the failure and do not start the dependent stage or start another copy of a still-running helper. Do not emit overall success unless both jobs and the new final file have been verified. Completed reports a result; returning from Main ends execution.
6. Repeat at the requested interval with interruptible Pause and Program.IsRunning checks. Be explicit whether the interval is measured from cycle start or completion; a fixed cadence should subtract elapsed processing time rather than always sleeping another full 60 minutes.

Do not create a fresh helper on every cycle or silently combine the requested three-program design. Include the above lifecycle requirements in the prompts sent to Request.Execute, not just in the Director's own sources.

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

Request.Generate starts an asynchronous text request. Validate its requestId before polling Request.Status. Accept ONLY state=completed with a nonempty string in text as the summary. queued/running/cancelling are unfinished; failed/busy/cancelled/output_limit/context_limit/not_found are not summaries. On failure, emit Error using the reported error and return; never save the whole result object, Response, ResponseValue, an exception or partial text as Markdown. Use a bounded wait and cancel on timeout or program stop. Request a clean Markdown list of up to ten actual facts from the supplied news, without inventing extra facts if fewer are available. Verify a fresh nonempty summary before reporting Completed. Expose its file through the correct editor option.

Keep ordinary deployment/compilation/recovery in the host. Calling the documented Request.Execute from a requested Director is the explicit orchestration exception, not permission to invoke raw Programs.Add/Compile APIs.

Background requests accept one active job per provider. Records are bounded and memory-only; restarting AI Genie clears them. No automatic replay. Never blindly repeat after timeout/unknown outcome: inspect receipts and check whether the uniquely named helper already exists. Context/capabilities are checked again before effects; cancellation prevents subsequent steps but does not undo completed actions.
