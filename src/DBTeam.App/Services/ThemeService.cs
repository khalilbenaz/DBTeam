using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using ModernWpf;

namespace DBTeam.App.Services;

public enum AppTheme { Dark, Light, System }

public sealed class ThemeService
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DBTeam", "theme.json");

    public AppTheme Current { get; private set; } = AppTheme.Light;
    public event Action<AppTheme>? ThemeChanged;

    public void Apply(AppTheme theme)
    {
        Current = theme;
        ThemeManager.Current.ApplicationTheme = theme switch
        {
            AppTheme.Light => ApplicationTheme.Light,
            AppTheme.Dark => ApplicationTheme.Dark,
            _ => null
        };
        Save();
        ThemeChanged?.Invoke(theme);
    }

    public void LoadAndApply()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("Theme", out var el) && Enum.TryParse<AppTheme>(el.GetString(), out var t))
                    Current = t;
            }
        }
        catch { }
        Apply(Current);
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(new { Theme = Current.ToString() }));
        }
        catch { }
    }
}
