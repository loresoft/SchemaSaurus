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
    /// Indicates whether this is a temporal (system-versioned) table
    /// (<c>WITH (SYSTEM_VERSIONING = ON)</c>).
    /// When <see langword="true"/>, <see cref="HistoryTable"/> identifies the
    /// associated history table.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    [JsonPropertyName("isTemporalTable")]
    public bool IsTemporalTable { get; init; }

    /// <summary>
    /// Schema-qualified name of the associated temporal history table.
    /// <see langword="null"/> when <see cref="IsTemporalTable"/> is
    /// </summary>
    [JsonPropertyName("historyTable")]
    public SchemaQualifiedName? HistoryTable { get; init; }

    /// <summary>
    /// Name of the column that stores the start of the temporal period.
    /// <see langword="null"/> when <see cref="IsTemporalTable"/> is
    /// <see langword="false"/> or the provider does not expose period metadata.
    /// </summary>
    [JsonPropertyName("periodStartColumnName")]
    public string? PeriodStartColumnName { get; init; }

    /// <summary>
    /// Name of the column that stores the end of the temporal period.
    /// <see langword="null"/> when <see cref="IsTemporalTable"/> is
    /// <see langword="false"/> or the provider does not expose period metadata.
    /// </summary>
    [JsonPropertyName("periodEndColumnName")]
    public string? PeriodEndColumnName { get; init; }

    /// <summary>
    /// Indicates whether the table is memory-optimized.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    [JsonPropertyName("isMemoryOptimized")]
    public bool IsMemoryOptimized { get; init; }

    /// <summary>
    /// Indicates whether the table is a file table.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    [JsonPropertyName("isFileTable")]
    public bool IsFileTable { get; init; }
}
