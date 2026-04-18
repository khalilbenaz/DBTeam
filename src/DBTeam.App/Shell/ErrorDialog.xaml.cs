using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace DBTeam.App.Shell;

public partial class ErrorDialog : Window
{
    private readonly string _details;

    public ErrorDialog(Exception ex, string source)
    {
        InitializeComponent();
        _details = ex.ToString();
        SourceText.Text = source;
        MessageText.Text = ex.Message;
        DetailsText.Text = _details;
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        try { Clipboard.SetText(_details); } catch { }
    }

    private void OpenLogs_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var logs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DBTeam", "logs");
            if (!Directory.Exists(logs)) Directory.CreateDirectory(logs);
            Process.Start(new ProcessStartInfo(logs) { UseShellExecute = true });
        }
        catch { }
    }

    private void Report_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var title = Uri.EscapeDataString("[bug] " + (MessageText.Text ?? "crash"));
            var body = Uri.EscapeDataString("**Source**: " + (SourceText.Text ?? "") + "\n\n```\n" + _details + "\n```\n");
            var url = $"https://github.com/khalilbenaz/DBTeam/issues/new?title={title}&body={body}";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch { }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
