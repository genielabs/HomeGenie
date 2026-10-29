# Lailama model profiles

The distributed catalog is `assets/build/all/data/programs/940/models.yaml`. Runners 1028/1029 remain development-only. Runtime catalogs and compiled program 940 must use a compatible schema; do not copy new keys into an older running program before upgrading it.

## Official sampling profiles

| Model | Current mode | DefaultTemperature | TopP | TopK | MinP | PresencePenalty | RepeatPenalty |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Qwen3-4B-Instruct-2507 | Native non-thinking | 0.7 | 0.8 | 20 | 0 | 0 | 1 |
| Qwen3-1.7B | Default thinking-capable ChatML | 0.6 | 0.95 | 20 | 0 | 0 | 1 |
| Qwen3-4B | Default thinking-capable ChatML | 0.6 | 0.95 | 20 | 0 | 0 | 1 |
| Qwen3-30B-A3B-Thinking-2507 | Thinking-only | 0.6 | 0.95 | 20 | 0 | 0 | 1 |
| Qwen3.8-27B | Non-thinking (`qwen35-no-think`) | 0.7 | 0.8 | 20 | 0 | 1.5 | 1 |

References: official Qwen model cards and generation configurations linked beside each YAML entry. Qwen3 repetition penalty 1 and presence penalty 0 leave those penalties neutral; a non-neutral repetition penalty of 1.2 was the historical HG fallback, not an official Qwen3 recommendation. Qwen3.8 explicitly recommends repetition penalty 1 and presence penalty 1.5 for its non-thinking profile. This is configuration alignment, not a claim of improved benchmark results.

## UI defaults versus profile overrides

`DefaultTemperature` is applied through `Program.Emit` when the model selection changes to a different known model. The user can then change it in the normal Lailama options. Restarting the program, saving models.yaml or receiving the same selection again does not reset that value. Loading an existing selected model at startup also preserves its options; switch to a different model and back to apply its defaults explicitly.

Optional `DefaultMaxTokens` accepts 64..4096. Optional `DefaultContextSize` accepts 1024..32768 in steps of 1024. Omitted values leave user settings unchanged. Invalid defaults are rejected before emitting any option. Temperature must be finite and between 0 and 2.

The five profiles deliberately omit those output/context defaults. Official Qwen recommendations include 16384 or 32768 output tokens (and larger budgets for complex reasoning), above HG's current 4096-token output limit. Native maximum context is not a universally suitable local default. The implementation neither silently clamps those recommendations nor expands native buffers on model selection.

Sampling fields (`TopP`, `TopK`, `MinP`, `PresencePenalty`, `RepeatPenalty`) and optional `BatchSize`/`UBatchSize` are applied directly from the profile on initialization. These are different from user-editable UI defaults. The conservative batch settings retained for the 27B are local hardware tuning, not official model recommendations.

Thinking behavior remains selected by the supported template. This change does not add a generic Thinking UI option, reasoning_effort, preserve_thinking, speculative decoding or arbitrary Jinja execution.

Validation: 48 focused isolated tests passed, covering shipped script compilation, official catalog deserialization, sampling application, defaults on changed/same/unknown selections, invalid defaults, invariant number formatting, stop policy and package checksum. Live acceptance of switching models and UI defaults is separate. The Vulkan runtime was left unchanged during this source update.

When a changed model selection applies option defaults, Lailama emits a persistent version-1 notification with `Presentation: "toast"`, naming the model and listing the emitted option values. YoT shows it immediately until closed and keeps the notice in the archive. The configuration dialog keeps its existing save-and-close behavior. The notice does not imply that model loading or inference has completed.
