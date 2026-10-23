# Securing HomeGenie

HomeGenie is a programmable automation server. Its administration and scripting
features can control devices, install packages, and execute code with the permissions
of the operating-system account running the service. Treat access to the server as
administrative access, and grant it only to trusted users and clients.

## Protect access before connecting clients

A fresh installation has no password configured. Without password protection,
clients that can reach the HTTP service can access its API, including administration
and program execution. A private LAN is not an authentication mechanism: guests,
shared computers, and other connected devices may also be able to reach the server.

Perform initial setup from a trusted computer on a restricted network, or locally
on the server. Before allowing other clients to connect:

1. Open **Maintenance** from the main menu.
2. Under **Service configuration**, select **HTTP**.
3. Enable **Password protected access**.
4. Set a strong, unique password and click **Confirm**. The current username is `admin`.
5. Open a private browser window and check that access requires authentication.
   Configure your mobile apps and API integrations with the same credentials.

These controls are in Maintenance; completing the Setup Wizard does not itself
configure a password. If you deliberately leave password protection disabled for
an isolated test system, every client able to reach that service must be trusted.

Configure the password through the interface rather than pasting it into URLs,
terminal commands, screenshots, or support reports. Changing the password may
require reconnecting existing clients.

## Use a dedicated, unprivileged account

Run HomeGenie under a dedicated non-root account on Linux, and without administrator
privileges on Windows. Do not use `sudo` to launch the application for normal operation.
Administrative privileges used to install a service do not need to be granted to
the service process itself.

Give the account write access to the HomeGenie installation and the specific data
locations it needs. Keep unrelated personal files, credentials, and system
configuration outside its writable locations. A dedicated account limits permissions;
it is not a sandbox that automatically blocks all reads or network access.

Grant hardware access through the relevant device permissions or groups, only for
hardware you use, rather than running the whole service as root. Programs, including
AI-generated programs, run with this account's permissions; review code and package
sources before executing them.

If you use a container, avoid privileged mode, broad host-directory mounts, and
mounting the container-management socket. Check the actual write requirements for
your deployment: configuration, programs, logs, and application updates may need
writable locations beyond `data`. Test hardware access and updates with those permissions.

## Restrict network exposure

Keep the HomeGenie service on a trusted network and use firewall rules to limit
which devices can reach its configured port. Avoid forwarding that port directly
from your Internet router. For remote access, prefer a VPN with access restricted
to the users and devices that need HomeGenie.

Use HTTPS when accessing HomeGenie across an untrusted network. HTTPS protects
traffic in transit; it does not replace password protection. If you terminate HTTPS
at a reverse proxy, keep HomeGenie's authentication enabled and prevent clients
from bypassing the proxy to reach an unprotected backend. Do not assume that
securing the proxy automatically secures a separately reachable HomeGenie port.

## Maintain and verify your installation

- Keep HomeGenie and installed packages up to date. Review release notes and security advisories.
- Install programs and packages only from sources you trust; inspect generated automation code before running it.
- Protect backups as sensitive files: they can contain configuration, credentials, and automation code. Restrict access and keep a recovery copy outside the installation folder.
- After restoring a backup, recheck password protection, service settings, and network exposure before reconnecting clients.
- Review access again after changes to your router, firewall, proxy, service account, or hardware permissions.

## Report a security issue privately

Use [GitHub private vulnerability reporting](https://github.com/genielabs/HomeGenie/security/advisories/new)
to send a suspected vulnerability to the maintainers. Include the affected version,
configuration prerequisites, reproduction steps, and observed impact. Remove passwords,
API keys, and personal data from attachments. Do not post an unpatched vulnerability
or its exploit details in a public issue.

For a Linux systemd example, see [Running as a System Service](../../README.md#running-as-a-system-service).
