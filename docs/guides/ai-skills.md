# Shared AI skills and prompt composition

> Historical integration notes: the specialized Genie programs 930, 931 and 932 are no longer shipped in the AI package. Existing installations and their data are preserved. The migration stages and specialized-provider examples below describe that earlier implementation; for the current providers, see the [shared Genie engine](shared-genie-engine.md).

HomeGenie uses the [Agent Skills format](https://agentskills.io/specification): a directory containing `SKILL.md` with YAML frontmatter and Markdown instructions. Shared skills live in `data/ai/skills/`. Optional reference documents belong in a skill's `references/` directory.

The first two migration milestones are implemented. Intent Handler (930), Automan (932) and Widget Genie (931) load their default prompts through the shared composer. `homegenie-core` provides common conversation/correctness rules; its `references/automation.md` contains the shared programming rules used by Automan and Widget Genie. Each provider retains its own role, response schema, tools and execution path. Do not combine all three role skills as a unified Genie yet: action-contract unification is the next milestone.

Default `data/programs/<id>/gemini_system_prompt.txt` files contain a single `#use-skill` directive. Existing custom full-text prompts remain supported unchanged; the provider bypasses composition for a plain-text file. Missing files or broken directive composition return an error rather than falling back to generic instructions. Provider updates do not overwrite user-edited prompt files automatically.
## Catalog

`GET /api/System.Utility/ContextEngine/Skills.List`

Returns `version`, `skills` and `diagnostics`. Entries include name, description, compatibility, metadata, the declared handler and whether that binding is registered. Invalid files are omitted from entries and reported in diagnostics. Missing directories produce an empty catalog. The catalog contains instructions, not system inventories or credentials.

`metadata.homegenie-handler` is a HomeGenie extension. Bindings are explicitly registered in backend code; a new skill file cannot register an executor, grant permissions or execute scripts. This first milestone does not change executor authorization. The standard experimental `allowed-tools` field is not used to grant permissions.

## Composition

`POST /api/System.Utility/ContextEngine/Prompt.Compose`

```json
{
  "version": 1,
  "template": "#use-skill homegenie-widget"
}
```

Alternatively, use a base template plus explicit selections:

```json
{
  "version": 1,
  "template": "You are a HomeGenie assistant.",
  "skills": ["homegenie-widget"]
}
```

Returns `version`, the composed `prompt`, selected `skills`, included `resources` and a SHA-256 `fingerprint` of the prompt and composition metadata. The fingerprint is a content identity for cache invalidation, not an authorization token. Composition does not call an AI, collect device information, alter preferences, or execute actions. Clients must check for `{ "Status": "Error", "Message": "..." }` before using a result.

## HomeGenie inclusion directives

These directives are HomeGenie preprocessing extensions, not part of the Agent Skills specification:

```text
#use-skill homegenie-widget
#use-kb homegenie-widget/references/zuix-api.md
```

Directives occupy a whole line and are expanded in place. Skill metadata is parsed separately and is not inserted into the prompt body. Reference paths are relative to the skill catalog root and must identify Markdown files under a skill's `references/` directory. Directives within fenced examples remain literal. Ordinary Markdown links remain links; composition does not automatically fetch them.

Nested directives are supported. Repeated inclusions are omitted deterministically; cycles, missing files and malformed directives fail composition. Absolute paths, traversal, linked files/directories and invalid skill names are rejected. Limits: 64 inclusions and 512 KiB per file/template/result. Files use UTF-8 and prompts use LF line endings. The composer reads current files on every request; no stale in-memory prompt cache is maintained.

## Provider diagnostics and cache behavior

Read-only `Prompt.Preview` endpoints are available under each provider:

- `/api/AI.IntentHandlers/Gemini/Prompt.Preview`
- `/api/AI.Automan/Gemini/Prompt.Preview`
- `/api/AI.WidgetGenie/Gemini/Prompt.Preview`

These report the composed skill/resource list, fingerprint and prompt byte count without returning the prompt body, calling Gemini, modifying history or executing actions. Widget Genie's existing `Context.Preview` also uses the composed prompt while enforcing selected information categories.

Automan and Widget Genie's legacy deep-knowledge caches are matched against the full assembled context hash, so edited skills/references cannot reuse a stale prompt. Widget Genie's explicit context-profile requests continue bypassing these legacy caches. Changing instructions does not reset visible history or convert messages.

For an existing installation, deploy `data/ai/skills` along with the provider updates. To adopt the shipped prompt in a customized installation, back up the current prompt and replace it explicitly with the appropriate `#use-skill` line. Plain-text prompts continue working without migration. The test runtime is migrated only when its prompt matches the known original; customizations are preserved.

## Migration and chat compatibility

1. Validate catalog/composition and preserve the existing providers.
2. Completed: shared rules extracted and provider prompt loading wired to composition; role-specific response contracts remain separate.
3. Unify internal actions while adapting function calls, structured output and existing markers.
4. Introduce the unified Genie and UI skill selection independently from information-sharing choices.
5. Normalize legacy chat exports during import into one versioned internal format. Preserve content and optional metadata; importing/rendering must never execute actions. Keep visual history separate from the context sent to an AI.

No chat conversion occurs in these prompt-migration milestones. Current import/export behavior and stored histories remain unchanged. Resource discovery, dynamic model-driven skill loading, executable bundled scripts and an MCP adapter are future work.

Shared assets are included in fresh-install build data. Updating the Context Engine program alone does not install `data/ai/skills`: deployment of shared assets must accompany provider migration. User-edited prompts are not replaced automatically.

## Scripting API references

The shared `homegenie-core/references/automation.md` includes the essential lifecycle and type rules, including `Modules.Each` (`false` continues, `true` stops), `ModuleHelper.Instance`, optional parameters and real versus demonstration data. It is still loaded for automation requests by the shared Genie engine.

Detailed API references are generated from Doxygen XML into `homegenie-core/references/api-*.md`, with an `api-index.md` catalog. They preserve public signatures, overloads, default arguments and inheritance for the selected scripting helpers and data types. Constructors and implementation-only classes are excluded. Source examples are reviewed separately rather than copied into the generated catalog. This is the automation programming API, not a complete device API inventory.

Load only a relevant topic by adding a whole-line directive to a provider prompt, for example:

```text
#use-kb homegenie-core/references/api-modules.md
```

The catalog is not included in every conversation. Markdown links do not load files, and automatic model-driven retrieval remains future work. Device-specific operations still require discovery from the installation. Deploy the shared assets to make these changes available; the files do not update an already installed backend automatically.

From the repository root, regenerate the API website and references with `assets/deploy/linux/doxygen.sh` (Doxygen and Python 3 required). To generate only references from existing XML:

```sh
python3 assets/deploy/linux/Doxy/generate-ai-api-reference.py --xml-dir assets/deploy/linux/Doxy/homegenie_api/xml --output-dir assets/build/all/data/ai/skills/homegenie-core/references
```

Add `--check` to validate that references match the XML without writing files. Refresh Doxygen first when changing signatures or documentation. Generator tests: `python3 assets/deploy/linux/Doxy/test_generate_ai_api_reference.py`.
