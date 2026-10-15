# Program update inspection

`GET /api/HomeAutomation.HomeGenie/Config/Packages.Updates` provides the common backend report for program update lists and persistent notifications. It reads the **local** package catalog in `data/packages`; it does not query GitHub/NuGet or download a release.

This endpoint is read-only: it does not install, compile, enable or disable programs, import package groups/modules/schedules, or write application state. Existing `Packages.List` and installation APIs are unchanged. The Programs update view and notification service now use this report. The existing global menu badge retains its manifest comparison until the next UI consolidation step.

## Response

- `schemaVersion`: 1.
- `Status`: `Ok` for a complete check, `Error` if diagnostics make the catalog/inventory unsuitable for an automatic update decision. An error response can still contain partial findings; never interpret it as “everything is up to date”.
- `complete`, `checkedAt`: check outcome and UTC time.
- `fingerprint`: SHA-256 of findings and diagnostics, excluding the check time. Intended for change detection/deduplication, not authorization or an installation guarantee.
- `updateCount`: number of installed program instances with a newer available version.
- `affectedProgramCount`: number of distinct repository/package/program identities with findings, including missing programs.
- `missingRequiredCount`, `missingDependencyCount`: counts by reason; a program can have multiple reasons, so these must not be summed to obtain a distinct total.
- `reviewCount`: program identities with an update target whose local modifications are detected or cannot be determined.
- `items`, `issues`: findings and explicit catalog/dependency diagnostics.

Each item identifies a program by `repository`, `packageId`, and `id`. It includes the owning package version, available program version/checksum, suggested standard address, required flag, reason codes, installed instances, target `updateAddresses`, and the packages requiring it (`requiredBy`). An instance records its current address, name, version, enabled state and baseline/actual code checksums. A suggested address is not reserved: a later installer must handle collisions without overwriting unrelated programs.

`localChanges[address]` is `true` if current code differs from the stored installed checksum, `false` if it matches, and `null` if a trustworthy comparison cannot be made. Checksums cover Setup, Main, Context and Data using the existing package checksum convention. The check does not replace custom code or change options/data files.

## Selection rules

- Installed instances are matched by package identity, including clones with nonstandard addresses. Newer local versions are never proposed for downgrade. Versions compare numeric components and prerelease identifiers; build metadata does not affect precedence. Invalid versions are reported explicitly.
- A package is active if it appears in `installed_packages.json`, or installed program metadata identifies it. This supports installations without a registry. Merely shipping a package does not activate its required programs.
- Missing local programs are proposed only if marked `required` in an active package. Uninstalled optional programs are not proposed. Existing disabled programs remain present; no re-enablement is requested.
- Required external program references resolve against their **owning** package manifest, not stale copies of dependency metadata. Required dependencies are traversed transitively and deduplicated. Traversing an otherwise inactive dependency package does not select unrelated local programs from it.
- Missing/ambiguous catalog identities, unresolved or unsatisfied dependencies, dependency cycles, invalid versions/checksums and catalog/registry read errors produce diagnostics. No arbitrary conflicting entry is selected.

Reason codes: `updateAvailable`, `missingRequired`, `missingDependency`, `dependencyUpdate`. An item may have several reasons. Optional installed programs, including older specialized Genies, can still have an available update; the report does not remove them or change their enabled state.

## Inspection and execution

The report is a snapshot. The program-only updater described below revalidates it before accepting a job and checks each source again before writing. The inspection endpoint itself never installs anything.

Regression tests cover inactive/active packages, new required programs, optional exclusions, clones, preserved enabled state, local changes, semantic version ordering, downgrade avoidance, transitive dependencies, duplicate catalog records and cycles.

## Persistent update notice

Base program 582 checks this endpoint ten seconds after its startup and then once per minute, independently of open browsers. It reuses its own Main loop; no detached timer survives program shutdown. The local inspection does not download catalogs or install anything.

A complete non-empty report creates or updates one persistent notice, `program-updates-available`, scoped to the service module. Findings include newer installed programs and missing required programs/dependencies. The report fingerprint detects changes. Identical checks do not write the archive, reset read/archive flags or emit another notification. After a complete empty report the notice is marked resolved and archived. An incomplete/failed check preserves the existing notice and logs a repeated identical error only once. If findings return after resolution, the notice becomes active/unread again, even if they match the earlier fingerprint.

The navigation-only button opens `/automation?updates=true&adapter=...` for the originating connection. It can be used repeatedly and never calls an installation endpoint. The view uses backend update addresses and shows missing required package entries separately. Existing per-program update controls remain available. Update All uses the dedicated program-only operation below, including missing required programs and dependency ordering. With an older backend, the view retains its existing manifest comparison. Notification labels reuse catalog keys already translated into all supported UI languages.

Update the installed Persistent Notifications program from the base package and rebuild/deploy YoT to use this step. Merely recompiling HomeGenie does not replace an already installed automation program.

## Update All: program-only jobs

In the Programs update view, **Update All** first obtains a fresh complete report and asks for confirmation. The summary includes target versions and marks programs excluded because their code is modified or its baseline is unknown. The page displays per-program results and progress without holding the global loading spinner. It can reconnect to an existing job after navigation or a lost request response. The new label is available in all supported UI languages.

The job updates installed instances and installs missing required programs/dependencies selected by the report. It uses the local package catalog; it does not download releases or call `Packages.Install`. It does not import package groups, dashboards, modules or schedules. Normal program installation still creates the program's own service module.

For existing programs, the installer preserves the name, category, enabled choice, module options and runtime files under `data/programs`. It replaces the packaged code, description and package metadata. Newly required programs use the package's initial enabled choice; address collisions allocate an unused program address instead of replacing another program or its data folder. Enabled programs can run their own Setup/Main logic after installation, as with an ordinary program update.

Dependencies run first. A failed or locally modified prerequisite blocks dependent targets; independent updates can continue. Code with local changes or an unknown baseline is skipped for manual review. Sources and checksums are validated before any mutation and again before each installation. Legacy package installation/removal is rejected while a job is active.

### API

All routes below use `/api/HomeAutomation.HomeGenie/Config/`:

- `GET Packages.Updates`: read-only report, including `fingerprint` and `complete`.
- `POST Packages.UpdatePrograms`: JSON body with `fingerprint`, a client-generated UUID `requestId`, and optional `retryOf` job ID. Returns the accepted job. A repeated UUID with identical inputs returns that original job, including after restart; reusing it with different inputs is rejected.
- `GET Packages.UpdateStatus`: latest job, or an empty `Status: Ok` response if none exists.
- `GET Packages.UpdateStatus/<jobId>`: a specific recorded job.
- `GET Packages.UpdateNotice`: latest job as a persistent-notification envelope, or an empty `Status: Ok` response. Used by the archive service for recovery.

A job includes `id`, `requestId`, `state`, UTC timestamps, monotonic `sequence`, counters and `items`. States are `running`, `completed`, `review` (manual review remains), `partial` or `interrupted`. Item states include `pending`, `running`, `completed`, `skipped`, `failed`, `blocked` and `interrupted`; each item identifies its package/program, address, versions, result message and relative backup path where available.

Retry requires a fresh report/fingerprint and a new request UUID. `retryOf` must identify the latest job with failures. Only its failed, blocked or interrupted targets that still need an update are reconsidered; successful, skipped and unrelated new findings are not silently retried. The UI asks for confirmation again. A lost POST response triggers status recovery, never an automatic duplicate submission.

### Persistence, rollback and shutdown

Jobs, request identities and backups are stored under `data/program-updates`. Journal writes are flushed and atomically replaced. Compact outcome journals and request identities remain for diagnosis and idempotency. Backup retention and cleanup are described below.

Each existing target is backed up before replacement. Compilation or persistence failure attempts to restore the previous code, metadata, binary and enabled state. An explicit message identifies a rollback that needs manual recovery. A concurrently removed/replaced program is not overwritten during rollback. Runtime data files are not deleted. This is a per-program operation, not an all-or-nothing transaction across the whole batch.

On an orderly service shutdown the current installation finishes, and remaining pending items become interrupted. Compiler/native work is not forcibly aborted. After an unexpected stop, a recovered running journal is marked interrupted and is never replayed automatically: inspect current state before explicitly retrying. A malformed journal is preserved for manual recovery and does not prevent HomeGenie startup.

### Job notifications

Each job has a separate persistent outcome notice, distinct from the availability notice. Progress uses a monotonic sequence so late asynchronous events cannot replace a newer result. Service 582 also polls `Packages.UpdateNotice`, recovering the latest result after restart or after the archive program updates itself. Completed job notices remain available for the user to read/archive; the separate availability notice resolves when a complete check finds no pending updates.

Install the updated Persistent Notifications program from the base package to enable this recovery behavior. Update All requires explicit user confirmation. The post-release reconciliation described below automatically installs missing required programs only.

## Permanent updates view and backup retention

The Programs update button is always visible, with a badge only when updates exist and a selected appearance while viewing updates. This view remains open after the last update. Current availability is separate from the latest operation: an up-to-date status requires a complete successful report and no pending findings or running job. Inspection errors are not shown as an up-to-date system. Job panels are shown only in this view; no dismiss-and-reappear behavior is needed.

Expandable history shows the most recent 50 jobs, dates, outcomes and per-program details. Backup space and removable successful-backup space are reported separately. Manual cleanup asks for confirmation and removes only completed item backups from terminal jobs; it never removes journals or request identities. Running jobs prevent cleanup. Previously removed backup paths are no longer presented as available.

Automatic maintenance runs on startup and on history/job requests, at most hourly while idle: successful item backups expire after seven days; other terminal item backups after thirty days. Interrupted/ambiguous items and explicit manual-recovery failures are protected without automatic expiry. These protected backups require operator review and may still accumulate. Unknown or malformed records are preserved and reported, and cleanup never follows symbolic links or uses paths supplied in a journal's `backup` field. Backup removal is derived only from a valid job UUID and numeric item index below `data/program-updates`. The compact journals are retained independently; they are not a second copy of source/binary backups.

Additional API routes under `/api/HomeAutomation.HomeGenie/Config/`:

- `GET Packages.UpdateHistory`: newest jobs, total count, backup/removable bytes, available paths, protected count, retention periods and diagnostics.
- `POST Packages.UpdateClean`: remove eligible successful backups and return refreshed history/storage information. No arbitrary path or job deletion parameter is accepted.

The retention API does not install programs; post-release installation uses the dedicated reconciliation below.

## New required programs after a HomeGenie release update

A successful release installation writes `data/program-updates/reconciliation/pending.txt` after all staged files have been copied and requires restart. The new process performs reconciliation only after the ordinary program/module inventory and interfaces are initialized. No new package programs are compiled against the previous runtime during release-file copying.

At startup, a durable `reconciliation/state.json` tracks the observed release identity (version and release date), marker token, request UUID, report fingerprint and accepted job ID. The first release containing this feature also reconciles an existing installation without a marker from the old updater. Subsequent ordinary restarts do not repeat the operation. A fresh explicit update marker can trigger a new pass even for the same release identity. Missing release metadata without a marker does not trigger installation.

Automatic jobs have `kind: required`. They select only missing required programs/dependencies from active packages and the unsatisfied external dependencies needed by those targets. Other available program updates remain manual. Existing disabled programs count as installed and are not re-enabled; edited or unknown-baseline dependency code is skipped and dependent targets are blocked. Inactive packages and uninstalled optional programs remain excluded. All changes use the program-only store: no package group/dashboard/scheduler import, with the same checksum validation, address collision checks, backup/rollback and persistent progress/outcome notices as Update All.

The coordinator persists a request identity before submission and reuses it when recovering ambiguous acceptance. Accepted jobs, including failed/interrupted jobs, are never automatically retried at every boot. Incomplete catalogs or preflight failures preserve a failed reconciliation record and log an actionable error; the normal Programs update report/notice continues to expose unresolved findings for manual review and retry. A malformed coordinator record is preserved rather than overwritten and does not prevent HomeGenie itself from starting. This is not a transaction or rollback for the entire system release.
