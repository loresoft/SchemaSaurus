using System.Diagnostics;

using SchemaSaurus.Metadata.Converters;

namespace SchemaSaurus.Metadata;

/// <summary>
/// Represents a foreign key constraint owned by a <see cref="Table"/>
/// (the dependent / referencing side).
/// </summary>
/// <remarks>
/// Each foreign key identifies the principal (referenced) table via
/// <see cref="PrincipalTableName"/> and maps one or more dependent columns to their
/// corresponding principal columns through <see cref="ColumnMappings"/>.
/// Referential actions (<see cref="OnDelete"/> / <see cref="OnUpdate"/>) control
/// cascade behavior when principal rows are modified.
/// </remarks>
[Equatable]
[DebuggerDisplay("{Name}")]
public sealed partial class ForeignKey : IAnnotatable
{
    /// <summary>
    /// Catalog-level name that uniquely identifies this foreign key constraint
    /// within the database (e.g., <c>"FK_Order_Customer"</c>).
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Schema-qualified name of the principal (referenced) table.
    /// Stored for serialization; resolved to <see cref="PrincipalTable"/> during
    /// <see cref="DatabaseModelExtensions.ResolveReferences"/>.
    /// </summary>
    [JsonPropertyName("principalTableName")]
    public required SchemaQualifiedName PrincipalTableName { get; init; }

    /// <summary>
    /// Resolved reference to the principal (referenced) <see cref="Table"/>.
    /// Populated during <see cref="DatabaseModelExtensions.ResolveReferences"/>; excluded
    /// from JSON serialization and equality comparison.
    /// </summary>
    [JsonIgnore]
    [IgnoreEquality]
    public Table PrincipalTable { get; internal set; } = null!;

    /// <summary>
    /// Back-reference to the dependent (owning / referencing) <see cref="Table"/>.
    /// Populated during <see cref="DatabaseModelExtensions.ResolveReferences"/>; excluded
    /// from JSON serialization and equality comparison.
    /// </summary>
    [JsonIgnore]
    [IgnoreEquality]
    public Table DependentTable { get; internal set; } = null!;

    /// <summary>
    /// Ordered column mappings between the dependent and principal tables,
    /// matching the declaration order of the constraint definition.
    /// Defaults to an empty collection.
    /// </summary>
    [SequenceEquality]
    [JsonPropertyName("columnMappings")]
    public IReadOnlyList<ForeignKeyColumnMapping> ColumnMappings { get; init; } = [];

    /// <summary>
    /// Referential action taken on dependent rows when the principal row is deleted
    /// (e.g., <c>CASCADE</c>, <c>SET NULL</c>, <c>NO ACTION</c>).
    /// Defaults to <see cref="ReferentialAction.NoAction"/>.
    /// </summary>
    [JsonPropertyName("onDelete")]
    public ReferentialAction OnDelete { get; init; } = ReferentialAction.NoAction;

    /// <summary>
    /// Referential action taken on dependent rows when the principal row's key is updated
    /// (e.g., <c>CASCADE</c>, <c>SET NULL</c>, <c>NO ACTION</c>).
    /// Defaults to <see cref="ReferentialAction.NoAction"/>.
    /// </summary>
    [JsonPropertyName("onUpdate")]
    public ReferentialAction OnUpdate { get; init; } = ReferentialAction.NoAction;

    /// <summary>
    /// Indicates whether this foreign key constraint is currently disabled.
    /// Primarily a SQL Server concept (<c>ALTER TABLE … NOCHECK CONSTRAINT</c>).
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
