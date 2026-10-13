# AI response extraction and execution

Local AI and Gemini Intent Handler share the parser provided by automation program 940. This first refactoring separates parsing from execution; it does not yet add Magik approval policies or high-level widget actions.

## Endpoints

Use the prefix `/api/HomeAutomation.HomeGenie.Automation/940/`:

- `Extract`: accepts response text and returns the interleaved text and complete `api-call` segments. It never executes commands.
- `Execute`: accepts response text, extracts executable commands and returns an ordered list of results with `Index`, `Command`, `Success`, `Response` and `Error`.
- `ExtractAndExecute`: compatibility endpoint that explicitly executes commands and returns the interleaved display segments.

POST UTF-8 plain text is recommended. An encoded response can also be supplied as the first positional option. POST text is not URL-decoded, so literal percent signs and plus signs are preserved.

`Extract` previously executed commands. Custom callers relying on that behavior must explicitly call `Execute` or switch to `ExtractAndExecute`. The built-in Gemini Intent Handler has been updated to call extraction and execution separately. Local AI also uses the two separate phases.

## Parsing rules

Only complete standalone fenced blocks labelled exactly `api-call` contain executable commands. Commands are one per line. Blocks inside `<think>` sections or other fenced code examples are excluded, as are unfinished blocks. Original reasoning and example text remain part of the display response.

Extraction can be repeated without side effects. Each explicit execution request executes its commands again; operation IDs and retry deduplication remain future work. API failures are returned per command, and subsequent commands are still attempted. No new confirmation behavior is introduced by this refactoring.
