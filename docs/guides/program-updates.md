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

## Next stages

The report is a snapshot, not an execution plan or transaction. Before applying changes, a future updater must revalidate current versions, local modifications, dependencies and source integrity. Dependency ordering, recoverable installation jobs, progress notifications and UI integration are separate steps. No automatic install or notification is started by this inspection endpoint.

Regression tests cover inactive/active packages, new required programs, optional exclusions, clones, preserved enabled state, local changes, semantic version ordering, downgrade avoidance, transitive dependencies, duplicate catalog records and cycles.

## Persistent update notice

Base program 582 checks this endpoint ten seconds after its startup and then once per minute, independently of open browsers. It reuses its own Main loop; no detached timer survives program shutdown. The local inspection does not download catalogs or install anything.

A complete non-empty report creates or updates one persistent notice, `program-updates-available`, scoped to the service module. Findings include newer installed programs and missing required programs/dependencies. The report fingerprint detects changes. Identical checks do not write the archive, reset read/archive flags or emit another notification. After a complete empty report the notice is marked resolved and archived. An incomplete/failed check preserves the existing notice and logs a repeated identical error only once. If findings return after resolution, the notice becomes active/unread again, even if they match the earlier fingerprint.

The navigation-only button opens `/automation?updates=true&adapter=...` for the originating connection. It can be used repeatedly and never calls an installation endpoint. The view uses backend update addresses and shows missing required package entries separately. Existing per-program update controls remain manual; installing missing programs, Update All, dependency ordering and progress jobs are subsequent work. With an older backend, the view retains its existing manifest comparison. Notification labels reuse catalog keys already translated into all supported UI languages.

Update the installed Persistent Notifications program from the base package and rebuild/deploy YoT to use this step. Merely recompiling HomeGenie does not replace an already installed automation program.
