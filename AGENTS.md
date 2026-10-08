## BitShelter (Windows 11 only)

BitShelter runs on **Windows 11 (x64)** with **.NET 10**. The build policy mirrors SuperMemoAssistant (`S:\SuperMemo\SMA`) so the projects can be vendored into SMA unchanged.

### Build / run

- Build: `dotnet build .\BitShelter.slnx -c Release`
- Tests: `dotnet test --solution .\BitShelter.slnx -c Release` (Microsoft.Testing.Platform, xunit v3; see `TESTING.md`)
- Agent (WinForms tray app, requires elevation): `BitShelter.Agent`
- Service (Windows Service, runs as LocalSystem): `BitShelter.Service` (service name is `BitShelter`)
- App data: `%ProgramData%\BitShelter`
- Architecture overview (components, IPC, state files, scheduling): `ARCHITECTURE.md`

### Architecture constraints

- Do **not** reintroduce WCF. IPC is **Named Pipes** (see `BitShelter.Common\Ipc`). The pipe ACL admits only the service account and elevated Administrators. Never widen it to Authenticated Users, because the service can delete every shadow copy.
- IPC messages are capped at `SnapshotIpcProtocol.MaxMessageBytes`.
- Service state files (`instances.json`, `backups.json`, `rule_*.json`) are written atomically through `ConfigLoader.WriteJsonAtomic` and read through `ConfigLoader.LoadTrustedJson`, which ignores files that SYSTEM or Administrators do not own. Keep both for any new state file.
- The service restricts `%ProgramData%\BitShelter` to SYSTEM and Administrators at startup (`AppDataSecurity`). Do not widen it: the LocalSystem service runs whatever rule files it finds there.
- Backup encryption is standard OpenPGP only (`OpenPgpEncryption`), so users can restore with GnuPG. Do not add custom encryption formats. Passphrases are stored only DPAPI-protected (`PassphraseProtector`).
- The backup engine lives in `BitShelter.Common\Backup` and has no VSS dependency. VSS path mapping and scheduling stay in `BitShelter.Service`.
- VSS integration is BitShelter's own COM interop on .NET source-generated COM (`BitShelter.Common\VSS\Interop`), declared from the Windows SDK headers `vsbackup.h` and `vss.h`. Keep vtable order exact. Do not add a third-party VSS wrapper. Processes must be x64 (VSS does not support WOW64).
- Scheduling uses **Quartz 4** with an in-memory store. The service rebuilds the triggers from the saved rules.
- **Open source only.** Every dependency and build tool must be under an OSI-approved license. Do not use Syncfusion or other proprietary UI kits. Stock WinForms controls replaced the old Syncfusion controls. Do not use packages that require you to accept the OSMF EULA. For this reason, WiX stays on 5.x, and the benchmark exports CSV instead of using NPOI. Coverage uses coverlet, not Microsoft's code coverage extension.
- The service is the background component and needs no UI. The Agent is optional. It starts elevated at logon through the `BitShelter` scheduled task when the user enables "Run at startup".
- Installer: `BitShelter.Setup` (WiX 5 SDK project, not in the solution). Build with `dotnet build .\BitShelter.Setup -c Release`. Keep the UpgradeCode unchanged so new MSIs upgrade old installs.

### Repository conventions

- Target framework is `net10.0-windows`, x64 only, Windows 11 only.
- `TreatWarningsAsErrors` is on. `Nullable` and `ImplicitUsings` are off for product code (legacy sources) and on for tests, as in SMA.
- Package versions live only in `Directory.Packages.props` (central package management). Do not put `Version` on a `PackageReference`.
- `THIRD-PARTY-NOTICES.txt` lists every component that the MSI ships, with its license. When you add or update a runtime package, update this file. `ThirdPartyNoticesTests` fails if a runtime package in `Directory.Packages.props` is missing.
- Do not add third-party documents, images, logos, or tool files to the repository. Use links instead. The icon (`BitShelter.Agent/BitShelter.ico`), the screenshots, and `Resources/how-it-works.svg` are project-owned.
- Releases: see `RELEASING.md`. A `v*` tag runs `.github/workflows/release.yml`. Keep `<Version>` in `Directory.Build.props` and the `CHANGELOG.md` section in step with the tag.
- Keep source files under about 250 lines when reasonable (designer-generated files and the vendored `VssClient` are known exceptions).
- Keep changes focused. Update `docs/user-guide.md` when behavior changes, and `README.md` when requirements or the overview change. Keep the README short. Update `ARCHITECTURE.md` when components, IPC, state files, or scheduling change.
- Write docs in plain, simple English: short sentences, active voice, one term for each concept.
