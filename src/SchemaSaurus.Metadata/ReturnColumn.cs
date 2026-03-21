using System.Diagnostics;

namespace SchemaSaurus.Metadata;

/// <summary>
/// Describes a single column in the result set returned by a
/// <see cref="TableValuedFunction"/>.
/// </summary>
/// <remarks>
/// Inherits all type facets (native type name, <see cref="TypeMapping.DbType"/>, CLR mapping,
/// length, precision, scale, Unicode, and fixed-length) from <see cref="TypeMapping"/>.
/// Unlike <see cref="Column"/>, return columns do not carry identity, computed, or
/// concurrency metadata because they describe query output rather than persisted storage.
/// </remarks>
[Equatable]
[DebuggerDisplay("{Name}")]
public sealed partial class ReturnColumn : TypeMapping
{
    /// <summary>
    /// Column name as declared in the function's <c>RETURNS TABLE</c> definition.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// One-based ordinal position of this column within the function's result set,
    /// reflecting the declaration order in the database catalog.
    /// </summary>
    [JsonPropertyName("ordinalPosition")]
    public required int OrdinalPosition { get; init; }

    /// <summary>
    /// Indicates whether this return column can contain <see langword="null"/> values.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    [JsonPropertyName("isNullable")]
    public bool IsNullable { get; init; }
}
