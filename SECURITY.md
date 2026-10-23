# Security policy

## Reporting a vulnerability

Please report suspected security vulnerabilities through
[GitHub private vulnerability reporting](https://github.com/genielabs/HomeGenie/security/advisories/new).
Do not open a public issue containing an unpatched vulnerability or exploit details.

Include the HomeGenie version and operating system, relevant configuration
(including whether password protection is enabled), reproduction steps, and observed
impact. Use an isolated test environment and remove credentials and personal data
from logs and attachments. Please coordinate public disclosure with the maintainers
so that a correction and user guidance can be made available.

## Updates and deployment

Use the latest stable release and review the
[published security advisories](https://github.com/genielabs/HomeGenie/security/advisories).
This policy does not promise security backports to older releases.

Read [Securing HomeGenie](docs/guides/securing-homegenie.md) for password protection,
service-account permissions, network exposure, and backup guidance.

HomeGenie intentionally supports compiling and executing automation programs.
These programs run with the privileges of the service account. Without password
protection, clients able to reach the HTTP service can access administrative APIs.
Run the service under a dedicated, unprivileged account and restrict access to trusted
users and clients. Suspected authentication bypasses or unintended filesystem access
should still be reported privately.
