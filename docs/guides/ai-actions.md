# AI Action Executor

AI Action Executor (program 581, Utilities) is a required base service. Gemini Intent Handler and Local AI use it independently of model/provider lifecycles. This version adds stable action identities and recorded API outcomes; approval policies and program/widget actions remain future work.

## Endpoints

Prefix: `/api/System.Utility/ActionExecutor/`.

- `Extract`: raw UTF-8 response text to interleaved display segments; no execution.
- `Execute`: raw legacy text, or a JSON request envelope, to ordered execution results.
- `Results`: request ID, or `{ "requestId": "..." }`, to recorded results; no execution.
- `ExtractAndExecute`: legacy combined path; executes and returns display segments.

The `/api/HomeAutomation.HomeGenie.Automation/940/{Extract,Execute,ExtractAndExecute}` endpoints remain delegates while Local AI runs. Use service endpoints when Local AI may be disabled.

## Stable requests

```json
{
  "requestId": "chat-request-123",
  "response": "```api-call\nHomeAutomation.PhilipsHue/3/Control.Level/25\n```"
}
```

Each command receives `ActionId = requestId + ":" + index`. Request IDs allow letters, digits, underscores and hyphens, up to 128 characters. Gemini and Lailama supply the chat request ID. Reusing an action ID returns the recorded outcome with `Replayed: true`; it does not call the API again. Reusing that ID for another command returns a failure without replacing the original record. A new user request receives a new ID, so repeated commands remain legitimate.

Raw-text legacy calls get a fresh request ID per invocation and intentionally execute again. To make transport retries safe, send the same JSON request envelope and ID. Do not hash command text as identity: two deliberate requests for the same command are different actions.

Structured API proposals are also accepted:

```json
{
  "requestId": "chat-request-124",
  "actions": [{
    "type": "api-call",
    "target": { "domain": "HomeAutomation.PhilipsHue", "address": "3" },
    "command": "Control.Level",
    "arguments": ["25"]
  }]
}
```

This accepts adapter-generated actions without requiring small models to emit structured JSON. Arguments are strings; slash/newline characters are rejected in structured arguments. Existing raw API blocks retain their positional path syntax.

## Outcomes and persistence

Results retain `Index`, `Command`, `Success`, `Response`, `Error` and add `ActionId`, `RequestId`, `State`, `DurationMs`, `CompletedAt`, `Replayed`.

- `completed`: the API returned a non-error response.
- `failed`: validation rejected the action or the API explicitly reported an error.
- `unknown`: an exception, missing response or interrupted execution prevents confirming the outcome. No automatic retry is performed.

An API acknowledgement does not prove a physical device reached its requested state. Device state remains separately observable.

`data/programs/581/action-results.json` records an unknown reservation before dispatch, then the result. A process restart preserves reservations and completed outcomes. If the ledger is unreadable or cannot be reserved, execution is refused. If the final result cannot be persisted, the reserved unknown state prevents redispatch. This does not claim exactly-once physical effects or protection from loss of the underlying storage. Do not remove the ledger while retrying old IDs. Retention/cleanup policy remains future work.

The chat stores results in `metadata.actions`, outside provider message content. Its Comandi API panel shows an outcome icon with duration, ID and error details. Legacy messages without recorded outcomes do not show a successful status by inference.

Only complete standalone `api-call` fences execute. Reasoning, code examples and incomplete fences are excluded. Commands retain order, including repeated commands at different positions. Subsequent commands continue after a per-command failed/unknown outcome.

Install AI Action Executor 1.1.0 from homegenie-base 1.0.9 before Gemini Intent Handler 1.0.21 / Local AI 1.0.16 from homegenie-ml-ai 1.0.14. Explicit execution still follows existing immediate device-command behavior.
