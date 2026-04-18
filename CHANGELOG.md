# Changelog

All notable changes to DB TEAM are documented here. Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versioning: [Semantic Versioning](https://semver.org/).

## [Unreleased]

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
