# Shared Genie engine: implementation plan

Status: program 583 (AI Genie 0.1.1) is connected to Gemini Genie 1.0.10 for chat and text-only generation. Shared context/action/history orchestration extraction and the Lailama adapter remain pending. Initial live acceptance passed on Vulkan on 2026-10-17.

## Scope and evidence

The distributed package is the reference, not a running installation's custom scripts:

- `assets/build/all/data/packages/homegenie/homegenie-ml-ai/programs.xml`: Gemini Genie 933 and Lailama 940, including Setup/Main/Context.
- `assets/build/all/data/packages/homegenie/homegenie-base/programs.xml`: Context Engine, Chat History and Action Executor services.
- `src/HomeGenie/Automation/Engines/CSharpAppFactory.cs`: automation compilation and assembly references.
- YoT `src/app/services/ai-chat.service.ts`: Implements-driven feature discovery, version-1 requests and module-backed preferences.

Gemini 933 already owns selected-context assembly, skill composition, capability/schema filtering, source lookup, response validation, deployment/compilation repair, widget/composite presentation, scheduled tasks, metrics and history persistence. Gemini generation transport is now behind the provider boundary; rate-limit state and cached-content lifecycle remain in that coordinator for this first extraction. Its main request path is blocking generateContent with JSON Schema; it does not currently stream responses or expose Prompt.Cancel.

Lailama 940 owns model selection/download/loading, warmup, LLamaSharp ChatSession/KV state, templates, inference, token streaming and native cancellation/cleanup. It restores provider session history from Chat History, injects optional system/device context, then extracts text/api-call markers and invokes the shared Action Executor. It does not yet implement Gemini's version-1 context/capability envelope, source-lookup/repair loop or full program/widget/composite coordinator. Its GetContext/Render path differs from Gemini's scoped-context/Prompt.Compose path.

There is reusable infrastructure already: Response.Normalize, Execute, Program.Deploy and Program.Recover on Action Executor; shared skills/Context policies; separate history stores; module preferences and Program.Implements descriptors. Preserve these services rather than replacing them.

## Recommended code boundary

Use the **AI Genie utility automation program (583)** as the shared service. This supersedes the earlier proposal to place the engine in the core assembly. The provider-facing Genie receives chat requests and calls the utility with `Api.Call`; the utility calls a distinct provider generation handler. The provider handler must never re-enter Prompt.Submit or acquire a lock held by its caller.

`ApiHelper.Call` dispatches dynamic handlers synchronously through `InterfaceControl` and `ProgramDynamicApi`. The original `Data` object and handler return value remain in memory. Only unhandled system-domain calls use its HTTP fallback. No core changes or shared assembly identities are required: the initial contract uses BCL dictionaries, native message objects and a cooperative CancellationToken. Public request/history formats remain unchanged.

The service is now enabled/required in the base package and included as a dependency of the AI package. Existing installations must install/enable 583 before using Gemini Genie 1.0.10. Missing/incompatible service responses produce a visible error; there is no silent fallback to the old transport. No runtime inventory is edited during development. This is transport extraction, not yet a shared complete chat coordinator.

The engine should depend on an explicit host abstraction, not on static global HomeGenie state or an untyped copy of the entire automation script. Inject the host services for API calls, preferences, module lookup, events, time, persistence and logging. Provider adapters own their transport/session resources, never permission policy or execution decisions.

| Shared engine | Provider adapter | Existing services |
| --- | --- | --- |
| Request validation and per-Genie serialization | Invocation, transport and provider message mapping | Context policy, skills and prompt composition |
| Context/capability policy and prompt plan | Gemini payload/schema conversion and provider errors | Chat History storage/import/export |
| History selection and outcome persistence | Lailama model/download/options/native session | Action normalization and execution journal |
| Canonical response handling and action routing | Model templates, stop sequences and token counts | Program deployment/recovery |
| Bounded lookup/repair and stable action identities | Streaming, cancellation and finish reasons | Existing YoT artifact cards and previews |
| Task queue/status, metrics and UI artifacts | Local token/Context limits and session reset | MIG.Audio and current voice UI |

## Proposed internal provider contract

Define versioned neutral request/result types before extraction. Names are illustrative:

- Provider descriptor: provider/model identity, text/structured-output support, schema constraints, optional native tool support, streaming/cancellation and known context/output limits. Technical abilities are separate from user-authorized capabilities and observed model quality.
- Generate request: system instructions, selected conversation messages, purpose (conversation, action, text-only or repair), output specification, generation budget and cancellation token.
- Generate result: final visible text, normalized finish status (complete, cancelled, output limit, context limit, failure), usage when available and structured error/retry information. Do not invent usage counts when unavailable.
- Progress callback: request ID plus typed text/status events. Adapters filter provider thought/technical tokens. Action JSON is buffered rather than shown/spoken as ordinary answer text; partial or cancelled output must never execute.
- Local session lifecycle: invalidate/reconcile cached inference state with the engine's context/history fingerprint; stop after active native work drains, reusing LailamaNativeLifetime's guarantees. Rebuild inference state when the selected context/history changes without reloading model weights unnecessarily.

The common service serializes active generation per Genie owner identity. The existing wrappers still own their chat/task serialization in this first extraction. Busy/cancel/error outcomes must be explicit. Different Genies must not share permissions, history, task dictionaries or KV state accidentally. Queueing chat/Ask/scheduled work must be specified rather than hidden behind indefinite locks.

## Response formats and small local models

Retain the mixed strategy: constrained JSON where supported, native function-call adapters later, and existing text/api-call markers for small local models. Normalize the final response before a shared permission check and action dispatch. Reuse the Action Executor contract instead of maintaining divergent validators in 933 and 581; preserve all current validation, module-existence and recovery checks.

For a small model's device/report workload, compose a compact prompt with selected devices/current values and only relevant instructions. Do not send the full automation/widget schema or all source files. Reports can use the supplied current snapshot and return plain text without executing a command. Marker commands must receive the same applicable capability, context and target checks before execution; format choice cannot widen authority. Keep existing result metadata and reusable api-call code cards.

If the user enables code/widget capabilities, add the relevant skills and response instructions within an explicit context/output budget. Do not silently omit required instructions or truncate generated code into an executable artifact. If the request does not fit, report the limit and suggest a narrower task. Optional schema/grammar acceleration requires a separate check against the actual LLamaSharp backend; it is not assumed in the first adapter.

Gemini's current bounds (12 total rounds, up to 3 lookups, 2 contract repairs, 5 deployment attempts) must become policy settings with finite defaults. Local interactive workloads should not inherit long chains of hidden inference calls. Retry budgets and progress must be explicit; failed complex generation must not automatically become a fallback cloud request.

## Compatibility and migration traps

- Preserve program IDs 933/940, module-backed settings and separate store IDs. Do not rewrite working imported histories or custom prompts as part of extraction.
- Keep Gemini's current public route and response/card formats. Preserve current Lailama routes, Extract/Execute/ExtractAndExecute and download controls during the planned transition; do not introduce routes or aliases incidentally.
- Lailama currently advertises @AI:IntentHandler; Gemini uses @AI:Genie with chat.requestVersion=1 and context/capability lists. Advertise the new descriptor only when the corresponding backend request and policy handling exists. Existing YoT discovery can then expose the controls.
- Preserve the meaning of Lailama's existing SendContextData setting during initialization of the new module profile. Do not silently enable additional context categories or code-generation capabilities for existing local installations.
- Prompt.Schedule is not equivalent today: Gemini returns a queued task ID with Task.Status; Lailama performs synchronous inference without persistent chat history and still post-processes api-call actions. Keep an explicit legacy facade until callers migrate; an alias to a new queued endpoint would change behavior.
- Prompt.Ask must be genuinely text-only, isolated from actions and visible chat history. On Lailama, use explicit session isolation/reset while sharing weights; do not allocate a second large model or contaminate the chat KV state.
- Preserve cancellation before dispatch, single history writes, action request IDs, committed deployment target/binding during repairs and no execution during history rendering/import.
- Gemini-specific legacy-retirement notices remain in its wrapper. Helper Completed/Quit behavior belongs to the shared automation capability, with explicit subscription ownership so multiple configured Genies do not execute the same lifecycle action repeatedly.

## Implementation sequence and acceptance gates

1. **Freeze current behavior with deterministic fixtures.** Cover Gemini reply/device/program/update/widget/composite/Context responses and error/repair outcomes; local text/markers/stream cancellation; history/event metadata and version-1 permissions. Use a fake host/provider and current distributed-script fixtures. Define provider/host interfaces and request/result types here.
2. **Extract Gemini transport only (implemented; initial live acceptance passed).** generateContent and text-only Ask now use the provider boundary. Keep payload, normalized final response and orchestration behavior unchanged. Capture quota/status/finish errors structurally. Preserve current non-streaming behavior initially.
3. **Extract the shared coordinator in small slices.** Request/Context/history and capabilities first, then normalization/action/presentation, then bounded repairs/task coordination. Reuse existing base services. Keep Gemini 933 as the reference adapter and compare observable artifacts/events before/after each slice.
4. **Connect Lailama with a compact device/report profile.** Preserve model loading/download/warmup/stop. Implement the structured request envelope and versioned descriptor, align context/history invalidation, and retain marker support. Live acceptance with 1.7B covers conversation, device status, on/off/level, permissions, cancel/retry and switching/reopening histories.
5. **Enable advanced local workflows progressively.** Exercise common program/widget/composite logic mainly with deterministic recorded/fake outputs. Run a small optional 7B code-generation smoke test only when practical; slow/poor model output is distinct from a coordinator regression. Do not require intensive local generation as a release gate.
6. **Add further providers.** Add OpenAI through the same contract after Gemini/local adapters are stable; evaluate native tool-call translation separately. Broader mixed-artifact plans/Magik, durable task scheduling and UX polish remain separate work.

## Companion milestone: descriptive module/feature discovery

After defining the provider/host contracts and baseline tests (step 1), implement read-only module/feature discovery before extracting the shared context assembly in step 3. It need not block Gemini transport extraction in step 2. Read-only Modules.Get/{domain}/{address}/Schema and Programs.List/Features and Programs.Get/{id}/Features[/{feature}] are now implemented, together with gated Gemini moduleIds/parameterIds lookups and the context-safe ParameterGet view. See [module discovery](module-discovery.md). Earlier discovery and standard setting-change tests passed. Verification of the renamed Schema/Features/Context routes passed on Vulkan after backend and package updates, including the Gemini setting-change batch and independent readback; provider-engine extraction remains unimplemented. Modules.Get/{domain}/{address}/Schema excludes field values and timestamps, while preserving existing Modules.Get and targeted ParameterGet. Discover applicable features and their owner programs from existing metadata, with bounded filters and no full Programs.List source payload. Reuse applicability rules without Modules.FeaturesGet's missing-property initialization side effects. Missing metadata stays absent and descriptive discovery does not imply write access.

Integrate this catalog through Context Engine and the common engine, then add separately authorized, validated parameter changes via Action Executor with prior values and read-back outcomes. Creating/updating an automation program can register new AddFeature declarations that become discoverable automatically. Module configuration is distinct from ordinary device control; broad system API administration comes later. Pilot Smart Device carefully: zero timeout currently means default 30 seconds and disabling its Enable property disables the whole feature. No new auto-off-specific option should be invented by the model.

## Validation budget

Most cases require no external AI calls or local inference: valid/invalid outputs, missing capabilities, repeated IDs, failed compile/recovery, changed targets during repair, cancellation after partial output, histories, task bounds and isolation can be tested deterministically.

Use Gemini for a short representative live integration sequence. Use 1.7B locally for a handful of device/report requests and one cancellation, accounting for cold start. A 4B/7B programming trial is optional and can be slow; do not repeatedly generate complex examples just to test routing. Record warmup/first-token/generation/action times separately. No automatic model download or model switch is required for the analysis phase.

## Release consolidation notes

The user reports successful v2.0.28 release installation and package restore on the production Raspberry Pi. Treat that reported spinner/restore concern as verified for this release, reopening only with a reproducible failure. The general LoggerFactory-disposal/thread-shutdown investigation remains low priority; Lailama's successful native shutdown fix does not prove every host logging race resolved. AI/UX polish and chat CPU profiling remain deferred.

## Implemented first slice: program 583

Route `System.Utility/AiGenie/Describe` reports contract version and stage. `Generate` and `Cancel` currently accept **in-process dictionaries only**, not HTTP JSON. This is an internal contract, not a replacement public chat API.

Generate request: integer `version=1`, nonempty `requestId`, `owner` (Genie identity), simple `provider` identifier, native `messages`, and optional `systemPrompt`, `purpose`, `output`, `budget`, `providerOptions`. The service shallow-copies its envelope and forwards these fields to `AI.Genies/{provider}/Provider.Generate`, adding a cooperative `cancellationToken`. Provider credentials/resources stay inside the adapter. Messages must be treated as read-only by both caller and provider during the synchronous call.

Provider result: dictionary with integer `version=1`, matching `requestId`, state (`completed`, `cancelled`, `failed`, `output_limit`, `context_limit`) and text. Only a nonempty completed response exposes text to the next stage. Exceptions, null/malformed results and incomplete outputs never become executable actions. The service itself does not normalize or execute actions yet. Provider-specific options (currently Gemini cached-content identity) remain opaque to the service. During this transport-only step, message/output objects retain the existing Gemini shape; neutral message/schema translation is a later extraction gate.

One active generation per owner, with explicit busy instead of waiting indefinitely; other owners remain independent. Cancel identifies the active request ID, signals its token and suppresses output even if the provider returns completed afterward. Shutdown signals all active tokens and rejects new work. Cancellation is cooperative: a provider that ignores its token cannot be forcibly stopped; no detached native inference is started by this service. Request IDs correlate calls, but are not an execution ledger or automatic retry mechanism. Existing Action Executor remains responsible for action identity/replay.

Gemini chat and Prompt.Ask now use this boundary. Next: perform live acceptance after deployment, then extract context/response coordination in separately tested slices. Lailama stays unchanged until its adapter is connected. Streaming/progress routing and per-provider budgets will be specified before their implementation.

Initial boundary validation: 52 tests passed (13 AI Genie service/dispatch tests plus 39 discovery and configuration regressions). Full shipped scripts 581, 583 and 933 compile; package 583 checksum/manifest and LF checks pass. No live provider routing has changed.

## Connected Gemini transport

Gemini 933 registers a distinct `AI.Genies/Gemini/Provider.Generate` in-process handler before registering chat handlers. It never acquires the caller's chat gate, composes context, reads/writes history or executes actions. The existing chat loop prepares the same messages, instructions, schema, temperature and cache identity, then calls the common service. The adapter maps these into generateContent, keeps the key in the HTTP header, applies the existing 180-second timeout and uses cancellation-aware send/read operations. Shutdown cancels its active HTTP request through a linked provider-lifetime token.

Only STOP with nonempty visible text completes; thought parts are omitted, MAX_TOKENS becomes output_limit, blocked/missing/malformed content fails, and no partial text is passed back for action processing. HTTP errors retain their numeric status without exposing the key, URL or response body. A request timeout is a failure, distinct from a caller cancellation. Provider shutdown may also surface as an interrupted transport failure; it never yields executable content.

Prompt.Ask uses the same provider boundary with user text only: no selected context, system instructions, response schema, history writes or action processing. Existing public endpoints, prompt/skill composition, bounded lookups/repairs, deployment, history and result cards remain in 933 unchanged. Common-service cancellation is implemented internally; a new UI Prompt.Cancel contract is not advertised in this step.

Deployment: rebuild/copy the package assets, install/enable AI Genie 583 (0.1.1) and update Gemini Genie 933 (1.0.10), then exercise one conversation, device command and text-only Ask. Lailama requires no update for this step. Live acceptance passed on Vulkan on 2026-10-17: chat greeting, Porch Light Control.On with a completed successful executor receipt, and Prompt.Ask returning TEXT_ONLY_OK with the chat history unchanged. Cancellation/error cases remain covered by deterministic tests; no live shutdown/cancellation test is claimed.

Gemini transport boundary validation: 65 tests passed (including actual in-memory MIG dispatch through the common service and adapter, payload/cache/thought filtering, partial outputs, HTTP errors, cancellation and existing discovery/configuration regressions). Full shipped scripts 581/583/933 compile; manifest/dependency checksums and diff checks pass. Live deployment remains pending.

## Prompt and response preparation slice

AI Genie 583 0.2.0 adds Prompt.Compose and Response.Normalize. Gemini 933 1.0.11 forwards its local prompt template and normalized capability selection to the shared composer; skill/KB expansion reuses Context Engine. Capability instructions and the resulting fingerprint remain byte-for-byte equivalent across the eight profiles. Custom templates are preserved. Input and dependency result objects are not mutated.

Response.Normalize delegates to the existing Action Executor Response.Normalize endpoint, never Execute. Contract failures retain a contract classification for Gemini's bounded repair loop; unavailable/incompatible services fail the request without asking the model to repair an infrastructure error. The provider still owns JSON generation/schema mapping, selected-data collection, capability enforcement, bounded lookup/deployment repair, history and presentation. Local contract/schema helpers remain for those existing paths; this is not yet the complete shared coordinator.

Package versions: homegenie-base 1.0.22 and homegenie-ml-ai 1.0.33, with matching 583 dependency versions/checksums. Update 583 before 933. No core, YoT, public chat endpoint, history-format or Lailama changes. Live acceptance of this slice is pending deployment; previous transport acceptance remains valid for its tested versions.

Preparation-slice validation: 82 tests passed (17 prompt/normalization regressions plus 65 existing provider, service, discovery and configuration tests). Complete scripts 581/583/933 compile. Live acceptance remains pending.

Preparation-slice initial live acceptance passed on Vulkan on 2026-10-17 with 583 0.2.0 and 933 1.0.11: Prompt.Preview verified device-only and automation/widget profiles, expected skill/KB selection and allowed kinds; browser Control.On completed with a successful executor receipt and normal chat presentation. Lailama 1.0.19 was enabled/running without ScriptErrors before and after the check; temporary Download Manager outage recovery remains unit-tested, not live-induced here.

## Shared context collection

AI Genie 583 0.3.0 now exposes Context.Prepare and owns the former Gemini scoped-context collector and WidgetContextRequest helper. Gemini 933 1.0.12 reads its module-backed selections and its own custom prompt file, then forwards a version-1 envelope containing request and template. The service clones the request, normalizes capabilities, obtains Context Engine policy, collects only selected categories and explicitly requested sources, and composes the prompt through the existing shared composer. Per-provider prompt paths and preference stores remain in the wrapper; callers cannot supply a filesystem root. Widget source lookup uses the common installation's data/widgets directory, independent of the provider ID.

The existing output profile key, included categories, discovery routes, current-time format, source limits (16 IDs per resource field, 128 KiB per widget file), path containment and 256 KiB collected-data budget are retained. Program summaries exclude source; only requested program IDs include setup/main/context. Dashboard data is summarized. No prompt request or cached context is shared between owners. Existing discovery tests now compile the block from its shared owner rather than the Gemini script.

Provider response coordination, repair/deployment loops, action capability enforcement, history and result presentation remain in Gemini; Lailama is not connected to this path yet. No core or YoT changes. Deployment versions: base package 1.0.23, AI package 1.0.35; update 583 before 933. Live acceptance of this slice remains pending.

Shared-collector validation: 108 tests passed, including 12 full context-collection cases, previous discovery guards at their new shared owner, prompt/provider/service/configuration regressions and Lailama download recovery. Distributed scripts 581/583/933/940 compile. Live acceptance is pending package deployment.

## Shared device response dispatch

AI Genie 583 0.4.0 adds Device.Execute; Gemini 933 1.0.13 delegates the device-response branch to it. The wrapper still saves history and records timings/results; the common service owns capability/context checks, canonical normalization through Action Executor, validation of all target modules before any effect, the existing Execute call, receipt correlation and device reply/card formatting. Program/composite deployment, compiler recovery and widget presentation remain in Gemini for a separate extraction.

The request is a version-1 JObject containing requestId, response, capabilities and context. Device Control and Devices context must be explicitly enabled. Request identity is forwarded unchanged to the existing execution ledger; the common service creates no independent action ledger, automatic retry or rollback. Every result must match the request ID, command and index. Missing/mismatched receipts fail without dispatching again. Configuration failures retain the existing unconfirmed-setting explanation. The normal api-call card and result metadata remain unchanged.

This is an internal automation-service API under System.Utility/AiGenie, using Api.Call objects and module lookup in the utility script; no core or frontend changes. Deploy base 1.0.24 / AI package 1.0.36, update 583 before 933. Lailama remains unchanged. Live acceptance of device dispatch is pending package deployment; the maintainer reported successful context-selection checks for the preceding slice, including a request to enable Devices when it was disabled.

Shared-device dispatch validation: 120 tests passed (12 new dispatch cases plus 108 prior context/provider/service/discovery/configuration/Lailama regressions). Full distributed scripts 581/583/933/940 compile. Live acceptance remains pending; no browser was used.

## Shared program deployment decisions

AI Genie 583 0.5.0 adds Program.Execute; Gemini 933 1.0.14 delegates the program-response branch. The service validates automation capability, canonical response, attempt range 1..5 and committed kind/action/target. Updating requires Automation Programs context and the target among requested source IDs. It forwards the same request ID and attempt to Action Executor Program.Deploy, correlates the deployment receipt and returns either a bounded compiler-repair step or the existing automan_deploy result card.

Compiler diagnostics request another complete response only when deployment State is failed and fewer than five attempts have been used. An unknown ledger result never authorizes retry, even if it contains a compile_failed diagnostic. At a terminal failed result, Program.Recover is called for the same request ID; unknown/mismatched receipts do not trigger recovery. The existing executor retains reservation, replay and restoration ownership. No new program allocation, ledger, rollback algorithm or automatic provider retry is introduced.

The wrapper retains the model loop, committed identity guard, source lookup, history, scheduling and response publication. It validates the returned next-attempt step and records additional recovery action/failure counts, preserving program-attempt telemetry. Composite deployment and widget presentation remain for the next slice. No core, frontend or Lailama changes. Update 583 before 933 (base 1.0.25 / AI package 1.0.37); live acceptance is pending. The maintainer reported the previous device-dispatch live test successful with existing history.

Shared-program dispatch validation: 136 tests passed (16 program-dispatch cases plus 120 existing regressions). Full automation scripts compile; package checksum/dependency and LF checks pass. Runtime programs were not changed; manual create/update acceptance remains pending. No browser used.

## Shared composite deployment coordination

AI Genie 583 0.6.0 adds Composite.Execute; Gemini 933 1.0.15 delegates its composite-response branch. CompositeSolutionContract moved to the utility. The service validates both capabilities and the full program/widget/binding contract before deployment, then reuses AiGenieProgramActions for ID correlation, attempt limits, compiler diagnostics and same-ID recovery. It does not duplicate program installation or rollback logic.

Each repair returns the committed unresolved binding and requires it on subsequent attempts. Kind/action/target and binding must remain unchanged. Compiler feedback explicitly requests the complete composite response. Successful deployment requires a program ID, resolves the existing dollar-program binding placeholder and emits the unchanged ai_solution envelope with widget state waiting/dependsOn program. The existing frontend then observes the bound fields and renders the preview. Failed or unknown deployment emits a blocked widget; uncertain outcomes do not retry or recover automatically. Recovery errors and action counts are retained.

History, provider inference loop and preview readiness remain in their existing owners. Standalone widget presentation is still in Gemini; Lailama is unchanged. No core or YoT changes. Update 583 before 933 using base package 1.0.26 / AI package 1.0.38; composite live acceptance is pending. The maintainer reported program create/update successful, retaining program ID 1000 and displaying Automan.Program.Message directly in its chat card.

Shared-composite validation: 152 tests passed (16 composite cases plus 136 previous regressions). Full distributed scripts 581/583/933/940 compile; checksum/dependency/LF checks pass. Manual composite acceptance remains pending and runtime programs were not modified. No browser used.

## Common structured conversation loop and standalone widgets

AI Genie 583 0.7.0 adds the in-process Conversation.Run endpoint. Gemini 933 1.0.16 forwards cloned conversation/context data, its template, provider identity and provider-specific output configuration. The service owns the structured generation loop, bounded contract repair, context requests, committed deployment identity/binding checks, device/program/composite dispatch and direct reply/widget formatting. Widget code fences and CSS fallback match the previous presentation. The provider wrapper retains identity/configuration, transport/schema mapping, cache/rate-limit state, module preferences, history access and scheduled queue/lifecycle handling.

The request is a BCL dictionary carrying version, requestId, owner, provider, messages (JArray), preparedContext, request, template, systemPrompt, output, optional cache, scheduled and elapsedBefore. It is not an HTTP JSON public replacement endpoint. Messages, context and scope are cloned per run; no conversation state or hidden intermediate responses are persisted by the service. The wrapper serializes its own work as before; this extraction introduces no new implicit job queue. Results correlate request ID and return one final reply/outcome or an error plus metrics. The wrapper imports counts, action receipts and generation timing, then saves the final history message once. Prompt.Ask keeps its separate text-only provider path.

Limits remain 12 rounds, 3 context lookups, 2 contract repairs and 5 deployment attempts. Missing/incompatible infrastructure is not a model contract-repair request. Module stopping is checked before generation and after normalization, before dispatch. The service does not roll back an action already in flight. The public UI cancellation contract is unchanged. Provider transport cancellation remains cooperative. The loop currently consumes the structured response contract; compact marker handling for Lailama is the next provider-integration step, not claimed by this extraction.

Deploy base 1.0.27 / AI package 1.0.39, updating 583 before 933. No core, frontend or Lailama changes; manual live acceptance remains pending. The maintainer reported the previous composite preview and subsequent Sensor.Counter read successful.

Common structured coordinator validation: 171 tests passed (19 end-to-end coordinator/presentation/history-bridge cases plus 152 prior regressions). Full scripts 581/583/933/940 compile. Prompt.Ask remains outside the action loop; the history bridge test verifies one user write and one final reply even if SaveReply is invoked twice. Manual acceptance is pending package updates. No browser used.


### Lailama generation boundary (first migration step)

Lailama 1.0.20 routes its existing inference worker through
`LailamaGenerationProvider`. The adapter uses the version-1 generation result
(`requestId`, `state`, `text`, `error`), validates a single user continuation,
and suppresses empty, failed or cancelled completions before action processing.
The wrapper continues to own the native operation lease, streaming, session,
history and action dispatch. No second model or native context is created.

This is a session-continuation adapter, not yet the complete provider endpoint:
it deliberately rejects supplied system prompts, output schemas and multi-message
histories instead of silently ignoring them. It is not registered as
`AI.Genies/Lailama/Provider.Generate` yet. The next step must define explicit
session reconstruction/profile invalidation and the compact text/`api-call`
strategy before routing chat through `Conversation.Run`. Existing Lailama public
APIs and UI capabilities remain unchanged.

Deterministic provider tests cover validation, message isolation, cancellation,
empty output and error redaction without loading model weights. Live acceptance
for this extraction is a normal reply, a device read/control command, followed
by cancellation and shutdown with a loaded model.


### Lailama compact context discovery

AI Genie 0.8.0 adds `System.Utility/AiGenie/Text.Context.Step`; Lailama 1.0.21
uses it before action extraction. The local model continues to produce ordinary
text and `api-call` blocks. A fenced `context-request` containing `moduleIds` or
`parameterIds` requests read-only reference data. The shared step calls the same
`Context.Prepare` collector as Gemini, which uses module Schema and parameter
Context APIs. It accepts no arbitrary API route. Mixed read/write blocks,
unsupported resources, malformed markers and a fourth lookup fail before dispatch.
Reference-data failures are errors, not model-generated values. Text inside a
closed reasoning block is not interpreted as a context request.

The existing SendContextData option is the master switch. If present, the module's
AI.Chat.Context.devices selection is also respected; other context categories
remain disabled for this local strategy. This step does not add the full Gemini
capability selector or program/widget generation to Lailama.

Lailama rebuilds native state from completed profile-matching history when the
session is invalidated. See the persistent-session checkpoint below for the current
reuse policy; the initial discovery implementation rebuilt on every turn. Pending, cancelled and failed turns are excluded by
ChatHistory/GetContext. The wrapper records contextProfileKey on new messages.
Previously stored messages remain visible/exportable; messages without a matching
profile are not silently injected into the new session. The final answer is saved
once; lookup requests and returned reference data are not persisted as chat turns.
No extra model weights or native context are loaded. Re-evaluating clean history
may increase latency compared with reusing the KV cache.

The continuation adapter is still wrapper-owned, not a public Provider.Generate
endpoint or the full structured Conversation.Run strategy. This checkpoint adds
shared context discovery and session consistency; configuration-write acceptance
and further provider unification remain subsequent steps.

Manual acceptance: with context sharing enabled, ask for the Porch Light's exact
auto-off feature and owning program, then for its current timeout without changing
anything. Change that value manually in settings and ask again, to distinguish a
fresh read from history. Turn context sharing off and repeat: no reference lookup
or device command should be dispatched. Test a normal device command with sharing
on, cancellation and shutdown as lifecycle regression checks. Use the existing
1.7B model; a failed multi-step result is useful evidence, not a reason to force
full JSON generation or install a larger model automatically.

Validation for this checkpoint: 210 targeted tests passed; full distributed scripts
581, 583, 933 and 940 compiled. Package checksums/dependency references and LF line
endings were checked. No browser, live program catalog or runtime model was changed.


### Local continuation diagnostics

Lailama 1.0.22 emits `Lailama.Trace` JSON records through the existing logger.
Correlate `requestId` and zero-based `round`: generation.start,
generation.native.end, generation.result, reference.start and reference.end.
Each round reports yielded text chunks (not native generated-token count), output
characters, time to first nonempty chunk, elapsed time, cancellation/stopping flags
and any observed configured textual anti-prompt. The stream does not expose an
exact native finish reason; finishReasonReported=false deliberately avoids claiming
EOS or an output limit without evidence.

Generation start also records actual native context capacity, configured context
option, message/input lengths, tokenized history-plus-input text estimate and
estimated headroom. This text estimate omits chat-template overhead and is not an
exact remaining-token allowance. Available executor-state scalars include past and
consumed token counts, prompt-run state and input-buffer length. Generated text,
prompts, reference values, model paths and full native state are not logged.
Diagnostic probe/logger failures are suppressed without changing inference cleanup.

The adapter and lookup behavior are unchanged. Update only program 940 after
refreshing packages, repeat the same question with the same model/options, and
inspect `log/homegenie.log` for matching records. A successful reference.end followed
by a round-1 generation.result failed/empty can now be compared with stream chunks,
executor counters and context estimates before considering a behavioral fix.


### LLamaSharp continuation input/output accounting

The 1.0.22 trace reproduced a successful schema read followed by zero yielded
chunks: round 1 appended 2447 input tokens while MaxTokens was 2048. Only the next
512-token batch was consumed before inference stopped. Native capacity was 32768,
so this was not evidence of context exhaustion or a small model refusing the task.

In LLamaSharp v0.27.0, InteractiveExecutor.PreprocessInputs subtracts continuation
input length from RemainedTokens; PostProcess stops when that budget is nonpositive.
Source: https://github.com/SciSharp/LLamaSharp/blob/v0.27.0/LLama/LLamaInteractExecutor.cs

Lailama 1.0.23 uses a small LailamaInteractiveExecutor subclass to preserve the
preprocessing output budget after the base method has tokenized/appended input.
Input processing, native session, sampler, stop sequences, cancellation and the
configured output limit are otherwise unchanged. There is no empty-response retry,
unlimited generation workaround or model reload. This is an automation-program
adapter change, not a HomeGenie-core or LLamaSharp-package modification.

The regression harness compiles the shipped override against a deterministic
model of the 0.27.0 continuation-accounting behavior. It reproduces the observed
2447/2048 failure, checks full input consumption with a bounded output budget,
initial prompts, unlimited sentinel and cancellation without model weights.
Full-script compilation verifies the actual LLamaSharp 0.27.0 override signature.
Live acceptance still requires repeating the same Porch Light request with the
same 1.7B model/options and checking the retained Lailama.Trace round-1 records.


### Multi-round streaming lifecycle

Lailama 1.0.24 uses `LLM.TokenStream.Reset` between a provisional generation and
its reference-data continuation. YoT discards the provisional streaming view but
keeps the submitted request busy and the waiting indicator active. It neither
adds that intermediate text to history nor sends it to speech synthesis.

An empty `LLM.TokenStream` event now means the entire successful request has
finished, not an individual native generation. It is emitted after SaveReply and
before Widget.Refresh; the latter reloads the authoritative final history.
Existing Status.Error/cancel handling remains terminal. No new polling or timer
was added. Update both Lailama and the frontend for the reset-view behavior.

The standalone frontend regression can be run with
`node e2e/src/ai-genie-stream.cjs` from the YoT repository. It exercises the actual
transpiled module-event subscription with multi-round resets, a long inter-round
pause, one final history message and speech, duplicate/late events, cancellation
and errors without opening a browser or running inference.


### Compact-read instruction refinement

AI Genie 0.8.1 removes the sequential wording that encouraged a local model to emit
both schema and parameter requests in one answer. An exact known parameter ID is
read directly; schema lookup is reserved for discovering unknown feature names or
program ownership. The model is instructed to emit one context-request and stop
immediately after the closing fence, without explaining later requests. The prompt
contains one fenced example only. Parser validation, the three-lookup bound and
permissions are unchanged; no malformed-output retry was added.

Update program 583 only for this instruction revision. Lailama fetches the shared
instructions at the start of each submitted turn, so its model and program do not
need to be reloaded. Repeat the direct parameter-read request with the same model
and options; multi-step feature/owner discovery remains a separate acceptance case.


### Persistent native session and current-turn provenance

Lailama 1.0.25 reuses ChatSession and the native KV cache across successful requests.
The wrapper compares a fingerprint of completed persisted messages plus the context
profile, prompt/strategy text, formatting template and history limit. It does not
resend completed history to inference on a cache hit. Current scoped device data
is appended as a clearly labelled CURRENT TURN DATA block with the new user request.
Subsequent lookup results are labelled FRESH API RESULT FOR THIS TURN.

Cache validity is cleared before a native turn and committed only after its final
reply has been persisted. Interrupted/failed native work is therefore rebuilt on
the next request. Changed/imported/deleted history, clear-history, prompt/profile
changes and the native message-count limit also trigger reconstruction. Isolated
no-history requests invalidate rather than pollute the reusable chat session.
Reconstruction drops closed or unfinished think blocks from assistant messages;
the saved/displayed history is unchanged. Reasoning already present in a valid KV
cache is not retrospectively removed; doing that would require recomputation.

Lailama.Session logs requestId, reused and message counts without chat contents.
Check reused=true and nonzero initial PastTokensCount on the next ordinary turn.
History is still read locally to detect changes, but not fed to the model again
when native state can be reused. This does not guarantee that a small model will
choose a fresh API lookup: current-read behavior remains a manual acceptance item.

Acceptance: update 940, ask the direct parameter question, change the timeout
manually and ask again without restarting or clearing the chat. Confirm the new
value, the fresh ParameterGet call and session reuse in logs. Also check history
clear/import, context changes and recovery after cancellation before broader use.


### One local format repair and visible read receipts

AI Genie 0.8.2 validates resource-ID shape before calling Context.Prepare and marks
only model-format failures as contract errors. Disabled context, mixed executable
commands, exhausted lookup limits and collector/API failures are not repairable.
Lailama 1.0.26 permits one correction generation for a contract error. It returns
format guidance to the same native session without guessing missing target IDs.
A second format failure ends the request; the three successful-lookup limit remains
independent. The combined generation bound is five, with no dispatch after exhaustion.
The waiting/stream reset protocol applies during repair as during a lookup.

Completed explicit schema/parameter reads return receipt paths. The wrapper stores
those strings under metadata.contextCalls, including on a later failed final turn.
YoT shows them in a collapsed API group using the existing thoughts-panel style.
No returned values or reasoning are added to these receipts, and the receipt strings
are never sent to ActionExecutor, rendered as api-call blocks or spoken. The feature
covers completed local context reads; device commands retain their existing group.
Failed attempts remain in diagnostics rather than being marked successful.

Update programs 583 and 940 plus the frontend. Manual acceptance: repeat the prior
follow-up that produced a bare property ID; if malformed output recurs, check one
reference.format-repair event followed by either a valid read or a terminal error.
Successful final messages should retain the collapsed read paths after history reload.
Older chat messages without contextCalls remain unchanged.


### History removal acknowledgement and clearing a loaded model

The three-segment History.RemoveAt handler receives a MigInterfaceCommand whose
OriginalRequest is the remaining index tail, not an integer itself. Gemini Genie
1.0.17 and Lailama 1.0.27 parse/validate that tail and pass a numeric Index to the
history service. Lailama also invalidates its session fingerprint. Chat History
1.0.6 confirms Count only after a successful save; on save failure it restores the
removed in-memory entry and returns an error. YoT requires a successful HTTP result
and a nonnegative integer Count before removing the local message/showing success.

The regression uses actual shipped switch/handler bodies and real MIG tail objects,
with temporary files only. It checks disk reload, invalid indices, save-failure
rollback and both provider wrappers. The standalone UI acknowledgement regression
is `node e2e/src/ai-chat-delete.cjs` in YoT.

Clearing Lailama history resets the conversation/session and invalidates its cache;
it keeps the already-loaded weights and context allocation. WarmingUp belongs to
initial model loading and is not required after clearing a conversation. The next
request evaluates its prompt on the reset session; no model reload is necessary.


### Independent read access and device control in Lailama

Lailama 1.0.28 advertises chat.capabilities=[deviceControl] through its existing
@AI:IntentHandler descriptor. It deliberately does not advertise requestVersion=1:
the text request protocol remains unchanged. YoT's existing capability menu stores
the choice in the program module's AI.Chat.Capabilities property. No new frontend
production code or translation is required; the standalone UI regression is
`node e2e/src/lailama-capabilities.cjs`.

Devices information-sharing remains controlled by the existing SendContextData
option and any persistent AI.Chat.Context.devices selection. With that context
allowed and deviceControl=false, schema/parameter lookups remain available, while
the prompt reports read-only mode. Automation/widget capabilities are not enabled.
Missing deviceControl preserves the existing enabled default for compatibility;
users explicitly turn it off for read-only operation.

ExecuteResponse and ExtractAndExecute both re-read persistent capabilities and
context at dispatch time, before calling ActionExecutor. Fenced commands and the
legacy structured-actions envelope cannot bypass disabled control. This also
covers direct legacy execution endpoints and a capability disabled while generation
is still running. Extraction and context preparation remain read-only operations.
Malformed settings fail closed. Capability changes alter the prepared profile/key,
so subsequent native sessions do not retain an incompatible permission profile.

Acceptance: update 940, reload/reselect Lailama, disable device control in the
existing capability menu and leave device context enabled. A current timeout read
must still work. An on/off request must explain disabled control (or be rejected
if the model nevertheless emits a command); no control may be dispatched. Re-enable
control to verify unchanged device commands. Persist the choice across a page reload.

### Reasoning across local context lookups

Lailama retains completed `<think>` blocks from intermediate lookup and format-repair rounds in the final saved reply. Provisional answers and context-request syntax are discarded. The chat preserves the same reasoning during streaming and combines multiple blocks into its existing Thoughts group; request completion and context-call receipts retain their existing behavior.

### Local model initialization failures

Lailama emits Status.Init = Error when model validation or loading fails, alongside Status.Error. The chat displays the persisted initialization error instead of an indefinite loading indicator, including when no generation is active. A new initialization clears the previous error; Ready restores the normal chat.

### Local configuration through the common device dispatcher

Lailama adapts textual `api-call` batches containing `Modules.ParameterSet` to the canonical device response and calls `System.Utility/AiGenie/Device.Execute` in process. The common dispatcher normalizes the entire batch and resolves its targets before dispatching it to the existing action executor. Request IDs are preserved for execution-ledger replay protection. Ordinary device controls retain their existing local path.

The existing executor accepts only existing non-sensitive numeric or checkbox features declared by active programs, validates range/step or On/Off values, checks the current value before writing, and reads the setting back after the standard `Modules.ParameterSet`. Uncertain or rejected writes are not retried automatically and replace provisional success text with a non-confirmation message in the saved reply. Both local execution endpoints enforce current device-control permissions.

Shared text instructions teach schema discovery, parameter reads, units and configuration syntax without replacing customized provider prompts. No HomeGenie core API or provider route changes are needed.

Manual acceptance: update AI Genie (583) and Lailama (940), enable context and device control, ask to set the porch-light automatic-off timeout to three minutes, then request a fresh API read. Expect 180 seconds, one verified parameter-write receipt and a fresh read receipt. Record the previous value first if it should be restored. Also verify that the same write is blocked with device control disabled. Slow local inference is expected; model compliance remains separate from executor validation.

### Shared text conversation strategy

`System.Utility/AiGenie/Conversation.Text.Run` accepts an in-process versioned dictionary containing a request ID, initial input, CancellationToken and strongly typed callbacks for generation, reference steps, round resets, read receipts, diagnostics and final execution. These delegates cross Api.Call in memory; they are not an HTTP/JSON tool contract.

AI Genie now owns the local text strategy: generation-result validation, fresh-reference continuations, one format repair, three-read/five-generation limits, intermediate-reasoning retention and exactly one final dispatch. Cancellation is checked before and after generation, reference retrieval and final dispatch. Exceptions or uncertain execution never trigger automatic redispatch. Diagnostics are observational and cannot fail the turn.

Lailama keeps its model/context/session and native operation lease. Its callbacks still handle native token filtering, tracing and streaming, current-permission checks, the existing common device/configuration dispatch, telemetry, and correction of unconfirmed configuration presentation. Only after a valid coordinated result does the wrapper save the final history message, commit its native-session cache and emit stream completion. No extra model or history service is created.

Gemini keeps the existing structured Conversation.Run strategy in the same utility. The two response protocols remain distinct, and this extraction does not register a new Lailama provider HTTP endpoint or modify Prompt.Ask behavior. Legacy local Execute/ExtractAndExecute endpoints are unchanged by this refactor.

### Local automation programs

Lailama advertises deviceControl and automation independently, plus devices and automationPrograms context. Newly exposed automation is seeded off in the persistent module preference when absent. Widgets remain unsupported. The existing SendContextData setting is still the master switch for context sharing; the chat context menu selects which supported categories are included.

When automation is explicitly enabled, compact shared text instructions define one `genie-action` JSON fence using the same version-1 program contract as Gemini. AI Genie Text.Program.Execute parses and normalizes this envelope and uses the existing AiGenieProgramActions/ActionExecutor deployment path. Ordinary replies and device api-call syntax remain supported. A program envelope cannot mix device commands or reference requests, and compiler recovery cannot switch its action or target.

Program updates require enabled Automation Programs context and complete sources read during the current turn via context-request/programIds. Successful source reads are accumulated in the turn scope; receipts show the Programs.List API actually used by the existing collector, not a fictional Programs.Get request. Device context is not needed for a program-only source read or for creating an independent helper.

Only confirmed compile_failed results may request another generation, up to five deployment attempts with the same request/target. The text generation ceiling grows from five to at most nine only for these bounded compiler corrections; unknown outcomes do not cause a retry. On completion, the existing automan_deploy card is returned, with reasoning retained and deployment JSON excluded from speech. The wrapper still owns history/native session persistence.

Manual acceptance with a CPU model: enable Automations; create a persistent helper that emits Automan.Program.Status=Running and Automan.Program.Message once, then Program.GoBackground. Enable Automation Programs context and update that exact assigned ID after reading its sources. Verify the card says updated and no second program is created. Also verify the automation permission blocks creation when disabled.

This checkpoint does not expose widgets/composites locally. Migration of provider-specific child Quit handling into the common service remains pending before complete automation-lifecycle parity with Gemini; keep the first acceptance test persistent.

### Required sources for local updates

If a local model emits an update before requesting the target's sources, the text program adapter returns a required context step instead of deploying. The common coordinator retrieves that exact numeric program ID through the existing scoped collector, records the read receipt and resumes generation with the returned sources. The pending update action and target are committed across that continuation; a replacement creation is rejected. Disabled source context, lookup failures and exhausted read limits prevent deployment.

Both the adapter and direct program dispatcher accept missing or JSON-null resources as absent source context rather than indexing a JValue. The direct dispatcher retains its explicit source-read validation; only the text coordinator performs the automatic prerequisite lookup.

Acceptance status: the 9B model created managed program 1002 on its first compile attempt. Its installed code emits the requested first-version message and calls Program.GoBackground. The first update exposed the JSON-null resources bug before any recorded action; retest that same existing ID after updating the shared service and local wrapper.

### Reasoning visibility and failed compiler recovery

Retained reasoning remains visible during streaming resets even when the current round buffer is empty. Complete and unfinished think blocks are retained for display; unfinished blocks are closed in the retained copy so the next round cannot be swallowed into the previous reasoning. An unfinished reasoning channel is excluded from reference and program action parsing.

The text coordinator attaches accumulated/current reasoning to a thrown error. Its handler returns that diagnostic to the local wrapper, which includes it in the saved error message. This preserves earlier reasoning when the final generation fails its response contract. A late native failure does not duplicate the previous round's thoughts.

On failure/cancellation, Lailama checks the existing request ledger for the latest program deployment. Only a confirmed failed/compile_failed receipt without an existing recovery permits calling the existing Program.Recover. Completed, unknown, unrelated and already-recovered outcomes are left untouched; recovery exceptions or unconfirmed responses are not retried. The existing executor independently blocks unknown/completed rollback and restores the original sources and prior enabled state for a failed update.

Observed 9B update: CS0103 resulted from treating Automan.Program.Message as a C# property rather than a quoted Program.Emit field. The following generation did not return the required action. Program 1002 was manually restored through the existing recovery API to its original first-version background code before retesting these fixes. Shared instructions now explicitly explain that Automan is not a C# class or namespace.

### Context request validation

Local text context requests reject program API routes used as module/parameter IDs before any collector call. The existing single contract-repair budget explains programIds with an exact ID for source reads. Collector error responses produce a concise reference-data error rather than exposing internal stack traces; infrastructure and permission failures do not trigger format retries.

### Shared generated-program lifecycle

AI Genie utility 583 owns the generated-program lifecycle listener for both Gemini and Lailama. Gemini 933 no longer registers its own Quit listener. Automan.Program.Status=Completed reports completion and leaves the program enabled; returning from Main ends that execution. Only Automan.Program.Request=Quit, case-insensitive with surrounding whitespace ignored, requests Programs.Disable for the emitting automation module. It never deletes the program. Persistent helpers and programs with interactive/file options should stay enabled. Numeric automation addresses are required; the utility cannot disable itself through this listener. In-flight duplicate requests are ignored and queued callbacks are ignored after service stop. No provider inference is required to process the event. Legacy Automan remains unchanged.

Upgrade the base package/AI Genie service before Gemini Genie, because lifecycle ownership now belongs to the utility. Runtime verification pending: Completed retains enabled state, then explicit Quit disables the same test helper while retaining its sources.

### Local session cleanup between turns

ChatHistory.GetContext already excludes failed, cancelled and pending turns. Lailama additionally cleans the provider-only projection of completed model messages: reasoning and internal genie-action/context-request fences are removed, while final deployment receipts and ordinary text/code remain. User messages and stored/UI/export history are unchanged.

After a multi-generation turn, reference lookup, action, compiler/format repair, error or generated reasoning, the native session is not marked reusable. The next request restores the baseline and rebuilds from filtered completed history, without reloading model weights. Plain single-generation text exchanges can still reuse the session. This prevents native compiler feedback/rejected code from surviving a successful recovery even when the persisted reply contains only a success receipt. Failed/cancelled turns were already invalidated and remain excluded. Intermediate feedback stays available within its current bounded recovery loop.

### Compact C# emission guidance

Local program instructions separate executable C# examples from their JSON encoding and use only Program.Emit calls for field emission. The property=value shorthand has been removed from this coding guidance. A single complete genie-action example keeps program_id a string. Tests normalize the example and compile its C# statements. Confirmed C# compiler failures mentioning Automan receive a short literal-string emission hint in the shared program dispatcher, preserving the original compiler diagnostics and retry/identity limits. Other compiler errors and non-C# programs do not receive that hint. This changes guidance only; generated source is never rewritten automatically.

### Local widgets and composite artifacts

AI Genie Text.Artifact.Execute adapts a single genie-action fence to the existing canonical contracts. Program responses delegate to the existing text-program adapter, widgets reuse AiGeniePresentation and its HTML/CSS/JavaScript preview format, and composites reuse AiGenieCompositeActions and its ai_solution envelope. The existing UI waits for the deployed program and required fields before rendering its dependent widget; failed/unknown deployment blocks the preview. Compiler retries keep the committed composite identity and binding, with the same five-attempt deployment limit and existing recovery behavior. Composite creation is supported; composite program updates remain a separate program action.

Lailama exposes Widgets as an explicit opt-in action capability and Widgets as an independent context-sharing choice, using the existing module preferences and Program.Implements descriptor. Missing capability preferences are initialized false. Standalone widgets do not require Automation, Devices or boundModule. Composite generation requires both Widgets and Automation; final dispatch reads current permissions. Existing widget source requests use widgetIds and the shared collector, including path/size checks and the three-lookup limit. The local prompt uses compact widget instructions rather than the legacy Widget Genie output contract.

Every artifact result invalidates native session reuse because its final presentation differs from raw model JSON. The UI composite parser accepts complete leading think blocks while preserving literal think tags inside widget sources. Saved messages retain reasoning; inline preview, binding selection, save and dashboard selection use existing components. No automatic widget file save or dashboard insertion is added.

Runtime acceptance pending: enable Widgets on Lailama and create a small standalone counter widget, verify inline preview/Save, then enable Automation and create a minimal program+widget using a shared emitted field. Disabled Widgets and revoked capability during repair are covered by automated adapter tests. Device controls and program-only flows remain covered by regressions.

## Convergence audit after widget/composite acceptance (2026-10-18)

This checkpoint supersedes the initial extraction assessment above. Shared utility 583 now owns scoped context/prompt preparation, canonical execution adapters, program/compiler recovery coordination, widget presentation, composite dependency envelopes and Quit lifecycle. There are two intentional conversation strategies: structured JSON for Gemini and text/marker streaming for Lailama. Both use the same existing Action Executor journal and deployment APIs. Gemini composite 1004 passed preview, Save, dashboard insertion and reload persistence; local composite code generation quality is not claimed equivalent.

Remaining differences and recommended order:

1. Canonical response schema and capability policy. Gemini 933 still embeds GenieResponseContract plus GenieCapabilities (schema filtering, defaults, allowed kinds, profile key), while Action Executor 581 owns the actual response normalizer and shared 583 already composes capability guidance. Expose/reuse the canonical executor schema through in-process Api.Call and consolidate validation/allowed-kind filtering in utility code. Preserve per-provider defaults (Gemini existing behavior; local automation/widgets explicit opt-in), module settings, custom prompts and separate context-sharing permissions. Technical provider abilities must not be inferred from user permissions or model quality. Remove duplicate classes only after references/tests migrate; no core changes.
2. Common per-turn bookkeeping and history projection. Metrics wrappers and SaveUser/SaveReply live in both provider scripts. Share outcome/request IDs, final persistence and sanitized provider projection, while keeping local ChatSession/KV ownership and invalidation in 940. Preserve imported/visible history and per-Genie stores. History already excludes failed/cancelled turns; native session reuse was a distinct issue. Avoid another broad history format migration.
3. Common abort cleanup and action outcome handling. Lailama retains RecoverFailedProgram ledger inspection and parameter-write text adaptation; shared dispatchers already enforce canonical execution. Bring the confirmed compile-failure recovery decision behind a common boundary and test provider failures during repair. Never auto-retry unknown/completed actions. Route-specific text extraction can remain a strategy adapter.
4. Scheduled tasks / Ask parity is separate feature work: Gemini exposes background scheduling, Task.Status and text-only Prompt.Ask; Lailama Prompt.Schedule is synchronous and its current script has no matching Task.Status/Prompt.Ask endpoints. Do not claim these contracts are interchangeable. Specify semantics before changing them, especially generation without actions and CPU cancellation/queue behavior.
5. Keep native/provider responsibilities local: LLamaSharp loading/download/warmup, sampling, context size, KV state, streaming and cleanup in 940; Gemini credentials, HTTP request mapping, cached content/rate-limit behavior in 933. Their generation/finish capabilities differ (including unavailable native finish reasons); never invent usage or completion guarantees.

Recommended next implementation is item 1, a bounded refactor with capability/schema tests and one brief Gemini regression. Reuse the existing tested local read/control flow rather than requiring another long coding inference. Release assessment follows this cleanup; unified streaming, transient reasoning visibility, CPU optimization and LoggerFactory shutdown remain deferred.


### Canonical response schema and capability policy

Action Executor 581 is the sole owner of GenieResponseContract. Its new Response.Schema endpoint exposes the same version-1 schema used by the existing normalizer. AI Genie 583 Response.Schema retrieves that schema through in-process Api.Call, clones it and filters its variants using the shared GenieCapabilities policy. Reply/context remain available; composites require both Automation and Widgets. No normalization/schema endpoint executes actions. Missing, incompatible or error schema replies fail before generation rather than falling back to a provider copy.

Gemini 933 no longer embeds GenieResponseContract or GenieCapabilities. Its response_json_schema, preview, capability description and persistent capability validation use the utility. AI Genie context preparation, profile keys, capability guidance and structured coordinator allowed-kind checks use the same policy. The prompt text and profile-key encoding remain unchanged.

Capabilities.Normalize and Capabilities.Describe accept version-1 objects with optional capabilities and complete provider defaults. Omitted defaults retain Gemini's existing all-enabled behavior. Lailama 940 explicitly supplies deviceControl=true, automation=false, widgets=false; its small adapter additionally gates device control on the current Devices context/master switch. It calls the service for initial preparation and final dispatch, including legacy Execute/ExtractAndExecute. Stored preferences are not rewritten, and context-sharing selections remain independent from action permissions.

Update Action Executor 581 (1.2.6), then AI Genie 583 (0.13.0), before Gemini 933 (1.0.19) and Lailama 940 (1.3.1). This is an automation-package change; it does not require a core/frontend rebuild or clearing chat history. Structured Gemini JSON and local text/marker generation remain distinct strategies that converge on canonical normalization/execution.

Regression coverage includes all eight capability combinations against the executor schema, unchanged prompt fingerprints, provider defaults, invalid selections, missing/incompatible executor responses, permission revocation and local read-only execution paths. A brief runtime read/control regression remains pending after installing the updated programs; local coding quality is outside this checkpoint.


### Shared per-turn metrics and persistence

AI Genie 583 Turn.Create accepts an in-process version-1 dictionary with storeId, persist and contextCalls (whether to retain that existing metadata field). It returns a BCL dictionary of typed delegates bound to one private AiGenieTurn instance, not a provider-script class or an HTTP-serializable service object. The utility owns request ID, start time/stopwatch, first-token/generation/action metrics, Gemini run-metric validation/import, snapshots, SaveUser/SaveReply and error/cancellation persistence. Calls to the existing ChatHistory/Add service retain the supplied store and message format. A successful final write is guarded against repetition; a thrown write does not falsely mark the reply saved. No global turn registry or inference queue is added.

The two ChatRequestMetrics classes are thin delegate adapters. Gemini keeps its existing metadata shape without contextCalls; Lailama keeps its receipt array and persist=false behavior for requests without history. Final outcome strings, context profile keys, visible text, saved messages and imported chats remain unchanged. Local action counts continue to be recorded separately from their result receipts; Gemini imports the structured coordinator metrics. The factory performs no history write or action execution.

Update AI Genie 583 to 0.14.0 before Gemini 933 1.0.20 and Lailama 940 1.3.2. Action Executor 581 remains at 1.2.6. No frontend or core rebuild and no history clearing are required. Native lifetime, streaming completion, session reuse/invalidation, provider message projection and recovery decisions remain in their existing owners at this checkpoint.

Automated coverage checks per-turn isolation, one final save, error/cancellation text and outcome, retry after a thrown history write, nonpersistent requests, provider-specific metadata and defensive import of structured action receipts. Runtime acceptance remains pending after installing the updated programs: a brief read with each provider followed by refresh/reopen, plus a cancelled local turn without duplicate final messages. Intensive local code generation is unnecessary.


### Shared provider history projection

AI Genie 583 History.Read accepts a version-1 object with storeId, optional profileKey and projection (structured or text). It delegates outcome/profile filtering to the existing ChatHistory/GetContext, verifies the requested context profile and returns a cloned contextProfileKey/messages envelope. Absent/null profiles preserve the unscoped legacy service behavior; a mismatched profile or unavailable service fails instead of falling back to another conversation. History.Project applies the same projection to an already-read message array, without storage access.

Structured projection is a deep copy with no message/text rewriting. Gemini 933 now obtains its provider-turn history through History.Read, retaining its user-message append, typed ChatMessage mapping and existing context/permission logic. This extraction does not simplify discovery instructions or replace natural-language requests with technical-coordinate requirements; the user's successful Smart Device read/enable interactions remain a runtime regression case.

Text projection uses the exact previous Lailama cleanup: remove complete/unfinished think blocks and internal genie-action/context-request fences from model messages, preserve user examples, ordinary answer/code and deployment receipts. Lailama 940 now reads this projection through the shared service. Its LailamaContextSession helper delegates cleanup to History.Project and still owns fresh system-message assembly, role validation and native message limits. Native session initialization/warmup, cache keys/reuse/invalidation and LLamaSharp resources remain provider-specific. The startup warmup's existing history synchronization is not changed in this checkpoint.

No history files are rewritten, no import migration is added, and cancelled responses still display the accepted [STOPPED] behavior. The existing ChatHistory service continues to exclude failed/cancelled/pending turns from provider context. Visible history, exports and per-provider stores are unaffected.

Update AI Genie 583 to 0.15.0 before Gemini 933 1.0.21 and Lailama 940 1.3.3; Action Executor 581 remains unchanged. No core/frontend rebuild or history clearing is needed. Tests exercise the actual shipped ChatHistory filtering, scoped/unscoped legacy messages, unavailable/mismatched-profile responses, input immutability, exact structured-message preservation, local cleanup and native rebuild limits. Runtime acceptance pending: continue existing conversations with a short follow-up referring to a preceding request, including Gemini's natural-language Smart Device queries; no intensive local generation is required.


### Common abort recovery decision

AI Genie 583 Program.RecoverAborted accepts a version-1 requestId object. AiGenieAbortRecovery contains the former local abort decision: inspect ActionExecutor/Results for that exact request, ignore any already-recorded recover action, and select the latest deploy receipt by attempt index. Only State=failed plus Response.status=compile_failed authorizes one Program.Recover call. Missing/malformed journal data, unrelated requests, later completed/unknown attempts and any existing recovery receipt do not authorize recovery. A thrown or unconfirmed recovery is reported but never retried.

Lailama 940 keeps its cancellation/error presentation and metrics; RecoverFailedProgram is now a thin call to the utility. Gemini's shared structured coordinator invokes the same decision only after an exception on a path that may have deployed a program/composite. It adds an attempted recovery to run metrics and includes its confirmed/unconfirmed result in the final error. Ordinary reply/device errors do not inspect the deployment journal through this hook.

The existing terminal-failure recovery in AiGenieProgramActions and the actual rollback/session/journal logic in Action Executor 581 remain unchanged. Completed and unknown effects are still protected by the executor; this refactor adds no replay, replacement creation or native-session behavior. [STOPPED] persistence remains as accepted by the user.

Update AI Genie 583 to 0.16.0 before Lailama 940 1.3.4. Gemini 933 remains at 1.0.21 because its coordinator already resides in the utility; no provider/core/frontend rebuild is needed. Automated tests simulate a model transport failure during compiler repair, later completed/unknown journal entries, non-compiler failures, existing recovery receipts and unconfirmed recovery. They verify action counts, one recovery attempt and no replacement deploy, without requiring slow local inference or modifying live programs.


### Lailama public API prefix

Lailama 940 1.3.5 registers AI.Genies/Lailama exclusively. Its API_URL constant is shared by the existing Program.Implements descriptor and public chat/history/Schedule/Cancel handlers, so YoT chat follows the new prefix without a provider-name special case. The @AI:IntentHandler interface and text request protocol remain unchanged; renaming the public prefix does not change UI history parsing or the local generation strategy. The internal Download/Extract/Execute routes under automation module 940 are unaffected.

The YoT scheduler Genie preset and bundled frontend template now call AI.Genies/Lailama/Prompt.Schedule. No compatibility alias, duplicate registration or automatic rewrite of saved scheduler events is added. This migration does not implement Prompt.Generate or change Schedule semantics. Update the Lailama program and deploy the updated frontend assets/template; no core rebuild or history reset is required.
