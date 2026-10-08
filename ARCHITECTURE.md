## BitShelter architecture

This document describes the parts of BitShelter, how they communicate, and which part owns which data.
For build, install, and test commands, see the [user guide](docs/user-guide.md#install) and [TESTING.md](TESTING.md).

### Components

| Project | Type | Responsibility |
|---|---|---|
| `BitShelter.Service` | Windows service `BitShelter` (`BitShelter.Service.exe`). It runs as LocalSystem. | Owns the rules, the records of the snapshots that it created, and the backup state. Schedules and runs the snapshot, prune, and backup jobs. Maps backup input folders to snapshot paths. Answers the Agent on a named pipe. |
| `BitShelter.Agent` | WinForms tray app (`BitShelter.Agent.exe`). It requires elevation. | User interface to create, edit, and delete rules and their backups. Shows backup notifications. Creates and deletes the `BitShelter` logon task for **Run at startup**. It keeps no rule data of its own. |
| `BitShelter.Common` | Class library | Shared models, the IPC contract, VSS access (own COM interop in `VSS\Interop`, no third-party wrapper), volume helpers, path filters, the backup engine (`BitShelter.Common\Backup`), OpenPGP encryption, and logging. The backup engine has no VSS dependency, so tests run it on normal folders. |
| `BitShelter.Tests` | xunit v3 test project | Automated tests. See [TESTING.md](TESTING.md). |
| `BitShelter.Benchmark`, `BitShelter.Benchmark.Console` | Library and console app | Benchmark of the archive, compression, and OpenPGP combinations that backups can use. It exports CSV. |
| `BitShelter.Setup` | WiX 5 MSI project. It is not in the solution. | Publishes the service and the Agent, adds `LICENSE.txt` and `THIRD-PARTY-NOTICES.txt`, then packages them into `BitShelter-<version>-x64.msi`. |

The service is the only component that must run. The Agent is optional. You need it only to change the rules.

### Dependency direction

- `BitShelter.Agent` and `BitShelter.Service` reference `BitShelter.Common`. They do not reference each other.
- The Agent and the service share only the types in `BitShelter.Common\Ipc` and `BitShelter.Common\Models`.
- `BitShelter.Benchmark` references `BitShelter.Common`. `BitShelter.Benchmark.Console` references `BitShelter.Benchmark`.
- `BitShelter.Tests` references `BitShelter.Common`, `BitShelter.Service`, and `BitShelter.Agent`.
- `BitShelter.Common` and `BitShelter.Service` show their `internal` types only to `BitShelter.Tests` (`InternalsVisibleTo`).

### Agent-to-service communication

The Agent and the service communicate through a named pipe. WCF was removed in the .NET 10 upgrade. Do not add it again.

- Pipe name: `BitShelter.SnapshotService` (`SnapshotIpcProtocol.PipeName`).
- Each connection carries one request and one response.
- Each message is a 4-byte length followed by UTF-8 JSON (`SnapshotIpcStream`).
- The maximum message size is 16 MiB (`SnapshotIpcProtocol.MaxMessageBytes`). Both sides reject a larger message.
- The Agent waits at most 2 seconds for a connection (`SnapshotIpcProtocol.DefaultConnectTimeout`).
- Operations: `Ping`, `GetRules`, `AddOrUpdateRule`, `DeleteRule`, and `GetBackupResults`. `DeleteRule` can also delete the snapshots of the rule. `GetBackupResults` returns the backup results with an `Id` greater than `SinceId`.

#### Security boundary

The service runs as LocalSystem and can delete every shadow copy on the computer.
For this reason, the pipe ACL (`SnapshotPipeServer.CreatePipeSecurity`) grants access to two principals only:

- The service account gets full control.
- `BUILTIN\Administrators` gets read and write access.

A process of a standard user, or a non-elevated process of an administrator, cannot connect.
The Agent manifest requests `requireAdministrator`, so Windows starts the Agent elevated.
Do not grant access to Authenticated Users or to other broad groups.

### Data and state

The service owns all state. It keeps the state in `%ProgramData%\BitShelter`.
When the service starts, it sets the folder ACL (`AppDataSecurity`): only SYSTEM and Administrators have access, and inheritance from `%ProgramData%` is blocked. By default, `BUILTIN\Users` can create files in `%ProgramData%` subfolders. Without this ACL, a standard user could plant a rule file that the LocalSystem service then runs.

| File | Content | Writer |
|---|---|---|
| `rule_<ticks>.json` | All rules. Each change writes a new file. At startup, the service loads the newest file. It keeps the 10 newest files and deletes older ones. | Service |
| `instances.json` | The snapshots that the service created, and the rule of each snapshot. The prune job uses this file. | Service |
| `backups.json` | The snapshot counter of each rule (for **Every** and **Offset**) and the 100 most recent backup results. | Service |
| `appconfig.json` | Reserved. The service does not read it at present. | None |
| `log-<date>.txt` | Log file. A new file starts each day. A file can grow to 5 MB. After that, the day's messages are not written to the file. The 7 newest files are kept. | Service and Agent |

- The service writes each state file atomically through `ConfigLoader.WriteJsonAtomic`. It writes a temporary file, flushes it to disk, and then replaces the old file. Use this method for every new state file.
- The service loads a rule or state file only if SYSTEM or the Administrators group owns it (`ConfigLoader.LoadTrustedJson`). It ignores other files and writes a warning to the log. Use this method for every new state file that the service reads.
- The output folder of a backup is the only record of which backups exist. Retention finds earlier backups from their names (`BackupNaming`).
- Both programs also write messages at level Information and higher to the Windows Event Log, with the source `BitShelter`.
- The Agent stores only the `BitShelter` scheduled task. The MSI deletes this task when you uninstall BitShelter.

### Scheduling

The service uses Quartz 4 with an in-memory job store. Quartz keeps no data on disk. The service rebuilds all triggers from the rules.

1. At startup, the service starts Quartz, starts logging, and loads `instances.json` and the newest rule file.
2. The service creates the triggers (`VssScheduler.CreateAllTriggers`) and then starts the named-pipe server.
3. After each rule change, the service clears the scheduler and creates all triggers again.

| Job | Trigger | Action |
|---|---|---|
| `SnapshotJob` | One trigger for each enabled rule | Creates the snapshots of one rule. If it fails, it tries again after 1 minute, up to the retry count of the rule. If the rule asks for it, the job restarts the `VSS` service before each retry. With the **Global** pruning strategy, the job first deletes the oldest snapshots of the rule on each volume that is at its limit (`VolumeLimitPruning`), so that VSS does not delete a snapshot of another rule or a System Restore point. When it succeeds and backups are on, it increments the snapshot counter and starts a `BackupJob` for the backup rules that are due. |
| `PruneJob` | Every minute | Deletes the snapshots that are older than the lifetime of their rule. It skips the snapshots that a running backup reads. |
| `BackupJob` | Started by `SnapshotJob` | Reads the input folders from the new snapshots and writes one backup for each due backup rule. Only one backup runs at a time. It saves each result in `backups.json` and writes it to the log and the Windows Event Log. |

#### Backup flow

1. `SnapshotBackupSources` maps each input folder to the same folder in the snapshot of its volume, for example `\\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy7\Docs`.
2. `BackupFileWalker` lists the files. It applies the include and exclude patterns to the normal path of each file, and does not follow folder junctions or symbolic links.
3. `BackupWriter` writes a folder copy or a zip or tar archive. For an encrypted backup, the archive goes through `OpenPgpEncryption` (an OpenPGP message with an integrity check). `SizeLimitStream` stops the backup when it exceeds its size limit.
4. `BackupEngine` writes to a `.partial` name, renames the result when it is complete, and then applies the count and total-size limits (`BackupRetention`).
5. The Agent asks for new results once a minute (`BackupNotifier`) and shows a notification when the rule asks for one.

#### Encryption and secrets

- Backups use only standard OpenPGP, so GnuPG can decrypt them. Do not add a custom encryption format. The 2018 AES and PBE code was removed in 0.2.0 because its output could not be decrypted with any tool. The `EncryptionAlgorithm` enum keeps its old values only so that old rule files still load.
- A public key is not secret. The rule stores it as ASCII-armored text.
- The Agent protects a passphrase with DPAPI (local machine scope) before it sends the rule (`PassphraseProtector`). The service unprotects the passphrase only when it writes the backup.

### VSS access

`VssClient` (`BitShelter.Common\VSS`) is a VSS requester. It calls `vssapi.dll` through BitShelter's own COM interop (`VSS\Interop`, .NET source-generated COM), declared from the Windows SDK headers `vsbackup.h` and `vss.h`. There is no third-party VSS library.

- Snapshots use one of two contexts. Both are persistent and client-accessible, so File Explorer shows them under Previous Versions:
  - `ClientAccessible` (default): no VSS writers.
  - `ClientAccessibleWriters` (the rule option **Ask applications to save their data first**): writers flush their data first, as for System Restore points. The requester uses non-component mode and the `VSS_BT_COPY` backup type, so it does not change the backup history of applications. A failed writer is logged as a warning and does not stop the snapshot.
- Each operation uses a new VSS session (`IVssBackupComponents`). In writer mode, a query on the session that then creates the snapshot makes `AddToSnapshotSet` fail. For this reason, `SnapshotJob` makes room in its own session before it creates the snapshot.
- The service sets the process COM security for a VSS requester at startup (`VssClient.InitializeProcessSecurity`), as documented by Microsoft.
- Rules from earlier versions can contain other VSS contexts. `SnapshotRule.SnapshotContext` maps a context with writers to `ClientAccessibleWriters` and any other context to `ClientAccessible`.

### Known gaps

- BitShelter has no restore function for backups. Users restore with standard tools (see the [user guide](docs/user-guide.md#backups)).
- An encrypted zip backup cannot use Zip64, so it is limited to 4 GB. Encrypted tar backups have no such limit.
