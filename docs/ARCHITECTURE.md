# Architecture

This document explains how DB TEAM is organized and why.

## Layers

```
┌────────────────────────────────────────────────────────┐
│                    DBTeam.App (WPF)                    │
│  Shell · DI bootstrap · Theme · Localization · Menu    │
└────────────────────────────────────────────────────────┘
           │                                  │
           ▼                                  ▼
┌──────────────────────────┐     ┌────────────────────────┐
│  Modules.*               │     │  DBTeam.UI             │
│  ConnectionManager       │     │  Shared WPF controls   │
│  ObjectExplorer          │     │                        │
│  QueryEditor             │     └────────────────────────┘
│  SchemaCompare / ...     │
└──────────────────────────┘
           │
           ▼
┌──────────────────────────┐     ┌────────────────────────┐
│  DBTeam.Data             │     │  DBTeam.Core           │
│  SqlServerConnection…    │◀────│  Models · Abstractions │
│  SqlServerMetadata…      │     │  IEventBus · Events    │
│  SqlServerQueryExec…     │     │  ServiceLocator        │
│  DpapiProtector          │     │                        │
│  JsonConnectionStore     │     └────────────────────────┘
└──────────────────────────┘
```

**Dependency direction**: App → Modules → Core/Data/UI. Modules never reference each other directly — they communicate through `IEventBus`.

## Composition

The app's entry point is `App.OnStartup`:

```csharp
Services = new ServiceCollection()
    .AddSingleton<IConnectionService, SqlServerConnectionService>()
    .AddSingleton<IDatabaseMetadataService, SqlServerMetadataService>()
    .AddSingleton<IQueryExecutionService, SqlServerQueryExecutionService>()
    .AddSingleton<IEventBus, EventBus>()
    // ...
    .BuildServiceProvider();

ServiceLocator.Services = Services;
```

Each module exposes a `ModuleRegistration.Register(IServiceCollection)` that adds its ViewModels and Views to the container. The shell calls them all from `ConfigureServices`.

## Event bus

Cross-module events are defined in `DBTeam.Core/Events/AppEvents.cs`:

| Event | Published by | Consumed by |
|---|---|---|
| `ConnectionOpenedEvent` | ConnectionDialog, ConnectionsPanel | ObjectExplorer, MainViewModel, Shell |
| `ConnectionsChangedEvent` | ConnectionDialog (after save) | ConnectionsPanel |
| `OpenQueryEditorRequest` | ObjectExplorer, Schema/Data Compare, Table Designer, Data Generator | Shell |
| `OpenDocumentRequest` | MainViewModel (menu commands) | Shell |
| `ShowPaneRequest` | MainViewModel (View menu) | Shell |

The shell subscribes in `MainWindow.xaml.cs` and manipulates AvalonDock accordingly.

## Data flow: "Execute a query"

1. User types SQL in `QueryEditorView` → `TextChanged` pushes to `QueryEditorViewModel.Sql`
2. User presses **F5** → `ExecuteCommand`
3. VM calls `IQueryExecutionService.ExecuteAsync(connection, request)`
4. `SqlServerQueryExecutionService` opens a `SqlConnection`, runs `ExecuteReaderAsync`, loads each result set into a `DataTable`
5. VM updates `Results`, `Messages`, `Elapsed`, `RowCount`, `StatusText`
6. View reacts via data binding (ItemsControl over `Results`)

## Data flow: "Autocomplete"

1. User types letter → `TextArea.TextEntered`
2. `QueryEditorView.ShowCompletion()` instantiates `CompletionWindow`
3. Items populated from `SqlCompletionProvider` — keywords + live tables (via `IDatabaseMetadataService.GetTablesAsync`) cached per `(connectionId, database)`
4. After a `.`, the provider extracts the identifier and fetches columns from `IDatabaseMetadataService.GetColumnsAsync`
5. `Enter`/`Tab` commits selection, handled by AvalonEdit's completion engine

## Connection persistence

- Model: `SqlConnectionInfo` (Id, Name, Server, Database, AuthMode, User, Password, …)
- Serialized to `%AppData%\DBTeam\connections.json`
- `Password` is encrypted with `ISecretProtector` (DPAPI-backed `DpapiProtector`) before write, decrypted on load
- `JsonConnectionStore` hides the format — swap it to put the store in an encrypted vault / SQLite later

## Theme

- `ThemeService` toggles `ModernWpf.ThemeManager.Current.ApplicationTheme`
- Persisted in `%AppData%\DBTeam\theme.json`
- Views use `{DynamicResource SystemControl*}` brushes from ModernWpf — they react to theme switch without reload

## Localization

- One `ResourceDictionary` per language in `src/DBTeam.App/Lang/xx-YY.xaml` with `<sys:String x:Key="...">`
- `LocalizationService.SetLanguage("fr-FR")` removes the previous merged dictionary and adds the new one via pack URI
- Bindings use `{DynamicResource Key}` so they live-update

## Extension points

Adding a new module (example: "Database Backup"):

```csharp
// src/Modules/DBTeam.Modules.Backup/ModuleRegistration.cs
public static void Register(IServiceCollection s)
{
    s.AddTransient<BackupViewModel>();
    s.AddTransient<BackupView>();
}
```

```csharp
// src/DBTeam.App/App.xaml.cs
DBTeam.Modules.Backup.ModuleRegistration.Register(s);
```

```csharp
// src/DBTeam.App/ViewModels/MainViewModel.cs
[RelayCommand]
private void Backup()
{
    var view = App.Services.GetRequiredService<DBTeam.Modules.Backup.Views.BackupView>();
    var bus = App.Services.GetRequiredService<IEventBus>();
    bus.Publish(new OpenDocumentRequest { Title = "Backup", Content = view });
}
```

```xml
<!-- src/DBTeam.App/Shell/MainWindow.xaml -->
<MenuItem Header="{DynamicResource Menu.Backup}" Command="{Binding BackupCommand}">
    <MenuItem.Icon><md:PackIcon Kind="BackupRestore"/></MenuItem.Icon>
</MenuItem>
```

Done — the module appears as a dockable document tab with full access to connections, metadata, and the event bus.
