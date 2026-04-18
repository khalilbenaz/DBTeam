# PRD — DB TEAM (v1.0 → v1.1 brownfield)

## 1. Vision

DB TEAM is a modular Windows SQL Server IDE. The product is currently a working prototype (~80 %) reachable only via `dotnet run`. **This PRD scopes the path to a shippable, installable, trustworthy v1.0 plus the backlog toward v1.1.**

## 2. Users

| Persona | Need |
|---|---|
| **Backend developer** | Run ad-hoc queries, compare schemas across dev/staging/prod, generate DDL |
| **Data engineer** | Populate test tables, compare data between environments, document a schema |
| **DBA** | Browse an unfamiliar database fast, profile slow queries, generate sync scripts |

## 3. Goals (v1.0 release)

### G1 — Installable product **(HIGHEST PRIORITY)**
- Single installer (MSIX or self-contained .exe) users can download and double-click
- Starts from Start menu
- Uninstallable cleanly
- Signed (optional v1.0, required v1.1)
- Ships as a GitHub Release with binaries

### G2 — Trust & stability
- Logs accessible at `%LocalAppData%\DBTeam\logs`
- Crash-resistant: unhandled exceptions logged, user sees an error dialog, app survives
- Minimum smoke tests before tagging a release

### G3 — Feature parity baseline
Current modules must be polished (no "TODO" popups in user-facing menus).

## 4. Non-goals (v1.0)

- No T-SQL step-through debugger (deferred to v1.1+)
- No cross-platform (macOS/Linux) — Windows only
- No Azure Data Studio parity (no notebooks, no Jupyter)
- No multi-user collaboration

## 5. Functional requirements

### FR1 — Packaging & distribution
- **FR1.1** Produce a single `.msix` (preferred) or `.exe` self-contained installer for `win-x64`
- **FR1.2** Include app icon, version metadata, publisher name
- **FR1.3** Install to user scope (no admin required)
- **FR1.4** Create a Start menu shortcut and file associations for `.sql` (optional)
- **FR1.5** Auto-update channel (deferred — v1.1)

### FR2 — Release automation
- **FR2.1** GitHub Actions workflow: build + test + package on tag `v*`
- **FR2.2** Attach the installer as a GitHub Release asset
- **FR2.3** Generate a changelog from commits between tags

### FR3 — Crash resilience
- **FR3.1** Global `AppDomain.UnhandledException` + `Dispatcher.UnhandledException` handlers
- **FR3.2** Log the exception, show a friendly dialog with "Open logs" button, let user continue when possible
- **FR3.3** First-chance recovery in critical paths (query execution, connection open)

### FR4 — Results export (removes a user pain)
- **FR4.1** Export the active result set to CSV, Excel (.xlsx), JSON
- **FR4.2** File dialog, proper encoding (UTF-8 BOM for CSV), streaming for large grids

### FR5 — Polish tracked bugs
- **FR5.1** All menu items produce either a visible outcome or a clear "coming in v1.1" card (no dead popups)
- **FR5.2** Welcome page replace click-Y-does-nothing tiles with working shortcuts

### FR6 — Documentation
- **FR6.1** README with screenshots, install instructions, first-use walkthrough
- **FR6.2** Short video/GIF in README (<20s)
- **FR6.3** `docs/USER-GUIDE.md` for end-users

## 6. Non-functional requirements

- **Performance**: App cold start < 3 s on mid-range laptop; Object Explorer lazy-loads per node; query execution doesn't freeze UI (already async)
- **Compatibility**: Windows 10 22H2+, Windows 11; SQL Server 2016+, Azure SQL, LocalDB
- **Size**: Installer < 80 MB (MSIX compressed)
- **Accessibility**: Keyboard-only usable for core flows (F5, Ctrl+K, Ctrl+Space, Ctrl+N, Ctrl+Q)
- **i18n**: Every new user-facing string added to both `en-US.xaml` and `fr-FR.xaml`

## 7. Acceptance criteria (v1.0 shippable)

- [ ] `dotnet publish` produces a single-file self-contained exe or an MSIX
- [ ] User installs it, opens it from Start menu, connects to a SQL Server, runs a query, sees results, exports to CSV
- [ ] App does not crash on: invalid credentials, dropped connection mid-query, syntax error in SQL
- [ ] README shows a screenshot and a 5-step install & first-use flow
- [ ] GitHub Release `v1.0.0` exists with the installer attached
- [ ] Log file is created at `%LocalAppData%\DBTeam\logs\app-YYYYMMDD.log`
- [ ] Uninstall removes the app (MSIX handles this natively)

## 8. Roadmap toward v1.1+

Ordered by value-over-effort (see `docs/bmad/EPICS.md` for detailed stories):

1. **E1 — Installer & Release pipeline** (v1.0 blocker)
2. **E2 — Crash resilience & logging polish** (v1.0 blocker)
3. **E3 — Results export CSV/Excel/JSON** (v1.0)
4. **E4 — Dead-menu cleanup & Welcome polish** (v1.0)
5. **E5 — Documentation & screenshots** (v1.0)
6. **E6 — Unit tests baseline** (v1.0.1)
7. **E7 — Schema Compare column-level ALTER** (v1.1)
8. **E8 — Table Designer: edit existing + FK/Index editors** (v1.1)
9. **E9 — Diagram improvements (edge routing, drag, zoom)** (v1.1)
10. **E10 — IntelliSense: CTE/alias resolution, function signatures** (v1.1)
11. **E11 — Query history & favorites** (v1.1)
12. **E12 — Profiler graphical plan tree** (v1.2)
13. **E13 — T-SQL Debugger** (v2)

## 9. Out of scope / parking lot

- Source control integration (git-aware SQL editor)
- SSDT-style project concepts (schema-as-code)
- Redgate-style drift monitoring
- Scheduling / agent jobs

## 10. Success metrics (post v1.0 launch)

- 100 GitHub stars / 6 months
- 10 external contributors
- Zero "installer does not work" issues remaining open after 30 days
- >= 1 screenshot/GIF in every major PR affecting UI
