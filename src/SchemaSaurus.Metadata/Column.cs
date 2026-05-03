using System.Diagnostics;

using SchemaSaurus.Metadata.Converters;

namespace SchemaSaurus.Metadata;

/// <summary>
/// Represents a column in a <see cref="Table"/> or <see cref="View"/>.
/// </summary>
/// <remarks>
/// Inherits all type facets (native type name, <see cref="TypeMapping.DbType"/>, CLR mapping,
/// length, precision, scale, Unicode, and fixed-length) from <see cref="TypeMapping"/> and adds
/// column-level metadata such as nullability, identity, computed expressions, and concurrency
/// markers.
/// </remarks>
[Equatable]
[DebuggerDisplay("{Name} ({NativeTypeName})")]
public sealed partial class Column : TypeMapping, IAnnotatable
{
    /// <summary>
    /// Column name as defined in the database catalog.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// One-based ordinal position of the column within its parent relation,
    /// as reported by the database engine.
    /// </summary>
    [JsonPropertyName("ordinalPosition")]
    public int OrdinalPosition { get; init; }

    /// <summary>
    /// Indicates whether the column accepts <see langword="null"/> values.
    /// </summary>
    [JsonPropertyName("isNullable")]
    public bool IsNullable { get; init; }

    /// <summary>
    /// Default value expression as a raw SQL string
    /// (e.g., <c>"(getdate())"</c>, <c>"'active'"</c>).
    /// <see langword="null"/> when no default is defined.
    /// </summary>
    [JsonPropertyName("defaultValueSql")]
    public string? DefaultValueSql { get; init; }

    /// <summary>
    /// Indicates whether this column is an identity / auto-increment column.
    /// When <see langword="true"/>, <see cref="IdentitySeed"/> and
    /// <see cref="IdentityIncrement"/> carry the generation parameters.
    /// </summary>
    [JsonPropertyName("isIdentity")]
    public bool IsIdentity { get; init; }

    /// <summary>
    /// Seed value for an identity column (the first value generated).
    /// <see langword="null"/> when <see cref="IsIdentity"/> is <see langword="false"/>.
    /// </summary>
    [JsonPropertyName("identitySeed")]
    public long? IdentitySeed { get; init; }

    /// <summary>
    /// Increment step between successive identity values.
    /// <see langword="null"/> when <see cref="IsIdentity"/> is <see langword="false"/>.
    /// </summary>
    [JsonPropertyName("identityIncrement")]
    public long? IdentityIncrement { get; init; }

    /// <summary>
    /// Indicates whether this is a computed (virtual or persisted) column.
    /// When <see langword="true"/>, <see cref="ComputedColumnSql"/> contains the defining
    /// expression and <see cref="IsStored"/> indicates whether the value is persisted to disk.
    /// </summary>
    [JsonPropertyName("isComputed")]
    public bool IsComputed { get; init; }

    /// <summary>
    /// SQL expression that defines the computed column value
    /// (e.g., <c>"([Quantity] * [UnitPrice])"</c>).
    /// <see langword="null"/> when <see cref="IsComputed"/> is <see langword="false"/>.
    /// </summary>
    [JsonPropertyName("computedColumnSql")]
    public string? ComputedColumnSql { get; init; }

    /// <summary>
    /// Indicates whether a computed column is persisted (SQL Server <c>PERSISTED</c>) or
    /// stored (PostgreSQL <c>STORED</c>) on disk rather than recalculated on every read.
    /// Only meaningful when <see cref="IsComputed"/> is <see langword="true"/>.
    /// </summary>
    [JsonPropertyName("isStored")]
    public bool IsStored { get; init; }

    /// <summary>
    /// Indicates whether this column is a row version / timestamp column used for
    /// optimistic concurrency (SQL Server <c>ROWVERSION</c> / <c>TIMESTAMP</c>,
    /// PostgreSQL <c>xmin</c> system column).
    /// </summary>
    [JsonPropertyName("isRowVersion")]
    public bool IsRowVersion { get; init; }

    /// <summary>
    /// Indicates whether this column acts as a concurrency token for optimistic
    /// concurrency control. A column with <see cref="IsRowVersion"/> set to
    /// <see langword="true"/> is implicitly a concurrency token.
    /// </summary>
    [JsonPropertyName("isConcurrencyToken")]
    public bool IsConcurrencyToken { get; init; }

    /// <summary>
    /// Collation override for this column.
    /// <see langword="null"/> when the column inherits the table- or database-level collation.
    /// </summary>
    [JsonPropertyName("collation")]
    public string? Collation { get; init; }

    /// <summary>
    /// Human-readable description or comment for the column, sourced from
    /// <c>MS_Description</c> extended properties (SQL Server) or
    /// <c>COMMENT ON COLUMN</c> (PostgreSQL / MySQL / Oracle).
    /// <see langword="null"/> when no description has been set.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Back-reference to the parent <see cref="RelationBase"/> (table or view).
    /// Populated during <see cref="DatabaseModelExtensions.ResolveReferences"/>; excluded from
    /// JSON serialization and equality comparison.
    /// <see langword="null"/> for columns belonging to a <see cref="UserDefinedType"/>.
    /// </summary>
    [JsonIgnore]
    [IgnoreEquality]
    public RelationBase? Parent { get; internal set; }

    /// <inheritdoc />
    [DictionaryEquality]
    [JsonPropertyName("annotations")]
    [JsonConverter(typeof(ReadOnlyDictionaryConverter<string, object?>))]
    public IReadOnlyDictionary<string, object?> Annotations { get; init; } = new Dictionary<string, object?>();
}
