using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DBTeam.App.Services;

public sealed class SessionState
{
    public List<SessionDocument> Documents { get; set; } = new();
    public string? ActiveDocumentId { get; set; }
}

public sealed class SessionDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Kind { get; set; } = "Query";
    public string Title { get; set; } = "";
    public string? ConnectionId { get; set; }
    public string? Database { get; set; }
    public string? Sql { get; set; }
}

public sealed class SessionService
{
    private static readonly string AppDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DBTeam");
    private static readonly string FilePath = Path.Combine(AppDir, "session.json");
    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

    public SessionState Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new SessionState();
            return JsonSerializer.Deserialize<SessionState>(File.ReadAllText(FilePath), Opts) ?? new SessionState();
        }
        catch { return new SessionState(); }
    }

    public void Save(SessionState state)
    {
        try
        {
            Directory.CreateDirectory(AppDir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(state, Opts));
        }
        catch { }
    }
}
