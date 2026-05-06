namespace SchemaSaurus.Metadata;

/// <summary>
/// Represents a metadata object that is identified by a schema-qualified name.
/// </summary>
public interface IQualifiedName
{
    /// <summary>
    /// Gets the schema-qualified object name.
    /// </summary>
    SchemaQualifiedName QualifiedName { get; init; }

    /// <summary>
    /// Gets the unqualified object name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the schema name, or <see langword="null"/> when the provider does not use schema.
    /// </summary>
    string? Schema { get; }
}
