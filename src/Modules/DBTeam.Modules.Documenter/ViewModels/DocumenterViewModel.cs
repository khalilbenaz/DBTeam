using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Models;
using DBTeam.Modules.Documenter.Engine;

namespace DBTeam.Modules.Documenter.ViewModels;

public partial class DocumenterViewModel : ObservableObject
{
    private readonly IConnectionService _connSvc;
    private readonly IDatabaseMetadataService _meta;
    private readonly HtmlDocumenter _engine;

    public DocumenterViewModel(IConnectionService connSvc, IDatabaseMetadataService meta)
    {
        _connSvc = connSvc; _meta = meta;
        _engine = new HtmlDocumenter(meta);
        Connections = new(); Databases = new();
        _ = LoadAsync();
    }

    public ObservableCollection<SqlConnectionInfo> Connections { get; }
    public ObservableCollection<string> Databases { get; }

    [ObservableProperty] private SqlConnectionInfo? connection;
    [ObservableProperty] private string? database;
    [ObservableProperty] private string status = "Ready";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? lastOutput;

    private async Task LoadAsync()
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
    private async Task GenerateAsync()
    {
        if (Connection is null || string.IsNullOrEmpty(Database)) { Status = "Pick connection + database"; return; }
        IsBusy = true; Status = "Generating documentation...";
        try
        {
            var html = await _engine.BuildAsync(Connection, Database!);
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DBTeam-Docs");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"{Database}-{DateTime.Now:yyyyMMdd-HHmmss}.html");
            await File.WriteAllTextAsync(path, html);
            LastOutput = path;
            Status = $"Saved to {path}";
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex) { Status = ex.Message; }
        IsBusy = false;
    }

    [RelayCommand]
    private void OpenLast()
    {
        if (!string.IsNullOrEmpty(LastOutput) && File.Exists(LastOutput))
            Process.Start(new ProcessStartInfo(LastOutput) { UseShellExecute = true });
    }
}
