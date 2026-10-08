# BitShelter user guide

This guide tells you how to install, set up, and use BitShelter. For a short overview, see the [README](../README.md).

- [How it works](#how-it-works)
- [Install](#install)
- [Set up snapshots](#set-up-snapshots)
- [Default settings](#default-settings)
- [Backups](#backups)
- [Advanced options](#advanced-options)
- [Upgrade](#upgrade)
- [Limitations](#limitations)
- [FAQ](#faq)
- [Best practices](#best-practices)
- [Glossary](#glossary)

## How it works

![How BitShelter works: 1. snapshots on the same drive, 2. backups to another drive, 3. cloud copies through a sync folder](../Resources/how-it-works.svg)

| Operation | What it does | Where | Size | Speed |
|---|---|---|---|---|
| **Snapshot** | Records your files at one point in time, like **Ctrl+Z** for files. You [browse and restore](../Resources/Windows_PreviousVersions.png) earlier versions from File Explorer. | Same drive as the data | Only the changes between two snapshots | Almost instantaneous |
| **Backup** | Copies files from a snapshot to another place, like **Ctrl+C** and **Ctrl+V**. Optionally as a compressed and encrypted archive. | Another drive, a network share, a removable device | At most the size of the data | Minutes to hours |
| **Cloud copy** | BitShelter does not connect to cloud storage. Save backups in the sync folder of a cloud provider, and the provider uploads them. | In the cloud | The same as the backups | Depends on the upload speed |

A snapshot does not copy your data. Windows records where the data is and writes new data to free space ([copy-on-write](https://en.wikipedia.org/wiki/Copy-on-write)). You can continue to work during snapshots and backups.

BitShelter has two parts:

- **BitShelter service**: a Windows service that runs as LocalSystem and starts with Windows. It creates and deletes snapshots and makes backups. It does not need the Agent.
- **BitShelter Agent**: a tray app that runs as administrator. You use it to edit rules, and it shows backup notifications. The **Run at startup** option in its tray menu starts it at sign-in through a scheduled task, without a UAC prompt.

## Install

### With the installer (recommended)

1. Install the [.NET 10 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/10.0). If it is missing, the installer tells you and stops.
2. Run `BitShelter-<version>-x64.msi` from the [releases page](https://github.com/sm18lr88/BitShelter/releases). It installs to `C:\Program Files\BitShelter`, starts the `BitShelter` service, and adds **BitShelter Agent** to the Start menu.
3. Start **BitShelter Agent** from the Start menu.

Uninstall removes the service, the files, and the Agent startup task. It keeps your snapshots, and your settings and logs in `%ProgramData%\BitShelter`.

### From source

You need the .NET 10 SDK (`global.json` pins the version). In an elevated shell:

1. Build and test: `dotnet build .\BitShelter.slnx -c Release`, then `dotnet test --solution .\BitShelter.slnx -c Release`.
2. Build the installer: `dotnet build .\BitShelter.Setup -c Release`. The MSI is in `BitShelter.Setup\bin\x64\Release\`.

To run without the installer, publish both apps and register the service:

```powershell
dotnet publish .\BitShelter.Service\BitShelter.Service.csproj -c Release -r win-x64 --self-contained false -o .\artifacts\service
dotnet publish .\BitShelter.Agent\BitShelter.Agent.csproj -c Release -r win-x64 --self-contained false -o .\artifacts\agent
sc.exe create BitShelter binPath= "C:\full\path\to\artifacts\service\BitShelter.Service.exe" start= auto obj= LocalSystem
sc.exe failure BitShelter reset= 86400 actions= restart/60000/restart/60000/restart/60000
sc.exe start BitShelter
.\artifacts\agent\BitShelter.Agent.exe
```

## Set up snapshots

1. Double-click the BitShelter icon ![](../Resources/BitShelter_Icon.png) in the notification area.
2. In the [main window](../Resources/BitShelter.Agent_Rules.png), click **Add Schedule**.
3. Turn on System Protection for each drive that you want to snapshot. You do this only one time per drive:
   1. In the [General tab](../Resources/BitShelter.Agent_General.png), click **Enable other Drive(s)**.
   2. In the [System Protection dialog](../Resources/Windows_SystemProtection.png), select the drive and click **Configure**.
   3. In the [next dialog](../Resources/Windows_SystemProtectionConfigure.png), click **Turn on system protection**, select the disk space for snapshots, and click **OK**.
4. In the **General** tab, select the drives and the **Lifetime** (how long snapshots are kept).
5. In the [Schedule tab](../Resources/BitShelter.Agent_Schedule.png), check the schedule.
6. Click **Create**.
7. Later, open the **Previous Versions** tab of a folder on that drive and [check that the snapshots are there](../Resources/Windows_PreviousVersions.png).

### Snapshot limit

Windows keeps at most 64 snapshots per drive by default. When a drive is full, Windows deletes the oldest snapshot, which can be a System Restore point. The default rule (every 4 hours, kept 1 week) makes 42 snapshots, so it fits.

For more snapshots, for example every hour for a week (168), click **Raise limit** in the **General** tab and set **512**, the Windows maximum. This changes a setting for the whole computer.

## Default settings

A new rule starts with these settings. You can change all of them.

| Setting | Default | Why |
|---|---|---|
| Schedule | Every 4 hours, every day | BitShelter does not catch up on missed times. If the computer is off or asleep at a scheduled time, that snapshot is skipped. With a single daily time, a computer that is often off at that time gets few snapshots. |
| Lifetime | 1 week | 42 snapshots per drive, under the Windows default limit of 64. |
| Retry count | 3 | A failed snapshot is tried again, one minute apart. |
| Restart VSS on failure | Off | A restart can make other programs that use VSS at that time fail. |
| Pruning strategy | Global | Keeps room under the snapshot limit by deleting this rule's oldest snapshot, instead of letting Windows delete a System Restore point. |
| Backups | Off | You must select the folders and the output folder. |
| Total backup size | 0 (no limit) | The **Keep N backups** setting of each backup controls retention. |

## Backups

The rule must snapshot the drive of each folder that you back up, because the backup reads from the snapshot.

1. In the rule editor, open the [Backup tab](../Resources/BitShelter.Agent_Backup.png) and select **Enable backups**.
2. Click **Add**. In the [Backup dialog](../Resources/BitShelter.Agent_BackupRule.png):
   - Type a **Name** and add the **Folders to back up**.
   - Select an **Output folder**. It must not be inside a folder that you back up.
   - Optionally, add **Include patterns** and **Exclude patterns**, one per line. A pattern is a [glob](https://github.com/dazinator/DotNet.Glob#patterns), for example `**/*.docx`. For a regular expression, start the line with `regex:`. With no include pattern, all files are included.
   - In **When**, set after how many snapshots a backup runs, and the first snapshot that gets one. Snapshot numbers start at 0 when you enable backups.
   - In **Limits**, set the maximum size of one backup and how many backups to keep. 0 means no limit.
   - In **Format**, select **Create an archive** and the type, or clear it to copy the files into a folder.
   - In **Encryption**, select **Public key** or **Passphrase**, and the cipher. Both make a standard OpenPGP file.
   - Click **OK**.
3. Optionally, set **Total Max. Size** for all backups of the rule (0 = no limit), and select **Enable desktop notifications**.
4. Click **Create** or **Save**.

A backup is saved as `<output folder>\<rule name> - <backup name>\<yyyyMMdd-HHmmss><extension>`, for example `D:\Backups\Daily - Documents\20261008-140000.zip.gpg`. It has a `.partial` name until it is complete.

Formats: folder copy, zip (stored, deflate, bzip2, PPMd), or tar (none, gzip, bzip2, lzip). Ciphers: AES128/256, Blowfish, Camellia128/256, CAST5, 3-DES, and Twofish.

> [!WARNING]
> Keep your OpenPGP private key or passphrase somewhere other than the backed-up computer. Without it, nobody can decrypt the backups.

> [!CAUTION]
> The service runs as LocalSystem, so it can read files that other users cannot. A backup gets the permissions of its output folder. Use an output folder that only trusted users can read, or use encryption.

To restore from a backup:

- Folder copy: copy the files back.
- Zip: open it in File Explorer or 7-Zip.
- Tar: run `tar -xf <file>` (Windows 11 includes `tar`).
- Encrypted: run `gpg --decrypt-files <file>.gpg` with [GnuPG](https://gnupg.org/) or use Kleopatra, then extract the archive.

The Agent protects a backup passphrase with DPAPI (local machine scope). The plain passphrase is never written to disk or sent over the named pipe. Because any process on the computer can unprotect it, the service lets only SYSTEM and Administrators read `%ProgramData%\BitShelter`.

## Advanced options

The [Advanced tab](../Resources/BitShelter.Agent_Advanced.png) of the rule editor has these options:

- **On failure: retry count**: how many times the service tries a failed snapshot again, one minute apart.
- **On failure: restart the VSS service**: before each retry, restart the Volume Shadow Copy service. This can fix a VSS service in a bad state, but other programs that use VSS at that moment can fail. Off by default.
- **Pruning strategy**:
  - **Local**: delete the rule's snapshots only when they are older than the lifetime.
  - **Global**: also, before each snapshot, if a drive is at its limit, delete this rule's oldest snapshot on that drive. Default for new rules. Rules from earlier versions use **Local**.

## Upgrade

- MSI install: run the new MSI. It replaces the earlier version, including installs from the 2018 installer.
- Manual install: stop the `BitShelter` service, replace the files, and start the service.

Your settings, snapshots, and logs are kept. The service keeps the 10 newest `rule_*.json` files.

From version 0.2.0, only SYSTEM and Administrators can access `%ProgramData%\BitShelter`. The service ignores a rule or state file that SYSTEM or Administrators does not own. If you copy a rule file in by hand, run `icacls <file> /setowner *S-1-5-32-544`.

## Limitations

- At most [512 snapshots per drive](https://learn.microsoft.com/en-us/windows/win32/backup/registry-keys-for-backup-and-restore#maxshadowcopies).
- You must turn on System Protection by hand for each drive.
- Encryption works only with archives. An encrypted zip can be at most 4 GB; use tar for larger encrypted backups.
- BitShelter has no restore function for backups. Use the standard tools above.
- If a file cannot be read during a backup, it is skipped and logged.
- BitShelter cannot update itself.

## FAQ

**The Agent says "Cannot reach the BitShelter service".**

![](../Resources/BitShelter.Agent_ConnectionFailed.png)

1. Check that the **BitShelter** service is running: `Get-Service BitShelter` in PowerShell, or `services.msc`.
2. Check that the Agent runs as administrator. The service accepts only administrators.

**Does BitShelter run when the Agent is closed?**

Yes. The service does all the work. To start the Agent at sign-in, right-click its tray icon and check **Run at startup**.

**Snapshots do not work on Windows with Boot Camp on a Mac.**

See [this guide](https://www.edandersen.com/2015/07/06/windows-10-on-mac-bootcamp-fixes/), section *System Restore, Restore Points and Windows 7 style backups do not work*.

## Best practices

Follow the 3-2-1 rule: keep 3 copies of your data, on 2 different types of storage, with 1 copy off-site. Snapshots protect against mistakes, not against a failed drive, so also keep backups on another drive.

## Glossary

- **Agent**: the BitShelter tray app (`BitShelter.Agent.exe`).
- **Backup rule**: the settings of one backup in a rule: folders, output folder, patterns, frequency, limits, format, and encryption.
- **Lifetime**: how long the service keeps the snapshots of a rule.
- **Pruning**: deleting snapshots that are older than their lifetime. The service checks every minute.
- **Rule**: drives, a schedule, and a lifetime. The Agent also calls it a "schedule".
- **Service**: the `BitShelter` Windows service (`BitShelter.Service.exe`).
- **Snapshot** (shadow copy): a read-only copy of a drive at one point in time.
- **System Protection**: the Windows setting that reserves disk space for snapshots on a drive.
- **VSS** (Volume Shadow Copy Service): the Windows component that creates snapshots.
