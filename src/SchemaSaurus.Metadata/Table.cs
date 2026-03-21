namespace SchemaSaurus.Metadata;

/// <summary>
/// Represents a persistent base table (as opposed to a <see cref="View"/>) in the database.
/// </summary>
/// <remarks>
/// Extends <see cref="RelationBase"/> with constraint collections (primary key, unique,
/// check, and foreign key), provider-specific <see cref="TableOptions"/>, and an estimated
/// row count sourced from database statistics.
/// </remarks>
[Equatable]
public sealed partial class Table : RelationBase
{
    /// <summary>
    /// Primary key constraint defined on this table.
    /// <see langword="null"/> for heap tables (tables created without a primary key).
    /// </summary>
    [JsonPropertyName("primaryKey")]
    public PrimaryKey? PrimaryKey { get; init; }

    /// <summary>
    /// Unique constraints defined on this table.
    /// Defaults to an empty collection.
    /// </summary>
    [SequenceEquality]
    [JsonPropertyName("uniqueConstraints")]
    public IReadOnlyList<UniqueConstraint> UniqueConstraints { get; init; } = [];

    /// <summary>
    /// Check constraints that enforce domain-level validation rules on this table.
    /// Defaults to an empty collection.
    /// </summary>
    [SequenceEquality]
    [JsonPropertyName("checkConstraints")]
    public IReadOnlyList<CheckConstraint> CheckConstraints { get; init; } = [];

    /// <summary>
    /// Foreign key constraints where this table is the dependent (referencing) side.
    /// Each <see cref="ForeignKey"/> identifies the principal table and the column
    /// mappings that define the relationship.
    /// Defaults to an empty collection.
    /// </summary>
    [SequenceEquality]
    [JsonPropertyName("foreignKeys")]
    public IReadOnlyList<ForeignKey> ForeignKeys { get; init; } = [];

    /// <summary>
    /// Provider-specific table-level flags and options (e.g., memory-optimized,
    /// temporal, or ledger table indicators).
    /// Always non-null; defaults to an instance with all flags set to <see langword="false"/>.
    /// </summary>
    [JsonPropertyName("options")]
    public TableOptions Options { get; init; } = new();
}
