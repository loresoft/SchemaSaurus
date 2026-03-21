namespace SchemaSaurus.Metadata;

/// <summary>
/// Provider-specific table-level flags and options.
/// </summary>
/// <remarks>
/// Encapsulated in a dedicated type so that provider-specific properties do not
/// pollute the <see cref="Table"/> type itself. Flags not covered by these properties
/// can be stored as <see cref="IAnnotatable.Annotations"/> on the owning
/// <see cref="Table"/>.
/// </remarks>
[Equatable]
public sealed partial class TableOptions
{
    /// <summary>
    /// Indicates whether this is a SQL Server temporal (system-versioned) table
    /// (<c>WITH (SYSTEM_VERSIONING = ON)</c>).
    /// When <see langword="true"/>, <see cref="HistoryTableName"/> identifies the
    /// associated history table.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    [JsonPropertyName("isTemporalTable")]
    public bool IsTemporalTable { get; init; }

    /// <summary>
    /// Schema-qualified name of the associated temporal history table (SQL Server).
    /// <see langword="null"/> when <see cref="IsTemporalTable"/> is
    /// <see langword="false"/>.
    /// </summary>
    [JsonPropertyName("historyTableName")]
    public SchemaQualifiedName? HistoryTableName { get; init; }

    /// <summary>
    /// Indicates whether the table is memory-optimized
    /// (SQL Server In-Memory OLTP, <c>WITH (MEMORY_OPTIMIZED = ON)</c>).
    /// Defaults to <see langword="false"/>.
    /// </summary>
    [JsonPropertyName("isMemoryOptimized")]
    public bool IsMemoryOptimized { get; init; }

    /// <summary>
    /// Indicates whether the table is a SQL Server FileTable
    /// (<c>AS FileTable</c> in <c>CREATE TABLE</c>).
    /// Defaults to <see langword="false"/>.
    /// </summary>
    [JsonPropertyName("isFileTable")]
    public bool IsFileTable { get; init; }
}
