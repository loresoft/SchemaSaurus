using System.Diagnostics;

using SchemaSaurus.Metadata.Converters;

namespace SchemaSaurus.Metadata;

/// <summary>
/// Represents a user-defined type (UDT) in the database.
/// </summary>
/// <remarks>
/// The <see cref="Kind"/> property determines the structural category of the type and
/// which additional members are populated:
/// <list type="bullet">
///   <item><see cref="UserDefinedTypeKind.TableType"/> — <see cref="Columns"/> describes the table structure.</item>
///   <item><see cref="UserDefinedTypeKind.Enum"/> — <see cref="EnumLabels"/> lists the allowed values.</item>
///   <item><see cref="UserDefinedTypeKind.Alias"/> — the inherited <see cref="TypeMapping"/> facets describe the underlying system type.</item>
/// </list>
/// Inherits base-type facets (e.g. <see cref="TypeMapping.MaxLength"/>,
/// <see cref="TypeMapping.Precision"/>) from <see cref="TypeMapping"/>.
/// </remarks>
[Equatable]
[DebuggerDisplay("{QualifiedName} ({Kind})")]
public sealed partial class UserDefinedType : TypeMapping, IAnnotatable, IQualifiedName
{
    /// <summary>
    /// Schema-qualified name of the user-defined type.
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
    /// Structural kind of this user-defined type.
    /// </summary>
    /// <remarks>
    /// See <see cref="UserDefinedTypeKind"/> for the supported categories
    /// (e.g. <see cref="UserDefinedTypeKind.Alias"/>,
    /// <see cref="UserDefinedTypeKind.TableType"/>,
    /// <see cref="UserDefinedTypeKind.Enum"/>).
    /// </remarks>
    [JsonPropertyName("kind")]
    public UserDefinedTypeKind Kind { get; init; }

    /// <summary>
    /// Column descriptors for table-valued parameter types
    /// (<see cref="UserDefinedTypeKind.TableType"/>).
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> for other <see cref="Kind"/> values.
    /// Note that <see cref="Column.Parent"/> will be <see langword="null"/> for these
    /// columns because they do not belong to a <see cref="Table"/> or <see cref="View"/>.
    /// </remarks>
    [SequenceEquality]
    [JsonPropertyName("columns")]
    public IReadOnlyList<Column>? Columns { get; init; }

    /// <summary>
    /// Ordered list of label strings for PostgreSQL enum types
    /// (<see cref="UserDefinedTypeKind.Enum"/>).
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> for other <see cref="Kind"/> values.
    /// Labels are returned in the declaration order defined by
    /// <c>CREATE TYPE … AS ENUM (…)</c>.
    /// </remarks>
    [SequenceEquality]
    [JsonPropertyName("enumLabels")]
    public IReadOnlyList<string>? EnumLabels { get; init; }

    /// <inheritdoc />
    [DictionaryEquality]
    [JsonPropertyName("annotations")]
    [JsonConverter(typeof(ReadOnlyDictionaryConverter<string, object?>))]
    public IReadOnlyDictionary<string, object?> Annotations { get; init; } = new Dictionary<string, object?>();
}
