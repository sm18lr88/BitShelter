## Security policy

### Supported versions

Only the latest release of BitShelter gets security fixes.

| Version | Supported |
|---|---|
| 0.2.x | Yes |
| 0.1 (2018, original repository) | No |

### Report a vulnerability

Report a vulnerability privately on GitHub:

1. Open the [Security tab](https://github.com/sm18lr88/BitShelter/security) of this repository.
2. Click **Report a vulnerability**.
3. Describe the problem, the affected version, and the steps to reproduce it.

Do not open a public issue for a vulnerability. Do not put exploit details in public discussions.

The maintainer replies in the private advisory. BitShelter is a volunteer project, so response times can vary. When a fix is ready, the maintainer publishes the advisory and a release that contains the fix.

### Scope

The BitShelter service runs as LocalSystem. It can create and delete shadow copies, and it reads files to make backups. These are examples of vulnerabilities in scope:

- A standard (non-administrator) user can connect to the `BitShelter.SnapshotService` named pipe.
- A standard user can make the service load a rule or state file, for example through `%ProgramData%\BitShelter`.
- A standard user can make the service read, copy, or delete files or snapshots that the user cannot access.
- A backup passphrase becomes readable by a standard user.
- The installer or the Agent creates a way for a standard user to run code as an administrator or as LocalSystem.

Administrators are trusted. Actions that need administrator rights, such as editing rules in the Agent, are not vulnerabilities.

[ARCHITECTURE.md](ARCHITECTURE.md) describes the security boundary.
