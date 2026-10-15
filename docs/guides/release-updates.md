# Persistent release downloads

Release downloads are owned by HomeGenie's backend. Closing a dialog or browser stops only UI monitoring. Reopening the dialog restores a matching download, paused transfer or verified ready archive without submitting another download automatically. A paused transfer resumes only on explicit request.

## Identity and storage

The selected source is an official `https://github.com/genielabs/HomeGenie/releases/download/` asset discovered through the GitHub release catalog. Identity includes URL, version, runtime, asset ID, size, exact asset update timestamp and published digest. Asset names preserve the running CPU/Vulkan/CUDA edition separately from the runtime RID stored inside release_info.xml.

`_update/download/state.json` records the operation, validators and progress. `release.part` contains an incomplete transfer, and `release.zip` a completed transfer pending/after verification. Only one selected asset is cached: choosing a different asset or explicitly restarting clears these known cache files, rather than deleting the entire `_update` directory or its previous-install backup. No client-supplied URL or filesystem path is accepted by the download API.

A resumed HTTP request uses Range and If-Range. A valid 206 must start at the stored file offset and have compatible validators/size. A 200 response restarts from byte zero instead of appending. A 416 for a partial transfer resets that partial and retries once from zero. Without a validator, an incomplete partial is restarted. A partial already matching the expected full size can be verified locally without another download. GitHub redirects are handled by HttpClient; temporary redirect URLs are not persisted.

Progress is reported in bytes, expected total and percentage when known. Journal writes are throttled during transfer. Pausing or service shutdown cancels the HTTP read and retains the partial. A recovered downloading/verifying/extracting journal becomes paused; it never launches a worker on its own. Invalid journals are preserved and reported as errors. A stalled body read times out after 45 seconds without data and leaves the partial available for retry.

## Verification and installation

Before ready state, the backend verifies expected size, GitHub SHA-256 digest when supplied, and ZIP integrity including CRC. Extraction happens in an isolated directory and rejects traversal/symlink entries. The official outer launcher (`start.sh`, `start.bat` or `start.command`, according to the runtime) is skipped: in-place updates install only the contents of `homegenie/`. Other files outside that directory are rejected. The archive must contain `homegenie/release_info.xml` with the selected version and RID. Its release date is normalized to the GitHub asset date as before. Only successful extraction promotes the staging tree to `_update/files`.

Ready state includes SHA-256 hashes of the archive and staged files. Installation rechecks that neither the archive nor staging has changed, and rejects missing or extra files. Only then does the ordinary release installer run. Its successful completion creates the post-release required-program marker described in [program updates](program-updates.md). This is not an all-or-nothing rollback for the existing system installer.

Download, extracted staging, installation and manual archive installation are serialized. Manual upload continues to use its existing format; it cannot replace staging while a download or installation is active. A paused download can be resumed after a manual update, but its staging must be regenerated before downloaded installation is allowed.

## API

Routes use `/api/HomeAutomation.HomeGenie/Config/System.Configure/`:

- `UpdateManager.DownloadStatus`: read-only snapshot (`state`, `release`, `bytes`, `totalBytes`, `percent`, `error`).
- `UpdateManager.DownloadUpdate`: start/resume the server-selected available asset; optional JSON `{ "restart": true }` explicitly discards its cache and starts over. Returns immediately. A second start for the same active asset returns its existing state; a different active asset is rejected.
- `UpdateManager.PauseDownload`: request cancellation of the transfer; monitor status until paused.
- `UpdateManager.InstallUpdate`: install only a verified ready operation.

Download states are idle, downloading, paused, validating, extracting, ready, error, installing and installed. The UI shows phase/byte progress, explicit pause/resume/retry and install only when ready. Status failure is not treated as successful download completion. The UI also recovers installing/installed operations, including a lost HTTP response during restart. When restart is required it checks for a new process start before reporting completion. An ambiguous interrupted installation is reported for review rather than replayed. Status polling omits the internal staging file-hash manifest. The backend rejects selected releases that are already installed or older, using the existing release-date comparison.

## Verification plan

Automated tests use an in-memory HTTP handler serving a real ZIP, without weakening production source restrictions. They cover cached reopen, partial failure/resume, ignored Range, changed validators, complete partial files, checksum/corruption failures, pause/shutdown, staged-file tampering, serialization and real InstallFiles execution in a temporary folder. Existing program reconciliation tests cover required-only selection and persistence across restart. Frontend tests cover reopening, no automatic duplicate POST, explicit resume, failure handling and monitor recovery.

For live tests, use a disposable copy of a HomeGenie installation with a correctly identified older release and the desired runtime/edition. Exercise GitHub download, close/reopen, pause/resume and backend restart before installing. Preserve the copy's configuration and verify groups, schedules, custom/disabled programs and required reconciliation after restart. Do not downgrade or install a test release over the normal development/production directory. A full live install should target a release containing this implementation; the currently published older binary cannot validate its new post-restart behavior.

## Live verification

The Vulkan test environment completed a real GitHub transfer with pause/resume, page refresh, continued transfer after closing the dialog, official-archive verification/extraction and ready-cache reuse. A controlled local staging fixture then used the current build binaries and package catalogs, with explicitly regenerated staging hashes while the backend was stopped. Installation, UI recovery after the user manually relaunched the service, and post-release reconciliation completed successfully. The test environment had no automatic process relaunch, so that launcher behavior was not tested. Groups, automation groups, programs and scheduler remained byte-for-byte unchanged; only the configuration update timestamp changed. No required programs were missing in this installation fixture; missing-required installation and repeat-start protection were exercised separately by the migration scenario. This controlled fixture validates installation orchestration, not the compatibility of every file in a future complete published bundle.

A second controlled fixture exercised the complete Maintenance install/restart/reconciliation path with a package catalog gaining a new required program and a new optional program. Exactly the missing required program was installed. An existing customized program retained its source and version; an existing disabled program retained its disabled state and version despite newer catalog versions. A second restart, again with manual process relaunch by the user, retained the identical completed job (same ID, sequence, timestamps and one completed item), with no duplicate program addresses. Groups, automation groups and scheduler hashes remained unchanged. Test catalog entries were confined to the Vulkan runtime and controlled staging; repository package catalogs were not changed.
