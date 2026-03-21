namespace SchemaSaurus.Metadata;

/// <summary>
/// Represents a database view (standard or materialized/indexed).
/// </summary>
/// <remarks>
/// Extends <see cref="RelationBase"/> with the SQL definition text and a materialization flag.
/// Unlike <see cref="Table"/>, views do not own constraints — primary keys, unique constraints,
/// check constraints, and foreign keys exist only on tables.
/// </remarks>
[Equatable]
public sealed partial class View : RelationBase
{
    /// <summary>
    /// The SQL <c>SELECT</c> statement that defines the view body.
    /// <see langword="null"/> when the caller lacks the necessary permissions to read the
    /// definition or when the provider does not expose it.
    /// </summary>
    [JsonPropertyName("definition")]
    public string? Definition { get; init; }

    /// <summary>
    /// Indicates whether this view persists its result set to disk — a materialized view
    /// (PostgreSQL <c>CREATE MATERIALIZED VIEW</c>) or an indexed view
    /// (SQL Server view with a unique clustered index).
    /// Defaults to <see langword="false"/> for standard (non-materialized) views.
    /// </summary>
    [JsonPropertyName("isMaterialized")]
    public bool IsMaterialized { get; init; }
}
