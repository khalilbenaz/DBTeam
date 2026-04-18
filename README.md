# DB TEAM

> Professional SQL Server IDE built with WPF.

![.NET](https://img.shields.io/badge/.NET-8-blueviolet)
![Platform](https://img.shields.io/badge/platform-Windows-blue)
![Status](https://img.shields.io/badge/status-WIP-orange)
![License](https://img.shields.io/badge/license-MIT-green)

**DB TEAM** is a modular Windows desktop IDE for Microsoft SQL Server. It ships connection management, object browsing, a T-SQL editor with IntelliSense, schema/data comparison, data generation, HTML documentation, a visual query profiler, a table designer, and an ER-style diagram viewer.

> ⚠️ **Work in progress.** Several modules are already functional but the product is not feature-complete. See [Roadmap](#roadmap).

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
        ├── DBTeam.Modules.ConnectionManager
        ├── DBTeam.Modules.ObjectExplorer
        ├── DBTeam.Modules.QueryEditor
        ├── DBTeam.Modules.ResultsGrid
        ├── DBTeam.Modules.SchemaCompare
        ├── DBTeam.Modules.DataCompare
        ├── DBTeam.Modules.TableDesigner
        ├── DBTeam.Modules.Profiler
        ├── DBTeam.Modules.Debugger          (stub — future)
        ├── DBTeam.Modules.Diagram
        ├── DBTeam.Modules.DataGenerator
        └── DBTeam.Modules.Documenter
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

### In progress / todo
- [ ] **T-SQL Debugger** (step-through, breakpoints, watches) — currently stubbed
- [ ] Export Results to Excel / CSV / JSON
- [ ] Improved Schema Compare (`CREATE`/`ALTER` column-level diff instead of `DROP`+`CREATE`)
- [ ] Data Compare: non-PK diff (`CHECKSUM`), chunking for large tables
- [ ] Diagram: draw FK arrows between table box borders (not center-to-center), drag tables, zoom
- [ ] Table Designer: edit existing tables (generate ALTER), FK/Index editors
- [ ] SQL IntelliSense: CTE / alias resolution, function signatures, inline docs
- [ ] Query history + favorites
- [ ] Multiple query tabs with per-tab DB selector (currently shared)
- [ ] Schema snapshot & compare against snapshot
- [ ] Profiler: graphical tree view of execution plan
- [ ] Unit tests (xUnit)
- [ ] Installer (MSIX / WiX)

### Known limitations
- No tests yet
- The Debugger module is a placeholder
- Diagram auto-layout is naive (grid) — no force-directed graph
- Schema Compare `ALTER TABLE` generation is a TODO (currently emits `-- TODO: manual ALTER TABLE`)

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
