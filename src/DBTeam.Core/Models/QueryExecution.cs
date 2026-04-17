using System;
using System.Collections.Generic;
using System.Data;

namespace DBTeam.Core.Models;

public sealed class QueryBatchResult
{
    public List<DataTable> ResultSets { get; } = new();
    public List<string> Messages { get; } = new();
    public int RowsAffected { get; set; }
    public TimeSpan Elapsed { get; set; }
    public Exception? Error { get; set; }
    public bool HasError => Error is not null;
}

public sealed class QueryRequest
{
    public string Sql { get; set; } = string.Empty;
    public int CommandTimeoutSeconds { get; set; } = 30;
    public Dictionary<string, object?> Parameters { get; } = new();
    public string? Database { get; set; }
}
