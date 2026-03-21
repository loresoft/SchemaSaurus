using System.Diagnostics;

namespace SchemaSaurus.Metadata;

/// <summary>
/// Represents a unique constraint defined on a <see cref="Table"/>.
/// </summary>
/// <remarks>
/// A unique constraint enforces that no two rows share the same values in the
/// constrained columns (<c>UNIQUE</c>). Unlike a <see cref="PrimaryKey"/>, a table
/// may have multiple unique constraints and the columns may permit
/// <see langword="null"/> values (depending on the provider).
/// Unique constraints are listed in <see cref="Table.UniqueConstraints"/>.
/// </remarks>
[Equatable]
[DebuggerDisplay("{Name}")]
public sealed partial class UniqueConstraint
{
    /// <summary>
    /// Constraint name as defined in the database catalog.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Ordered list of column references that form the unique key.
    /// </summary>
    /// <remarks>
    /// Each <see cref="ColumnReference"/> includes the column identity and its
    /// <see cref="ColumnReference.SortDirection"/>. Defaults to an empty list.
    /// </remarks>
    [SequenceEquality]
    [JsonPropertyName("columns")]
    public IReadOnlyList<ColumnReference> Columns { get; init; } = [];
}
