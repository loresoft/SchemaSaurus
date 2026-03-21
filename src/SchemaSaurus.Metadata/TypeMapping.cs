using System.Diagnostics;

using SchemaSaurus.Metadata.Converters;

namespace SchemaSaurus.Metadata;


/// <summary>
/// Encapsulates the complete type description for a <see cref="Column"/> or <see cref="Parameter"/>.
/// </summary>
/// <remarks>
/// Centralizes all type-related fields — native type name, normalized <see cref="DbType"/>,
/// CLR mapping, and facets (length, precision, scale, Unicode, fixed-length) — so that
/// neither <see cref="Column"/> nor <see cref="Parameter"/> duplicates them independently.
/// </remarks>
[Equatable]
[DebuggerDisplay("{NativeTypeName}")]
public partial class TypeMapping
{
    /// <summary>
    /// Normalized, provider-independent data type identifier
    /// (e.g., <see cref="DbType.String"/>, <see cref="DbType.Int32"/>).
    /// </summary>
    [JsonPropertyName("dbType")]
    [JsonConverter(typeof(JsonStringEnumConverter<DbType>))]
    public required DbType DbType { get; init; }

    /// <summary>
    /// Raw provider type name preserved verbatim for round-tripping
    /// (e.g., <c>"nvarchar(256)"</c>, <c>"jsonb"</c>, <c>"uniqueidentifier"</c>).
    /// Never normalized or simplified.
    /// </summary>
    [JsonPropertyName("nativeTypeName")]
    public required string NativeTypeName { get; init; }

    /// <summary>
    /// The .NET CLR type that this database type maps to.
    /// For nullable columns whose <see cref="DbType"/> maps to a value type, this should be
    /// the <c>Nullable&lt;T&gt;</c> variant (e.g., <c>typeof(int?)</c> rather than <c>typeof(int)</c>).
    /// Serialized as the type's <see cref="Type.FullName"/> string via
    /// <see cref="TypeJsonConverter"/>.
    /// </summary>
    [JsonPropertyName("systemType")]
    [JsonConverter(typeof(TypeJsonConverter))]
    public required Type SystemType { get; init; }

    /// <summary>
    /// Maximum character or byte length for string and binary types
    /// (e.g., <c>256</c> for <c>nvarchar(256)</c>).
    /// <see langword="null"/> when not applicable to the type.
    /// </summary>
    [JsonPropertyName("maxLength")]
    public int? MaxLength { get; init; }

    /// <summary>
    /// Total number of significant digits for numeric and decimal types
    /// (e.g., <c>18</c> for <c>decimal(18, 2)</c>).
    /// <see langword="null"/> when not applicable.
    /// </summary>
    [JsonPropertyName("precision")]
    public int? Precision { get; init; }

    /// <summary>
    /// Number of digits to the right of the decimal point for numeric and decimal types
    /// (e.g., <c>2</c> for <c>decimal(18, 2)</c>).
    /// <see langword="null"/> when not applicable.
    /// </summary>
    [JsonPropertyName("scale")]
    public int? Scale { get; init; }

    /// <summary>
    /// Indicates whether the string type stores Unicode characters.
    /// <see langword="true"/> for types like <c>nvarchar</c> / <c>nchar</c>;
    /// <see langword="false"/> for <c>varchar</c> / <c>char</c>.
    /// <see langword="null"/> when not applicable (non-string types, or providers like
    /// PostgreSQL where all text is inherently Unicode).
    /// </summary>
    [JsonPropertyName("isUnicode")]
    public bool? IsUnicode { get; init; }

    /// <summary>
    /// Indicates whether the type is fixed-length.
    /// <see langword="true"/> for types like <c>char</c> and <c>binary</c>;
    /// <see langword="false"/> for variable-length types like <c>varchar</c> and <c>varbinary</c>.
    /// <see langword="null"/> when not applicable (non-string / non-binary types).
    /// </summary>
    [JsonPropertyName("isFixedLength")]
    public bool? IsFixedLength { get; init; }
}
