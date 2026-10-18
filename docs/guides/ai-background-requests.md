# Background AI requests

`Request.Generate` produces text without using or modifying the chat conversation, and without interpreting or executing commands in the output. Each request starts from a fresh provider context. These endpoints are implemented by automation programs, with no core API changes.

## Entry points

| Endpoint prefix | Provider selection |
| --- | --- |
| `System.Utility/AiGenie` | Optional string `providerId`; otherwise program 583's `ConfigureOptions.DefaultProvider` |
| `AI.Genies/Gemini` | Gemini program 933 |
| `AI.Genies/Lailama` | Lailama program 940 |

Append `Request.Generate`, `Request.Execute`, `Request.Status` or `Request.Cancel`. Use POST with JSON for HTTP calls, prefixed with `/api/`. Backend programs use `Api.Call` with objects directly, without HTTP or `/api/`.

Providers advertise `Options.requests = { version: 1, generate: true, execute: true }` through `Programs.Implements`. Disabled, missing, ambiguous or outdated providers fail explicitly; no fallback to another provider occurs. The selected default is resolved for each new request. Status and cancellation remain attached to the original provider.

## Generate and retrieve the result

```csharp
var raw = Api.Call("System.Utility/AiGenie/Request.Generate", new {
    version = 1,
    providerId = "933", // omit to use the configured default
    prompt = "Write one sentence explaining what HomeGenie does."
});
var accepted = raw is string json ? JObject.Parse(json) : JObject.FromObject(raw);
```

Accepted response:

```json
{"version":1,"requestId":"server-generated-id","providerId":"933","state":"queued","text":null,"error":null,"createdAt":"..."}
```

Validate `state` and a nonempty `requestId` before polling. Request.Status body:

```json
{"version":1,"requestId":"server-generated-id"}
```

Only `state: "completed"` provides a nonempty final `text`. The text can contain Markdown or code; it is returned as data and is never executed. Do not save a serialized status object as an AI summary.

Use a bounded polling loop, Pause(2) between checks, and Program.IsRunning checks. Stop on completion or failure. On timeout/program shutdown, call Request.Cancel with the same version/requestId and stop the workflow. Cancellation is cooperative: `cancelling` remains pending until native inference exits. Late output is discarded. Do not automatically retry uncertain requests.

States: `queued`, `running`, `cancelling`, `completed`, `cancelled`, `failed`, `busy`, `output_limit`, `context_limit`, `not_found`. Invalid input or unavailable providers fail without starting a job. Only one background request per provider is accepted at a time; Lailama also returns busy while its native model is occupied by chat/loading. It does not allocate a second copy of the model or publish background tokens into the chat.

At most 100 jobs/results are held in memory. Old completed results are evicted as new requests arrive. Restarting AI Genie clears all jobs/results and cancels outstanding work; not_found is explicit. Status cannot recover a result after restart. Prompt size is limited to 256 KiB, result text to 512 KiB; existing provider/model output limits still apply.

## Execute an independent request

Use the same body as Generate, sent to `System.Utility/AiGenie/Request.Execute` or directly to `AI.Genies/{provider}/Request.Execute`:

```json
{"version":1,"providerId":"933","prompt":"Turn on the porch light."}
```

Poll Request.Status and cancel with Request.Cancel exactly as for Generate. Generate and Execute share the same provider slot; a second background request is busy while the first is active. Execute starts with no chat history. Gemini uses the existing structured coordinator; Lailama uses the existing text/context/artifact coordinator with a native session dedicated to the whole background request, including lookup and repair rounds. Background tokens and resets are not published to the chat UI.

Execute reads the selected provider's stored `AI.Chat.Context` and `AI.Chat.Capabilities`. Local defaults/master sharing follow Lailama's policy. Callers cannot increase permissions. Policies are checked again before additional lookups/actions and intersected with the original request: changes can restrict an active request but cannot expand it.

Device controls, configurable module settings, automation deployment/repair and generated widget/composite output use the existing action classes and canonical contracts. Read-only lookups require their context categories; device writes require deviceControl. Program and widget work require their separate capabilities. Generated widget sources are returned in the result; Execute does not automatically save or place a widget on a dashboard.

Execute status additionally includes:

- `effectsMayHaveOccurred`: conservative indication that execution may have crossed an action boundary. It remains true if the provider disappears after dispatch and no correlated result can be recovered.
- `result.reply` and `result.outcome`: final coordinator output when available.
- `result.actions`: retained executor receipts, including known outcomes and explicit unknown outcomes. An entry marked returned means the executor returned a response; inspect that response's Success/State to determine the actual result.

Cancelling prevents subsequent work; it does not undo a command already sent. Status may be cancelled with text=null while result.actions still contains completed actions. Likewise failed/output_limit/context_limit can follow earlier action attempts. Never automatically repeat an Execute request after an error, timeout or cancellation; inspect receipts and the current target state first. Compilation repair and recovery retain the existing bounded behavior for the same deployment identity; uncertain execution is never automatically replayed.

The common `Request.Run` and provider `Provider.Request.Execute` bridges accept native objects (including CancellationToken and an inference delegate); they are internal in-process boundaries, not JSON alternatives to the public request API. Only provider transport/session handling lives in those adapters. Policy, lookups, execution orchestration and results remain in AI Genie 583.

## Policy and isolation

The service reads `AI.Chat.Context` from the selected provider module for each job. Lailama's `ConfigureOptions.SendContextData` master switch is also honored. Callers cannot override context selections or permissions through the request body. Generate has no action execution, even when the provider's chat allows device control or program creation. Selected system context may be sent to the selected provider, including cloud providers, as configured by the user.

Local generation exclusively leases the already-loaded native context, resets its conversation contents for the request and invalidates chat-session reuse. The persisted chat stays unchanged; the next chat turn rebuilds its native conversation normally. Cancellation does not release native resources until inference has exited.

## Migration and next step

Gemini's former Prompt.Ask endpoint is removed. Replace it with Request.Generate plus bounded Request.Status polling and use the completed result's text field. No alias is provided. Existing generated programs using Prompt.Ask need updating.

## Scheduler Genie command

The scheduler Genie preset submits Request.Execute with the selected provider program ID. Default omits providerId, resolving AI Genie's current DefaultProvider on every run. Only enabled providers declaring requests.version=1 and requests.execute=true appear as explicit choices. Unknown/unavailable choices fail; there is no silent fallback.

The preset uses `wait: true, timeoutSeconds: 600`. This optional Execute mode keeps the same job/ID/receipts but waits inside the common C# service until a terminal outcome. On deadline it requests cancellation and returns a non-completed state; on native thread interruption its C# finally requests cancellation before propagating the exception. Default Execute requests remain asynchronous. timeoutSeconds is allowed only with wait=true, ranges from 1 to 3600, and defaults to 600.

Native waiting is deliberate: Jint lets CLR exceptions escape without running JavaScript finally. The scheduler core is unchanged. Its existing per-event thread remains active during the request, prevents overlap of that event and keeps subsequent preset actions ordered. A non-completed response throws a scheduler error and stops following commands; it never repeats an uncertain request. Disabling/stopping the running script triggers the existing thread interruption and native cancellation path. Completed actions are not rolled back.

Prompt text and optional provider ID are rendered as a JSON object, preserving quotes, newlines, backslashes and literal dollar tokens. Multiple Genie actions have separate scopes and selections across each/start/end slots. Other preset templates are unchanged.

Gemini and Lailama no longer register Prompt.Schedule; Gemini's former Task.Status queue/worker is removed as well. No compatibility aliases are provided. Reopen and save an existing generated Genie schedule to regenerate its stored script from the new template. Custom scripts and generated programs using the removed endpoints require manual migration to Request.Execute/Status/Cancel. Historic Automan program endpoints remain separate legacy program functionality.

The new templates require AI Genie 583's native-wait support: update the packages/programs before running the updated scheduler action.
