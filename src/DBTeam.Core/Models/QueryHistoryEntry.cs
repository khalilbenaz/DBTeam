using System;

namespace DBTeam.Core.Models;

public sealed class QueryHistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public string Sql { get; set; } = string.Empty;
    public string? ConnectionName { get; set; }
    public string? Database { get; set; }
    public TimeSpan Elapsed { get; set; }
    public int RowsAffected { get; set; }
    public bool Success { get; set; }
    public bool IsFavorite { get; set; }
    public string? Label { get; set; }
    public string Preview => Sql.Length <= 80 ? Sql : Sql[..80] + "…";
}
