using System.Diagnostics;

using SchemaSaurus.Metadata.Converters;

namespace SchemaSaurus.Metadata;

/// <summary>
/// Represents a parameter of a <see cref="StoredProcedure"/>,
/// <see cref="ScalarFunction"/>, or <see cref="TableValuedFunction"/>.
/// </summary>
/// <remarks>
/// Inherits all type facets (native type name, <see cref="TypeMapping.DbType"/>, CLR mapping,
/// length, precision, scale, Unicode, and fixed-length) from <see cref="TypeMapping"/> and adds
/// parameter-level metadata such as ordinal position, direction, and default value.
/// </remarks>
[Equatable]
[DebuggerDisplay("{Name}")]
public sealed partial class Parameter : TypeMapping, IAnnotatable
{
    /// <summary>
    /// Parameter name, including any provider-specific prefix
    /// (e.g., <c>"@customerId"</c> for SQL Server, <c>"p_customer_id"</c> for PostgreSQL).
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// One-based ordinal position of the parameter within the routine's signature,
    /// reflecting the declaration order in the database catalog.
    /// </summary>
    [JsonPropertyName("ordinal")]
    public required int Ordinal { get; init; }

    /// <summary>
    /// Indicates whether the parameter is input (<see cref="ParameterDirection.Input"/>),
    /// output (<see cref="ParameterDirection.Output"/>), bidirectional
    /// (<see cref="ParameterDirection.InputOutput"/>), or a return value
    /// (<see cref="ParameterDirection.ReturnValue"/>).
    /// Defaults to <see cref="ParameterDirection.Input"/>.
    /// </summary>
    [JsonPropertyName("direction")]
    public ParameterDirection Direction { get; init; } = ParameterDirection.Input;

    /// <summary>
    /// Default value expression as a raw SQL string
    /// (e.g., <c>"0"</c>, <c>"NULL"</c>, <c>"GETDATE()"</c>).
    /// <see langword="null"/> when no default is defined.
    /// </summary>
    [JsonPropertyName("defaultValueSql")]
    public string? DefaultValueSql { get; init; }

    /// <inheritdoc />
    [DictionaryEquality]
    [JsonPropertyName("annotations")]
    [JsonConverter(typeof(ReadOnlyDictionaryConverter<string, object?>))]
    public IReadOnlyDictionary<string, object?> Annotations { get; init; } = new Dictionary<string, object?>();
}
