using System.Diagnostics;

namespace SchemaSaurus.Metadata;

/// <summary>
/// Maps a single column on the dependent (referencing) table to the corresponding column
/// on the principal (referenced) table within a <see cref="ForeignKey"/> constraint.
/// </summary>
/// <remarks>
/// Column references are serialized by name (<see cref="DependentColumnName"/> and
/// <see cref="PrincipalColumnName"/>) and resolved to <see cref="Column"/> instances
/// during <see cref="DatabaseModelExtensions.ResolveReferences"/>.
/// </remarks>
[Equatable]
[DebuggerDisplay("{DependentColumnName} → {PrincipalColumnName}")]
public sealed partial class ForeignKeyColumnMapping
{
    /// <summary>
    /// Name of the column on the dependent (referencing) table.
    /// Acts as the serialization key and is used to resolve the
    /// <see cref="DependentColumn"/> instance during
    /// <see cref="DatabaseModelExtensions.ResolveReferences"/>.
    /// </summary>
    [JsonPropertyName("dependentColumnName")]
    public required string DependentColumnName { get; init; }

    /// <summary>
    /// Name of the column on the principal (referenced) table.
    /// Acts as the serialization key and is used to resolve the
    /// <see cref="PrincipalColumn"/> instance during
    /// <see cref="DatabaseModelExtensions.ResolveReferences"/>.
    /// </summary>
    [JsonPropertyName("principalColumnName")]
    public required string PrincipalColumnName { get; init; }

    /// <summary>
    /// Resolved <see cref="Column"/> on the dependent table that
    /// <see cref="DependentColumnName"/> refers to.
    /// Populated during <see cref="DatabaseModelExtensions.ResolveReferences"/>; excluded
    /// from JSON serialization and equality comparison.
    /// </summary>
    [JsonIgnore]
    [IgnoreEquality]
    public Column DependentColumn { get; internal set; } = null!;

    /// <summary>
    /// Resolved <see cref="Column"/> on the principal table that
    /// <see cref="PrincipalColumnName"/> refers to.
    /// Populated during <see cref="DatabaseModelExtensions.ResolveReferences"/>; excluded
    /// from JSON serialization and equality comparison.
    /// </summary>
    [JsonIgnore]
    [IgnoreEquality]
    public Column PrincipalColumn { get; internal set; } = null!;
}
