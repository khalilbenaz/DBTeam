# DB TEAM — Project Context

> BMad-optimized codebase overview. Load this first before working on any story.

## Identity

- **Name**: DB TEAM
- **Purpose**: Professional SQL Server IDE on Windows desktop
- **Status**: Brownfield, ~80 % complete, not yet installable, not shipping
- **Repo**: https://github.com/khalilbenaz/DBTeam
- **License**: MIT

## Tech

| Concern | Choice |
|---|---|
| Runtime | .NET 8 (net8.0-windows) |
| UI | WPF, AvalonDock (docking), AvalonEdit (code editor), ModernWpfUI (theme chrome), MaterialDesignThemes (icons) |
| MVVM | CommunityToolkit.Mvvm (ObservableObject / RelayCommand / ObservableProperty) |
| DI | Microsoft.Extensions.DependencyInjection |
| SQL client | Microsoft.Data.SqlClient 7.x |
| SQL parse/format | Microsoft.SqlServer.TransactSql.ScriptDom (TSql160Parser + Sql160ScriptGenerator) |
| SMO | Microsoft.SqlServer.SqlManagementObjects |
| Fake data | Bogus |
| Data helper | Dapper |
| Logging | Serilog + Serilog.Sinks.File |

## Solution layout

```
DBTeam.sln
└── src/
    ├── DBTeam.Core         — domain models, abstractions, events, ServiceLocator. NO WPF / NO SQL.
    ├── DBTeam.Data         — SqlClient impls (connection/metadata/exec), DPAPI, JSON connection store. NO UI.
    ├── DBTeam.UI           — shared WPF controls
    ├── DBTeam.App          — WPF shell, DI bootstrap, theme + localization services, AvalonDock layout
    └── Modules/
        ├── DBTeam.Modules.ConnectionManager  ✅
        ├── DBTeam.Modules.ObjectExplorer     ✅
        ├── DBTeam.Modules.QueryEditor        ✅ (autocomplete, format, execute, plans)
        ├── DBTeam.Modules.ResultsGrid        ✅ (basic — no export yet)
        ├── DBTeam.Modules.SchemaCompare      ✅
        ├── DBTeam.Modules.DataCompare        ✅
        ├── DBTeam.Modules.TableDesigner      ✅ (create only — ALTER TODO)
        ├── DBTeam.Modules.Profiler           ✅
        ├── DBTeam.Modules.Debugger           ✅ (v1.4 — statement-level stepping, breakpoints, session state)
        ├── DBTeam.Modules.Diagram            ✅ (basic grid layout)
        ├── DBTeam.Modules.DataGenerator      ✅
        └── DBTeam.Modules.Documenter         ✅ (HTML)
```

## Architecture invariants

1. **Dependency direction**: App → Modules → Core/Data/UI. Modules **never** reference each other.
2. **Cross-module comms**: via `IEventBus` only. Events defined in `DBTeam.Core/Events/AppEvents.cs`.
3. **Module registration**: every module exposes `ModuleRegistration.Register(IServiceCollection s)` called from `DBTeam.App/App.xaml.cs → ConfigureServices`.
4. **View/VM resolution**: views get their VMs from `ServiceLocator.TryGet<T>()` (in Core). Views do **not** reference the App project.
5. **MVVM strict**: no logic in code-behind beyond view wiring + AvalonEdit glue.

## Key abstractions (Core)

- `IConnectionService` — CRUD saved connections, TestAsync
- `IDatabaseMetadataService` — GetDatabases/Tables/Views/Procedures/Functions/Columns/Indexes/ForeignKeys/ScriptObject
- `IQueryExecutionService` — ExecuteAsync, GetEstimatedPlanXmlAsync, ExecuteWithActualPlanAsync
- `IEventBus` — Publish/Subscribe
- `ISecretProtector` — DPAPI wrapper
- `IConnectionStore` — persistence port

## Persistence

- Connections: `%AppData%\DBTeam\connections.json` (passwords encrypted DPAPI, CurrentUser scope)
- Theme: `%AppData%\DBTeam\theme.json`
- Language: `%AppData%\DBTeam\lang.json`
- Logs: `%LocalAppData%\DBTeam\logs\app-*.log`

## UX

- Shell: AvalonDock with 2 side panes (Connections, Object Explorer) and document tabs
- Welcome tab at startup (quick-start cards, keyboard tips)
- Light (default) / Dark / System theme, ModernWpf, persisted
- EN / FR i18n via `Lang/*.xaml` merged dictionaries, live swap
- Icons: MaterialDesign PackIcon everywhere (menus, toolbars, context menus, tree)
- Keyboard: F5 execute · Ctrl+K format · Ctrl+Space completion · Ctrl+N connection · Ctrl+Q query

## Known gaps (roadmap anchors)

- **Not installable yet** — run via `dotnet run` only. No MSIX/WiX/MSI, no auto-update, no signing, not in Microsoft Store.
- **No test project** — 0 % coverage.
- **T-SQL Debugger** — functional since v1.4 (statement-level stepping, breakpoints, session state, PRINT/error capture). Step-into stored procedures is not implemented (would require full instrumentation — see DESIGN-NOTES).
- **Schema Compare ALTER** — column-level ALTER generation is a TODO (currently emits `-- TODO: manual ALTER TABLE`).
- **Results export** — no CSV / Excel / JSON export yet.
- **Table Designer** — create only, no edit-existing, no FK/Index editors.
- **Diagram** — FK lines are center-to-center (not box-edge routing), no drag, no zoom.
- **IntelliSense** — keyword+table+view+proc+func+column-after-dot, but no CTE/alias resolution, no signatures/inline docs, no snippet library.

## Conventions

- One type per file, PascalCase, namespace = folder path
- `{DynamicResource Key}` for all user-facing strings — **no hardcoded text** outside resource dictionaries
- No hardcoded colors in XAML — use `{DynamicResource SystemControl*}` (ModernWpf theme-aware) or `{StaticResource Brand.*}` (AppStyles.xaml)
- Icons: MaterialDesign PackIcon Kind, sizes standard (16 tree · 18 icon-button · 20 toolbar · 28 section header · 32 welcome card · 48+ empty state)
- Styles: reuse from `Themes/AppStyles.xaml` (`Card`, `SectionHeader`, `PrimaryButton`, `SuccessButton`, `DangerButton`, `IconButton`, `H1`, `H2`, `Caption`, `FieldLabel`, `Pill`)

## Build

```bash
dotnet restore
dotnet build DBTeam.sln
dotnet run --project src/DBTeam.App/DBTeam.App.csproj
```

## How to add a new module

1. `dotnet new wpflib -n DBTeam.Modules.Xxx -f net8.0 -o src/Modules/DBTeam.Modules.Xxx`
2. `dotnet sln add src/Modules/DBTeam.Modules.Xxx/*.csproj`
3. `dotnet add <csproj> reference ../../DBTeam.Core ../../DBTeam.Data ../../DBTeam.UI`
4. `dotnet add <csproj> package MaterialDesignThemes CommunityToolkit.Mvvm`
5. Create `ModuleRegistration.cs`, `ViewModels/XxxViewModel.cs`, `Views/XxxView.xaml(.cs)`
6. Register in `src/DBTeam.App/App.xaml.cs → ConfigureServices`
7. Wire a menu command in `MainViewModel` publishing `OpenDocumentRequest`
