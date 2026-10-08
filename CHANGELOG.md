## Changelog

All notable changes to BitShelter are in this file. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/).

### [0.2.0] - 2026-10-08

This release moves BitShelter to .NET 10 and Windows 11, and adds backups.

#### Added

- Backups: after every N-th snapshot of a rule, BitShelter copies selected folders from the new snapshot to an output folder. A backup can be a folder copy or a zip or tar archive, with compression. Include and exclude patterns (glob or regex) select the files.
- Backup encryption in the standard OpenPGP format, with a public key or a passphrase. GnuPG and Kleopatra can decrypt the backups.
- Backup limits: maximum size of one backup, number of backups to keep, and total size of the backups of a rule.
- Desktop notifications in the Agent when a backup completes or fails.
- The **Global** pruning strategy. When a drive is at its snapshot limit, BitShelter deletes the oldest snapshot of the rule. Then Windows does not delete a System Restore point or a snapshot of another rule.
- The **On failure: restart the VSS service** option now works: the service restarts the VSS service before each retry.
- An MSI installer (WiX 5). It installs the service and the Agent, and replaces installs of the 2018 version.
- Automated tests (xunit v3) and a GitHub Actions workflow that builds, tests, and makes the MSI.
- `THIRD-PARTY-NOTICES.txt`, also installed by the MSI, with the licenses of all bundled components.

#### Changed

- BitShelter now targets .NET 10 (x64) and Windows 11. It needs the .NET 10 Desktop Runtime.
- The Agent and the service communicate through a named pipe instead of WCF. Only the service account and elevated administrators can open the pipe.
- The Agent uses stock WinForms controls instead of the Syncfusion controls.
- Scheduling uses Quartz 4. Logging uses Serilog 4.
- The service writes its state files atomically.
- New project-owned icon and "how it works" diagram. New screenshots of Windows 11 and of the current Agent.
- New rules take a snapshot every 4 hours instead of once a day at 08:00, because missed times are not caught up. **On failure: restart the VSS service** is off by default, and the total backup size has no limit by default.
- The service accepts up to four Agent connections at the same time.
- VSS access uses BitShelter's own COM interop on .NET source-generated COM, declared from the Windows SDK headers, instead of AlphaVSS. The installer no longer ships a C++/CLI assembly or `Ijwhost.dll`. VSS errors in the log now name the VSS error code.
- The Advanced tab has one VSS option, **Ask applications to save their data first (VSS writers)**, instead of the snapshot context, snapshot type, and include/exclude writer fields. The old snapshot type was ignored, and two of the old contexts made snapshots that Windows deleted at once. Rules from earlier versions keep their choice: a context with writers maps to the new option.
- The README is short. Details are in the new [user guide](docs/user-guide.md).

#### Removed

- The custom AES and PBE encryption code. It used the password directly as the key, a fixed salt, and the wrong cipher for some options, and no tool could decrypt its output.
- Support for Windows versions older than Windows 11.
- Third-party documents and images that the project had no license for: a saved blog article, copied Microsoft documentation, an API Monitor filter file, an infographic, and an icon and a banner that contained third-party logos.

#### Fixed

- New rules got the **Local** pruning strategy instead of **Global**.
- Help links in the Agent did not open on .NET 10.
- The rule editor did not open, because of controls that were left over from the Syncfusion removal.
- Buttons with white text on a white background, and grid columns that did not scale with the display DPI.
- The service wrote an error to the log each time it stopped.

#### Security

- The service now lets only SYSTEM and Administrators access `%ProgramData%\BitShelter`. Before, a standard user could add a rule file that the LocalSystem service loaded.
- The service ignores rule and state files that SYSTEM or the Administrators group does not own.
- Backup passphrases are stored protected with DPAPI. The plain passphrase does not go to disk or over the named pipe.

### [0.1-alpha] - 2018-05-05

The first release, by Alexis Incogito, in the [original repository](https://github.com/alexis-/BitShelter).
