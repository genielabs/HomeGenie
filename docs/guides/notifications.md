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

Legacy plain-text notifications and non-object JSON values are ignored by the persistent decoder, including ordinary strings inside the Program.Notify wrapper. Explicit persistent envelopes still undergo schema validation; startup recovery diagnostics identify the sender domain/address. Archive-file validation and backup recovery remain strict.

Supported metadata includes `TitleKey`, `MessageKey`, `Details`, and action `LabelKey`/`ConfirmationKey`. Translation keys use the frontend language catalog, with supplied text as fallback. Message text supports the existing sanitized Markdown rendering. Severity is `info`, `success`, `warning` or `error`.

The initial contract supports at most four actions, each with a unique ID and a label. API actions use a local endpoint, POST method and optional object body. Absolute URLs, traversal paths and executable UI code are not supported. The payload limit is 64K characters. Use `Idempotent=true` only when the target handler safely supports repeated requests.

## Archive and UI behavior

Records are kept in `data/programs/582/notifications.json`, with flushed temporary-file writes, atomic replacement and a previous-file backup. Default limits are 500 visible records and 90 days; archived records are preferred when removing excess items. Retention is checked on incoming messages and list requests; records with running actions are protected, so the count limit may temporarily be exceeded while they complete. Startup recovery uses the module parameter timestamp and does not renew already collected messages merely because the service restarts, or resurrect expired ones. Change `MaximumItems` or `RetentionDays` in program options and restart the service to apply them.

- Repeating the same sender/ID and content preserves read and archive status.
- Changed content reopens the record as unread and increments its content revision.
- Sender identity distinguishes records with otherwise identical IDs.
- A monotonic sequence protects the frontend from delayed list/event responses.
- Read and archived flags are shared across browsers. Reading does not remove a persistent record; archiving hides it from the active list and can be reversed.
- The toolbar badge counts only active unread messages. The reader remains accessible when that count is zero.
- On connection and when opening the reader, YoT retrieves the archive. Live `Notification.ArchiveChanged` events update existing records.

The service must be running to collect every event. At startup it also recovers each module's latest persistent `Program.Notification` value; this is recovery of the last value, not reconstruction of all notifications emitted before the archive was installed or while it was disabled.

## Manage the list

The reader header has Active and Archive icons, with counts and tooltips. Choose **Manage** to show a checkbox beside each notification. **Toggle All** selects or deselects the manageable notifications in the current category. The footer offers **Archive** for active notifications, **Restore** for archived ones, and **Delete** with confirmation. Notifications with running actions cannot be archived, restored or deleted.

Deleting a notification does not delete its program, update job, update history or backup files. The message, details and action results are removed from the archive. A small identity and content-fingerprint marker is retained for the configured retention period so startup recovery and repeated checks do not restore the same message. New content from that producer can create a notification again. Deletion markers are excluded from List and the visible-record limit, and expire during the normal retention cleanup.

Bulk requests are validated and saved together per backend. A failed batch leaves that backend's notifications unchanged; failures on one backend do not hide the outcome on another. Live deletion events remove the notification in other connected clients, with sequence checks protecting against older events.

## APIs

| API | Request / result |
| --- | --- |
| `GET /api/System.Utility/Notifications/List` | All retained records, including archived ones |
| `POST /api/System.Utility/Notifications/Get` | `{key}`; one record |
| `POST /api/System.Utility/Notifications/Read` | `{key, value:true}`; updated record |
| `POST /api/System.Utility/Notifications/Archive` | `{key, value:true}`; use false to restore. Bulk: `{keys:[...], value:true/false}` |
| `POST /api/System.Utility/Notifications/Delete` | `{key}` or `{keys:[...]}`; delete selected notifications |
| `POST /api/System.Utility/Notifications/Execute` | `{key, revision, actionId}`; updated record or explicit error |

Bulk Read, Archive and Delete requests accept between one and 500 distinct keys and return `{Status:"Ok", items:[...]}`. Single-key requests return the updated record; Delete returns its deletion marker.

The UI sends only a stored record/action identity and revision. The backend loads the stored action and body; the client cannot substitute an endpoint. Execution requires a click, and optional confirmation is handled by the existing confirmation dialog. Archived records, stale revisions and unavailable/disabled sender programs are rejected.

Actions have `running`, `completed`, `failed` or `unknown` state. Completed actions are not dispatched again. A restart converts interrupted running actions to unknown without replaying them. Failed/unknown actions can be retried explicitly only when the producer declares them idempotent. Both action results and errors are retained. A separate per-record ledger keeps the last 32 action runs, including results from earlier action definitions. Only one action can run per notification at a time; rejected duplicate requests do not change the running outcome. Large response bodies are replaced with a bounded summary (16K-character threshold). An endpoint returning no result produces an unknown outcome rather than a false success. Unknown effects require inspection rather than automatic retry.

## Gemini Genie transition

When configured, Gemini Genie publishes one stable persistent notice if installed previous Gemini Genies remain enabled. Its button lists those programs and asks for confirmation. `AI.Genies/Gemini/Legacy.Status` reports their state; `Legacy.Disable` accepts only IDs 930, 931 and 932 whose package identities match the original Genies. It verifies results, reports partial failures and supports idempotent retry.

Programs 930, 931 and 932 are no longer included in the distributed AI package. Removing them from the package catalog does not uninstall or disable existing copies, so the optional retirement notice remains relevant to existing installations.

Disabling is optional and reversible. No program, prompt, credentials, module configuration or chat history is deleted or imported. A report is emitted as `AI.Genie.LegacyMigration` on the new Genie module. Existing package installation already inherits a program's enabled state, so a later update does not intentionally re-enable retired programs. There is no removal endpoint in this version.

## Navigation actions and program updates

A navigation action uses `Type: "navigate"`, `Destination: "program-updates"`, plus `Id`, `Label` and optional `LabelKey`. This is the only allowed destination; arbitrary routes, URLs, endpoints and bodies are rejected. YoT opens the originating connection’s Programs update view and closes the reader only after successful navigation. Navigation is repeatable, works from archived notices too, and does not enter the API execution ledger. The backend Execute endpoint explicitly rejects navigation actions.

The archive service now maintains its own update notice by polling the shared read-only Packages.Updates report. See [program updates](program-updates.md) for timing, deduplication, failure and automatic resolution behavior. No program is installed, enabled or removed by this notice.

## Program update job outcomes

Update All produces a persistent notice per job, alongside the separate availability notice. Each progress envelope carries an increasing `Sequence`; the archive ignores an older or equal sequence for the same sender and notification ID, preserving the newer result and its read/archive state. The archive polls `Packages.UpdateNotice` independently of the availability check to recover missed results, including when the archive program itself was updated. Details and API semantics are documented in [program updates](program-updates.md).

### Immediate display in YoT

Structured version-1 notifications may opt in to `Presentation: "toast"`. YoT displays a live notification immediately in a multiline snackbar, queued until the user closes it. Existing badge behavior is preserved. With `Persistent: true` and an `Id`, the notice is also archived by program 582. History reloads, read/archive operations and duplicate revisions do not replay the popup. Clients connected after the event can still find the unread notice in the archive. Omitted or unknown presentation values retain the usual badge behavior.

Lailama uses this presentation after a different model applies YAML option defaults, listing the model and the emitted values. Startup, same-model saves and YAML refresh alone do not produce this notice.
