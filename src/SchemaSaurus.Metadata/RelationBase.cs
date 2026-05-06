using System.Diagnostics;

using SchemaSaurus.Metadata.Converters;

namespace SchemaSaurus.Metadata;

/// <summary>
/// Abstract base class for column-bearing database objects (<see cref="Table"/> and <see cref="View"/>).
/// </summary>
/// <remarks>
/// Encapsulates the structural members shared by all relations — columns, indexes, triggers,
/// and provider annotations — so that generic code can operate on any relation without
/// knowing its concrete type.
/// </remarks>
[Equatable]
[DebuggerDisplay("{QualifiedName}")]
public abstract partial class RelationBase : IAnnotatable, IQualifiedName
{
    /// <summary>
    /// Schema-qualified name that uniquely identifies this relation within the database.
    /// </summary>
    [JsonPropertyName("qualifiedName")]
    public required SchemaQualifiedName QualifiedName { get; init; }

    /// <summary>
    /// Gets the unqualified name component of the object.
    /// </summary>
    [JsonIgnore]
    [IgnoreEquality]
    public string Name => QualifiedName.Name;

    /// <summary>
    /// Gets the schema component of the qualified name, if available.
    /// </summary>
    [JsonIgnore]
    [IgnoreEquality]
    public string? Schema => QualifiedName.Schema;

    /// <summary>
    /// Back-reference to the owning <see cref="DatabaseModel"/>.
    /// Populated after deserialization during model fixup; excluded from JSON serialization
    /// and equality comparison.
    /// </summary>
    [JsonIgnore]
    [IgnoreEquality]
    public DatabaseModel Database { get; internal set; } = null!;

    /// <summary>
    /// Columns belonging to this relation, ordered by <see cref="Column.OrdinalPosition"/>.
    /// Defaults to an empty collection.
    /// </summary>
    [SequenceEquality]
    [JsonPropertyName("columns")]
    public IReadOnlyList<Column> Columns { get; init; } = [];

    /// <summary>
    /// Indexes (clustered, non-clustered, filtered, etc.) defined on this relation.
    /// Defaults to an empty collection.
    /// </summary>
    [SequenceEquality]
    [JsonPropertyName("indexes")]
    public IReadOnlyList<Index> Indexes { get; init; } = [];

    /// <summary>
    /// DML triggers (INSERT, UPDATE, DELETE) defined on this relation.
    /// Defaults to an empty collection.
    /// </summary>
    [SequenceEquality]
    [JsonPropertyName("triggers")]
    public IReadOnlyList<Trigger> Triggers { get; init; } = [];

    /// <summary>
    /// Human-readable description or comment attached to this relation.
    /// Sourced from <c>MS_Description</c> extended properties (SQL Server) or
    /// <c>COMMENT ON TABLE</c> / <c>COMMENT ON VIEW</c> (PostgreSQL / MySQL / Oracle).
    /// <see langword="null"/> when no description has been set.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <inheritdoc />
    [DictionaryEquality]
    [JsonPropertyName("annotations")]
    [JsonConverter(typeof(ReadOnlyDictionaryConverter<string, object?>))]
    public IReadOnlyDictionary<string, object?> Annotations { get; init; } = new Dictionary<string, object?>();
}
