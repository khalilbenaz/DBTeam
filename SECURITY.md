# Security Policy

## Supported versions

Only the latest release published on [GitHub Releases](https://github.com/khalilbenaz/DBTeam/releases) receives security fixes.

## Reporting a vulnerability

**Please do not open a public issue for a security problem.**

Use GitHub private vulnerability reporting: <https://github.com/khalilbenaz/DBTeam/security/advisories/new>
(repository maintainers: this requires *Settings → Code security → Private vulnerability reporting* to be enabled).

Please include the DB TEAM version, Windows version, SQL Server version, reproduction steps, and the impact you foresee.
You can expect an acknowledgement within 7 days and a status update within 30 days.

## Security model and known limits

DB TEAM is a desktop tool that runs with the rights of the current Windows user. Things worth knowing:

- **Stored secrets** (SQL passwords in `%APPDATA%\DBTeam\connections.json`, AI API key in `ai.json`) are protected with Windows DPAPI (`CurrentUser`, fixed application entropy). They are bound to the Windows profile: any process running as the same user can decrypt them. A secret that cannot be decrypted (other profile or machine) is reported, never silently erased. Passwords also stay in memory for the session.
- **TLS**: connections are encrypted and the server certificate is validated by default. The *Trust server certificate* option disables that validation and exposes the connection to man-in-the-middle attacks; use it only on trusted networks.
- **Generated SQL**: scripts produced by Schema Compare, Data Compare, Data Generator, Table Designer, import and admin tools are meant to be reviewed before execution. Identifiers are escaped, but the scripts can still be destructive (`DROP`, `DELETE`).
- **AI assistant**: the endpoint is user-configurable and the API key is sent to whatever URL is configured. What you type (queries, schema information) is sent to that provider; do not send confidential data to a provider you do not trust.
- **Terminal and Git modules** run real local processes with your rights.
- **Query history** is stored unencrypted on disk and may contain sensitive statements (for example `CREATE LOGIN ... PASSWORD`).
- Release binaries are not code-signed by default (SmartScreen will warn); see [docs/SIGNING.md](docs/SIGNING.md).

## Supply chain

GitHub Actions are pinned to commit SHAs, CodeQL runs weekly and on pull requests, Dependabot watches NuGet and Actions, and CI fails on known-vulnerable NuGet packages.
