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
