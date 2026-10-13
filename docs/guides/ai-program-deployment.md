# Shared program deployment

Program 581 (AI Action Executor 1.2.0) performs program deployment independently of the AI provider. Gemini Automan 1.0.9 delegates filesystem/program changes to this service; the Genie retains model requests and its bounded compiler-repair loop. Public Automan chat responses remain compatible with the existing UI.

## Request

POST `/api/System.Utility/ActionExecutor/Program.Deploy`:

```json
{
  "requestId": "deployment-unique-id",
  "attempt": 1,
  "mode": "create",
  "program_id": "",
  "program": {
    "name": "Example",
    "language": "csharp",
    "description": "Example program",
    "setup": "Program.Run();",
    "main": "Program.GoBackground();",
    "context": ""
  }
}
```

For `update`, supply the existing numeric program ID. Updates are restricted to programs in the `Automan` group. The service reads and preserves the original record, including options/data and its enabled state. New programs belong to that group and are created disabled before compilation.

The service accepts one attempt at a time. After confirmed compiler errors, retry with the same request ID and mode/target, incrementing `attempt` sequentially (maximum five). The assigned program ID remains fixed. The model can correct the complete source bundle; it must not switch to another target. Every attempt fingerprints mode, target and source fields. Repeating the same attempt returns its recorded result without API side effects; changing its payload under the same attempt ID fails.

## Results and recovery

Results use `ApiCommandExecution`: `ActionId`, `RequestId`, `State`, `Success`, `Response`, `Error`, `DurationMs`, `CompletedAt`, and `Replayed`. A compilation failure has `State=failed` and a Response containing `status=compile_failed`, `program_id`, and `compiler_errors`. Successful compilation returns `status=success`; new programs and previously enabled updates are enabled, while previously disabled updates remain disabled.

Enabling a program is not a readiness guarantee. Its Setup controls execution; a combined program/widget workflow must separately verify the emitted modules/properties before declaring the widget ready.

POST `Program.Recover` with `{ "requestId": "deployment-unique-id" }` after known failed attempts. An update recompiles the original record and restores its enabled state. A failed new creation is retained disabled for inspection, matching Automan's previous behavior; it is not deleted. Recovery cannot automatically mutate a completed or uncertain deployment.

Durable outcome reservations are written before mutations. Exceptions, invalid mutation responses or interrupted writes produce `unknown` outcomes, which block automatic continuation/recovery. Use the existing `Results` endpoint to inspect the request. Program action IDs use `requestId:deploy-N` and `requestId:recover`; API-call IDs keep their existing format.

Session files, including original program backups, live in `data/programs/581/program-deployments/`. The existing `action-results.json` stores action outcomes. Both files are required for diagnosis/recovery and must not be silently reset when unreadable.

## Structured dispatch

`Execute` also accepts a single action with type `program-deploy` and the same mode/target/program/attempt fields, or `program-recover`. The envelope supplies `requestId`. Mixed batches of deployment and other actions are not yet supported; project orchestration and linked widget previews are the next milestone. Legacy and structured `api-call` execution remain compatible.

## Validation

Tests cover creation, replay after restart, payload collisions, compiler repair without duplicate programs, metadata preservation, update recovery, wrapped compiler responses, disabled updates, ownership restrictions, uncertain outcomes, failed activation, typed dispatch, and the existing API action ledger behavior.
