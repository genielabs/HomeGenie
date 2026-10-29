# Early post-update checks

`PostUpdateCheck.Run` is called by `ServiceWorker.ExecuteAsync` before `PostInstallCheck` and before constructing `HomeGenieService`. Interfaces and the automation engine have not been initialized. This release contains only optional housekeeping of abandoned empty compiler outputs.

## Empty compiler temporary files

The step `empty-compiler-temporaries-v1` removes only zero-byte, top-level files named `<GUID>.dll`, `<GUID>.dll.pdb` or `<GUID>.dll.mdb` from `programs/`. It preserves numeric program assemblies (even empty ones), nonempty GUID files, unknown names and subdirectories such as Arduino projects. Symbolic-link redirections are rejected rather than followed.

The cause is also fixed: `CSharpAppFactory.CompileScript` previously opened DLL/PDB outputs before Roslyn emission and kept them when emission failed. It now closes streams and removes outputs created by that failed attempt, preserving the original diagnostics/exception. Successful DLL/PDB outputs remain available for `CSharpEngine`'s normal move/load flow.

## Completion and errors

Each migration has a permanent revision ID. Successfully completed IDs and UTC timestamps are stored atomically in `data/post-update/completed.json`. Rebooting or updating HG again does not repeat a completed revision. Add a new ID for a new migration; do not reuse completed IDs for changed work. Existing step records are preserved.

The completion record is written only after the operation succeeds. Filesystem errors, link/path rejection and a malformed/unreadable completion record are logged and defer housekeeping without stopping HomeGenie. An unreadable record is preserved for inspection instead of being overwritten. Failure to save completion leaves the step pending; a later startup can finish safely because already deleted empty temporaries require no further action.

## New installations

A clean installation has no compiler residues. The check records completion without creating `programs/`, `runtimes/`, `tmp/` or `_update/`. It requires no publish inventory and does not initialize or inspect LLamaSharp.

## Excluded scope (maintainer decision, 2026-10-29)

LLamaSharp binary verification and legacy cleanup are excluded from this release. The post-update does not read `.deps.json` or `HomeGenie.llamasharp-assets.txt`, move adapter/native binaries, create binary backups or gate startup on LLamaSharp files. The publish inventory target has been removed. Previously created inventories and backups, if present, are left untouched.

`tmp/` and `_update/` cleanup is also excluded: Vulkan's `tmp/` predominantly contains an old Z-Wave device catalog cache, while `_update/` contains installed-release download/staging state and development/recovery backups. Read-only inventories found 134 empty GUID files on Vulkan; the maintainer's production listing contained 332 zero-byte GUID DLL/PDB files. No live runtime files were deleted during inspection or source preparation.

## Validation

Regression cases cover failed and successful C# compilation, debug-output creation failure, exact empty-GUID filtering, preservation of numeric/nonempty/nested files, fresh installs, repeat startup, existing step records, completion-write failure/retry, corrupt state and non-interference with legacy native files or malformed old inventories. The symbolic-link case requires host privileges. A live startup check on Linux remains pending; no running Vulkan instance was restarted by this source change.
