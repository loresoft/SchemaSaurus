using System.Diagnostics;

using SchemaSaurus.Metadata.Converters;

namespace SchemaSaurus.Metadata;

/// <summary>
/// Represents a table-valued user-defined function in the database.
/// </summary>
/// <remarks>
/// Table-valued functions return a result set (one or more columns) and can be used in
/// the <c>FROM</c> clause of a query. For functions that return a single scalar value,
/// see <see cref="ScalarFunction"/>.
/// </remarks>
[Equatable]
[DebuggerDisplay("{QualifiedName}")]
public sealed partial class TableValuedFunction : IAnnotatable, IQualifiedName
{
    /// <summary>
    /// Schema-qualified name of the function.
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
    /// Ordered list of input parameters, sorted by <see cref="Parameter.Ordinal"/>.
    /// </summary>
    /// <remarks>
    /// Defaults to an empty list when the function accepts no parameters.
    /// </remarks>
    [SequenceEquality]
    [JsonPropertyName("parameters")]
    public IReadOnlyList<Parameter> Parameters { get; init; } = [];

    /// <summary>
    /// Descriptors of the columns in the result set returned by the function.
    /// </summary>
    /// <remarks>
    /// May be empty for multi-statement table-valued functions whose columns are not
    /// statically declared in the catalog. Defaults to an empty list.
    /// </remarks>
    [SequenceEquality]
    [JsonPropertyName("returnColumns")]
    public IReadOnlyList<ReturnColumn> ReturnColumns { get; init; } = [];

    /// <summary>
    /// SQL definition of the function.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> when the caller lacks the necessary permissions to read
    /// the definition, or when the database does not expose function bodies.
    /// </remarks>
    [JsonPropertyName("definition")]
    public string? Definition { get; init; }

    /// <summary>
    /// Human-readable description or comment for the function.
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
