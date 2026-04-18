# User Guide

A pragmatic tour of DB TEAM for people using it, not writing it.

## The shell

```
┌── Menu ─────────────────────────────────────────────────────┐
│ File  Edit  Database  Tools  View  Help                    │
├── Toolbar ──────────────────────────────────────────────────┤
│ [+DB] [+Query] | [▶ Execute] | [Schema↔] [Data↔] [Diag] …  │
├──────────────────┬──────────────────────────────────────────┤
│  Connections     │                                          │
│  Object Explorer │            Document tabs                 │
│  (side pane)     │     (Welcome, Queries, Compare, …)       │
└──────────────────┴──────────────────────────────────────────┘
   Status bar — active connection · current status
```

Panes can be docked, floated, auto-hidden — drag their headers (AvalonDock).

## Connecting

1. **File → New Connection** or `Ctrl+N`.
2. **Connection name** is free-form — shown in the list.
3. **Server**: `localhost`, `MYHOST\SQLEXPRESS`, `tcp:myhost,1433`, `.` (local default instance), Azure FQDN, etc.
4. **Authentication**:
   - *Windows Authentication* — uses your Windows account (no credentials needed)
   - *SQL Server Authentication* — login + password
   - *Azure AD – Integrated* — current Windows account authenticates to Azure AD
   - *Azure AD – Password* — AAD user + password (interactive MFA not supported yet)
5. **Trust server certificate** — leave on for dev; turn off for prod with a valid cert.
6. **Load** enumerates databases; pick one (or leave empty to use the default).
7. **Test** → **Connect**.

Saved connections appear in the left side panel. Double-click to reconnect.

## Object Explorer

The tree lazily loads per node. Icons indicate type:

| Icon | Kind |
|---|---|
| 🖥 | Server |
| 🗄 | Database |
| 📁 | Folder (Tables, Views, Procs, Functions) |
| ▦ | Table |
| 👁 | View |
| ⚙ | Stored procedure |
| ƒ | Function |

Right-click on any node for the context menu:

- **New Query Here** — opens a new editor with the selected connection + database; if it's a table or view, pre-fills `SELECT TOP 100 …`.
- **Script Object** — opens the DDL as a new query.
- **Disconnect** — removes the connection from the tree (saved connection stays).

**Double-click** a table/view = quick `SELECT TOP 100`.

## Query Editor

- **F5** = Execute
- **Ctrl+K** = Format (T-SQL reformatted by ScriptDom)
- **Ctrl+Space** = Autocomplete (keywords + tables + views + procs + functions; columns after `.`)
- **Database** dropdown (toolbar) switches the context without opening a new tab.
- Results grid supports multiple result sets. A **Messages** tab captures `PRINT` / errors.
- Row count and elapsed time shown in the toolbar.

### Execution plans

- **Estimated Plan** — without running, shows `SHOWPLAN_XML`.
- Handled by the **Tools → Query Profiler** tab (see below) for a structured view.

## Schema Compare

**Database → Schema Compare**

1. Pick source connection + database, target connection + database.
2. **Compare** — enumerates tables, views, procedures, functions on both sides.
3. Results list shows the state for each object:
   - ➕ green = only in source
   - ➖ red = only in target
   - ≠ orange = different definition
   - ✓ grey = identical
4. Click an item to see its Source and Target scripts side-by-side.
5. **Sync Script** opens a T-SQL script in a new Query tab that applies the diff from source to target.

> Tables with definition differences currently emit a `-- TODO: manual ALTER TABLE` placeholder. Column-level `ALTER` is slated for v1.1.

## Data Compare

**Database → Data Compare**

Compares rows between two tables with the same primary key.

1. Pick source connection/database, target connection/database, and a table.
2. **Compare** fetches both sides and diffs by PK.
3. Differences list shows only non-identical rows.
4. **Sync Script** produces `INSERT / UPDATE / DELETE` under `BEGIN TRAN / COMMIT` for the target side.

> A primary key is required; the compare aborts otherwise.

## Table Designer

**Database → New Table**

Visual editor for `CREATE TABLE`:

- Pick connection + database + schema + name.
- Add columns with Name / Type / Length / Nullable / Identity / PK / Default.
- **Generate DDL** opens the resulting `CREATE TABLE` script in a new Query tab (not executed automatically — run it yourself after review).

## Query Profiler

**Tools → Query Profiler**

Analyze a query's plan and runtime statistics:

- **Estimated** — just parses and gets the XML plan.
- **Run with actual plan** — actually executes, captures `STATISTICS XML`, populates the operator table with cost, estimated rows, actual rows, logical reads.

## Database Diagram

**Tools → Database Diagram**

Visual canvas of tables and FK lines:

- Pick connection + database → **Load**.
- Grid layout, scroll to navigate.
- Each box shows PK columns with a key icon and the first columns of the table.

> Drag, zoom, and proper FK edge routing are v1.1 items.

## Data Generator

**Database → Data Generator**

Fill a table with realistic fake data using [Bogus](https://github.com/bchavez/Bogus):

- Pick table and row count.
- **Preview** generates 50 sample rows in a grid.
- **Export as script** produces `INSERT` statements in a new Query tab.
- **Insert to database** runs them directly (asks confirmation first, shows progress).

Column name heuristics kick in: `email` → email addresses, `phone` → phone numbers, `firstname`/`lastname`/`city`/`country`/`company`/`url`/`ip`/`user`/`password`/`description` all get appropriate fakers.

## Documenter

**Database → Documenter**

Generates a self-contained HTML document with every table, column, index, FK, view, procedure, and function of a database. Saved in `Documents\DBTeam-Docs\` and opened in your default browser.

## Themes & Language

- **View → Theme** — Light / Dark / System (persisted).
- **View → Language** — English / Français (live swap, persisted).

## Keyboard shortcuts

| Shortcut | Action |
|---|---|
| `F5` | Execute current query |
| `Ctrl+K` | Format SQL |
| `Ctrl+Space` | Autocomplete |
| `Ctrl+N` | New connection |
| `Ctrl+Q` | New query |

## Troubleshooting

| Symptom | Check |
|---|---|
| App won't start | Check `%LocalAppData%\DBTeam\logs\app-YYYYMMDD.log` |
| "Cannot connect" | Try `sqlcmd -S <server> -U <user> -P <pass>` to isolate; TLS? firewall? |
| SmartScreen warning | Normal — binary not yet code-signed. *More info → Run anyway* |
| Missing Azure AD interactive | Not supported yet; use SQL auth or Windows auth |

## Reporting bugs

The error dialog (triggered on unhandled exceptions) has a **Report** button that opens a prefilled GitHub issue with the stack trace. Or manually: https://github.com/khalilbenaz/DBTeam/issues
