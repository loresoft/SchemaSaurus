using System.Diagnostics;

namespace SchemaSaurus.Metadata;

/// <summary>
/// A reference to a <see cref="Column"/> with an associated sort direction, used in
/// primary keys, unique constraints, and index column lists.
/// </summary>
/// <remarks>
/// The column is serialized by <see cref="ColumnName"/> only and resolved back to the
/// full <see cref="Column"/> instance during
/// <see cref="DatabaseModelExtensions.ResolveReferences"/>.
/// </remarks>
[Equatable]
[DebuggerDisplay("{ColumnName}")]
public partial class ColumnReference
{
    /// <summary>
    /// Name of the referenced column within its parent table or view.
    /// Acts as the serialization key and is used to resolve the <see cref="Column"/>
    /// instance during model rewiring.
    /// </summary>
    [JsonPropertyName("columnName")]
    public required string ColumnName { get; init; }

    /// <summary>
    /// Sort direction for this column within the constraint or index definition
    /// (e.g., <c>ASC</c> or <c>DESC</c> in a <c>PRIMARY KEY</c> or <c>CREATE INDEX</c> clause).
    /// Defaults to <see cref="SortDirection.Ascending"/>.
    /// </summary>
    [JsonPropertyName("sortDirection")]
    public SortDirection SortDirection { get; init; } = SortDirection.Ascending;

    /// <summary>
    /// Resolved <see cref="Column"/> instance that <see cref="ColumnName"/> refers to.
    /// Populated during <see cref="DatabaseModelExtensions.ResolveReferences"/>; excluded from
    /// JSON serialization and equality comparison.
    /// </summary>
    [JsonIgnore]
    [IgnoreEquality]
    public Column Column { get; internal set; } = null!;
}
