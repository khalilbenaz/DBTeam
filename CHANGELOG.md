# Changelog

All notable changes to DB TEAM are documented here. Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versioning: [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [2.1.0] — 2026-04-18 — Conditional breakpoints + full i18n polish

### Added — Conditional breakpoints
- New `Breakpoint` model (`Line`, `Condition`, `HitCount`) replaces the former `int` list. `IsConditional` is `true` whenever `Condition` is non-empty.
- `TSqlStepExecutor.EvaluateConditionAsync` runs `SELECT CASE WHEN (<expr>) THEN 1 ELSE 0 END` on the live debug session, so the expression can reference `DECLARE`'d variables and `@@` state. Errors are surfaced in Messages rather than silently swallowed.
- `DebuggerViewModel.ContinueAsync` evaluates the condition before breaking; if it returns 0 the runner simply skips the breakpoint and keeps stepping. Hit counts increment per actual break.
- `BreakpointConditionWindow` — a small modal to type/clear the T-SQL expression; opened via the step list context menu (`Set condition…` / `Clear condition`).

### Fixed
- PackIcon `PulseOutline` → `Pulse` (the former is not a valid `PackIconKind`; it crashed the app at startup when the user opened it after a clean install).
- `Author` line added to About dialog — `Khalil Benazzouz`.

### i18n
- All hardcoded English strings in Welcome tab, History panel, Monitoring module, Debugger context menu/tooltips and the Tools menu items (Monitoring, Git, Query Builder, Import CSV) are now bound with `DynamicResource`.
- Status bar: `Ready` / `No connection` / `Connected` / `Theme: X` / `Language: Y` all go through `LocalizationService.T(key, fallback)` and refresh live via a new `LanguageChanged` event.
- AvalonDock document tab titles (Schema Compare, Data Compare, Monitoring, …) are resolved through the localization service at open time — opening a tool after switching language yields its translated title.
- 15 new `Tab.*`, 4 new `Status.*`, 4 new `Debugger.*` (ConditionTitle/Header/Hint/ToggleBreakpoint), full `Monitoring.*` and `History.*` key sets in both `fr-FR.xaml` and `en-US.xaml`.

### Changed
- Version bumped to **2.1.0**.

## [2.0.0] — 2026-04-18 — Feature complete

Closes the last two items from the roadmap.

### Added — Function signatures in autocomplete
- `RoutineSignature` + `RoutineParameter` models in Core
- `IDatabaseMetadataService.GetRoutineSignaturesAsync` — SQL against `sys.parameters`/`sys.objects`/`sys.types` returns signatures for every `P/FN/IF/TF/FS/FT` object with typed parameters and the return type for scalar UDFs
- `SqlCompletionProvider.GetSignatureAsync` caches per (connection, database) and falls back to `dbo.<name>` if no schema
- Query Editor hooks `(` keystroke → `OverloadInsightWindow` displays the signature header (`schema.name(p1 type, p2 type) → returnType`) and a parameter-by-parameter body with directionality indicators (`→` in, `⇄` out) and `[default]` markers

### Added — Debugger step-into stored procedures
- `DebuggerViewModel.StepIntoAsync` detects `EXEC/EXECUTE [schema].[proc] @p=…` statements, parses the target schema + name, fetches the body via `ScriptObjectAsync`, strips the `CREATE PROCEDURE … AS` header, and splices the resulting statements into the Steps list right after the EXEC so the user can `Step Over` through them. Parameter bindings from the call site are recorded as comment headers before the body. Inner statements are prefixed `↘` in the UI.
- `DebuggerView` toolbar gains a **Step Into** button (`DebugStepInto` icon) between Step Over and Continue.

### Changed
- Version bumped to **2.0.0** — roadmap complete. Remaining items are either infrastructure scaffolds (Velopack activation awaiting signing, FlaUI screenshots awaiting visuals) or polish.

## [1.9.0] — 2026-04-18

### Added
- **IntelliSense alias resolution**: `SqlCompletionProvider.ExtractAliases` parses the current SQL and resolves `alias.` to the real `schema.table`, so typing `c.` after `FROM dbo.Customers AS c` now lists the customer columns. `ExtractCteNames` also captures CTE declarations for future completion sources.
- **Master-detail drill-down**: `QueryEditorViewModel.FollowRelation` deduces the referenced table from common conventions (`XxxId` → `Xxx`, `FK_A_B` → `B`) and opens a `SELECT TOP 100` filtered by the clicked value in a new tab.
- **Pivot generator**: new toolbar button in the Query Editor turns the first result set into a `PIVOT` skeleton (row/column/value axes inferred from column order, distinct pivot values pre-populated up to 20).
- **Profiler Graph tab**: plan operators rendered as boxes on a canvas (layered layout — root at top, children 140 px below, siblings 210 px apart), one colored header per operator with its icon and cost/rows/reads stats.
- **Per-tab database refresh button** next to the database selector in the Query Editor.
- **UpdateService scaffold** (`src/DBTeam.App/Services/UpdateService.cs`): placeholder that returns "no update" today; drop-in instructions for Velopack + GithubSource embedded in the file.
- **DBTeam.Screenshots** harness (`tests/DBTeam.Screenshots/`): console project targeting net8.0-windows, ready for FlaUI.UIA3 wiring. See `docs/bmad/DESIGN-NOTES.md#e18`.

### Changed
- Version bumped to 1.9.0.

## [1.8.0] — 2026-04-18

### Added — distribution & quality
- **MSIX manifest scaffold** at `installer/msix/Package.appxmanifest` with `runFullTrust` capability, file association for `.sql`, and multi-language (en-US / fr-FR) resources. Build recipe in `installer/msix/README.md`.
- **winget manifests** at `installer/winget/` (installer + locale + version YAMLs) ready to submit to `microsoft/winget-pkgs` after the first signed release.
- **Code signing step** in `release.yml` using `azure/trusted-signing-action@v0.4.0` — automatically activates when the `AZURE_TENANT_ID` secret is defined; keeps the release flow unchanged until credentials are provisioned.
- **`DBTeam.IntegrationTests`** project (xUnit + LocalDB) with `LocalDbFixture` (creates/drops a disposable DB per test class) and a `[SkipIfLocalDbUnavailable]` attribute so the suite auto-skips on machines without LocalDB. Initial tests cover `GetDatabases`, `GetTables`, `GetColumns`, and `ExecuteAsync`.

### Changed
- CI's existing `dotnet test` step auto-discovers the new integration tests; they run only when LocalDB is present (Windows runners).

## [1.7.0] — 2026-04-18

### Added — 4 new modules
- **Monitoring** (`Tools → Monitoring`) — real-time DMV polling (configurable interval), 4 live counters (active sessions / running requests / buffer cache hit % / page life expectancy), rolling 120-sample DataGrid, Top-10 waits table.
- **Import** (`Database → Import CSV...`) — CSV/TSV file picker with delimiter + header options, auto type inference (INT/BIGINT/DECIMAL/BIT/DATETIME2/NVARCHAR), preview grid, `CREATE TABLE` generator, `SqlBulkCopy` import with progress reporting.
- **Query Builder** (`Database → Query Builder...`) — visual tree of tables + views with per-column checkboxes, WHERE / ORDER BY / TOP / DISTINCT controls, live `SELECT` generation, "Open in Editor" to move to Query Editor.
- **Git** (`Tools → Git`) — pick a Git repository folder, list `.sql` files, double-click to open in a new Query tab, commit with message, pull/push, `git log --oneline -15` pane.

### Added — engines
- **SchemaSnapshot** — capture a full DB schema to `%AppData%\DBTeam\snapshots\<db>-<timestamp>.json`, reload and diff two snapshots without needing both live connections.
- **Data Compare — CHECKSUM mode** — `DataCompareEngine.CompareByChecksumAsync` groups rows by `BINARY_CHECKSUM(*)` and reports matching / only-source / only-target counts; works on tables without a primary key.

### Changed
- Shell menu gains *Database → Query Builder…*, *Database → Import CSV…*, *Tools → Monitoring*, *Tools → Git*.

## [1.6.0] — 2026-04-18

### Added
- **AI Assistant module** — chat panel with BYO-key for Anthropic / OpenAI / Azure OpenAI / Ollama. Provider presets, DPAPI-encrypted key storage, quick SQL prompt buttons (explain, optimize, generate INSERTs, convert to CTE…), configurable system prompt and model. Menu: *Tools → AI Assistant*.
- **Full i18n coverage** for new modules (Admin, Terminal, Debugger, AI Assistant) in both `en-US` and `fr-FR`.

### Changed
- README roadmap: 16 items moved from "todo" to "Shipped" with version tags; remaining items bucketed by target version (v1.6 / v1.7 / v1.8 / v2).
- Pages landing page (`docs/index.md`) expanded: new feature cards for Administration, Debugger, Terminal, AI Assistant; status grid updated with accurate shipping state.

### Fixed
- `DBTeam.Modules.Terminal`: removed invalid `-NonInteractive:$false` argument that broke `pwsh` / `powershell` on start.
- `docs/MODULES.md`, `docs/bmad/project-context.md`, `README.md`: removed remaining "stub — future" wording for the Debugger module, now accurately reflects v1.4 functionality.

## [1.5.0] — 2026-04-18

### Added
- **Snippets** — 16 Tab-triggered T-SQL snippets in Query Editor (`sel`, `ins`, `upd`, `del`, `mrg`, `cte`, `join`, `tbl`, `idx`, `sp`, `fn`, `tryc`, `tran`, `pivot`, `row`, `selw`). Type trigger + `Tab` to expand.
- **F12 Go to Definition** — press `F12` on any identifier in the Query Editor to script the underlying object (table/view/procedure/function) and open it in a new tab.
- **Real-time SQL validation** — ScriptDom parses on idle (700 ms debounce) and paints red squiggles under parse errors directly in the editor.
- **XML export** — new format in Query Editor export dropdown (in addition to CSV/Excel/JSON).
- **Administration module** (`Tools → Administration`) — new dedicated view with 8 tabs: Databases (size/recovery), Logins, Users, Roles, Permissions, Index fragmentation (with recommendation column: OK/REORGANIZE/REBUILD), Slow queries (top 50 via `sys.dm_exec_query_stats`), Active sessions. Three script generators: BACKUP DATABASE, RESTORE DATABASE, index rebuild/reorganize.
- **Terminal module** (`Tools → Terminal`) — interactive shell embedded in a document tab. Supports `pwsh`, `powershell`, `cmd`, `claude`, `gh`, `sqlcmd`, or any CLI. Working directory picker, Quick snippets sidebar with common commands (Claude Code, gh, sqlcmd), send-on-Enter input box, clear + stop + start controls.

### Changed
- Query Editor export menu: 4 formats (CSV, Excel, JSON, XML).

## [1.4.0] — 2026-04-18

### Added
- **T-SQL Debugger — fully functional** (E13): the placeholder view is replaced by a real debugger.
  - **Statement-level stepping** — the script is parsed with ScriptDom into individual statements (`TSqlStatementInfo`) and executed one by one on a persistent `SqlConnection` so `DECLARE`, `SET`, transaction state, temp tables survive between steps.
  - **Breakpoints** — double-click a statement in the Steps list to toggle a breakpoint on its start line. Continue pauses before any step whose start line is flagged.
  - **Controls** — Attach / Step Over / Continue / Stop / Restart / Detach.
  - **Output** — per-step Results grid with streaming result sets, Messages tab with per-step timing (ms) + rows affected + `PRINT`/`RAISERROR` messages.
  - **Session state panel** — live `@@ROWCOUNT`, `@@ERROR`, `@@TRANCOUNT`, `@@SPID`, current DB, login.
  - **Parse errors** surfaced in Messages without aborting the UI.

### Changed
- `DBTeam.Modules.Debugger` gained runtime dependencies: `Microsoft.SqlServer.TransactSql.ScriptDom`, `Microsoft.Data.SqlClient`, `AvalonEdit`.

## [1.3.0] — 2026-04-18

### Added
- **Table Designer — load existing** (E8): new **Load** command in the designer fetches an existing table's columns via `IDatabaseMetadataService` and populates the grid for editing. Full column-level `ALTER` generation for cross-database diffs lives in Schema Compare (v1.2.0).
- **Profiler graphical plan tree** (E12): the Profiler has a new **Plan tree** tab rendering the `SHOWPLAN_XML` hierarchy as a `TreeView`, with distinct icons per physical operator (Seek, Scan, Nested Loops, Hash Match, Merge Join, Sort, Filter, Compute Scalar, Aggregate, Top, Parallelism…) and inline stats (cost, estimated rows, actual rows).
- **Session save/restore** (E23): open Query Editor tabs (connection Id, database, SQL) are persisted to `%AppData%\DBTeam\session.json` on window close and restored on next launch.
- **Accessibility baseline** (E22): global implicit `Button` style binds `AutomationProperties.Name` to each button's `ToolTip` (Narrator-friendly for icon-only buttons). Added a `BrandFocusVisual` dashed ring for keyboard focus. Remaining items (high-contrast swap, full Tab audit) tracked in `docs/bmad/DESIGN-NOTES.md`.
- **Local install script**: `scripts/install-local.ps1` copies the published output into `%LocalAppData%\Programs\DBTeam`, creates Start menu + desktop shortcuts, and registers the app in *Apps & features* for clean uninstall — useful for dev loops and environments without Inno Setup.
- **Technical design notes**: `docs/bmad/DESIGN-NOTES.md` captures concrete implementation plans for items deferred to v2 or external-dependency items: T-SQL Debugger (E13), Code signing (E14), MSIX (E15), Auto-update via Velopack (E16), Screenshots automation via FlaUI (E18), LocalDB integration tests (E20), winget manifest (E27), AI assistant BYO-key (E25).
- **Screenshots capture guide**: `docs/images/README.md` (naming convention, sizing, tools).

### Changed
- **README**: added CI / CodeQL / Website / Release badges, concise pitch and direct download link, documentation index header.
- **GitHub Pages** redesign: custom Jekyll layout with sticky nav, dark theme, custom CSS (`docs/assets/style.css`), hero with gradient, 12 feature cards with colored icons, shortcuts panel with styled `<kbd>`, full module status grid, tech-stack pills, doc cards, footer, inline SVG favicon.
- **`.gitignore`**: added `dist/` to avoid committing published artifacts.

### Fixed
- **Installer script**: `scripts/build-installer.ps1` now passes absolute paths to ISCC; previously the relative `dist/` path broke on GitHub Actions runners (ISCC resolves paths relative to the `.iss` file, not the current directory).
- Dependabot group PRs merged: test deps (xUnit, coverlet, Microsoft.NET.Test.Sdk) bumped.

## [1.2.0] — 2026-04-18

### Added
- **.exe installer** via Inno Setup — `DBTeam-Setup-{version}.exe` with Start menu entry, clean uninstall in *Apps & features*, silent install (`/SILENT` or `/VERYSILENT`). Released alongside the portable ZIP.
- **Schema Compare column-level ALTER**: tables with definition differences now emit `ALTER TABLE ADD/DROP/ALTER COLUMN`, FK add/drop, and index add/drop — no more `-- TODO` placeholder. Whole script wrapped in `BEGIN TRAN / COMMIT`.
- **Query History panel** with favorites, filter, double-click to reopen (shipped in v1.1.0, now prominent in shell).
- **Diagram module**: drag tables to rearrange, `Ctrl+Wheel` to zoom, zoom in/out/reset buttons, **Export as PNG**. FK lines now route between box centers (+110,+40 offset for visual centering).

### Changed
- GitHub Actions bumped: `actions/checkout@v6`, `actions/setup-dotnet@v5`, `actions/upload-artifact@v7`, `softprops/action-gh-release@v3`, `github/codeql-action@v4`.
- Release workflow installs Inno Setup and builds the installer automatically on tag push. Both assets (installer + zip) attached to the GitHub Release.

## [1.1.0] — 2026-04-18

### Added
- **E11 Query History & favorites**: new side pane, filter, star favorites, right-click context (open in new tab, favorite, delete), 1000-entry JSONL store in `%AppData%\DBTeam\history.jsonl`, automatic recording on each query execution.

## [1.0.1] — 2026-04-18

### Added
- **Results export**: CSV (UTF-8 BOM, RFC 4180), Excel (EPPlus, auto-filter + frozen header), JSON (System.Text.Json) — toolbar splitbutton in Query Editor
- **T-SQL Debugger** stub replaced with a proper "Coming in v2" view (accessible via Tools → Debugger)
- **Auto-format** of generated scripts: Schema Compare / Data Compare / Table Designer / Data Generator outputs arrive pre-formatted in the editor
- **Format SQL** button added to the Query Profiler
- **GitHub Pages**: landing page at https://khalilbenaz.github.io/DBTeam/
- **Dependabot**: weekly NuGet + monthly GitHub Actions updates with grouped PRs
- **CodeQL** security analysis workflow (push/PR + weekly schedule)
- **12 Epic issues** on GitHub tracking the full roadmap (E1–E13)
- **25 unit tests** covering TSqlFormatter, DataCompare script gen, DataGenerator, EventBus, ConnectionStringFactory

### Changed
- Log file format enriched (version, OS, properties)

## [1.0.0] — 2026-04-18

Initial tagged release.

## [1.0.0] — 2026-04-18

### Added
- Shell + AvalonDock + DI + theme/language services
- Connection Manager (dialog + side panel), DPAPI-encrypted store
- Object Explorer (lazy tree, icons, context menu, multi-connection)
- Query Editor: AvalonEdit, T-SQL highlighting, execute, format (ScriptDom), autocomplete (keywords + tables + views + procs + functions + columns after `.`), estimated plan
- Schema Compare (tables/views/procs/functions) + sync script generation
- Data Compare (row-level diff + merge script)
- Table Designer (CREATE DDL)
- Query Profiler (estimated + actual plan, operator stats)
- Database Diagram (grid layout + FK lines)
- Data Generator (Bogus, preview/script/insert, column-name heuristics)
- HTML Documenter
- EN / FR localization, live swap
- Light / Dark / System theme, persisted
- Welcome tab with quick-start cards
