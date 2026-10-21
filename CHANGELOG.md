# Changelog

## Unreleased

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
