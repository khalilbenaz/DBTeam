using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Events;

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
    public void PickFolder()
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "Select Git repository" };
        if (dlg.ShowDialog() == true) { RepoPath = dlg.FolderName; Refresh(); }
    }

    [RelayCommand]
    public void Refresh()
    {
        Files.Clear();
        if (!Directory.Exists(RepoPath)) { Status = "Folder does not exist"; return; }
        try
        {
            Branch = RunGit("rev-parse --abbrev-ref HEAD").Trim();
            foreach (var f in Directory.GetFiles(RepoPath, "*.sql", SearchOption.AllDirectories))
                Files.Add(Path.GetRelativePath(RepoPath, f));
            GitLog = RunGit("log --oneline -15");
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
    public void Commit()
    {
        if (string.IsNullOrWhiteSpace(CommitMessage) || !Directory.Exists(RepoPath)) return;
        try
        {
            RunGit("add -A");
            var result = RunGit($"commit -m \"{CommitMessage.Replace("\"", "'")}\"");
            Status = result.Trim();
            CommitMessage = "";
            GitLog = RunGit("log --oneline -15");
        }
        catch (System.Exception ex) { Status = ex.Message; }
    }

    [RelayCommand] public void Pull() => Status = RunGitSafe("pull");
    [RelayCommand] public void Push() => Status = RunGitSafe("push");

    private string RunGit(string args)
    {
        var psi = new ProcessStartInfo("git", args)
        {
            WorkingDirectory = RepoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var p = Process.Start(psi);
        if (p is null) throw new Exception("git process failed to start");
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0 && string.IsNullOrEmpty(stdout)) throw new Exception(stderr);
        return stdout + stderr;
    }

    private string RunGitSafe(string args) { try { return RunGit(args).Trim(); } catch (Exception ex) { return ex.Message; } }
}
