## Testing BitShelter (Windows 11)

BitShelter uses Windows-only subsystems: VSS, Windows services, the registry, named pipes, and WinForms. Validation has two groups:

- **Automated tests**: they do not change system state. You can run them on a development computer or on a CI runner.
- **Privileged validation**: it needs administrator rights and can change system state (snapshots, services).

### Automated tests (default)

The tests use xunit v3 on Microsoft.Testing.Platform. `global.json` selects this test runner.

- `dotnet test --solution .\BitShelter.slnx -c Release`
- One test: `dotnet test --project .\BitShelter.Tests -c Release --filter-method "*Pipe*"`

The suite covers:

- IPC framing, serialization, and the message size limit (`BitShelter.Common/Ipc/SnapshotIpcStream.cs`)
- Named-pipe client/server round trips (`BitShelter.Service/Ipc/SnapshotPipeServer.cs`, `BitShelter.Agent/Ipc/SnapshotClient.cs`)
- The pipe ACL: only the service account and Administrators are granted access
- Atomic state-file writes and `rule_*.json` retention (`BitShelter.Service/Config/ConfigLoader.cs`)
- Quartz 4 runtime behavior: rule triggers rebuilt with a past start do not fire early, and jobs receive `RuleId` by property injection
- VSS interop: the native `VSS_SNAPSHOT_PROP` and `VSS_OBJECT_PROP` layouts match `vss.h`, and VSS errors are reported by name (no VSS service access)
- Schedule helpers, path filters, `schtasks` argument quoting, and small utilities
- Backup rules: the Every/Offset schedule, output names, and retention by count and total size
- The backup engine on normal folders: zip, tar.gz, folder copy, filters, the size limit, and old-backup deletion
- OpenPGP: passphrase and public-key backups decrypt with BouncyCastle as GnuPG would, and the integrity check passes
- The `%ProgramData%\BitShelter` ACL, the trusted-owner check for state files, and `backups.json` persistence
- Snapshot path mapping and the include/exclude pattern parser of the Agent
- `THIRD-PARTY-NOTICES.txt` names every runtime package of `Directory.Packages.props`
- The Agent rule editor and backup editor open and keep the settings of a rule. These tests catch designer errors, for example a control cast to an interface that it does not implement.

The named-pipe tests connect to a pipe that only Administrators can open, and the state-file tests change file owners to Administrators. Run the tests from an elevated shell. The Windows runners of GitHub Actions are elevated.

#### Privileged VSS test (opt-in)

`VssBackupIntegrationTests` creates a real shadow copy of the drive of `%TEMP%`, once without and once with VSS writers, changes a file, and checks that the backup contains the version from the snapshot. At the end, it deletes the shadow copy. `VssClientSequenceTests` also has a privileged test: it creates a real shadow copy with VSS writers, makes the writer steps after `DoSnapshotSet` fail, and checks that the shadow copy IDs are still returned. These tests are skipped unless you set `BITSHELTER_VSS_TESTS=1`. They run one at a time (`RealVssCollection`), because VSS takes one shadow copy set at a time. Run them from an elevated shell:

- PowerShell: `$env:BITSHELTER_VSS_TESTS = "1"; dotnet test --solution .\BitShelter.slnx -c Release`

### Benchmark

The benchmark measures each archive, compression, and OpenPGP encryption combination on 5 MB of random data. It writes `BitShelter_Bench_<ticks>.csv` to your Documents folder.

- `dotnet run --project .\BitShelter.Benchmark.Console -c Release`

### Code coverage

- `dotnet test --solution .\BitShelter.slnx -c Release --coverlet --coverlet-output-format cobertura --results-directory .\TestResults`

Coverage uses coverlet (MIT) through `coverlet.MTP`. The report goes to `.\TestResults\coverage.cobertura.*.xml`.

Coverage is low in this legacy codebase. A passing test run does not mean that all code is tested.

### Privileged validation (manual)

A normal test run cannot safely validate these areas:

- **VSS snapshot creation and deletion**, with and without VSS writers. These change the shadow copy state and need administrator rights.
- **Windows Service installation and lifecycle** (the MSI, or `sc.exe create/start/stop/delete`)
- **WinForms UI behavior** (requires a desktop session)

Manual checklist (Windows 11 x64):

1. Enable **System Protection** on the target drive(s).
2. Build and install the MSI (`dotnet build .\BitShelter.Setup -c Release`). Each build has a new ProductCode, and an MSI replaces only older versions. Before you install a new build of the same `<Version>`, uninstall the installed one, or both stay installed with the old binaries. Then confirm `Get-Service BitShelter` reports `Running` and `sc.exe qfailure BitShelter` shows the restart actions.
3. Start the Agent (it asks for elevation) and confirm it connects to the service (no "connection failed" banner).
4. Create a snapshot rule, wait for it to run, then verify shadow copies exist (`vssadmin list shadows`).
5. Browse "Previous versions" in Explorer and confirm restore works.
6. Add, update, and delete rules (including "delete snapshots") and confirm the UI reflects the changes.
7. Add a backup to a rule (an archive with OpenPGP passphrase encryption, **Every** = 1). After the next snapshot, confirm that the backup file is in the output folder, that `gpg --decrypt-files` decrypts it, and that the Agent shows a notification.
8. Confirm that `icacls "%ProgramData%\BitShelter"` lists only SYSTEM and Administrators.
9. Toggle **Run at startup** in the tray menu, sign out and in, and confirm the Agent starts without a UAC prompt.
10. Uninstall the MSI and confirm the service and the `BitShelter` scheduled task are gone and `%ProgramData%\BitShelter` is kept.
