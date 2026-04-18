using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DBTeam.Modules.Terminal.ViewModels;

public partial class TerminalViewModel : ObservableObject, IDisposable
{
    private Process? _process;
    private StreamWriter? _stdin;

    [ObservableProperty] private string shell = "pwsh.exe";
    [ObservableProperty] private string workingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    [ObservableProperty] private string output = "";
    [ObservableProperty] private string currentInput = "";
    [ObservableProperty] private bool isRunning;
    [ObservableProperty] private string status = "Not started";

    public ObservableCollection<string> ShellChoices { get; } = new()
    {
        "pwsh.exe",
        "powershell.exe",
        "cmd.exe",
        "claude",
        "gh",
        "sqlcmd"
    };

    public ObservableCollection<string> QuickSnippets { get; } = new()
    {
        "claude",
        "claude --help",
        @"claude 'Explain this SQL query and suggest an index'",
        "gh repo view",
        "gh issue list",
        "gh pr list",
        @"sqlcmd -S localhost -d master -Q ""SELECT name FROM sys.databases""",
        "dotnet --version",
        "git status",
        "git log --oneline -10"
    };

    [RelayCommand]
    public void Start()
    {
        Stop();
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = Shell,
                WorkingDirectory = Directory.Exists(WorkingDirectory) ? WorkingDirectory : Environment.CurrentDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            // Interactive / no logo where supported
            if (Shell.Contains("pwsh") || Shell.Contains("powershell"))
                psi.Arguments = "-NoLogo -NoProfile";
            _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            _process.OutputDataReceived += (_, e) => Append(e.Data ?? "");
            _process.ErrorDataReceived  += (_, e) => Append(e.Data ?? "");
            _process.Exited += (_, _) => Application.Current?.Dispatcher?.Invoke(() =>
            {
                IsRunning = false;
                Status = $"Exited (code {_process?.ExitCode})";
            });
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            _stdin = _process.StandardInput;
            IsRunning = true;
            Status = $"Running · PID {_process.Id} · {Shell}";
            Append($"$ {Shell} started in {psi.WorkingDirectory}\n");
        }
        catch (Exception ex)
        {
            Status = ex.Message;
            Append($"\n[ERROR] {ex.Message}\n");
        }
    }

    [RelayCommand]
    public void Stop()
    {
        try { _stdin?.Dispose(); } catch { }
        try { if (_process is not null && !_process.HasExited) _process.Kill(true); } catch { }
        try { _process?.Dispose(); } catch { }
        _process = null; _stdin = null;
        IsRunning = false;
        Status = "Stopped";
    }

    [RelayCommand]
    public void Send()
    {
        if (_stdin is null) { Start(); if (_stdin is null) return; }
        var line = CurrentInput ?? "";
        Append($"> {line}\n");
        try { _stdin.WriteLine(line); _stdin.Flush(); } catch (Exception ex) { Append($"[ERROR] {ex.Message}\n"); }
        CurrentInput = "";
    }

    [RelayCommand]
    public void UseSnippet(string? snippet)
    {
        if (!string.IsNullOrEmpty(snippet)) CurrentInput = snippet;
    }

    [RelayCommand]
    public void Clear() => Output = "";

    private void Append(string s)
    {
        if (string.IsNullOrEmpty(s)) return;
        Application.Current?.Dispatcher?.Invoke(() =>
        {
            Output = Output + s + "\n";
            if (Output.Length > 200_000) Output = Output.Substring(Output.Length - 150_000);
        });
    }

    public void Dispose() => Stop();
}
