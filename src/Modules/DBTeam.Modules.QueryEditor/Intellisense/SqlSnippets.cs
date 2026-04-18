using System.Collections.Generic;

namespace DBTeam.Modules.QueryEditor.Intellisense;

public sealed class SqlSnippet
{
    public string Trigger { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string Body { get; init; } = "";
}

public static class SqlSnippets
{
    public static readonly SqlSnippet[] All =
    {
        new() { Trigger = "sel", Title = "SELECT TOP 100", Description = "Select top N from table",
                Body = "SELECT TOP 100 *\nFROM [dbo].[{0}]\nWHERE 1 = 1\nORDER BY 1;" },
        new() { Trigger = "selw", Title = "SELECT … WHERE", Description = "Parameterized select",
                Body = "SELECT *\nFROM [dbo].[{0}]\nWHERE [{1}] = {2};" },
        new() { Trigger = "ins", Title = "INSERT", Description = "Insert statement skeleton",
                Body = "INSERT INTO [dbo].[{0}] ({1})\nVALUES ({2});" },
        new() { Trigger = "upd", Title = "UPDATE", Description = "Update with JOIN",
                Body = "UPDATE t\nSET t.[{1}] = {2}\nFROM [dbo].[{0}] AS t\nWHERE t.[{3}] = {4};" },
        new() { Trigger = "del", Title = "DELETE", Description = "Delete with WHERE",
                Body = "DELETE FROM [dbo].[{0}]\nWHERE [{1}] = {2};" },
        new() { Trigger = "mrg", Title = "MERGE", Description = "MERGE upsert skeleton",
                Body = "MERGE [dbo].[{0}] AS tgt\nUSING (SELECT * FROM [dbo].[{1}]) AS src\n   ON tgt.[{2}] = src.[{2}]\nWHEN MATCHED THEN\n   UPDATE SET tgt.[col] = src.[col]\nWHEN NOT MATCHED BY TARGET THEN\n   INSERT ([col]) VALUES (src.[col])\nWHEN NOT MATCHED BY SOURCE THEN DELETE;" },
        new() { Trigger = "cte", Title = "WITH CTE", Description = "Common table expression",
                Body = "WITH Cte AS (\n    SELECT *\n    FROM [dbo].[{0}]\n    WHERE 1 = 1\n)\nSELECT *\nFROM Cte;" },
        new() { Trigger = "join", Title = "INNER JOIN", Description = "Join two tables",
                Body = "SELECT a.*, b.*\nFROM [dbo].[{0}] AS a\nINNER JOIN [dbo].[{1}] AS b ON a.[{2}] = b.[{3}];" },
        new() { Trigger = "tbl", Title = "CREATE TABLE", Description = "New table with identity PK",
                Body = "CREATE TABLE [dbo].[{0}] (\n    [Id]        INT IDENTITY(1,1) NOT NULL,\n    [CreatedAt] DATETIME2(3) NOT NULL CONSTRAINT [DF_{0}_CreatedAt] DEFAULT SYSUTCDATETIME(),\n    CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED ([Id])\n);" },
        new() { Trigger = "idx", Title = "CREATE INDEX", Description = "Non-clustered index",
                Body = "CREATE NONCLUSTERED INDEX [IX_{0}_{1}]\n    ON [dbo].[{0}] ([{1}])\n    INCLUDE ([{2}]);" },
        new() { Trigger = "sp", Title = "CREATE PROCEDURE", Description = "Stored procedure skeleton",
                Body = "CREATE OR ALTER PROCEDURE [dbo].[{0}]\n    @{1} {2}\nAS\nBEGIN\n    SET NOCOUNT ON;\n    SELECT 1;\nEND;" },
        new() { Trigger = "fn", Title = "CREATE FUNCTION (scalar)", Description = "Scalar UDF",
                Body = "CREATE OR ALTER FUNCTION [dbo].[{0}] (@{1} {2})\nRETURNS {3}\nAS\nBEGIN\n    RETURN {4};\nEND;" },
        new() { Trigger = "tryc", Title = "TRY/CATCH", Description = "Error handling block",
                Body = "BEGIN TRY\n    -- code\nEND TRY\nBEGIN CATCH\n    DECLARE @err INT = ERROR_NUMBER(),\n            @msg NVARCHAR(4000) = ERROR_MESSAGE();\n    RAISERROR(@msg, 16, 1);\nEND CATCH;" },
        new() { Trigger = "tran", Title = "BEGIN TRAN", Description = "Transaction skeleton",
                Body = "SET XACT_ABORT ON;\nBEGIN TRAN;\n    -- work\nCOMMIT TRAN;" },
        new() { Trigger = "pivot", Title = "PIVOT", Description = "Pivot aggregation",
                Body = "SELECT *\nFROM (\n    SELECT [{0}], [{1}], [{2}]\n    FROM [dbo].[{3}]\n) src\nPIVOT (\n    SUM([{2}]) FOR [{1}] IN ([A], [B], [C])\n) AS p;" },
        new() { Trigger = "row", Title = "ROW_NUMBER", Description = "Window function",
                Body = "SELECT ROW_NUMBER() OVER (PARTITION BY [{0}] ORDER BY [{1}] DESC) AS rn, *\nFROM [dbo].[{2}];" },
    };
}
