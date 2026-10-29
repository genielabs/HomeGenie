# Changelog

## Unreleased

- Remove abandoned zero-byte GUID compiler temporaries from `programs/` through an independent post-update step. Preserve numbered assemblies, nonempty files, subdirectories and all `tmp/`/`_update/` contents. Prevent recurrence by removing DLL/PDB outputs after failed C# compilation, while retaining diagnostics and successful compilation behavior.

- Add an early optional core post-update check before interfaces and automation initialization, with durable step revisions. Corrupt/unreadable state and housekeeping errors defer cleanup without blocking HG startup. LLamaSharp verification/legacy cleanup and its publish inventory are excluded from this release; native binaries and existing backups remain untouched. See [post-update checks](docs/guides/post-update-checks.md).

- Notify users when selecting a Lailama model applies YAML option defaults, listing the model and applied values. Structured notifications can request immediate display in YoT using `Presentation: "toast"`; persistent notices remain in the notification archive.

- Add optional YAML defaults for temperature, output budget and context, emitted through Program.Emit only when selecting a different model. Startup, YAML refresh and same-model events preserve user options. The five distributed profiles now record official Qwen sampling recommendations for their current modes; oversized official output recommendations remain documented rather than silently clamped to HG limits. Focused isolated validation: 48 tests passed. The running Vulkan program was not restarted or recompiled by this source change.

### LLamaSharp 0.30.0 preparation â€” 2026-10-26

- Align LLamaSharp, CPU, CUDA12 and Vulkan package references to 0.30.0. Remove discontinued SemanticKernel/KernelMemory adapters and their automatic scripting imports/references; distributed programs have no uses of those adapters.
- Adapt Lailama's memory-mapping option to `LoadMode = LLamaLoadMode.MemoryMap` without changing its RAM profiles, inference settings or model selection. Explicitly reference the .NET expression/dynamic assemblies used by compiled scripts.
- Add an end-to-end compilation regression test for the distributed Lailama script. Isolated Windows validation passed 827 tests, with six platform-dependent symbolic-link cases skipped. A clean framework-dependent linux-x64 Vulkan publish succeeded, and an isolated CPU native probe successfully read `LLamaContextParams.Default` (160-byte managed structure).
- Follow-up 2026-10-27: clean framework-dependent Linux Vulkan binaries were deployed with matching managed/native 0.30.0 versions; previous binaries were isolated and configuration, models, chat history and user data preserved. Real loading/inference succeeded. This confirms deployment and basic inference, not general GPU stability: Intel i915 hangs/native device-lost termination and separate API/WebSocket stalls remain under investigation. Local benchmark observations and the dashboard/widget product priority are recorded in [the evaluation checkpoint](docs/local-llm-evaluation-20261027.md). Include the curated five-model YAML and optional per-model sampler/batch profiles, preserving UI temperature/output limits and existing defaults for omitted fields. Runners 1028/1029 remain development-only. Focused isolated validation passed 35 tests including checksum consistency.

### Filesystem containment and legacy endpoints â€” 2026-10-24

- Confine widget operations and final import copies; fix nested paths, module-file deletion and export metadata preservation. Give widget import/export independent temporary workspaces and close ZIP handles on errors. Isolated Windows fixture passed 72 checks; maintainer confirmed valid export/import on Linux Vulkan.
- Validate Arduino program IDs, types and sketch/project paths (114 isolated Windows checks). Explicitly disable the five legacy Arduino editor endpoints and direct Interface.Import/Interface.Install endpoints pending YoT integration, before any file access. Internal Arduino and interface-install functions remain available, including legacy backup restore; current interface configuration and HomeGenie release updates remain enabled. Interface-install hardening and other import/restore flows remain pending.
- Validate wallpaper upload destinations and deletion paths. MIG 1.3.12 corrects Unicode multipart file offsets (68 isolated parser cases); maintainer confirmed Unicode wallpaper upload and display after updating and rebuilding Vulkan. Complete Linux containment and end-to-end gateway regression coverage is not claimed.

### Follow-up consolidation â€” 2026-10-22

These additions describe the current working tree; they do not assign a new release or imply that all changes below shipped in v2.0.32. The 2026-10-21 consolidation remains below as earlier evidence.

- Generated widgets now expose Saved, Update, Save As and Edit actions. Their association is stored in `metadata.savedWidget` in server-side chat history, not browser storage; Chat History Service 577 is updated to 1.0.8. Validation checks the originating message before patching metadata, and failed persistence restores the previous metadata. Fourteen focused frontend cases and 32 backend history cases passed at implementation checkpoints. On 2026-10-24 the maintainer confirmed reload/second-browser persistence and Edit / Update / Save As acceptance, and confirmed the completed chat widget button/status layout.
- Release-update dialogs distinguish archive verification from extraction and use a responsive minimum width of 360 px. The Update all confirmation lists program address, installed name, current version and target version in aligned columns. On 2026-10-24 the maintainer confirmed the completed Update all visual layout and live acceptance of archive-phase labels and dialog width.
- Optional scripting API references are generated from Doxygen XML. Shared automation guidance clarifies iteration, module access, optional parameters and genuine versus demonstration data. The maintainer confirmed Gemini generated the expected light-counting loop, then repeated the test with program context disabled. This supports that scenario; it is not acceptance of every documented API. Detailed references remain opt-in, not automatic model-driven retrieval.
- HomeGenie Panel notifications add `Notifications.Unregister` (program 580, version 1.0.1). Explicit per-device notification opt-in in Android and Mini notification delivery remain future work; this endpoint alone does not implement either workflow.
- Raspberry Pi update monitoring was confirmed by the maintainer for the published v2.0.31 -> v2.0.32 upgrade, without the previous false installation failure. The old frontend still showed that failure during v2.0.30 -> v2.0.31. Archive-phase labels and dialog width were separately accepted by the maintainer on 2026-10-24.
- Final bundled frontend/cache verification, package consistency, full test suite and release build remain separate maintainer checks. No release is declared by this documentation update.

Consolidated on 2026-10-21. These notes describe work since the previous consolidation; a release version and publication date have not been assigned.

### Added

- Conversational widget presentation mode: render only the interactive widget, without source/Save panels, while preserving ordinary reusable-widget behavior when mode is omitted.
- Correlated widget feedback to the originating AI conversation, with assistant continuation and no extra user-history message; duplicate/busy/cancellation handling and current capability checks.
- Widget speech through `utils.speech.say(sentence)` and `utils.speech.stop()`, sharing chat voice/engine/language preferences and supporting completion, cancellation and errors.
- Local Chart.js, Anime.js and Three.js assets and compact chart-pulse, anime-orbit and three-knot examples. Chart.js is exposed as native ESM to avoid the editor's AMD-loader conflict.
- Explicit widget-authoring guidance for Material Symbols, CSS/library imports, matching fields/events, lifecycle cleanup, responsive layout, speech and feedback. Compact local-provider guidance includes a complete presentation example.

### Changed

- Notification Active/Archive controls moved to the title bar with count badges; selected icons use primary styling. Manage enables selection and bulk Toggle All, Archive/Restore and Delete actions, and resets when the list changes between Active and Archive.
- Notification details use a simpler header/footer, centered content actions, and a footer date with full date/time tooltip. Service messages no longer resize the dialog footer; empty lists show a larger centered secondary message.
- Updateable programs precede Last Update. Update status indicates available updates; history uses aligned columns, status icons, Material expansion icons, a frame and previous/target versions when available. Backup information and cleanup share a row outside History.
- New notification/update labels aligned across 91 languages, translation cache refreshed, and affected dates formatted using the UI language.
- Default Lailama context window increased from 2048 to 8192 for unset options; existing selected values are preserved. Context Window controls actual native context capacity.
- Updated frontend included in the HomeGenie application assets.

### Fixed

- MIG.Audio 1.0.1 exposes only `0` and `service` in both speech interfaces. Request addresses no longer create modules; HTTP results and addressed events remain unchanged. This supersedes the unshipped idle-expiry implementation. HG module automations do not receive events for unregistered addresses. Development Linux Vulkan acceptance passed for chat recognition and synthesis (synthesis from two PCs); the active module list contains only `0`/`service` per audio interface. The maintainer published 1.0.1, updated the package, rebuilt and restarted Vulkan, and reconfirmed both synthesis and recognition. Model-download lifecycle checks remain separate. Both audio domains are hidden from Add widget; engine/service modules retain Generic type. HomeGenie's module-recovery garbage is unchanged and tracked separately.

- Custom-widget component changes discard the previous saved size so the new widget can apply its declared default. Bound-module-only changes preserve the chosen size.
- Free-size chat previews can opt into `fillWidth` to follow both narrowing and widening; fixed-size presets retain their behavior. The reverted dashboard-wide sizing experiment is excluded.
- Chat speech excludes widget source payloads from spoken answers.
- Gemini presentation feedback normalizes persistent context/capability selections consistently with ordinary requests, avoiding false stale-profile rejections.
- Local inference workers retain ownership of native resources until inference/reset completes; initialization failures retain full diagnostics and use bounded cleanup. This addresses concrete lifetime defects, but the historical native core dump has not been conclusively diagnosed.
- DirectML package reference restricted to supported Windows runtime targets, avoiding the unconditional dependency in Linux Vulkan builds; the maintainer reported successful Vulkan tests after a clean rebuild.

### Verification and remaining work

- Maintainer confirmed Gemini presentation/feedback, chart-pulse, chat-preview shrinking/growth, and the notification-detail/updateable-row presentation. Latest Qwen 4B output uses presentation/free-size/import guidance but still contains generated binding/counter/layout defects; it is not recorded as full interaction acceptance.
- Focused backend, speech/audio, sizing and Angular compilation checks passed at implementation checkpoints. Full final release-suite/build and bundled resource/cache verification remain required; the earlier 493-case backend checkpoint is not a new run against this entire release candidate.
- Deferred: intermittent blank local chat after an error/restart, clearer ordinary-chat context-overflow errors, dashboard free-size layout, remaining native lifetime acceptance and optional speech captions. See the [roadmap](docs/roadmap.md) for evidence and scope.

Release publication uses the head commit message plus generated GitHub notes. This file is a maintained summary; no release/tag is created by updating it.
