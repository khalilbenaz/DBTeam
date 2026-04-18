using System;
using System.IO;
using System.Text.Json;
using DBTeam.Core.Abstractions;

namespace DBTeam.Modules.AiAssistant.Engine;

public sealed class AiSettingsStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DBTeam", "ai.json");

    private readonly ISecretProtector _protector;
    public AiSettingsStore(ISecretProtector protector) { _protector = protector; }

    public AiSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new AiSettings();
            var s = JsonSerializer.Deserialize<AiSettings>(File.ReadAllText(FilePath)) ?? new AiSettings();
            if (!string.IsNullOrEmpty(s.ApiKey)) s.ApiKey = _protector.Unprotect(s.ApiKey);
            return s;
        }
        catch { return new AiSettings(); }
    }

    public void Save(AiSettings s)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var clone = new AiSettings
            {
                Provider = s.Provider, Endpoint = s.Endpoint, Model = s.Model,
                ApiKey = string.IsNullOrEmpty(s.ApiKey) ? "" : _protector.Protect(s.ApiKey),
                MaxTokens = s.MaxTokens, SystemPrompt = s.SystemPrompt
            };
            File.WriteAllText(FilePath, JsonSerializer.Serialize(clone, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
