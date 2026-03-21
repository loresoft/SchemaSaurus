using System.Diagnostics;

namespace SchemaSaurus.Metadata;

/// <summary>
/// Represents the primary key constraint of a <see cref="Table"/>.
/// </summary>
/// <remarks>
/// A primary key enforces entity integrity by guaranteeing that the key columns are
/// unique and non-null. The constraint's backing index may be clustered or non-clustered,
/// controlled by <see cref="IsClustered"/>.
/// </remarks>
[Equatable]
[DebuggerDisplay("{Name}")]
public sealed partial class PrimaryKey
{
    /// <summary>
    /// Catalog-level name that uniquely identifies this primary key constraint
    /// within the database (e.g., <c>"PK_Order"</c>).
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Ordered list of <see cref="ColumnReference"/> entries that form the primary key,
    /// each carrying the column name and its <see cref="ColumnReference.SortDirection"/>
    /// within the backing index.
    /// Defaults to an empty collection.
    /// </summary>
    [SequenceEquality]
    [JsonPropertyName("columns")]
    public IReadOnlyList<ColumnReference> Columns { get; init; } = [];

    /// <summary>
    /// Indicates whether the index backing this primary key is physically clustered
    /// (SQL Server <c>CLUSTERED</c>).
    /// Defaults to <see langword="true"/> for SQL Server; not meaningful for providers
    /// that do not support clustered indexes.
    /// </summary>
    [JsonPropertyName("isClustered")]
    public bool IsClustered { get; init; } = true;
}
