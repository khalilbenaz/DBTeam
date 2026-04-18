---
layout: default
title: DB TEAM
description: Professional SQL Server IDE built with WPF
---

# DB TEAM

**Professional SQL Server IDE for Windows.** Open-source, modular, bilingual (EN/FR), themeable.

[![.NET](https://img.shields.io/badge/.NET-8-blueviolet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-Windows-blue)](https://github.com/khalilbenaz/DBTeam/releases)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](https://github.com/khalilbenaz/DBTeam/blob/main/LICENSE)
[![GitHub](https://img.shields.io/badge/github-repo-black?logo=github)](https://github.com/khalilbenaz/DBTeam)

---

## Download

Grab the latest release from the [Releases page](https://github.com/khalilbenaz/DBTeam/releases).

Self-contained — no .NET install required. Extract the zip, run `DBTeam.App.exe`.

## What you get

- **Query Editor** — T-SQL syntax highlighting, autocomplete (keywords, tables, views, procedures, functions, columns), format on `Ctrl+K`, execute on `F5`, estimated and actual execution plans
- **Object Explorer** — hierarchical tree with icons per object type, multi-connection, lazy loading, right-click scripting
- **Schema Compare** — diff tables, views, procedures, functions across two databases; generate a sync script
- **Data Compare** — PK-based row-level diff; generate `INSERT/UPDATE/DELETE` merge script under a transaction
- **Table Designer** — visual column editor with Generate DDL
- **Query Profiler** — structured view of execution plans with per-operator stats (cost, estimated rows, actual rows, logical reads)
- **Database Diagram** — visual ER-style canvas of tables and foreign keys
- **Data Generator** — Bogus-powered fake data insertion with column-name heuristics (email, phone, name, city, company…)
- **HTML Documenter** — generate a standalone HTML documentation of a database

## Docs

- [Install guide](INSTALL.html) · ([markdown](INSTALL.md))
- [User guide](USER-GUIDE.html) · ([markdown](USER-GUIDE.md))
- [Architecture](ARCHITECTURE.html) · ([markdown](ARCHITECTURE.md))
- [Modules reference](MODULES.html) · ([markdown](MODULES.md))
- [Product requirements](bmad/PRD.html) · ([markdown](bmad/PRD.md))
- [Epics & backlog](bmad/EPICS.html) · ([markdown](bmad/EPICS.md))
- [Changelog](https://github.com/khalilbenaz/DBTeam/blob/main/CHANGELOG.md)

## Status

**Work in progress — v1.0 installable.** Several advanced features are planned for v1.1+. See [PRD](bmad/PRD.html) and [EPICS](bmad/EPICS.html) for the roadmap.

| Module | State |
|---|---|
| Connection Manager | ✅ |
| Object Explorer | ✅ |
| Query Editor | ✅ |
| Schema Compare | ✅ |
| Data Compare | ✅ |
| Table Designer | ✅ create · 🚧 edit existing (v1.1) |
| Profiler | ✅ |
| Data Generator | ✅ |
| Documenter | ✅ |
| Diagram | ✅ basic |
| T-SQL Debugger | 🚧 v2 |
| Results export | 🚧 v1.0 |

## Stack

WPF · .NET 8 · AvalonDock · AvalonEdit · ModernWpfUI · MaterialDesign · CommunityToolkit.Mvvm · Microsoft.Data.SqlClient · ScriptDom · Bogus · Dapper · Serilog

## License

MIT — see [LICENSE](https://github.com/khalilbenaz/DBTeam/blob/main/LICENSE).

## Contribute

Read [CONTRIBUTING](https://github.com/khalilbenaz/DBTeam/blob/main/CONTRIBUTING.md) and open a PR.
