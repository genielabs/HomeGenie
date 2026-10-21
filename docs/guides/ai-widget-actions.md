# Widget actions from AI chat

Design for the next AI Action Executor milestone. The action described below is not implemented yet. Program 581 supports `api-call`, `program-deploy`, and `program-recover`; `widget-install` remains a later milestone.

## Existing behavior

Chat previews are in-memory CustomWidget instances. Their module binding and size are temporary. In an open widget editor, Update emits `onWidgetUpdated` and changes the editor draft; it does not write the widget files. Save As outside that editor calls `Widgets.Add`, then separate `Widgets.Save` calls for HTML, CSS and JavaScript.

Keep draft editing separate from backend installation. Installing a widget does not add it to a dashboard. A future dashboard action can use the preview binding and layout as defaults, with an explicit destination.

## Conversational presentations

The canonical widget response supports optional `widget.mode: "presentation"`. The same field can accompany a composite widget whose backing program was requested. Omitting mode preserves the existing preview/save/editor behavior.

In presentation mode, the chat renders the optional explanation and the temporary widget, without Markdown sources, the Actions/Save panel, the standard widget menu or automatic updates to an open editor. Complete presentation sources are retained in a versioned `ai_widget` history envelope; composites retain their existing `ai_solution` envelope with the widget mode. Reopening or importing history restores the display mode. Only final messages instantiate presentations; partial control blocks and envelopes are not rendered as source text.

The AI may choose a presentation for an illustrated answer, chart, narrative scene, animation or local choices. Standalone presentations require neither a module nor a deployed program. Responsive free sizing and the existing viewport/page pause/resume lifecycle apply. Reload reconstructs the controller; arbitrary in-memory state is not persisted by this mode.

Presentation widgets can send a user choice back to their original AI conversation:

```javascript
// Call from a user gesture, never create/resume/refresh. Keep eventId stable for this logical choice.
this.apiCall('System.Utility/AiGenie/Chat.Feedback', {
  eventId: 'portal-choice-1', event: 'choice-selected',
  data: {scene: 'portal', choice: 'roots', label: 'Follow the roots'}
}).subscribe({next: res => {
  const receipt = res.response;
  // Disable this choice after Status === 'Ok'; the chat displays the assistant continuation.
  // On busy, let the user try later. On unknown, inspect history before any retry.
}, error: error => { /* Unconfirmed outcome: inspect the original conversation. */ }});
```

The host supplies adapter/provider/source-request correlation; do not supply or invent those IDs. Only completed presentation messages with request metadata in a feedback-capable provider support this route. `eventId` (1–128 characters) and `event` (1–80) use ASCII letters, digits, `_ . : -`; `data` is a JSON object and the complete request must fit within 16 KiB. The event is reference data representing a user selection, never a new policy or system instruction. Existing context/capability settings apply; a changed context profile requires a new presentation.

This continues the original provider conversation and saves only the assistant reply, with the event in its metadata. No USER row is added. Future model context includes the selection. A completed event survives history reload and repeating the same eventId/data returns its saved result; reusing an ID with different data is rejected. After an unconfirmed result, never retry effects automatically. Recovery guards for unconfirmed outcomes are in memory and do not survive a provider/shared-service restart. Deleted presentations and cleared history cannot be continued. Local-only animations/counters need no feedback call. Dispose subscriptions and listeners with the normal widget lifecycle.


## Proposed action

```json
{
  "requestId": "widget-install-unique-confirmation-id",
  "actions": [{
    "type": "widget-install",
    "target": { "componentId": "custom/tank-level" },
    "mode": "create",
    "sources": {
      "html": "<div class=\"container\">Tank level</div>",
      "css": ":host { display: block; }",
      "js": "class TankLevel extends ControllerInstance { settings = { defaultSize: 'small' }; onCreate() {} onDispose() {} }"
    }
  }]
}
```

The example illustrates the envelope, not a complete recommended widget implementation. The client creates this action from the generated sources and the path confirmed by the user. The AI does not authorize its own installation.

- `create`: reject an existing target without changing files.
- `replace`: explicitly confirmed replacement of an existing target.
- Editor draft updates continue through `onWidgetUpdated`. Existing editor auto-update behavior remains a draft operation.

Validate the complete proposal before writing: supported mode, relative component path, supported source names and string values, nonempty HTML, and configured payload limits. Permit the path naming rules already used by YoT; reject absolute paths, traversal, reserved template files and resolved paths outside the widget directory. Missing source entries must have explicit semantics; a full installation bundle should distinguish empty CSS/JS from omitted updates.

## Execution and recovery

Use the executor's stable request/action identities and durable outcome records. A transport retry uses the original immutable request ID and payload. Another deliberate installation receives a new ID. Reusing an ID with different type, target, mode or sources must fail; comparing only the target path is insufficient.

Delegate filesystem work to the HomeGenie widget handler. Replace the current independent source saves with one bundle operation, including generated module metadata. Validate first and prepare staging/backup files before changing the installed files. Report `failed` only when no change occurred or rollback restored the original bundle; report `unknown` when an interrupted operation or failed rollback prevents a reliable conclusion.

The executor returns the existing outcome fields: action ID, state, success, duration, response/error and replay flag. A timeout must trigger a Results lookup before any retry. Show pending/completed/failed/unknown feedback in the chat and refresh the widget catalog only after confirmed completion.

## Verification

1. Create an unused widget path and verify the complete source bundle and catalog entry.
2. Reject duplicate creation and invalid paths without changing installed files.
3. Confirm replacement and verify all resources, including module metadata.
4. Replay the same request without repeating file writes; reject changed payloads under the same ID.
5. Simulate write failures, timeout and interrupted outcomes; verify rollback and Results lookup.
6. Verify that preview settings and editor draft updates never install widgets or save dashboards.

Dashboard insertion, automation program creation and autonomous Magik execution remain separate later milestones.
