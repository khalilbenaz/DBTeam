using System;
using System.IO;
using System.Text.Json;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Infrastructure;

namespace DBTeam.Modules.AiAssistant.Engine;

public sealed class AiSettingsStore
{
    private static readonly string DefaultFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DBTeam", "ai.json");
    private static readonly JsonSerializerOptions WriteOpts = new() { WriteIndented = true };

    private readonly ISecretProtector _protector;
    private readonly string _filePath;
    private string? _unreadableProtectedKey;

    public AiSettingsStore(ISecretProtector protector) : this(protector, DefaultFilePath) { }

    internal AiSettingsStore(ISecretProtector protector, string filePath)
    {
        _protector = protector;
        _filePath = filePath;
    }

    /// <summary>Anomalie du dernier <see cref="Load"/> (clé illisible, fichier invalide), à afficher à l'utilisateur ; null si tout va bien.</summary>
    public string? LastWarning { get; private set; }

    public AiSettings Load()
    {
        LastWarning = null;
        _unreadableProtectedKey = null;
        if (!File.Exists(_filePath)) return new AiSettings();
        AiSettings s;
        try
        {
            s = JsonSerializer.Deserialize<AiSettings>(File.ReadAllText(_filePath)) ?? new AiSettings();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            LastWarning = $"ai.json est illisible ({ex.Message}) : paramètres par défaut utilisés.";
            return new AiSettings();
        }
        if (!string.IsNullOrEmpty(s.ApiKey))
        {
            var stored = s.ApiKey;
            try { s.ApiKey = _protector.Unprotect(stored); }
            catch (SecretUnreadableException)
            {
                _unreadableProtectedKey = stored;
                s.ApiKey = "";
                LastWarning = "La clé API enregistrée est illisible (chiffrée par un autre profil Windows ou une autre machine). Ressaisissez-la ; l'ancienne valeur est conservée tant que vous ne la remplacez pas.";
            }
        }
        return s;
    }

    /// <summary>Écrit les paramètres de façon atomique. Propage les erreurs d'E/S : un échec n'est plus silencieux.</summary>
    public void Save(AiSettings s)
    {
        var clone = new AiSettings
        {
            Provider = s.Provider, Endpoint = s.Endpoint, Model = s.Model,
            ApiKey = !string.IsNullOrEmpty(s.ApiKey) ? _protector.Protect(s.ApiKey) : (_unreadableProtectedKey ?? ""),
            MaxTokens = s.MaxTokens, SystemPrompt = s.SystemPrompt
        };
        AtomicFile.WriteAllText(_filePath, JsonSerializer.Serialize(clone, WriteOpts));
        if (!string.IsNullOrEmpty(s.ApiKey)) { _unreadableProtectedKey = null; LastWarning = null; }
    }
}
