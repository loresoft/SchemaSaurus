namespace SchemaSaurus.Metadata.Builders;

/// <summary>
/// Represents a metadata builder that supports provider-specific annotations.
/// </summary>
/// <typeparam name="TBuilder">The concrete builder type.</typeparam>
public interface IAnnotationBuilder<TBuilder>
    where TBuilder : IAnnotationBuilder<TBuilder>
{
    /// <summary>Adds a provider-specific annotation.</summary>
    TBuilder WithAnnotation(string key, object? value);
}
