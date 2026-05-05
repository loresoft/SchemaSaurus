namespace SchemaSaurus.Metadata.Builders;

/// <summary>
/// Represents a metadata builder that supports provider-specific annotations.
/// </summary>
/// <typeparam name="TBuilder">The concrete builder type.</typeparam>
public interface IAnnotationBuilder<TBuilder>
    where TBuilder : IAnnotationBuilder<TBuilder>
{
    /// <summary>Adds a provider-specific annotation when the value is not <see langword="null"/>.</summary>
    /// <param name="key">The annotation key.</param>
    /// <param name="value">The annotation value. When <see langword="null"/>, no annotation is added.</param>
    /// <returns>The current builder instance.</returns>
    TBuilder WithAnnotation(string key, object? value);
}
