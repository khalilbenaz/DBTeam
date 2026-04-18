# Changelog

All notable changes to DB TEAM are documented here. Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versioning: [Semantic Versioning](https://semver.org/).

## [Unreleased]

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
