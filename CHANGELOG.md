# Changelog

All notable changes to DB TEAM are documented here. Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versioning: [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added
- BMad project artefacts: `docs/bmad/{project-context,PRD,EPICS}.md`
- `Directory.Build.props` centralizing version/metadata
- `scripts/publish.ps1` — self-contained single-file Windows publish
- GitHub Actions: `ci.yml` (build on push/PR), `release.yml` (publish + Release on tag `v*`)
- End-user docs: `docs/INSTALL.md`, `docs/USER-GUIDE.md`
- Global exception handlers + friendly error dialog (Copy details / Open logs / Report issue)

### Changed
- Log file format enriched (version, OS, properties)

## [1.0.0] — 2026-04-18

Initial tagged release.

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
