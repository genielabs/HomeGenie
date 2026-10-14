# Gemini Genie pilot

Gemini Genie (program 933, interface `@AI:Genie`, API `AI.Genie/Gemini`) combines device control, automation lifecycle and custom widget/composite generation. The existing Intent Handler, Automan and Widget Genie remain available. Each provider has a separate history store; switching to the new Genie does not move or delete old conversations.

Default instructions use `#use-skill homegenie-genie`, composing common conversation, automation and widget references without concatenating conflicting legacy role prompts. Configure the Gemini API key and model in the program's options. The Context menu is available to the new interface and preserves choices separately per program/provider.

## Common response contract

Version 1 requires `version`, `kind` and `explanation`. A discriminated JSON Schema requires the appropriate artifacts for each kind and excludes unrelated fields. Empty optional provider containers are normalized away; nonempty incompatible artifacts remain errors. Up to two contract-format repairs are allowed before any new action dispatch. Optional fields depend on kind:

| Kind | Fields / behavior |
| --- | --- |
| reply | Explanation only, no artifacts/actions |
| device | `commands`: validated known-module control commands executed once through the shared Action Executor |
| program | `language`, `automation` with create/update, exact update ID and complete sources; shared deployment/recovery |
| widget | Complete HTML/CSS/JavaScript; temporary preview, explicit Save |
| composite | One new program plus widget and exact field binding; preview waits for data |
| context | Program/widget IDs whose sources are needed; read-only lookup, gated by selected information categories |

`POST /api/System.Utility/ActionExecutor/Response.Normalize` validates canonical outputs and adapts legacy Automan/Widget outputs or plain responses with `api-call` markers. It never executes actions. The normalizer and JSON schema are provider independent. Existing legacy execution APIs remain intact.

The Gemini pilot uses structured JSON Schema (`response_json_schema`) with per-kind variants, not native function calls. Only final text parts are parsed; thought parts are excluded and incomplete responses are rejected. It receives a fresh selected device snapshot and current time each request. Up to three bounded source lookups are available within the same request; disabled categories cannot be read. Program updates require reading the exact target sources first, and the shared executor currently limits updates to managed Automan-group programs. Compilation repair retains kind/action/ID and composite data contracts. Unknown execution outcomes are not retried automatically. Requests are serialized per Genie.

Only documented control commands, thermostat settings, security commands and existing program scene run/break operations are supported by the device branch; arbitrary configuration/deletion APIs are rejected. Full batches and target existence are validated before dispatch. The action ledger prevents replay under the same request identity. Device responses contain display-only `api-call` blocks and durable `metadata.actions` results. The backend executes commands; the UI renders completion checks and reusable C#/JavaScript/Python/HTTP snippets. Loading history never executes commands.

The coordinator adapts confirmed results to existing UI cards/previews. This is transport/rendering compatibility, not chat-history conversion. Import normalization into a single current history format is a later milestone. No widget is installed automatically on a dashboard: Save remains the installation path.

## Verification APIs

- `GET /api/AI.Genie/Gemini/Prompt.Preview`: instruction skills/resources/fingerprint; no model call.
- `POST /api/AI.Genie/Gemini/Context.Preview`: selected information categories and assembled prompt size; no model call.
- `POST /api/AI.Genie/Gemini/Prompt.Submit`: text or the existing version-1 context/message envelope.
- Existing History.Get/Clear/RemoveAt/Export/Import APIs use this program's own store.

Deploy the shared skills and prompt file with the package update. Existing installations receive an optional persistent notification with a confirmed action to disable previous Gemini Genies. Configuration and histories remain in place; the pilot does not remove or overwrite them. See [Persistent notifications](notifications.md). Broader multi-action plans are future work. Provider adapters for OpenAI/Claude/local models can reuse the response contract and executors without reusing Gemini HTTP code.

## Selectable capabilities

The `Program.Implements` options expose a `chat` descriptor with `requestVersion`, supported `context` keys and supported `capabilities` keys. The frontend builds menu availability and request payloads from this descriptor, without hard-coded program IDs. The chat has a separate Capabilities menu for device control, automation creation/updates and widgets. Defaults preserve existing behavior: all three enabled. Selections are stored as JSON properties `AI.Chat.Context` and `AI.Chat.Capabilities` on each automation program module and sent with each request. The frontend uses the existing `Modules.ParameterSet` API and module events, so changes are shared between browsers. Existing browser preferences initialize absent properties once; existing backend values take precedence. Context controls information sharing independently. Composite solutions require automation and widgets. The backend filters the response schema and validates the selected capability before executing any action. Conversation profiles include both selections, while visible history is preserved. Disabled capabilities can be proposed by the AI but cannot be enabled by it.

`GET /api/AI.Genie/Gemini/Capabilities.Describe` lists supported capabilities. `POST /api/AI.Genie/Gemini/Prompt.Preview` accepts a `capabilities` object and reports permitted kinds and conditional knowledge resources without calling the model.

## Optional transition from specialized Genies

The new Genie publishes a persistent notice once configured, listing enabled previous Genies. The confirmed Disable previous Genies action preserves their settings, prompts and chat stores. No automatic configuration or history migration is performed. The existing specialized programs continue to work until the user chooses to disable them.

## Unattended orchestration

With Automation enabled, the composed prompt includes `homegenie-orchestration`. A Director can create its helpers using `POST AI.Genie/Gemini/Prompt.Schedule` (string prompt), inspect `POST AI.Genie/Gemini/Task.Status` (string task ID), and use `POST AI.Genie/Gemini/Prompt.Ask` (string text) for plain text completion. Backend calls use `Api.Call` without the `/api/` prefix; HTTP clients use `/api/`.

Schedule returns `{Status:"Ok",taskId,state:"queued"}`. Status exposes queued/running/completed/failed and the deployed programId. Failures are explicit and never automatically replayed. The FIFO accepts at most 32 waiting jobs and uses the same deployment/repair path as the chat. Prompt.Ask does not deploy, load context, or modify chat history; it returns plain text or a Status=Error object. A queued task snapshots selected context and permits automation only. Automation is checked again before execution.

The queue and bounded status records are memory-only and disappear on Genie restart. Directors must use unique helper names, bounded waits and existing-program checks instead of blindly rescheduling. New helper deployment enables it; explicit-run helpers remain idle until the Director invokes Program.Run(name). One-off helpers must return from Main; emitting Completed alone does not stop execution. Explicit-run helpers omit Program.Run() from Setup and are started with Program.Run(name). Program.WithName(name).Run() only sets their Setup trigger flag. Directors use bounded IsRunning polling and validate fresh output files and completion status before starting dependent jobs; Program.WaitFor has no timeout.
