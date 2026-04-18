using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace DBTeam.App.Services;

public sealed class LocalizationService
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DBTeam", "lang.json");

    public string Current { get; private set; } = "en-US";
    public event Action? LanguageChanged;

    public static string T(string key, string? fallback = null)
        => Application.Current?.TryFindResource(key) as string ?? fallback ?? key;

    public void LoadAndApply()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var doc = JsonDocument.Parse(File.ReadAllText(ConfigPath));
                if (doc.RootElement.TryGetProperty("Lang", out var el) && el.GetString() is { } l) Current = l;
            }
        }
        catch { }
        SetLanguage(Current);
    }

    public void SetLanguage(string code)
    {
        Current = code;
        var app = Application.Current;
        if (app is null) return;

        for (int i = app.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
        {
            var rd = app.Resources.MergedDictionaries[i];
            if (rd.Source is { } src && src.OriginalString.Contains("/Lang/", StringComparison.OrdinalIgnoreCase))
                app.Resources.MergedDictionaries.RemoveAt(i);
        }

        var uri = new Uri($"pack://application:,,,/DBTeam.App;component/Lang/{code}.xaml", UriKind.Absolute);
        try
        {
            var dict = new ResourceDictionary { Source = uri };
            app.Resources.MergedDictionaries.Add(dict);
            Save();
            LanguageChanged?.Invoke();
        }
        catch { }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(new { Lang = Current }));
        }
        catch { }
    }
}
