# Modules reference

Overview of each feature module, its entry point, and current state.

| Module | Menu | Status | Notes |
|---|---|---|---|
| ConnectionManager | `File → New Connection`, side panel | ✅ | Full CRUD, DPAPI, test |
| ObjectExplorer | Side panel | ✅ | Lazy tree, multi-connection, context menu, disconnect |
| QueryEditor | `File → New Query`, dbl-click in tree | ✅ | AvalonEdit, format, autocomplete, execute, plans |
| ResultsGrid | Embedded in QueryEditor | ✅ (basic) | Export CSV/Excel/JSON TODO |
| SchemaCompare | `Database → Schema Compare` | ✅ | Table/view/proc/function diff + sync script |
| DataCompare | `Database → Data Compare` | ✅ | PK-based row diff + merge script |
| TableDesigner | `Database → New Table` | ✅ | Visual column editor + DDL; ALTER TODO |
| Profiler | `Tools → Query Profiler` | ✅ | Estimated + actual plan, operator stats |
| Debugger | `Tools → Debugger` | 🚧 | Placeholder |
| Diagram | `Tools → Database Diagram` | ✅ (basic) | Grid layout, FK lines center-to-center |
| DataGenerator | `Database → Data Generator` | ✅ | Bogus-backed, preview/script/insert |
| Documenter | `Database → Documenter` | ✅ | HTML output to `Documents\DBTeam-Docs\` |

---

## ConnectionManager

- `ConnectionDialog` — server, auth, credentials, `Test` + `Connect`
- `ConnectionsPanel` — list with double-click to connect, toolbar (reload/connect/delete), empty state
- `ConnectionDialogViewModel.SaveAndConnectAsync` publishes `ConnectionsChangedEvent` and `ConnectionOpenedEvent`
- Storage: `JsonConnectionStore` in `%AppData%\DBTeam\connections.json`, passwords via `DpapiProtector`

## ObjectExplorer

- `ObjectExplorerViewModel` subscribes to `ConnectionOpenedEvent` and adds a root node per connection (dedup by Id)
- Tree hierarchy: Server → Database → (Tables/Views/Procedures/Functions folders) → objects
- Lazy loading: `TreeNodeViewModel.Loader` fires on first expand
- Icons: `KindToIconConverter` maps `DbObjectKind` → `PackIconKind`
- Context menu: New Query Here · Script Object · Disconnect
- Right-click selects the hit item before opening the menu

## QueryEditor

- `QueryEditorView` hosts AvalonEdit with TSQL syntax highlighting
- `QueryEditorViewModel` wraps `IQueryExecutionService`
- Shortcuts: F5 (execute), Ctrl+K (format via ScriptDom `Sql160ScriptGenerator`), Ctrl+Space (completion)
- Autocomplete: `SqlCompletionProvider` + `SqlCompletionItem`
  - Static keywords
  - Cached tables/views per (connection, db)
  - Columns after `.` — identifier is extracted from the text before the caret

## Schema Compare

- `SchemaCompareEngine.CompareAsync` enumerates objects on both sides, scripts each, normalizes whitespace, and classifies: `Identical | OnlyInSource | OnlyInTarget | Different`
- `GenerateSyncScript`:
  - `OnlyInSource` → emits the source script (+ `GO`)
  - `OnlyInTarget` → emits `DROP <kind> [schema].[name]`
  - `Different` (table) → emits a `-- TODO: manual ALTER TABLE` (column-level ALTER generation is TODO)
  - `Different` (view/proc/func) → `DROP` + `CREATE`

## Data Compare

- Loads both tables into `DataTable` via `SqlServerQueryExecutionService`-style connection
- PK required — aborts otherwise
- Joins by concatenated PK string, classifies per row, emits `INSERT` / `UPDATE` / `DELETE` under `BEGIN TRAN/COMMIT`

## Table Designer

- `ColumnRowViewModel` — editable row in a DataGrid
- `BuildDdl()` produces `CREATE TABLE` + `CONSTRAINT PK_*` from the rows
- Move up/down via `ObservableCollection<T>.Move`
- Types: int/bigint/bit/decimal/varchar/nvarchar/date/datetime/uniqueidentifier/…

## Profiler

- `EstimatedCommand` → `SET SHOWPLAN_XML ON`, reads the plan XML
- `ActualCommand` → `SET STATISTICS XML ON` during execution, captures the last `ShowPlanXML` reader column
- XML parsed with `XDocument`, each `<RelOp>` becomes an `OperatorStat` (cost, estimated rows, and for actual: actual rows, logical reads from `<RunTimeCountersPerThread>`)

## Diagram

- Tables laid out on a grid (`cols = ceil(sqrt(n))`, 240x260 cells)
- Each box: header bar with table name, up to 12 columns (🔑 for PK) listed
- FK lines are center-to-center (TODO: route between box borders)
- Canvas size 3000x3000 inside a ScrollViewer

## Data Generator

- `DataGeneratorEngine.GenerateValue(col, faker)` maps column to fake data:
  - Numeric types → Bogus `Random.*`
  - Strings → name-based heuristic (email/phone/address/company/url/user/password/description) with length clamping
  - Dates / Guids / blobs
- `InsertAsync` loops one INSERT per row with parameters and reports progress every 50 rows
- `PreviewAsync` caps at 50 rows for responsiveness

## Documenter

- `HtmlDocumenter.BuildAsync` pulls tables → columns → indexes → FKs + views + procs + funcs
- Renders a single self-contained HTML file with embedded CSS, sticky nav, pills for PK/IDENTITY/NOT NULL, 3-column list for views/procs/funcs
- Saves to `%USERPROFILE%\Documents\DBTeam-Docs\{db}-{yyyyMMdd-HHmmss}.html` and opens it via the default browser

## Debugger (stub)

Currently shows a placeholder message. T-SQL debugging requires either:
- The legacy **SQL Server Debugger API** (deprecated since SSMS 18)
- A custom instrumentation approach (rewrite stored procs with checkpoint markers)
- Integrating an external third-party debugger

Design decision pending.
