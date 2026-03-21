using System.Diagnostics;

using SchemaSaurus.Metadata.Converters;

namespace SchemaSaurus.Metadata;

/// <summary>
/// Represents an index defined on a <see cref="Table"/> or <see cref="View"/>.
/// </summary>
/// <remarks>
/// Covers clustered, non-clustered, unique, filtered (partial), and covering indexes.
/// Key and included columns are accessible through <see cref="Columns"/>.
/// </remarks>
[Equatable]
[DebuggerDisplay("{Name}")]
public sealed partial class Index : IAnnotatable
{
    /// <summary>
    /// Catalog-level name that uniquely identifies this index within its parent relation
    /// (e.g., <c>"IX_Order_CustomerId"</c>).
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Ordered list of key columns and, for covering indexes, included columns.
    /// Included columns appear after key columns and have
    /// <see cref="IndexColumn.IsIncludedColumn"/> set to <see langword="true"/>.
    /// Defaults to an empty collection.
    /// </summary>
    [SequenceEquality]
    [JsonPropertyName("columns")]
    public IReadOnlyList<IndexColumn> Columns { get; init; } = [];

    /// <summary>
    /// Indicates whether the index enforces uniqueness across all key columns.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    [JsonPropertyName("isUnique")]
    public bool IsUnique { get; init; }

    /// <summary>
    /// Indicates whether the index is physically clustered — the table data is stored in
    /// index key order (SQL Server <c>CLUSTERED</c>).
    /// Not meaningful for providers that do not support clustered indexes.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    [JsonPropertyName("isClustered")]
    public bool IsClustered { get; init; }

    /// <summary>
    /// Indicates whether this is a filtered (SQL Server) or partial (PostgreSQL) index.
    /// When <see langword="true"/>, <see cref="FilterExpression"/> contains the predicate.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    [JsonPropertyName("isFiltered")]
    public bool IsFiltered { get; init; }

    /// <summary>
    /// SQL predicate expression for a filtered or partial index
    /// (e.g., <c>"([IsActive] = 1)"</c>).
    /// <see langword="null"/> when <see cref="IsFiltered"/> is <see langword="false"/>.
    /// </summary>
    [JsonPropertyName("filterExpression")]
    public string? FilterExpression { get; init; }

    /// <summary>
    /// Physical index access method or structure type
    /// (e.g., <c>"BTREE"</c>, <c>"HASH"</c>, <c>"GIN"</c>, <c>"GiST"</c>).
    /// <see langword="null"/> when the provider does not expose this information.
    /// </summary>
    [JsonPropertyName("indexType")]
    public string? IndexType { get; init; }

    /// <summary>
    /// Index fill factor as a percentage (<c>1</c>–<c>100</c>), controlling how full each
    /// index page is packed during creation or rebuild.
    /// <see langword="null"/> when the server default fill factor is used.
    /// </summary>
    [JsonPropertyName("fillFactor")]
    public int? FillFactor { get; init; }

    /// <summary>
    /// Indicates whether this index is currently disabled
    /// (SQL Server <c>ALTER INDEX … DISABLE</c>).
    /// Defaults to <see langword="false"/>.
    /// </summary>
    [JsonPropertyName("isDisabled")]
    public bool IsDisabled { get; init; }

    /// <inheritdoc />
    [DictionaryEquality]
    [JsonPropertyName("annotations")]
    [JsonConverter(typeof(ReadOnlyDictionaryConverter<string, object?>))]
    public IReadOnlyDictionary<string, object?> Annotations { get; init; } = new Dictionary<string, object?>();
}
