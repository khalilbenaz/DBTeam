# DB TEAM

> Professional SQL Server IDE built with WPF · **v2.1.1**

[![.NET](https://img.shields.io/badge/.NET-8-blueviolet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-Windows-blue)](https://github.com/khalilbenaz/DBTeam/releases)
[![Release](https://img.shields.io/github/v/release/khalilbenaz/DBTeam?include_prereleases&label=release)](https://github.com/khalilbenaz/DBTeam/releases)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![CI](https://github.com/khalilbenaz/DBTeam/actions/workflows/ci.yml/badge.svg)](https://github.com/khalilbenaz/DBTeam/actions/workflows/ci.yml)
[![CodeQL](https://github.com/khalilbenaz/DBTeam/actions/workflows/codeql.yml/badge.svg)](https://github.com/khalilbenaz/DBTeam/actions/workflows/codeql.yml)
[![Website](https://img.shields.io/badge/website-khalilbenaz.github.io%2FDBTeam-1E88E5)](https://khalilbenaz.github.io/DBTeam/)

**DB TEAM** is a modular, bilingual (EN/FR), themeable Windows desktop IDE for Microsoft SQL Server.

Connection management · Object Explorer · T-SQL editor with autocomplete + format · Schema Compare with column-level ALTER · Data Compare · Table Designer (create + edit) · Query Profiler with plan tree · Data Generator · HTML Documenter · Interactive ER Diagram · Query History & favorites · Session restore.

**📥 Download installer**: [DBTeam-Setup-2.1.1.exe](https://github.com/khalilbenaz/DBTeam/releases/latest) or [portable ZIP](https://github.com/khalilbenaz/DBTeam/releases/latest)
**🌐 Website**: <https://khalilbenaz.github.io/DBTeam/>
**📚 Docs**: [Install](docs/INSTALL.md) · [User guide](docs/USER-GUIDE.md) · [Architecture](docs/ARCHITECTURE.md) · [Modules](docs/MODULES.md) · [Changelog](CHANGELOG.md)

---

## ✨ Features

### Connection & Navigation
- **Connection manager** with Windows / SQL / Azure AD Integrated / Azure AD Password auth modes
- **Encrypted credential storage** via DPAPI (current user scope)
- **Persistent connection list** in `%AppData%\DBTeam\connections.json`
- **Object Explorer** — hierarchical, lazy-loaded tree (Server → Databases → Tables / Views / Stored Procedures / Functions), icons per object type, right-click actions, disconnect, multi-connection

### Query Editor
- **AvalonEdit** with T-SQL syntax highlighting, line numbers
- **Autocomplete / IntelliSense** — keywords + live tables/views + columns after `.`
- **F5** to execute, **Ctrl+K** to format SQL (via ScriptDom), **Ctrl+Space** to trigger suggestions
- Multi-tab documents (AvalonDock), dockable panels
- **Results grid** with multiple result sets, messages tab, timing and row-count

### Database Tools
- **Schema Compare** — compare source/target databases across tables, views, procs, functions; dual script pane; **Generate Sync Script** opens the diff as T-SQL
- **Data Compare** — row-level diff between two tables via primary key; produces `INSERT / UPDATE / DELETE` under a transaction
- **Table Designer** — visual column editor (type, length, nullable, identity, PK, default), reorder, **Generate DDL**
- **Query Profiler** — estimated plan (`SHOWPLAN_XML`) and actual plan (`STATISTICS XML`); operator breakdown with cost, estimated rows, actual rows, logical reads
- **Data Generator** — **Bogus**-powered fake data per column type and name hints (email, phone, name, city, company…); preview, export as script, insert with progress
- **Documenter** — generates a standalone HTML documentation of the database (tables, columns, PK/FK/indexes, views, procs, functions) saved under `Documents\DBTeam-Docs\`
- **Database Diagram** — visual canvas showing all tables with their columns and FK lines

### UX
- **Light / Dark / System** theme (ModernWpfUI), persisted
- **Localization** EN / FR with hot-swap at runtime, persisted
- **Welcome screen** with quick-start cards
- **Material Design icons** throughout menus, toolbars, context menus

---

## 🏗️ Architecture

```
DBTeam.sln
└── src/
    ├── DBTeam.Core            → domain models, abstractions, event bus, service locator
    ├── DBTeam.Data            → SqlClient implementations (connection / metadata / exec), DPAPI, JSON store
    ├── DBTeam.UI              → shared WPF controls
    ├── DBTeam.App             → WPF shell, DI, theme + localization services, AvalonDock layout
    └── Modules/
        ├── DBTeam.Modules.ConnectionManager   connect, save, encrypt
        ├── DBTeam.Modules.ObjectExplorer      lazy tree, script, disconnect
        ├── DBTeam.Modules.QueryEditor         AvalonEdit + autocomplete + F12 + snippets + validation
        ├── DBTeam.Modules.ResultsGrid         CSV / Excel / JSON / XML export
        ├── DBTeam.Modules.SchemaCompare       column-level ALTER + FK/index diff
        ├── DBTeam.Modules.DataCompare         PK-based row diff + merge script
        ├── DBTeam.Modules.TableDesigner       create + load existing
        ├── DBTeam.Modules.Profiler            plan tree + operator stats
        ├── DBTeam.Modules.Debugger            statement-level stepping + breakpoints + session state
        ├── DBTeam.Modules.Diagram             drag + zoom + PNG export
        ├── DBTeam.Modules.DataGenerator       Bogus-powered fake data
        ├── DBTeam.Modules.Documenter          HTML schema documentation
        ├── DBTeam.Modules.Admin               logins / users / indexes / backup / restore
        ├── DBTeam.Modules.Terminal            embedded CLI (pwsh / claude / gh / sqlcmd / …)
        └── DBTeam.Modules.AiAssistant         BYO-key chat (Anthropic / OpenAI / Ollama / Azure)
```

**Pattern**: MVVM with `CommunityToolkit.Mvvm` (ObservableObject, RelayCommand, ObservableProperty). **DI**: `Microsoft.Extensions.DependencyInjection`. **Messaging**: custom in-memory `IEventBus` (`ConnectionOpenedEvent`, `OpenQueryEditorRequest`, `OpenDocumentRequest`, `ShowPaneRequest`, `ConnectionsChangedEvent`).

Each **module** exposes a `ModuleRegistration.Register(IServiceCollection)` static method and is composed via the App's DI bootstrap. Views resolve their ViewModels through `ServiceLocator` (UI-layer pattern, keeps modules decoupled from the shell).

### Key abstractions

| Interface | Purpose |
|---|---|
| `IConnectionService` | CRUD on saved `SqlConnectionInfo`, `TestAsync` |
| `IDatabaseMetadataService` | `GetDatabases / GetTables / GetViews / GetProcedures / GetFunctions / GetColumns / GetIndexes / GetForeignKeys / ScriptObject` |
| `IQueryExecutionService` | `ExecuteAsync`, `GetEstimatedPlanXmlAsync`, `ExecuteWithActualPlanAsync` |
| `IEventBus` | Publish/subscribe app-wide events |
| `ISecretProtector` | DPAPI-backed encryption for passwords |

---

## 🧰 Stack

| Concern | Library |
|---|---|
| UI framework | WPF (.NET 8, net8.0-windows) |
| Docking | [Dirkster.AvalonDock](https://github.com/Dirkster99/AvalonDock) |
| Code editor | [AvalonEdit](https://github.com/icsharpcode/AvalonEdit) |
| Theme chrome | [ModernWpfUI](https://github.com/Kinnara/ModernWpf) |
| Icons | [MaterialDesignThemes](http://materialdesigninxaml.net/) (PackIcon) |
| MVVM | [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) |
| SQL client | [Microsoft.Data.SqlClient](https://github.com/dotnet/SqlClient) |
| SQL parser / generator | [Microsoft.SqlServer.TransactSql.ScriptDom](https://www.nuget.org/packages/Microsoft.SqlServer.TransactSql.ScriptDom) |
| SMO | [Microsoft.SqlServer.SqlManagementObjects](https://learn.microsoft.com/sql/relational-databases/server-management-objects-smo/) |
| Fake data | [Bogus](https://github.com/bchavez/Bogus) |
| Data helper | [Dapper](https://github.com/DapperLib/Dapper) |
| Logging | [Serilog](https://serilog.net/) |

---

## 🚀 Getting Started

### Prerequisites
- **Windows 10 / 11**
- **.NET 8 SDK** — <https://dotnet.microsoft.com/download>
- **SQL Server** reachable (local, remote, Azure, Express, or LocalDB)

### Install (end user)

Download the latest `DBTeam-<version>-win-x64.zip` from the [Releases page](https://github.com/khalilbenaz/DBTeam/releases), extract, and run `DBTeam.App.exe`. Full instructions in [docs/INSTALL.md](docs/INSTALL.md). No .NET install required — the binary is self-contained.

### Build from source

```bash
git clone https://github.com/khalilbenaz/DBTeam.git
cd DBTeam
dotnet restore
dotnet build DBTeam.sln
dotnet run --project src/DBTeam.App/DBTeam.App.csproj
```

Produce your own installable binary:
```powershell
pwsh ./scripts/publish.ps1
```

### First use
1. **File → New Connection** (or Ctrl+N) — fill server, auth mode, credentials
2. Click **Load** to enumerate databases, select one
3. **Test** → **Connect**
4. The Object Explorer activates automatically — expand your database
5. Double-click a table to get a `SELECT TOP 100`, or right-click → **New Query Here**

### Keyboard shortcuts

| Key | Action |
|---|---|
| `F5` | Execute query |
| `Ctrl+K` | Format SQL |
| `Ctrl+Space` | Autocomplete |
| `Ctrl+N` | New connection |
| `Ctrl+Q` | New query |

---

## 🌐 Localization

Languages: **English (en-US)** and **Français (fr-FR)**, switched live via **View → Language**.

Resources live in `src/DBTeam.App/Lang/*.xaml` as `ResourceDictionary` with `<sys:String x:Key="...">`. To add a language:

1. Copy `en-US.xaml` → `xx-YY.xaml`, translate the values
2. Add a menu entry in `Shell/MainWindow.xaml` under *View → Language*
3. Rebuild — the `LocalizationService` merges the dictionary at runtime

---

## 🎨 Theming

Themes are managed by **ModernWpfUI**. The `ThemeService` persists the choice in `%AppData%\DBTeam\theme.json` and applies it on startup. Reusable styles are in `src/DBTeam.App/Themes/AppStyles.xaml`:

- Brand brushes (`Brand.Primary`, `Brand.Source`, `Brand.Target`, `Brand.Success`, `Brand.Danger`…)
- Typography (`H1`, `H2`, `Caption`, `FieldLabel`)
- Reusable controls (`Card`, `SectionHeader`, `Pill`, `PrimaryButton`, `SuccessButton`, `DangerButton`, `IconButton`)
- Implicit DataGrid / ComboBox / TextBox polish

---

## 🔐 Security notes

- Passwords are encrypted with **DPAPI** (`ProtectedData.Protect`) with a static entropy, scoped to `CurrentUser` — credentials cannot be decrypted by another Windows user on the same machine.
- `TrustServerCertificate` defaults to `true` (developer-friendly). **Disable for production** and provision a valid TLS certificate on the SQL Server.
- The app ships with `ApplicationName=DBTeam` so queries can be traced in `sys.dm_exec_sessions`.

---

## 📚 Documentation

- [docs/INSTALL.md](docs/INSTALL.md) — install & first run
- [docs/USER-GUIDE.md](docs/USER-GUIDE.md) — end-user guide
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — layers, DI, event bus, data flows
- [docs/MODULES.md](docs/MODULES.md) — per-module reference
- [docs/bmad/PRD.md](docs/bmad/PRD.md) — product requirements
- [docs/bmad/EPICS.md](docs/bmad/EPICS.md) — detailed backlog (stories)
- [CHANGELOG.md](CHANGELOG.md) — release notes

## 🗺️ Roadmap

### Done
- [x] Shell + AvalonDock + DI
- [x] Connection Manager (dialog + panel, auto-refresh, encrypted store)
- [x] Object Explorer (lazy tree, icons, context menu, multi-connection)
- [x] Query Editor (AvalonEdit, T-SQL highlighting, execute, format, autocomplete, estimated plan)
- [x] Results Grid
- [x] Schema Compare
- [x] Data Compare
- [x] Table Designer
- [x] Query Profiler
- [x] Database Diagram
- [x] Data Generator (Bogus)
- [x] HTML Documenter
- [x] Theming (Light/Dark/System)
- [x] i18n (EN/FR)

### Shipped since v1.0
- [x] **T-SQL Debugger** (statement-level stepping, breakpoints, session state) — v1.4
- [x] **Results export** CSV / Excel / JSON / XML — v1.0.1 / v1.5
- [x] **Schema Compare** column-level `ALTER` (ADD/DROP/ALTER COLUMN, FK diff, index diff) — v1.2
- [x] **Diagram** drag, `Ctrl`+wheel zoom, PNG export — v1.2
- [x] **Table Designer**: create + **load existing** tables — v1.3
- [x] **Query History & favorites** — v1.1
- [x] **Profiler plan tree** with icons per physical op — v1.3
- [x] **25 unit tests** on CI — v1.0
- [x] **Installer** `.exe` (Inno Setup) + portable ZIP — v1.2
- [x] **Snippets** (16 Tab-triggered templates) — v1.5
- [x] **Go to Definition** (F12) — v1.5
- [x] **Real-time validation** (ScriptDom squiggles) — v1.5
- [x] **Administration panel** (logins, users, roles, permissions, indexes, slow queries, sessions, backup/restore scripts) — v1.5
- [x] **Terminal** (pwsh / claude / gh / sqlcmd / any CLI, embedded) — v1.5
- [x] **Session save/restore** of open tabs — v1.3
- [x] **Accessibility** baseline (AutomationProperties, focus visual) — v1.3
- [x] **CodeQL + Dependabot** — v1.0.1

### Shipped in v1.6 / v1.7 / v1.8
- [x] **AI assistant** inline panel (BYO-key Anthropic / OpenAI / Azure / Ollama) — v1.6
- [x] **Query Builder** (visual table + column picker with WHERE / ORDER BY / TOP / DISTINCT) — v1.7
- [x] **Data Compare** `CHECKSUM` mode for tables without PK — v1.7
- [x] **Schema snapshot** and offline compare against snapshot — v1.7
- [x] **Monitoring** real-time DMV polling (sessions / requests / buffer cache hit % / PLE) + top waits — v1.7
- [x] **Import CSV** with auto type detection and bulk insert — v1.7
- [x] **Git source control** integration (repo picker, .sql list, commit / pull / push, log) — v1.7
- [x] **MSIX manifest** scaffold — v1.8
- [x] **winget manifests** ready for submission — v1.8
- [x] **Code signing** workflow step (Azure Trusted Signing, gated on secret) — v1.8
- [x] **LocalDB integration tests** — v1.8

### Shipped in v1.9
- [x] **IntelliSense alias resolution** — `alias.column` autocomplete resolves through `FROM/JOIN table AS alias`; CTE names extracted
- [x] **Profiler graphical plan** — Graph tab with canvas + per-operator boxes (cost, estimated rows, actual rows, logical reads) — layered layout
- [x] **Per-tab DB selector** — explicit refresh button next to each Query Editor's database combo
- [x] **Master-detail** — right-click cell → `FollowRelation` command infers FK target table and opens a filtered `SELECT` in a new tab
- [x] **Reports & pivots** — one-click `PIVOT` skeleton generator from the active result set
- [x] **Auto-update scaffold** — `UpdateService` ready for Velopack (activation blocked on signing)
- [x] **Automated screenshots** — `DBTeam.Screenshots` console project scaffold for FlaUI.UIA3

### Shipped in v2.0
- [x] **Function signatures in autocomplete** — typing `(` after a proc/function opens an `OverloadInsightWindow` with parameter list (name, type, direction) and return type
- [x] **Debugger step-into** — `EXEC` statements expand inline: the proc body is fetched, parsed, and its statements spliced into the Steps list so you can step through them

### Waiting on external credentials
- [ ] **Velopack auto-update activation** — scaffold `UpdateService` ready; activation requires a code-signed release (see `docs/SIGNING.md`)
- [ ] **FlaUI automated screenshots** — scaffold `DBTeam.Screenshots` ready; activation requires actually capturing images (manual for now)

All scaffolds + design notes: [docs/bmad/DESIGN-NOTES.md](docs/bmad/DESIGN-NOTES.md) · signing paths in [docs/SIGNING.md](docs/SIGNING.md).

Detailed implementation plans: [docs/bmad/DESIGN-NOTES.md](docs/bmad/DESIGN-NOTES.md).

### Known limitations
- Diagram auto-layout is a naive grid — no force-directed graph yet.
- Debugger cannot interrupt mid-statement or step into stored procedures (statement-level only).
- Binary is not yet code-signed → SmartScreen warns on first download.

---

## 🤝 Contributing

Contributions are welcome. Please:

1. Fork, branch from `main` (`feat/xxx`, `fix/xxx`)
2. Keep modules self-contained — respect the existing `ModuleRegistration` pattern
3. Follow MVVM — no logic in code-behind beyond view wiring
4. Open a PR with a clear description

### Project conventions
- **Namespace** = folder path (module-prefixed)
- **Views/ViewModels** folders per module
- **DataContext** resolved via `ServiceLocator.TryGet<T>()` in code-behind (views do not reference the App project)
- **Cross-module communication** through `IEventBus` events, never direct references

---

## 📜 License

MIT — see [LICENSE](LICENSE) if present, otherwise consider the code MIT-licensed by the author.

---

## 🙏 Acknowledgements

- [Dirkster99](https://github.com/Dirkster99) — AvalonDock
- [icsharpcode](https://github.com/icsharpcode) — AvalonEdit
- [Kinnara](https://github.com/Kinnara) — ModernWpf
- [Material Design In XAML Toolkit](http://materialdesigninxaml.net/)
- [Bogus](https://github.com/bchavez/Bogus)
