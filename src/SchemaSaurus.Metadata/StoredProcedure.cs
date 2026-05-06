using System.Diagnostics;

using SchemaSaurus.Metadata.Converters;

namespace SchemaSaurus.Metadata;

/// <summary>
/// Represents a stored procedure in the database.
/// </summary>
/// <remarks>
/// A stored procedure encapsulates a reusable block of SQL logic that accepts
/// zero or more <see cref="Parameters"/> and is invoked by name. For routines
/// that return scalar values, see <see cref="ScalarFunction"/>; for those that
/// return result sets, see <see cref="TableValuedFunction"/>.
/// </remarks>
[Equatable]
[DebuggerDisplay("{QualifiedName}")]
public sealed partial class StoredProcedure : IAnnotatable, IQualifiedName
{
    /// <summary>
    /// Schema-qualified name that uniquely identifies this stored procedure within the database.
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
    /// Ordered list of parameters, sorted by <see cref="Parameter.Ordinal"/>.
    /// Defaults to an empty collection for parameterless procedures.
    /// </summary>
    [SequenceEquality]
    [JsonPropertyName("parameters")]
    public IReadOnlyList<Parameter> Parameters { get; init; } = [];

    /// <summary>
    /// SQL definition of the stored procedure.
    /// <see langword="null"/> when the caller lacks the necessary permissions to read
    /// the definition or when the provider does not expose it.
    /// </summary>
    [JsonPropertyName("definition")]
    public string? Definition { get; init; }

    /// <summary>
    /// Human-readable description or comment for the procedure, sourced from
    /// <c>MS_Description</c> extended properties (SQL Server) or
    /// <c>COMMENT ON PROCEDURE</c> (PostgreSQL / MySQL / Oracle).
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
