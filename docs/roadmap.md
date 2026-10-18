# Future work

## Current priorities after v2.0.28 (2026-10-16)

This status takes precedence over historical implementation-stage notes below.

- Release/program update work is consolidated in v2.0.28: persistent release downloads, Update All, notices/history/backups and post-release missing-required reconciliation. The complete controlled install/restart test installed only the new required fixture, left the optional absent, preserved customized/disabled programs and groups/schedules, and did not repeat the job on a second startup. The user manually relaunched the test service; automatic process relaunch through start.sh was not tested.
- Production Raspberry Pi verification reported by the user: upgrading to v2.0.28 and package restore both completed successfully. The previously reported restore-spinner issue is no longer an open release blocker; reopen for a reproduced failure.
- Next development focus: provider-independent Genie engine, first Gemini extraction, then a compact Lailama device/report path, followed by advanced local workflows and additional providers. Source audit and implementation/compatibility gates: [Shared Genie engine plan](guides/shared-genie-engine.md). No engine refactoring has been implemented in this analysis step.
- Local test budget: use the 1.7B model for conversation, device control and reports. Code-generation tests with 4B/7B models can be very slow on the user's CPU-only PC; cover routing/deployment/repair with deterministic fake/recorded provider outputs and make a small 7B live trial optional. Do not make intensive local code generation a release prerequisite.
- LoggerFactory-disposal/automation-thread shutdown investigation is explicitly deferred and low priority. Retain the original stack trace and investigate logging lifetime later; successful Lailama cleanup does not close this separate host race.
- AI/UX polish comes last. Chat CPU profiling, optional domain skills, broader Magik/mixed-action workflows, durable task queues and RTL remain separate future work. Do not normalize working imported chats without a concrete requirement.

## Module and feature discovery for AI-assisted configuration

Read-only discovery implemented and verified live with Gemini after backend rebuild and package updates. API contract: [Module discovery](guides/module-discovery.md). Validated checkbox/slider setting changes now live in Action Executor and use the standard Modules.ParameterSet API. The earlier experimental-endpoint pilot passed, but the replacement standard-API live test passed on Vulkan (60 -> 30 -> 60, verified receipts and independent readbacks).

- Implemented `Modules.Get/{domain}/{address}/Schema`: a descriptive view of module identity/type, property names and available descriptions/types/options/constraints, applicable features and their owning automation programs. Exclude property values, timestamps, sources and large runtime payloads. Preserve `Modules.Get` for complete module data and use `Modules.ParameterGet` to read only the values actually needed. Do not invent missing metadata or infer that every property is a writable setting.
- Implemented `Programs.List/Features` / `Programs.Get/{id}/Features/{feature}`: discover feature definitions from existing ProgramFeature metadata and program ownership, with bounded/filterable results by module domain/address, program and/or text. Reuse domain/type/property/virtual-module applicability rules used by the UI. Disabling or updating a provider program must be reflected without maintaining a separate hand-authored feature catalog.
- Discovery is read-only: `Modules.FeaturesGet` still initializes missing module properties for the UI, while discovery reuses the extracted applicability matcher without those writes. The general backend catalog is useful to UI and integrations as well as AI; Context Engine selects information according to the request and allowed context, rather than exposing all program sources.
- Gemini now supports gated moduleIds/parameterIds context lookups. The optional /Context view of existing ParameterGet returns only the requested value, blocks known sensitive fields and enforces a 4096-byte limit. Read values on demand; setting changes now route through the common Action Executor and Modules.ParameterSet with active/applicable checkbox or slider metadata, range/step checks, Devices/deviceControl gates, recorded prior values, stale-value rejection and standard API response/readback verification. Other field types remain future work. Distinguish device control from module configuration; broader system administration and API discovery are a later extension. Do not silently enable configuration permissions or include credential values in model context.
- Discovering a feature and its owning program enables the AI to configure it, inspect that program's source only when necessary, or create/update an automation program registering new features through AddFeature. Existing metadata makes the new functionality discoverable; generating a feature does not require editing prompts/catalogs manually.
- Smart Device (145) is the pilot: discover Porch Light features and actual values before proposing a change. Currently SwitchOffTimeout=0 means a 30-second default, while disabling SmartLights.Enable disables the full feature, including sensor control. A separate auto-off-only setting may be needed to express the narrower user intent; do not guess that setting or alter semantics as part of discovery work.
- Sequence: read-only discovery and Gemini integration are now implemented, with metadata and shipped-script regression tests. Live Gemini verification passed: Porch Light metadata identified Smart Device (145), then targeted reads returned SmartLights.Enable=On and SwitchOffTimeout=30; backend logs confirmed both lookup stages. Continue provider/host contracts and shared coordinator extraction, reusing these APIs, then extend configuration actions beyond the initial checkbox/slider implementation. Validate initial local discovery/report/control with 1.7B and deterministic metadata/action fixtures, not intensive code generation. A general searchable HomeGenie API operation catalog remains a later milestone.

## Earlier milestones and design notes

- OpenAI Genie: after extracting the shared engine, add an OpenAI Responses API adapter for chat, intent handling and widget generation. Reuse Context Engine, chat history, MIG.Audio and shared HomeGenie tools with the Gemini providers. Start with chat/device control, then widget generation and editing; keep API credentials on the backend.
- YoT: add RTL layout support for languages written from right to left.

- Interface grouping capabilities: consider a general interface capability to opt out of automatic control-group creation. The audio domains already use a targeted exclusion; module registration and address-based events are preserved, as are existing and manually created groups.

- Magik (working code name): coordinate an end-to-end project from an AI chat, combining hardware guidance, backend automation, widget generation and dashboard integration. The AI designs the program and widget together, using the same module addresses, properties and units. Start by evaluating reuse of Gemini Intent Handler and existing HomeGenie actions, rather than introducing a separate executor. First milestone: create a working program and its linked widget, then display an isolated interactive preview in the chat. Later milestones: install or update widgets and add them to a chosen dashboard. Offer assisted execution with user confirmation and an opt-in autonomous mode scoped to the project. Keep object IDs stable across retries, validate results, report changes and support recovery of reversible operations. Keep these capabilities independent of the AI provider. Pilot example: a Raspberry Pi tank-level sensor, including hardware setup, calibration, readings, error states and a level widget.

- Documentation organization: move Markdown guides into `docs/guides/` before the next release, keeping static service files at the root. Update README and cross-links, and retain redirects or compatibility links where published URLs need to remain valid. New action-pipeline documentation starts in this subdirectory.


- Unified Gemini Genie: converge Intent Handler, Automan and Widget Genie behind one conversation and coordinator, retaining reusable specialized capabilities and the existing providers during migration. Support reply-only responses, device queries/control, program creation/update and widget/composite solutions. Use shared action contracts and stable solution/artifact identities; distinguish assisted execution from explicit project-scoped Magik authorization. Keep provider-specific function calling and marker/structured-response fallbacks behind the same coordinator.

- Selectable AI context: add a Context menu after the speech toggle, with Devices (initially enabled), Widgets, Automation Programs, Dashboards and Deep Knowledge (initially disabled). Remember selections per Genie and apply them to subsequent requests. Share directories first, then relevant sources/references as needed, rather than all source files. Information sharing does not grant execution permissions. Include the catalog, actual selections and information availability in system instructions; the AI may propose the minimum useful missing context with a reason, but cannot enable it itself. A structured context_request proposal can offer an explicit activation button; activation must not automatically resend a prompt. Explain cloud sharing for cloud providers and that turning off a category does not retract information already sent. Gate all data collectors and provider tools, review chat-history context, and invalidate/scope caches when profiles change. Start with the versioned shared Context Engine profile/policy API; then integrate one provider, verify exclusion end to end and add the UI menu before migrating the remaining providers.

- Agent Skills migration: use the portable SKILL.md format in data/ai/skills, with YAML metadata and Markdown instructions/references. Foundation implemented: Context Engine Skills.List and Prompt.Compose, deterministic inclusion/fingerprints, diagnostics and backend handler bindings. Provider integration implemented: a common conversation skill and shared automation reference are composed into the three role skills; default prompts use directives, custom plain-text prompts remain valid, and cache reuse checks the assembled context hash. Provider diagnostics expose skill/resource fingerprints without AI calls. Next: unify role-specific action contracts and capability routing before introducing a unified Genie. Then expose skill selection separately from information-sharing context, unify action contracts, and evaluate on-demand resource loading/MCP.

- Chat import normalization: detect legacy JSON formats at import and convert them to one versioned internal message/artifact format. Preserve text, code and composite contents, accept missing optional metadata, and never execute actions during import or rendering. Keep the original export recoverable and report unsupported content. Separate visible history from model context; explicitly choose whether to reuse imported messages under the selected skill/Context profile. New exports use the current format. Validate against old chat, api-call, program, widget and composite fixtures.

- Unified Genie pilot implemented: Gemini Genie (933, @AI:Genie) uses the version-1 provider-independent response contract and shared device/program executors, with program creation/update, widget previews and composite data readiness. Legacy providers remain available and retain their stores. Next: user-selectable skills/capability routing, broader mixed-action plans, import normalization, and further provider adapters. The pilot supports one program per response and composite creation, not composite updates or unattended dashboard installation. Native function-call adapters can be layered onto the same contract; the pilot uses structured JSON and gated source lookups.

- Result presentation polish: keep short widget status badges such as NORMAL on one line; lead compiler-recovery responses with the outcome and show technical repair details secondarily. Apply the badge rule to shared widget guidance and the result/detail separation when consolidating action feedback.

- Existing-installation transition: implemented an optional persistent Gemini Genie notice with confirmed, reversible retirement of the previous specialized Genies. Settings, custom prompts and chat histories remain in place. The current release does not need an automatic migrator; history import normalization remains a separate future milestone.

### Gemini Genie pilot: selectable capabilities

Implemented persistent module preferences per Genie for device control, automation and widgets, with legacy browser preference initialization and Implements-driven menu support, with request-specific schema filtering and backend checks. API-call result cards and reusable code snippets are restored using backend execution metadata. Next: imported-history normalization, migration of existing Genie configurations and broader multi-action plans.

### Persistent notifications and optional Genie transition

Implemented base service 582 with explicit version-1 persistent messages, bounded disk archive, shared read/archive state and recoverable action results. YoT provides an active/archive list and clickable detail, with local stored API actions and optional confirmation. Gemini Genie offers reversible retirement of the previous specialized Genies; existing configuration and histories stay in place. This supersedes the earlier automatic migrator proposal for the current release. Further work: optional history import normalization, richer notification filters and per-user read state if HomeGenie gains user-scoped accounts.

### Upgrade and package restore reliability (reported on Raspberry Pi)

- Release frontend base-href guard implemented: NUnit checks the final bundled `assets/build/all/app/index.html` for exactly one `<base href="/app/">`, with missing, incorrect, duplicate, comment and script cases covered. Existing CI test steps and the explicit build-all.sh guard block release builds on failure; standalone frontend development remains unchanged. All 14 tests passed.

- Package restore UI: user verified package restore successfully on the production Raspberry Pi after v2.0.28. Existing update loading/recovery fixes are shipped; investigate further only if a reproducible restore-spinner failure recurs.
- Required programs on upgrade: inspect the updater/package reconciliation flow. After a service upgrade, newly introduced programs marked required must be installed with their dependencies. Preserve existing program configuration, data, history and enabled/disabled choices; do not automatically reinstall or enable optional retired Genies. Report failures and allow an idempotent retry. Regression-test an existing installation upgrading to a release that introduces required services and Gemini Genie.
- Shutdown logging (deferred, low priority): investigate automation threads logging after LoggerFactory disposal. Coordinate thread termination and logging lifetime; retain the original runtime error and avoid an unhandled exception during shutdown.
- AI chat CPU investigation: deferred. Profile the same dashboard with chat closed and an unconfigured/empty Genie open before further changes. Initial DOM-scan/message-view optimizations are implemented, but the cause of sustained extra CPU is not established.

### Unified Genie orchestration progress

Gemini Genie now provides Prompt.Schedule, Task.Status and text-only Prompt.Ask. Shared orchestration instructions cover explicit helper execution, bounded waits, valid editor options and checking fresh output files. The newsroom Director/Scout/Editor test completed successfully. Task queues/status remain bounded and memory-only; durable scheduling/recovery is a future extension. Broader mixed-artifact plans, user-selectable skills and project-scoped Magik remain future work.

### Skills selection and imported history: scope clarification

- The chat Capabilities menu selects execution permissions, and the backend already loads the corresponding fundamental skills/references automatically. Keep this behavior; do not introduce a duplicate Skills menu for the same choices. An explicit selector is future work for optional/domain-specific skills. Selecting a skill must never enable an execution capability by itself.
- Legacy chat import normalization is deferred, not a prerequisite for the next release. Existing imports already work, including the older conversation reported by the user in the current UI. Preserve the working import/rendering path and do not rewrite histories solely to unify formats. Revisit normalization only for a reproduced unsupported format, lost metadata/artifacts, or a concrete new contract requirement. If revisited, normalize at import, retain the original, and never execute historical actions. Read-only regression coverage with legacy reply, api-call, program, widget and composite fixtures remains useful independently of conversion.

This clarification supersedes earlier roadmap entries proposing immediate user-selectable fundamental skills or treating import normalization as the next required migration step.

- Gemini Genie configuration cleanup implemented: program 933 no longer registers or reads EnableDeepKnowledge; obsolete 933 translation labels removed. Structured Context selections remain authoritative, and specialized legacy Genies retain their option. Explicit Quit handling restored: disable the emitting automation program asynchronously without deleting it; Completed alone leaves reusable helpers enabled. Compilation and lifecycle checks passed; verify after installing the updated package in the running test service.


### Shared Genie engine and interchangeable providers

Planned: extract the provider-independent orchestration currently embedded in Gemini Genie into a common Genie engine. Connect Gemini and Lailama through one provider contract, then add other providers such as OpenAI. This supersedes implementing a separate copy of the complete Genie logic for each provider.

- Common engine: conversation/history, context profiles, skill composition, capability checks, response normalization, device/program/widget/composite actions, compilation repair, task queues, progress, cancellation coordination, errors and metrics. Reuse existing Context Engine, Chat History and Action Executor services.
- Provider adapters: model invocation, prompt/message and response translation, streaming, cancellation and provider errors. Keep local model download/loading/disposal in the Lailama adapter. Describe technical abilities (structured output, function calling, context limits, streaming and cancellation) separately from user-enabled execution capabilities.
- Preserve a mixed response strategy: use constrained JSON or native function calling where available, and supported text/marker formats where necessary. Normalize all outputs into the same internal action contract and backend checks. Do not require function calling from small local models.
- Aim for the same application features across providers; model quality, speed and reliability will differ. Test local conversation and device control first, followed by automation, widgets, compilation repair and composite readiness. Do not advertise equal model performance or guaranteed complex code generation.
- Retain @AI:Genie as the UI contract and derive API routes/features from Program.Implements. Gemini now uses AI.Genies/Gemini exclusively; no singular-prefix alias is retained. Consider AI.Genies/Lailama for the future unified adapter, while preserving the current 940 APIs during an explicitly designed migration. Keep credentials, preferences, custom prompts and history stores intact.
- Phases: (1) audit 933/940 dependencies and define the provider contract; (2) extract the engine with Gemini as its first adapter and regression-test existing behavior; (3) connect Lailama and validate progressively with small local models; (4) consider OpenAI and further providers. No provider-engine extraction has been implemented yet.

### Current consolidation status

- Shared device/program executors, Gemini composite creation, inline widget previews and saving widgets to a selected dashboard are implemented. Broader mixed-action plans, composite updates and project-scoped autonomous Magik remain pending.
- Lailama download finalization fix implemented in program 940: wait for Completed plus the final file, continue directly into model initialization, and retain polling during paused/error states. Seven shipped-script regression scenarios and full program compilation passed; verify a real download after updating the running test installation.
- Before the next release, verify updated 933/940 packages end to end, review checksums and generated frontend artifacts, and complete the website documentation review. Historical roadmap entries describe earlier stages; completed work above is not a new migration requirement.

### Common backend program update inspection

Implemented the read-only Packages.Updates endpoint and shared ProgramUpdateChecker. It reports newer installed instances, missing required programs from active packages, transitive required dependencies and local modifications; package identity includes repository/package/program ID. It preserves disabled instances and excludes uninstalled optional programs and inactive packages. Catalog errors, invalid versions, duplicate identities and dependency cycles are explicit incomplete reports. The local catalog is the source; no network release lookup, installation, UI integration or notification runs yet. Next: use this report to drive safe required-program reconciliation and program-only update jobs, then connect badge/list and persistent notifications. See guides/program-updates.md.

### Notification recovery and Lailama shutdown hardening

Implemented: notification 582 ignores ordinary/non-object payloads during recovery, preserves persistent-envelope validation, and includes the sender in recovery errors. Lailama 940 cancels active native work before cleanup, protects initialization/warmup and synchronous callers, releases saved context state/Context/weights once, removes forced GC/finalizer waits, and bounds the stop callback to five seconds. Pending native work or disposal continues in a background cleanup; resources in use are never forcibly disposed. Model reconfiguration waits for cleanup before loading another model. Managed regression tests cover cancellation, blocked disposal, interrupted callers and disposed loggers. Real native shutdown with a loaded model still needs verification on the running Linux host after the programs are updated. This does not claim to fix every host shutdown/logging race.

### Persistent program update notification

Implemented in base service 582: browser-independent local check every minute, stable persistent notice keyed by report fingerprint, repeatable navigation to the originating connection’s Programs update view, and automatic archiving only after a complete empty result. Unchanged checks preserve read/archive state and do not write or notify; failed/incomplete checks retain the previous notice. The Programs view uses the common report and separately lists missing required entries. Existing translated labels are reused across all supported languages. Tests cover deduplication, restart/read state, resolution/recurrence, sender isolation, incomplete reports and navigation validation.

Next: align the global Programs badge and update-view error/loading feedback with the common report; then implement recoverable program-only update jobs, required-program reconciliation and Update All, preserving local edits, options/data and enabled choices. No automatic installation is part of the notification step. User verified Lailama shutdown with a loaded model successfully after the preceding fixes.

### Program-only Update All

Implemented: dedicated recoverable backend jobs and the Programs Update All control, with confirmation, dependency ordering, missing required programs from active packages, per-target progress, idempotent request IDs, explicit retry and persistent job outcome notices. Modified/unknown-baseline code is skipped. Existing names/categories, enabled choices, options and runtime data files are preserved; package groups/modules/schedules are not imported. Original code/binaries are backed up for rollback, and interrupted jobs are never replayed automatically. Notification recovery handles the archive service updating itself and ignores stale progress events. Documentation: guides/program-updates.md.

Pending validation: rebuild/restart the test backend and exercise the new job APIs from YoT on port 4200, including the notification service self-update. Automated tests cover planning, retries, shutdown/recovery, original-binary rollback and frontend loading/reconnection. Automatic required-program reconciliation during a system release upgrade remains future work; the new program-only mechanism is its intended reusable basis. Journal/backup retention policy and broader browser CPU optimization remain separate items.

### Stable Programs updates view and backup retention

Implemented: always-visible updates control, permanent update-only status/history area, explicit up-to-date state only after complete inspection, latest 50 outcomes, backup storage totals and confirmed manual cleanup of successful backups. Automatic idle maintenance expires successful backups after 7 days and ordinary failed backups after 30 days; interrupted and manual-recovery backups remain protected. Journals/request identities survive cleanup. Regression tests cover expiry, protected/malformed records, path handling and UI confirmation. Backend rebuild/restart and an end-to-end history/cleanup check on the Linux test host remain to be performed. Previous live tests passed 582 self-update and a two-program batch (572 enabled, 900 disabled), with groups.xml unchanged.

### Post-release required-program reconciliation

Implemented: release completion marker followed by a one-shot startup reconciliation under the new runtime. Initial adoption also works with older updaters through release version/date identity. Persistent UUID/fingerprint/job records prevent duplicate submissions and normal restarts do not repeat installation. Only missing required roots and their necessary unsatisfied dependencies are selected; unrelated program updates remain manual. Existing disabled/modified programs, options, runtime data and package groups are preserved by the existing program-only installer. Failed/interrupted outcomes remain visible through update reports/jobs and require explicit review/retry. Tests simulate release catalog copying, failed/no-op installation, first adoption, repeated boots, crash acceptance recovery and modified/disabled dependency handling. Live migration verification passed, including the complete Maintenance install path, new required vs optional entries, customized/disabled existing programs and repeated startup. Fixture changes were made only with the test backend stopped and were cleaned afterward. Process relaunch in this environment was manual.

### Persistent GitHub release download and resume

Implemented: backend-owned download state/cache, progress polling, explicit pause/resume/restart, Range/validator checks, SHA-256/CRC validation, isolated extraction and verified install gating. UI reopening restores the operation without a repeated download; errors never advertise a ready archive. Manual installs and downloads share an operation guard. GitHub asset selection preserves CPU/Vulkan/CUDA edition independently of RID. Official v2.0.27 Vulkan asset was probed with Range (206, ETag and size), and its ZIP index/release manifest were read without downloading the complete archive. Automated download/install and frontend regressions cover the pipeline; live GitHub pause/reopen/resume, extraction and ready-cache reuse passed. Installation/reconciliation passed with a controlled current-build staging fixture; test process relaunch was manual, so automatic launcher restart is not claimed. See guides/release-updates.md. Prior required-program migration tests passed on Vulkan, including no repeated job on the second boot and unchanged groups/schedules; the temporary 1981 scenario was removed and catalog/572 restored.

Module configuration correction: validation/readback now live in AI Action Executor and writes reuse the unchanged Modules.ParameterSet API. The experimental core endpoint/helper were removed; the standard-API live test passed on Vulkan.

Development constraint: discuss proposed HomeGenie core changes with the maintainer before implementing them. Reuse existing APIs and automation-program extension points where appropriate.

API/schema migration implemented: Programs.List/Features, Programs.Get/{id}/Features[/{feature}], Modules.Get/{domain}/{address}/Schema and the exact /Context parameter view. Schema separates properties (empty FieldType) and features (nonempty FieldType), one row per key with owner programs. New experimental discovery names have no aliases. Package consumers and tests are updated. Live verification passed on Vulkan on 2026-10-16 after backend restart and updates of Executor 1.2.5 / Gemini 1.0.9: catalog and targeted feature reads, 14 properties / 20 features for Porch Light, unchanged ordinary Get responses, and a Gemini 60 -> 30 -> 60 batch with two completed/verified receipts and final readback of 60. Ordinary Get/List/ParameterGet behavior is preserved.

### Common AI Genie utility: initial provider boundary

Implemented optional, initially disabled program 583 (AI Genie, Utilities) with versioned in-memory Api.Call dictionary requests/results, distinct Provider.Generate dispatch, per-owner busy handling, cooperative cancellation/shutdown and incomplete-output rejection. This supersedes the initial core-assembly engine proposal; no core changes. Gemini/Lailama are not connected yet and keep existing endpoints, stores and behavior. Next: Gemini adapter and behavior-preserving transport extraction, shared coordinator slices, then compact Lailama integration. Do not enable/require the service or advertise parity until the adapter/dependency and acceptance gates are complete. See guides/shared-genie-engine.md.

Initial boundary validation: 52 tests passed (13 AI Genie service/dispatch tests plus 39 discovery and configuration regressions). Full shipped scripts 581, 583 and 933 compile; package 583 checksum/manifest and LF checks pass. No live provider routing has changed.

### Gemini transport connected to AI Genie

Gemini Genie 1.0.10 now routes chat and isolated Prompt.Ask generation through AI Genie 583 (0.1.1), enabled/required with an AI-package dependency. Distinct provider handler, no callback chat-lock acquisition, unchanged prompt/schema/cache/history/action orchestration, cancellation-aware HTTP and provider shutdown, explicit sanitized failure/finish states, and no partial output execution. No core or YoT changes. Neutral message/schema mapping, shared context/action coordinator and Lailama adapter remain next steps. Deployment/live acceptance is pending; do not claim complete shared-engine parity yet.

Gemini transport boundary validation: 65 tests passed (including actual in-memory MIG dispatch through the common service and adapter, payload/cache/thought filtering, partial outputs, HTTP errors, cancellation and existing discovery/configuration regressions). Full shipped scripts 581/583/933 compile; manifest/dependency checksums and diff checks pass. Live deployment remains pending.

Gemini/common-service initial live acceptance passed on Vulkan on 2026-10-17 with 583 0.1.1 and 933 1.0.10 active: ordinary chat response, Porch Light Control.On completed successfully through Action Executor, and text-only Prompt.Ask returned TEXT_ONLY_OK without changing the chat history. Live cancellation/shutdown is not claimed; those paths have deterministic coverage. Next: extract shared context/response coordination, then connect Lailama.

### AI Genie prompt/response preparation

Implemented 583 0.2.0 / Gemini 1.0.11: shared skill/KB prompt composition via Context Engine, unchanged capability instruction fingerprints, and response normalization through the existing Action Executor parser without execution. Contract vs service failures remain distinct so infrastructure errors do not trigger model repair. Selected-data reads, capability checks, bounded recovery, history and artifacts remain in 933 for the next extraction. No core/YoT/Lailama changes. Deployment/live acceptance pending; update 583 before 933.

Preparation-slice validation: 82 tests passed (17 prompt/normalization regressions plus 65 existing provider, service, discovery and configuration tests). Complete scripts 581/583/933 compile. Live acceptance remains pending.

Lailama download-service availability fix (940 1.0.19): the 2026-10-17 00:01 log shows Download Manager 575 stopping/recompiling while Lailama was polling GetStatus; the null result reached DeserializeObject and disabled Lailama. Polling now tolerates null, empty and malformed status replies, waits the existing two seconds and resumes once the service returns. Stop/reconfigure checks and finalized-model requirements remain unchanged. No model download/load or runtime-program edit is performed by this fix.

Lailama availability regression verification: 14 download/lifecycle tests passed, including null/empty/malformed status recovery and stop/reconfigure while unavailable. Full distributed program 940 compiles; checksum/manifest and LF checks pass. Runtime 940 remains untouched and must be updated to 1.0.19, then re-enabled if desired.

Preparation-slice initial live acceptance passed on Vulkan on 2026-10-17 with 583 0.2.0 and 933 1.0.11: Prompt.Preview verified device-only and automation/widget profiles, expected skill/KB selection and allowed kinds; browser Control.On completed with a successful executor receipt and normal chat presentation. Lailama 1.0.19 was enabled/running without ScriptErrors before and after the check; temporary Download Manager outage recovery remains unit-tested, not live-induced here.

### Shared Genie context collector

Implemented 583 0.3.0 / Gemini 1.0.12: scoped context data collection and WidgetContextRequest moved to AI Genie. Provider-owned preferences/custom prompt remain in 933; selected data, source lookup gates, module Schema/Context reads, widget-root containment, source/data limits, profile keys and prompt composition are preserved in the shared service. Caller envelopes are cloned. No core/YoT/Lailama changes. Next: live acceptance after package updates, then shared response/action coordination. Update 583 before 933.

Shared-collector validation: 108 tests passed, including 12 full context-collection cases, previous discovery guards at their new shared owner, prompt/provider/service/configuration regressions and Lailama download recovery. Distributed scripts 581/583/933/940 compile. Live acceptance is pending package deployment.

### Shared AI device-response dispatch

Implemented 583 0.4.0 / Gemini 1.0.13: common Device.Execute validates explicit device capability/context, canonical batch and all module targets before forwarding the original request identity to Action Executor. Receipts are correlated by identity/command/index; uncertain outcomes never auto-repeat. Existing configuration-failure wording, api-call cards and results are returned to the wrapper for history/metrics. No core/YoT/Lailama change. Deployment/live acceptance pending; update 583 before 933. Next: program/composite deployment and bounded recovery coordination.

Shared-device dispatch validation: 120 tests passed (12 new dispatch cases plus 108 prior context/provider/service/discovery/configuration/Lailama regressions). Full distributed scripts 581/583/933/940 compile. Live acceptance remains pending; no browser was used.

### Shared Genie program deployment decisions

Implemented 583 0.5.0 / Gemini 1.0.14: program create/update permission/source gates, committed target validation, stable request IDs, deployment receipt correlation, maximum five compiler attempts, same-ID recovery and legacy result formatting now run in AI Genie. Unknown ledger state cannot authorize retry via a stale compiler diagnostic. Wrapper still owns model loop/history/metrics; extra recovery calls are reflected in counts. Composite/widget coordination remains next. No core/YoT/Lailama changes. Package update and manual live acceptance pending; browser not used. Previous device-dispatch acceptance was reported successful by the maintainer.

Shared-program dispatch validation: 136 tests passed (16 program-dispatch cases plus 120 existing regressions). Full automation scripts compile; package checksum/dependency and LF checks pass. Runtime programs were not changed; manual create/update acceptance remains pending. No browser used.

### Shared composite response coordination

Implemented 583 0.6.0 / Gemini 1.0.15: common Composite.Execute reuses program deployment coordination, validates both capabilities and complete artifacts before effects, preserves binding across bounded repairs and returns the existing ai_solution dependency envelope. Preview waits for program fields after a confirmed deployment; failed/unknown results block it. Recovery errors and metrics are preserved. No core/YoT/Lailama changes; deployment/manual acceptance pending. Previous program create/update was reported successful by the maintainer with stable ID 1000. Next: standalone widget/result presentation and remaining model-loop coordination before Lailama integration.

Shared-composite validation: 152 tests passed (16 composite cases plus 136 previous regressions). Full distributed scripts 581/583/933/940 compile; checksum/dependency/LF checks pass. Manual composite acceptance remains pending and runtime programs were not modified. No browser used.

### Common structured conversation loop completed

Implemented 583 0.7.0 / Gemini 1.0.16: generation/lookup/repair loop, committed identity checks, shared action dispatch and standalone widget/reply presentation now run in AI Genie. Provider wrapper retains configuration/transport/schema, identity-specific preferences/history, rate-limit state and task queue. Single final history write and imported action/generation metrics preserve existing formats. Same loop/lookup/repair/deployment limits; stop checks prevent new dispatch after stopping. No core/YoT/Lailama changes. Live acceptance pending package updates; browser not used. Next: Lailama provider adapter plus compact text/api-call response strategy, followed by compatibility checks. Previous composite execution and live counter read were reported successful by the maintainer.

Common structured coordinator validation: 171 tests passed (19 end-to-end coordinator/presentation/history-bridge cases plus 152 prior regressions). Full scripts 581/583/933/940 compile. Prompt.Ask remains outside the action loop; the history bridge test verifies one user write and one final reply even if SaveReply is invoked twice. Manual acceptance is pending package updates. No browser used.


#### Lailama provider migration checkpoint

- [x] Extract the native generation result boundary while preserving the current
  session, token streaming and native lifetime lease (Lailama 1.0.20).
- [x] Add deterministic adapter validation/cancellation tests without loading a model.
- [ ] Verify the extracted path with the existing 1.7B model: reply, device command,
  cancellation and shutdown.
- [ ] Add explicit full-message session mapping and context-profile invalidation;
  then expose the provider endpoint and connect the compact text/`api-call`
  strategy to the shared coordinator. Do not require structured JSON from small models.

The shared Gemini coordinator's latest manual acceptance was reported successful
by the user before this checkpoint.


#### Lailama shared context discovery checkpoint

- [x] User confirmed Lailama 1.0.20 replies, controls and cancellation; shutdown
  remains a separate manual lifecycle check.
- [x] Add compact read-only context-request handling through the shared collector
  (AI Genie 0.8.0 / Lailama 1.0.21), bounded to three lookups.
- [x] Rebuild the native session using fresh scoped context and completed history
  for the current profile; retain stored history for display/export.
- [x] Add deterministic lookup and session reconstruction tests.
- [ ] Manual 1.7B acceptance: discover Porch Light features/owner, read timeout,
  manually change it and verify a fresh read; repeat with context sharing disabled.
- [ ] Complete configuration-write/readback acceptance and integrate further with
  the common provider/coordinator strategy after the read-only checkpoint.


- [x] Add content-free Lailama continuation diagnostics (1.0.22): generation and
  reference phases, executor counters, context estimates and cancellation flags.
- [ ] Repeat the observed Porch Light discovery with the same 1.7B model and
  inspect round-1 empty completion before changing model, stop sequences or session behavior.


- [x] Diagnose the empty Lailama continuation: LLamaSharp 0.27.0 subtracts the
  reference-input token count from the response budget.
- [x] Preserve output budget in the Lailama executor adapter (1.0.23), retaining
  existing limits, cancellation and native lifecycle; add deterministic regression.
- [ ] Repeat the same 1.7B discovery request to validate the native continuation
  and subsequent fresh parameter read before moving to configuration changes.


- [x] Separate Lailama intermediate stream reset from whole-request completion
  (1.0.24 + YoT): keep waiting through lookups, discard provisional rounds and
  speak/save only the final response.
- [ ] Manual acceptance of the multi-round waiting/streaming UI with the existing
  1.7B model; retain the simple parameter-read prompt as a functional reference.


- [x] Simplify local read instructions (AI Genie 0.8.1): direct parameter lookup
  when its ID is known; exactly one block and stop, with no future-step examples.
- [ ] Recheck direct-read compliance with the existing 1.7B model; keep multi-step
  feature/owner discovery as a separate model-capability test.


- [x] Restore persistent Lailama ChatSession/KV reuse (1.0.25); reconstruct only
  after incompatible history/profile/prompt, interrupted work or memory limit.
- [x] Label fresh per-turn data and API results separately from historical values;
  omit historical think blocks during reconstruction without altering stored chat.
- [ ] Verify session reuse plus a fresh parameter reread after a manual value change,
  without clearing history or restarting between requests; keep 1.7B compliance
  separate from the correctness of cache invalidation and API dispatch.


- [x] Add one bounded local context-request format correction; no retries for
  permissions, mixed commands, collector outages or exhausted lookup limits.
- [x] Persist completed context-read paths in message metadata and show them in
  a collapsed API group, separate from executable device commands and TTS.
- [ ] Manual repair/receipt acceptance with Gemma: incomplete ID, single correction,
  fresh read and persistence of the read group after reloading history.


- [x] Fix History.RemoveAt numeric-tail handling in Gemini/Lailama; confirm on-disk
  save and reject HTTP-200 application errors in the UI instead of false success.
- [x] Verify deletion/reload, invalid indices and save-failure rollback using only
  temporary history files. Clearing a loaded Lailama conversation needs no warmup.
- [ ] Manual deletion acceptance after updating history service and wrapper: remove
  a test message, reload the page and verify that it remains absent before AI tests.


- [x] Separate Lailama information sharing from deviceControl (1.0.28), using the
  existing module preference and capability selector without changing text payloads.
- [x] Enforce current permissions at both execution entry points, including legacy
  structured actions and permission changes during generation; keep reads available.
- [ ] Manual read-only acceptance: current parameter read, blocked control, persistence
  across reload and re-enabled control before connecting shared parameter writes.

### Lailama configurable features checkpoint

- [x] Route local parameter-write batches through the common device dispatcher and existing feature validation/readback, preserving action identities and current permissions.
- [x] Add shared instructions for feature discovery, units and standard Modules.ParameterSet writes.
- [x] Avoid saving provisional configuration-success text when execution is rejected or unconfirmed.
- [x] User acceptance with Qwen3-4B-Instruct-2507: fresh read 120 seconds, verified write to 180 seconds, fresh read 180 seconds; read-only request refused with zero actions; re-enabled write verified from 180 to 60 seconds.
- [ ] Continue provider-independent coordination after this checkpoint; program/widget generation remains a later local-model test.

### Common text conversation coordinator

- [x] Extract local lookup/format-repair/final-dispatch sequencing into AI Genie Conversation.Text.Run using in-memory delegates and cancellation.
- [x] Preserve local native session ownership, streaming, request IDs, intermediate thoughts, read receipts and single final persistence.
- [x] Keep existing limits: three reference reads, one format repair and five generations; never repeat final actions after an uncertain result.
- [x] Manual acceptance after updating AI Genie and Lailama: fresh read 60 seconds, one verified write from 60 to 90 seconds, and a new API read of 90 seconds. User confirmed the chat stays busy through API calls and the end of inference. Permission disable/re-enable was verified in the preceding checkpoint.
- [ ] Continue convergence with the structured Gemini strategy after regression acceptance; local program/widget generation remains later.

### Lailama program deployment checkpoint

- [x] Add opt-in Automation capability and the existing chat context menu for Devices/Automation Programs, preserving text prompt payloads and provider APIs.
- [x] Adapt one genie-action fence to the canonical program contract and the existing shared deployment/recovery executor.
- [x] Require current permissions, explicit source context and a successful current-turn source read before updating a managed program.
- [x] Allow bounded compilation corrections with stable action/target/request identity; never retry unknown deployment outcomes.
- [x] Render deployment cards and speak their explanation even when reasoning precedes the receipt.
- [x] Manual creation acceptance with the 9B model: program 1002 created on attempt one; installed sources and enabled state match the requested background helper.
- [x] Handle JSON-null resources and automatically retrieve missing exact-target program sources before a local update, preserving pending action/target and read limits.
- [ ] Retest updating program 1002 after the required-source correction, and check the disabled Automation capability.
- [x] Move provider-specific child Quit handling into the common service before local widget/composite support (runtime acceptance recorded below).

- [x] Keep prior reasoning visible during empty-buffer resets and persist reasoning in failed local responses, including unfinished think blocks.
- [x] Exclude unfinished reasoning from context/program parsing and recover confirmed compile-failed programs on aborted correction, using the existing request ledger and recovery API.
- [ ] Retest program 1002 after the 9B CS0103 failure; original first-version background sources were restored through Program.Recover.

- [ ] Investigate intermittent disappearance of reasoning during local multi-round streaming/tool calls. User still observes it after retention fixes; defer code changes. Reference: chat-history-1792268747142.json (Qwen3.5-9B). Its final successful update retains two think blocks, so distinguish transient rendering/reset loss from history persistence loss.

- [x] Move Gemini generated-program Quit handling to AI Genie utility 583, shared with Lailama; Completed remains a result and Quit disables without deleting. Add lifecycle regression tests.
- [x] Verify shared lifecycle in the running test environment: program 1003 stayed enabled after Completed; Qwen3.5-9B updated it successfully at the first deployment attempt, emitted Quit, and utility 583 disabled it with sources preserved (2026-10-18, chat-history-1792278506270.json).

- [x] Invalidate Lailama native session after technical/multi-round turns, including successful compiler recovery; rebuild next turn from the cleaned completed-message projection while retaining full visible history.
- [ ] Runtime check of post-recovery context cleanup with Lailama 1.2.4; no need to clear the stored conversation.

### Lailama widget/composite checkpoint

- [x] Opt-in Widgets capability and independent Widgets context through the existing descriptor/module preferences.
- [x] Adapt local genie-action widgets/composites to existing shared presentation, deployment, recovery and dependency readiness contracts.
- [x] Preserve committed composite binding during compiler repairs and block preview for failed/unknown deployment.
- [x] Support bounded current-context widget source reads and invalidate native cache after artifact presentation.
- [x] Parse leading local reasoning before ai_solution without changing literal widget source content.
- [x] Local standalone widget generation/presentation acceptance: Qwen3.5-9B returned HTML/CSS/JS and the user saw the widget (chat-history-1792312467517.json, 276 seconds). Generated interaction has code defects; functional preview and Save acceptance remain separate.
- [x] Gemini composite acceptance: program 1004 emits Sensor.Counter=42; inline preview shows 42, widget saved as tests/test-composito-ai-1004 and added to Dashboard, retaining value/binding after page reload (2026-10-18). Local standalone counter generation had code defects; local composite acceptance remains separate.

### Provider convergence audit after composite checkpoint

- [x] Audit current 583/933/940 responsibilities after composite preview/Save/dashboard persistence acceptance.
- [x] One canonical response contract in Action Executor 581, schema access through in-process Api.Call, and shared capability validation/allowed-kind filtering in AI Genie 583. Removed Gemini copies; Lailama keeps only its provider-default/context adapter. Provider defaults, persistent preferences and context profile keys are preserved.
- [x] User confirmed canonical-contract runtime regression. Attached local chat verifies fresh reads with device control disabled and host blocking of a forbidden write. An enabled command borrowed the fictional MQTT prefix; the local prompt now requires copying the exact inventory ID and removes that misleading example. User confirmed subsequent commands use correct addresses (2026-10-18).
- [x] Extract per-turn request IDs, clocks, metric imports, outcomes and final history persistence into AI Genie Turn.Create; both providers use in-memory BCL delegates with no shared global turn registry. Preserve provider metadata fields and existing history format.
- [x] Runtime acceptance of shared turn persistence: user tested both providers and refresh/reopen. Stored history_933.json (16 messages) and history_940.json (12 messages) confirmed no duplicate model finals per requestId; local cancellation persisted one cancelled response containing [STOPPED] (2026-10-18).
- [x] Centralize provider-turn History.Read/History.Project in AI Genie 583. ChatHistory 577 retains completed/profile filtering; Gemini structured messages remain verbatim, Lailama retains its previous text cleanup. Provider stores, saved/imported messages, native cache/rebuild rules and message limits are unchanged.
- [x] Shared history projection runtime check (2026-10-18): Gemini correctly recalled Luce 1 and confirmed Smart Device=On after a verified parameter write. Lailama continued from an actual earlier Porch Light command, but described it as the latest request despite a newer completed Luce 1 read in the same profile. Record that local response imprecision; persisted chats alone do not establish its cause. Both stores have zero duplicate model finals; no extra actions occurred in the recall turn.
- [ ] Optional improvement: preserve partial local response text when cancelled, marked as interrupted and excluded from future provider context; never execute partial commands. User accepts the current [STOPPED]-only saved response for now; defer implementation.
- [ ] Share confirmed compile-failed abort recovery decisions; retain ledger protection for unknown/completed effects.
- [ ] Define scheduled-task and pure-text Ask parity separately (Gemini background task/status/Ask differs from synchronous local Schedule).
- [ ] Reassess release readiness after the contract/policy cleanup and brief provider regression tests; no requirement for intensive local code-generation tests.

### Dashboard preset refresh policy (deferred)

- [ ] Evaluate whether/how an already-configured browser should refresh a shared backend preset. Current behavior is intentional pending review: startup uses local configuration; clearing browser data reloads the remote preset during setup, and users can explicitly select a preset. User verified this behavior after the 2.0.29 release commit. Preserve it for now. Discuss local-vs-remote precedence, browser-specific preferences, concurrent edits and explicit reload/conflict handling before any implementation; do not introduce automatic overwrite or synchronization as an assumed optimization.
