# Local LLM evaluation checkpoint — 2026-10-27

## Decision

Pause the search for a best small local model. The priority is HomeGenie's provider-independent AI workflow, including creating dashboards and placing/configuring widgets. Gemini already provides a useful working baseline according to maintainer acceptance. Local inference remains supported and experimental; do not treat general reasoning scores as a complete assessment of device control or automation coding.

## Recorded observations

Program 1028 uses Request.Generate with ten cases repeated three times. Strict passes require both expected content and format. These are observations of particular GGUF files, runtime profiles and prompts, not rankings of model families. The validator has known accent/case/whitespace sensitivities. Disabling context sharing/functions does not remove all HomeGenie system instructions. The later context-off repeat is not a neutral-prompt benchmark or a controlled causal result.

| Model/profile | Strict passes | JSON-only | Explanation + JSON | Mean observed response |
| --- | --- | --- | --- | --- |
| Qwen3-4B-Instruct-2507 UD-Q5_K_XL, temperature 0.7 | 17/30 | 7/18 | 10/12 | 38 s |
| Qwen3-4B Q5_K_M, temperature 0.7 | 17/30 | 17/18 | 0/12 | 87 s |
| Empero Qwen3.8-4B-Distill Q5_K_M, temperature 0.6 | 11/30 | 7/18 | 4/12 | 79 s |
| Qwen2.5-3B-Instruct Q5_K_M, temperature 0.7 | 6/30 | 5/18 | 1/12 | 20 s |
| HauhauCS Qwen3.5-4B Aggressive Q6_K, non-thinking, temperature 0.7, shared context | 10/30 | 4/18 | 6/12 | 57 s |
| Same HauhauCS profile, context sharing disabled | 13/30 | 4/18 | 9/12 | 48 s |

SmolLM3-3B UD-Q5_K_XL: 1028 stopped on its first request after about nine seconds with `Local AI returned no final text`. A manual chat response was generated but failed the logic/JSON task. Template, stop handling and filtering were not resolved; no suite score is available.

Qwen3-4B original's explanation-format failures frequently contain correct answers, so its 17/30 overall score must not be equated with Instruct's different strengths. Empero had one output-limit case. Other completed suites above had no output limits or timeouts. General benchmark runs do not establish device-action or generated-program acceptance.

The maintainer confirmed useful device-control behavior in chat; one inspected request executed Porch Light On and Light 2 Off with two successful action receipts. This limited evidence supports keeping integration evaluation separate from general puzzle/math evaluation, not claiming universal reliability.

Current curated development catalog: Qwen3-1.7B Q8_0; Qwen3-4B Q5_K_M; Qwen3-4B-Instruct-2507 UD-Q5_K_XL; Qwen3-30B-A3B-Thinking-2507 UD-Q4_K_XL; Unsloth Qwen3.8-27B UD-Q4_K_M. The large models have manual evidence rather than comparable completed 30-case suites. The 27B remains provisional, with poor responsiveness and variable accuracy on the current host.

## Runtime checkpoint and open limitations

LLamaSharp 0.30.0 was deployed as a clean framework-dependent Linux Vulkan binary set, isolating previous binaries and preserving data/configuration/models/history. Real model loading and inference subsequently succeeded. The distributed five-model YAML now includes the optional sampler/batch profile support already exercised on Vulkan. Benchmark runners 1028/1029 and their results remain development-only; benchmark opt-in flags are not distributed. Focused isolated verification passed 35 tests, including script compilation, sampling overrides, stop policy and package checksum.

Large-model operation also produced Intel i915 GPU hangs and native device-lost termination. Separately, API/WebSocket stalls while automations kept running remain unexplained; external SSD I/O/swap is a maintainer hypothesis, not a confirmed cause. No production-wide stability claim is made.

## Next product work

- Design AI dashboard creation and widget placement/configuration using existing dashboard semantics; inspect existing APIs and layout intentions first.
- Preserve explicit action outcomes and distinguish generated proposals from successful saved changes.
- Evaluate simple automation programs through generation, compilation and execution, independently of general benchmark scores.
- Keep deterministic regression fixtures and recorded results; resume broad local model comparisons when they answer a concrete product question.

Raw run evidence remains in Vulkan `data/programs/1028/runs/`: `20261027-010136-matrix-e380411f`, `20261027-090230-matrix-c5afa07d`, `20261027-101153-matrix-faefce22`, `20261027-105120-matrix-7b6ef555`, `20261027-121839-matrix-90b872c9`, and `20261027-130220-matrix-d419e120`.
