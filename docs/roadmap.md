# Roadmap

Consolidated status: 2026-10-18, after the shared Genie engine and scheduler execution tests.

This is the active task list. Detailed stage-by-stage decisions, earlier API names, test counts and acceptance evidence are preserved in [implementation history](roadmap-history.md); they do not override this status. Completed work is not reopened merely because an earlier checkpoint still says pending.

## Next activities

- [ ] Next release: deprecate the old specialized Genie programs 930, 931 and 932 and remove them from shipped package manifests and program bundles. Review package dependencies, installation/restore behavior and current documentation references. This item does not authorize automatically deleting existing user installations, settings or histories.

- [x] Consolidate the active roadmap and preserve historical evidence without treating unverified tests as passed. Prepare separate HomeGenie and YoT commit descriptions; the maintainer performs commits.
- [x] Complete AI Genie default-provider settings and scheduler provider labels in all 91 supported languages. Added the six missing keys in the other 89 locales, preserving unrelated values. Structural audit of 728 files across eight catalogs found no missing keys, empty translations, value-type mismatches or interpolation-token mismatches against English. Existing decorative separator duplicates in English/Italian were retained. Distributed and Vulkan HomeGenie catalogs match the source; service-worker translation hashes were refreshed. This is structural completeness validation, not native-speaker review of every existing translation.
- [ ] Review current documentation, website guidance and examples against the public Request APIs. Keep historical Automan examples identified with their actual legacy program; do not silently rewrite their original workflow. Review the longer shared-engine guide's intermediate-stage descriptions as well.
- [x] Local release-readiness assessment (2026-10-18): all 31 owned programs in homegenie-base/homegenie-ml-ai have matching script/XML/manifest checksums and versions; cross-package program versions/checksums agree. Bundled main contains the new scheduler UI/rendering logic, its preset matches YoT, and all referenced application resources/cache hashes pass. The pre-existing missing favicon.svg was fixed in YoT build/test assets, SVG MIME declaration and service-worker configuration, then staged in both bundles with officially regenerated Angular manifests; live /app/favicon.svg returned HTTP 200 and image/svg+xml. Final bundled base-href is /app/ and all 14 dedicated tests passed. Together with 493 backend regressions, translation audit and live provider/scheduler acceptance, no new functional release blocker was identified. Final CI/release artifacts still depend on the actual release build; no tag or publication was performed. Release notes must explain removed APIs and regenerating saved Genie scripts. No intensive local coding test is required.
- [ ] After consolidation and release review, discuss the next provider adapter. OpenAI is a candidate; start with conversation, reads and device control, reusing the common engine and keeping credentials on the backend. No new provider implementation is authorized by this roadmap item alone.

## Completed: shared Genie engine and providers

- [x] Required AI Genie utility 583, shipped in homegenie-base and categorized under AI - Machine Learning. Existing installations retain their chosen program group on package updates; the test installation was moved explicitly.
- [x] Shared context collection/composition, capability policy, canonical contracts, response coordination, device/configuration actions, automation deployment/repair, widget/composite presentation, metrics and final history persistence. Reuse Context Engine, Chat History and Action Executor rather than duplicating their responsibilities.
- [x] Gemini and Lailama connect through in-process Api.Call, including native objects/delegates where needed. Provider adapters retain transport, response strategy and session ownership; Lailama retains model download/loading/disposal. Application features share contracts, not a guarantee of equal model quality or performance.
- [x] Per-provider module preferences for context and capabilities, discovered through Program.Implements. Information sharing and execution permissions remain separate; the model cannot enable them. Profile changes restrict in-flight work and invalidate/rebuild native context as required.
- [x] Module schema and feature-owner discovery, targeted fresh parameter reads and validated checkbox/slider configuration writes through the existing Modules.ParameterSet API. No generic parameter-write permission or unrestricted system-administration API is implied. See [module discovery](guides/module-discovery.md).
- [x] Program lifecycle: Completed leaves reusable helpers enabled; Quit disables without deleting sources. Common bounded compiler repair and confirmed compile-failed abort recovery protect completed/unknown effects from replay.
- [x] Program creation/update and widget/composite paths connected. Gemini composite preview, Save and dashboard persistence passed. Local standalone widget creation/presentation passed, but generated interaction had code defects; local composite functional acceptance is not claimed.
- [x] Shared turn/history projection preserves provider stores and visible/exported/imported messages. Legacy chat import works; do not normalize or rewrite working histories without a concrete requirement.

## Completed: independent requests and scheduler

Public common prefix: System.Utility/AiGenie. Provider prefixes: AI.Genies/Gemini and AI.Genies/Lailama. Public request contract: [background requests](guides/ai-background-requests.md).

- [x] Request.Generate returns generated text without executing it. Request.Execute uses the shared action coordinators. Both use Request.Status/Request.Cancel and independent, bounded in-memory jobs, without altering chat history or publishing background tokens into chat.
- [x] Explicit provider program ID or configured DefaultProvider, resolved at each request. DefaultProvider is a standard AI Genie setting; context/capability preferences stay in each provider module. Disabled/unavailable choices fail without silent fallback. Only one background job per provider; no durable queue.
- [x] Current provider policy applies to both operations. Callers cannot expand permissions. Execute retains action receipts and conservative uncertain-effect information; cancellation stops subsequent work and does not roll back dispatched actions. Do not automatically repeat uncertain requests.
- [x] Scheduler Genie select precedes the natural-language field. Each action, including each/start/end slots, stores its own provider. Explicit choices require an enabled requests.execute descriptor; an empty choice means Default.
- [x] Scheduler uses Request.Execute with native wait=true and timeoutSeconds=600, preserving the asynchronous default. Native waiting owns cancellation because escaping CLR exceptions bypass JavaScript finally in Jint. Stop following commands on a non-completed outcome; no scheduler/core changes.
- [x] Removed unified provider Prompt.Ask, Prompt.Schedule and Gemini Task.Status/queue worker without aliases. Lailama uses AI.Genies/Lailama exclusively. Legacy specialized Automan functionality remains separate.

Migration: reopen and save existing generated Genie schedules to regenerate their stored script. Custom scripts/programs using removed routes need explicit migration to Request.Generate or Request.Execute with status/cancellation handling. Do not automatically rewrite custom code, imported chat histories or saved schedules.

### Verification completed

- [x] 493 backend regressions passed, including actual Jint execution, native timeout/cancellation, disposal followed by thread interruption and subsequent-action ordering. Scheduler frontend regression and TypeScript compilation passed; full distributed 581/583/933/940 scripts compile and package checksums were verified.
- [x] User confirmed Generate for both providers, configured-default resolution and local cancellation. API Execute tests covered fresh reads, real device commands with receipts, disabled deviceControl and cancellation; test device state/preferences were restored.
- [x] Browser scheduler acceptance: Test.AiGenie.Scheduler retained Default/Gemini/Lailama selections on save/reopen. The generated script requires completed results for all three calls; actual scheduler Start at 20:58:00 and End at 20:58:28 had no Error.
- [x] During a later long local request the provider slot was busy. Disabling the event ended the script; a subsequent local Generate completed with SCHEDULER_RESUMED_OK. Both chat-history SHA256 hashes stayed unchanged across this stop/resume test.
- [x] Maintainer updated programs 583 0.20.0, 933 1.0.24 and 940 1.3.8 and reloaded the frontend before scheduler acceptance. Test event retained disabled with cron 0 0 1 1 * and short prompts restored. No remaining "install these test versions" task.

## Completed: release/program update reliability

- [x] 2026-10-19: release-update dialog now verifies the installed release, replacement process and existing System.GetBootProgress API before reporting success. It polls boot progress after observing the replacement process, tolerates transient disconnects/ready states, displays confirmed backend error details, and distinguishes an unconfirmed restart timeout from installation failure. Retry checks status only. Frontend regression coverage includes slow boots, missed events, fractional process timestamps and invalid/overflowing progress; no core changes.
- [x] RPi false release-installation error: identified a startup race in the v2.0.28 HTTP poller (YoT 6fad56b0, 2026-10-16). HTTP is reachable inside Reload() before the HomeGenieService constructor assigns UpdateChecker. On production the first DownloadStatus poll ran at 16:40:17.003 and completed at 17.159, before Reload() logged service ready at 17.969. The Config handler converts the null-updater exception into HTTP 200 with Status: Error; the original poller immediately displayed installation failure and unsubscribed. The same sequence reproduces against the original component and recovers with the corrected component. WebSocket disconnection alone schedules automatic reconnection; HTTP transport failures alone remain pending until the deadline. The actual response body was not captured on the RPi; the startup ordering and matching frontend failure are established by log/source and deterministic reproduction. The initial unsuccessful Update All attempt remains a separate unexplained symptom; the later production retry updated all four programs.

- [x] Persistent GitHub release download state, progress, partial-download/resume handling and reuse of completed downloads. Controlled install/restart tests covered release and required-program reconciliation; only the new required fixture was installed, optional/customized/disabled programs and groups/schedules were preserved, and a second startup did not repeat migration.
- [x] Common backend program update inspection, persistent update notice, Update All, stable updates view, update history and bounded backup retention/cleanup.
- [x] Production Raspberry Pi acceptance reported by the maintainer: v2.0.28 upgrade and package restore completed successfully. Reopen the spinner issue only for a reproduced failure.
- [x] Bundled frontend base-href guard and release-build enforcement. Custom-widget redraw correction consolidated in v2.0.29; do not introduce unrelated rendering optimizations.
- [x] Persistent notifications and optional, consent-based retirement of old specialized Genies. Existing settings/prompts/histories remain; no automatic destructive migration.

## Deferred investigations and optional verification

These are not newly declared release blockers. Keep the original evidence and discuss scope before changing behavior.

- [ ] Investigate intermittent loss of earlier reasoning during local multi-round streaming/tool calls. Distinguish transient rendering/reset loss from saved-history loss. Reference: chat-history-1792268747142.json; earlier retention fixes did not settle every observed case.
- [ ] Optionally preserve partial local text after cancellation, marked interrupted and excluded from model context. The maintainer accepts the current saved [STOPPED] behavior; never execute partial commands.
- [ ] Reassess local model-specific discovery/coding quality only when useful. Earlier exact 1.7B/Gemma test cases and post-compiler-recovery context-cleanup acceptance remain historical unconfirmed checks, not inferred passes from later Qwen/Gemini tests. Local composite preview/Save functional acceptance is also separate. Prefer deterministic recorded/fake outputs and brief live tests; intensive local coding is optional. Vulkan GPU use was confirmed, so CPU fan noise alone is not evidence of GPU failure.
- [ ] A real model-download regression and automatic service relaunch through start.sh were not established by the manual test-service restarts. Revisit when those exact paths need verification; do not claim they passed.
- [ ] LoggerFactory disposal versus automation-thread shutdown race: explicitly low priority. Lailama cleanup now works, but does not close this separate host logging-lifetime issue.
- [ ] Profile chat CPU with the same dashboard, chat closed versus empty/unconfigured chat open. Cause of sustained extra CPU remains unestablished; investigate before optimizing.
- [ ] Evaluate shared dashboard preset refresh policy. Preserve browser-local configuration precedence for now; discuss explicit reload, conflicts and concurrent edits before adding synchronization or overwrite behavior.

## Future extensions

- [ ] Scheduler code caching per Genie action, not per whole schedule: explicit cacheable flag, original prompt, invalidation/regeneration and wizard round trip. Dynamic reads may live in reusable code. Do not replay partial/uncertain effects. Explicitly deferred; current execution remains unchanged.
- [ ] Optional domain skills and on-demand resource loading/MCP. Fundamental skills already follow capabilities; do not add a duplicate menu for the same choices. A skill must not enable execution permissions.
- [ ] Additional configurable field types beyond checkbox/slider and a searchable HomeGenie API operation catalog. Broader system configuration requires a separate design discussion. Smart Device timeout=0 currently means the 30-second default; disabling SmartLights.Enable affects the whole feature, not just auto-off. Do not invent a narrower setting.
- [ ] Broader mixed-action plans, composite updates and project-scoped Magik workflows, including hardware guidance and dashboard integration. Keep existing preview/Save behavior; broader autonomy is not implied.
- [ ] Durable background jobs/recovery and richer notification filters; per-user read state only if user-scoped accounts exist.
- [ ] General interface capability to opt out of automatic control-group creation, preserving existing/manual groups and module/event routing.
- [ ] RTL support and remaining AI/UX polish, including compact widget badges and clearer result-versus-compiler-detail presentation. These come last.
- [ ] Revisit import normalization only for a reproduced unsupported format, lost metadata/artifact or concrete contract requirement. Normalize on import if needed, retain the original and never execute historical actions.
- [ ] Complete documentation organization/cross-link review where still needed, without moving published URLs blindly.

HomeGenie and YoT follow deliberate host conventions. Discuss core changes with the maintainer first; do not infer that an unfamiliar design is an optimization opportunity.
