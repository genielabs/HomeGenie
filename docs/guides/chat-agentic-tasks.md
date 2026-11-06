# Agentic chat tasks

## Incremental implementation

Checkpoint one adds durable conversation/task storage to Chat History Service 577. Checkpoint two exposes provider-scoped routes and connects persisted change events to non-blocking YoT history reconciliation. The deterministic demonstration runner is now prepared only in the local Vulkan test installation. Services 577 and 933 were updated there after backing up their original sources/history; no LLM request was started during installation. The maintainer subsequently confirmed success, simulated failure, responsive Stop, refresh and synchronized two-browser updates/Stop. Checkpoint three adds task rendering, cooperative cancellation and restart/import recovery. The next checkpoint connects bounded Gemini widget objectives.

One automation program will own each objective. Its persisted task record belongs to the same provider-scoped conversation as the user's messages. The chat uses dedicated task metadata from creation. Automation result rows also inspect `Program.UiType`: an absent property keeps Automan controls, `agentic-task` resolves `AI.Task.Id` to the persisted task, and unknown types use an informational fallback without execution buttons. A runner must set its type and task association before publishing a program-result message.

## Persistence and compatibility

History files remain under service 577 as `history_<store>.json`. On the first successful write, legacy message arrays become a versioned document:

```json
{
  "version": 1,
  "conversationId": "32-character-guid",
  "revision": 1,
  "messages": []
}
```

`Get`, `GetContext`, `Export`, provider history endpoints and portable image exports retain their current response formats. Existing text, attachments and widget metadata are preserved. Messages acquire stable `id` fields when saved. Public exports remain message arrays; importing an export starts a new conversation identity. The document envelope is internal storage, not a new provider prompt format.

The identity is persisted even for an empty conversation. Clearing or importing history replaces it, so a runner holding the previous identity cannot write into the replacement conversation. The identity and revision are in the same file as the messages, avoiding a separate metadata file that could become inconsistent.

Writes use a unique staging file in the destination directory, flush it and replace the destination. A failed write does not advance the revision or publish a successful task update; staging files and tentative in-memory changes are removed. Invalid existing history documents fail rather than silently becoming an empty conversation. This does not promise filesystem recovery from every hardware or power failure.

An older service version cannot read the new on-disk envelope. Preserve the original history when testing upgrades; for a deliberate downgrade, export through the new service and restore/import the compatible message array. No existing runtime history is migrated merely by editing these package sources.

## Internal API checkpoint

Call `System.Utility/ChatHistory/<command>` with `StoreId` fixed by the caller/provider. Gemini and Lailama also expose the same command names under `AI.Genies/Gemini/` and `AI.Genies/Lailama/`. Each wrapper replaces `StoreId` with its own provider ID and leaves the caller payload unchanged. Routes are registered before native model initialization; reading progress does not require a loaded LLM.

- `Conversation.Get`: returns `{Status, version:1, conversationId, revision, unchanged:false, messages}`. Send the last known `conversationId` and `revision` to receive `unchanged:true` without messages when nothing has changed. Reading an already persisted conversation does not advance its revision.
- `Task.Create`: `{StoreId, conversationId, taskId, title}`. The caller creates a GUID task ID before submission. Retrying the same ID/title returns the existing task without creating another record.
- `Task.Get`: `{StoreId, conversationId, taskId}`. Returns the current task record and conversation revision.
- `Task.Cancel`: `{StoreId, conversationId, taskId}`. Idempotently records `cancelRequested:true` without claiming the worker has stopped. Available through both provider wrappers. Ended/read-only tasks return their existing receipt.
- `Task.Update`: `{StoreId, conversationId, taskId, expectedRevision, state?, operation?, message?}`. `expectedRevision` is the task's own revision, allowing independent tasks in one conversation. A stale update returns an error and must be re-read before a deliberate retry; it is never applied twice.

Successful task mutations return `{Status:"Ok", conversationId, revision, message}`. Errors return `{Status:"Error", Message}`. `Task.Create` and `Task.Update` run under the same lock as ordinary history writes. Receipts contain detached snapshots, not references that can mutate the canonical history.

A task is a model message with `metadata.agenticTask` containing version, ID, title, state, task revision, update time and operations. Operations contain GUID `id`, `title`, `state` and optional `details`, and are updated in place by ID. An optional progress message is appended in the same write, linked with `metadata.agenticTaskMessage.taskId`.

States are `pending`, `running`, `waiting`, `completed`, `failed`, `cancelled`, `interrupted`. Ended tasks cannot be reopened through `Task.Update`. A task cannot be completed while an operation remains incomplete or failed. Starting a retry or a resumed run requires a later explicit lifecycle design, not an ordinary automation Play action. After cancellation is requested, further progress/completion updates are rejected; the worker re-reads `Task.Get` and acknowledges `cancelled` (or reports `failed`/`interrupted`) using the current task revision.

Limits at this checkpoint: 240 characters per title, 16,000 per detail/progress message, and 200 operations per task. Progress is observable facts/results, not private model reasoning. These records stay out of the existing model-context projection. Dedicated continuation/context handling will be implemented with the agentic loop.

## Live synchronization checkpoint

After a successful history write, service 577 emits `Chat.Changed` on the owning numeric provider's automation module. The event contains only `{version:1, conversationId, revision}`. It is emitted outside the history lock, after persistence, for ordinary messages, task changes, widget metadata, removals, import and clear. Failed saves and read-only requests do not announce a new revision; the first persisted conversation identity does.

The selected open YoT chat treats events as hints and retrieves an authoritative conditional snapshot over bounded HTTP. Changed snapshots are reconciled by stable message ID; unchanged message objects and DOM identities are retained. Full changed snapshots also account for deleted messages and replacement conversations without a separate deletion journal. This checkpoint does not implement per-message delta transport.

Reads are coalesced, time out after 10 seconds, and defer both fetching and applying while a local request/stream is busy or the page is hidden. Provider changes and chat closure cancel outstanding synchronization. Draft text, pending image attachments, retry state and the reader's scroll position are preserved; background synchronization does not show a blocking loader or speak replies.

While the chat is open, a conditional read every 15 seconds recovers dropped socket events, including the final event after a reconnect. Online, pageshow and visibility-return events also request reconciliation. Older providers retain their existing `History.Get` behavior when the new route is unavailable. Gemini and the historical Lailama `IntentHandler` transformation keep their current rendering conventions.

Automated checks cover persistent event ordering, provider isolation, conditional reads, deletion/clear, stale and coalesced hints, paused streams, failed/hung reads, provider switching, and unchanged message identity. Two-browser updates, refresh and Stop have been accepted live by the maintainer. Sleep/reconnect and disposable-history clearing remain separate checks.

## Task card and lifecycle checkpoint

YoT renders a dedicated card for known `metadata.agenticTask` records. It shows the objective, state, completed/current operation count, progress, and operation details as escaped text. Stable operation IDs preserve expanded details during updates. The card has a cooperative Stop action and no Play/Restart controls. Unknown metadata versions/types use an informational fallback. Normal program-result rows still use Automan unless their module explicitly selects another renderer.

The Stop action calls only the provider's `Task.Cancel` endpoint. It is scoped to the original selected provider/conversation, has a 10-second timeout, prevents duplicate clicks while pending and displays stop-requested only after a valid persisted receipt. Failures keep the task active and allow retry. Neither the browser nor the storage service invokes automation Break or claims that arbitrary code has stopped. Workers must check the cancellation flag between operations and during interruptible waits; the demonstration runner will exercise this protocol.

When history is loaded after a service restart/recompile, active task records are persisted as `interrupted`, with their revision advanced once. Active operation labels are also interrupted; completed/failed outcomes are preserved. If recovery cannot be persisted, the read fails without caching a false recovery result. A second restart does not advance an already ended task. This is deliberately conservative: restarting service 577 invalidates active task ownership even if an automation worker is still alive; its later updates are rejected and it must exit.

Imported tasks are read-only history. Active imported records are marked interrupted; nothing is resumed or scheduled. Terminal records keep their outcomes. A cleared/replaced conversation still rejects a late cancellation or worker update carrying its old identity.

The four new status labels are supplied in all 91 distributed locales; the translation cache version is incremented. Automated checks cover card DOM behavior, escaped details, stable disclosure state, cancellation receipts/failures, renderer selection and restart/import recovery. The maintainer has now confirmed the deterministic demonstration live, including synchronized Stop in two browsers.

## Following checkpoints

1. Completed: provider-scoped APIs, persisted change notifications and conditional revision reconciliation, including removals and missed WebSocket events.
2. Completed: selected-Genie synchronization, dedicated task card, generic renderer fallback and stable operation details (automated DOM checks).
3. Completed: persisted cancellation requests/acknowledgement fencing and conservative restart/import recovery. The worker-side Stop was accepted live.
4. Accepted on Vulkan: local-only program 1031, with successful completion, simulated error, responsive Stop, refresh and two-browser synchronization confirmed by the maintainer. Explicit sleep/reconnect and disposable-history clearing are still separate checks.
5. Implemented for the next live test: explicit Gemini widget objectives, bounded provider continuation and real widget saves. Program generation, dashboard placement and broader agentic operations remain future checkpoints.

There is still no automatic restart/resume or generalized artifact API. The local demonstration calls the same task APIs intended for future Genie runners, performs three simulated operations and checks cancellation during waits. It is excluded from distributed packages, as are benchmark fixtures 1028/1029. Default live target is Gemini; Lailama 940 has not been updated or restarted by this installation. The demonstration was run and accepted as described above.

## First real Gemini widget objective

Use the flag button **Widget objective** in the Gemini composer, with widget creation enabled, then submit a text objective. Ordinary chat submission is unchanged. This first loop supports widget sets and parent widgets that import their children as Web Components; it does not generate automation programs, dispatch device commands or install dashboard layouts. Existing widget sizes are unchanged. Images in goal requests and Lailama goals are deferred.

Gemini exposes `Goals.Describe` and `Goal.Start`. Start takes `{goalId, conversationId, request}` with the ordinary version-1 scoped request. The provider fixes its identity internally and uses the existing generation adapter/API key/model. History 577 atomically appends the user objective and task card, retaining the initiating context profile. A single fixed-template automation runner per objective is created in **AI Tasks**, with `Program.UiType=agentic-task`, `AI.Task.Id`, no auto-restart and no automatic execution from Setup. It starts only after the task is bound to its program ID. Repeating a lost start request with the same goal ID returns the existing task and never recreates or restarts its runner. An already active widget objective blocks starting a second one for that provider.

The worker requests one JSON step at a time: `read_context`, `save_widget`, `complete` or `fail`. It reuses scoped context/knowledge and response normalization. A failed validation is fed back for at most two consecutive corrections. Limits are 12 generations, eight widgets and 15 minutes, in addition to the existing provider timeout. Stop is checked during generation and between steps; it cancels the matching generation rather than another chat request. Clear/import identity changes detach the old worker. No implicit recovery or retry of uncertain effects is implemented.

Saves are confined to `goals/g<taskId>/<leaf-slug>`, so existing widgets outside the objective are not overwritten. JavaScript parsing precedes saving. All three sources and the generated module are checked after the write. If saving fails, new partial bundles are removed or previous owned sources are restored through the widget API; a failed recovery is explicitly reported, never hidden or retried by the model. A short three-file save finishes before acknowledging Stop, retaining any confirmed artifact. This is a source/save check, not a claim that browser rendering or device interactions have passed.

The persisted task lists confirmed `{kind:"widget", id}` artifacts, with links to their editor. Terminal user-visible summaries re-enter the same context profile; intermediate progress and private planning do not. Finished/cancelled objectives retain their saved widgets. Runner programs also remain available for inspection; manually running an ended task cannot reopen it.

Automated tests cover idempotent starts, capability refusal, source readback, bounded repair, false completion, Stop, history-clear fencing, save failures/partial cleanup and artifact namespace validation. Live Gemini generation/composition is the next maintainer acceptance test. The local-only demonstration/benchmark programs remain excluded from shipped packages.

## Live widget composition and narration (2026-11-03)

The maintainer's first real Gemini objective completed with three saved widgets: digital clock, counter and a parent that embeds both as Web Components. Screenshots confirm the individual widgets and composite render in the editor. The first failed attempt exposed missing URL encoding of a widget ID as a single API option; saves and rollback now use the same encoding convention as YoT, and worker tests parse routes with the actual MIG command parser.

YoT now narrates newly observed public task messages and short completed-step titles through the existing shared browser/local speech channel, honoring automatic speech settings. Updates are queued rather than interrupting each other. Initial history, provider resets and replacement/imported histories establish a silent baseline; regular direct replies retain their existing speech path. Repeated snapshots do not repeat narration. Stop, chat closure, provider changes, speech disable and another explicit speaker discard obsolete queued audio. Technical operation details are not read aloud. Automated narration selection/queue checks pass; audible maintainer acceptance follows the frontend refresh.

## Revising a saved widget set (2026-11-03)

An ended objective with saved results exposes **Edit** in its task card. It selects that objective as the target for the next text request; the composer shows its title. Turning objective mode off clears the target. Ordinary chat and new untargeted objectives keep their previous behavior.

`Goal.Start` accepts optional `parentTaskId`. History 577 validates that the parent belongs to the same conversation, has ended, owns saved results and is not imported/read-only. The child task persists `parentTaskId`, derives `rootTaskId` from the parent and inherits its artifact references. A lost start receipt remains idempotent, including its parent identity. A revision has a separate runner/card and does not reopen the old task.

The worker supplies actual saved HTML/CSS/JavaScript for the selected set to Gemini. Saves retain the original namespace and stable leaf names, including existing Web Component imports/tags. Source reads are limited to 128 KiB per file and 512 KiB total revision context. A revision must perform at least one confirmed save before it can complete; inherited artifacts alone are insufficient. Revisions can be chained while retaining the root widget paths.

Existing widget sources are fingerprinted when read, then compared immediately before writing. A detected manual edit stops the revision rather than overwriting it. A name already occupied by a widget outside the inherited/current result list is also rejected. Failed saves restore the previous owned sources where possible and report any failed recovery. These checks do not provide a cross-process atomic transaction for arbitrary external writers.

Automated checks cover in-place updates, actual source context, chained revisions, invalid/active parents, no-op completion rejection, concurrent edits, rollback, unselected name collisions and UI target selection. The maintainer confirmed the in-place revision: the composite retained its widget paths and changed to a horizontal layout. Speech acceptance remains separate. Saved-widget Edit links now request an explicit source/preview reload even when their URL is already open. Unsaved editor changes use the existing discard confirmation; cancelled confirmation, obsolete requests and network failure preserve the existing buffers. This also applies to ordinary saved-widget chat results. Editor buffers are not replaced automatically when a background revision finishes.

## Automation programs in objectives (2026-11-03)

The bounded Gemini loop now accepts `save_program` when the existing automation capability is enabled. Its payload is a stable logical `name` plus `program:{name,language,setup,main,context}`. The model never selects a deployment PID or create/update mode. The host derives those from confirmed results; a new key creates a program, and a selected inherited/current key updates that exact program. Revision remains explicit through the objective card. Unselected existing programs, core services and the runner itself are not update targets.

Program creation/compilation uses the existing `AiGenieProgramActions` dispatcher and Action Executor 581 deployment sessions/ledger. Their one-turn behavior is unchanged. The goal retains one deployment request/target across compiler repair attempts and feeds diagnostics to Gemini. A repair cannot switch to another key or perform a widget/context/completion step first. The goal allows two corrections within its existing 12-step/15-minute bounds. Unknown deployment outcomes are not automatically replayed. Stop/error during a known incomplete compilation invokes the existing recovery, restoring an earlier program or retaining a newly created program disabled. The allocated PID stays in compiler details even when no compiled artifact is confirmed.

The worker verifies the returned PID, Automan ownership, compile result and installed sources before recording `{kind:"program",id,key,name}`. Program sources and enabled state are fingerprinted when supplied to the model, including between repair attempts. A detected manual change prevents further writes or automatic recovery from overwriting it. Explicitly selected program sources are supplied locally to update validation; this does not enable broad automation-program context globally. Capabilities are checked again immediately before writes. Limits are eight combined widget/program results and at most four programs.

The host supplies real PIDs to subsequent widget steps. Widgets can use existing `Program.UseWidget`, `moduleSelect`, `boundModule` and frontend API conventions; a compile receipt is not proof that runtime data is ready. Deployment preserves normal Setup/Main activation behavior and does not add arbitrary device dispatch or dashboard installation. The objective card lists programs separately with links to their normal editor; it retains its dedicated Stop behavior for the runner.

Automated tests cover program artifacts, compiler recovery on a single PID, selected-program revisions, capability refusal, Stop recovery, uncertain outcomes, forbidden identity changes and manual edits during generation/repair. The initial live test should use a manual notification program and a widget button that starts it, before testing persistent automation or device operations.

## Custom dashboard containers

`defaultSize: 'dashboard'` declares a widget usable as a dashboard container. YoT dashboard preferences expose Layout → Standard / Custom Widget and a widget selector. The saved dashboard config stores `layout:{mode:'widget',widget:{type:'custom-widget',configuration:{componentId,size:'dashboard'}}}` while keeping its standard `widgets` array. Standard mode retains the selected container as well. The existing configuration storage handles save/reload.

`HomeAutomation.HomeGenie/Config/Widgets.DashboardList` discovers complete HTML/controller bundles using Esprima and a literal `settings.defaultSize` class field. It does not execute controller code. Dynamic/computed settings and symlinked paths are not catalog entries. A missing/invalid/load-failed selection displays recovery controls for Retry and Preferences. Rebuild/restart the backend before testing the selector on an existing Vulkan instance.

The selected layout receives the remaining viewport area below application navigation, and the host provides overflow scrolling. Chat/editor/list use bounded previews. The AI may save a new/revised container, but selecting it for an existing dashboard remains a manual operation in this phase. No bundled widgets or existing dashboard selections are changed by installation.


### Embedded widgets and YoT context

Import the saved widget's `.module.js` and use its registered custom tag. In YoT the generated loader delegates to `ZxWidget`, providing `apiCall`, `subscribe`, translation and the enclosing widget's context. Outside YoT it loads as an ordinary zUIx Web Component; autonomous widgets remain usable without HomeGenie.

Use native zUIx option attributes (their values are JavaScript expressions evaluated in the parent component):

```html
<!-- No module option: inherit the enclosing widget's boundModule. -->
<examples-sensor></examples-sensor>
<!-- Explicit module, using the enclosing adapter by default. -->
<examples-sensor :module="'HomeAutomation.Demo:1'"></examples-sensor>
<!-- Select a different configured adapter when needed. -->
<examples-sensor :module="'HomeAutomation.Demo:1'" :adapter="'adapter-id'"></examples-sensor>
<!-- Explicitly unbound: do not inherit the parent module. -->
<examples-clock :module="null"></examples-clock>
```

These are syntax examples: use actual saved component tags, confirmed module IDs and configured adapter IDs. `domain:address` identifies a bound module; API calls still use `domain/address/command`. Binding is available before `onCreate()`, including through nested components. An unknown explicit module stays unbound; it never silently uses the parent module. The controller accesses `this.boundModule` as usual. Check its presence and render unavailable readings as unavailable, not static values suggesting live data. A widget that controls several modules can stay unbound and use `this.apiCall`.

The parent layout assigns each embedded widget its dimensions; `defaultSize` does not automatically size a raw custom tag. Use compact, responsive slots unless full-height cards are explicitly requested. Do not stretch every card across the remaining dashboard height by default. Child widgets that use height:100% need a slot with a defined height.

## Acceptance checkpoint (2026-11-05)

The maintainer confirmed deterministic success/error/Stop across two browsers, real Gemini widget composition and revision, program/widget objectives with retained identities, custom dashboard persistence and small-screen scrolling, live dashboard revision, and embedded YoT widget context. The current focused dashboard/conversation/embedding suite passed 35 tests with the real zUIx runtime; the generic loader fallback and reconnect behavior also passed targeted template checks. Final consolidation passed 204 frontend regression tests, 69 isolated backend tests against the shipped chat/runner/catalog sources, and Angular application template/type compilation. Package XML/catalog checksums and shared dependency references, all 11 generated source wrappers, and 7 new translation keys across 91 locales were checked; translation cache version is 53. These automated checks do not replace the outstanding live cases below.

Outstanding live recovery cases remain explicit in the roadmap, including Standard restoration, missing-widget recovery, objective speech after its fix, sleep/reconnect and history clearing during work. No claim is made that those paths passed. Multi-module reactive lookup (`this.getModule`) remains a next implementation step; it is not part of the current widget API. UI/UX polish is planned before release.


### Reactive multi-module state and command feedback

Inside YoT, `this.getModule('domain:address', adapterId?)` returns the current frontend `Module` reference without an HTTP request. It is available before `onCreate()` in ordinary and embedded widgets, even when the widget has no bound module. The default adapter is the widget's context (bound module / enclosing widget); an explicit adapter ID selects another configured adapter. Unknown IDs return `undefined`; never silently select the same module ID on a different adapter. Do not invent `utils.getModule`, `utils.yot`, or `this.utils` lookup APIs.

Render current values immediately, then use `this.subscribe(field, handler)` for one field or `this.subscribe(module, handler)` for all property events, including fields that appear later. Both return an unsubscribable subscription and are automatically released when this widget instance unloads. They do not replay an initial value. These are event subscriptions, not HTTP polling.

```javascript
// In onCreate(). Use a confirmed module ID.
const light = this.getModule('HomeAutomation.PhilipsHue:2');
const render = () => {
  const level = light?.field('Status.Level')?.decimalValue;
  this.model().available = Number.isFinite(level);
  this.model().level = Number.isFinite(level) ? level : null;
  this.model().isOn = Number.isFinite(level) && level > 0;
};
render();
if (light) {
  this.subscribe(light, field => {
    if (field.key === 'Status.Level') render();
  });
}
```

For a multi-light panel, resolve each confirmed module and keep independent state and subscriptions for each. Commands can use `module.control(command).subscribe(...)` or `this.apiCall` with the documented API path. Track directly owned `control` subscriptions and timers in `onDispose()`. Show sending/accepted/error feedback for the command receipt, but derive ON/OFF/level indicators from authoritative module fields; updates must also appear when another widget/app controls the device. A positive receipt alone is not confirmation of the target state. Use a bounded timeout for absent confirmation if necessary; do not retry the command automatically or fabricate success. Display unavailable/unknown for missing modules or fields. Prefer existing module/field events over recurring `Modules.Get`/`setInterval` polling.

## Readable widget folders

For a new set, Gemini proposes a short descriptive `folder` on its first `save_widget`, for example `cyberpunk`, and a leaf `name`, for example `clock`. HG assigns the directory and returns the confirmed ID (`cyberpunk/clock`), module URL and Web Component tag. Dependent widgets must use those returned paths. If a name is occupied, HG assigns `cyberpunk-2`, then `cyberpunk-3`, without adopting the existing directory or overwriting its files. Names are lowercase ASCII slugs; directory proposals are limited to 48 characters.

The assigned `widgetPrefix` is persisted in the task's chat metadata before any widget source is saved. A small `.ai-goal` ownership file in that directory associates it with the internal root objective ID. Revisions inherit the prefix, including chained revisions; changing the objective title does not rename files. Missing or mismatched ownership stops a revision. Folder allocation is serialized inside the service and uses an exclusive ownership-file create. Ordinary pre-save checks continue protecting manually changed or unrelated widget files; this is not a filesystem transaction against arbitrary external writers.

Existing objectives without this metadata retain their original `goals/g<id>/...` paths and imports. There is no automatic migration or renaming. Clearing history retains saved widget directories, so future objectives receive another available name. A failed or cancelled objective can leave its reserved directory; it is not silently reused for a different objective. This change needs updated services 577 and 583 together and a YoT build whose task parser recognizes `widgetPrefix`. The frontend validates artifact paths against that persisted folder while retaining the legacy GUID fallback. Live acceptance: create a small named set, revise it through Edit and confirm stable paths, then create a separate set with the same proposed folder and confirm a suffixed name.

Automated verification: 91 isolated backend tests against the shipped service sources and 214 frontend regression tests passed. Vulkan services were installed with the standard programs-only updater; source readback, enabled state, runtime configuration, conversation contents and zero pending program updates were verified. The live Gemini creation/revision/collision test remains pending.

## Prompt cancellation and active objectives

Gemini declares `chat.cancelVersion: 1`. YoT sends ordinary prompts as `{requestId, prompt}` and uses provider-scoped `Prompt.Cancel` / `Prompt.Status` with the same identity. The provider owns the complete turn until it exits, including context preparation, generation, action coordination and final history persistence. A cancelled native token reaches the provider HTTP request and is checked before processing its output. A late completed generation cannot become an executable action or a completed chat reply. Final history commit and Stop use the same lifetime gate: if completion has already committed, a late Stop reports completion rather than falsely reporting cancellation. Existing effects are not rolled back merely because Stop was pressed; already-dispatched actions can finish.

Stop shows the existing localized interruption-requested status and keeps new sends blocked until the original response or status confirms completion/cancellation. Failed or unrecognized cancellation receipts keep the request pending and allow Stop to be retried. Transport failure of Submit retains the correlation so the user can still stop/check the server work. Stale callbacks cannot overwrite a subsequent turn or a different selected provider. Presentation feedback uses a deterministic identity from its original request and event IDs. Recent terminal receipts are bounded; Stop received before Submit retains a separate fence so a delayed request cannot start afterward.

While a persisted objective is pending/running/waiting (including cancellation requested), YoT keeps the text draft editable but disables sending, voice submission, new objectives and presentation feedback on that Genie. Task Stop remains in its card. The Goal.Start receipt blocks sends immediately, and normal conversation synchronization updates other browsers and clears the guard after terminal acknowledgement. Gemini also checks active tasks on the backend and rejects competing ordinary turns; its Goal.Start handshake shares prompt ownership to prevent overlap with a still-running ordinary request. A restored/imported read-only task does not lock the chat.

Automated cancellation checks use the shipped lifetime, native generation boundary and conversation coordinator with local fake providers. Live acceptance remains: Stop an ordinary Gemini request, wait for acknowledgement and submit again; keep a draft during a goal and confirm Enter/send remain blocked until it ends or acknowledges Stop.

Verification: 120 isolated backend tests and 225 frontend regressions passed, together with Angular application type/template compilation. Vulkan services 583/933 were updated through the standard programs-only updater; source readback, history/configuration preservation and zero pending package updates were confirmed. A real HTTP Stop-before-Submit/Status probe confirmed the cancellation fence without invoking Gemini or changing chat history. Live cancellation during a Gemini response and the interrupted revision test remain for maintainer acceptance.

Maintainer acceptance: stopping an ordinary Gemini turn was confirmed in live history (`outcome: cancelled`, zero executed actions). Its persisted empty result now renders the existing localized cancellation status in YoT, retaining the record and message identity. Partial text, if present, remains visible. The frontend cancellation display passed 228 selected regressions and Angular template/type compilation; live retry and goal-time send exclusion remain pending.


The first readable-path revision was confirmed in history (runner 1046, same `prova-pannello` IDs), but its generated horizontal layout used the wrong flex alignment and collapsed both child slots. The saved HTML was corrected from `row center-stretch` to `row stretch-center`; widget generation guides now explain axis ordering. A browser regression using the actual flex-layout stylesheet reproduces zero-height slots before the correction and visible side-by-side slots afterwards. Explicit goal submission also restores scroll following when sending from an earlier message, while subsequent intentional upward navigation is still respected. These changes passed 231 selected frontend regressions and Angular template/type compilation; visual maintainer recheck is pending.
