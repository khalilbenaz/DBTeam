# Changelog

All notable changes to DB TEAM are documented here. Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versioning: [Semantic Versioning](https://semver.org/).

## [Unreleased]

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
