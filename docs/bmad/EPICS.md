# Epics & Stories

Detailed backlog driving v1.0 (ship) and v1.1+ (expand). Epics are ordered by value/effort. Each story is small enough to complete in one context window.

---

## Epic E1 — Installer & Release pipeline (v1.0 blocker)

**Goal**: Users can download, install, and launch DB TEAM without a dev environment.

- **E1-S1** Configure `DBTeam.App.csproj` for self-contained single-file publish (`PublishSingleFile=true`, `SelfContained=true`, `RuntimeIdentifier=win-x64`, `PublishReadyToRun=true`, trim off — WPF can't trim safely).
- **E1-S2** Add assembly version `1.0.0` from a single source (`Directory.Build.props`).
- **E1-S3** Write `scripts/publish.ps1` producing `dist/DBTeam-{version}-win-x64/` with the exe + icon + LICENSE + README.
- **E1-S4** Add MSIX packaging project (`DBTeam.Package`) or use `dotnet-msix` / `Windows Application Packaging Project` generating a `.msix`.
- **E1-S5** Create `Package.appxmanifest` with publisher, logo, capabilities, file associations (`.sql` optional).
- **E1-S6** Add a portable ZIP alternative (`scripts/publish-portable.ps1`) for users who don't want MSIX.
- **E1-S7** Smoke test checklist: install → open → New Connection → Execute → Uninstall.
- **E1-S8** GitHub Actions workflow `.github/workflows/release.yml` triggered on tag `v*`: build, publish (self-contained + MSIX if possible), attach assets to Release.
- **E1-S9** GitHub Actions workflow `.github/workflows/ci.yml` on push/PR: build + (later) tests.
- **E1-S10** Tag `v1.0.0` and produce the first Release.

**Acceptance**: Non-dev user installs the produced `.msix` or unzips the portable, runs DB TEAM, connects, queries, exits — no console, no .NET install prompt.

---

## Epic E2 — Crash resilience & logging

**Goal**: App survives unexpected errors, logs everything, users can report issues.

- **E2-S1** Global handlers in `App.xaml.cs`: `AppDomain.UnhandledException`, `Dispatcher.UnhandledException`, `TaskScheduler.UnobservedTaskException`.
- **E2-S2** Error dialog with: message, "Copy details" (stack trace), "Open logs folder", "Report issue" (opens GitHub issues URL prefilled).
- **E2-S3** Logs enriched with version, OS, culture, last user action.
- **E2-S4** Wrap `IQueryExecutionService.ExecuteAsync` errors so the UI never crashes on bad SQL/connection.
- **E2-S5** Defensive: `try/catch` around `OnConnectionOpened` in ObjectExplorer, keep other connections alive.

**Acceptance**: Kill the SQL server mid-query — user sees a toast, app stays open, log contains the exception.

---

## Epic E3 — Results export

**Goal**: User can export a grid to CSV, Excel, JSON.

- **E3-S1** Add `IResultExporter` in Core (CSV / Excel / JSON implementations in Data).
- **E3-S2** CSV export: UTF-8 BOM, RFC 4180 quoting, streaming writer.
- **E3-S3** Excel export via `ClosedXML` or `EPPlus` (already referenced): one sheet per result set, auto-filter, freeze header.
- **E3-S4** JSON export: array of objects, `System.Text.Json`, `WriteIndented=true`, `DateTimeOffset` ISO 8601.
- **E3-S5** Toolbar button in Query Editor "Export" splitbutton → CSV / Excel / JSON.
- **E3-S6** `SaveFileDialog` with suggested filename `query-YYYYMMDD-HHmmss.{ext}`.
- **E3-S7** Large result sets: chunked write, progress in status bar.

**Acceptance**: Run `SELECT * FROM sys.objects`, export to Excel, open in Excel — columns correct, dates readable, no truncation.

---

## Epic E4 — Dead-menu cleanup & Welcome polish

- **E4-S1** Replace `MessageBox "Debugger - TODO"` with a "Coming in v1.1" card in a document tab (same pattern as other modules).
- **E4-S2** Every menu item produces a visible outcome or a clear "coming soon" tab.
- **E4-S3** Welcome tab: add recent connections list and recent queries list.
- **E4-S4** Welcome: "Open sample query" card that opens a pre-filled editor without requiring a connection (SELECT @@VERSION).

---

## Epic E5 — Documentation & screenshots

- **E5-S1** Capture 5 screenshots (welcome, object explorer, query editor with results, schema compare, diagram).
- **E5-S2** Record a 15-second GIF of the golden path (connect → execute → see results).
- **E5-S3** Embed them in README.
- **E5-S4** Write `docs/USER-GUIDE.md` (end-user, not dev).
- **E5-S5** Write `docs/INSTALL.md` (download + install + first-run).
- **E5-S6** `CHANGELOG.md` seeded with v1.0.0.

---

## Epic E6 — Unit tests baseline

- **E6-S1** Create `tests/DBTeam.Core.Tests` (xUnit).
- **E6-S2** Create `tests/DBTeam.Data.Tests` with Testcontainers or LocalDB.
- **E6-S3** Create `tests/DBTeam.Modules.SchemaCompare.Tests` — snapshot tests of diff output.
- **E6-S4** Wire tests to CI; fail PR on regression.

---

## Epic E7 — Schema Compare ALTER column-level

- **E7-S1** Column-level diff: added / removed / changed (type / nullability / default).
- **E7-S2** Generate `ALTER TABLE ADD COLUMN` / `DROP COLUMN` / `ALTER COLUMN`.
- **E7-S3** Index/FK diff.
- **E7-S4** Dry-run mode (produce script without executing) — already default.
- **E7-S5** Apply mode with transaction and rollback on failure.

---

## Epic E8 — Table Designer: edit existing + editors

- **E8-S1** "Open in designer" entry in Object Explorer context menu on tables.
- **E8-S2** Loader that fills the grid from `GetColumnsAsync`.
- **E8-S3** Diff-based DDL: generate ALTER only for changes.
- **E8-S4** FK editor tab.
- **E8-S5** Index editor tab.

---

## Epic E9 — Diagram improvements

- **E9-S1** Route FK lines between nearest box edges (compute intersection with rectangle).
- **E9-S2** Draggable boxes (MouseDown/Move, store X/Y on VM).
- **E9-S3** Zoom (Ctrl+Wheel) + fit-to-screen button.
- **E9-S4** Persist layout per `(connection, database)`.
- **E9-S5** Export diagram as PNG.

---

## Epic E10 — IntelliSense advanced

- **E10-S1** Parse current query with ScriptDom, resolve table aliases.
- **E10-S2** After `alias.`, propose the aliased table's columns.
- **E10-S3** Function signatures + inline docs on hover.
- **E10-S4** Snippet library (SELECT template, JOIN, CTE, window functions).

---

## Epic E11 — Query history & favorites

- **E11-S1** Append each executed query to `%AppData%\DBTeam\history.jsonl`.
- **E11-S2** History panel in shell (docked).
- **E11-S3** Star a query → favorites list.
- **E11-S4** Search.

---

## Epic E12 — Profiler graphical plan tree

- **E12-S1** Render `<RelOp>` nodes as boxes on a canvas with icons per physical op.
- **E12-S2** Connect parents → children with arrows; arrow thickness = estimated rows.
- **E12-S3** Hover shows tooltip with all XML attributes.
- **E12-S4** Click node highlights in XML tab.

---

## Epic E13 — T-SQL Debugger (v2)

- **E13-S1** Design spike: evaluate instrumentation approach (insert SELECT checkpoints around statements) vs external tooling.
- **E13-S2** Implement instrumentation generator.
- **E13-S3** Breakpoints panel.
- **E13-S4** Watch panel.
- **E13-S5** Step over / step into / step out.

---

## Sprint plan — v1.0 target

**Sprint 1** (week 1–2): **E1** + **E2** (shippable skeleton).
**Sprint 2** (week 3): **E3** + **E4** (feature completeness to stated scope).
**Sprint 3** (week 4): **E5** (polish) + smoke testing + **tag v1.0.0**.
**Sprint 4+ (v1.0.1 / v1.1)**: **E6**, **E7**, **E8**, **E9**, **E10**, **E11**.
