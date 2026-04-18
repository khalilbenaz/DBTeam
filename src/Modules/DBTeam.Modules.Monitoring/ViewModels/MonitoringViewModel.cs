using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Models;
using DBTeam.Data.Sql;
using Microsoft.Data.SqlClient;

namespace DBTeam.Modules.Monitoring.ViewModels;

public sealed class Sample
{
    public DateTime At { get; set; }
    public double ActiveSessions { get; set; }
    public double RunningRequests { get; set; }
    public double BufferCacheHitRatio { get; set; }
    public double PageLifeExpectancy { get; set; }
    public double CpuPct { get; set; }
}

public partial class MonitoringViewModel : ObservableObject, IDisposable
{
    private readonly IConnectionService _connSvc;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(5) };

    public MonitoringViewModel(IConnectionService connSvc)
    {
        _connSvc = connSvc;
        Connections = new();
        _timer.Tick += async (_, _) => await PollAsync();
        _ = LoadConnectionsAsync();
    }

    public ObservableCollection<SqlConnectionInfo> Connections { get; }
    public ObservableCollection<Sample> Samples { get; } = new();
    public ObservableCollection<DataRow> TopWaits { get; } = new();

    [ObservableProperty] private SqlConnectionInfo? connection;
    [ObservableProperty] private Sample? latest;
    [ObservableProperty] private bool isRunning;
    [ObservableProperty] private int intervalSeconds = 5;
    [ObservableProperty] private string status = "Stopped";

    private async Task LoadConnectionsAsync()
    {
        var all = await _connSvc.LoadAllAsync();
        Connections.Clear(); foreach (var c in all) Connections.Add(c);
    }

    partial void OnIntervalSecondsChanged(int value)
    {
        if (value < 1) value = 1;
        _timer.Interval = TimeSpan.FromSeconds(value);
    }

    [RelayCommand]
    public void Start()
    {
        if (Connection is null) { Status = "Pick a connection"; return; }
        _timer.Interval = TimeSpan.FromSeconds(Math.Max(1, IntervalSeconds));
        _timer.Start(); IsRunning = true; Status = "Polling...";
        _ = PollAsync();
    }

    [RelayCommand]
    public void Stop()
    {
        _timer.Stop(); IsRunning = false; Status = "Stopped";
    }

    [RelayCommand]
    public void Clear() { Samples.Clear(); TopWaits.Clear(); }

    private async Task PollAsync()
    {
        if (Connection is null) return;
        try
        {
            var s = new Sample { At = DateTime.Now };
            await using var conn = new SqlConnection(ConnectionStringFactory.Build(Connection, "master"));
            await conn.OpenAsync();
            async Task<double> Single(string sql)
            {
                await using var cmd = new SqlCommand(sql, conn);
                var r = await cmd.ExecuteScalarAsync();
                return r is null or DBNull ? 0 : Convert.ToDouble(r, System.Globalization.CultureInfo.InvariantCulture);
            }
            s.ActiveSessions = await Single("SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE is_user_process = 1");
            s.RunningRequests = await Single("SELECT COUNT(*) FROM sys.dm_exec_requests WHERE session_id > 50");
            s.BufferCacheHitRatio = await Single("SELECT CAST(a.cntr_value*100.0/NULLIF(b.cntr_value,0) AS DECIMAL(6,2)) FROM sys.dm_os_performance_counters a JOIN sys.dm_os_performance_counters b ON a.object_name=b.object_name WHERE a.counter_name='Buffer cache hit ratio' AND b.counter_name='Buffer cache hit ratio base'");
            s.PageLifeExpectancy = await Single("SELECT cntr_value FROM sys.dm_os_performance_counters WHERE object_name LIKE '%Buffer Manager%' AND counter_name='Page life expectancy'");

            Samples.Add(s);
            while (Samples.Count > 120) Samples.RemoveAt(0);
            Latest = s;

            // Top waits
            await using var waitCmd = new SqlCommand(@"
SELECT TOP 10 wait_type, waiting_tasks_count, wait_time_ms, max_wait_time_ms
FROM sys.dm_os_wait_stats
WHERE wait_type NOT LIKE '%SLEEP%' AND wait_type NOT LIKE 'BROKER%'
ORDER BY wait_time_ms DESC", conn);
            await using var r = await waitCmd.ExecuteReaderAsync();
            var dt = new DataTable(); dt.Load(r);
            TopWaits.Clear();
            foreach (DataRow row in dt.Rows) TopWaits.Add(row);

            Status = $"Last poll: {s.At:HH:mm:ss}";
        }
        catch (Exception ex) { Status = ex.Message; }
    }

    public void Dispose() => _timer.Stop();
}
