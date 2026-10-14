# Shared AI context profiles

The shared implementation establishes a provider-neutral context contract in Context Engine 578.
Existing Render/GetStatus behavior is preserved. The new endpoints do not collect or transmit system
information, change provider settings, or enable actions. Widget Genie 931 now consumes this contract through structured requests and the YoT Context menu.
Other providers keep their previous behavior during migration.

- `System.Utility/ContextEngine/Context.Describe`: versioned catalog with descriptions and defaults.
- `System.Utility/ContextEngine/Context.Policy`: validates a JSON profile and returns its normalized
  selections, a stable profile discriminator and system-instruction text for provider integration.

Example Policy payload:

```json
{"version":1,"context":{"devices":true,"widgets":true,"automationPrograms":false,"dashboards":false,"deepKnowledge":false}}
```

Supported option IDs are `devices`, `widgets`, `automationPrograms`, `dashboards`, `deepKnowledge`.
Omitted fields default to devices enabled and the others disabled. Explicit false is preserved.
Unknown fields, non-boolean selections and unsupported contract versions are rejected.
The returned `profileKey` is a profile discriminator, not a complete content cache key: provider caches
must also include relevant content/prompt versions and must not reuse excluded context.

The policy tells the model which categories are selected, without claiming the data was actually
retrieved. It instructs the model not to invent unavailable identifiers, to request the smallest useful
missing context and to distinguish sharing from execution authorization. A `context_request` proposal
has `options` (catalog IDs) and `reason`; it is never an executable action or an authorization grant.
The future renderer will expose explicit activation, without automatically submitting another prompt.

## Next integration steps

1. Migrate Intent Handler, Automan and local providers to the same per-request profile.
2. Add provider retrieval tools for relevant sources, gated by the selected context categories.
3. Expand technical-reference retrieval without automatically including installation sources.
4. Render structured context proposals as explicit activation buttons, without automatic resubmission.
5. Converge the providers into the unified Genie after end-to-end regression tests.

## Widget Genie integration

YoT shows Context after the speech icon for Widget Genie. It remembers selections per Genie and sends
`{version:1,message,context}` for subsequent prompts. Dashboard context is a client projection of
names, widget paths and module associations, supplied only when that option is enabled.

The scoped backend invokes only selected collectors. Devices include the existing module directory
and live status. Widgets and programs initially supply inventories; optional `resources.programIds`
and `resources.widgetIds` request specific sources (up to 16 per category). Program Options/Data
are excluded. Widget paths must be in the inventory and resolve inside the widget data folder.
Individual widget files are limited to 128 KiB and the collected data budget is 256 KiB.
Dashboards are unavailable unless provided by the client. Deep Knowledge currently adds a small
public technical reference, independently of local program/widget sources; broader retrieval is future work.

`AI.WidgetGenie/Gemini/Context.Preview` accepts the same structured envelope and returns profile,
included categories and prompt byte count without sending a request to Gemini or returning raw data.

Each explicit profile uses only completed history pairs carrying the same `contextProfileKey`.
The visible history remains complete. Unknown/unprofiled earlier turns are not sent with an explicit
profile. Returning to a previously used profile restores its eligible model context. Chat History
confirms the requested profile in its response; an older incompatible service blocks the request.
Explicit profiles bypass and clear the legacy Gemini cache reference. Already-uploaded information
is not retracted. Legacy plain-text API clients retain the old EnableDeepKnowledge behavior;
for structured requests the Context profile takes priority over that setting.

Reply-only responses are supported, allowing the model to ask for missing context without generating
artifacts. Proposals are currently plain explanations; structured activation buttons and migration of
Intent/Automan/local providers are the next steps. Sharing choices do not alter deployment authorization.
