namespace SchemaSaurus.Metadata;

/// <summary>
/// Immutable value type that uniquely identifies a database object by its schema and name.
/// </summary>
/// <remarks>
/// Equality is ordinal and case-sensitive, matching typical database identifier comparison
/// semantics. For case-insensitive lookups, use the <c>Find*</c> methods on
/// <see cref="DatabaseModelExtensions"/> which apply
/// <see cref="StringComparison.OrdinalIgnoreCase"/>.
/// </remarks>
public readonly record struct SchemaQualifiedName
{
    /// <summary>
    /// Schema (or owner) name
    /// (e.g., <c>"dbo"</c> for SQL Server, <c>"public"</c> for PostgreSQL).
    /// <see langword="null"/> for providers that do not use schemas (e.g., SQLite).
    /// </summary>
    [JsonPropertyName("schema")]
    public string? Schema { get; init; }

    /// <summary>
    /// Unqualified object name (e.g., <c>"Orders"</c>, <c>"uspGetCustomer"</c>).
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Returns the canonical <c>"schema.name"</c> representation.
    /// When <see cref="Schema"/> is <see langword="null"/>, returns just the
    /// <see cref="Name"/>.
    /// </summary>
    /// <returns>
    /// A string in the form <c>"schema.name"</c>, or <c>"name"</c> when
    /// <see cref="Schema"/> is <see langword="null"/> or empty.
    /// </returns>
    public override string ToString()
        => string.IsNullOrEmpty(Schema) ? Name : $"{Schema}.{Name}";

    /// <summary>
    /// Parses a <c>"schema.name"</c> or bare <c>"name"</c> string into a
    /// <see cref="SchemaQualifiedName"/>. The first dot is treated as the schema separator;
    /// any subsequent dots remain part of the object name.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>A <see cref="SchemaQualifiedName"/> instance with <see cref="Schema"/> set
    /// to <see langword="null"/> when no dot is present.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value"/> is <see langword="null"/>.
    /// </exception>
    public static SchemaQualifiedName Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var dot = value.IndexOf('.');

        return dot < 0
            ? new SchemaQualifiedName { Name = value }
            : new SchemaQualifiedName { Schema = value[..dot], Name = value[(dot + 1)..] };
    }

    /// <summary>
    /// Explicit conversion from a <c>"schema.name"</c> or bare <c>"name"</c> string.
    /// Equivalent to calling <see cref="Parse"/>.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    public static explicit operator SchemaQualifiedName(string value) => Parse(value);

    /// <summary>
    /// Implicit conversion to the canonical string representation.
    /// Equivalent to calling <see cref="ToString"/>.
    /// </summary>
    /// <param name="name">The <see cref="SchemaQualifiedName"/> to convert.</param>
    public static implicit operator string(SchemaQualifiedName name) => name.ToString();
}
