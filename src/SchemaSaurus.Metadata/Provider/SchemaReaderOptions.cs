namespace SchemaSaurus.Metadata.Provider;

/// <summary>
/// Controls which database objects and metadata are included when an
/// <see cref="IDatabaseSchemaReader"/> reads a database schema.
/// </summary>
/// <remarks>
/// Filter list entries are passed to the provider as written and compared with the
/// database's own identifier rules. An empty list means "include all."
/// Boolean flags default to <see langword="true"/> so that a default-constructed instance
/// captures a complete snapshot.
/// </remarks>
public sealed class SchemaReaderOptions
{
    /// <summary>
    /// Schema names to include (e.g., <c>"dbo"</c>, <c>"public"</c>).
    /// When empty, objects from all schemas are included.
    /// </summary>
    public IReadOnlyList<string> Schemas { get; init; } = [];

    /// <summary>
    /// Table names to include. When empty, all tables are included.
    /// Entries may be schema qualified (<c>"sales.orders"</c>), which pins the table to that
    /// schema; the first dot separates the schema from the table name. Unqualified entries
    /// (<c>"orders"</c>) match the name in any schema; combine with <see cref="Schemas"/> for
    /// scoped filtering.
    /// </summary>
    public IReadOnlyList<string> Tables { get; init; } = [];

    /// <summary>Whether to include <see cref="View"/> objects. Default is <see langword="true"/>.</summary>
    public bool IncludeViews { get; init; } = true;

    /// <summary>Whether to include <see cref="StoredProcedure"/> objects. Default is <see langword="true"/>.</summary>
    public bool IncludeStoredProcedures { get; init; } = true;

    /// <summary>Whether to include <see cref="ScalarFunction"/> objects. Default is <see langword="true"/>.</summary>
    public bool IncludeScalarFunctions { get; init; } = true;

    /// <summary>Whether to include <see cref="TableValuedFunction"/> objects. Default is <see langword="true"/>.</summary>
    public bool IncludeTableValuedFunctions { get; init; } = true;

    /// <summary>Whether to include <see cref="Sequence"/> objects. Default is <see langword="true"/>.</summary>
    public bool IncludeSequences { get; init; } = true;

    /// <summary>Whether to include <see cref="UserDefinedType"/> objects. Default is <see langword="true"/>.</summary>
    public bool IncludeUserDefinedTypes { get; init; } = true;
}
