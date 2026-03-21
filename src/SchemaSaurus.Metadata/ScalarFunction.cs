using System.Diagnostics;

using SchemaSaurus.Metadata.Converters;

namespace SchemaSaurus.Metadata;

/// <summary>
/// Represents a scalar-valued user-defined function in the database.
/// </summary>
/// <remarks>
/// A scalar function accepts zero or more <see cref="Parameters"/> and returns a single
/// value described by <see cref="ReturnType"/>. For functions that return a result set,
/// see <see cref="TableValuedFunction"/>.
/// </remarks>
[Equatable]
[DebuggerDisplay("{SchemaQualifiedName}")]
public sealed partial class ScalarFunction : IAnnotatable
{
    /// <summary>
    /// Schema-qualified name that uniquely identifies this function within the database.
    /// </summary>
    [JsonPropertyName("schemaQualifiedName")]
    public required SchemaQualifiedName SchemaQualifiedName { get; init; }

    /// <summary>
    /// <see cref="TypeMapping"/> describing the scalar value returned by the function,
    /// including its native type name, <see cref="TypeMapping.DbType"/>, and CLR mapping.
    /// </summary>
    [JsonPropertyName("returnType")]
    public required TypeMapping ReturnType { get; init; }

    /// <summary>
    /// Ordered list of parameters, sorted by <see cref="Parameter.Ordinal"/>.
    /// Defaults to an empty collection for parameterless functions.
    /// </summary>
    [SequenceEquality]
    [JsonPropertyName("parameters")]
    public IReadOnlyList<Parameter> Parameters { get; init; } = [];

    /// <summary>
    /// Indicates whether this function is deterministic — it always returns the same
    /// result given the same inputs, with no observable side effects.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    [JsonPropertyName("isDeterministic")]
    public bool IsDeterministic { get; init; }

    /// <summary>
    /// SQL definition of the function.
    /// <see langword="null"/> when the caller lacks the necessary permissions to read
    /// the definition or when the provider does not expose it.
    /// </summary>
    [JsonPropertyName("definition")]
    public string? Definition { get; init; }

    /// <inheritdoc />
    [DictionaryEquality]
    [JsonPropertyName("annotations")]
    [JsonConverter(typeof(ReadOnlyDictionaryConverter<string, object?>))]
    public IReadOnlyDictionary<string, object?> Annotations { get; init; } = new Dictionary<string, object?>();
}
