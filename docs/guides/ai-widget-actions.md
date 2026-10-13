# Widget actions from AI chat

Design for the next AI Action Executor milestone. The action described below is not implemented yet. Program 581 supports `api-call`, `program-deploy`, and `program-recover`; `widget-install` remains a later milestone.

## Existing behavior

Chat previews are in-memory CustomWidget instances. Their module binding and size are temporary. In an open widget editor, Update emits `onWidgetUpdated` and changes the editor draft; it does not write the widget files. Save As outside that editor calls `Widgets.Add`, then separate `Widgets.Save` calls for HTML, CSS and JavaScript.

Keep draft editing separate from backend installation. Installing a widget does not add it to a dashboard. A future dashboard action can use the preview binding and layout as defaults, with an explicit destination.

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
