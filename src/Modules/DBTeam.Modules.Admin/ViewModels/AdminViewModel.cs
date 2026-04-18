using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Events;
using DBTeam.Core.Models;
using DBTeam.Modules.Admin.Engine;

namespace DBTeam.Modules.Admin.ViewModels;

public partial class AdminViewModel : ObservableObject
{
    private readonly IConnectionService _connSvc;
    private readonly IDatabaseMetadataService _meta;
    private readonly IEventBus _bus;

    public AdminViewModel(IConnectionService connSvc, IDatabaseMetadataService meta, IEventBus bus)
    {
        _connSvc = connSvc; _meta = meta; _bus = bus;
        Connections = new();
        Databases = new();
        _ = LoadConnectionsAsync();
    }

    public ObservableCollection<SqlConnectionInfo> Connections { get; }
    public ObservableCollection<string> Databases { get; }

    [ObservableProperty] private SqlConnectionInfo? connection;
    [ObservableProperty] private string? database;
    [ObservableProperty] private DataTable? databaseSizes;
    [ObservableProperty] private DataTable? logins;
    [ObservableProperty] private DataTable? users;
    [ObservableProperty] private DataTable? roles;
    [ObservableProperty] private DataTable? permissions;
    [ObservableProperty] private DataTable? indexes;
    [ObservableProperty] private DataTable? slowQueries;
    [ObservableProperty] private DataTable? sessions;
    [ObservableProperty] private string status = "Pick a connection and database, then Refresh.";
    [ObservableProperty] private bool isBusy;

    private async Task LoadConnectionsAsync()
    {
        var all = await _connSvc.LoadAllAsync();
        Connections.Clear(); foreach (var c in all) Connections.Add(c);
    }

    partial void OnConnectionChanged(SqlConnectionInfo? value) => _ = LoadDbsAsync();
    private async Task LoadDbsAsync()
    {
        Databases.Clear();
        if (Connection is null) return;
        try { foreach (var d in await _meta.GetDatabasesAsync(Connection)) Databases.Add(d); } catch { }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (Connection is null) return;
        IsBusy = true; Status = "Loading...";
        try
        {
            DatabaseSizes  = await AdminQueries.DatabaseSizeAsync(Connection);
            Logins         = await AdminQueries.LoginsAsync(Connection);
            Sessions       = await AdminQueries.ActiveSessionsAsync(Connection);
            if (!string.IsNullOrEmpty(Database))
            {
                Users       = await AdminQueries.UsersAsync(Connection, Database!);
                Roles       = await AdminQueries.RolesAsync(Connection, Database!);
                Permissions = await AdminQueries.PermissionsAsync(Connection, Database!);
                Indexes     = await AdminQueries.IndexFragmentationAsync(Connection, Database!);
                SlowQueries = await AdminQueries.SlowQueriesAsync(Connection, Database!);
            }
            Status = "Loaded";
        }
        catch (System.Exception ex) { Status = ex.Message; }
        IsBusy = false;
    }

    [RelayCommand]
    private void GenerateBackupScript()
    {
        if (Connection is null || string.IsNullOrEmpty(Database)) return;
        _bus.Publish(new OpenQueryEditorRequest
        {
            Connection = Connection,
            Database = Database,
            InitialSql = AdminQueries.GenerateBackupScript(Database!)
        });
    }

    [RelayCommand]
    private void GenerateRestoreScript()
    {
        if (Connection is null || string.IsNullOrEmpty(Database)) return;
        _bus.Publish(new OpenQueryEditorRequest
        {
            Connection = Connection,
            Database = Database,
            InitialSql = AdminQueries.GenerateRestoreScript(Database!, $@"C:\Backup\{Database}.bak")
        });
    }

    [RelayCommand]
    private void GenerateIndexMaintenanceScript()
    {
        if (Connection is null || Indexes is null) return;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("-- Index maintenance script");
        foreach (DataRow r in Indexes.Rows)
        {
            var rec = r["recommendation"]?.ToString();
            if (rec == "OK" || rec is null) continue;
            sb.AppendLine(AdminQueries.GenerateIndexRebuildScript(
                r["schema_name"]?.ToString() ?? "dbo",
                r["table_name"]?.ToString() ?? "",
                r["index_name"]?.ToString() ?? "",
                rec));
        }
        _bus.Publish(new OpenQueryEditorRequest
        {
            Connection = Connection,
            Database = Database,
            InitialSql = sb.ToString()
        });
    }
}
