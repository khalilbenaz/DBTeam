using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Events;
using DBTeam.Core.Infrastructure;

namespace DBTeam.Modules.Git.ViewModels;

public partial class GitPanelViewModel : ObservableObject
{
    private readonly IEventBus _bus;
    private readonly IConnectionService _connSvc;

    public GitPanelViewModel(IEventBus bus, IConnectionService connSvc)
    {
        _bus = bus; _connSvc = connSvc;
        Files = new();
    }

    public ObservableCollection<string> Files { get; }

    [ObservableProperty] private string repoPath = "";
    [ObservableProperty] private string status = "Pick a Git repository folder.";
    [ObservableProperty] private string branch = "";
    [ObservableProperty] private string? selectedFile;
    [ObservableProperty] private string commitMessage = "";
    [ObservableProperty] private string gitLog = "";

    [RelayCommand]
    public async Task PickFolderAsync()
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "Select Git repository" };
        if (dlg.ShowDialog() == true) { RepoPath = dlg.FolderName; await RefreshAsync(); }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        Files.Clear();
        if (!Directory.Exists(RepoPath)) { Status = "Folder does not exist"; return; }
        try
        {
            Branch = (await RunGitAsync("rev-parse", "--abbrev-ref", "HEAD")).Trim();
            foreach (var f in await Task.Run(() => Directory.GetFiles(RepoPath, "*.sql", SearchOption.AllDirectories)))
                Files.Add(Path.GetRelativePath(RepoPath, f));
            GitLog = await RunGitAsync("log", "--oneline", "-15");
            Status = $"Branch: {Branch} · {Files.Count} .sql file(s)";
        }
        catch (System.Exception ex) { Status = ex.Message; }
    }

    [RelayCommand]
    public void OpenFile(string? rel)
    {
        var path = rel ?? SelectedFile; if (path is null) return;
        var full = Path.Combine(RepoPath, path);
        if (!File.Exists(full)) return;
        var sql = File.ReadAllText(full);
        var conn = _connSvc.Saved.FirstOrDefault();
        if (conn is null) { Status = "No saved connection to attach"; return; }
        _bus.Publish(new OpenQueryEditorRequest { Connection = conn, InitialSql = sql });
    }

    [RelayCommand]
    public async Task CommitAsync()
    {
        if (string.IsNullOrWhiteSpace(CommitMessage) || !Directory.Exists(RepoPath)) return;
        try
        {
            await RunGitAsync("add", "-A");
            // Le message est un argument à part entière (ArgumentList) : guillemets, $, \ et retours ligne sont conservés tels quels.
            var result = await RunGitAsync("commit", "-m", CommitMessage);
            Status = result.Trim();
            CommitMessage = "";
            GitLog = await RunGitAsync("log", "--oneline", "-15");
        }
        catch (System.Exception ex) { Status = ex.Message; }
    }

    [RelayCommand] public async Task PullAsync() => Status = await RunGitSafeAsync("pull");
    [RelayCommand] public async Task PushAsync() => Status = await RunGitSafeAsync("push");

    private async Task<string> RunGitAsync(params string[] args)
    {
        var r = await ProcessRunner.RunAsync("git", args, RepoPath);
        if (r.ExitCode != 0 && string.IsNullOrEmpty(r.StdOut)) throw new Exception(r.StdErr);
        return r.StdOut + r.StdErr;
    }

    private async Task<string> RunGitSafeAsync(params string[] args)
    {
        try { return (await RunGitAsync(args)).Trim(); } catch (Exception ex) { return ex.Message; }
    }
}
