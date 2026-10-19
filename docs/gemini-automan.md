# Gemini Automan

> Historical integration notes: the specialized Genie programs 930, 931 and 932 are no longer shipped in the AI package. Existing installations and their data are preserved. The migration stages and specialized-provider examples below describe that earlier implementation; for the current providers, see the [shared Genie engine](guides/shared-genie-engine.md).

Gemini Automan can answer questions, create automation programs, and update existing programs. Informational questions do not automatically deploy code. If the requested action or target program is ambiguous, Automan should ask for clarification.

## Response actions

The Gemini response schema requires all fields (`action`, `explanation`, `program_id`, `name`, `language`, `setup`, `main`, `context`) to produce a stable JSON structure. The `action` determines whether code is deployed:

- `reply`: save and display the explanation as a normal chat message; program fields are empty strings and `language` is `csharp`. No program is created or modified.
- `create`: require `name`, `setup`, non-empty `main`, and a supported `language` (`csharp`, `python`, or `javascript`). Optional `context` contains shared declarations. A successful deployment returns `automan_deploy` metadata with `action: "create"` and the new `program_id`.
- `update`: require the same complete code fields and an exact `program_id` from the current inventory. Only programs in the `Automan` group, excluding Automan itself, are eligible. A successful deployment returns `action: "update"` and the existing ID.

Each request receives a fresh inventory of eligible programs, including their IDs, names, code and enabled state, even when the static Gemini context is cached. Compilation retries keep the same action and program ID.

## Updates and recovery

Updates preserve the original program data and metadata. Previously disabled programs remain disabled; enabled programs are enabled again after successful compilation. The original record is saved in `automan-backups` inside the Automan program data folder before changing its code.

If compilation or deployment fails, Automan attempts to restore and recompile the original program. An unsuccessful recovery is reported in the chat, and the saved JSON record remains available for manual recovery. Errors and informational replies are also persisted in chat history.

The chat labels new deployments as **Program created** or **Program updated**. Older deployment messages remain compatible. Historical explanations remain attached to their original message when the program is subsequently updated.

## Provider responses

Gemini HTTP errors and the 180-second request timeout are reported explicitly. Context caches are invalidated when the system instructions change. Incomplete deployment fields are retried before any program is created or modified. Responses with a completion reason other than `STOP`, or invalid JSON, fail without treating partial code as a successful deployment. Logs record the completion reason and response length without exposing the API key or generated source.
