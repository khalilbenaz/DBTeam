using System.Collections.Generic;
using System.Linq;

namespace DBTeam.Core.Models;

public enum DbObjectKind
{
    Server, Database, SchemaFolder, Schema,
    TableFolder, Table, ViewFolder, View,
    ColumnFolder, Column,
    IndexFolder, Index,
    KeyFolder, PrimaryKey, ForeignKey, UniqueKey,
    ConstraintFolder, Constraint,
    TriggerFolder, Trigger,
    ProgrammabilityFolder,
    StoredProcedureFolder, StoredProcedure,
    FunctionFolder, Function,
    ParameterFolder, Parameter,
    SynonymFolder, Synonym,
    UserFolder, User,
    RoleFolder, Role,
    LoginFolder, Login
}

public sealed class DbObjectNode
{
    public string Name { get; set; } = string.Empty;
    public string? Schema { get; set; }
    public string? Parent { get; set; }
    public DbObjectKind Kind { get; set; }
    public bool HasChildren { get; set; }
    public object? Tag { get; set; }
    public List<DbObjectNode> Children { get; } = new();
    public string DisplayName => Schema is null ? Name : $"{Schema}.{Name}";
}

public sealed class ColumnInfo
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool IsNullable { get; set; }
    public bool IsIdentity { get; set; }
    public bool IsComputed { get; set; }
    public string? DefaultExpression { get; set; }
    public string? CollationName { get; set; }
    public int OrdinalPosition { get; set; }
    public bool IsPrimaryKey { get; set; }
}

public sealed class IndexInfo
{
    public string Name { get; set; } = string.Empty;
    public bool IsUnique { get; set; }
    public bool IsClustered { get; set; }
    public bool IsPrimaryKey { get; set; }
    public List<string> Columns { get; } = new();
    public List<string> IncludedColumns { get; } = new();
}

public sealed class RoutineParameter
{
    public string Name { get; set; } = "";
    public string DataType { get; set; } = "";
    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool IsOutput { get; set; }
    public bool HasDefault { get; set; }
    public int Ordinal { get; set; }
}

public sealed class RoutineSignature
{
    public string Schema { get; set; } = "dbo";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = ""; // P, FN, IF, TF, FS, FT
    public string? ReturnType { get; set; }
    public List<RoutineParameter> Parameters { get; } = new();
    public string QualifiedName => $"{Schema}.{Name}";
    public string Display
    {
        get
        {
            var ps = string.Join(", ", Parameters.Select(p =>
                $"{p.Name} {FormatType(p)}" + (p.IsOutput ? " OUTPUT" : "")));
            var ret = ReturnType is null ? "" : $" → {ReturnType}";
            return $"{QualifiedName}({ps}){ret}";
        }
    }
    private static string FormatType(RoutineParameter p)
    {
        var t = (p.DataType ?? "").ToLowerInvariant();
        return t switch
        {
            "varchar" or "char" or "varbinary" or "binary" => $"{p.DataType}({(p.MaxLength == -1 ? "MAX" : p.MaxLength?.ToString() ?? "?")})",
            "nvarchar" or "nchar" => $"{p.DataType}({(p.MaxLength == -1 ? "MAX" : ((p.MaxLength ?? 0) / 2).ToString())})",
            "decimal" or "numeric" => $"{p.DataType}({p.Precision},{p.Scale})",
            _ => p.DataType
        };
    }
}

public sealed class ForeignKeyInfo
{
    public string Name { get; set; } = string.Empty;
    public string ReferencedSchema { get; set; } = string.Empty;
    public string ReferencedTable { get; set; } = string.Empty;
    public List<(string Column, string ReferencedColumn)> Columns { get; } = new();
    public string DeleteAction { get; set; } = "NO_ACTION";
    public string UpdateAction { get; set; } = "NO_ACTION";
}
