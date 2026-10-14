# Persistent and interactive notifications

The required base-package program **582 — Persistent Notifications** listens to `Program.Notification`, archives explicitly persistent messages and exposes `System.Utility/Notifications` APIs. Existing `Program.Notify` calls and transient notifications retain their behavior. No HomeGenie core changes are required.

## Publish a notification

Pass a version-1 JSON message through the existing title/message overload. Add `#using Newtonsoft.Json` to the program context when needed.

```csharp
Program.Notify("Maintenance", JsonConvert.SerializeObject(new {
    Version = 1,
    Persistent = true,
    Id = "maintenance-ready",
    Title = "Maintenance",
    Message = "The maintenance task is ready.",
    Severity = "info",
    Actions = new [] {
        new {
            Id = "run",
            Label = "Run maintenance",
            Type = "api",
            Method = "POST",
            Endpoint = "System.Utility/MyService/Maintenance.Run",
            Body = new { },
            Confirmation = "Run this maintenance task?",
            Idempotent = true
        }
    }
}));
```

The archive recognizes both the existing `{Title, Message}` wrapper with structured JSON in `Message`, and direct version-1 payloads emitted with `Program.Emit("Program.Notification", json)`. Only explicit `Persistent=true` messages are archived. `Id` is scoped to the event's actual sender domain/address; a producer cannot supply a different sender identity in the payload.

Supported metadata includes `TitleKey`, `MessageKey`, `Details`, and action `LabelKey`/`ConfirmationKey`. Translation keys use the frontend language catalog, with supplied text as fallback. Message text supports the existing sanitized Markdown rendering. Severity is `info`, `success`, `warning` or `error`.

The initial contract supports at most four actions, each with a unique ID, a label, a local API endpoint, POST method and optional object body. Absolute URLs, traversal paths and executable UI code are not supported. The payload limit is 64K characters. Use `Idempotent=true` only when the target handler safely supports repeated requests.

## Archive and UI behavior

Records are kept in `data/programs/582/notifications.json`, with flushed temporary-file writes, atomic replacement and a previous-file backup. Default limits are 500 records and 90 days; archived records are preferred when removing excess items. Retention is checked on incoming messages and list requests; records with running actions are protected, so the count limit may temporarily be exceeded while they complete. Startup recovery uses the module parameter timestamp and does not renew already collected messages merely because the service restarts, or resurrect expired ones. Change `MaximumItems` or `RetentionDays` in program options and restart the service to apply them.

- Repeating the same sender/ID and content preserves read and archive status.
- Changed content reopens the record as unread and increments its content revision.
- Sender identity distinguishes records with otherwise identical IDs.
- A monotonic sequence protects the frontend from delayed list/event responses.
- Read and archived flags are shared across browsers. Reading does not remove a persistent record; archiving hides it from the active list and can be reversed.
- The toolbar badge counts only active unread messages. The reader remains accessible when that count is zero.
- On connection and when opening the reader, YoT retrieves the archive. Live `Notification.ArchiveChanged` events update existing records.

The service must be running to collect every event. At startup it also recovers each module's latest persistent `Program.Notification` value; this is recovery of the last value, not reconstruction of all notifications emitted before the archive was installed or while it was disabled.

## APIs

| API | Request / result |
| --- | --- |
| `GET /api/System.Utility/Notifications/List` | All retained records, including archived ones |
| `POST /api/System.Utility/Notifications/Get` | `{key}`; one record |
| `POST /api/System.Utility/Notifications/Read` | `{key, value:true}`; updated record |
| `POST /api/System.Utility/Notifications/Archive` | `{key, value:true}`; use false to restore |
| `POST /api/System.Utility/Notifications/Execute` | `{key, revision, actionId}`; updated record or explicit error |

The UI sends only a stored record/action identity and revision. The backend loads the stored action and body; the client cannot substitute an endpoint. Execution requires a click, and optional confirmation is handled by the existing confirmation dialog. Archived records, stale revisions and unavailable/disabled sender programs are rejected.

Actions have `running`, `completed`, `failed` or `unknown` state. Completed actions are not dispatched again. A restart converts interrupted running actions to unknown without replaying them. Failed/unknown actions can be retried explicitly only when the producer declares them idempotent. Both action results and errors are retained. A separate per-record ledger keeps the last 32 action runs, including results from earlier action definitions. Only one action can run per notification at a time; rejected duplicate requests do not change the running outcome. Large response bodies are replaced with a bounded summary (16K-character threshold). An endpoint returning no result produces an unknown outcome rather than a false success. Unknown effects require inspection rather than automatic retry.

## Gemini Genie transition

When configured, Gemini Genie publishes one stable persistent notice if installed previous Gemini Genies remain enabled. Its button lists those programs and asks for confirmation. `AI.Genie/Gemini/Legacy.Status` reports their state; `Legacy.Disable` accepts only IDs 930, 931 and 932 whose package identities match the original Genies. It verifies results, reports partial failures and supports idempotent retry.

Disabling is optional and reversible. No program, prompt, credentials, module configuration or chat history is deleted or imported. A report is emitted as `AI.Genie.LegacyMigration` on the new Genie module. Existing package installation already inherits a program's enabled state, so a later update does not intentionally re-enable retired programs. There is no removal endpoint in this version.
